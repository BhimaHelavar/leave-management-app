# Data Model: Apply for leave with an accurate working-day count

**No schema change and no migration.** Everything below already exists from increment 4b. This file records how this story *uses* each table.

## Tables written

| Table | What this story writes | Notes |
| --- | --- | --- |
| `leave.LeaveRequest` | One row: EmployeeId (from the token), LeaveTypeId, StartDate, EndDate, HalfDay, DaysCount (snapshot), Status = Pending, Reason, SubmittedAtUtc | `LeaveYear` is computed by the database. `DecidedAtUtc` stays null (CHECK). `RowVersion` set by the database. |
| `leave.LeaveRequestDay` | One row per working half-day slot: RequestId, EmployeeId (copy), LeaveDate, Slot (AM or PM) | Full day = two rows. Half day = one row. Unique index (EmployeeId, LeaveDate, Slot) settles overlap races. |
| `balances.LeaveBalance` | `Available` reduced by DaysCount with a guarded update | Skipped for types with no balance (AccrualMode None). `CHECK Available >= 0` is the backstop. |
| `balances.LeaveBalanceEntry` | One `Deduction` row: Days = -DaysCount, RequestId, CreatedByEmployeeId | Skipped for types with no balance. |
| `audit.AuditEvent` | One row, action `leave.apply` | Detail JSON: ids, dates, days only. Never the reason text, name or email (security rule 12). |
| `leave.IdempotencyKey` | One row per accepted key: EmployeeId, KeyValue, RequestHash, StatusCode, ResponseJson | Same transaction as the request. Cleanup job is later work. |

## Tables read

| Table | Why |
| --- | --- |
| `people.Employee` | LocationId (which holidays apply) and IsActive. Through `IApplicantReader`. |
| `people.Location` | TimeZoneId, for the person's "today" (R4). |
| `balances.Holiday` | Dates in the range for the location. Through `IHolidayReader`. |
| `balances.LeaveType` | Rules: MinNoticeWorkingDays, MaxConsecutiveDays, AllowHalfDay, AccrualMode, IsActive. Through `ILeaveTypeReader`. |
| `leave.LeaveRequestDay` | Overlap pre-check, for a friendly message before the unique index is hit. |

## Constraints that back each rule

| Rule | First line (code) | Second line (database) |
| --- | --- | --- |
| Range sane, one leave year | `LeaveRules` | `CK_LeaveRequest_Dates` (End >= Start, same year) |
| Half day is one day (11) | `LeaveRules` | `CK_LeaveRequest_HalfDay` |
| Count above zero | no-working-days check | `CK_LeaveRequest_Days` (DaysCount > 0) |
| Overlap (13) | pre-check query | `UX_LeaveRequestDay_Employee_Date_Slot` |
| Balance never negative (5) | guarded UPDATE | `CK_LeaveBalance_Available` |
| Notice (7), maximum (8) | `LeaveRules` | none (policy, stored on `LeaveType`, so admin-changeable) |

## Value shapes

- `DaysCount`: `decimal(4,1)`, multiples of 0.5, greater than 0.
- Dates: `DateOnly` in C#, `date` in SQL, `yyyy-MM-dd` in JSON.
- `HalfDay` in the API: `None`, `FirstHalf`, `SecondHalf` (FirstHalf = AM slot, SecondHalf = PM slot).

## State

The only state this story creates is `Pending`. Moving on from `Pending` (approved, rejected, cancelled) is increment 4e.

```text
submit -> Pending   (days held: Available down, Deduction ledger row, audit row)
```

## Sequence of the single transaction

```text
BEGIN
  1. idempotency row for (employee, key)?  yes -> return saved answer (or 422 if hash differs)
  2. overlap pre-check on LeaveRequestDay  yes -> 409
  3. INSERT LeaveRequest                   (get Id)
  4. INSERT LeaveRequestDay x slots        (unique index violation -> rollback, 409)
  5. UPDATE LeaveBalance ... WHERE Available >= @days   0 rows -> rollback, 422 balance
  6. INSERT LeaveBalanceEntry (Deduction)
  7. INSERT AuditEvent
  8. INSERT IdempotencyKey (saved answer)
COMMIT
```

Steps 5 and 6 are skipped for a type with no balance.
