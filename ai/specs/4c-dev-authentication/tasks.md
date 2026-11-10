# 4c: Dev authentication: Tasks

Status: Draft (needs approval after the design)
Design: not yet approved

Progress: 8 of 10 done (T1 to T8). T9 is the owner's end-to-end check; T10 is wrap-up. Notes below.

- [x] **T1: Package, options, settings** (R2, R6)
  - Files: `src/LeaveManagement.Api/LeaveManagement.Api.csproj`, `Auth/DevTokenOptions.cs`, `appsettings.Development.json`
  - Do: add `Microsoft.AspNetCore.Authentication.JwtBearer`; add `Auth:Dev:SigningKey` (32+ characters, throwaway) and `LifetimeMinutes`
  - Verify: `dotnet build LeaveManagement.slnx`

- [x] **T2: Authentication and authorization setup** (R3, R4, R5, R6, R7)
  - Files: `Auth/AuthSetup.cs`
  - Do: fail-fast checks; JWT validation; fallback policy; three role policies; rate limiter
  - Verify: build passes

- [x] **T3: Token service and endpoints** (R2, R5, R9)
  - Files: `Auth/DevTokenService.cs`, `Auth/AuthEndpoints.cs`
  - Do: sign tokens; `/api/me`, `/api/dev/users`, `/api/dev/token`
  - Verify: build passes

- [x] **T4: Wire up Program.cs** (R3, R7, R10)
  - Files: `Program.cs`
  - Do: call `AddLeaveAuthentication`; add middleware in the right order (CORS, rate limiter, authentication, authorization); map endpoints; keep `/health` anonymous
  - Verify: build passes

- [x] **T5: Back-end manual verification** (R3, R4, R5, R6, R9, R10)
  - Do: run the API; check `/health` (200), `/api/me` without token (401), `/api/dev/users` (list), sign in through `/api/dev/token` and call `/api/me` with the token (roles correct for each person); try an unknown id (404)
  - Verify: results written in the task notes

- [x] **T6: Front-end auth building blocks** (R1, R2, R4, R8)
  - Files: `web/src/app/auth/*`
  - Do: models, `AuthService` (token in a signal), interceptor, guard
  - Verify: `npm test -- --watch=false`, `npm run build` (in `web`)

- [x] **T7: Screens and routing** (R1, R2, R4)
  - Files: `features/login/*`, `features/home/*`, `app.routes.ts`, `app.config.ts`, `app.ts`
  - Do: login picker, home screen (identity, `/api/me`, health, sign out), router with guard, shell
  - Verify: build passes; click through login, home, sign out

- [x] **T8: Front-end tests** (R1, R4, R8)
  - Files: `*.spec.ts` for service, interceptor, guard, login, home
  - Verify: all tests pass

- [ ] **T9: End-to-end check by the owner** (R1 to R10)
  - Do: Visual Studio F5 plus `npm start`; sign in as each person; check roles on the home screen; reload and confirm sign-out
  - Verify: the owner confirms

- [ ] **T10: Wrap up** (all)
  - Do: README update; `/review`; `/learn 4c-dev-authentication`; `/status`
  - Verify: no open high or medium review findings

## Done when

- All boxes ticked
- Build and tests pass
- `/review` has no open high or medium findings
- `ai/learning/4c-dev-authentication.md` written
- `ai/specs/INDEX.md` updated

## Implementation notes (2026-11-10)

- T1: package `Microsoft.AspNetCore.Authentication.JwtBearer` 10.0.* added with the owner's yes. Signing key is a throwaway value in `appsettings.Development.json`; other environments must supply `Auth__Dev__SigningKey` (32+ characters) themselves.
- T2/T4: Dev mode is allowed in `Development` or `Demo` (security rule 6, merged with T-02). `EntraId` fails at start-up with a message that it arrives in 4g.
- T5 (run on the owner's PC with LocalDB): `/health` 200; `/api/me` without token 401; `/api/dev/users` 200 with six people; token for each of the six people gives the right roles on `/api/me`; unknown id 404; garbage and tampered tokens 401; a burst of 33 calls got 429 after the limit; Production and Demo without a key refuse to start; `EntraId` refuses to start.
- T6/T7 deviations: the T-02 stub people and `useStubPerson` were removed from `AuthService` (4c replaces them). There is no separate `features/home`: the T-02 dashboard placeholder with the top bar (name, links, sign out) is the home screen. `devStub` in `config.json` is now unused and can be removed in a later clean-up.
- T8: new and updated specs for service, interceptor, guards, login and top bar. `npm run test:ci`: 13 files, 73 tests pass. `npm run build` and `dotnet build LeaveManagement.slnx` pass with 0 warnings.
- Not done: T9 (owner click-through in the browser) and T10 (README, `/review`, `/learn`, `/status`). I did not open the app in a browser.
