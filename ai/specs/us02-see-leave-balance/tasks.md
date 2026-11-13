# Tasks: See my leave balance per leave type

**Input**: Design documents from `ai/specs/us02-see-leave-balance/` (Jira SCRUM-9)

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/balances-api.md, quickstart.md. The owner has approved spec, plan and these tasks, and has answered the three open decisions in the Notes at the end (R5 carry-forward, R4 Unpaid, R6 demo data).

**Tests**: There is no .NET test project (Phase 5), so back-end checks are the manual steps in `quickstart.md`, and the final report must say so. Front-end tasks include Vitest specs because `.claude/rules/frontend-angular.md` requires a spec per component and service.

**Organization**: grouped by user story. Do one task at a time, in order. Tick a box only after its verify step passes.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: can run in parallel (different files, no dependency on an unfinished task)
- **[Story]**: US1 or US2 (from spec.md)
- Commands work in Command Prompt or PowerShell, from the repo root.

## Phase 1: Setup

- [ ] T001 Confirm the baseline is green before any change. Verify: `dotnet build LeaveManagement.slnx` succeeds; in `web`, `npm test -- --watch=false` and `npm run build` succeed. Record any failure that already exists, so it is not blamed on this feature.

---

## Phase 2: Foundational (blocks both stories)

**Purpose**: the shapes and ports both stories use. No behaviour yet.

**CRITICAL**: complete before Phase 3.

- [X] T002 [P] Create the response records in `src/LeaveManagement.Application/Balances/BalanceDtos.cs`: `MyBalancesResponse(short LeaveYear, IReadOnlyList<LeaveTypeBalance> Balances)` and `LeaveTypeBalance(int LeaveTypeId, string Code, string Name, bool HasBalance, decimal? Accrued, decimal? Used, decimal? Pending, decimal? Available, decimal? CarryForwardDays, DateOnly? CarryForwardExpiresOn)`. Field names and nullability must match `contracts/balances-api.md`. Verify: `dotnet build LeaveManagement.slnx`.
- [X] T003 [P] Create the two ports in `src/LeaveManagement.Application/Balances/`: `IBalanceReader.cs` (returns, for one employee and leave year, each active leave type with its `Available` (null when no balance row), net sum of Deduction and Restore entries, sum of CarryForward entries with `ExpiresOn` on or after a given date, and the earliest such `ExpiresOn`) and `IPendingLeaveReader.cs` (returns, for one employee and leave year, pending `DaysCount` totals per leave type id). Both take a `CancellationToken`. Verify: `dotnet build LeaveManagement.slnx`.
- [X] T004 Create the pure derivation in `src/LeaveManagement.Domain/BalanceSummary.cs` per `data-model.md` and research R1: given `available`, `held` (= `-(Deduction + Restore)`) and `pending`, return `accrued = available + held`, `used = held - pending`. Domain references nothing; use `decimal`. Verify: `dotnet build LeaveManagement.slnx`; check the worked examples in `data-model.md` (AC1: available 8, held 2, pending 0 gives accrued 10, used 2; AC2: available 5, held 5, pending 3 gives accrued 10, used 2) by reading the code against them, because no test project exists yet (say so in the report).

**Checkpoint**: the solution builds and the shapes are fixed.

---

## Phase 3: User Story 1 - See my balance for each leave type on my dashboard (Priority: P1) MVP

**Goal**: a signed-in person sees accrued, used, pending and available for each leave type.

**Independent Test**: sign in as the demo person with the Jira scenario and check the dashboard against AC1 and AC2 (`quickstart.md` checks 1 to 3, 6 to 9).

### Back end

- [X] T005 [P] [US1] Implement `BalanceReader` in `src/LeaveManagement.Infrastructure/Persistence/BalanceReader.cs` using `LeaveDbContext`, `AsNoTracking()`, EF parameterized LINQ only, reading only `balances.*` tables (`LeaveType`, `LeaveBalance`, `LeaveBalanceEntry`). Group the ledger by leave type; `held` from entry types Deduction and Restore. Verify: `dotnet build LeaveManagement.slnx` with no warnings.
- [X] T006 [P] [US1] Implement `PendingLeaveReader` in `src/LeaveManagement.Infrastructure/Persistence/PendingLeaveReader.cs`: sum `LeaveRequest.DaysCount` where `Status = LeaveStatus.Pending`, for the employee and leave year, grouped by `LeaveTypeId`; `AsNoTracking()`; reads only `leave.LeaveRequest`. Verify: `dotnet build LeaveManagement.slnx`.
- [X] T007 [US1] Implement the use case `GetMyBalances` in `src/LeaveManagement.Application/Balances/GetMyBalances.cs`: take the employee id and `TimeProvider`, compute the leave year (`today.Year`), call both ports, apply `BalanceSummary` (T004), and return `MyBalancesResponse` ordered by leave type id. A type with a balance row but nothing else shows zeros, not an error. For the first version, types with `AccrualMode.None` and no balance row return `HasBalance = false` with null numbers (Story 2 finishes the screen side). Depends on T002 to T006. Verify: `dotnet build LeaveManagement.slnx`.
- [X] T008 [US1] Register the use case and both readers: `src/LeaveManagement.Application/DependencyInjection.cs` (use case) and `src/LeaveManagement.Infrastructure/DependencyInjection.cs` (readers). Verify: `dotnet build LeaveManagement.slnx`.
- [X] T009 [US1] Create `src/LeaveManagement.Api/Balances/BalanceEndpoints.cs` with `MapBalanceEndpoints` and `GET /api/me/balances`: read the employee id from the `sub` claim (as `/api/me` does in `Auth/AuthEndpoints.cs`), no id from the client, return 200 with `MyBalancesResponse`, and `Results.NotFound()` (ProblemDetails) when the `sub` claim is missing or is not an active employee. No role policy; the fallback policy requires sign-in. Log only the employee id. Add `app.MapBalanceEndpoints();` in `src/LeaveManagement.Api/Program.cs` next to the other `Map...` calls. Verify: build, then run the API and call `GET /api/me/balances` with no token (expect 401 ProblemDetails) and with a dev token (expect 200 matching `contracts/balances-api.md`).

### Demo data (needs the owner's answer to R6)

- [X] T010 [US1] Add the idempotent demo top-up in `src/LeaveManagement.Infrastructure/Persistence/DevSeeder.cs` (or a new method in the same folder called from `DatabaseInitializer.cs`, Development only). For one demo person, add the Jira scenario: Annual Grant 10, one Approved request of 2 days and one Pending request of 3 days, matching Deduction ledger rows (negative, signed) and `LeaveBalance.Available = 5`. Add rows only when the scenario is missing, so a second run changes nothing. Use `TimeProvider`-independent fixed dates inside the current year, and `DateOnly` for leave days. Leave days must keep the database rules (one `LeaveRequestDay` per working half day, no overlap). Do not reset or drop the database. Verify: start the API twice; the second start adds no rows; the reconciliation (sum of ledger equals `LeaveBalance.Available`) returns no mismatch for that person.

### Front end

- [X] T011 [P] [US1] Create the typed service `web/src/app/features/dashboard/balance.service.ts` and its spec `balance.service.spec.ts`: `getMyBalances()` calls `GET /api/me/balances` using the base URL from `web/src/app/api-config.ts`, with typed interfaces matching the contract (no `any`). Spec: one success case and one failure case with `HttpTestingController`. Verify: `npm test -- --watch=false` in `web`.
- [X] T012 [US1] Create the dashboard component in `web/src/app/features/dashboard/dashboard.ts`, `.html`, `.scss` and `dashboard.spec.ts` (class `Dashboard`, standalone, signals for `loading`, `error`, `balances`, `@if` and `@for` with `track`, `inject()`). One card per leave type showing accrued, used, pending and available exactly as received (7.5 stays 7.5). A visible loading state, and a visible error message when the load fails, never zeros. Labels and headings for accessibility. Spec: renders the Jira AC2 figures (available 5, pending 3), shows the error state on failure. Depends on T011. Verify: `npm test -- --watch=false`.
- [X] T013 [US1] Route the dashboard path to the new component in `web/src/app/app.routes.ts`: keep the `authGuard` and `roleGuard` and the `NAV_ITEMS` table, and replace only the loader for `dashboard` (other paths stay on the placeholder). Update `web/src/app/app.routes.spec.ts` if it asserts the placeholder for `dashboard`. Verify: `npm test -- --watch=false` and `npm run build` in `web`.

### Verify the story

- [ ] T014 [US1] Run `quickstart.md` checks 1, 2, 3, 6, 7, 8 and 9 in the browser and with the API. Report each result. Anything not run is stated plainly.

**Checkpoint**: User Story 1 works on its own. This is the MVP.

---

## Phase 4: User Story 2 - Understand leave types that behave differently (Priority: P2)

**Goal**: Unpaid, zero-balance types and Annual carry-forward are shown without misleading figures.

**Independent Test**: `quickstart.md` checks 4 and 5.

- [X] T015 [US2] Add the carry-forward rule to `src/LeaveManagement.Domain/BalanceSummary.cs` per research R5 (as confirmed by the owner): `carryForwardDays = max(0, CarryForwardGranted - held)`, capped at `available`, where `CarryForwardGranted` is the sum of `CarryForward` entries whose `ExpiresOn` is on or after today; null when there are none. Verify: `dotnet build LeaveManagement.slnx`; check the carry-forward row in the `data-model.md` worked examples (Grant 12, CarryForward 4, Deduction 1: available 15, carry-forward 3) by reading the code.
- [X] T016 [US2] Fill `CarryForwardDays` and `CarryForwardExpiresOn` in `src/LeaveManagement.Application/Balances/GetMyBalances.cs` from the reader totals and today's date, and confirm the Unpaid and zero-balance behaviour from T007 against FR-006 and the "0 accrued shows 0" scenario. Verify: `dotnet build LeaveManagement.slnx`; call the endpoint for a person with carry-forward and for Unpaid.
- [X] T017 [P] [US2] Add the demo carry-forward example for a second person (Annual Grant 12, CarryForward 4 with `ExpiresOn` 31 March of the next occurrence, matching `LeaveBalance`) to the idempotent top-up from T010 in `src/LeaveManagement.Infrastructure/Persistence/DevSeeder.cs` (or the same method). Idempotent, adds rows only, no reset. Verify: start the API twice with no duplicate rows, and the ledger sum equals `LeaveBalance.Available`.
- [X] T018 [US2] Update `web/src/app/features/dashboard/dashboard.html` and `dashboard.ts`: for `hasBalance = false` show the text "No balance, unpaid" and no numbers; when `carryForwardDays` is set show "X days carry forward, expire on <date>" (date formatted as a date only). Extend `dashboard.spec.ts` with an Unpaid case and a carry-forward case. Verify: `npm test -- --watch=false` and `npm run build` in `web`.
- [ ] T019 [US2] Run `quickstart.md` checks 4 and 5 and report each result.

**Checkpoint**: Stories 1 and 2 both work.

---

## Phase 5: Polish and finish

- [ ] T020 Run the full checks: `dotnet build LeaveManagement.slnx`, then in `web` `npm test -- --watch=false` and `npm run build`. Report the results; say plainly that no .NET tests exist.
- [ ] T021 Run `quickstart.md` in full, including check 8 (`accrued - used - pending = available` on every card) and check 9 (under 2 seconds). Report the results.
- [ ] T022 Run the `security-reviewer` agent and the `code-reviewer` agent on the changed files, and fix the findings (own balances only, no ids from the client, ProblemDetails, no personal data in logs).
- [ ] T023 Update `ai/specs/INDEX.md` with the US-02 status and run `/learn` to write `ai/learning/us02-see-leave-balance.md` (what we built, diagram, why, interview Q&A, teach-back questions, exercise). Do not answer the teach-back or the exercise for the owner.

---

## Dependencies and execution order

- Phase 1, then Phase 2, then Phase 3, then Phase 4, then Phase 5.
- Phase 2: T002 and T003 can run in parallel; T004 is independent of both; all three must finish before Phase 3.
- Phase 3: T005 and T006 in parallel, then T007, then T008, then T009. T010 can follow T009. T011 can run in parallel with the back end (it needs only the contract); T012 needs T011; T013 needs T012; T014 needs T009, T010 and T013.
- Phase 4: T015, then T016. T017 is parallel with T015 and T016. T018 needs T016. T019 needs T016, T017 and T018.
- Story 2 depends on Story 1's endpoint and screen, because it changes the same screen and use case.

## Implementation strategy

- **MVP**: Phase 1 to 3 (User Story 1). Stop and validate with the Jira acceptance criteria before starting Phase 4.
- Do tasks one at a time and tick each only after its verify step passes. If the spec turns out to be wrong, stop, fix the spec, get approval again.

## Notes

- Owner decisions still open when these tasks were written: (R5) carry-forward used first, (R4) Unpaid shown as a "no balance" card, (R6) demo data added by an idempotent top-up with no database reset. T010, T015 and T017 depend on these answers.
- Resetting or dropping the dev database is destructive and is not part of any task. Ask first if it becomes necessary.
- No new NuGet or npm package is needed.
