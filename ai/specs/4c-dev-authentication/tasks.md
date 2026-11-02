# 4c: Dev authentication: Tasks

Status: Draft (needs approval after the design)
Design: not yet approved

Progress: 0 of 10 done. No 4c code has been delivered to the owner's copy yet. Some back-end drafts exist in the working copy; they count as done only after they are built and verified.

- [ ] **T1: Package, options, settings** (R2, R6)
  - Files: `src/LeaveManagement.Api/LeaveManagement.Api.csproj`, `Auth/DevTokenOptions.cs`, `appsettings.Development.json`
  - Do: add `Microsoft.AspNetCore.Authentication.JwtBearer`; add `Auth:Dev:SigningKey` (32+ characters, throwaway) and `LifetimeMinutes`
  - Verify: `dotnet build LeaveManagement.slnx`

- [ ] **T2: Authentication and authorization setup** (R3, R4, R5, R6, R7)
  - Files: `Auth/AuthSetup.cs`
  - Do: fail-fast checks; JWT validation; fallback policy; three role policies; rate limiter
  - Verify: build passes

- [ ] **T3: Token service and endpoints** (R2, R5, R9)
  - Files: `Auth/DevTokenService.cs`, `Auth/AuthEndpoints.cs`
  - Do: sign tokens; `/api/me`, `/api/dev/users`, `/api/dev/token`
  - Verify: build passes

- [ ] **T4: Wire up Program.cs** (R3, R7, R10)
  - Files: `Program.cs`
  - Do: call `AddLeaveAuthentication`; add middleware in the right order (CORS, rate limiter, authentication, authorization); map endpoints; keep `/health` anonymous
  - Verify: build passes

- [ ] **T5: Back-end manual verification** (R3, R4, R5, R6, R9, R10)
  - Do: run the API; check `/health` (200), `/api/me` without token (401), `/api/dev/users` (list), sign in through `/api/dev/token` and call `/api/me` with the token (roles correct for each person); try an unknown id (404)
  - Verify: results written in the task notes

- [ ] **T6: Front-end auth building blocks** (R1, R2, R4, R8)
  - Files: `web/src/app/auth/*`
  - Do: models, `AuthService` (token in a signal), interceptor, guard
  - Verify: `npm test -- --watch=false`, `npm run build` (in `web`)

- [ ] **T7: Screens and routing** (R1, R2, R4)
  - Files: `features/login/*`, `features/home/*`, `app.routes.ts`, `app.config.ts`, `app.ts`
  - Do: login picker, home screen (identity, `/api/me`, health, sign out), router with guard, shell
  - Verify: build passes; click through login, home, sign out

- [ ] **T8: Front-end tests** (R1, R4, R8)
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
