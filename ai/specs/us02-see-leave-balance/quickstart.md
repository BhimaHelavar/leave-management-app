# Quickstart: validate US-02 end to end

This is a run-and-check guide. It is not code. Commands work in Command Prompt or PowerShell, from the repo root.

## Prerequisites

- US-01 / 4c sign-in works (dev token for a demo person).
- The demo scenario from research R6 is seeded (needs the owner's decision first).
- API and web run from one place only (Visual Studio F5 **or** `dotnet run`).

## Run

```text
dotnet build LeaveManagement.slnx
dotnet run --project src\LeaveManagement.Api
```

In a second window:

```text
cd web
npm install
npm start
```

## Checks

| # | Do | Expect | Spec item |
| --- | --- | --- | --- |
| 1 | Sign in as the demo person with Annual 10 granted, 2 approved, none pending, open the dashboard | Annual shows 8 available, 0 pending | AC1, SC-002 |
| 2 | Sign in as the demo person with a pending request of 3 days | Annual shows 5 available, 3 pending, 2 used | AC2, SC-002 |
| 3 | Look at the Sick and Casual cards | Each shows its own figures; they do not change when Annual does | Story 1, scenario 3 |
| 4 | Look for Unpaid | No number; text says there is no balance | FR-006 |
| 5 | Sign in as the carry-forward demo person | Annual shows how many days are carry-forward and the 31 March expiry | FR-007 |
| 6 | Call `GET /api/me/balances` with no token | 401 ProblemDetails | contract |
| 7 | Stop the API and reload the dashboard | A clear error message, not zeros | edge case |
| 8 | For every card check `accrued - used - pending = available` | Always true | data-model invariant |
| 9 | Time the dashboard load | All balances visible within 2 seconds | SC-001 |

## Automated checks

```text
cd web
npm test -- --watch=false
npm run build
```

Back-end tests do not exist yet (Phase 5). The derivation function in Domain is written so that unit tests can be added then. Until that happens, checks 1 to 8 are manual, and the final report must say so.

## Not covered

Team balances, past years, any change to balances, and creating requests (4d, 4e, 4f).
