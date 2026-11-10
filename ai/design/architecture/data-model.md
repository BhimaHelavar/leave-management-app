# Data model

Derived from `src\LeaveManagement.Domain` and the EF configurations. Five schemas: `people`, `balances`, `leave`, `notify`, `audit`. Every foreign key is `Restrict` (no cascade, no hard delete). Only key fields and a few important columns are shown.

```mermaid
erDiagram
    Location ||--o{ Employee : "has"
    Employee |o--o{ Employee : "manages (ManagerId)"
    Employee ||--o{ EmployeeRole : "holds"
    Location ||--o{ Holiday : "has"

    Employee ||--o{ LeaveBalance : "owns"
    LeaveType ||--o{ LeaveBalance : "of"
    Employee ||--o{ LeaveBalanceEntry : "ledger"
    LeaveType ||--o{ LeaveBalanceEntry : "of"
    LeaveRequest |o--o{ LeaveBalanceEntry : "RequestId"

    Employee ||--o{ LeaveRequest : "applies"
    LeaveType ||--o{ LeaveRequest : "of"
    Employee |o--o{ LeaveRequest : "decides (DecidedByEmployeeId)"
    LeaveRequest ||--o{ LeaveRequestDay : "covers"
    Employee ||--o{ IdempotencyKey : "owns"

    Location {
        int Id PK
        string Name UK
        string TimeZoneId
    }
    Employee {
        int Id PK
        string Email UK
        string ExternalId UK "nullable"
        int LocationId FK
        int ManagerId FK "nullable"
        date JoinDate
        bool IsActive
    }
    EmployeeRole {
        int EmployeeId PK
        string Role PK "Manager, HR, Admin"
    }
    LeaveType {
        int Id PK
        string Code UK
        string AccrualMode
        bool IsActive
    }
    Holiday {
        int Id PK
        int LocationId FK
        date HolidayDate "unique with LocationId"
    }
    LeaveBalance {
        int Id PK
        int EmployeeId FK
        int LeaveTypeId FK
        short LeaveYear "unique with the two ids"
        decimal Available
        rowversion RowVersion
    }
    LeaveBalanceEntry {
        long Id PK
        int EmployeeId FK
        int LeaveTypeId FK
        short LeaveYear
        string EntryType
        decimal Days "signed"
        int RequestId FK "nullable"
    }
    LeaveRequest {
        int Id PK
        int EmployeeId FK
        int LeaveTypeId FK
        date StartDate
        date EndDate
        string HalfDay
        decimal DaysCount "snapshot"
        string Status
        rowversion RowVersion
    }
    LeaveRequestDay {
        int RequestId PK
        date LeaveDate PK
        string Slot PK "AM, PM"
        int EmployeeId "copy for overlap index"
    }
    IdempotencyKey {
        int EmployeeId PK
        string KeyValue PK
        string RequestHash
    }
```

## Tables with no foreign keys (by design)

```mermaid
erDiagram
    OutboxMessage {
        long Id PK
        guid MessageId UK
        string Type
        string Payload "ids only"
        datetime SentAtUtc "nullable"
    }
    ProcessedMessage {
        string Consumer PK
        guid MessageId PK
    }
    AuditEvent {
        long Id PK
        int ActorEmployeeId "no FK on purpose"
        string Action
        string EntityType
        string EntityId
        string DetailJson "ids and values only"
    }
```

## Schema ownership

| Schema | Tables |
| --- | --- |
| `people` | Location, Employee, EmployeeRole |
| `balances` | LeaveType, Holiday, LeaveBalance, LeaveBalanceEntry |
| `leave` | LeaveRequest, LeaveRequestDay, IdempotencyKey |
| `notify` | OutboxMessage, ProcessedMessage |
| `audit` | AuditEvent |

## Rules the database holds

- `LeaveRequestDay` has a composite foreign key `(RequestId, EmployeeId)` to `LeaveRequest` and a unique index on `(EmployeeId, LeaveDate, Slot)` so overlapping leave is impossible.
- `LeaveBalance` is unique per `(EmployeeId, LeaveTypeId, LeaveYear)`; `CK_LeaveBalance_Available` keeps `Available >= 0`.
- `LeaveRequest` checks: `DaysCount > 0`; a half day needs `StartDate = EndDate`; `DecidedAtUtc` is set exactly when `Status` is not `Pending`.
- `LeaveRequest.LeaveYear` is a stored computed column from `StartDate`.
- Enums are stored as text with a `CHECK` constraint.
- Audit and outbox rows hold ids and values, never names, emails or free text.

Checked against the EF configuration code (tables, schemas, keys, foreign keys, unique indexes, check constraints). Not checked: the generated migration SQL, and Mermaid rendering (no renderer available here; preview the file in VS Code or GitHub).
