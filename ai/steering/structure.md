# Structure

```
leave-management/
  CLAUDE.md                 short rules, imports ai/steering
  ai/                       steering, specs, decisions, learning (see ai/README.md)
  .claude/                  agents, skills, rules, settings
  src/
    LeaveManagement.Domain/          entities, enums, business rules. Depends on nothing
    LeaveManagement.Application/     use cases, validation, interfaces. Depends on Domain
    LeaveManagement.Infrastructure/  EF Core, SQL Server, seeders. Depends on Application
      Persistence/                   DbContext, Configurations/, Migrations/, seeders
    LeaveManagement.Api/             endpoints, auth, config, headers. The composition root
      Auth/                          JWT setup, dev token service, auth endpoints
  web/                      Angular app (src/app/...)
  docker-compose.yml, .github/workflows/ci.yml
```

## Dependency rule

Dependencies point inward: Api and Infrastructure -> Application -> Domain. Domain never references EF Core, ASP.NET or any package. If a change needs the reverse, define an interface in Application and implement it in Infrastructure.

## Database schemas (one per module)

`people` (Location, Employee, EmployeeRole), `balances` (LeaveType, Holiday, LeaveBalance, LeaveBalanceEntry), `leave` (LeaveRequest, LeaveRequestDay, IdempotencyKey), `notify` (OutboxMessage, ProcessedMessage), `audit` (AuditEvent). Code in one module does not read another module's tables, even though foreign keys cross schemas.

## Web structure

```
web/src/app/
  auth/            AuthService, interceptor, guard, models
  features/<name>/ one folder per screen: <name>.ts, .html, .scss, .spec.ts
  api-config.ts    API base URL
```

File names follow the Angular 22 style: `login.ts`, `login.html` (no `.component` suffix). Classes are named `Login`, `Home`.

## Where new things go

| New thing | Goes in |
| --- | --- |
| Entity or enum | `Domain` |
| Use case, DTO, validator, interface | `Application` |
| Table mapping, repository, migration | `Infrastructure/Persistence` |
| Endpoint, policy, middleware | `Api` (endpoints grouped per feature in their own file) |
| Screen | `web/src/app/features/<name>` |
