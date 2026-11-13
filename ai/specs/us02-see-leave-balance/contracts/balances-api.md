# Contract: GET /api/me/balances

Returns the signed-in person's leave balances for the current leave year.

## Request

- Method and path: `GET /api/me/balances`
- Authentication: bearer token (any signed-in person). No role policy.
- Parameters: none. The person is identified by the `sub` claim, never by a client-sent id.
- Body: none.

## Response 200

```json
{
  "leaveYear": 2026,
  "balances": [
    {
      "leaveTypeId": 1,
      "code": "ANNUAL",
      "name": "Annual leave",
      "hasBalance": true,
      "accrued": 10.0,
      "used": 2.0,
      "pending": 3.0,
      "available": 5.0,
      "carryForwardDays": null,
      "carryForwardExpiresOn": null
    },
    {
      "leaveTypeId": 5,
      "code": "UNPAID",
      "name": "Unpaid leave",
      "hasBalance": false,
      "accrued": null,
      "used": null,
      "pending": null,
      "available": null,
      "carryForwardDays": null,
      "carryForwardExpiresOn": null
    }
  ]
}
```

- Numbers are exact decimals (for example 7.5), never rounded to whole days.
- `carryForwardExpiresOn` is a date only (`yyyy-MM-dd`), no time.
- `balances` is ordered by leave type id. It contains only active leave types (R4).
- When `hasBalance` is false, the five numeric fields are null.
- A person with no rows still gets 200 with the types, each showing 0 (types with a balance) or `hasBalance: false`.
- Manual-accrual types (Comp-off, Maternity, Paternity) have no row until HR grants days, and show 0.
- A deactivated person whose token has not expired can still read their own (empty or old) balances for up to 60 minutes. This matches `/api/me`, and no one else's data is exposed.

## Errors (ProblemDetails, no stack trace, no SQL)

| Status | When |
| --- | --- |
| 401 | No token, an invalid or expired token, or a token whose `sub` claim is not a number (same as `/api/me`) |
| 500 | Unexpected failure; details go to the log, not the response |

There is no 403: every signed-in person may read their own balances. There is no 422: the endpoint takes no input.

## Rules enforced here (server side)

- Own balances only, by construction (FR-008): no employee id in the request.
- Figures come from stored ledger and request data (FR-010).
- Available is never below zero (database CHECK plus the guarded update elsewhere).
- Nothing is logged except the employee id.

## Not in this contract

Team balances, other years, and any write. These belong to later specs.
