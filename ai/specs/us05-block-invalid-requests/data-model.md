# Data Model: Block requests that break my balance or overlap

**No data change.** No table, column, index, constraint or migration is added or edited.

The story reads these US-03 items unchanged:

| Item | Where | How this story uses it |
| --- | --- | --- |
| `LeaveRequestDay` (`EmployeeId`, `LeaveDate`, `Slot`) | `leave.LeaveRequestDay` | The store reads the caller's rows inside the requested range. A row that matches a wanted `(date, slot)` is a clash. The unique index `UX_LeaveRequestDay_Employee_Date_Slot` stays the backstop for races. |
| `LeaveBalance.Available` | `balances.LeaveBalance` | The guarded update `Available >= days` decides "insufficient balance". Unchanged. |
| `LeaveRequest.Status` | `leave.LeaveRequest` | Not read for the clash. A request holds days while pending or approved; reject and cancel (4e) release its day rows. |

## Who holds a day

```text
a (date, slot) row exists for the person   ->  that slot is taken (pending or approved)
no row                                      ->  free (never requested, rejected or cancelled)
```

## Refusal shape (in memory, not stored)

| Piece | Rule |
| --- | --- |
| `LeaveRuleError.Code` | `leave.overlap` (unchanged) |
| `LeaveRuleError.Message` | Names the clashing dates: distinct, sorted ascending, ISO `yyyy-MM-dd`, at most 10, then `and N more`. Falls back to the old generic text when the list is empty. |
| Dates source | The caller's own `LeaveRequestDay` rows that match a wanted `(date, slot)` inside `[StartDate, EndDate]` |

Nothing about the refusal is written to the database. The audit row is written only for a successful apply (US-03), so no date list reaches the audit table.
