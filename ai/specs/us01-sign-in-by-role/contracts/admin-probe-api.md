# Contract: Admin probe endpoint

A small Admin-only endpoint that proves the role check and the audit (research D5). Remove it when real Admin endpoints exist.

| Method and path | Policy | Success | Errors |
| --- | --- | --- | --- |
| `GET /api/admin/ping` | `RequireAdmin` | `204 No Content` | `401` no or bad token; `403` signed in without Admin |

## 403 response

`ProblemDetails`, no stack trace, no role list:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.4",
  "title": "Forbidden",
  "status": 403,
  "traceId": "00-..."
}
```

The `traceId` matches `CorrelationId` in the audit row.

## 401 response

`ProblemDetails` with status 401. Not audited.

## Behaviour by person (Dev mode demo people)

| Signed in as | Result |
| --- | --- |
| Employee only | 403 and one `AccessDenied` audit row |
| Manager or HR (no Admin) | 403 and one audit row |
| Admin | 204, no audit row |
| not signed in | 401, no audit row |
