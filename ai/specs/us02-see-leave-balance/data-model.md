# Data Model: See my leave balance per leave type

No table or column is added. This feature reads four existing tables and returns a derived view.

## Source tables (read only)

| Table | Columns used | Why |
| --- | --- | --- |
| `balances.LeaveBalance` | EmployeeId, LeaveTypeId, LeaveYear, Available | Current available days (already net of pending) |
| `balances.LeaveBalanceEntry` | EmployeeId, LeaveTypeId, LeaveYear, EntryType, Days, ExpiresOn | Totals per entry type for accrued, held and carry-forward |
| `balances.LeaveType` | Id, Code, Name, IsActive, AccrualMode | Which types to show, and whether a type has a balance |
| `leave.LeaveRequest` | EmployeeId, LeaveTypeId, LeaveYear, Status, DaysCount | Pending days |

Existing indexes serve the queries: unique (EmployeeId, LeaveTypeId, LeaveYear) on `LeaveBalance`, the covering index on `LeaveBalanceEntry`. No new index (database rule: only for a named query that needs one).

## Derived view: BalanceSummary (per leave type, per year)

| Field | Meaning | Rule |
| --- | --- | --- |
| `leaveTypeId`, `code`, `name` | the type | from `LeaveType` |
| `hasBalance` | false for types with no balance (Unpaid) | `AccrualMode = None` and no balance row |
| `accrued` | net entitlement this year after expiry | `available + held` |
| `used` | approved days | `held - pending` |
| `pending` | days of requests awaiting approval | sum `DaysCount`, status Pending |
| `available` | days the person can still take | `LeaveBalance.Available`, never below 0 (database CHECK) |
| `carryForwardDays` | part of available that is carry-forward | R5; null when none |
| `carryForwardExpiresOn` | `DateOnly` the carry-forward lapses | earliest `ExpiresOn` on or after today; null when none |

`held` is an intermediate value: `-(sum of Deduction and Restore entry Days)`.

Invariant (checked in the verification run): `accrued - used - pending = available`.

## Worked examples

| Case | Ledger | Requests | accrued | used | pending | available |
| --- | --- | --- | --- | --- | --- | --- |
| Jira AC1 | Grant +10, Deduction -2 | 1 Approved, 2 days | 10 | 2 | 0 | 8 |
| Jira AC2 | Grant +10, Deduction -2, Deduction -3 | Approved 2, Pending 3 | 10 | 2 | 3 | 5 |
| Rejected | as AC2, plus Restore +3 | Approved 2, Rejected 3 | 10 | 2 | 0 | 8 |
| Half day | Grant +10, Deduction -0.5 | Pending 0.5 | 10 | 0 | 0.5 | 9.5 |
| Carry-forward | Grant +12, CarryForward +4 (expires 31 Mar), Deduction -1 | Approved 1 | 16 | 1 | 0 | 15 (4 - 1 = 3 are carry-forward) |

## State changes

None. The endpoint never writes. The ledger and `LeaveBalance` are changed only by the apply, approve, reject, cancel and HR adjustment features (4d to 4f).
