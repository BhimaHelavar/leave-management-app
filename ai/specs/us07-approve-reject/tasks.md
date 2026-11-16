# Tasks: Approve or reject a request with a comment

**Input**: Design documents from `ai/specs/us07-approve-reject/` (Jira SCRUM-14)

**Prerequisites**: spec.md, plan.md, research.md, data-model.md, contracts/decision-api.md, quickstart.md. **Owner approval of these tasks is needed** before T001 starts. **US-06 (SCRUM-13) must be far enough along**: its overlap wiring (US-06 T010) and its web service (US-06 T007) are used here. T001 checks this and stops if they are open.

**Tests**: There is no .NET test project (Phase 5), so back-end checks are the manual steps in `quickstart.md`, and every report must say so. `LeaveDecision` is a pure Domain rule so unit tests can be added in Phase 5. The new web code gets Vitest specs (a success path and a failure path each) because `.claude/rules/frontend-angular.md` requires them.

**Organization**: grouped by user story. Do one task at a time, in order. Tick a box only after its verify step passes. Commands work in Command Prompt or PowerShell; run `npm` commands in `web`. Back-end rules: `.claude/rules/backend-dotnet.md` and `.claude/rules/database-efcore.md` (file-scoped namespaces, `CancellationToken` on every async method, `AsNoTracking()` on reads, warnings are errors, one guarded atomic balance update plus a ledger entry, no names, emails or free text in audit or outbox rows).

**Owner decisions needed before a run task**: every successful decision in T009, T011 and T014 changes real data (a request, and for a rejection a balance, day rows, an audit row and an outbox row) and **cannot be undone** in this story. Ask first, name the requests, and report exactly what was decided. Reads and refusals write nothing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: can run in parallel (different files, no dependency on an unfinished task)
- **[Story]**: US1 to US5 (from spec.md)

## Phase 1: Setup

- [X] T001 Confirm the baseline and the prerequisite before any change. Verify: `dotnet build LeaveManagement.slnx` succeeds with 0 warnings and 0 errors; in `web`, `npm test -- --watch=false`, `npm run format:check` and `npm run build` succeed. Record any failure that already exists. Then read `ai/specs/us06-manager-inbox/tasks.md` and `src/LeaveManagement.Application/Approvals/GetManagerInbox.cs`: **if US-06 T010 (overlap wiring) or T007 (web service) is still open, stop and tell the owner**, who chooses between finishing US-06 first and having T006 here finish the shared pieces. Do not decide silently.
  - Result 2026-11-14: baseline green. US-06 T007 and T010 were open, so the owner chose to **finish US-06 first** (T007 to T012 there), then return to US-07 T002.

---

## Phase 2: Foundational (blocks all stories)

**Purpose**: the pure rule, the shapes and ports, the readers, the named store, the shared item builder and the use cases. Nothing is visible to a user yet.

**CRITICAL**: complete before Phase 3.

- [X] T002 [P] Create the pure rule in `src/LeaveManagement.Domain/LeaveDecision.cs` per research R7 and `data-model.md`. A `DecisionOutcome` enum (Approve, Reject), a constant `MaxCommentLength = 500`, and a function that takes the outcome and the raw comment and returns either the cleaned comment (trimmed; spaces only counts as none, so `null`) or a refusal reason. Rules, quoted from the spec: a rejection **requires** a comment that is "not empty and not only spaces"; an approval comment is optional; a comment is "at most 500 characters" after trimming, longer is refused as too long; the checks run in this order: too long, then missing for a rejection. Domain references nothing: no clock, no database, no package. Verify: `dotnet build LeaveManagement.slnx` succeeds; read the code against these cases and say in the report that no test project exists: reject with null, empty and spaces-only comments; reject with exactly 500 and with 501 characters; approve with null and with a comment; a comment with surrounding spaces is trimmed.
- [X] T003 [P] In `src/LeaveManagement.Application/Approvals/` add the shapes, the write port and the errors. In `ApprovalDtos.cs` add `ApprovalDetail` (the inbox item fields plus `Status`, `Decision`, `AllowedActions`; `Balance` and `TeamOverlap` null once decided), `DecisionInfo(string Outcome, DateTime DecidedAtUtc, string? Comment)` and `DecideRequestBody(string? Comment)`, exactly as in `contracts/decision-api.md`; enums go out as strings; no reason, email or field outside the contract. Create `ILeaveDecisionStore.cs`: `Task<DecisionResult> DecideAsync(LeaveDecisionCommand command, CancellationToken ct)` where the command holds the request id, owner id, leave type id, leave year, days, outcome, cleaned comment, deciding manager id and whether the type has a balance, and the result is either success or one of the refusals (`request.not_pending`); document that this is the **named cross-module store** allowed by constitution Principle III (spec FR-018). Add to `IApprovalRequestReader.cs` a method `GetByIdAsync(int requestId, CancellationToken ct)` returning one request of any status with its decision fields, or null. Add error builders next to the other refusals (`src/LeaveManagement.Application/Leave/LeaveRuleError.cs` pattern): code `comment.required`, text "A rejection needs a comment that tells the employee why."; code `request.not_pending`, text "This request has already been decided." Application references Domain only. Verify: `dotnet build LeaveManagement.slnx` (it fails only at the reader implementation, fixed in T004, or succeeds if T004 is done in the same step); format checks pass.
- [X] T004 Implement `GetByIdAsync` in `src/LeaveManagement.Infrastructure/Persistence/ApprovalRequestReader.cs`. Reads **`leave.LeaveRequest` only**, `AsNoTracking()`, one row by id, any status, with `Status`, `DecidedByEmployeeId`, `DecidedAtUtc`, `DecisionComment`. Parameterized EF queries only (security rule 9). Verify: `dotnet build LeaveManagement.slnx` with 0 warnings and 0 errors; `dotnet format style LeaveManagement.slnx --verify-no-changes --severity warn --no-restore` and the `analyzers` variant pass.
- [X] T005 Implement the named store in `src/LeaveManagement.Infrastructure/Persistence/LeaveDecisionStore.cs` and register it as scoped in `src/LeaveManagement.Infrastructure/DependencyInjection.cs`. Follow `LeaveApplicationStore`: `CreateExecutionStrategy`, explicit transaction, `ChangeTracker.Clear()` at the start, `IAuditWriter` on the same context, `TimeProvider` for "now" (UTC). One transaction, in this order: (1) a **guarded update** with `ExecuteUpdateAsync`: set `Status`, `DecidedByEmployeeId` (the manager), `DecidedAtUtc` and `DecisionComment` **where `Id` equals the request id and `Status` equals Pending**; zero rows means roll back and return `request.not_pending`; (2) **reject only**: delete the request's `LeaveRequestDay` rows (`ExecuteDeleteAsync`, filtered by `RequestId`); (3) **reject only, when the type has a balance**: one atomic update `Available = Available + days` on the owner's `LeaveBalance` for the type and leave year, **zero rows changed is a fault: roll back and throw**, never a silent loss of days; then add one `LeaveBalanceEntry` with `EntryType` Restore, `Days` equal to **+days**, the request id, `CreatedByEmployeeId` the manager, `CreatedAtUtc`; `days` is the request's saved `DaysCount` (decimal 4,1), never recalculated; (4) the audit row through `IAuditWriter`: action `leave.approve` or `leave.reject`, entity `LeaveRequest`, actor the manager, detail ids and numbers only (`requestId`, `leaveTypeId`, `outcome`, `days`), **never the comment, a name or an email** (security rule 12); (5) one `OutboxMessage`: `MessageId` a new `Guid`, `Type` `leave.decided` (up to 60 characters), `Payload` JSON `{ requestId, employeeId, outcome }` with **ids only and no comment**, `CreatedAtUtc` now, `Attempts` 0; (6) save, commit. A refusal or a fault changes nothing. Do not add a migration. Verify: `dotnet build LeaveManagement.slnx` with 0 warnings and 0 errors; both `dotnet format` checks pass; read the code against `data-model.md` (the table of what a decision touches) and `.claude/rules/database-efcore.md`, and say that no test project exists.
- [X] T006 Extract the item building from `src/LeaveManagement.Application/Approvals/GetManagerInbox.cs` into one shared class `ApprovalItemBuilder.cs` in the same folder (balance before and after from `IBalanceReader`, team overlap from `TeamOverlap`, names from the reports), and make `GetManagerInbox` use it, **without changing what the inbox returns** (same fields, order, paging, balance and overlap). The detail use case (T007) will use the same class so the two screens cannot disagree (research R9). No new rules here. Verify: `dotnet build LeaveManagement.slnx` with 0 warnings and 0 errors; format checks pass; read both files and confirm in the report that the inbox output is unchanged (state how you checked; the quickstart of US-06 scenario A is the run check and is repeated in T009).
- [X] T007 Create the use cases in `src/LeaveManagement.Application/Approvals/` and register them as scoped in `src/LeaveManagement.Application/DependencyInjection.cs`. `GetApprovalDetail.cs`: `ExecuteAsync(int managerId, int requestId, ct)` reads the team through `IDirectReportReader` and the request through `IApprovalRequestReader.GetByIdAsync`; a missing request **or** an owner who is not a direct report returns null (the endpoint answers 404, the same for both, security rule 4); otherwise it returns the detail with `AllowedActions` of approve and reject **only while Pending** and `Balance` and `TeamOverlap` only while Pending. `DecideLeaveRequest.cs`: `ExecuteAsync(int managerId, int requestId, DecisionOutcome outcome, string? comment, ct)` in this order: `LeaveDecision` rule (too long, then comment required) first; then the team and the request as above (not found returns "not found"); then a status other than Pending returns `request.not_pending`; then the leave type through `ILeaveTypeReader` (has balance or not); then `ILeaveDecisionStore.DecideAsync`; then it returns the new detail. The caller is the signed-in manager, never taken from the body. Verify: `dotnet build LeaveManagement.slnx` with 0 warnings and 0 errors; format checks pass.

**Checkpoint**: the solution builds and every building block is in place. No endpoint calls them yet.

---

## Phase 3: User Story 1 - Reject a request with a comment (Priority: P1) MVP

**Goal**: a manager rejects a pending request of a direct report with a comment; without one it is blocked; the days come back and the dates are free (spec FR-002, FR-005, FR-006, FR-011, FR-018; research R3, R4, R5, R7).

**Independent Test**: reject a pending request with a comment: status Rejected, the owner's available is higher by the request's days, and the same dates can be applied for again; reject without a comment: 422 and nothing changes.

- [X] T008 [US1] In `src/LeaveManagement.Api/Approvals/ApprovalEndpoints.cs` add `POST /api/approvals/{requestId:int}/reject` with `.RequireAuthorization(Policies.RequireManager)`. The manager id is `int.TryParse(user.FindFirstValue("sub"))`; a failure is 401. Read the body yourself (as `LeaveRequestEndpoints` does, maximum 4096 bytes) so malformed JSON, a body over the limit, a non-text `comment` or a comment over **500 characters** is a clean 400 `ProblemDetails` that names the limit. Call `DecideLeaveRequest` with the Reject outcome. Map the results: refused comment 422 with code `comment.required`, not found 404 (no detail), `request.not_pending` 409 with its code, success 200 with the detail. Keep the endpoint thin; the endpoint accepts no person id, status or time. Verify: `dotnet build LeaveManagement.slnx` with 0 warnings and 0 errors; format checks pass.
- [ ] T009 [US1] Run quickstart scenarios A and B against the real API. **Ask the owner first** (B decides one request for good): name the request that will be rejected and the owner whose balance is checked. Start the API from one place only (`dotnet run --project src\LeaveManagement.Api`). A (writes nothing): reject with `{}` and with spaces only gives 422 `comment.required`; a 501 character comment and `not json` give 400; Employee token gives 403 (the approve and detail calls are added later, so check reject now); HR token 403; no token 401; the request is still Pending with the same balance. B: reject with a comment gives 200 `Rejected`; the owner's `available` is higher by exactly `daysCount` and `pending` lower by the same; the owner can apply again for the same dates (201). Say plainly that no browser and no .NET tests were used, that the decision cannot be undone, and list the exact request decided. Stop the API when done. Verify: the report lists each case with its result.

**Checkpoint**: User Story 1 works on the server. The days and dates come back after a rejection.

---

## Phase 4: User Story 2 - Approve a request, with an optional comment (Priority: P1)

**Goal**: a manager approves a pending request; nothing more is taken from the balance; the days move from pending to used (spec FR-003, FR-004, FR-007, FR-008, FR-011).

**Independent Test**: approve a pending request: status Approved, available unchanged, pending lower and used higher by the request's days; deciding it again gives 409.

- [ ] T010 [US2] In `src/LeaveManagement.Api/Approvals/ApprovalEndpoints.cs` add `POST /api/approvals/{requestId:int}/approve` with `.RequireAuthorization(Policies.RequireManager)`. The body is **optional**: no body or an empty body means no comment; otherwise the same body reading, size limit and 500 character rule as reject; a comment of only spaces counts as none. Call `DecideLeaveRequest` with the Approve outcome and map the results as in T008. Share the body-reading and result-mapping code with T008 rather than copying it. Verify: `dotnet build LeaveManagement.slnx` with 0 warnings and 0 errors; format checks pass.
- [ ] T011 [US2] Run quickstart scenarios C and D against the real API. **Ask the owner first** (they decide requests for good): name the requests. C: approve one request with no body (or a comment): 200 `Approved`; the owner's `available` is unchanged, `pending` lower and `used` higher by `daysCount`; the owner then applies for overlapping dates and gets 409 `leave.overlap`. D: deciding the same request again gives 409 `request.not_pending` and nothing changes; two decisions started together on one pending request (PowerShell `Start-Job`): exactly one 200 and one 409, one audit row and one outbox row for it (check with the read-only queries in T014). Say plainly that no browser and no .NET tests were used and list the exact requests decided. Stop the API when done. Verify: the report lists each case with its result.

**Checkpoint**: User Story 2 works. Approve and reject both work on the server.

---

## Phase 5: User Story 3 - Only the right manager can decide, and only once (Priority: P1)

**Goal**: only the owner's manager sees or decides a request; everyone else is refused or gets "not found" (spec FR-007 to FR-010; security rules 1, 3, 4, 7).

**Independent Test**: an Employee gets 403; a request that is not a direct report's, a request that does not exist and the manager's own request all give the same 404.

- [ ] T012 [US3] In `src/LeaveManagement.Api/Approvals/ApprovalEndpoints.cs` add `GET /api/approvals/{requestId:int}` with `.RequireAuthorization(Policies.RequireManager)` that calls `GetApprovalDetail` and answers 200 with the detail or **404 with no detail** when the use case returns null. The manager is the token's `sub` (401 if it is not a number). Verify: `dotnet build LeaveManagement.slnx` with 0 warnings and 0 errors; format checks pass.
- [ ] T013 [US3] Prove the resource rule. (a) Read `ApprovalEndpoints.cs`, `GetApprovalDetail.cs`, `DecideLeaveRequest.cs`, `LeaveDecisionStore.cs`, `DirectReportReader.cs` and `ApprovalRequestReader.cs` and confirm, point by point in the report: the manager id comes from `sub` only; the team is read from the database on every call; the caller is never in their own team; a missing request and a request of a non-report give the same 404; the decision cannot be sent for another person; no leave reason, email or outside person is in any response record. (b) Run what the seeded data allows against the API (read only): the detail of a request of Meera's report gives 200 with `allowedActions` while pending and `[]` once decided; a non-existing id gives 404; an Employee token gives 403 on all three calls; HR (no Manager role) gives 403; no token gives 401. (c) Report **NOT RUN** for "another manager's report gives 404" and for "Meera deciding her own request gives 404" unless the owner agrees to create the test data (a second manager with a report, or a pending request of Meera's own). Verify: the report states each point as confirmed from the code, run, or NOT RUN with the reason.

**Checkpoint**: the server rules are in place and proven as far as the seeded data allows.

---

## Phase 6: User Story 4 - The employee is told the outcome (Priority: P2)

**Goal**: every successful decision records exactly one notification row and one audit row, and every refusal records none (spec FR-011, FR-012, FR-018; research R7, R13). Delivery of the email is increment 4g.

**Independent Test**: after a decision, one `leave.decided` outbox row and one `leave.approve` or `leave.reject` audit row exist for the request, holding ids and the outcome only; after a refusal, none.

- [ ] T014 [US4] Check the rows with read-only queries (no writes). Use `sqlcmd -S "(localdb)\MSSQLLocalDB" -d LeaveManagement -E -Q "<query>"` (it is installed on this PC), or give the queries to the owner for SSMS if it fails. Queries: the outbox rows `SELECT Id, MessageId, Type, Payload, CreatedAtUtc, SentAtUtc FROM notify.OutboxMessage WHERE Type = 'leave.decided' ORDER BY Id DESC;` and the audit rows `SELECT TOP 10 Action, EntityType, EntityId, ActorEmployeeId, DetailJson FROM audit.AuditEvent WHERE Action IN ('leave.approve','leave.reject') ORDER BY Id DESC;`. Expected for each request decided in T009 and T011: exactly one outbox row with a payload of `requestId`, `employeeId` and `outcome` only; exactly one audit row; **neither holds the comment, a name or an email**; the refusal calls of T009 created none; the request with two simultaneous decisions has one of each; `SentAtUtc` is null (nothing delivers it yet). Also check the Restore ledger entry for a rejected request. If the owner did not allow T009 or T011, report NOT RUN. Verify: the report shows the rows seen (ids only) and the result of each expectation.

**Checkpoint**: the notification and audit records are proven; the email itself waits for 4g.

---

## Phase 7: User Story 5 - Open a request from the link and decide it (Priority: P2)

**Goal**: a manager who follows a link signs in if needed and lands on the request with Approve and Reject buttons; a request they may not see looks like one that does not exist (spec FR-013 to FR-017; research R12).

**Independent Test**: open `/approvals/{id}` signed out, sign in as the manager, and see that request with buttons; open someone else's request and see the not-found page.

- [ ] T015 [P] [US5] Extend the typed service `web/src/app/features/approvals/approvals.service.ts` (created in US-06 T007) and its spec: `getApproval(requestId: number)` calls `GET {API_URL}/api/approvals/{id}`; `approve(requestId: number, comment: string | null)` and `reject(requestId: number, comment: string)` call the two POSTs with a JSON body `{ comment }`. Types mirror `contracts/decision-api.md` exactly (`ApprovalDetail`, `DecisionInfo`, allowed actions as a string list); no `any`; the base URL comes from `api-config.ts`; the screen never calls `HttpClient`. Spec uses `HttpTestingController`: success path for each call (URL, method, body) and a failure path (a 404, a 409 and a 422 are passed on as errors). Verify: `npm test -- --watch=false` passes and `npm run format:check` is clean.
- [ ] T016 [US5] Add return-URL support. In `web/src/app/auth/auth.guard.ts` a signed-out visit goes to the sign-in page **keeping the wanted URL** (as a query parameter on the login URL tree), and in `web/src/app/features/login/login.ts` a successful sign-in navigates to that URL instead of always `/dashboard`. The wanted URL is accepted **only if** it starts with a single `/`, does not start with `//`, has no scheme (no `:` before the first `/`), and contains no backslash; anything else opens `/dashboard`. Put the check in one small, exported, pure function (for example `safeReturnUrl` in `web/src/app/auth/`) so it is tested on its own. The access token stays in memory only; nothing is written to `localStorage`, `sessionStorage` or a cookie (security rule 7). Specs: the guard keeps the URL; login returns to a safe URL; and these return values go to the dashboard: `//example.com`, `https://example.com`, `/\evil`, an empty value, a value with `javascript:`. Verify: `npm test -- --watch=false` passes, `npm run format:check` is clean and `npm run build` succeeds.
- [ ] T017 [US5] Make the child route work. In `web/src/app/auth/role-guard.ts` let a route name the navigation entry it belongs to (for example `data: { navPath: 'approvals' }`), falling back to its own path as today, and add the route `approvals/:requestId` in `web/src/app/app.routes.ts` with `canActivate: [authGuard, roleGuard]`, `data: { navPath: 'approvals', title: 'Request' }` and a lazy load of the new page component (T018). Update `role-guard.spec.ts` and `app.routes.spec.ts`: a Manager may open `approvals/42`, an Employee is sent to the not-found view, a signed-out visit goes to sign-in with the wanted URL. The guard stays a screen convenience; the API enforces the policy (security rule 1). Verify: `npm test -- --watch=false` passes, `npm run format:check` is clean and `npm run build` succeeds.
- [ ] T018 [US5] Create the request page `web/src/app/features/approvals/approval-detail.ts`, `approval-detail.html`, `approval-detail.scss` and `approval-detail.spec.ts`. Standalone component, `inject()`, signals, new control flow, no `innerHTML`, no `any`. It reads `requestId` from the route and loads the detail through the service. States from `contracts/decision-api.md`: loading ("Loading request..." in a `role="status"` region); a 404 sends the person to the existing not-found view (`NOT_FOUND_PATH`) so a hidden request looks like an unknown one; other errors show the message with Try again (`role="alert"`). Content: name, leave type, dates (First half or Second half for a half day), days, balance "Before x, after y" and the overlap text from the US-06 screen (only while pending). Buttons exist **only when `allowedActions` contains them**: Approve (one click, sends no comment) and Reject. Reject opens a labelled comment box (`maxlength` 500, required, plain text) and a Send rejection button; an empty or spaces-only comment shows "Please write a comment" and **sends nothing**. After a decision the page shows the server's new detail (status, date, the comment as plain text) with no buttons; a refusal (409, 422) shows the server's message and the buttons only if the request is still pending. The comment is shown with `{{ }}` interpolation only. No decider name is shown. Spec (Vitest, mocked service): pending shows both buttons; decided shows the outcome and no buttons; reject without a comment sends nothing; reject with a comment calls the service with it; approve calls the service; a 409 and a 422 show the message; a 404 goes to not-found. Verify: `npm test -- --watch=false` passes, `npm run format:check` is clean and `npm run build` succeeds.
- [ ] T019 [US5] Run quickstart scenario G if a browser can be driven (the API and `npm start` both running, **ask the owner first** because a decision changes data): signed out, open `http://localhost:4200/approvals/{id}` for a pending request of Meera's report, sign in as Meera, expect to land on it with both buttons; Reject with an empty comment sends nothing; with a comment shows Rejected and no buttons; a decided request shows the outcome and no buttons; someone else's request and an unknown id show the same not-found page; a sign-in return address of `//example.com` opens the dashboard. If a browser cannot be driven, report NOT RUN and say these were covered only by the Vitest specs. Verify: the report lists each step with its result or NOT RUN.

**Checkpoint**: all five stories are in place, with a plain statement of what was and was not run.

---

## Phase 8: Polish and finish

- [ ] T020 Run `/ci-check` (build, standards, tests, repo guard, packages). Verify: every step is PASS or an honest NOT RUN that CI covers (the missing .NET test project is expected).
- [ ] T021 Run the finish steps from `CLAUDE.md`: `/review` (code-reviewer and security-reviewer; the security-reviewer checks rules 1, 3, 4, 7, 10, 12, 13 and 14 against the three endpoints, the store, the audit and outbox rows, the return-URL check and the comment display), then `/learn us07-approve-reject` for `ai/learning/us07-approve-reject.md`, then `/status` to update `ai/specs/INDEX.md`. Do not write the owner's teach-back answers or exercise. Verify: review findings are fixed or listed, the notes file exists, and the INDEX row is updated.

---

## Dependencies and order

```text
T001 -> (T002, T003 in any order) -> T004 -> T005 -> T006 -> T007
     -> T008 -> T009 -> T010 -> T011 -> T012 -> T013 -> T014
     -> (T015 in any order with T016) -> T017 -> T018 -> T019 -> T020 -> T021
```

- T002 and T003 are marked [P] (different projects, no shared file). T004 needs T003 (the reader port); T005 needs T002 and T003; T006 needs US-06 T010; T007 needs T003 to T006.
- T008, T010 and T012 edit the same file, so they run in order.
- T015 is marked [P] (a service file) but T018 uses it, so run T015 first. T016 and T015 touch different files. T017 needs T018's component to exist: create the route with the lazy import in T017 and the component in T018 in the same sitting, or build T018 first and tick T017 after; say which in the report.
- T009, T011 and T014 change real data and need the owner's OK.
- T014 needs T009 and T011 to have decided something; otherwise it reports NOT RUN.

## Requirement coverage

| Requirement | Task |
| --- | --- |
| FR-001 manager decides a report's pending request | T005, T007, T008, T010, T018 |
| FR-002 rejection needs a comment (500 characters, no blanks) | T002, T003, T007, T008, T009 |
| FR-003 approval comment optional (500 characters) | T002, T010, T011 |
| FR-004 approve takes no more days; pending to used | T005, T010, T011 |
| FR-005 reject restores days and releases dates | T005, T008, T009 |
| FR-006 Unpaid: no balance change | T005 (code), T013 (NOT RUN unless data is created) |
| FR-007 only pending can be decided | T005, T007, T011 |
| FR-008 two at once: one wins | T005, T011 (scenario D) |
| FR-009 only the owner's manager; 404 for others | T007, T012, T013 |
| FR-010 relationship looked up each time; decider from sign-in | T007, T013 |
| FR-011 one audit row per decision, ids only | T005, T014 |
| FR-012 notification recorded, ids only, none on refusal | T005, T014 |
| FR-013 link carries no secret | T016, T018 |
| FR-014 open from link or inbox; hidden looks unknown | T012, T017, T018, T019 |
| FR-015 reject asks for the comment first | T018 |
| FR-016 new status or reason shown; accessible buttons | T018 |
| FR-017 comment shown as plain text | T018 |
| FR-018 one transaction, named store | T005 (read against the plan), T011, T014 |
| SC-001 reject without comment refused 100% | T009 |
| SC-002 reject restores days and frees dates | T009 |
| SC-003 approve keeps available, pending to used | T011 |
| SC-004 non-owners refused, no existence leak | T009, T013 |
| SC-005 two at once: exactly one | T011 |
| SC-006 one audit and one notification per success, none per refusal | T014 |
| SC-007 open from link and decide in under 1 minute | T019 (state whether it was timed) |

## Notes

- If a step finds a gap in the spec or plan, stop, fix the spec and plan, and get approval again (constitution VI).
- Not verifiable here: how the screen looks in a browser and with a screen reader, another manager's reports (needs test data), Unpaid decisions (needs a request), real email delivery (increment 4g), the employee's view of the comment (increment 4d), and automated back-end tests (Phase 5).
- Open from earlier work, not part of this story: the weekend or holiday half-day message (`halfday.invalid` or `days.none`) from US-04, and the rest of US-06 (tasks T007 to T015 there).
