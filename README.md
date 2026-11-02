# Leave Management

A leave management web app for Employees, Managers, HR and Admins. It is a personal portfolio project built to senior full-stack standards: Angular 22 on the front end, .NET 10 with Clean Architecture on the back end, SQL Server, and a one-command local start.

Status: **Phase 4b (database)**. The API creates and seeds a SQL Server database (13 tables in 5 schemas) and the web app shows the API and database status. Features arrive in later increments (see the roadmap below).

## Run it

Requirements: Docker Desktop (or Docker Engine with Compose).

```bash
cp .env.example .env        # Windows cmd: copy .env.example .env
# edit .env and set MSSQL_SA_PASSWORD (8+ chars, upper, lower, digit, symbol)
docker compose up --build
```

Then open:

| What | URL |
| --- | --- |
| Web app | http://localhost:4200 |
| API health | http://localhost:5000/health |

You should see "API is Healthy" on the web page. The first start takes a few minutes while images download and SQL Server initialises.

## Project layout

| Path | Purpose |
| --- | --- |
| `src/LeaveManagement.Domain` | Entities and business rules. Depends on nothing. |
| `src/LeaveManagement.Application` | Use cases, validation, interfaces. Depends on Domain. |
| `src/LeaveManagement.Infrastructure` | EF Core, SQL Server, identity, later Redis and RabbitMQ. Depends on Application. |
| `src/LeaveManagement.Api` | HTTP endpoints, configuration, security headers. Composition root. |
| `web` | Angular 22 standalone app using Signals. |
| `docker-compose.yml` | API, web and SQL Server for local use. |
| `.github/workflows/ci.yml` | Builds and tests on every push and pull request. |

Dependencies point inward: Api and Infrastructure depend on Application, Application depends on Domain, Domain depends on nothing.

## Run without Docker (Windows, SQL Server LocalDB)

Requirements: .NET 10 SDK, Node 24, SQL Server LocalDB (installs with Visual Studio), and the EF Core tool.

```bash
# One time: install the EF Core command line tool
dotnet tool install --global dotnet-ef

# One time (and again whenever the model changes): create the migration
dotnet ef migrations add Initial --project src/LeaveManagement.Infrastructure --startup-project src/LeaveManagement.Api --output-dir Persistence/Migrations

# API: in Development it applies migrations and seeds data on startup
dotnet run --project src/LeaveManagement.Api       # http://localhost:5000

# Web (second terminal)
cd web
npm install
npm start                                          # http://localhost:4200
```

The API connects to `(localdb)\MSSQLLocalDB`, database `LeaveManagement` (see `appsettings.Development.json`). Open it in SSMS with Windows Authentication.

Commit the generated `src/LeaveManagement.Infrastructure/Persistence/Migrations` folder. Read the SQL before you apply a migration: `dotnet ef migrations script`. Never edit a migration that has been applied; add a new one.

## Database

13 tables in 5 schemas, one schema per module: `people`, `balances`, `leave`, `notify`, `audit`. The design (constraints, indexes, the balance ledger) is in the Phase 3c document.

Demo data (Development only): six fictional people (a manager with three team members, one HR user, one admin), opening balances, and a few fixed-date holidays. Reference data (the Bengaluru location and seven leave types) is added in every environment.

Check that each balance row agrees with its ledger. This must return no rows:

```sql
SELECT b.EmployeeId, b.LeaveTypeId, b.LeaveYear, b.Available
FROM balances.LeaveBalance b
WHERE b.Available <> (SELECT COALESCE(SUM(e.Days), 0)
                      FROM balances.LeaveBalanceEntry e
                      WHERE e.EmployeeId = b.EmployeeId
                        AND e.LeaveTypeId = b.LeaveTypeId
                        AND e.LeaveYear = b.LeaveYear);
```

## Configuration

| Setting | Where | Default | Meaning |
| --- | --- | --- | --- |
| `Auth:Mode` | API config or env `Auth__Mode` | `Dev` | `Dev` uses local test tokens (Phase 4c); `EntraId` uses Microsoft Entra ID (later). |
| `Cors:AllowedOrigin` | API config or env `Cors__AllowedOrigin` | `http://localhost:4200` | The one browser origin allowed to call the API. |
| `ConnectionStrings:Default` | API config or env `ConnectionStrings__Default` | LocalDB (Development) | The database connection. Compose sets it for the SQL Server container. |
| `MSSQL_SA_PASSWORD` | `.env` | none, required for Docker | SQL Server administrator password. Never commit `.env`. |

## Roadmap

| Step | Increment |
| --- | --- |
| 4a | Scaffold, Docker, CI |
| 4b | Database: EF Core entities, migrations, seed data (this step) |
| 4c | Dev authentication, JWT validation, login screen |
| 4d | Apply for and view own leave |
| 4e | Approval workflow, audit log, concurrency |
| 4f | Balances, holidays, team calendar, HR and Admin screens |
| 4g | Redis cache, RabbitMQ notifications, Entra ID mode |

Design documents for phases 1 to 3 (requirements, UI/UX, high-level design, database, API, class, Angular and security design) live in Confluence and the project's design docs.
