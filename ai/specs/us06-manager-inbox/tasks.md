# Tasks: Review pending requests in a manager inbox

**Input**: Design documents from `ai/specs/us06-manager-inbox/` (Jira SCRUM-13)

**Prerequisites**: spec.md, plan.md, research.md, data-model.md, contracts/manager-inbox-api.md, quickstart.md. **Owner approval of these tasks is needed** before T001 starts.

**Tests**: There is no .NET test project (Phase 5), so back-end checks are the manual steps in `quickstart.md`, and every report must say so. `TeamOverlap` and the response shaping are pure so unit tests can be added in Phase 5. The new screen and service get Vitest specs (success path and a failure path each) because `.claude/rules/frontend-angular.md` requires them.

**Organization**: grouped by user story. Do one task at a time, in order. Tick a box only after its verify step passes. Commands work in Command Prompt or PowerShell; run `npm` commands in `web`. Back-end rules: `.claude/rules/backend-dotnet.md` (file-scoped namespaces, `CancellationToken` on every async method, `AsNoTracking()` on reads, warnings are errors).

**Owner decisions needed before a run task**: T012 (scenario C) creates new pending requests in the owner's LocalDB, and there is no cancel or delete until 4e. Ask first, name the requests, and report exactly what was created. Every other run task only reads.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: can run in parallel (different files, no dependency on an unfinished task)
- **[Story]**: US1, US2 or US3 (from spec.md)

## Phase 1: Setup

- [X] T001 Confirm the baseline is green before any change. Verify: `dotnet build LeaveManagement.slnx` succeeds with 0 warnings and 0 errors; in `web`, `npm test -- --watch=false`, `npm run format:check` and `npm run build` succeed. Record any failure that already exists, so it is not blamed on this story.

---

## Phase 2: Foundational (blocks all stories)

**Purpose**: the pure rule, the shapes, the ports and their readers. Nothing is visible to a user yet.

**CRITICAL**: complete before Phase 3.

- [X] T002 [P] Create the pure overlap rule in `src/LeaveManagement.Domain/TeamOverlap.cs` per research R6 and `data-model.md`. Input: one request (id, person id), the held slot rows of the team (`RequestId`, `EmployeeId`, `Date`, `Slot`) and the team size. Output: the other people with a shared slot (person id and the shared dates, distinct and ascending, people in id order) and `AwayIfApproved`. Rules, quoted from the spec: the request's own slots are the rows with its `RequestId`; another person overlaps only when they hold the **same date and the same slot** (a first half does not overlap a second half on one date; a half day overlaps a full day); the requester never counts as "other"; `AwayIfApproved` is `1 + number of distinct other people` (the requester counts, FR-006); a request with no own rows returns no others and `AwayIfApproved` 1. Domain references nothing: no clock, no database, no package. Verify: `dotnet build LeaveManagement.slnx` succeeds; read the code against these cases and say in the report that no test project exists: no overlap; one person sharing 1 of 2 dates; first half against second half (no overlap); full day against first half (overlap); two others on different dates (away = 3); a person with two requests on the same date (counted once).
- [X] T003 [P] Create the shapes and ports in `src/LeaveManagement.Application/Approvals/`: `ApprovalDtos.cs` with records `ManagerInboxResponse(IReadOnlyList<ApprovalItem> Items, int Page, int PageSize, int TotalCount)`, `ApprovalItem(int RequestId, PersonRef Employee, LeaveTypeRef LeaveType, DateOnly StartDate, DateOnly EndDate, HalfDay HalfDay, decimal DaysCount, DateTime SubmittedAtUtc, BalanceFigures? Balance, TeamOverlapInfo TeamOverlap)`, `BalanceBeforeAfter(decimal Before, decimal After)` (named so it does not clash with the Domain `BalanceFigures` used by the balances use case), `TeamOverlapInfo(int TeamSize, int AwayIfApproved, IReadOnlyList<OverlapPerson> Others)`, `OverlapPerson(int EmployeeId, string Name, IReadOnlyList<DateOnly> Dates)`, plus the small `PersonRef` and `LeaveTypeRef` records, exactly as in `contracts/manager-inbox-api.md`. The response must have **no** reason, email or any field not in the contract. `IDirectReportReader.cs` (people port): for a manager id returns the active direct reports as `(EmployeeId, FullName)`, never the manager, `Task<IReadOnlyList<ReportInfo>> GetAsync(int managerId, CancellationToken ct)`. `IApprovalRequestReader.cs` (leave port): `GetPendingPageAsync(IReadOnlyCollection<int> employeeIds, int page, int pageSize, CancellationToken ct)` returning the page of pending requests and the total count, ordered `StartDate`, `SubmittedAtUtc`, `Id` ascending; and `GetHeldSlotsAsync(IReadOnlyCollection<int> employeeIds, DateOnly from, DateOnly to, CancellationToken ct)` returning `(RequestId, EmployeeId, Date, Slot)` rows. Each takes a `CancellationToken`. Application references Domain only. Verify: `dotnet build LeaveManagement.slnx`.
- [X] T004 Implement the two readers in `src/LeaveManagement.Infrastructure/Persistence/DirectReportReader.cs` and `ApprovalRequestReader.cs`, and register both as scoped in `src/LeaveManagement.Infrastructure/DependencyInjection.cs` next to the other readers. `DirectReportReader` reads **`people.Employee` only**: `ManagerId == managerId && IsActive && Id != managerId`, `AsNoTracking()`, ordered by name. `ApprovalRequestReader` reads **`leave.*` only**: pending page = `Status == LeaveStatus.Pending && employeeIds.Contains(EmployeeId)`, ordered `StartDate`, `SubmittedAtUtc`, `Id` (all ascending), `Skip((page - 1) * pageSize).Take(pageSize)` plus a `CountAsync` for the total; held slots = `LeaveRequestDay` rows for those employees with `LeaveDate` between `from` and `to`, `AsNoTracking()`. Use `DateOnly` throughout and EF Core parameterized queries only (security rule 9). Do not read `balances.*` here. Verify: `dotnet build LeaveManagement.slnx` with 0 warnings and 0 errors; `dotnet format style LeaveManagement.slnx --verify-no-changes --severity warn --no-restore` and the `analyzers` variant pass.

**Checkpoint**: the solution builds and the building blocks are in place.

---

## Phase 3: User Story 1 - See my reports' pending requests in one list (Priority: P1) MVP

**Goal**: a manager opens the Approvals screen and sees every pending request of their direct reports, sorted by start date, with the balance before and after (spec FR-001 to FR-004, FR-010, FR-014; research R2, R4, R5, R7, R10).

**Independent Test**: sign in as Meera Rao and open Approvals: every item is pending, belongs to Asha, Ravi or Sneha, shows dates, days and balance, in start-date order; an empty team shows the "nothing waiting" message.

- [X] T005 [US1] Create the use case `src/LeaveManagement.Application/Approvals/GetManagerInbox.cs` and register it as scoped in `src/LeaveManagement.Application/DependencyInjection.cs`. `ExecuteAsync(int managerId, int page, int pageSize, CancellationToken ct)`: (1) read the reports through `IDirectReportReader`; none means return an empty page (`items` empty, `totalCount` 0, the given `page` and `pageSize`); (2) read the page through `IApprovalRequestReader.GetPendingPageAsync`; (3) for each distinct (person, leave year = `StartDate.Year`) on the page call the existing `IBalanceReader.GetLedgerAsync(employeeId, leaveYear, today, ct)` once, with `today` from `TimeProvider`; (4) per item set `Balance` = `After` is the ledger's `Available`, `Before` is `After + DaysCount`; `Balance` is null when the type has no balance or the type is not in the ledger (Unpaid); (5) set `TeamOverlap` to the **interim** value `TeamSize = number of reports, AwayIfApproved = 1, Others = empty` (replaced in T010). Names and the leave type's `Code` and `Name` come from the reports and the ledger. No reason, no email. Writes nothing and writes no audit row (FR-013). Verify: `dotnet build LeaveManagement.slnx` with 0 warnings and 0 errors; format checks pass; read the code against `contracts/manager-inbox-api.md` and say that no test project exists.
- [X] T006 [US1] Create `src/LeaveManagement.Api/Approvals/ApprovalEndpoints.cs` with `MapApprovalEndpoints` and call it in `src/LeaveManagement.Api/Program.cs` after `app.MapLeaveRequestEndpoints();`. `GET /api/approvals` with `.RequireAuthorization(Policies.RequireManager)` (a named policy, never a raw role string). The manager id is `int.TryParse(user.FindFirstValue("sub"))`; a failure is 401 `ProblemDetails`. Bind `page` (default 1) and `pageSize` (default 20) from the query; a value that is not a whole number, `page` below 1, or `pageSize` outside 1 to 100 returns 400 `ProblemDetails` with a short detail ("The constraint, verbatim: `pageSize` is 1 to 100"), never a stack trace. No person id is accepted from the client. Return `Results.Ok(response)`. Keep the endpoint thin: no rules in the lambda. Verify: `dotnet build LeaveManagement.slnx` with 0 warnings and 0 errors; format checks pass.
- [ ] T007 [P] [US1] Create the typed service `web/src/app/features/approvals/approvals.service.ts` and its spec `approvals.service.spec.ts`. Types mirror the contract exactly (`ManagerInboxResponse`, `ApprovalItem`, `BalanceFigures`, `TeamOverlapInfo`, `OverlapPerson`); no `any`. `getApprovals(page: number, pageSize: number)` calls `GET {API_URL}/api/approvals` with `page` and `pageSize` as `params`; the base URL comes from `api-config.ts`; the component never calls `HttpClient`. Spec uses `HttpTestingController`: success path (right URL and params, the body returned) and a failure path (a 403 and a 500 are passed on as errors). Verify: `npm test -- --watch=false` passes and `npm run format:check` is clean.
- [ ] T008 [US1] Create the screen `web/src/app/features/approvals/approvals.ts`, `approvals.html`, `approvals.scss` and `approvals.spec.ts`, and make `loaderFor('approvals')` in `web/src/app/app.routes.ts` open it (import it like the apply screen; update `app.routes.spec.ts` if it asserts the placeholder for `approvals`). Standalone component, `inject()`, signals, new control flow (`@if`, `@for` with `track item.requestId`), no `innerHTML`. States from `contracts/manager-inbox-api.md`: loading ("Loading requests..." in a `role="status"` region), error with a Try again button (`role="alert"`), empty ("Nothing is waiting for your decision."), and the list of cards (name, leave type, dates, "First half" or "Second half" for a half day, days, "Before 8.0, after 6.0" when `balance` is not null). Paging: Previous and Next buttons driven by `totalCount` and `pageSize`, disabled at the ends, with "Page N of M"; page size fixed at 20. Labelled fields, buttons for actions, no colour-only meaning. No Approve or Reject button. Do not show the overlap text yet (T011). Spec (Vitest, mocked service): success path (cards in the given order with the fields), empty state, error state with retry, and paging (Next asks for page 2; Previous is disabled on page 1). Verify: `npm test -- --watch=false` passes, `npm run format:check` is clean and `npm run build` succeeds.
- [ ] T009 [US1] Run quickstart scenarios A, B and F against the real API (all read only) and report each as PASS, FAIL or NOT RUN. Start the API from one place only (`dotnet run --project src\LeaveManagement.Api`), use a manager token (Meera Rao, id 1) and the other tokens listed in the quickstart. Expected: A: 200, only the three reports' pending requests, sorted by `startDate` then `submittedAtUtc`, `balance.before` minus `balance.after` equals `daysCount`, no field outside the contract; B: Meera 200, Asha 403, Hema 403, no token 401, `page=0`, `pageSize=500` and `pageSize=abc` give 400; F: balances and request counts identical before and after several reads. State the exact counts seen and say plainly that the Jira example (3 pending from reports) is matched only if the data gives it, that no browser and no .NET tests were used. Stop the API when done. Verify: the report lists each scenario with its result.

**Checkpoint**: User Story 1 works on the server and in the screen (overlap shows as "no one else" until US2).

---

## Phase 4: User Story 2 - See who else is away for the same dates (Priority: P1)

**Goal**: each item names the other reports away on the same days and shows "N of team size away" with the requester counted (spec FR-005, FR-006; research R4, R6).

**Independent Test**: with Sneha and Asha both holding leave on 2026-12-02, each of their items names the other with the shared date and shows 2 of 3 away.

- [ ] T010 [US2] Wire the overlap into `src/LeaveManagement.Application/Approvals/GetManagerInbox.cs`, replacing the interim value from T005. After reading the page, read the team's held slots once with `GetHeldSlotsAsync(reportIds, minStartOnPage, maxEndOnPage)`; for each item call `TeamOverlap` (T002) with the item's request id and person, the held rows and the team size, then turn the person ids into `OverlapPerson` records using the reports' names (people outside the team never appear). `TeamSize` is the number of reports; `AwayIfApproved` is the Domain result (the requester counts). Do not change the ordering, paging or balance code. Verify: `dotnet build LeaveManagement.slnx` with 0 warnings and 0 errors; format checks pass; read the code against the contract's `teamOverlap` table and say that no test project exists.
- [ ] T011 [US2] Show the overlap in `web/src/app/features/approvals/approvals.html` and extend `approvals.spec.ts`. Texts from the contract, in words and never colour only: no others: "No one else in your team is away on these days."; one or more: "Also away: Asha Nair (2026-12-02); Ravi Kumar (2026-12-01, 2026-12-02). 3 of 3 of your team would be away if you approve." (names and dates from the data, `awayIfApproved` of `teamSize`). New control flow, no `innerHTML`. Spec cases: no others; one other; several others; the count shown is the one in the data (not recomputed on the screen, FR-006 and security rule 1). Verify: `npm test -- --watch=false` passes, `npm run format:check` is clean and `npm run build` succeeds.
- [ ] T012 [US2] Run quickstart scenario C against the real API. **Ask the owner first** (it creates new pending requests): with Sneha's request on 2026-12-01 to 12-02 present, create one pending Annual request for Asha on 2026-12-02 only (new `Idempotency-Key`, see the US-03 contract). Expected: Sneha's item names Asha with `2026-12-02`; Asha's item names Sneha with `2026-12-02`; both show `awayIfApproved` 2 and `teamSize` 3; an item with no overlap shows no others and `awayIfApproved` 1. The half-day cases (Ravi first half against Asha second half, no overlap; a full day against a first half, overlap) only if the owner agrees to one more request. If the owner declines, run what the existing data allows (read only) and report the rest NOT RUN. Say plainly that rejected, cancelled and approved requests cannot be created (4e not built), that no browser and no .NET tests were used, and list every request created. Stop the API when done. Verify: the report lists each case with its result and the exact requests created.

**Checkpoint**: User Story 2 works. The approved, rejected and cancelled overlap cases stay untested until 4e.

---

## Phase 5: User Story 3 - See only my own reports' requests (Priority: P1)

**Goal**: the server shows a manager only their direct reports' pending requests and refuses everyone else (spec FR-007 to FR-009, FR-011; security rules 1, 3, 4, 7, 12). The rule is enforced in the code written in T004 to T006; this phase proves it.

**Independent Test**: Asha (Employee) is refused with 403; Meera's list never contains her own or anyone else's requests.

- [ ] T013 [US3] Prove the resource rule. (a) Read `DirectReportReader.cs`, `ApprovalRequestReader.cs`, `GetManagerInbox.cs` and `ApprovalEndpoints.cs` and confirm, in the report, line by line: the manager id comes from `sub` only; the team is read from `people.Employee` on every call (never from the token or the query); the caller and inactive people are excluded; the pending query filters on the reports' ids; the held-slots query uses only the reports' ids; no reason, email or outside person is in any response record. (b) Run what the seeded data allows against the API (read only, also covered by T009 scenario B): Employee-only 403, HR-only 403, no token 401. (c) Report **NOT RUN** for the cases that need a second manager and his own reports (another manager's reports never appear; a person who no longer reports to the caller disappears; Manager plus HR sees only reports), unless the owner agrees to create that test data. Verify: the report states each point as confirmed from the code, run, or NOT RUN with the reason.

**Checkpoint**: all three stories are checked, with a plain statement of what was and was not run.

---

## Phase 6: Polish and finish

- [ ] T014 Run `/ci-check` (build, standards, tests, repo guard, packages). Verify: every step is PASS or an honest NOT RUN that CI covers (the missing .NET test project is expected).
- [ ] T015 Run the finish steps from `CLAUDE.md`: `/review` (code-reviewer and security-reviewer; the security-reviewer checks rules 1, 3, 4, 7, 10 and 12 against the new endpoint, the three ports and the response), then `/learn us06-manager-inbox` for `ai/learning/us06-manager-inbox.md`, then `/status` to update `ai/specs/INDEX.md`. Do not write the owner's teach-back answers or exercise. Verify: review findings are fixed or listed, the notes file exists, and the INDEX row is updated.

---

## Dependencies and order

```text
T001 -> (T002, T003 in any order) -> T004 -> T005 -> T006 -> (T007, T008) -> T009
     -> T010 -> T011 -> T012 -> T013 -> T014 -> T015
```

- T002 and T003 are marked [P] (different projects, no shared file) but T004 needs T003.
- T007 is marked [P] (a service file), but T008 uses it, so run T007 first.
- T010 needs T002 (the rule) and T005 (the use case). T011 needs T008 and T010. T012 needs T010.
- T013 needs T006 (the endpoint) and is best after T012.

## Requirement coverage

| Requirement | Task |
| --- | --- |
| FR-001 inbox of reports' pending requests | T004, T005, T006, T008, T009 |
| FR-002 item fields | T003, T005, T008, T009 |
| FR-003 pending only | T004, T009 |
| FR-004 sort and stable order | T004, T008, T009 |
| FR-005 who else is away | T002, T010, T011, T012 |
| FR-006 team-away count includes the requester | T002, T010, T011, T012 |
| FR-007 only direct reports | T004, T013 |
| FR-008 Manager role only, sign-in required | T006, T009, T013 |
| FR-009 relationship looked up each time | T004, T006, T013 |
| FR-010 empty, error and loading states | T008 |
| FR-011 names only to the manager, no reasons | T003, T013, T015 |
| FR-012 keyboard and screen reader | T008, T011 (Vitest attributes only; no real screen reader) |
| FR-013 reading changes nothing | T005, T009 (scenario F) |
| FR-014 paging | T004, T006, T008 |
| SC-001 | T009 |
| SC-002 | T012 (only if the owner agrees to the new request) |
| SC-003 | T013 (a second manager is NOT RUN unless test data is created) |
| SC-004 | T009 (scenario B) |
| SC-005 | T009 (state the measured time, or say it was not timed) |
| SC-006 | T009 (scenario F) |

## Notes

- Not verifiable here: how the screen looks in a browser and with a screen reader, approved, rejected and cancelled overlap cases (4e), another manager's reports (needs test data), and automated back-end tests (Phase 5).
- If a step finds a gap in the spec or plan, stop, fix the spec and plan, and get approval again (constitution VI).
- Open from earlier work, not part of this story: the weekend or holiday half-day message (`halfday.invalid` or `days.none`) from US-04.
