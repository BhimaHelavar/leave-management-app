# Tasks: Application Shell, Health Check and First Deployment

**Input**: `ai/specs/t02-angular-shell/` (spec.md, plan.md, research.md, data-model.md, contracts/, quickstart.md)

**Prerequisites**: spec and plan approved. Spec 4c is a dependency for real sign-in only; this feature uses the stub person (research R10).

**Tests**: INCLUDED. `.claude/rules/frontend-angular.md` requires a Vitest spec (success path plus one failure path) for every new component and service. Back-end checks are manual until Phase 5 (no test project yet).

**Rules for every task**: one task at a time, in order; run the stated verify step; tick the box only after it passes (CLAUDE.md). Commands work in PowerShell or Command Prompt. "Owner runs" means Claude has no Azure session; Claude writes the files and the commands, the owner runs them, and Claude says it did not verify them.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: can run in parallel (different files, no dependency on an unfinished task)
- **[Story]**: US1 shell, US2 health, US3 sign-in mode by configuration, US4 deployed environment

## Phase 1: Setup (approvals and facts)

- [x] T001 Owner approves the exact wording, then edit `ai/steering/security.md` rule 6 to: "`Auth:Mode=Dev` is allowed only in the `Development` or `Demo` environment; the API refuses to start otherwise; Demo never migrates or seeds at startup." Mirror it in `.specify/memory/constitution.md` Principle II and Technology section, bump to version 1.1.0, set Last Amended. Verify: grep both files for "Demo"; the version line reads 1.1.0.
- [x] T002 Ask the owner, then install `@angular/material@22.2.5` and `@angular/cdk@22.2.5` (exact versions, same major as Angular core 22.2.x) in `web/package.json` by running `npm install @angular/material@22.2.5 @angular/cdk@22.2.5` in `web`. Verify: `npm ls @angular/material` shows 22.2.5 with no peer warning; `npm run build` passes.
- [x] T003 Free-tier facts checked against Microsoft pages on 2026-11-02 and recorded in `ai/specs/t02-angular-shell/research.md` (Verified / Not confirmed / Test at creation). Owner chose South India (fallback Central India, then Southeast Asia) and asked for a free spending limit: none exists on pay-as-you-go, so the plan uses a 1 USD monthly budget alert plus the free tiers' own caps. Items still marked "Test at creation" (F1 and free SQL in the region, .NET 10 on Linux, App Service access restriction on F1, action-group test email) are checked in T021, T024 and T027.

## Phase 2: Foundational (blocks every story)

- [x] T004 Runtime configuration. Create `web/public/config.json` as `{"apiUrl":"http://localhost:5000","devStub":true}`; add `web/src/app/config/runtime-config.ts` (typed `RuntimeConfig`, loader that fetches `/config.json` before first render, state `loading | ready | failed`; `apiUrl` required, absolute http(s), no trailing slash); change `web/src/app/api-config.ts` to read the loaded value instead of the constant `API_URL`; register the loader in `web/src/app/app.config.ts`; add `web/src/app/config/config-error.ts/.html` ("configuration could not be loaded", no blank page); update `health.service.ts` if it imports `API_URL`. Add `runtime-config.spec.ts`: valid file loads; missing file and invalid `apiUrl` give `failed`. Verify: `npm test -- --watch=false` and `npm run build` in `web`.
- [x] T005 [P] Theme. In `web/src/styles.scss` set up the Angular Material v22 theme using the Phase 3a tokens: primary `#1F4E79`, success `#1B6E3C`, warning text `#7A4B00` on `#FFF3D6`, error `#B3261E`, neutral `#5F6368`, Roboto 16px / line height 1.5, 8px spacing, corners 4px and 8px. Read the v22 theming docs first (the API changed between versions). Record the contrast ratios of each text/background pair in `ai/specs/t02-angular-shell/research.md` R11 (4.5:1 text, 3:1 large text and borders); fix any pair that fails. Verify: `npm run build` passes; the ratios are written down.
- [x] T006 [P] Identity seam. Create `web/src/app/auth/auth.service.ts`: `person` signal with `{ id, name, roles }` per data-model.md (roles `Employee | Manager | HR | Admin`; an empty or unknown list becomes `['Employee']`), a `roles` computed, and a stub person chosen by `config.devStub` (a local-only list of four demo people). Never stores a token (rule 7). Add `auth.service.spec.ts`: stub gives the right roles for each of the four people; empty roles fall back to Employee; with `devStub` false and no real sign-in there is no person. Verify: `npm test -- --watch=false`.
- [x] T007 [P] API health time. In `src/LeaveManagement.Api/Program.cs` register `TimeProvider.System` and make `/health` use `TimeProvider.GetUtcNow()` instead of `DateTimeOffset.UtcNow` (response shape unchanged, see `contracts/health-api.md`). Verify: `dotnet build LeaveManagement.slnx` passes with no warnings; call `GET http://localhost:5000/health` and confirm `timeUtc` is UTC and all five fields are present.
- [x] T008 Navigation table and guard. Create `web/src/app/layout/nav-items.ts` with the single table from `contracts/navigation.md` (Dashboard, My requests: all roles; Approvals: Manager; Reports, Exceptions, Audit: HR; Leave types, Holidays, Audit: Admin; Audit appears once for HR+Admin) and a function `visibleItems(roles)` that de-duplicates by path; create `web/src/app/auth/role-guard.ts` as a `CanActivateFn` that sends a person without a required role to the not-found route (UX only, rule 1). Add `nav-items.spec.ts` and `role-guard.spec.ts`: each of the four roles gets the exact list from the contract; Manager+HR+Admin sees Audit once; guard allows and blocks. Verify: `npm test -- --watch=false`.
- [x] T009 Routing. Create `web/src/app/app.routes.ts` per `contracts/navigation.md` (`/` redirects to `/dashboard`; the eight paths use the shared placeholder with the role guard; `**` goes to not-found) and add `provideRouter(routes)` to `web/src/app/app.config.ts`. The not-found and placeholder components are created in US1, so use lazy `loadComponent` imports that point at their final paths. Verify: `npm run build` fails only on the not-yet-created components until T012 and T013 exist; after those, build passes (run the build again at the end of US1).

## Phase 3: User Story 1 - See a consistent app shell (Priority: P1) MVP

**Goal**: signed-in person sees the top bar and the links their roles allow, with placeholder pages.

**Independent test**: switch the stub person between Employee, Manager, HR, Admin; the links match `contracts/navigation.md`; an unallowed or unknown path shows the not-found view.

- [x] T010 [P] [US1] Create the shared placeholder page `web/src/app/features/placeholder/placeholder.ts/.html/.scss/.spec.ts`: reads the route title and shows "Coming soon" plus the section name; spec covers title shown and a missing title falls back to a generic heading. Verify: `npm test -- --watch=false`.
- [x] T011 [P] [US1] Create the not-found view `web/src/app/features/not-found/not-found.ts/.html/.scss/.spec.ts` (screen S-12: safe message that does not reveal whether a route exists for other roles, and a "Back to dashboard" link). Spec: renders the message and the link target. Verify: `npm test -- --watch=false`.
- [x] T012 [US1] Create the top bar `web/src/app/layout/top-bar.ts/.html/.scss/.spec.ts`: app name link to `/dashboard`; links from `visibleItems(roles)` with `aria-current="page"` on the active one; user menu (Material menu) showing the person's name; below 768 px the links collapse into a labelled menu button; a "skip to main content" link as the first focusable element; no `innerHTML` (rule 13). Spec: Employee sees 2 links, Admin sees the Admin set, collapsed menu button exists at narrow width, user name shown. Verify: `npm test -- --watch=false`.
- [x] T013 [US1] Change `web/src/app/app.ts` and `app.html` to `<app-top-bar>` + `<main id="main"><router-outlet/></main>` (the health card leaves `app.html` in US2) and update `app.scss` for layout (max content width 1120 px). Update `app.spec.ts` so it asserts the shell renders and no longer asserts the old health text. Verify: `npm test -- --watch=false` and `npm run build`.
- [ ] T014 [US1] Manual checks from `quickstart.md` scenarios 1, 2, 3, 8, 9, 10 on the local app (Claude says plainly which it could run; scenarios needing a browser are marked "not verified by Claude" if no browser session is available). Verify: owner confirms or the gaps are listed.

**Checkpoint**: US1 works alone, without the health card.

## Phase 4: User Story 2 - See that the API is reachable (Priority: P1)

**Goal**: the health status and its error state live inside the shell.

**Independent test**: with the API running the card shows Healthy, `Dev`, `Connected`, environment; with it stopped, an error and "Check again"; restarting and pressing it recovers without a reload.

- [ ] T015 [US2] Move the health card into `web/src/app/features/health/health.ts/.html/.scss` (state `loading | ok | error` using signals, "Check again" button, `aria-live="polite"`, shows status, sign-in mode, database, environment, server time). Keep `web/src/app/health.service.ts` and its `Health` interface unchanged (contract). Create `health.spec.ts` with `HttpTestingController`: success shows the five values; HTTP error shows the error text and the button; pressing the button after an error returns to loading and then ok. Verify: `npm test -- --watch=false`.
- [ ] T016 [US2] Host the health card on the dashboard route: add `web/src/app/features/dashboard/dashboard.ts/.html/.spec.ts` (title "Dashboard", placeholder text, `<app-health>`); point the `/dashboard` route in `app.routes.ts` at it instead of the shared placeholder. Spec: renders the health card. Verify: `npm test -- --watch=false` and `npm run build`.
- [ ] T017 [US2] Manual checks from `quickstart.md` scenarios 4 and 5 (stop the API, reload, restart, press "Check again"). Verify: owner confirms or the gap is listed as not verified by Claude.

**Checkpoint**: MVP (Phases 1 to 4) is complete locally.

## Phase 5: User Story 3 - Choose the sign-in mode by configuration (Priority: P2)

**Goal**: the API's sign-in mode is configuration only and Dev is refused outside Development or Demo.

**Independent test**: quickstart scenarios 6 and 7.

- [ ] T018 [US3] In `src/LeaveManagement.Api/Program.cs` add the startup guard (after the existing `Auth:Mode` validation): if `authMode == "Dev"` and the environment is neither `Development` nor `Demo`, throw `InvalidOperationException` with a clear message that does not include any secret. Keep migrations and seeding only under `IsDevelopment()` (Demo does not migrate or seed, plan R7). Only the startup code reads `Auth:Mode` (rule 5). Depends on T001. Verify: `dotnet build LeaveManagement.slnx` passes; with `ASPNETCORE_ENVIRONMENT=Production` and `Auth__Mode=Dev` the API exits with the message; with `Demo` it starts (set variables in PowerShell with `$env:ASPNETCORE_ENVIRONMENT="Demo"`, in Command Prompt with `set`).
- [ ] T019 [US3] Add the `Demo` launch profile or note to `src/LeaveManagement.Api/Properties/launchSettings.json` only if the owner wants it; otherwise document the two variables in `ai/specs/t02-angular-shell/quickstart.md`. Verify: quickstart scenarios 6 and 7 pass.

## Phase 6: User Story 4 - Reach one deployed environment (Priority: P2)

**Goal**: the shell and a healthy API reachable on Azure, restricted to the owner, with a budget email alert. Depends on T003 (verified facts), T018 (Demo guard) and US1/US2.

**Independent test**: quickstart A1 to A4.

- [ ] T020 [P] [US4] Create `web/staticwebapp.config.json`: SPA fallback to `/index.html` for unknown paths, and response headers equivalent to `web/nginx.conf` (`X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: no-referrer`, Content-Security-Policy with `connect-src 'self'` plus the Azure API origin, filled in at T024). Do not change `nginx.conf` (local container keeps it). Verify: valid JSON; `npm run build` unaffected.
- [ ] T021 [US4] Owner runs: create the Azure resources, region and names from T003. Claude writes the exact PowerShell commands into `ai/specs/t02-angular-shell/quickstart.md` section "Azure setup" (resource group; Static Web App Free; App Service plan F1 Linux and web app on .NET 10; Azure SQL free database and a least-privilege app login; budget). Never print or store passwords in the repo; the owner types them. Claude does not verify these commands. Verify: owner pastes the resource names and the two public addresses (no secrets).
- [ ] T022 [US4] Owner runs: set App Service settings: `ASPNETCORE_ENVIRONMENT=Demo`, `Auth__Mode=Dev`, `Cors__AllowedOrigin=<the Static Web Apps origin, exactly one>`, and the connection string `ConnectionStrings__Default` as an App Service connection string (never in the repo or workflow). Verify: owner confirms the names set (not the values).
- [ ] T023 [US4] Database: generate the idempotent script with `dotnet ef migrations script --idempotent --project src\LeaveManagement.Infrastructure --startup-project src\LeaveManagement.Api --output <scratch path outside the repo>`, read it (golden rule), and give it to the owner to run against the Azure SQL database with SSMS. Demo data load: the owner chooses (run the seed from a local run pointed at Azure, or a reviewed seed script) and Claude documents the chosen way in quickstart.md. Verify: owner confirms tables exist and the demo people are present.
- [ ] T024 [US4] Owner runs: restrict the API to the owner (App Service access restriction to the owner's IP range; research R7). Update the CSP `connect-src` in `web/staticwebapp.config.json` with the real API origin. Verify: quickstart A2 (a request from a non-allowed network gets 403).
- [ ] T025 [US4] Create `.github/workflows/deploy.yml`: manual trigger only (`workflow_dispatch`), builds the API and the web app, writes the deployed `web/dist/web/browser/config.json` with the Azure API URL and no `devStub`, deploys web to Static Web Apps and API to App Service. Uses only GitHub Actions secrets (names listed in `contracts/runtime-config.md`); no secret values anywhere in the file. Ask the owner before adding any third-party action beyond the official `actions/*` and Azure ones. Verify: YAML parses; a secrets grep of the repo finds nothing; owner adds the secrets in GitHub.
- [ ] T026 [US4] Run the workflow (owner triggers it) and do the smoke checks A1 and A4: shell loads, health card Healthy with environment `Demo`,
lose-out

- [ ] T028 Secrets scan of the repo and `web/dist` (search for connection strings, `Password=`, tokens, publish profiles); fix anything found. Verify: nothing found.
- [ ] T029 Full verification: `dotnet build LeaveManagement.slnx`; in `web` `npm test -- --watch=false` and `npm run build`; report exact results, and list every check that was not run.
- [ ] T030 Update `ai/specs/INDEX.md` (status per the real state) and tick the checklists in `ai/specs/t02-angular-shell/checklists/requirements.md` only for what was verified.
- [ ] T031 Run `/review`, then `/learn` (writes `ai/learning/t02-angular-shell.md`), then `/status`, per CLAUDE.md.

## Dependencies and order

- Phase 1 first. T001 gates T018. T002 gates T005, T010 to T016 (Material components). T003 gates all of Phase 6.
- Phase 2 gates every story. T005, T006, T007 are parallel; T008 needs T006; T009 needs T008 and the components from US1.
- US1 and US2 can run in parallel after Phase 2 (different files), except T016 needs T015 and the route file edited by both, so do route edits one at a time.
- US3 is independent of US1 and US2 but must finish before US4.
- US4 is sequential (T020 can run early in parallel with T021).

## Parallel examples

- After Phase 2: T010 and T011 together; later T015 alongside T012.
- Phase 2: T005, T006 and T007 together.

## Implementation strategy

- MVP = Phases 1 to 4: a themed shell with role-aware links and the health card, running locally. Stop and demo here.
- Then US3 (small API guard) and US4 (deployment), which carry the Azure risk and the owner-run steps.
- One task at a time; if the spec turns out wrong, stop and fix the spec first (CLAUDE.md).

## Counts

31 tasks: Setup 3, Foundational 6, US1 5, US2 3, US3 2, US4 8, Polish 4.
