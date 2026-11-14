# Contract: overlap refusal (changed message)

The API, routes, headers, status codes and codes in `ai/specs/us03-apply-leave-working-days/contracts/leave-requests-api.md` are **unchanged**. Only the text of the `leave.overlap` message changes.

## Response

`POST /api/me/leave-requests` answers **409** `application/problem+json`:

```json
{
  "title": "Request refused",
  "status": 409,
  "code": "leave.overlap",
  "detail": "You already have a pending or approved leave request on 2026-12-01, 2026-12-02.",
  "traceId": "..."
}
```

## Message rules

| Case | `detail` |
| --- | --- |
| One clashing date | `You already have a pending or approved leave request on 2026-12-01.` |
| Several clashing dates | `You already have a pending or approved leave request on 2026-12-01, 2026-12-02.` |
| More than 10 clashing dates | the first 10 in date order, then ` and N more.` (for example `... on 2026-12-01, ... (10 dates) and 3 more.`) |
| Exactly 11 clashing dates, 1 to 11 Dec | `You already have a pending or approved leave request on 2026-12-01, 2026-12-02, 2026-12-03, 2026-12-04, 2026-12-05, 2026-12-06, 2026-12-07, 2026-12-08, 2026-12-09, 2026-12-10 and 1 more.` (the dates are joined by `, `; ` and N more` follows the tenth date; the full stop comes last) |
| No date could be found (race edge case) | the old text: `You already have a pending or approved leave request on one or more of these days.` |

- Dates are distinct, sorted ascending, `yyyy-MM-dd`.
- Only dates inside the requested range, and only the caller's own.
- `code` stays `leave.overlap` and the status stays 409, so the screen's mapping is unchanged. The screen shows `detail` when it is present.
- A repeat of an already saved request (same `Idempotency-Key` and body) still returns the saved 201, not this refusal.
- The balance refusal is unchanged: 422 `balance.insufficient`, `You do not have enough balance for these days.`
- Every refusal leaves balances and requests unchanged.

## Examples

| Existing request | New request | Message |
| --- | --- | --- |
| Tue 1 Dec to Wed 2 Dec | Mon 30 Nov to Wed 2 Dec | `... on 2026-12-01, 2026-12-02.` |
| Wed 2 Dec only | Mon 30 Nov to Fri 4 Dec | `... on 2026-12-02.` |
| Tue 1 Dec and Fri 4 Dec (two requests) | Mon 30 Nov to Fri 4 Dec | `... on 2026-12-01, 2026-12-04.` |

## Change to the US-03 contract

In `leave-requests-api.md`, the refusal-message table row for `leave.overlap` becomes: "You already have a pending or approved leave request on {dates}." with a note pointing here. (Done in the task for the code change, not by this plan.)
