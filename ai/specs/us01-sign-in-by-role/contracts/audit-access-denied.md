# Contract: `AccessDenied` audit record

Written by the authorization result handler whenever a signed-in person is refused for a missing role.

```json
{
  "action": "AccessDenied",
  "entityType": "Endpoint",
  "entityId": "/api/admin/ping",
  "actorEmployeeId": 7,
  "detail": { "policy": "RequireAdmin" },
  "correlationId": "00-...",
  "occurredAtUtc": "2026-11-10T11:30:00.000Z"
}
```

## Interface (Application layer)

`IAuditWriter.WriteAsync(AuditRecord record, CancellationToken ct)`, where `AuditRecord` carries: `Action`, `EntityType`, `EntityId`, `ActorEmployeeId` (nullable), `Detail` (object serialised to JSON), `CorrelationId`.

## Guarantees

- Exactly one row per refused request.
- No name, email, free text or token.
- If writing fails, the 403 is still returned and the failure is logged with the trace id.
- 4e reuses this interface for apply, approve, reject, cancel and balance changes.
