# Contract: manager inbox

Same conventions as the US-03 contract (`ai/specs/us03-apply-leave-working-days/contracts/leave-requests-api.md`): JSON camelCase, enums as strings, leave days as `"yyyy-MM-dd"`, errors as `application/problem+json`, no stack trace or SQL.

## GET /api/approvals

Lists the pending leave requests of the caller's direct reports.

- **Auth**: Bearer token, role **Manager** (policy `RequireManager`). The manager is the token's `sub`. The team is read from the database on every call, never from the token or the query.
- **Query**: `page` (default 1, at least 1), `pageSize` (default 20, 1 to 100). No other parameter; a person id is never accepted.

### Success: 200

```json
{
  "items": [
    {
      "requestId": 35,
      "employee": { "id": 6, "name": "Sneha Iyer" },
      "leaveType": { "id": 1, "code": "ANNUAL", "name": "Annual leave" },
      "startDate": "2026-12-01",
      "endDate": "2026-12-02",
      "halfDay": "None",
      "daysCount": 2.0,
      "submittedAtUtc": "2026-11-14T11:51:50Z",
      "balance": { "before": 8.0, "after": 6.0 },
      "teamOverlap": {
        "teamSize": 3,
        "awayIfApproved": 2,
        "others": [
          { "employeeId": 4, "name": "Asha Nair", "dates": ["2026-12-02"] }
        ]
      }
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 1
}
```

| Field | Rule |
| --- | --- |
| `items` | Sorted by `startDate`, then `submittedAtUtc`, then `requestId`, all ascending. Only `Pending` requests of direct reports. |
| `halfDay` | `None`, `FirstHalf` or `SecondHalf`. `daysCount` is 0.5 for a half day. |
| `daysCount` | The saved snapshot of the request, never recalculated. |
| `balance` | `before` is `after` plus `daysCount`. `after` is the requester's available days now for that type and leave year. `null` for a type with no balance. With several pending requests of one type, each shows the same `after`. |
| `teamOverlap.teamSize` | Number of active direct reports, the requester included. |
| `teamOverlap.others` | Other direct reports holding a pending or approved slot equal to one of this request's slots. `dates` are the shared dates, distinct, ascending. Empty list when no one else is away. |
| `teamOverlap.awayIfApproved` | `1 + others.length` (the requester counts). |
| `totalCount` | All pending requests of the team, for paging. Empty team or nothing pending: `items` is `[]` and `totalCount` is 0. |

**Not in the response:** the leave reason, emails, any balance but the requester's, any person outside the team.

### Errors

| Status | When | Notes |
| --- | --- | --- |
| 400 | `page` or `pageSize` is not a whole number, or out of range | `ProblemDetails` with a short `detail`; nothing else changes |
| 401 | No token, a bad or expired token, `sub` not a number, or the person is unknown or inactive | |
| 403 | The caller is signed in but does not hold the Manager role | The same answer for every such caller, whether or not anyone has pending requests |

There is no 404 case: no id is in the URL, so there is no record whose existence could leak.

### Behaviour rules

- Reading changes no request, balance or audit record.
- A manager with no reports, or with nothing pending, gets 200 with an empty list.
- A person who holds Manager and HR still gets only direct reports here.
- The caller's own requests never appear, and a person who no longer reports to the caller (or is inactive) never appears.
- A page is repeatable: the same data gives the same order and the same pages.

## Screen contract (Approvals)

Route `approvals`, Manager role (already in the nav table). The screen shows:

| State | What the manager sees |
| --- | --- |
| Loading | "Loading requests..." in a status region |
| Error | The message with a Try again button |
| Empty | "Nothing is waiting for your decision." |
| List | One card per item: name, leave type, dates (with First half or Second half for a half day), days, balance "Before 8.0, after 6.0", and the overlap text |
| Paging | Previous and Next buttons, disabled at the ends, with "Page 1 of N" |

Overlap text examples (words, not colour):

| Data | Text |
| --- | --- |
| `others` is empty | "No one else in your team is away on these days." |
| One other | "Also away: Asha Nair (2026-12-02). 2 of 3 of your team would be away if you approve." |
| Several | "Also away: Asha Nair (2026-12-02); Ravi Kumar (2026-12-01, 2026-12-02). 3 of 3 of your team would be away if you approve." |

There are no Approve or Reject buttons in this story.
