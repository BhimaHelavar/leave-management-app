# Tech stack and commands

## Stack

| Layer | Choice |
| --- | --- |
| Back end | .NET 10, C# 14, ASP.NET Core minimal APIs, Clean Architecture |
| Data | EF Core 10, SQL Server (LocalDB on the owner's PC; SQL Server container in Docker) |
| Front end | Angular 22, standalone components, Signals, zoneless, Vitest for tests |
| Auth | JWT bearer. `Auth:Mode` = `Dev` (local tokens, increment 4c) or `EntraId` (increment 4g) |
| Later | Redis cache and RabbitMQ (4g), GitHub Actions CI, Azure free tier (Phase 6) |

Do not add a NuGet or npm package without asking the owner first. Say what it is for and what the alternative is.

## Environment

- Owner works on **Windows** with Visual Studio 2026, VS Code and SSMS. Commands in docs and answers must work in Command Prompt or PowerShell. Use backslashes in Windows paths and avoid bash-only syntax.
- Node 24, .NET 10 SDK. No Docker Desktop on the owner's PC; `docker-compose.yml` exists for later use.
- Ports: API `http://localhost:5000` (launch profile `http`), web `http://localhost:4200`.
- LocalDB connection string is in `src/LeaveManagement.Api/appsettings.Development.json`: `(localdb)\MSSQLLocalDB`, database `LeaveManagement`.
- Run the API from **one** place only: Visual Studio F5 **or** `dotnet run`. A second copy locks the DLLs and the build fails with "file is locked by LeaveManagement.Api.exe".

## Commands (from the repo root unless noted)

| Task | Command |
| --- | --- |
| Build back end | `dotnet build LeaveManagement.slnx` |
| Run API | `dotnet run --project src\LeaveManagement.Api` |
| Back-end tests (from Phase 5) | `dotnet test LeaveManagement.slnx` |
| Add a migration | `dotnet ef migrations add <Name> --project src\LeaveManagement.Infrastructure --startup-project src\LeaveManagement.Api --output-dir Persistence\Migrations` |
| Read migration SQL | `dotnet ef migrations script --project src\LeaveManagement.Infrastructure --startup-project src\LeaveManagement.Api` |
| Install web packages (in `web`) | `npm install` |
| Run web (in `web`) | `npm start` |
| Web tests once (in `web`) | `npm test -- --watch=false` |
| Build web (in `web`) | `npm run build` |

In Development the API applies migrations and seeds data on startup. Elsewhere migrations run as a separate step.

## Verification rule

Before saying a task is done, run the build and the tests that exist for the files you touched, and report the result. If something cannot be run in the current environment (for example the .NET SDK is missing in a cloud sandbox), say exactly what was not verified.
