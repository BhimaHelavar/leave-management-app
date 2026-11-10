# US-01: Sign in and see only what my role allows

**Jira:** SCRUM-8 · Story · In Progress · High · epic SCRUM-7 (Leave Management App: MVP) · SCRUM Sprint 4 (active)
**Code:** PR #2 (open) · branch `SCRUM-8-US-01-Sign-in-and-see-only-what-my-role-allows` · head `bc408b9` · 54 files (+1771, -167)
**Written:** 2026-11-10. I read the code and the design documents. I did not build or run anything. Results marked "the caller reported" were run by the person who launched me, on the owner's PC, before the two commits.

## What the story is

A person signs in and lands on their own dashboard. They only see the links their roles allow. If an Employee tries to use an Admin-only part of the API, the server says 403 (forbidden) and writes a line in the audit table saying who tried and which door.

Everything here is "dev mode": there is no password. You pick a demo person from a list and the API gives you a signed token.

**Why it matters:** it proves the core security idea of the app: the server decides, and every refusal leaves a trace (requirement BR-01 and BR-07).

## What the code delivers

Sign-in (criterion 1):

```
Login screen --GET /api/dev/users--> API --> people.Employee + EmployeeRole
   |  (click a person)
   +--POST /api/dev/token {employeeId}--> DevTokenService --> signed JWT (60 min)
   |
AuthService keeps token in a signal (memory only)
   |
authInterceptor adds "Authorization: Bearer ..." to calls to our API
   |
authGuard + roleGuard --> /dashboard ; top bar shows links for the roles
```

Refusal (criterion 2):

```
Employee --GET /api/admin/ping + Bearer--> JWT check --> policy RequireAdmin
                                                  |
        AuditingAuthorizationResultHandler (403 and signed in?)
          |-- IAuditWriter --> audit.AuditEvent (AccessDenied, /api/admin/ping,
          |                    {"policy":"RequireAdmin"}, actor = sub claim)
          +-- default handler --> 403 ProblemDetails
(no token / bad token: 401, no audit row.   Admin: 204)
```

1. The Login screen asks `GET /api/dev/users` for the active people and their roles. This endpoint is anonymous on purpose and rate limited (30 per minute per address).
2. Clicking a person sends `POST /api/dev/token`. `DevTokenService` reads the person and roles from the database and signs a JWT (a signed text token) with claims `sub`, `name`, `email`, `role`. The manager is not in the token because it can change.
3. `AuthService` stores the token in a signal. A signal lives in memory, so a page reload signs you out. This is deliberate (security rule 7).
4. `authInterceptor` adds the Bearer header only for calls to our own API. A 401 with a token clears the session and goes to `/login`.
5. `authGuard` sends a signed-out person to `/login`. `roleGuard` sends a person without the role to the not-found screen. Both are screen conveniences only.
6. On the API, JWT validation, a fallback policy (everything needs a signed-in user) and three role policies are set up in `AuthSetup.cs`.
7. `AuditingAuthorizationResultHandler` runs after the authorization decision. If the result is Forbidden and the user is signed in, it writes one `AccessDenied` row. Then the normal framework handler returns the 403. If the audit write fails, it logs the trace id and still returns 403.

**Example:** Priya (an Employee, id 4 in the seed data) signs in at `/login`. She sees Dashboard and My requests. A script calls `GET /api/admin/ping` with her token. The API answers 403. One row appears in `audit.AuditEvent` with her id as actor. Then an Admin signs in and gets 204 from the same endpoint, with no audit row.

## Acceptance criteria: where each one is delivered

| Criterion (from Jira) | Delivered by | State |
| --- | --- | --- |
| 1. Valid dev-mode user signs in and lands on their dashboard | `Login` (`login.ts`), `AuthService.signInAs`, `DevTokenService`, `AuthEndpoints`, guards, `app.routes.ts` (navigate to `/dashboard`). Unit tests: `login.spec.ts`, `auth.service.spec.ts`, `auth.guard.spec.ts`. | Partly. Code and unit tests exist. API token issue was checked by the caller. Nobody has clicked it in a browser (task T006 open). |
| 2. Employee opens /admin: 403 and the attempt is audited | `AdminEndpoints.cs` (`GET /api/admin/ping`, `RequireAdmin`), `AuditingAuthorizationResultHandler.cs`, `AuditWriter.cs` | Done for the API, with a wording difference (below). The caller reported 403 for Employee, Manager and HR, 204 for Admin, and four audit rows for four 403s. No automated back-end test. |

**Wording note:** Jira says "/admin". The Angular shell has no `/admin` path (its Admin links are `leave-types` and `holidays`). The check is on the API probe `GET /api/admin/ping`. If an Employee types `/leave-types` in the browser, `roleGuard` shows the not-found screen. That is not a 403 and writes no audit row.

## Code changes

The PR holds four things. Only the first two belong to the story.

**Back end (`src/`)**

| File | What it does |
| --- | --- |
| `Api/Auth/AuthSetup.cs` | Start-up checks, JWT validation, fallback policy, role policies, rate limiter. Part of 4c (criterion 1). |
| `Api/Auth/AuthEndpoints.cs` | `/api/me`, `/api/dev/users`, `/api/dev/token` (dev ones only in Dev mode). Part of 4c. |
| `Api/Auth/DevTokenService.cs`, `DevTokenOptions.cs`, `Policies.cs` | Sign the token; settings; policy names. Part of 4c. |
| `Api/Program.cs`, `Api/LeaveManagement.Api.csproj`, `appsettings.Development.json` | Wire the middleware order (rate limiter, authentication, authorization); JwtBearer package; throwaway dev key. |
| `Api/Auth/AuditingAuthorizationResultHandler.cs` | Writes the `AccessDenied` row on 403. Criterion 2. |
| `Api/Admin/AdminEndpoints.cs` | The Admin-only probe `GET /api/admin/ping`, returns 204. Criterion 2. Its comment says to remove it when real Admin features arrive. |
| `Application/Auditing/IAuditWriter.cs` | Interface and `AuditRecord`. Criterion 2. |
| `Infrastructure/Persistence/AuditWriter.cs`, `DependencyInjection.cs` | Inserts one `AuditEvent`, time from `TimeProvider`, text cut to column sizes. Criterion 2. |

**Front end (`web/src/app/`)**

| File | What it does |
| --- | --- |
| `features/login/login.ts`, `.html`, `.scss` | Dev sign-in screen: list of people as buttons, loading and error states. Criterion 1. |
| `auth/auth.service.ts`, `models.ts` | Token in a signal; `devUsers()`, `signInAs()`, `signOut()`. The T-02 stub people were removed. |
| `auth/auth.interceptor.ts` | Bearer header for our API only; 401 clears session. Skips requests until config is ready (bug fix, see Findings). |
| `auth/auth.guard.ts`, `app.routes.ts`, `app.config.ts` | Signed-out goes to `/login`; `/login` route; interceptor registered. |
| `layout/top-bar.ts`, `.html`, `features/placeholder/placeholder.ts`, `config/config-error.html` | Show the signed-in person and sign out; small adjustments. Not read line by line. |

**Tests**

| File | What it checks |
| --- | --- |
| `auth/auth.interceptor.spec.ts` | Header added for our API only; 401 signs out; 500 does not; config-not-ready regression. |
| `auth/auth.service.spec.ts`, `auth.guard.spec.ts`, `features/login/login.spec.ts` | Sign in, sign out, guard redirect, login states. |
| `layout/top-bar.spec.ts`, `app.routes.spec.ts`, `app.spec.ts`, `role-guard.spec.ts`, `config/runtime-config.spec.ts` | Updated for the new auth service. Not read in detail. |

The caller reported 13 files / 74 tests passing. There are no back-end automated tests (Phase 5).

**Docs and tooling (not the story)**

| File | What it is |
| --- | --- |
| `ai/specs/us01-sign-in-by-role/*` (spec, plan, research, data-model, quickstart, tasks, contracts, checklist) | Spec set for SCRUM-8. Related. |
| `ai/specs/4c-dev-authentication/tasks.md` | 4c task ticks and notes. Related. |
| `.claude/skills/ci-check/SKILL.md` | New skill that runs the CI checks locally. Unrelated to the story. |
| `.github/workflows/checks.yml` | CI now calls `npm run format:check`, `test:ci`, `audit:ci`. Tooling. |
| `web/package.json`, `.prettierrc`, `angular.json`, `tsconfig.app.json`, `tsconfig.spec.json`, `src/index.html`, `src/main.ts` | Scripts and formatting/config changes. Tooling, not read in detail. |

## Design versus code

| Point | Design says | Code does | Match? |
| --- | --- | --- | --- |
| Login screen | S-01 (Confluence 65835): a select "Choose dev user" and a "Sign in" button | A list of buttons, one per person, showing name and roles. Repo `ai/design/wireframes/login.md` also draws a list and says it is a sketch, not the Phase 3a wireframe | Differs |
| Login title and a11y | S-01: title "Leave Management", visible labels, errors announced | Heading "Sign in"; errors use `role="alert"`; buttons have `aria-label` | Partly |
| 403 screen | S-12 (65835): safe message with "Back to dashboard" | Screens never show 403: `roleGuard` goes to the not-found view. The 403 is seen only at the API | Differs (screen side) |
| Dev-only guard | ADR 5 (1277954): dev mode blocked outside Development by a start-up check; `security.md` rule 6 adds Demo | `AuthSetup` throws unless Development or Demo; EntraId throws "arrives in 4g". Caller reported Production, Demo without key and EntraId refuse to start | Yes (Demo is the repo rule) |
| Same user in both modes | 1277954 learning point 5: same claims in both modes | Claims `sub`, `name`, `email`, `role`; business code does not read `Auth:Mode`. `Program.cs` reads `authMode` to map dev endpoints | Yes, one small read in Program.cs |
| Token | Rule 7: 60 min, memory only, no manager in token | `LifetimeMinutes` setting (60 value not read in the json), signal storage, no manager claim | Yes |
| Deny by default | Rule 2 | Fallback policy; dev endpoints and `/health` are `AllowAnonymous` | Yes |
| 401 versus 403 | Jira: 403 and audited; research D2: 401 not audited | 401 writes nothing; 403 for a signed-in person writes a row | Yes |
| EntityId | Rule 12: ids and values only | Route pattern, for example `/api/admin/ping` (not the raw URL) | Yes |
| Audit row contents | Rule 12 and BR-01 concern "no personal data" | Action, EntityType, EntityId, actor id, `{"policy":...}`, trace id. No name, email or token | Yes |
| Append-only audit | Rule 14; insert-only permission and trigger in 4e | `AuditWriter` only inserts. No DB permission or trigger | Not built (4e) |
| Audit in same transaction | Rule 14 | Own `SaveChanges` per refusal; there is no business transaction for a refusal (research D4) | Differs by design |
| Errors | Rule 10: ProblemDetails | 403 and 404 from the framework and `Results.Problem`. Body of 403 was reported as ProblemDetails by the caller | Yes |
| `/api/me` | Dev-auth flow: after sign-in the app calls `/api/me` | The web app never calls `/api/me`; it uses the `user` in the token response | Differs |
| Definition of Done (491521) | Each criterion has a test; audit for state change | Web unit tests only; no back-end test | Partly |

**Design documents used:**
- Jira SCRUM-8 (facts given in the prompt)
- Confluence LMA 98443 Phase 1: Requirements Overview (excerpt)
- Confluence LMA 491521 Phase 2: Planning (excerpt)
- Confluence LMA 65835 Phase 3a: UI/UX Design (excerpt)
- Confluence LMA 1277954 Phase 3b: High-Level Design (excerpt)
- `ai/steering/*`, `ai/design/flows/dev-auth.md`, `ai/design/wireframes/login.md`
- `ai/specs/us01-sign-in-by-role/` (`tasks.md`, `research.md` read; the other files not read in detail) and `ai/specs/4c-dev-authentication/tasks.md`

**Looked for and not found:** Confluence 3g security design page (not in the space page list). Phase 3d API design: only its outline was read, so `/api/me` and `/api/dev/*` were not compared.

## State of the tasks

`us01-sign-in-by-role/tasks.md` (14 tasks): 11 ticked, 3 open.
- Done: T001 to T005, T007 to T012
- Open: T006 (owner browser click-through), T013 (`/review`), T014 (`/learn`, `/status`)

`4c-dev-authentication/tasks.md` (10 tasks): 8 ticked, 2 open.
- Done: T1 to T8
- Open: T9 (owner end-to-end check), T10 (README, review, learn, status)

The tasks note says 73 web tests; the caller reported 74 after the interceptor fix.

## Findings and gaps

- **Interceptor bug, fixed in this work:** the interceptor read the API URL before `config.json` was loaded, which showed the "configuration could not be loaded" page. It now skips requests until config is ready. Regression test in `auth.interceptor.spec.ts`.
- **Jira wording:** "/admin" does not exist as a screen path. The story is proved with `GET /api/admin/ping`, a probe meant to be removed later.
- **Browser screens:** a forbidden route gives the not-found screen with no audit row. Only API refusals are audited. Whether the story means the screen path too is for the owner to decide.
- **Login screen** is a button list, not the S-01 select box (Confluence). Repo wireframe already follows the list. Confluence may be the older source; the evidence does not settle it.
- **Audit is append-only by convention only.** No insert-only permission or trigger until 4e.
- **PR scope:** one PR holds the story, the whole 4c implementation, the `ci-check` skill, CI workflow edits and web config changes. Harder to review and to revert.
- **Not verified:** no browser run (T006); no back-end automated tests; `/review` (security rules 1, 2, 3, 7, 10, 12, 14) not done; `/api/me` is not used by the web app.
- **Audit row for tampered tokens:** none, by design (401).
- I did not build or run anything. The run results above come from the caller.

## Why it is built this way

| Decision | Chosen | Rejected, and why |
| --- | --- | --- |
| Where to audit 403 | One `IAuthorizationMiddlewareResultHandler` (research D1) | Audit in each endpoint (easy to forget); status-code middleware (cannot see policy name); MVC filter (not used in minimal APIs) |
| Audit 401? | No (D2) | Anonymous noise, a way to fill the table |
| Audit row content | Route pattern, policy name, ids (D3) | Full URL or role list: may carry client text |
| Audit failure | Log and still return 403 (D4) | Fail the request: an audit outage would lock people out |
| Proof endpoint | `GET /api/admin/ping` (D5) | Test-only endpoint (no test project yet); wait for 4f |
| Navigation | One `NAV_ITEMS` table for links and routes (D6) | Separate lists that can drift |
| Token storage | Memory signal | localStorage: readable by script, survives reload |
| Sign-in modes | Dev now, Entra ID later, one claims shape (ADR 5) | Dev-only code paths in business logic |

## Interview practice

1. **Q:** What is the difference between 401 and 403, and which do you audit? **A:** 401 means "I do not know who you are". 403 means "I know you, but you may not". We audit only 403 because it has a known actor and signals a real attempt.
2. **Q:** Why a custom `IAuthorizationMiddlewareResultHandler`? **A:** It runs right after the policy decision, so every protected endpoint is covered, now and later. It also knows the policy name.
3. **Q:** Why is the token kept in memory and not localStorage? **A:** A script injected into the page can read localStorage. Memory is lost on reload, which costs convenience but is safer for a dev-mode app.
4. **Q:** Hiding the Admin link in Angular, is that security? **A:** No. It is convenience. The server checks the policy on every call (rule 1).
5. **Q:** How do you stop dev sign-in reaching production? **A:** `AddLeaveAuthentication` checks mode and environment at start-up and refuses to start. The dev endpoints are mapped only in Dev mode.
6. **Q:** What does the audit row hold and why not the URL? **A:** Action, route pattern, actor id, policy name, trace id. Never names, emails or tokens. The raw URL could hold client-typed text.

**Teach-back (answer in your own words):**
1. Trace the request from clicking a person on `/login` to seeing `/dashboard`. Name each file and endpoint.
2. Why does the 403 handler catch exceptions from the audit write, and what would go wrong if it did not?
3. The interceptor bug showed the "configuration could not be loaded" page. Explain the cause and why the fix is safe.
