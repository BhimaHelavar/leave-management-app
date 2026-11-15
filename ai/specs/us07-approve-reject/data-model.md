# Data Model: Approve or reject a request with a comment

**No schema change.** No table, column, index, constraint or migration is added or edited. Everything this story writes already exists.

## What a decision touches

| Table | Approve | Reject | Notes |
| --- | --- | --- | --- |
| `leave.LeaveRequest` | `Status` = Approved, `DecidedByEmployeeId`, `DecidedAtUtc`, `DecisionComment` (optional) | `Status` = Rejected, the same three fields, `DecisionComment` required | One guarded update `WHERE Id = @id AND Status = 'Pending'`. `RowVersion` changes by itself. |
| `leave.LeaveRequestDay` | unchanged (the days stay held) | the request's rows are **deleted** | The dates become free; the unique index `UX_LeaveRequestDay_Employee_Date_Slot` stays. |
| `balances.LeaveBalance` | unchanged | `Available = Available + DaysCount` for the person, type and `LeaveYear` | Only for a type with a balance. One atomic update, never read-add-write. |
| `balances.LeaveBalanceEntry` | none | one entry: `EntryType` = Restore, `Days` = +DaysCount, `RequestId`, `CreatedByEmployeeId` = the manager | Keeps the ledger honest: the dashboard's "held" is `-(Deduction + Restore)`. |
| `audit.AuditEvent` | `leave.approve` | `leave.reject` | Detail: ids, outcome, days, type id. **No comment, name or email.** |
| `notify.OutboxMessage` | `leave.decided` | `leave.decided` | Payload: `{ requestId, employeeId, outcome }`. **No comment.** Delivered by 4g. |

## Constraints that already guard the data (second line of defence)

| Constraint | What it holds |
| --- | --- |
| `CK_LeaveRequest_RejectComment` | `Status <> 'Rejected' OR DecisionComment IS NOT NULL`: a rejection always has a comment |
| `CK_LeaveRequest_DecidedAt` | pending means no decision time; every other status has one |
| `CK_LeaveRequest_Status` | status is one of the enum values |
| `DecisionComment` length | up to 500 characters |
| `LeaveBalance` available at or above zero | a restore only adds days |
| FK `DecidedByEmployeeId` to `people.Employee` | the decider is a real person |

## State transitions of a request (this story's part)

```text
Pending --approve--> Approved       (days stay held; day rows stay; nothing more taken)
Pending --reject---> Rejected       (day rows deleted; days restored; comment required)
Approved, Rejected, Cancelled: no decision allowed (409 request.not_pending)
```

Cancel (employee before start, HR after) is another story and adds `Pending or Approved --> Cancelled`.

## Rules for each field (verbatim from the spec and the schema)

| Field | Rule |
| --- | --- |
| Comment (reject) | required; not empty and not only spaces; at most 500 characters |
| Comment (approve) | optional; if given, at most 500 characters; spaces only counts as no comment |
| Comment text stored | trimmed; never interpreted as markup |
| `DecidedAtUtc` | UTC, from `TimeProvider` |
| `DecidedByEmployeeId` | the signed-in manager (token `sub`), never taken from the body |
| Days restored | the request's saved `DaysCount` (decimal 4,1), never recalculated |

## Derived figures

| Figure | Rule |
| --- | --- |
| Employee's `used` after approve | grows by the request's days (they leave "pending") |
| Employee's `available` after approve | unchanged |
| Employee's `available` after reject | rises by the request's days |
| `allowedActions` on the request page | `["approve", "reject"]` when pending and the caller is the owner's manager; `[]` otherwise |

## Audit and outbox detail (examples, ids and numbers only)

```json
audit  leave.reject   { "requestId": 35, "leaveTypeId": 1, "outcome": "Rejected", "days": 2.0 }
outbox leave.decided  { "requestId": 35, "employeeId": 6, "outcome": "Rejected" }
```
