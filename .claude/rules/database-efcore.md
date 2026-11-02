---
paths:
  - "src/LeaveManagement.Infrastructure/**/*.cs"
  - "src/LeaveManagement.Domain/**/*.cs"
---

# Database rules (EF Core 10, SQL Server)

- One `IEntityTypeConfiguration<T>` class per entity in `Persistence/Configurations`. Table names are singular, with the module schema (`people`, `balances`, `leave`, `notify`, `audit`).
- Enums are stored as readable text with a `CHECK`. Use `EnumColumn.AsText` and `EnumColumn.InList<T>`, so the check list cannot drift from the C# enum.
- Every foreign key uses `OnDelete(DeleteBehavior.Restrict)`. No cascade deletes, no hard deletes (use `IsActive`).
- Leave days are `date`; timestamps are `datetime2(3)` in UTC (set by the convention in `LeaveDbContext`).
- Tables two people can edit at once get a `RowVersion` (`IsRowVersion`). Handle `DbUpdateConcurrencyException` as 409.
- Balance changes use one guarded atomic update plus a ledger entry in the same transaction. Never read a balance, add in code, and write it back.
- Rules the database must always hold are `CHECK` constraints or unique indexes, in addition to the code check.
- Add an index only for a named screen or job query. Check the generated migration; EF adds foreign-key indexes by convention.
- Reference data goes in `ReferenceDataSeeder` (every environment). Demo people go in `DevSeeder` (Development only). Both are safe to run again.
- Migrations: add with the command in `ai/steering/tech.md`, read the generated SQL, never edit an applied migration, change in two steps (expand then contract) when data exists.
- Never put names, emails or free-text reasons in audit or outbox rows.
