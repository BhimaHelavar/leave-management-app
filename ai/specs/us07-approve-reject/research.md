# Research: Approve or reject a request with a comment

No `NEEDS CLARIFICATION` items were left in the spec (FR-012 was answered: record the notification only). The questions below are design choices checked against the existing code and documents.

## R1. What does approving do to the balance

- **Decision**: Nothing. Approve changes only the request (status, who decided, when, comment). The days stay held; they move from "pending" to "used" because the dashboard derives pending from pending requests and used from the ledger (`BalanceSummary.Derive`: used = held minus pending).
- **Rationale**: The accepted policy facts say days are deducted at submit and restored on reject or cancel. The Jira wording "balance is deducted" on approval is read as "the days stay counted". Deducting again would take the days twice.
- **Alternatives considered**: Defer the deduction to approval (Jira's literal reading) — contradicts US-03 and US-05, which already deduct at submit and block on it, and would allow over-booking while requests are pending. Rejected. Owner confirmation is requested in the plan report.

## R2. One store for a decision, and why it is allowed

- **Decision**: `ILeaveDecisionStore` (port in Application) and `LeaveDecisionStore` (Infrastructure/Persistence) run the whole decision in one database transaction.
- **Rationale**: Principle III (1.2.0) allows one Application-level store to write across modules when a rule needs that atomicity, if the spec names it. A decision, its restored days, its audit row and its notification must succeed or fail together (security rule 14; transactional outbox). The spec names it in FR-018. It follows the shape of `LeaveApplicationStore`: execution strategy, explicit transaction, ledger entry, `IAuditWriter` in the same context.
- **Alternatives considered**: Several modules each saving separately, with compensation if one fails — more code, and a crash between steps leaves a decided request with the days not restored. Rejected. A domain-event dispatcher after commit — loses the notification if the process dies after the commit (the outbox exists precisely to avoid that).

## R3. Making sure only one decision wins

- **Decision**: A **guarded update**: `UPDATE leave.LeaveRequest SET Status = ..., DecidedByEmployeeId = ..., DecidedAtUtc = ..., DecisionComment = ... WHERE Id = @id AND Status = 'Pending'`. Zero rows changed means someone decided first: roll back and answer 409 `request.not_pending`. EF's `ExecuteUpdateAsync` is used; the database sets the new `RowVersion`.
- **Rationale**: The status condition inside the same statement is the lock, so there is no read-then-write window. It also covers the future employee cancel (a status change) without a version header.
- **Alternatives considered**: Load, change, `SaveChanges` and catch `DbUpdateConcurrencyException` — works, but loads the row and needs a retry path for the stale tracked entity. `If-Match` with an ETag (Phase 3d ADR 23): protects a manager who loaded an old view, but a pending request cannot be edited by anyone, so the only change is the status itself, which the guard already covers. **This deviates from 3d, which asks for `If-Match` on decisions.** Recorded here and in the plan report; adding the ETag later is a small change if requests become editable.

## R4. Rejecting: release the dates

- **Decision**: In the same transaction, delete the request's `LeaveRequestDay` rows.
- **Rationale**: The unique index on `(EmployeeId, LeaveDate, Slot)` blocks overlap whatever the status; rule 13 blocks only pending or approved requests. Releasing the rows on reject fulfils the hand-off recorded in US-03 research R6 and US-05 FR-005, so a rejected request never clashes.
- **Alternatives considered**: Keep the rows and filter by status in the overlap query — the unique index would still refuse the new request, so the index would have to change (a migration). Rejected.

## R5. Rejecting: restore the days

- **Decision**: If the leave type has a balance: one atomic update `Available = Available + @days` on the request's person, type and leave year (`StartDate.Year`), plus a ledger entry `Restore` with `+days` and the request id. Types with no balance (Unpaid) skip both. `days` is the request's saved `DaysCount`.
- **Rationale**: Database rules: one guarded atomic update plus a ledger entry; never read, add and write back. The saved snapshot is used so later holiday changes never rewrite history. The ledger entry type `Restore` already exists and is what the dashboard's "held" figure expects (`held = -(Deduction + Restore)`).
- **Whether the balance row exists**: A type with a balance always has one after a successful apply. If zero rows change anyway, the store fails and rolls back (a data problem, answered as a server error, never a silent loss of days).
- **Alternatives considered**: Re-derive the days from the day rows — equal today, but the snapshot is the agreed source. Not done.

## R6. Who may decide, and what the caller sees otherwise

- **Decision**: Role policy `RequireManager` first (403 for anyone else, the same answer for all). Then the use case reads the team from the database and the request by id: if the request does not exist **or** its owner is not one of the caller's direct reports, the answer is **404** `not_found`, the same for both. The caller is never in their own team, so their own request is also a 404.
- **Rationale**: Security rules 3 and 4: roles alone are not enough, and 404 stops id probing. Spec scenario "a manager never decides their own request" is met without a special case, and "refused" is a 404 here, which the contract states.
- **Alternatives considered**: 403 for "not your report" — confirms the id exists. A separate "own request" check with its own message — reveals nothing useful and adds a branch.

## R7. The comment and personal data (a refinement of the spec made during planning)

- **Decision**: The manager's comment is stored **only** on the request (`DecisionComment`, up to 500 characters). The audit row and the outbox row hold ids, the outcome, the day count and the leave type id, never the comment, a name or an email. The spec was edited to say the notification refers to the request and the email builder (4g) reads the comment from it.
- **Rationale**: Security rule 12: audit and outbox rows hold ids and values, never names, emails or free-text reasons. The first draft of the spec said the notification "carries the comment", which would have broken that rule.
- **Alternatives considered**: Copy the comment into the outbox payload — simple but breaks rule 12. A separate encrypted store for comments — nothing to protect beyond what the request row already holds.

## R8. Naming the store in the spec

- **Decision**: The spec now names "the leave decision store" (FR-018), because constitution Principle III 1.2.0 requires the spec to name each cross-module store.

## R9. The item shape and the dependency on US-06

- **Decision**: The request page returns the same item the inbox shows (person, leave type, dates, days, balance before and after, team overlap) plus `status`, a `decision` block and `allowedActions`. `balance` and `teamOverlap` are present only while the request is pending and are null once decided. One shared item builder (extracted from `GetManagerInbox`) serves both, so the two screens cannot disagree.
- **Dependency**: At the time of this plan, US-06 is merged with only T001 to T006 (back end of the list, overlap still the interim value, no screen). The overlap (T010) and the inbox screen (T008) are open. US-07 should start after them, or its first task must finish the shared builder and the overlap itself and say so.
- **Alternatives considered**: A second, smaller detail shape — two sources for the same numbers. Rejected.
- **Comment visibility**: The decision comment is shown to the manager who decided. There is no employee-facing view of it yet: "My requests" is increment 4d. This is a stated limit, not a gap in the decision.

## R10. Errors and codes

- **Decision**: Reuse the project's dotted codes in `ProblemDetails` with `code`, as US-03 does: `comment.required` (422, rejecting without a usable comment), `request.not_pending` (409), `not_found` style 404 with no detail, 403 from the policy, 401 as elsewhere, 400 for bad JSON or a comment over 500 characters (a shape problem, like the apply reason).
- **Rationale**: The screen already switches on the code, and the order of checks (shape, then comment, then ownership, then status) keeps the answers predictable.
- **Alternatives considered**: 3d's snake_case codes — the project already chose dotted codes in US-03; staying consistent matters more.

## R11. Idempotency of the decision call

- **Decision**: No `Idempotency-Key`. A repeat of the same decision returns 409 `request.not_pending`.
- **Rationale**: The state check makes a repeat harmless, which is the spec's FR-007 and FR-008. An idempotency table would replay the first answer, which hides the fact that nothing happened the second time.
- **Alternatives considered**: Require the key like apply — more machinery for a call that cannot do harm twice.

## R12. The request page and the sign-in return

- **Decision**: Route `approvals/:requestId` shows the request and, only when `allowedActions` has them, Approve and Reject buttons. A signed-out visit goes to sign-in and comes back: `authGuard` keeps the wanted URL, `Login` navigates to it after success. The returned URL is accepted **only** if it starts with a single `/` and not `//`, has no scheme, and contains no backslash; anything else goes to the dashboard. The role guard maps `approvals/:requestId` to the existing `approvals` navigation entry so a Manager may open it and others see the not-found page.
- **Rationale**: The deep link was the point of the story's role-play. An unchecked return URL is an open-redirect hole, so it is validated. The role guard already decides by navigation path, so a child path needs a mapping, not a second table.
- **Alternatives considered**: A `returnUrl` query parameter on the login URL — works but the guard's `UrlTree` and a validated parameter are about the same code; the token stays in memory only either way (no storage of the URL beyond the navigation). A cookie or storage for the wanted URL — security rule 7 and the front-end rules forbid storing session material; the URL itself is harmless but adds state to clean up.

## R13. Link and email

- **Decision**: The link is `{web base URL}/approvals/{requestId}`. Building the email body and delivering it is 4g. This story records only the outbox row `leave.decided` with `{ requestId, employeeId, outcome }`.
- **Alternatives considered**: Send now — owner declined (FR-012).

## R14. Testing without a .NET test project

- **Decision**: Keep `LeaveDecision` (the rule) and the message builders pure so Phase 5 can unit test them (comment missing, spaces only, 500 and 501 characters, approve with and without comment, reject of an Unpaid request). Verify now with the manual calls in the quickstart. Cover the web code with Vitest. State plainly that no .NET tests exist.
- **Alternatives considered**: A test project now — Phase 5.
