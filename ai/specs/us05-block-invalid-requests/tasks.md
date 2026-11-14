# Tasks: Block requests that break my balance or overlap

**Input**: Design documents from `ai/specs/us05-block-invalid-requests/` (Jira SCRUM-12)

**Prerequisites**: spec.md, plan.md, research.md, data-model.md, contracts/overlap-refusal.md, quickstart.md. Revised after `/speckit-analyze` (findings I1, A1, T1, C1, C2, C3, A2, U1, U2, P1). **Owner approval of these tasks is needed** before T001 starts.

**Tests**: There is no .NET test project (Phase 5), so back-end checks are the manual steps in `quickstart.md`, and every report must say so. The message builder is a pure function so unit tests can be added in Phase 5. The web screen already has a Vitest case for the overlap refusal; T004 extends it because `.claude/rules/frontend-angular.md` asks for a success path and a failure path.

**Organization**: grouped by user story. Do one task at a time, in order. Tick a box only after its verify step passes. Commands work in Command Prompt or PowerShell; run `npm` commands in `web`.

**Owner decisions needed before the run tasks**: T005 (scenarios B, C and F) and T007 (scenario D) create pending requests in the owner's LocalDB, and there is no cancel or delete until 4e. Ask first, name the user and the dates, and report exactly what was created.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: can run in parallel (different files, no dependency on an unfinished task)
- **[Story]**: US1 or US2 (from spec.md)

## Phase 1: Setup

- [X] T001 Confirm the baseline is green before any change. Verify: `dotnet build LeaveManagement.slnx` succeeds with 0 warnings and 0 errors (warnings are errors); in `web`, `npm test -- --watch=false`, `npm run format:check` and `npm run build` succeed. Record any failure that already exists, so it is not blamed on this story.

---

## Phase 2: Foundational

None. The story reuses the existing store, the existing refusal code `leave.overlap` and the existing screen. No new shape, port or table is needed.

---

## Phase 3: User Story 1 - Be told which dates clash (Priority: P1) MVP

**Goal**: the overlap refusal names the clashing dates (spec FR-003, FR-004, FR-006, FR-009, FR-010; research R2, R3, R4, R6).

**Independent Test**: hold a pending request for Tue 1 Dec to Wed 2 Dec, apply for Mon 30 Nov to Wed 2 Dec, and see 409 `leave.overlap` naming `2026-12-01` and `2026-12-02`, not `2026-11-30`.

- [X] T002 [US1] Make the overlap refusal name the dates, in two files changed together so the build is never left broken. **(a)** `src/LeaveManagement.Application/Leave/LeaveRuleError.cs`: change `LeaveRuleErrors.Overlap()` to `Overlap(IEnumerable<DateOnly> clashingDates)`, a **pure function** (no clock, no database). Rules from `contracts/overlap-refusal.md`: take the distinct dates, sort ascending, format each as ISO `yyyy-MM-dd` with `CultureInfo.InvariantCulture`, join with `, `; show at most 10 and, when there are more, put ` and N more` after the tenth date (N = total minus 10); the full text is `You already have a pending or approved leave request on {dates}.` with the full stop last (an 11-date example is in the contract); for an **empty** list return the old text `You already have a pending or approved leave request on one or more of these days.`. Keep the code `LeaveRuleCodes.Overlap` (`leave.overlap`). Do not add a field to `LeaveRuleError`. Domain stays untouched. **(b)** `src/LeaveManagement.Infrastructure/Persistence/LeaveApplicationStore.cs`: give both `LeaveRuleErrors.Overlap()` call sites the clashing dates. Pre-check in `RunAsync`: the code already loads `taken` for the range; keep the rows where `wanted.Contains((LeaveDate, Slot))`, take their distinct `LeaveDate` values and pass them to `Overlap(dates)`. The query stays filtered by `EmployeeId == a.EmployeeId` and the range, so only the caller's own dates and only dates inside the request are named (security rules 4 and 12). Race path after `RollbackAsync` in the unique-violation `catch`: after the existing `FindSavedAnswerAsync` check returns null, re-read the caller's taken day rows in `[StartDate, EndDate]` with `AsNoTracking`, keep those matching a wanted `(date, slot)`, and pass the dates to `Overlap(dates)`; if the re-read finds none, pass an empty list (old wording). Do not change the transaction, the unique index, the balance update, the audit row or the idempotency handling (FR-010). Use `DateOnly` throughout; a small private helper may serve both sites. Verify: `dotnet build LeaveManagement.slnx` succeeds with 0 warnings and 0 errors; `dotnet format style LeaveManagement.slnx --verify-no-changes --severity warn --no-restore` and the `analyzers` variant pass; read the builder against the contract's message table (one date, several, 10, 11, empty list) and say in the report that no test project exists.
- [X] T003 [P] [US1] Update the contract text in `ai/specs/us03-apply-leave-working-days/contracts/leave-requests-api.md`: change the refusal-message row for `leave.overlap` to `You already have a pending or approved leave request on {dates}.`, and add one line pointing to `ai/specs/us05-block-invalid-requests/contracts/overlap-refusal.md` for the date rules. Change nothing else in that file. (This edits the contract of a spec marked Done; it is a tracked doc change covered by the owner's approval of these tasks.) Verify: the row reads as above and the two contract files agree.
- [X] T004 [US1] In `web/src/app/features/apply-leave/apply-leave.spec.ts`, add one case next to the existing `shows the refusal for %s in an alert` table: a 409 `leave.overlap` refusal whose `detail` is `You already have a pending or approved leave request on 2026-12-01, 2026-12-02.`; after a submit, check the alert text contains `2026-12-01` and `2026-12-02` and that nothing is saved (`.saved` is null). Also add the failure edge: the same 409 with **no** `detail` shows the screen's fallback wording for `leave.overlap` (the text already in `REFUSAL_MESSAGES`). Do not change `apply-leave.ts` or `apply-leave.html`. Verify: `npm test -- --watch=false` passes and `npm run format:check` is clean.
- [X] T005 [US1] Run quickstart scenarios B, C, E and F against the real API and report each as PASS, FAIL or NOT RUN. **Ask the owner first**: B creates one pending request (E1, Tue 2026-12-01 to Wed 2026-12-02), C a second (E2, Fri 2026-12-04) and F a third (F1, Mon 2026-12-07 to Tue 2026-12-08) in LocalDB, with no way to remove them yet. Start the API from one place only (`dotnet run --project src\LeaveManagement.Api`). Read the balance first and use a dev user with at least 7 Annual days available (name the user and the days taken). Expected: the five range shapes in the quickstart (start clash, end clash, single day, a new range that contains E1, and a range that clashes with E1 and E2) each name exactly the dates in the table, never a date outside the range, in date order; balances and request counts are unchanged by each refusal. F (two submits at once): different keys give one 201 and one 409, same key gives the same 201 and one request. E: run only if a browser can be driven, else NOT RUN (covered by the Vitest case). Say plainly that the **approved** case cannot be created (4e not built) and that FR-005 (rejected and cancelled never clash) was not run, and that no .NET tests were used. Stop the API when done. Verify: the report lists each scenario and shape with its result and the exact requests created.

**Checkpoint**: User Story 1 works on the server and the screen shows the text. The approved-request case and FR-005 remain untested until 4e.

---

## Phase 4: User Story 2 - Be told the balance is too low, with nothing saved (Priority: P1)

**Goal**: prove the existing balance block end to end (spec FR-001, FR-002, FR-007, FR-008). No code change is expected.

**Independent Test**: as a person with N Annual days available, apply for N + 1 working days; see 422 `balance.insufficient`, nothing saved, balance unchanged.

- [ ] T006 [US2] Run quickstart scenario A against the real API (it writes nothing) and report PASS, FAIL or NOT RUN: read the Annual balance, apply for more working days than are available with a fresh `Idempotency-Key`, expect 422 `balance.insufficient` with `You do not have enough balance for these days.`, then read the balance again and confirm `available`, `pending` and `used` are unchanged. Also run the rule-order check (step 6 of scenario A: a range that clashes **and** exceeds the balance names the overlap first) using E1 from T005; if T005 was not run, report it NOT RUN with the reason. The exact Jira example (2 available, ask for 3) needs a user with exactly 2 Annual days: if no seeded user has that, say so and report the closest case. If a gap is found in the server, stop, fix the spec and plan, and get approval again (constitution VI). Verify: the report lists each case with its result.
- [ ] T007 [US2] **Only if the owner agrees** (it creates a pending request that takes the balance to 0): run quickstart scenario D, a request using exactly the available days is accepted (FR-002). Also check that Unpaid skips the balance check if a safe case exists. Otherwise report both NOT RUN. Verify: the report states what ran, what was created, and what was skipped.

**Checkpoint**: both Jira criteria are checked, with a plain statement of what was and was not run.

---

## Phase 5: Polish and finish

- [ ] T008 Run `/ci-check` (build, standards, tests, repo guard, packages). Verify: every step is PASS or an honest NOT RUN that CI covers (the missing .NET test project is expected).
- [ ] T009 Run the finish steps from `CLAUDE.md`: `/review` (code-reviewer and security-reviewer: the change reads the caller's own rows and puts dates in an error message, so security rules 4, 10 and 12 must be checked; also tell the reviewers that T003 edits a Done spec's contract), then `/learn us05-block-invalid-requests` for `ai/learning/us05-block-invalid-requests.md`, then `/status` to update `ai/specs/INDEX.md`. Do not write the owner's teach-back answers or exercise. Verify: review findings are fixed or listed, the notes file exists, and the INDEX row is updated.

---

## Dependencies and order

```text
T001 -> T002 -> T003 -> T004 -> T005 -> T006 -> T007 -> T008 -> T009
```

T003 is the only task marked [P] (a docs file nothing else touches), but keep the order above so each tick follows a passing verify. T005 needs T002. T006 uses E1 from T005 for the rule-order check.

## Requirement coverage

| Requirement | Task |
| --- | --- |
| FR-001, FR-002 | T006, T007 (existing behaviour, checked; FR-002 only if the owner agrees) |
| FR-003 | T005 (existing behaviour, checked) |
| FR-004 | T002, T005 |
| FR-005 | **Deferred to 4e** (hand-off recorded in `ai/specs/INDEX.md`); not tested here, stated in T005 |
| FR-006 | T004 (screen shows the server text), T005 |
| FR-007, FR-008 | T005, T006 (FR-008 needs E1) |
| FR-009 | T002 (own rows and range only), T009 (security review) |
| FR-010 | T002 (no change to the transaction), T005 scenario F (only if the owner agrees, else NOT RUN) |
| SC-001 | T006 |
| SC-002 | T005 (five range shapes) |
| SC-003 | T005, T006 |
| SC-004 | T005 scenario F (only if the owner agrees, else NOT RUN) |
| SC-005 | T005, T006 |

## Notes

- The message-only choice (no `clashingDates` field) is recorded in `research.md` R4. If the owner wants the field, update the spec and plan first.
- Not verifiable here: how the screen looks in a browser, any approved-request clash and FR-005 (4e), and automated back-end tests (Phase 5).
- Open from earlier work, not part of this story: the weekend or holiday half-day message (`halfday.invalid` or `days.none`) from US-04.

## T005 result (2026-11-14, API run with `dotnet run`, dev user Sneha Iyer id 6, Annual, 12.0 available before)

| Scenario | Result | What was run |
| --- | --- | --- |
| B, five shapes of overlap | PASS (four shapes plus the two-request case in C) | Created E1 (id 35, Tue 2026-12-01 to Wed 2026-12-02, 201). Start clash Mon 11-30 to Tue 12-01 named `2026-12-01`. End clash Wed 12-02 to Thu 12-03 named `2026-12-02`. Single day Wed 12-02 named `2026-12-02`. Range containing E1, Mon 11-30 to Wed 12-02, named `2026-12-01, 2026-12-02`, not 11-30. All 409 `leave.overlap`; balance unchanged (pending 2.0, available 10.0). |
| C, two requests | PASS | Created E2 (id 36, Fri 2026-12-04, 201). Mon 11-30 to Fri 12-04 named `2026-12-01, 2026-12-02, 2026-12-04` in order (409). |
| A, rule order and balance | PASS | A range that clashes and needs 11 working days with 9 available named the overlap first (409). A range with no clash needing 11 days (Dec 9 to Dec 23) gave 422 `balance.insufficient`, `You do not have enough balance for these days.` Balance unchanged. |
| F1, different keys at once | PASS | Dec 7 to Dec 8 sent twice together with different keys: one 201 (id 38), one 409 naming `2026-12-07, 2026-12-08`. One request only. |
| F2, same key at once | PASS | Dec 10 sent twice together with the same key: both calls returned the same 201 (id 40). One request only. |
| E, the screen | NOT RUN | No browser driven. Covered only by the Vitest cases (T004). |
| Approved request, FR-005 | NOT RUN | Approve, reject and cancel are 4e. Not built. |

**Requests created in the owner's LocalDB (all pending, user Sneha Iyer, Annual):** id 35 (2026-12-01 to 12-02, 2 days), id 36 (2026-12-04, 1 day), id 38 (2026-12-07 to 12-08, 2 days), id 40 (2026-12-10, 1 day). Annual for this user went from 12.0 available to 6.0 available, 6.0 pending. Ids 37 and 39 are gaps from the rolled-back losers of each race (normal for identity columns). Request 40 is from the same-key check (F2) and was not named in the earlier plan to the owner.

Not verified: no .NET tests exist; the screen was not seen in a browser.
