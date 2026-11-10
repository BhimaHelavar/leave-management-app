# Quickstart: validating the shell

Run from `C:\Projects\leave-management` in PowerShell or Command Prompt. Items marked (Azure) need the deployed
environment and cannot be run until the deployment tasks are done.

## Local

1. Start the API from one place only (see `tech.md`): `dotnet run --project src\LeaveManagement.Api`
2. In `web`: `npm install`, then `npm start`; open `http://localhost:4200`.
3. Expect: top bar with "Leave Management", links for the stub person's roles, a user menu, and the health card on the dashboard showing Healthy, `Dev`, `Connected`, `Development`.

## Scenarios (map to spec stories)

| # | Do | Expect |
|---|---|---|
| 1 (US1) | Switch the stub person between Employee, Manager, HR, Admin (local `config.json` setting) | Links match `contracts/navigation.md`; Audit shows once for HR+Admin |
| 2 (US1) | Open `/approvals` as an Employee | Not-found view with "Back to dashboard" |
| 3 (US1) | Open `/nothing-here` | Not-found view |
| 4 (US2) | Stop the API, reload | Health card shows an error and "Check again"; top bar still works |
| 5 (US2) | Start the API, press "Check again" | Card shows Healthy without page reload |
| 6 (US3) | Run the API with `ASPNETCORE_ENVIRONMENT=Production` and `Auth__Mode=Dev` | API refuses to start with a clear message |
| 7 (US3) | Run with `ASPNETCORE_ENVIRONMENT=Demo` and `Auth__Mode=Dev` | API starts; no migration or seed runs |
| 8 (Edge) | Remove `config.json` | Configuration error page, not a blank screen |
| 9 (Edge) | Browser width 320 px | No horizontal scroll; links collapse to a menu |
| 10 (A11y) | Keyboard only: Tab through the page | Skip link first; visible focus; menu opens with Enter |

## Automated

- `dotnet build LeaveManagement.slnx`
- In `web`: `npm test -- --watch=false` and `npm run build` (both must pass; CI runs the same)

## (Azure)

| # | Do | Expect |
|---|---|---|
| A1 | Open the Static Web Apps address from your own network | Shell loads; health card Healthy, environment `Demo` |
| A2 | Open the API address from a network not on the allow list | Blocked (403) |
| A3 | Trigger the test budget alert | Email arrives (SC-005) |
| A4 | Search the repo and the published web files for secrets and `devStub` | None found |

## Not verified by this document
Free-tier limits, .NET 10 availability on App Service and the exact budget amount are assumptions until task T03 checks them.
