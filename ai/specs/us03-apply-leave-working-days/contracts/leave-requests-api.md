# Contract: Leave requests (apply and preview)

Both endpoints act on the signed-in person only. The person comes from the `sub` claim, never from the URL or body.

## POST /api/me/leave-requests

Applies for leave.

### Request

- Authentication: bearer token (any signed-in person). No role policy.
- Header: `Idempotency-Key` (required), 1 to 64 characters from `A-Z a-z 0-9 - _`. Send a new random value for each new application and reuse it on a retry.
- Body:

```json
{
  "leaveTypeId": 1,
  "startDate": "2027-10-01",
  "endDate": "2027-10-05",
  "halfDay": "None",
  "reason": "Family trip"
}
```

| Field | Rules |
| --- | --- |
| `leaveTypeId` | Required. Must be an active leave type. |
| `startDate`, `endDate` | Required, `yyyy-MM-dd`. |
| `halfDay` | Optional, default `None`. `FirstHalf` or `SecondHalf` only when start equals end and the type allows half days. |
| `reason` | Optional, up to 500 characters. Stored on the request. Never copied into the audit row. |

There is no field for the employee, status, day count or owner. Unknown fields are ignored.

### Response 201

```json
{
  "id": 42,
  "leaveTypeId": 1,
  "startDate": "2027-10-01",
  "endDate": "2027-10-05",
  "halfDay": "None",
  "daysCount": 2.0,
  "status": "Pending",
  "leaveYear": 2027
}
```

A retry with the same key and the same body returns **the same 201 body again** and does not create or deduct twice. This also holds when two requests with the same key arrive at the same time: one is saved, and the other returns that saved answer. (With different keys for the same dates, one wins and the other gets 409 `leave.overlap`.)

### Errors (ProblemDetails, no stack trace, no SQL)

| Status | `code` | When |
| --- | --- | --- |
| 400 | none | Missing or malformed `Idempotency-Key`, bad date text, unknown `halfDay`, reason too long |
| 401 | none | No token, bad or expired token, `sub` not a number, or the person is unknown or inactive |
| 409 | `leave.overlap` | Any day in the range is already in the person's pending or approved request (also when two submits race) |
| 422 | `date.range` | End date before start date |
| 422 | `date.past` | Start date before today (in the person's time zone) |
| 422 | `year.cross` | Range crosses two leave years. Message says to submit one request per year |
| 422 | `days.none` | No working day in the range (weekend or holidays only) |
| 422 | `halfday.invalid` | Half day with start different from end, a type that does not allow it, or a non-working day |
| 422 | `type.unknown` | Leave type not found or not active |
| 422 | `notice.short` | Less notice than the type's minimum working days (rule 7) |
| 422 | `max.consecutive` | Longer than the type's maximum. Message says longer leave needs HR (rule 8) |
| 422 | `balance.insufficient` | Days needed exceed available (rule 5) |
| 422 | `idempotency.mismatch` | Same key reused with a different body |
| 500 | none | Unexpected failure; details go to the log |

Every 4xx leaves balances and requests unchanged. When several rules fail, the response names the **first** in this order: `type.unknown`, `date.range`, `year.cross`, `date.past`, `halfday.invalid`, `days.none`, `notice.short`, `max.consecutive`, `leave.overlap`, `balance.insufficient`.

### Refusal messages (sent as ProblemDetails `detail`; the screen shows the same text)

| `code` | Message |
| --- | --- |
| `date.range` | The end date cannot be before the start date. |
| `date.past` | The start date cannot be in the past. |
| `year.cross` | A request cannot cross two leave years. Submit one request for each year. |
| `days.none` | The dates you chose have no working days. Weekends and holidays are not counted. |
| `halfday.invalid` | A half day must be a single working day, and this leave type must allow half days. |
| `type.unknown` | This leave type is not available. |
| `notice.short` | This leave type needs at least {n} working days' notice. |
| `max.consecutive` | This leave type allows at most {n} days in a row. Longer leave needs HR. |
| `leave.overlap` | You already have a pending or approved leave request on {dates}. (The dates are named since US-05; see `ai/specs/us05-block-invalid-requests/contracts/overlap-refusal.md` for the date rules.) |
| `balance.insufficient` | You do not have enough balance for these days. |
| `idempotency.mismatch` | This request key was already used for a different request. |

`{n}` is the number from the leave type. A message never includes another person's data, SQL text or the reason text.

## GET /api/me/leave-requests/preview

Counts the working days for the form. It changes nothing and writes no audit row.

### Request

- Authentication: bearer token.
- Query: `leaveTypeId`, `startDate`, `endDate`, `halfDay` (optional). Same meaning as above.

### Response 200

```json
{
  "daysCount": 2.0,
  "workingDays": ["2027-10-01", "2027-10-05"],
  "skippedHolidays": ["2027-10-04"]
}
```

- `daysCount` is exactly what a submit would save if nothing changes in between.
- `workingDays` lists the counted dates. `skippedHolidays` lists holidays inside the range that were not counted (weekends are not listed).
- It runs the rule checks that need no balance or overlap lookup and returns the same 422 `code` values (`date.range`, `year.cross`, `date.past`, `halfday.invalid`, `days.none`, `notice.short`, `max.consecutive`, `type.unknown`). It does **not** check overlap or balance; those are decided only at submit.
- Errors: 400 for malformed query, 401 as above.

## Rules enforced here (server side)

- Own requests only, by construction: no employee id in either request.
- The count, status and owner are set by the server (no over-posting).
- Counting: Monday to Friday minus the person's location holidays; weekends and holidays inside the range never count (sandwich rule off); a half day counts 0.5.
- The request, day rows, balance deduction, ledger entry, audit row and idempotency row are one transaction.
- Nothing is logged except ids. The reason text and tokens are never logged.

## Not in this contract

Listing or reading my requests, approve, reject, cancel, balances for other people, back-dated requests. These belong to later specs.
