# Data model: Sign in and see only what my role allows

**No database change and no migration.** The feature writes to the existing table `audit.AuditEvent` (entity `AuditEvent` in `src/LeaveManagement.Domain/Messaging.cs`).

## AuditEvent as used here

| Column | Value for a role refusal | Notes |
| --- | --- | --- |
| `Id` | generated | `long` identity |
| `OccurredAtUtc` | `TimeProvider.GetUtcNow()` | UTC, `datetime2(3)` |
| `ActorEmployeeId` | the signed-in person's id (`sub` claim) | no foreign key on purpose |
| `Action` | `AccessDenied` | new value, free-text column so no migration |
| `EntityType` | `Endpoint` | |
| `EntityId` | route pattern, for example `/api/admin/ping` | never the raw URL |
| `DetailJson` | `{"policy":"RequireAdmin"}` | policy name only |
| `CorrelationId` | request trace id | links to the application log |

## Rules

- Rows are only inserted, never updated or deleted by code.
- No name, email, free text, token or request body (security rule 12 and 7).
- Roles are not copied into the row; they are looked up from `people.EmployeeRole` when needed.

## Entities touched (read only)

- `Employee` and `EmployeeRole` are read by 4c to build the token. This feature does not change them.
