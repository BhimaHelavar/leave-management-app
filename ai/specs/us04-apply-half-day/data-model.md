# Data Model: Apply for a half day

**No data change.** No table, column, index, constraint or migration is added or edited.

The story reuses these US-03 items unchanged:

| Item | Where | How this story uses it |
| --- | --- | --- |
| `LeaveRequest.HalfDay` (None, FirstHalf, SecondHalf) | `leave.LeaveRequest` | Saved when a single day is chosen as a half day. |
| `LeaveRequest.DaysCount` | `leave.LeaveRequest` | Snapshot, 0.5 for a half day. Never recalculated. |
| `LeaveRequestDay` slot | `leave.LeaveRequestDay` | One slot for a half day, two for a full day. |
| `LeaveType.AllowHalfDay` | `balances.LeaveType` | Decides on the server whether a half day is allowed. The screen does not read it. |

## Screen state (front end only)

| Signal | Type | Rule |
| --- | --- | --- |
| `startDate`, `endDate` | `yyyy-MM-dd` string, `''` when empty | Set from the date inputs. |
| `halfDay` | `'None' \| 'FirstHalf' \| 'SecondHalf'` | Reset to `'None'` whenever the dates are not one single day. |
| `canChooseHalfDay` (computed) | boolean | `startDate !== '' && startDate === endDate`. Drives `disabled` on the select. |

### State transitions of the half-day choice

```text
dates not a single day:  select disabled, value None, hint shown
        |  user sets start == end
        v
single day:              select enabled, value None (user may pick FirstHalf / SecondHalf)
        |  user changes end date to another day
        v
dates not a single day:  halfDay reset to None, select disabled again
```
