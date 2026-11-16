# Implementation Plan: Approve or reject a request with a comment

**Branch**: `SCRUM-14-US-07-Approve-or-reject-a-request-with-a-comment` | **Date**: 2026-11-14 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `ai/specs/us07-approve-reject/spec.md` (Jira SCRUM-14)

## Summary

The first story that **changes** a request after it is submitted (increment 4e).

- Three endpoints for a manager, all behind the `RequireManager` policy and a resource rule ("the owner is my direct report, and not me"):
  - `GET /api/approvals/{id}`: the request page (details, balance, team overlap, status, decision, and which buttons are allowed).
  - `POST /api/approvals/{id}/approve` (optional comment).
  - `POST /api/approvals/{id}/reject` (comment required, 1 to 500 characters).
- One **named cross-module store**, `LeaveDecisionStore`, saves a decision in **one transaction**: the status change (guarded so only a *pending* request can be decided), for a rejection the release of the day rows and the balance restore with a ledger entry, the audit row, and the notification row. It is the second store the constitution's Principle III exception allows (the first is `LeaveApplicationStore`).
- **No migration.** The columns and constraints this story needs already exist (`DecidedByEmployeeId`, `DecidedAtUtc`, `DecisionComment` with `CK_LeaveRequest_RejectComment`, `CK_LeaveRequest_DecidedAt`, `LedgerEntryType.Restore`, `notify.OutboxMessage`).
- The **notification is recorded only** (owner decision): an outbox row with ids and the outcome. The email itself is increment 4g.
- Web: a request page at `approvals/:requestId` with Approve and Reject buttons (shown only while pending), and **return-URL support** so a manager who follows a link signs in and lands on that request.

**No new package. No email library. No migration.**

## Technical Context

**Language/Version**: C# 14 on .NET 10; TypeScript on Angular 22 (standalone, Signals, zoneless).

**Primary Dependencies**: EF Core 10, ASP.NET Core minimal APIs. No new NuGet or npm package.

**Storage**: existing tables only, no schema change (see [data-model.md](data-model.md)): `leave.LeaveRequest`, `leave.LeaveRequestDay`, `balances.LeaveBalance`, `balances.LeaveBalanceEntry`, `audit.AuditEvent`, `notify.OutboxMessage`.

**Testing**: Vitest for the new web code (success and failure path each). There is still no .NET test project (Phase 5), so the back end is verified by the manual calls in [quickstart.md](quickstart.md), and every report must say so. The pure rule that decides what a decision does is a Domain function (`LeaveDecision`) so Phase 5 can unit test it.

**Target Platform**: Windows dev PC (LocalDB); API `http://localhost:5000`, web `http://localhost:4200`.

**Project Type**: web application (API + Angular app).

**Performance Goals**: a decision saves in under 2 seconds (one transaction, a handful of statements).

**Constraints**:
- The server decides who may decide what (security rules 1, 3, 4): role policy, then the resource rule, then the status rule. 404 for a request the caller may not see, so ids cannot be probed.
- The manager is the token's `sub`; the **team is read from the database** every time (security rule 7). The client sends no person id and no status.
- Audit and outbox rows hold **ids, outcome and numbers only**: never a name, email or the free-text comment (security rule 12).
- One guarded atomic update for the balance plus a ledger entry (database rules); never read, add and write back.
- Errors are `ProblemDetails`, no stack trace or SQL. Dates are `DateOnly`, "now" from `TimeProvider`.
- A comment is plain text, shown with `{{ }}`, never `innerHTML` (security rule 13).

**Scale/Scope**: three endpoints, one use case per action (or one shared), one store, one Domain rule, one request page, one service, return-URL support.

**Dependencies on other specs**:
- `us06-manager-inbox` (SCRUM-13): the request page reuses its item shape (details, balance, team overlap). **At the time of this plan, US-06 has only T001 to T006 done on `main` (back end of the list, overlap still interim, no screen).** Finish US-06 T007 to T012 (inbox screen and overlap wiring) before US-07 starts, or this story builds the shared item builder itself and says so (research R9).
- `us03` and `us05`: the day rows, ledger entries and the "rejected requests never clash" hand-off this story fulfils.
- `4g`: delivers the email from the notification row.

## Constitution Check

*GATE: passes before research and again after design.*

| Principle | Result | Why |
| --- | --- | --- |
| I. Server enforces every rule | Pass | Comment rule, status rule, ownership rule and the balance change are all on the server. The screen only hides buttons. |
| II. Security by default | Pass | Fallback policy plus `RequireManager`; resource rule inside the use case; 404 for hidden requests; every decision writes an audit row in the same transaction; no secrets in the link; return URL accepted only if it is a path inside the app. |
| III. Dependency rule and modules | Pass, uses the one exception | `LeaveDecisionStore` is a **single, named** Application-level store that writes across leave, balances, audit and notify in one transaction, because a decision, its balance restore, its audit row and its notification must succeed or fail together. The spec names it (FR-018). Reads of other modules go through ports (team through `IDirectReportReader`, leave type through `ILeaveTypeReader`, balances through `IBalanceReader`). |
| IV. Dates and time | Pass | `TimeProvider` for the decision time (UTC). `DateOnly` for leave days. |
| V. Migrations append-only | Pass | None needed. |
| VI. Spec-driven | Pass | Spec approved by the owner before this plan; tasks come next and need approval. The spec was refined once during planning (see R7, R8). |
| VII. Verify before done | Pass | Build, web tests and the quickstart run are listed. .NET tests are not available (stated). |

No violations, so the Complexity Tracking table is not needed.

## Project Structure

### Documentation (this feature)

```text
ai/specs/us07-approve-reject/
├── spec.md
├── plan.md              # This file
├── research.md          # Phase 0
├── data-model.md        # Phase 1
├── quickstart.md        # Phase 1
├── contracts/
│   └── decision-api.md  # Phase 1
├── checklists/requirements.md
└── tasks.md             # Next step: /speckit-tasks
```

### Source Code (files this story is expected to touch)

```text
src/LeaveManagement.Domain/
└── LeaveDecision.cs                     # new: pure rule (what a decision does, and what it refuses)

src/LeaveManagement.Application/Approvals/
├── ApprovalDtos.cs                      # add: ApprovalDetail, DecideRequest, DecisionInfo
├── ILeaveDecisionStore.cs               # new: the named cross-module write port
├── IApprovalRequestReader.cs            # add: GetByIdAsync (any status)
├── GetApprovalDetail.cs                 # new: the request page use case (reuses the inbox item builder)
├── DecideLeaveRequest.cs                # new: approve or reject use case
└── ApprovalRules.cs                     # new: LeaveRuleErrors for the decision (codes in the contract)
src/LeaveManagement.Application/DependencyInjection.cs

src/LeaveManagement.Infrastructure/Persistence/
├── LeaveDecisionStore.cs                # new: one transaction, guarded status update
└── ApprovalRequestReader.cs             # add: GetByIdAsync
src/LeaveManagement.Infrastructure/DependencyInjection.cs

src/LeaveManagement.Api/Approvals/
└── ApprovalEndpoints.cs                 # add: GET /api/approvals/{id}, POST approve, POST reject

web/src/app/features/approvals/
├── approvals.service.ts (+ spec)        # add: getApproval, approve, reject
├── approval-detail.ts / .html / .scss / .spec.ts   # new: the request page
web/src/app/app.routes.ts                # route approvals/:requestId (+ spec)
web/src/app/auth/auth.guard.ts           # keep the wanted URL when signing in
web/src/app/auth/role-guard.ts           # let a child route use its parent's navigation path
web/src/app/features/login/login.ts      # go back to the wanted URL (checked) after sign-in
```

Not touched: migrations, the apply endpoints, the balances use case, the nav table.

**Structure Decision**: the store lives in `Infrastructure/Persistence` as `LeaveApplicationStore` does; its port and the use cases in `Application/Approvals`; the pure rule in Domain; endpoints grouped in `ApprovalEndpoints`, as `ai/steering/structure.md` says.

## Design (how it works)

```text
POST /api/approvals/{id}/reject   { "comment": "..." }        (Bearer token, role Manager)
   |
   v  ApprovalEndpoints   policy RequireManager; sub -> managerId; read body (max 4 KB); 400 on bad JSON
   v  DecideLeaveRequest.ExecuteAsync(managerId, id, Reject, comment)
        1. LeaveDecision.Validate(outcome, comment)      -> 422 comment.required, 400 comment too long (before any read)
        2. reports = IDirectReportReader.GetAsync(managerId)  ; request = IApprovalRequestReader.GetByIdAsync(id)
              request missing, or its owner not in reports   -> 404  (same answer for both; the manager is never in reports)
        3. request.Status != Pending                         -> 409 request.not_pending
        4. leave type via ILeaveTypeReader                    -> hasBalance
        5. ILeaveDecisionStore.DecideAsync(...)   ONE TRANSACTION
              a. UPDATE LeaveRequest SET Status, DecidedBy, DecidedAtUtc, DecisionComment
                      WHERE Id = @id AND Status = 'Pending'      0 rows -> rollback -> 409 request.not_pending
              b. Reject only: DELETE the request's LeaveRequestDay rows        (dates free again)
              c. Reject only, type has a balance: UPDATE LeaveBalance SET Available = Available + @days
                      + ledger entry (Restore, +days, RequestId)
              d. audit row   leave.approve / leave.reject   (ids, outcome, days; no comment)
              e. outbox row  leave.decided                  (requestId, employeeId, outcome; no comment)
              COMMIT
   v  200 ApprovalDetail (the request page, now decided)
```

The approve path is the same minus b and c: days were already held at submit, so nothing more leaves the balance. See [research.md](research.md) for the decisions and rejected options and [contracts/decision-api.md](contracts/decision-api.md) for the exact shapes.

## Complexity Tracking

Not needed. No constitution violations. The use of the Principle III exception is named and justified above.
