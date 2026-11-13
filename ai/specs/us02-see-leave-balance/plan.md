# Implementation Plan: See my leave balance per leave type

**Branch**: `SCRUM-9-US-02-See-my-leave-balance-per-leave-type` | **Date**: 2026-11-10 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `ai/specs/us02-see-leave-balance/spec.md` (Jira SCRUM-9)

## Summary

Add one read-only endpoint, `GET /api/me/balances`, that returns the signed-in person's balance per leave type for the current leave year: accrued, used, pending, available, and the carry-forward part. Add a dashboard section that shows one card per type. The stored `LeaveBalance.Available` already has pending days taken out (days are deducted at submit), so the endpoint **derives** the other figures from the ledger (`LeaveBalanceEntry`) and from pending requests, and nothing new is stored. No migration.

## Technical Context

**Language/Version**: C# 14 on .NET 10; TypeScript on Angular 22 (standalone, Signals, zoneless)

**Primary Dependencies**: EF Core 10, ASP.NET Core minimal APIs. No new NuGet or npm package.

**Storage**: existing tables `balances.LeaveBalance`, `balances.LeaveBalanceEntry`, `balances.LeaveType`, `leave.LeaveRequest`. Read only. No schema change.

**Testing**: Vitest for the web app. Back-end automated tests arrive in Phase 5 (there is no .NET test project), so the back end is verified by the manual checks in `quickstart.md`. The derivation rules live in a pure Domain function, which is easy to unit test later.

**Target Platform**: Windows dev PC (LocalDB), later a restricted Demo environment

**Project Type**: web application (API + Angular app)

**Performance Goals**: all balances visible in under 2 seconds (SC-001). One person has at most seven balance rows, served by the unique index on (EmployeeId, LeaveTypeId, LeaveYear).

**Constraints**: the server computes every figure; the screen only displays them (FR-010). "Today" and the current leave year come from `TimeProvider`. Errors are `ProblemDetails`. Fractions stay exact (`decimal`).

**Scale/Scope**: one office, tens of users; one endpoint, one use case, one dashboard component.

**Dependencies on other specs**:
- `us01-sign-in-by-role` and `4c-dev-authentication`: the token, the `sub` claim and the dashboard route. The dashboard route is currently a placeholder (`web/src/app/app.routes.ts`).
- 4d (apply for leave) and 4e (approval) do not exist yet, so no request can be created through the app. Verification uses seeded demo data (see research R6).

## Constitution Check

*GATE: passes before research and after design.*

| Principle | Result |
| --- | --- |
| I Server enforces every rule | Pass: figures and the "own balances only" rule are computed on the server. The screen only shows them. |
| II Security by default | Pass: the endpoint needs a signed-in person (fallback policy). The employee id comes from the token, never from the URL, so there is no id to probe (FR-008). Response has no names or emails. |
| III Dependency rule | Pass: ports in Application, EF queries in Infrastructure, endpoint in Api. Balances and leave tables are read through two separate ports (R2). |
| IV Dates and time | Pass: `TimeProvider` for today and the leave year; `DateOnly` for the expiry. |
| V Migrations append-only | Pass: no migration. |
| VI Spec-driven | Pass once the owner approves this plan and the tasks. |
| VII Verify before done | Applies at implementation. No .NET tests exist yet; said plainly in the tasks. |
| VIII Ask first | Pass: no package added. One item needs the owner's decision (R6, demo data). |

## Project Structure

### Documentation (this feature)

```text
ai/specs/us02-see-leave-balance/
├── spec.md
├── plan.md              # this file
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── balances-api.md
├── checklists/requirements.md
└── tasks.md             # created later by /speckit-tasks
```

### Source Code (repository root)

```text
src/
├── LeaveManagement.Domain/
│   └── BalanceSummary.cs                 # new: pure derivation of used / carry-forward from ledger totals
├── LeaveManagement.Application/
│   └── Balances/
│       ├── IBalanceReader.cs             # new: port, reads balances.* (rows + ledger totals)
│       ├── IPendingLeaveReader.cs        # new: port, reads leave.* (pending days per type)
│       ├── GetMyBalances.cs              # new: use case, joins the two ports, returns DTOs
│       └── BalanceDtos.cs                # new: response records
├── LeaveManagement.Infrastructure/
│   └── Persistence/
│       ├── BalanceReader.cs              # new: AsNoTracking queries
│       └── PendingLeaveReader.cs         # new
└── LeaveManagement.Api/
    └── Balances/
        └── BalanceEndpoints.cs           # new: MapBalanceEndpoints -> GET /api/me/balances
web/src/app/
└── features/
    └── dashboard/
        ├── dashboard.ts / .html / .scss / .spec.ts   # new: replaces the placeholder for the dashboard route
        └── balance.service.ts / .spec.ts             # new: typed HTTP service
```

Also touched: `Program.cs` (one `MapBalanceEndpoints()` line), `Application/DependencyInjection.cs` and `Infrastructure/DependencyInjection.cs` (register the use case and readers), `web/src/app/app.routes.ts` (the dashboard path loads the new component), `Infrastructure/Persistence/DevSeeder.cs` or a new idempotent demo top-up (R6).

**Structure Decision**: follows `ai/steering/structure.md`. A use case with two ports keeps the balances module and the leave module from reading each other's tables directly (structure.md rule), while the use case in Application composes them.

## Complexity Tracking

No constitution violations. Two judgement calls are recorded in `research.md`: derive instead of store (R1), and two ports instead of one join (R2).
