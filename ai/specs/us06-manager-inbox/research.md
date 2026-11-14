# Research: Review pending requests in a manager inbox

No `NEEDS CLARIFICATION` items were left in the spec (FR-006 was answered: the team-away count includes the requester). The questions below are design choices checked against the existing code.

## R1. One query across three modules, or three ports

- **Decision**: Three read ports, one per module, composed in an Application use case: people (`IDirectReportReader`), leave (`IApprovalRequestReader`) and the existing balances port (`IBalanceReader`).
- **Rationale**: Constitution III says code in one module does not read another module's tables. Its one exception allows a single named store to **write** across modules in one transaction; it does not cover reads. `GetMyBalances` already composes two ports the same way (`IBalanceReader` and `IPendingLeaveReader`).
- **Alternatives considered**: One Infrastructure query joining `people`, `leave` and `balances` — fewer round trips, but it breaks the module rule and would need a constitution amendment for a read model. Rejected. A database view — same objection, and it hides logic from the code review.

## R2. Route and name

- **Decision**: `GET /api/approvals?page=&pageSize=`, policy `RequireManager`.
- **Rationale**: Matches the Phase 3d name (`GET /approvals?status=Pending&page=`) with the same `/api` prefix and no `/v1` the other endpoints already use. The `status` filter is dropped: this story lists only pending requests, and the same route can grow a filter in 4e if needed.
- **Alternatives considered**: `/api/me/approvals` — `me` means "my own record", and this is a team list. `/api/manager/inbox` — a new naming scheme for one screen.

## R3. Who the "team" is

- **Decision**: The active people whose `ManagerId` is the caller. The caller is never in their own list, even if their `ManagerId` points at themself. Inactive people are not on the team. Team size is the count of that list, so the requester is counted.
- **Rationale**: The spec's FR-007 and FR-009 (looked up every call, never from the token) and the owner's FR-006 decision (requester counts).
- **Alternatives considered**: The whole management chain (a manager's manager) — out of scope, nothing in the story asks for it. A cached team list — a changed reporting line must apply at once, and the data is tiny.

## R4. Balance and overlap figures without a join or an N+1 explosion

- **Decision**: Balance: call the existing `IBalanceReader.GetLedgerAsync` once per distinct person and leave year found on the page (a page has at most 100 items, a team is small). Overlap: one query for the team's held day rows (`LeaveRequestDay`: request id, person, date, slot) inside the date window of the page (earliest start to latest end), then compute per item in memory with the pure `TeamOverlap` rule.
- **Rationale**: Reuses a reviewed port, keeps modules apart, and a manager has a handful of reports. A day row exists only for a pending or approved request (rejected and cancelled requests release theirs, a duty of 4e), so the held rows are exactly "pending or approved".
- **Alternatives considered**: A new batch method on `IBalanceReader` taking many people — better for big teams; add it when a team outgrows a few dozen people. One overlap query per request — N queries for no gain.

## R5. What "balance after approval" shows

- **Decision**: Two numbers per item, both from stored data: **after** = the requester's available days for that type and leave year now; **before** = after plus this request's days. For a type with no balance (Unpaid) the balance is omitted.
- **Rationale**: Days are deducted at submit and restored on reject or cancel (product facts), so approving changes nothing and "after approval" equals the available balance now. Showing before and after matches the sketch "8.0 -> 5.0".
- **Alternatives considered**: Show only `available` — loses the sketch's before figure. Recompute from the ledger — the ledger is the source of `available` already, and a second derivation could disagree.
- **Edge**: A person with several pending requests of one type gets the same `after` on each item (it is net of all of them) and a `before` that is that item's days higher. This is stated in the contract.

## R6. Overlap rule details

- **Decision**: For a request R of person P: R's own slots are the held rows with `RequestId = R.Id`. Another report Q overlaps when Q has a held row with the same `(date, slot)` as one of R's slots. The item lists Q's name and the shared dates (distinct, sorted). `awayIfApproved` = 1 + the number of distinct such Q. `teamSize` = the active reports.
- **Rationale**: Spec edge cases: a first half does not overlap a second half on the same date, a half day overlaps a full day, and weekends never count because day rows exist only for working days.
- **Alternatives considered**: Compare date ranges — would count weekends and holidays inside both ranges as overlap. Compare dates only — would flag a first half against a second half.

## R7. Paging and sort

- **Decision**: Offset paging, default 20, maximum 100, as the API conventions say. Order: `StartDate`, then `SubmittedAtUtc`, then `Id`, all ascending, in the database. The response has `items`, `page`, `pageSize` and `totalCount`. A bad `page` or `pageSize` is a 400.
- **Rationale**: A stable total order makes pages repeatable (spec scenario 3 of story 1) and a past start date naturally sorts first.
- **Alternatives considered**: Keyset paging — better for large, fast-changing lists; not needed for a team.

## R8. Refusals and what an outsider learns

- **Decision**: A caller without the Manager role gets 403 from the role policy, the same for everyone, so it reveals nothing about pending requests. No token gets 401. A manager with no reports gets an empty page, not an error. There is no id in the URL, so there is nothing to probe and the 404 rule for hidden records does not arise here.
- **Rationale**: Security rules 2, 3 and 4. The resource rule is applied inside the query: the list is built from the caller's own team only.
- **Alternatives considered**: 404 for a non-manager — hides that the endpoint exists, but a role refusal is the project's convention for role policies (constitution II, security rule 3).

## R9. What the response must not carry

- **Decision**: No leave reason, no email, no balance of anyone except the requester, no person outside the team. Person ids are included (needed by 4e to act on a request), together with names, for the manager's own reports only.
- **Rationale**: Security rule 12 and spec FR-011.

## R10. Front end

- **Decision**: A new `approvals` feature folder with a typed service and a screen using signals. The nav item and its Manager role already exist; `loaderFor('approvals')` in `app.routes.ts` swaps the placeholder for the new screen. Paging uses Previous and Next buttons driven by `totalCount`. The overlap text is words ("Also away: Meena S. (2026-12-02). 2 of 3 away if approved."), not colour.
- **Rationale**: Matches the apply screen's pattern (service plus component, loading, error with retry). The role guard stays a convenience; the API enforces the policy.
- **Alternatives considered**: A table with a drawer (as in the sketch for My requests) — heavier than a list of cards and not needed to decide quickly.

## R11. Testing without a .NET test project

- **Decision**: Keep `TeamOverlap` and the response shaping pure so Phase 5 can unit test them (no overlap, partial overlap, first half against second half, approved plus pending, one report, a report with no one else). Verify now with the manual calls in the quickstart (including the Jira examples) and cover the screen with Vitest. State plainly that no .NET tests exist.
- **Alternatives considered**: Add a test project now — rejected, that is Phase 5 and outside this story.
