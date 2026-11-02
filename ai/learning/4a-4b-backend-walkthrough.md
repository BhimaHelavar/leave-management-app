# Back-end walkthrough (increments 4a scaffold and 4b database)

Study note for `src\`. Written from a read-through of the code; nothing here was built or run.

## 1. What we built

Four projects in one solution, laid out as Clean Architecture. Only Domain and Infrastructure have real content so far. Application is an empty shell, and Api has one endpoint (`/health`).

```
Api  ->  Infrastructure  ->  Application  ->  Domain
(HTTP)   (EF Core, SQL)      (use cases)      (entities, rules; depends on nothing)
```

Arrows point inward. The Domain `.csproj` is empty on purpose, so the compiler itself stops Domain from using EF Core or ASP.NET.

## 2. File by file

### Solution level
| File | What it is | Why it matters |
| --- | --- | --- |
| `LeaveManagement.slnx` | List of the four projects | One command builds everything in order |
| `Directory.Build.props` | Settings applied to every project: net10.0, Nullable, ImplicitUsings, TreatWarningsAsErrors | Set once, cannot drift between projects. Warnings fail the build. NU1901-1904 (new package vulnerabilities) stay warnings so the build does not break overnight |

### Domain
| File | What it holds |
| --- | --- |
| `Enums.cs` | Role, AccrualMode, LedgerEntryType, HalfDay, LeaveStatus, Slot. Stored as text in the database |
| `People.cs` | Location, Employee (ManagerId, JoinDate, IsActive, ExternalId), EmployeeRole |
| `Balances.cs` | LeaveType (policy as data), Holiday, LeaveBalance (current), LeaveBalanceEntry (ledger) |
| `Leave.cs` | LeaveRequest, LeaveRequestDay (one row per half-day), IdempotencyKey |
| `Messaging.cs` | OutboxMessage, ProcessedMessage, AuditEvent |
| `AssemblyMarker.cs` | Empty class so tests and DI can point at this assembly |

### Application
| File | What it holds |
| --- | --- |
| `DependencyInjection.cs` | `AddApplication()`, empty until 4d. Each layer registers itself so `Program.cs` stays short |

### Infrastructure
| File | What it holds |
| --- | --- |
| `DependencyInjection.cs` | `AddInfrastructure()`: reads the connection string, registers `LeaveDbContext`. Fails fast with a clear message if the string is missing |
| `Persistence\LeaveDbContext.cs` | 13 DbSets, a UTC `datetime2(3)` convention for every DateTime, and auto-loading of all configurations |
| `Persistence\Configurations\*.cs` | Fluent API mapping per schema: keys, lengths, precision, check constraints, indexes, foreign keys (all Restrict) |
| `Persistence\EnumColumn.cs` | `.AsText()` and `InList<T>()`, which builds the CHECK list from the C# enum so it cannot drift |
| `Persistence\UtcDateTimeConverters.cs` | Marks every DateTime read from SQL as UTC (datetime2 has no time zone) |
| `Persistence\DatabaseInitializer.cs` | Migrate, then reference data, then demo data. Development only |
| `Persistence\ReferenceDataSeeder.cs` | Bengaluru location and 7 leave types. Idempotent, runs in every environment |
| `Persistence\DevSeeder.cs` | Fictional people and opening balances for local use only |
| `Persistence\Migrations\*` | Generated: `Initial` (Up/Down), its Designer file, and the model snapshot. Never edit an applied migration |

### Api
| File | What it holds |
| --- | --- |
| `Program.cs` | Startup in order: validate Auth:Mode, register services, migrate (Dev), middleware, `/health` |
| `appsettings.json` | Non-secret settings for all environments |
| `appsettings.Development.json` | LocalDB connection string using Windows login (no password) |
| `Properties\launchSettings.json` | `dotnet run` / F5 profile: port 5000, Development |
| `Dockerfile` | Multi-stage: SDK image builds, small aspnet image runs as non-root |

## 3. How the key ideas work

**Ledger plus balance.** Like a bank statement and a current balance.

```
LeaveBalanceEntry (append-only)           LeaveBalance (fast read)
  Grant      +12.0                         Available = 12.0
  Deduction   -3.0  (request 41)
  Restore     +3.0  (request 41 rejected)
```

**Overlap cannot happen.** `LeaveRequestDay` has one row per half-day, with a unique index on (EmployeeId, LeaveDate, Slot). Two requests racing for the same half-day: the database lets one insert win and rejects the other.

**Outbox.** Approval and the "email needed" row are saved in one transaction. A worker sends later. No lost emails, no emails for rolled-back approvals.

**Two lines of defence.** Rules are checked in C# and again as CHECK constraints or unique indexes.

**Config layering.** `appsettings.json`, then `appsettings.{Env}.json`, then user-secrets, then environment variables. Later wins.

## 4. Why these choices, and what was rejected

| Choice | Rejected alternative | Trade-off |
| --- | --- | --- |
| 4 projects (Clean Architecture) | One project | More ceremony; worth it once real rules exist |
| Enums as text | Enums as ints | A few more bytes; readable data and safe reordering |
| No cascade delete, soft delete | Cascade | Extra `IsActive` checks; history survives |
| Overlap via unique index | Overlap check in C# only | One more table; no race window |
| DTOs, not entities, over the wire | Return entities | More classes; no over-posting or accidental leaks |
| Migrations at startup in Dev only | Always at startup | Extra deploy step; production login needs no schema rights |
| Fluent API in Infrastructure | Attributes on entities | More code; Domain stays free of EF |

## 5. When to use or avoid

- Clean Architecture: use when business rules are the valuable part. Avoid for a small CRUD tool.
- Outbox: use when a DB change must trigger a message reliably. Avoid if losing the message is harmless.
- Ledger table: use when you must answer "why is it this number". Avoid for a plain counter.
- Filtered and covering indexes: use for one hot, known query. Avoid as a default on every column.

## 6. Known gaps found during this read-through

- `Program.cs` (`/health`) uses `DateTimeOffset.UtcNow` and `DevSeeder.cs` uses `DateTime.UtcNow`. The project rule says to use `TimeProvider`. Fix when 4d starts (time-based logic arrives).
- `web\src\index.html` title is still "Web".
- `ai\specs\INDEX.md` lists 4c as "Requirements", but `design.md` and `tasks.md` already exist in the 4c folder. Confirm their status before relying on the index.

## 7. Interview Q&A

1. **Why Clean Architecture?** Business rules stay independent of the database and web framework, so they are testable and swappable. The cost is more projects.
2. **How do you stop overlapping leave under concurrency?** A unique index on (employee, date, slot). The database settles the race.
3. **Why a ledger and a balance table?** Ledger for audit and explanation, balance for fast reads, a reconciliation query to compare them.
4. **What is `RowVersion`?** Optimistic concurrency: a stale update fails instead of overwriting.
5. **Why enums as strings?** Readable data; reordering the enum cannot corrupt old rows.
6. **Why no cascade delete?** Leave and audit history must survive. Use soft delete.
7. **What is the outbox pattern?** Save the message with the business change in one transaction; a worker sends it later.
8. **Why migrate at startup only in Development?** Least privilege: the production app login should not change the schema.
9. **What is a filtered index?** An index over rows matching a condition, such as only `Pending`. Smaller and faster.
10. **Why `TimeProvider`?** So tests can control the clock, for example to test a carry-forward expiry date.

## 8. Teach-back (answer these yourself)

1. Draw the four projects and explain why the Domain `.csproj` is empty.
2. Explain to a non-developer why `LeaveRequestDay` has one row per half-day.
3. Walk through `Program.cs` line by line for a browser call to `/health`.

## 9. Exercise (hands-on, in SSMS)

Connect to `(localdb)\MSSQLLocalDB`, open the `LeaveManagement` database, then:

1. Try `UPDATE balances.LeaveBalance SET Available = -1 WHERE Id = 1` and read the error.
2. Insert two `leave.LeaveRequestDay` rows with the same employee, date and slot, and watch the unique index stop the second.
3. Write the reconciliation query yourself: sum `LeaveBalanceEntry.Days` per employee, type and year, and compare with `LeaveBalance.Available`.

Do not run the exercises against any database you care about; use the local dev one.

## 10. Rebuilding this shape without AI (order of decisions)

1. Create the solution and four projects; wire references inward only.
2. Add `Directory.Build.props` (target framework, nullable, warnings as errors).
3. Model Domain on paper first: nouns become classes, fixed choices become enums.
4. Add EF Core SQL Server to Infrastructure; write the DbContext and one configuration per entity. For each rule ask "can the database enforce this too?"
5. Create the first migration, read its SQL, apply it.
6. In Program.cs add DI registration, ProblemDetails, CORS, security headers and `/health`. Run it.
7. Only then build features one use case at a time: DTO, validation, use case, endpoint, tests.
