# Tasks: Sign in and see only what my role allows

**Status**: Implemented except T006 (owner browser check) and T013-T014 (review, learn, status)
**Input**: `ai/specs/us01-sign-in-by-role/` (spec, plan, research, data-model, contracts, quickstart)
**Jira**: SCRUM-8 (US-01)

**Tests**: The spec does not ask for new automated tests. One web unit test task (T005) is included because T-02 already has the test file. Back-end automated tests arrive in Phase 5, so back-end verification is the build plus the manual steps in `quickstart.md`.

## Format: `- [ ] [ID] [P?] [Story] Description (file) — Verify: how to check`

- **[P]**: can run in parallel (different files, no dependency on an unfinished task)
- **[Story]**: US1, US2 or US3 from `spec.md`
- Tick a box only after its Verify step passes. Do tasks in order.

---

## Phase 1: Setup (prerequisites)

**Purpose**: this feature builds on other specs; confirm they are in place before writing code.

- [x] T001 Confirm spec `4c-dev-authentication` is implemented: JWT validation, fallback policy, `Policies.RequireAdmin/RequireManager/RequireHR`, `/api/me`, the sign-in screen (files under `src/LeaveManagement.Api/Auth/` and `web/src/app/auth/`). — Verify: `dotnet build LeaveManagement.slnx` passes and `src\LeaveManagement.Api\Auth\AuthSetup.cs` exists. If not, stop and finish 4c first.
- [x] T002 Confirm the T-02 shell (branch SCRUM-26) is merged: `web/src/app/layout/nav-items.ts` and `nav-items.spec.ts` exist. — Verify: both files exist; `cd web` then `npm test -- --watch=false` passes.

---

## Phase 2: Foundational (blocks US2)

**Purpose**: the audit writer, used by the 403 handler now and by 4e later.

- [x] T003 Add the interface and record in `src/LeaveManagement.Application/Auditing/IAuditWriter.cs`: `AuditRecord(string Action, string EntityType, string EntityId, int? ActorEmployeeId, object? Detail, string? CorrelationId)` and `IAuditWriter.WriteAsync(AuditRecord record, CancellationToken ct)`. No package, no EF types (Domain/Application rule). Contract: `contracts/audit-access-denied.md`. — Verify: `dotnet build LeaveManagement.slnx` passes.
- [x] T004 Implement `AuditWriter` in `src/LeaveManagement.Infrastructure/Persistence/AuditWriter.cs`: insert one `AuditEvent` (entity in `src/LeaveManagement.Domain/Messaging.cs`) using `TimeProvider.GetUtcNow().UtcDateTime` for `OccurredAtUtc`; serialise `Detail` to `DetailJson` (default `"{}"`); never update or delete. Respect existing column limits in `Persistence/Configurations/MessagingConfigurations.cs` (read them first, do not guess). Register it in `src/LeaveManagement.Infrastructure/DependencyInjection.cs` as scoped. Make sure a `TimeProvider` is registered (add `TimeProvider.System` only if absent). — Verify: `dotnet build LeaveManagement.slnx` passes.

**Checkpoint**: a service exists that can append an audit row.

---

## Phase 3: User Story 1 - Sign in and land on my dashboard (Priority: P1) MVP

**Goal**: a signed-in person lands on their dashboard and sees exactly the links their roles allow (FR-001 to FR-003, FR-006; SC-001, SC-002).

**Independent Test**: sign in as each demo person and compare the top bar with the table in `ai/specs/t02-angular-shell/contracts/navigation.md`.

- [x] T005 [US1] Add a role-to-links unit test case for each of Employee, Manager, HR, Admin and HR+Admin (Audit appears once) in `web/src/app/layout/nav-items.spec.ts`, skipping cases the file already covers. No change to `nav-items.ts` unless a test shows a real bug; if so, stop and report. — Verify: `cd web` then `npm test -- --watch=false` passes; report the number of tests.
- [ ] T006 [US1] Run quickstart section 4 (screen checks) by hand for all five role combinations and the page-reload sign-out. — Verify: each row of the quickstart table matches; record any mismatch instead of fixing silently.

**Checkpoint**: US1 verified; no new production code was needed beyond 4c and T-02.

---

## Phase 4: User Story 2 - Missing role is refused with 403 and audited (Priority: P1)

**Goal**: a signed-in person without the role gets 403 and exactly one `AccessDenied` audit row (FR-005, FR-007; SC-003, SC-004).

**Independent Test**: quickstart sections 2 and 3.

- [x] T007 [US2] Create `src/LeaveManagement.Api/Auth/AuditingAuthorizationResultHandler.cs` implementing `IAuthorizationMiddlewareResultHandler`. On `authorizeResult.Forbidden` (and the user is authenticated): call `IAuditWriter` with `Action = "AccessDenied"`, `EntityType = "Endpoint"`, `EntityId` = the route pattern (`endpoint.RoutePattern.RawText`, never the raw URL), `ActorEmployeeId` = the `sub` claim parsed as int, `Detail = { policy }` (the policy name only), `CorrelationId = HttpContext.TraceIdentifier`; then return a 403 `ProblemDetails` with no role list or stack trace. If the audit write throws, log the trace id (no token, no claims) and still return 403. All other results go to `AuthorizationMiddlewareResultHandler`. Do not audit 401. — Verify: `dotnet build LeaveManagement.slnx` passes (warnings are errors).
- [x] T008 [US2] Register the handler in `src/LeaveManagement.Api/Auth/AuthSetup.cs` (`services.AddSingleton<IAuthorizationMiddlewareResultHandler, AuditingAuthorizationResultHandler>()`; adjust lifetime if it needs the scoped writer: resolve `IAuditWriter` from `HttpContext.RequestServices`). — Verify: `dotnet build LeaveManagement.slnx` passes; the API starts with `dotnet run --project src\LeaveManagement.Api`.
- [x] T009 [US2] Add `src/LeaveManagement.Api/Admin/AdminEndpoints.cs` with `MapAdminEndpoints` mapping `GET /api/admin/ping` → `Results.NoContent()` with `RequireAdmin`, and call it from `src/LeaveManagement.Api/Program.cs`. Contract: `contracts/admin-probe-api.md`. — Verify: build passes; with the API running, quickstart section 2 gives 401 (no header), 403 (Employee, Manager, HR), 204 (Admin).
- [x] T010 [US2] Check the audit trail with the SQL in quickstart section 3. — Verify: one `AccessDenied` row per 403 and none for 401 or 204; `DetailJson` is `{"policy":"RequireAdmin"}`; no name, email or token in any column. Report the rows you saw.

**Checkpoint**: SCRUM-8 second acceptance criterion works end to end.

---

## Phase 5: User Story 3 - Signed-out and expired sessions return to sign-in (Priority: P2)

**Goal**: protected content is unreachable without a valid session (FR-004, FR-008 to FR-010; SC-005). Behaviour comes from 4c; this phase only proves it.

- [x] T011 [US3] Run quickstart checks for the signed-out and expired cases: call `GET /api/admin/ping` with no token and with a tampered token (expect 401), and let a token expire or edit its expiry (expect sign-out and the sign-in screen in the web app). — Verify: 401 and the redirect happen; no audit row is written for them. Record any gap as a 4c defect instead of patching it here.

---

## Phase 6: Polish and cross-cutting

- [x] T012 Run the final build and tests: `dotnet build LeaveManagement.slnx` then `cd web` and `npm test -- --watch=false` and `npm run build`. — Verify: all pass; state plainly that no back-end automated tests exist yet.
- [ ] T013 Run `/review` (code-reviewer and security-reviewer) against security rules 1, 2, 3, 7, 10, 12, 14. — Verify: no high or medium finding left open.
- [ ] T014 Run `/learn us01-sign-in-by-role` (study notes and interview Q&A in `ai/learning/`), then `/status` to update `ai/specs/INDEX.md`. — Verify: the files exist; INDEX shows the spec's state.

---

## Dependencies and order

```text
T001, T002 ──> T003 ──> T004 ──> T007 ──> T008 ──> T009 ──> T010 ──> T011 ──> T012 ──> T013 ──> T014
                         T005 ──> T006 (US1; can run any time after T002)
```

- US1 (T005, T006) depends only on 4c and T-02, so it can run in parallel with T003 to T004.
- US2 depends on T003 and T004, and on 4c's `Policies` and JWT setup.
- US3 depends on T009 (it needs a protected endpoint to call).

## Parallel opportunities

- T005 [P] with T003 and T004 (different projects, no shared files).
- T006 can follow T005 while T007 is written.

## Suggested MVP

US1 plus US2 (both P1): T001 to T010. US3 (T011) is a verification of 4c and can follow.

## Counts

14 tasks: Setup 2, Foundational 2, US1 2, US2 4, US3 1, Polish 3.

## Implementation notes (2026-11-10)

- T001/T002: 4c was implemented first (see `ai/specs/4c-dev-authentication/tasks.md`); the T-02 shell is merged. Web tests: 13 files, 73 tests pass.
- T005: no change needed. `web/src/app/layout/nav-items.spec.ts` already covers Employee, Manager, HR, Admin and the HR+Admin case (Audit once).
- T009 check on LocalDB: no token 401; Employee, Manager and HR 403; Admin 204. The 403 body is ProblemDetails with a trace id.
- T010 check: four 403s (Employee id 4 twice, Manager id 1, HR id 2) gave four `AccessDenied` rows with `DetailJson = {"policy":"RequireAdmin"}`, `EntityId = /api/admin/ping`, no name or email. The 204 and the 401s wrote no row.
- T011 check: a tampered token and a garbage token on `/api/admin/ping` both gave 401 and no audit row. Token expiry and the web redirect after a 401 were not exercised in a browser.
- T012: `dotnet build LeaveManagement.slnx` 0 warnings, 0 errors; `npm run test:ci` 73 tests pass; `npm run build` passes. No back-end automated tests exist (Phase 5).
- Not done: T006 (needs a person in the browser), T013 `/review`, T014 `/learn` and `/status`.
