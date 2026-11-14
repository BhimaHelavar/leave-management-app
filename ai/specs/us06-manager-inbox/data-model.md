# Data Model: Review pending requests in a manager inbox

**No data change.** No table, column, index, constraint or migration is added or edited, and nothing is written.

## What is read, and by which port

| Port (module) | Reads | Fields used |
| --- | --- | --- |
| `IDirectReportReader` (people) | `people.Employee` | `Id`, `FullName`, `ManagerId`, `IsActive` |
| `IApprovalRequestReader` (leave) | `leave.LeaveRequest`, `leave.LeaveRequestDay` | request: `Id`, `EmployeeId`, `LeaveTypeId`, `StartDate`, `EndDate`, `HalfDay`, `DaysCount`, `SubmittedAtUtc`, `Status`; day rows: `RequestId`, `EmployeeId`, `LeaveDate`, `Slot` |
| `IBalanceReader` (balances), existing | `balances.LeaveBalance`, ledger entries, `balances.LeaveType` | per person and leave year: `Available`, type id, code, name, accrual mode |

No port reads another module's tables. The use case joins the results in memory.

## Who is on the team

```text
Employee where ManagerId = caller AND IsActive AND Id <> caller   ->  direct reports
team size = number of direct reports (the requester is one of them)
```

## Which requests are listed

```text
LeaveRequest where EmployeeId in direct reports AND Status = Pending
order by StartDate, SubmittedAtUtc, Id            (all ascending, in the database)
paged: OFFSET (page - 1) * pageSize, FETCH pageSize, plus the total count
```

## What "away" means

```text
a LeaveRequestDay row exists for a person   ->  that (date, slot) is held (pending or approved)
no row (never requested, rejected, cancelled) ->  not away
```

Day rows exist only for working days, so a weekend or a holiday is never an overlap. A first half and a second half on one date are different slots, so they do not overlap each other; a half day and a full day on one date share a slot, so they do.

## Derived figures (computed in the use case, never stored)

| Figure | Rule |
| --- | --- |
| `balance.after` | The requester's `Available` for the request's leave type and leave year (`StartDate.Year`). Already net of this request, because days are deducted at submit. |
| `balance.before` | `after + DaysCount` of this request. |
| `balance` | Omitted (null) for a type with no balance (Unpaid) or when the type is no longer active in the ledger. |
| `teamOverlap.others` | Other reports who hold a slot equal to one of this request's slots: name, id, and the shared dates (distinct, sorted). |
| `teamOverlap.awayIfApproved` | 1 + the number of distinct people in `others` (the requester counts, owner decision FR-006). |
| `teamOverlap.teamSize` | The number of direct reports. |

## State transitions

None here. A request leaves the list when 4e moves it out of `Pending`. A report leaves the team when their `ManagerId` changes or they are deactivated; the next read shows that.

## Nothing is audited

Reading the inbox writes no audit row (security rule 14 covers apply, approve, reject, cancel and balance changes only).
