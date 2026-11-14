# Tasks: Apply for leave with an accurate working-day count

**Input**: Design documents from `ai/specs/us03-apply-leave-working-days/` (Jira SCRUM-10)

**Prerequisites**: spec.md, plan.md, research.md, data-model.md, contracts/leave-requests-api.md, quickstart.md. **Owner approval of these tasks is still needed** before T001 starts. The four open decisions in the Notes at the end use the defaults in `research.md` (R3, R6, R8, R9) until the owner says otherwise. T008, T012 and T016 depend on them.

**Tests**: There is no .NET test project (Phase 5), so back-end checks are the manual steps in `quickstart.md`, and every report must say so. Front-end tasks include Vitest specs because `.claude/rules/frontend-angular.md` requires a spec per component and service. The Domain code is pure so unit tests can be added in Phase 5.

**Organization**: grouped by user story. Do one task at a time, in order. Tick a box only after its verify step passes.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: can run in parallel (different files, no dependency on an unfinished task)
- **[Story]**: US1, US2 or US3 (from spec.md)
- Commands work in Command Prompt or PowerShell, from the repo root.

## Phase 1: Setup

- [X] T001 Confirm the baseline is green before any change. Verify: `dotnet build LeaveManagement.slnx` succeeds; in `web`, `npm test -- --watch=false` and `npm run build` succeed. Record any failure that already exists, so it is not blamed on this feature.

---

## Phase 2: Foundational (blocks all stories)

**Purpose**: the pure counting code, the shapes and the ports. No behaviour visible to a user yet.

**CRITICAL**: complete before Phase 3.

- [X] T002 [P] Create the pure counter in `src/LeaveManagement.Domain/WorkingDayCalculator.cs` per research R1. Input: start `DateOnly`, end `DateOnly`, `HalfDay`, and a set of holiday `DateOnly`. Output: the list of counted `(DateOnly Date, Slot Slot)` pairs, the list of skipped holiday dates inside the range, and `DaysCount` = number of slots x 0.5 as `decimal`. Rules: Monday to Friday only; a Saturday, Sunday or holiday never counts, including between two leave days (sandwich off, rule 10); a full working day gives two slots (AM and PM); `FirstHalf` gives AM only and `SecondHalf` gives PM only, and a half day applies to a single day. Domain references nothing; no clock, no database. Verify: `dotnet build LeaveManagement.slnx`; read the code against the Jira examples (Fri 3 Oct to Tue 7 Oct, no holiday: 3.0; holiday Mon 6 Oct: 2.0; Sat to Sun: 0 slots; a holiday on a Saturday inside Fri to Mon still gives 2.0, not 1.5) and say in the report that no test project exists.
- [X] T003 [P] Create the shapes in `src/LeaveManagement.Application/Leave/LeaveDtos.cs` and `src/LeaveManagement.Application/Leave/LeaveRuleError.cs`. Records: `ApplyLeaveRequest(int LeaveTypeId, DateOnly StartDate, DateOnly EndDate, HalfDay HalfDay, string? Reason)`; `LeaveRequestResponse(int Id, int LeaveTypeId, DateOnly StartDate, DateOnly EndDate, HalfDay HalfDay, decimal DaysCount, string Status, short LeaveYear)`; `PreviewResponse(decimal DaysCount, IReadOnlyList<DateOnly> WorkingDays, IReadOnlyList<DateOnly> SkippedHolidays)`. `LeaveRuleError` holds a `Code` and a `Message`. The message text comes from the message table in `contracts/leave-requests-api.md`, with `{n}` replaced by the number from the leave type, and the endpoint sends it as the ProblemDetails `detail` (T011). Define the codes from `contracts/leave-requests-api.md` as constants (`date.range`, `date.past`, `year.cross`, `days.none`, `halfday.invalid`, `type.unknown`, `notice.short`, `max.consecutive`, `leave.overlap`, `balance.insufficient`, `idempotency.mismatch`). Request has no employee id, status or count. Constraints to keep verbatim: reason "up to 500 characters"; `DaysCount` is `decimal(4,1)`, multiples of 0.5, greater than 0. Verify: `dotnet build LeaveManagement.slnx`.
- [X] T004 [P] Create three read ports in `src/LeaveManagement.Application/Leave/`: `IApplicantReader.cs` (for an employee id returns `LocationId`, the location `TimeZoneId` and `IsActive`, or null), `IHolidayReader.cs` (for a location id and a date range returns the holiday dates), `ILeaveTypeReader.cs` (for a leave type id returns `IsActive`, `AccrualMode`, `AllowHalfDay`, `MinNoticeWorkingDays`, `MaxConsecutiveDays`, or null). Each takes a `CancellationToken`. Verify: `dotnet build LeaveManagement.slnx`.
- [X] T005 Create the write port `src/LeaveManagement.Application/Leave/ILeaveApplicationStore.cs` per research R2 and the sequence in `data-model.md`. One method takes: employee id, idempotency key, request hash, the request, the counted slots, `DaysCount` and `hasBalance` (the store takes the submitted time from `TimeProvider` itself, see T008); it returns either the saved `LeaveRequestResponse` (new or replayed) or a `LeaveRuleError` (`leave.overlap`, `balance.insufficient`, `idempotency.mismatch`). It takes a `CancellationToken`. Depends on T003, which defines `LeaveRuleError` and `LeaveRequestResponse`. Verify: `dotnet build LeaveManagement.slnx`.

**Checkpoint**: the solution builds and the shapes are fixed.

---

## Phase 3: User Story 1 - Apply for leave and get the right working-day count (Priority: P1) MVP

**Goal**: a signed-in person applies, the server counts working days, the request is saved as pending and the balance is reduced, all in one transaction.

**Independent Test**: `quickstart.md` scenarios 1, 2, 3, 4, 5, 12, 14, 15, 16, 17, 18, 19 and 21, and counting-table rows 1 to 10 (saved `daysCount`).

### Back end

- [X] T006 [US1] Create `src/LeaveManagement.Domain/LeaveRules.cs` with the basic checks that need no clock: end before start gives `date.range`; a half day with start different from end, or on a type that does not allow it, or on a non-working day gives `halfday.invalid`; zero counted slots gives `days.none`. Return the first failure only, in the order in `contracts/leave-requests-api.md`. Domain references nothing. Verify: `dotnet build LeaveManagement.slnx`.
- [X] T007 [P] [US1] Implement `ApplicantReader`, `HolidayReader` and `LeaveTypeReader` in `src/LeaveManagement.Infrastructure/Persistence/` (one file each) with `LeaveDbContext`, `AsNoTracking()`, EF parameterized LINQ only. Each reads only its own module's tables (`people.*`, `balances.Holiday`, `balances.LeaveType`). The holiday query is a range seek on (LocationId, HolidayDate). Verify: `dotnet build LeaveManagement.slnx` with no warnings.
- [X] T008 [US1] Implement `LeaveApplicationStore` in `src/LeaveManagement.Infrastructure/Persistence/LeaveApplicationStore.cs` as one explicit transaction in the order of `data-model.md`. Open the transaction on the injected `LeaveDbContext`, the same scoped instance that `AuditWriter` uses, so the audit row joins it (check both are registered as scoped in `Infrastructure/DependencyInjection.cs`). If the SQL Server provider is set to retry on failure, run the work inside `CreateExecutionStrategy().ExecuteAsync`, because a user-started transaction needs it. This store is the named cross-module store that constitution Principle III (version 1.2.0) allows (plan, Complexity Tracking). Steps: (1) look up `leave.IdempotencyKey` for (EmployeeId, key): same hash returns the saved answer, different hash returns `idempotency.mismatch`; (2) insert `LeaveRequest` with `Status = Pending`, `SubmittedAtUtc` from `TimeProvider`, `DaysCount` as given, `Reason` stored but never audited; (3) insert one `LeaveRequestDay` per slot; a unique-index violation (SQL error 2601 or 2627) rolls back and returns `leave.overlap`; (4) when `hasBalance`, run the guarded `ExecuteUpdate` "Available = Available - days where EmployeeId, LeaveTypeId, LeaveYear match and Available >= days"; 0 rows changed rolls back and returns `balance.insufficient`; (5) when `hasBalance`, insert one `LeaveBalanceEntry` of type `Deduction`, `Days = -DaysCount`, `RequestId` set, `CreatedByEmployeeId` = the applicant; (6) call `IAuditWriter` with action `leave.apply`, entity `LeaveRequest`, detail of ids, dates and days only (no reason, name or email); (7) insert the `IdempotencyKey` row with the saved answer; if that insert hits the primary-key conflict because a parallel request with the same key won, roll back and return that winner's saved answer (research R5). Never read-then-write the balance. Constraints to keep verbatim: `IdempotencyKey.KeyValue` max 64 characters; audit rows hold "ids and values, never names, emails or free-text reasons". Verify: `dotnet build LeaveManagement.slnx`; read the code against the sequence in `data-model.md` and confirm every database error path rolls back. Walk through the failure path by reading: if step 6 (audit) throws, steps 2 to 5 must not remain committed. State plainly that this was a read-through and not a run.
- [X] T009 [US1] Implement the use case `ApplyForLeave` in `src/LeaveManagement.Application/Leave/ApplyForLeave.cs`: read the applicant (inactive or missing gives a 401 outcome), read the leave type (`type.unknown` when missing or inactive), read holidays for the range, run `WorkingDayCalculator`, run the basic `LeaveRules` (T006), compute the request hash from the body fields, then call `ILeaveApplicationStore` with `hasBalance = AccrualMode != None`. The leave year is the start date's year. The count is never taken from the client. Depends on T002 to T008. Verify: `dotnet build LeaveManagement.slnx`.
- [X] T010 [US1] Register the use case and the four ports' implementations: `src/LeaveManagement.Application/DependencyInjection.cs` (`ApplyForLeave`) and `src/LeaveManagement.Infrastructure/DependencyInjection.cs` (readers and store). Verify: `dotnet build LeaveManagement.slnx`.
- [X] T011 [US1] Create `src/LeaveManagement.Api/Leave/LeaveRequestEndpoints.cs` with `MapLeaveRequestEndpoints` and `POST /api/me/leave-requests` per `contracts/leave-requests-api.md`: the employee id from the `sub` claim only (401 ProblemDetails when it is not a number, as in `Balances/BalanceEndpoints.cs`); require the `Idempotency-Key` header (1 to 64 characters from `A-Z a-z 0-9 - _`, else 400); bind a request DTO (never an entity); a `LeaveRuleError` becomes 422 ProblemDetails with a `code` extension and its `Message` as `detail`, except `leave.overlap` which becomes 409; success is 201 with `LeaveRequestResponse`. Thin endpoint, no business rule in the lambda. No role policy; the fallback policy requires sign-in. Log only ids. Add `app.MapLeaveRequestEndpoints();` in `src/LeaveManagement.Api/Program.cs` next to the other `Map...` calls. Verify: build; run the API and call with no token (401), with a token and no key (400), and with a token and a valid body (201).
- [X] T012 [US1] Add an idempotent demo top-up in `src/LeaveManagement.Infrastructure/Persistence/DevSeeder.cs` (Development only, called from `DatabaseInitializer.cs` if needed) per research R8: two demo holidays, named so they are clearly demo data, each added only when missing: a Monday (called H in `quickstart.md`) at least 14 days after today (using `TimeProvider`), and a Wednesday (called W) in a different week from H; and confirm one demo person has an Annual balance of at least 10 for the current year (add only if missing). Rows are added, never reset. Write both holiday dates to the log at startup and note it in `quickstart.md`. Verify: start the API twice; the second start adds no rows.

### Front end

- [X] T013 [P] [US1] Create `web/src/app/features/apply-leave/leave-request.service.ts` and `leave-request.service.spec.ts`: `apply(body, idempotencyKey)` calls `POST /api/me/leave-requests` with the header `Idempotency-Key`, using the base URL from `web/src/app/api-config.ts`, typed interfaces matching the contract, no `any`. Spec: one success (201) and one failure (422 with a `code`) with `HttpTestingController`. Verify: `npm test -- --watch=false` in `web`.
- [X] T014 [US1] Create the screen `web/src/app/features/apply-leave/apply-leave.ts`, `.html`, `.scss` and `apply-leave.spec.ts` (class `ApplyLeave`, standalone, signals, `@if` and `@for` with `track`, `inject()`). Fields: leave type (from `BalanceService`, types that have a balance or are Unpaid), start date, end date, half day (shown only when start equals end and the type allows it), optional reason (max 500). Labelled inputs and a submit button; a new random `Idempotency-Key` per new application, kept for a retry of the same form; a visible loading state; on 201 show the saved count and status; on error show the server message (the full code-to-message map is T019). No day counting in the browser. Spec: success shows the count; a 422 shows an error; the button is disabled while sending. Depends on T013. Verify: `npm test -- --watch=false`.
- [X] T015 [US1] Add the route and link: in `web/src/app/layout/nav-items.ts` and `web/src/app/app.routes.ts` load `ApplyLeave` for the apply-leave path (keep `authGuard`, `roleGuard` and the `NAV_ITEMS` table; other paths stay on the placeholder). Update `web/src/app/layout/nav-items.spec.ts` and `web/src/app/app.routes.spec.ts` if they assert the placeholder for that path. Verify: `npm test -- --watch=false` and `npm run build` in `web`.

### Verify the story

- [X] T016 [US1] Run `quickstart.md` scenarios 1, 2, 3, 4, 5, 12, 14, 15, 16, 17, 18, 19 and 21, counting-table rows 1 to 10 (compare the saved `daysCount` with the hand count), and the database checks after scenario 1. Report each result. Anything not run (for example the parallel submit in scenario 16) is stated plainly.

**Checkpoint**: User Story 1 works on its own. This is the MVP.

---

## Phase 4: User Story 3 - Be stopped when the request breaks a policy rule (Priority: P1)

**Goal**: notice, maximum, past date, leave-year, overlap and balance rules refuse a request with a clear message and change nothing.

**Independent Test**: `quickstart.md` scenarios 7, 8, 9, 10, 11 and 13.

- [X] T017 [US3] Extend `src/LeaveManagement.Domain/LeaveRules.cs` with the clock-dependent checks, in the order in `contracts/leave-requests-api.md`: `year.cross` (start and end in different years), `date.past` (start before the person's today, FR-019, for every leave type), `notice.short` (working days strictly between today and start is less than the type's `MinNoticeWorkingDays`, rule 7, research R3), `max.consecutive` (end - start + 1 calendar days is more than the type's `MaxConsecutiveDays` when it is not null, rule 8; message says longer leave needs HR). Rules read the numbers from the leave type, not a hard-coded "Annual". Pass "today" in as a parameter; the Domain has no clock. Verify: `dotnet build LeaveManagement.slnx`; read against the examples (apply Mon for Fri: 3 working days notice, allowed; apply Tue for Fri: 2, refused).
- [X] T018 [US3] Wire it in `src/LeaveManagement.Application/Leave/ApplyForLeave.cs`: take the instant from `TimeProvider`, convert it to the person's location time zone (`TimeZoneId` from `IApplicantReader`) to get today (research R4), and pass today to `LeaveRules`. If the time zone id cannot be resolved, fail with a 500 ProblemDetails and a log line, never a silent fall back to UTC. Add the overlap pre-check to `LeaveApplicationStore.cs` (a read of the person's `LeaveRequestDay` rows for the slots, before the insert) so the usual overlap returns `leave.overlap` with a clear message before the unique index is hit; the index stays as the race backstop. Confirm `balance.insufficient` from T008 returns a clear message and that a missing balance row counts as insufficient. Depends on T017. Verify: `dotnet build LeaveManagement.slnx`.
- [X] T019 [P] [US3] Use the exact wording from the message table in `contracts/leave-requests-api.md` (the server sends the same text as `detail`). Add the code-to-message map in `web/src/app/features/apply-leave/apply-leave.ts` and `.html` for every `code` in the contract (`date.range`, `date.past`, `year.cross` with "submit one request per year", `days.none`, `halfday.invalid`, `type.unknown`, `notice.short`, `max.consecutive` with "longer leave needs HR", `leave.overlap`, `balance.insufficient`, `idempotency.mismatch`), shown in an element with `role="alert"`. An unknown code shows the server `title`. Extend `apply-leave.spec.ts` with `notice.short`, `leave.overlap` and `balance.insufficient`. Verify: `npm test -- --watch=false` and `npm run build` in `web`.
- [X] T020 [US3] Run `quickstart.md` scenarios 7, 8, 9, 10, 11 and 13, and check that each message matches the wording in `contracts/leave-requests-api.md` (FR-020). For each refusal check that no table gained a row and `Available` did not change. Report each result.

**Checkpoint**: Stories 1 and 3 both work.

---

## Phase 5: User Story 2 - See the count before I submit (Priority: P2)

**Goal**: the form shows the working-day count before submit, using the same counting as the server.

**Independent Test**: `quickstart.md` scenario 6.

- [X] T021 [US2] Create the use case `src/LeaveManagement.Application/Leave/PreviewWorkingDays.cs` and register it: same applicant, type and holiday reads and the same `WorkingDayCalculator` and `LeaveRules` as `ApplyForLeave` (pull the shared steps into one private method or class so they cannot drift). Run only the checks that need no balance or overlap lookup; write nothing and no audit row. Returns `PreviewResponse`. Verify: `dotnet build LeaveManagement.slnx`.
- [X] T022 [US2] Add `GET /api/me/leave-requests/preview` to `src/LeaveManagement.Api/Leave/LeaveRequestEndpoints.cs` per the contract: query `leaveTypeId`, `startDate`, `endDate`, `halfDay`; the person from the `sub` claim only; 400 for a malformed query, 401 as for POST, 422 with `code` for rule failures. Verify: build; call with no token (401) and with a token for the Jira example (expect `daysCount` 3.0, or 2.0 with the demo holiday).
- [X] T023 [P] [US2] Add `preview(query)` to `web/src/app/features/apply-leave/leave-request.service.ts` and extend `leave-request.service.spec.ts` (one success, one 422). Verify: `npm test -- --watch=false` in `web`.
- [X] T024 [US2] Show the preview in `web/src/app/features/apply-leave/apply-leave.ts` and `.html`: when type and both dates are valid, call `preview` and show the count, the counted dates and the skipped holidays; update when a date changes; show the server message when the preview returns a rule error; the preview is information only and submit still recounts. Extend `apply-leave.spec.ts`: the preview shows the server count and changes when a date changes. Depends on T023. Verify: `npm test -- --watch=false` and `npm run build` in `web`.
- [X] T025 [US2] Run `quickstart.md` scenario 6 (preview equals saved count) and report the result.

**Checkpoint**: all three stories work.

---

## Phase 6: Polish and finish

- [X] T026 Run the full checks: `dotnet build LeaveManagement.slnx`, then in `web` `npm test -- --watch=false` and `npm run build`. Report the results; say plainly that no .NET tests exist.
- [X] T027 Run `quickstart.md` in full, including the counting table (SC-002), the timing check in scenario 20 (SC-003) and every database check. Report the results and list any scenario not run.
- [X] T028 Run the `security-reviewer` agent and the `code-reviewer` agent on the changed files, and fix the findings (own requests only, no ids or counts from the client, over-posting closed, ProblemDetails, no reason text or personal data in audit rows or logs).
- [X] T029 Update `ai/specs/INDEX.md` with the US-03 status and run `/learn` to write `ai/learning/us03-apply-leave-working-days.md` (what we built, diagram, why, interview Q&A, teach-back questions, exercise). Do not answer the teach-back or the exercise for the owner.

---

## Dependencies and execution order

- Phase 1, then Phase 2, then Phase 3, then Phase 4, then Phase 5, then Phase 6.
- Phase 2: T002, T003 and T004 can run in parallel; T005 follows T003. All four must finish before Phase 3.
- Phase 3: T006 and T007 in parallel, then T008 (needs T005 and T007), then T009 (needs T002 to T008), then T010, then T011. T012 can follow T010. T013 can run in parallel with the back end (it needs only the contract); T014 needs T013; T015 needs T014; T016 needs T011, T012 and T015.
- Phase 4: T017, then T018. T019 is parallel with T017 and T018. T020 needs T018 and T019.
- Phase 5: T021, then T022. T023 is parallel with T021 and T022. T024 needs T023. T025 needs T022 and T024.
- Story 3 and Story 2 both change the Story 1 use case and screen, so they follow it. Story 3 comes before Story 2 because it is also P1.

## Implementation strategy

- **MVP**: Phases 1 to 3 (User Story 1). Stop and validate with the two Jira acceptance criteria before starting Phase 4.
- Do not release User Story 1 alone to anyone: without the Phase 4 rules, a person can apply with too little notice or for too many days. Treat Phases 3 and 4 as one release.
- Do tasks one at a time and tick each only after its verify step passes. If the spec turns out to be wrong, stop, fix the spec, get approval again.

## Notes

- Owner decisions still open when these tasks were written, with the defaults used here: (R3) notice counts working days strictly between today and the start date, and rule 8 counts calendar days; (R6) 4e will have to free a rejected or cancelled request's day rows, which is not built here; (R8) a demo Monday holiday is added by an idempotent top-up; (R9) probation (rule 6) and the medical certificate (rule 9) are not enforced in this story. T008, T012, T017 and T018 depend on the first, third and fourth answers.
- The Jira example dates (Fri 3 Oct to Tue 7 Oct) are in the past, and back-dated requests are refused, so verification uses any future Friday-to-Tuesday range.
- US-02's `GetMyBalances` takes "today" from the UTC date. This story uses the person's local date. Do not change US-02 here; tell the owner if the difference shows up on 31 December or 1 January.
- Resetting or dropping the dev database is destructive and is not part of any task. Ask first if it becomes necessary.
- No new NuGet or npm package is needed.
