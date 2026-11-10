# Quickstart: check Sign in and see only what my role allows

Run after the tasks are done. Windows PowerShell, from the repo root. Prerequisites: specs 4c and T-02 implemented, LocalDB available. Run the API from **one** place only.

## 1. Start

```powershell
dotnet run --project src\LeaveManagement.Api
```

In a second window:

```powershell
cd web
npm start
```

## 2. API checks (SCRUM-8 second criterion)

Get a token for each demo person with `POST /api/dev/token` (see 4c contracts), then:

```powershell
$h = @{ Authorization = "Bearer $token" }
Invoke-WebRequest http://localhost:5000/api/admin/ping -Headers $h -SkipHttpErrorCheck | Select-Object StatusCode
```

| As | Expected |
| --- | --- |
| no header | 401 |
| Employee | 403 |
| Manager or HR | 403 |
| Admin | 204 |

## 3. Audit check

In SSMS or `sqlcmd` against `(localdb)\MSSQLLocalDB`, database `LeaveManagement`:

```sql
SELECT TOP 5 Id, OccurredAtUtc, ActorEmployeeId, Action, EntityType, EntityId, DetailJson, CorrelationId
FROM audit.AuditEvent ORDER BY Id DESC;
```

Expect one `AccessDenied` row per 403 above, none for 401 or 204, and no name or email in any column.

## 4. Screen checks (SCRUM-8 first criterion)

Open `http://localhost:4200`. Sign in as each person and compare the top bar with `ai/specs/t02-angular-shell/contracts/navigation.md`:

| Person | Links expected |
| --- | --- |
| Employee only | Dashboard, My requests |
| Manager | plus Approvals |
| HR | plus Reports, Exceptions, Audit |
| Admin | plus Leave types, Holidays, Audit |
| HR and Admin | Audit shown once |

After sign-in you land on the dashboard. Reload the page: you are signed out.

## 5. Automated

```powershell
cd web
npm test -- --watch=false
cd ..
dotnet build LeaveManagement.slnx
```

Back-end automated tests do not exist until Phase 5, so steps 2 and 3 are the back-end check.
