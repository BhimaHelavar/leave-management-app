# Contract: approve or reject a request

Same conventions as the US-03 and US-06 contracts: JSON camelCase, enums as strings, leave days `"yyyy-MM-dd"`, errors as `application/problem+json` with a stable `code`, no stack trace or SQL.

All three calls: **Bearer token, role Manager** (policy `RequireManager`). The manager is the token's `sub`. The team is read from the database on every call. No person id, status or decision time is ever accepted from the client.

## GET /api/approvals/{requestId}

The request page. `requestId` is a whole number.

### Success: 200

```json
{
  "requestId": 35,
  "status": "Pending",
  "employee": { "id": 6, "name": "Sneha Iyer" },
  "leaveType": { "id": 1, "code": "ANNUAL", "name": "Annual leave" },
  "startDate": "2026-12-01",
  "endDate": "2026-12-02",
  "halfDay": "None",
  "daysCount": 2.0,
  "submittedAtUtc": "2026-11-14T11:51:50Z",
  "balance": { "before": 8.0, "after": 6.0 },
  "teamOverlap": { "teamSize": 3, "awayIfApproved": 2, "others": [ { "employeeId": 4, "name": "Asha Nair", "dates": ["2026-12-02"] } ] },
  "decision": null,
  "allowedActions": ["approve", "reject"]
}
```

| Field | Rule |
| --- | --- |
| `requestId` .. `teamOverlap` | The same fields and rules as an inbox item (`ai/specs/us06-manager-inbox/contracts/manager-inbox-api.md`) |
| `status` | `Pending`, `Approved`, `Rejected` or `Cancelled` |
| `decision` | `null` while pending. Otherwise `{ "outcome": "Rejected", "decidedAtUtc": "...", "comment": "..." }`. The comment may be null for an approval. |
| `allowedActions` | `["approve", "reject"]` only while the status is `Pending`; `[]` otherwise. Computed on the server so the screen copies no rule. |
| `teamOverlap` | Present only while the request is `Pending` (it helps the decision). `null` once decided: a rejected request has released its day rows, and the overlap no longer informs anything. |
| `balance` | Present only while the request is `Pending` and its type has a balance (same rule as the inbox). `null` once decided, because "before and after this request" no longer describes anything: a rejection has already put the days back and an approval changed nothing. |

**Not in the response:** the employee's leave reason, emails, any balance but the requester's, any person outside the team.

## POST /api/approvals/{requestId}/approve

Body (optional): `{ "comment": "Enjoy the break." }`

- `comment`: optional; at most 500 characters; spaces only counts as none; stored trimmed.
- Effect: the request becomes `Approved`. The days stay held. Nothing else is taken from the balance.

### Success: 200

The request page shape above, now with `status` `Approved`, `decision` filled and `allowedActions` `[]`.

## POST /api/approvals/{requestId}/reject

Body (required): `{ "comment": "Release week, please move to the 16th." }`

- `comment`: required; not empty and not only spaces; at most 500 characters; stored trimmed.
- Effect: the request becomes `Rejected`; its days go back to the balance (for a type with a balance); its dates become free again.

### Success: 200

The request page shape above, with `status` `Rejected`, `decision` filled and `allowedActions` `[]`.

## Errors (all three calls)

| Status | `code` | When |
| --- | --- | --- |
| 400 | none | Body is not valid JSON, is over 4 KB, `comment` is not text, or `comment` is over 500 characters (message names the limit) |
| 401 | none | No token, a bad or expired token, `sub` not a number, or the person is unknown or inactive |
| 403 | none | Signed in but without the Manager role. The same answer for everyone. |
| 404 | none | No such request, **or** its owner is not the caller's direct report (this includes the caller's own request and an inactive or moved employee). The same answer for all of these. |
| 409 | `request.not_pending` | The request is already approved, rejected or cancelled, or another decision won a race. Nothing changes. |
| 422 | `comment.required` | Reject with no comment or a comment of only spaces. Nothing changes. |

Order of checks: role (403), body shape (400), comment rule (422), then the request lookup and ownership (404), then the status (409).

Every refusal leaves the request, balances, day rows, audit log and outbox exactly as they were.

### Messages (ProblemDetails `detail`)

| `code` | Text |
| --- | --- |
| `comment.required` | A rejection needs a comment that tells the employee why. |
| `request.not_pending` | This request has already been decided. |

## What a successful decision writes (one transaction)

| Table | Row |
| --- | --- |
| `leave.LeaveRequest` | status, `DecidedByEmployeeId`, `DecidedAtUtc`, `DecisionComment` |
| `leave.LeaveRequestDay` | reject only: the request's rows deleted |
| `balances.LeaveBalance` and `LeaveBalanceEntry` | reject only, type with a balance: `Available` + days and a `Restore` entry |
| `audit.AuditEvent` | `leave.approve` or `leave.reject`, ids and numbers only |
| `notify.OutboxMessage` | `leave.decided` with `{ requestId, employeeId, outcome }` |

## Screen contract (request page, route `approvals/:requestId`)

| State | What the manager sees |
| --- | --- |
| Loading | "Loading request..." in a status region |
| Not found | The same not-found page as an unknown route |
| Error | The message and a Try again button |
| Pending | Details, balance, overlap text, an Approve button and a Reject button |
| Reject chosen | A comment box (labelled, 500 character limit, required) and a Send rejection button; empty comment shows "Please write a comment" and nothing is sent |
| Decided | The outcome, who decided is not shown by name, the date, and the comment as plain text; no buttons |
| After a decision | The new status; a refusal shows the server's message |

Link: `{web base URL}/approvals/{requestId}`. It holds no token. A signed-out visit goes to sign-in and comes back to the same address (the returned address must start with a single `/`, with no scheme and no `//` or backslash, or the dashboard opens instead).
