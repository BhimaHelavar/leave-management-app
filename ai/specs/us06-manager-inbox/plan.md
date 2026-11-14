# Implementation Plan: Review pending requests in a manager inbox

**Branch**: `SCRUM-13-US-06-Review-pending-requests-in-a-manager-inbox` | **Date**: 2026-11-14 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `ai/specs/us06-manager-inbox/spec.md` (Jira SCRUM-13)

## Summary

One read-only endpoint and one screen.

- `GET /api/approvals?page=&pageSize=` returns the signed-in manager's direct reports' **pending** requests, soonest start date first, a page at a time. Each item carries the person, leave type, dates, day count, the balance before and after, and the **team overlap** (who else is away on the same days, and "N of team size away", requester included).
- The Manager role policy (`RequireManager`, already defined) guards the endpoint. The manager's reports are **looked up on the server** each time. Nothing about the team comes from the token or the browser.
- An **Approvals** screen replaces the placeholder behind the existing nav item `approvals` (roles: Manager). It shows loading, empty, error and paging states. It has no Approve or Reject button: that is increment 4e.
- The overlap rule is pure Domain code (`TeamOverlap`), like `WorkingDayCalculator` was for US-03, so it is easy to unit test later.

Three modules are involved (people, leave, balances), so the use case composes **three read ports** instead of one cross-module query. This keeps the constitution's module rule intact.

**No migration. No new package. No write of any kind.**

## Technical Context

**Language/Version**: C# 14 on .NET 10; TypeScript on Angular 22 (standalone, Signals, zoneless).

**Primary Dependencies**: EF Core 10, ASP.NET Core minimal APIs. No new NuGet or npm package.

**Storage**: existing tables, read only: `people.Employee`, `leave.LeaveRequest`, `leave.LeaveRequestDay`, `balances.LeaveBalance` (through the existing `IBalanceReader`). No schema change (see [data-model.md](data-model.md)).

**Testing**: Vitest for the new screen and service (success path and a failure path each). There is still no .NET test project (Phase 5), so the back end is verified by the manual calls in [quickstart.md](quickstart.md), and every report must say so. `TeamOverlap` and the response shaping are pure so unit tests can be added in Phase 5.

**Target Platform**: Windows dev PC (LocalDB); API `http://localhost:5000`, web `http://localhost:4200`.

**Project Type**: web application (API + Angular app).

**Performance Goals**: up to 20 items in under 2 seconds (SC-005). Queries: one for the reports, one page of requests plus a count, one for the team's held day rows in the page's date window, and one balance read per distinct person on the page. A team is small, so this is a handful of queries (research R4).

**Constraints**:
- The server decides who sees what (security rules 1, 3, 4). Role policy first, then the resource rule "only my direct reports".
- The manager is read from the token's `sub`; the **team is read from the database** (security rule 7, FR-009).
- Names appear only to the person's manager. No leave reason, no other people's balances, no one outside the team (security rule 12, FR-011).
- Errors are `ProblemDetails`, no stack traces or SQL. Dates are `DateOnly`, "now" from `TimeProvider`.
- Reading changes nothing and writes no audit row (FR-013).

**Scale/Scope**: one endpoint, one use case, three ports, one Domain function, one screen, one service.

**Dependencies on other specs**: `us01` (sign-in, roles in the token), `us03` (pending requests exist, day rows exist), `us02` (balance reader). Increment 4e (approve, reject, cancel) is not built; it must take a decided request out of the pending state (spec Assumptions).

## Constitution Check

*GATE: passes before research and again after design.*

| Principle | Result | Why |
| --- | --- | --- |
| I. Server enforces every rule | Pass | The team, the filter, the overlap and the numbers all come from the server. The screen only shows them. |
| II. Security by default | Pass | Fallback policy (signed in) plus the `RequireManager` role policy, then the resource rule (direct reports only). Non-manager gets 403 for every caller alike. No new anonymous endpoint. Dev-mode switch is not read by business code. |
| III. Dependency rule and modules | Pass | Three ports, one per module, each reading only its own schema. The use case in Application composes them. No cross-module write, so no new named store and no constitution change. |
| IV. Dates and time | Pass | `DateOnly` for leave days. `TimeProvider` for "today". |
| V. Migrations append-only | Pass | None needed. |
| VI. Spec-driven | Pass | Spec approved by the owner before this plan; tasks come next and need approval. |
| VII. Verify before done | Pass | Build, web tests and the quickstart run are listed. .NET tests are not available (stated). |

No violations, so the Complexity Tracking table is not needed.

## Project Structure

### Documentation (this feature)

```text
ai/specs/us06-manager-inbox/
├── spec.md
├── plan.md              # This file
├── research.md          # Phase 0
├── data-model.md        # Phase 1
├── quickstart.md        # Phase 1
├── contracts/
│   └── manager-inbox-api.md   # Phase 1
├── checklists/requirements.md
└── tasks.md             # Next step: /speckit-tasks
```

### Source Code (files this story is expected to touch)

```text
src/LeaveManagement.Domain/
└── TeamOverlap.cs                       # new: pure overlap and team-away rule

src/LeaveManagement.Application/Approvals/
├── ApprovalDtos.cs                      # new: response records
├── IDirectReportReader.cs               # new: people port
├── IApprovalRequestReader.cs            # new: leave port (page of pending requests, held slots)
├── GetManagerInbox.cs                   # new: the use case (composes the ports and the balance reader)
src/LeaveManagement.Application/DependencyInjection.cs   # register GetManagerInbox

src/LeaveManagement.Infrastructure/Persistence/
├── DirectReportReader.cs                # new: reads people.Employee only
└── ApprovalRequestReader.cs             # new: reads leave.* only
src/LeaveManagement.Infrastructure/DependencyInjection.cs  # register the two readers

src/LeaveManagement.Api/Approvals/
└── ApprovalEndpoints.cs                 # new: MapApprovalEndpoints, RequireManager policy
src/LeaveManagement.Api/Program.cs       # map the endpoints (the one line the other Map... calls use)

web/src/app/features/approvals/
├── approvals.ts / approvals.html / approvals.scss / approvals.spec.ts   # new screen
└── approvals.service.ts / approvals.service.spec.ts                      # new typed service
web/src/app/app.routes.ts                # loaderFor('approvals') opens the new screen (+ its spec)
```

Not touched: the migrations, the apply endpoints, the balances use case, the nav table (`approvals` already exists for the Manager role).

**Structure Decision**: a new `Approvals` folder in Application and Api, as `ai/steering/structure.md` says (use case and DTOs in Application, endpoints per feature in Api). Ports live in Application, readers in `Infrastructure/Persistence`, the pure rule in Domain.

## Design (how it works)

```text
GET /api/approvals?page=1&pageSize=20     (Bearer token, role Manager)
   |
   v  ApprovalEndpoints  (policy RequireManager; sub -> managerId; validate page, pageSize)
   v  GetManagerInbox.ExecuteAsync(managerId, page, pageSize)
        1. reports = IDirectReportReader.GetAsync(managerId)         people: active reports, not self
              empty -> return an empty page
        2. page    = IApprovalRequestReader.GetPendingPageAsync(reportIds, page, size)
              Status = Pending, ORDER BY StartDate, SubmittedAtUtc, Id, OFFSET/FETCH, plus totalCount
        3. held    = IApprovalRequestReader.GetHeldSlotsAsync(reportIds, minStart, maxEnd)
              (RequestId, EmployeeId, Date, Slot) for the whole team in the page's window
        4. balances= IBalanceReader.GetLedgerAsync(person, year, today) once per person and year on the page
        5. per item: TeamOverlap.For(request, held, reports)  -> others + dates, awayIfApproved, teamSize
                     balance before/after from the ledger and the request's days
   v  200 { items, page, pageSize, totalCount }
```

See [research.md](research.md) for the decisions and the rejected options, and [contracts/manager-inbox-api.md](contracts/manager-inbox-api.md) for the exact shapes.

## Complexity Tracking

Not needed. No constitution violations.
