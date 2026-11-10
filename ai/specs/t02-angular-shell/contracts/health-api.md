# Contract: GET /health

Existing endpoint (4a). Contract kept stable; the shell depends on it.

- **Auth**: anonymous (`AllowAnonymous`), stays public (spec FR-007, 4c R10).
- **Request**: no body, no parameters.
- **200 response** (JSON, camelCase):

```json
{
  "status": "Healthy",
  "authMode": "Dev",
  "database": "Connected",
  "environment": "Demo",
  "timeUtc": "2026-11-02T17:00:00+00:00"
}
```

| Field | Values |
|---|---|
| status | `Healthy` when the database answers, otherwise `Degraded` |
| authMode | `Dev` or `EntraId` |
| database | `Connected` or `Unavailable` |
| environment | ASP.NET environment name |

- **Failure**: network error, CORS block or 5xx -> the shell shows its error state and offers "Check again".
- **Changes in this feature**: `timeUtc` comes from `TimeProvider` (still UTC, same shape). No field is added or removed.
- **Startup rule (not a response)**: with `Auth:Mode=Dev` and an environment other than `Development` or `Demo`, the API does not start.
- **Privacy**: no names, emails or tokens in this response (security rule 12).
