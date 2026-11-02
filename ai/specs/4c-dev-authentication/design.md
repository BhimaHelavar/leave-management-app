# 4c: Dev authentication: Design

Status: Draft (needs approval after the requirements)
Requirements: not yet approved

## Approach in one paragraph

The API validates a JWT bearer token on every request and reads its claims. In Dev mode a small token service signs the tokens with a throwaway key; the same validation code will later accept Entra ID tokens. A fallback policy closes every endpoint unless it is marked public. The Angular app lists demo people, asks the API for a token, keeps it in memory, attaches it to API calls with an interceptor, and signs out on a 401.

## Diagram

```
 Browser (Angular)                              API (.NET)                          Database
 -----------------                              ----------                          --------
 /login  --GET /api/dev/users---------------->  list active people + roles  ------> Employee, EmployeeRole
   pick a person
         --POST /api/dev/token {employeeId}-->  find person, sign JWT (60 min)
         <-- { accessToken, expiresAtUtc, user }
   keep token in memory (signal)
 /       --GET /api/me + Bearer token-------->  validate: signature, issuer, audience, expiry
                                                fallback policy: signed-in user required
         <-- { id, name, email, roles }
   401 from any call  --> clear session --> /login
```

## Data

No database change. Reads `people.Employee` and `people.EmployeeRole`.

## Token claims (same names in Entra ID mode)

| Claim | Value |
| --- | --- |
| `sub` | employee id |
| `name` | full name |
| `email` | email |
| `role` | one entry per role: `Employee` plus any of `Manager`, `HR`, `Admin` |
| `iss`, `aud` | `leave-management-dev`, `leave-management-api` |
| `iat`, `nbf`, `exp` | now, now, now + 60 minutes |

The manager is not in the token. It is looked up from the database when a rule needs it.

## API

| Method and path | Who may call | Request | Response | Errors |
| --- | --- | --- | --- | --- |
| `GET /health` | anyone | none | status, auth mode, database, time | none |
| `GET /api/dev/users` | anyone (Dev mode only) | none | list of `{ id, fullName, email, roles[] }` | 429 |
| `POST /api/dev/token` | anyone (Dev mode only) | `{ employeeId }` | `{ accessToken, expiresAtUtc, user }` | 404 unknown or inactive, 429 |
| `GET /api/me` | any signed-in person | none | `{ id, name, email, roles[] }` | 401 |

Endpoint paths follow this spec. If the 3d API design uses other paths, change them here and in the code together.

## Back end (Api project, folder `Auth/`)

| File | Purpose |
| --- | --- |
| `DevTokenOptions.cs` | Settings: signing key, lifetime |
| `AuthSetup.cs` | `AddLeaveAuthentication`: fail-fast checks, JWT validation, fallback policy, role policies, rate limiter |
| `DevTokenService.cs` | Signs a token for a person |
| `AuthEndpoints.cs` | `/api/me`, `/api/dev/users`, `/api/dev/token` |
| `Program.cs` | Calls `AddLeaveAuthentication`; adds `UseRateLimiter`, `UseAuthentication`, `UseAuthorization`; maps endpoints; `/health` stays public |
| `appsettings.Development.json` | `Auth:Dev:SigningKey` (throwaway) and `LifetimeMinutes` |

## Front end (`web/src/app`)

| File | Purpose |
| --- | --- |
| `auth/auth.models.ts` | Types for users, token response, `/api/me` |
| `auth/auth.service.ts` | Signals for session; `devUsers()`, `signInAs()`, `signOut()`, `me()` |
| `auth/auth.interceptor.ts` | Adds `Authorization: Bearer ...` to API calls; on 401 signs out and goes to `/login` |
| `auth/auth.guard.ts` | Redirects to `/login` when not signed in |
| `features/login/` | Login screen (people picker) |
| `features/home/` | Home screen: who you are, what the server says, API health, sign out |
| `app.routes.ts`, `app.config.ts`, `app.ts` | Router, interceptor registration, shell with `<router-outlet>` |

## Security checklist

Rules 2, 5, 6, 7, 11 and 13 of `ai/steering/security.md`: deny by default; one switch for auth; Dev only in Development; short-lived token in memory; throwaway key, not a secret; CORS for one origin. Dev endpoints are rate limited.

## Decisions

| Decision | Choice | Why | Trade-off | Revisit when |
| --- | --- | --- | --- | --- |
| Token storage in Dev | Memory (a signal) | A script on the page cannot find it in storage; no CSRF | Reload signs you out | Entra ID mode (sessionStorage) or a BFF |
| Signing | HS256 with a shared key | Simple for local use | Anyone with the key can mint tokens, so Dev mode is locked to Development | 4g (Entra ID uses asymmetric keys) |
| Claims | Same names as Entra ID | Business code does not change in 4g | Must map Entra claims later | 4g |
| Authorization | Policies now, resource rules in 4d and 4e | Roles alone cannot say "your direct reports" | More code later | 4d |

## Test plan

| Requirement | Test | Where |
| --- | --- | --- |
| R1, R2, R8 | Login lists people; picking one signs in and navigates; token in memory only | `login.spec.ts`, `auth.service.spec.ts` |
| R4 | Interceptor adds header, ignores other URLs, signs out on 401 | `auth.interceptor.spec.ts` |
| R1, R4 | Guard redirects when signed out | `auth.guard.spec.ts` |
| R3, R5, R9, R10 | Manual API calls (below) | Browser or SSMS |
| R6, R7 | Manual start-up and burst checks | API |

Manual API checks: open `http://localhost:5000/health` (works), `http://localhost:5000/api/me` (401), `http://localhost:5000/api/dev/users` (JSON list). Back-end automated tests arrive with the test project in Phase 5.

## Risks

- The back-end files were written without compiling them. The first build may show errors; fix them before continuing.
- The `.slnx` build and F5 in Visual Studio may keep an old `LeaveManagement.Api.exe` locked; stop it before rebuilding.
