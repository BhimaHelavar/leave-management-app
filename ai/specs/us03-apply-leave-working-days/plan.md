# Implementation Plan: Apply for leave with an accurate working-day count

**Branch**: `SCRUM-10-US-03-Apply-for-leave-with-an-accurate-working-day-count` | **Date**: 2026-11-13 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `ai/specs/us03-apply-leave-working-days/spec.md` (Jira SCRUM-10)

## Summary

Add two endpoints and one form.

- `POST /api/me/leave-requests` applies for leave. The server counts the working days, checks the rules, then in **one transaction** saves the request, its day rows, the balance deduction, the ledger entry and the audit row.
- `GET /api/me/leave-requests/preview` returns only the count for the form (US-02 preview). It uses the same calculator, so preview and submit cannot disagree.
- An Apply Leave screen on the web app with the preview.

The counting and the rules live in pure Domain code (`WorkingDayCalculator`, `LeaveRules`) so they are easy to unit test. **No migration**: every table and constraint this story needs already exists (`LeaveRequest`, `LeaveRequestDay` with its unique overlap index, `IdempotencyKey`, `Holiday`, `LeaveBalance` with `CHECK >= 0`, `LeaveBalanceEntry`, `AuditEvent`).

## Technical Context

**Language/Version**: C# 14 on .NET 10; TypeScript on Angular 22 (standalone, Signals, zoneless)

**Primary Dependencies**: EF Core 10, ASP.NET Core minimal APIs. No new NuGet or npm package.

**Storage**: existing tables only: `leave.LeaveRequest`, `leave.LeaveRequestDay`, `leave.IdempotencyKey`, `balances.Holiday`, `balances.LeaveType`, `balances.LeaveBalance`, `balances.LeaveBalanceEntry`, `audit.AuditEvent`, `people.Employee`. No schema change.

**Testing**: Vitest for the web app. There is still no .NET test project (Phase 5), so the back end is verified by the manual checks in `quickstart.md`. The Domain code is pure and takes no clock or database, so it is ready for unit tests later. Say this plainly in the tasks.

**Target Platform**: Windows dev PC (LocalDB), later a restricted Demo environment.

**Project Type**: web application (API + Angular app).

**Performance Goals**: apply and see the new pending figure within 3 seconds (SC-003). The holiday lookup is a range seek on `UX (LocationId, HolidayDate)`; the overlap check uses `UX_LeaveRequestDay_Employee_Date_Slot`.

**Constraints**: the server decides every figure and rule (FR-005). "Now" comes from `TimeProvider`. Leave days are `DateOnly`. Errors are `ProblemDetails` (422 for a rule failure, 409 for a conflict). Balance change is one guarded atomic update plus a ledger entry. Fractions stay exact (`decimal`, 0.5 steps).

**Scale/Scope**: one office, tens of users; two endpoints, two use cases, one screen.

**Dependencies on other specs**:
- `us01-sign-in-by-role` / `4c`: the token and the `sub` claim.
- `us02-see-leave-balance`: shows the new pending figure. It derives pending from ledger `Deduction` entries, so this story must write a `Deduction` entry for every deduction (R7).
- Increment 4e (approve, reject, cancel) is **not** built. Hand-off note for 4e in R6: freeing overlap days.
- Holidays: the `Holiday` table exists and `DevSeeder` seeds some. Check the seeded dates against the quickstart (R8).

## Constitution Check

*GATE: passes before research and after design.*

| Principle | Result |
| --- | --- |
| I Server enforces every rule | Pass: count, notice, maximum, overlap, balance and year rules are all in the API. The form only previews. A count sent by the client does not exist in the request body. |
| II Security by default | Pass: fallback policy requires sign-in. The employee id comes from the token only; the body has no employee id, status or count (over-posting closed). Request DTO is separate from the entity. Audit row written in the same transaction. Audit detail holds ids and numbers, never the reason text. A leave type the caller may not use returns a rule error, not data. |
| III Dependency rule | Pass for layers: pure rules in Domain, use cases and ports in Application, EF in Infrastructure, endpoints in Api. **Exception for the schema-module rule**: one store touches `leave.*`, `balances.*` and `audit.*` in one transaction. The owner approved this exception on 2026-11-13 (option a). Recorded in Complexity Tracking and R2. |
| IV Dates and time | Pass: `DateOnly` for days; today is taken from `TimeProvider` and converted to the person's location time zone (R4). |
| V Migrations append-only | Pass: no migration. |
| VI Spec-driven | Pass once the owner approves this plan and the tasks. |
| VII Verify before done | Applies at implementation. No .NET tests exist yet; said plainly. |
| VIII Ask first | Pass: no package. Open decisions are listed in R3, R5 and R6 for the owner. |

## Project Structure

### Documentation (this feature)

```text
ai/specs/us03-apply-leave-working-days/
├── spec.md
├── plan.md              # this file
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── leave-requests-api.md
├── checklists/requirements.md
└── tasks.md             # created later by /speckit-tasks
```

### Source Code (repository root)

```text
src/
├── LeaveManagement.Domain/
│   ├── WorkingDayCalculator.cs        # new: pure. range + half day + holidays -> list of (date, slot), count
│   └── LeaveRules.cs                  # new: pure. notice, max consecutive, year, half-day checks -> rule result
├── LeaveManagement.Application/
│   └── Leave/
│       ├── ApplyForLeave.cs           # new: use case (validate, count, check rules, call the store)
│       ├── PreviewWorkingDays.cs      # new: use case (same calculator, no write)
│       ├── LeaveDtos.cs               # new: request and response records
│       ├── LeaveRuleError.cs          # new: code + message for a refused request
│       ├── IApplicantReader.cs        # new: port, people.* (location, active)
│       ├── IHolidayReader.cs          # new: port, balances.Holiday for a location and range
│       ├── ILeaveTypeReader.cs        # new: port, balances.LeaveType rules for one type
│       └── ILeaveApplicationStore.cs  # new: port, one transaction: overlap, request, days, deduction, ledger, audit, idempotency
├── LeaveManagement.Infrastructure/
│   └── Persistence/
│       ├── ApplicantReader.cs         # new
│       ├── HolidayReader.cs           # new
│       ├── LeaveTypeReader.cs         # new
│       └── LeaveApplicationStore.cs   # new: explicit transaction, guarded UPDATE, unique-index catch
└── LeaveManagement.Api/
    └── Leave/
        └── LeaveRequestEndpoints.cs   # new: MapLeaveRequestEndpoints -> POST and GET preview
web/src/app/
└── features/
    └── apply-leave/
        ├── apply-leave.ts / .html / .scss / .spec.ts   # new: form, preview, result and error states
        └── leave-request.service.ts / .spec.ts         # new: typed HTTP service
```

Also touched: `Program.cs` (one `MapLeaveRequestEndpoints()` line), `Application/DependencyInjection.cs` and `Infrastructure/DependencyInjection.cs` (register use cases and ports), `web/src/app/layout/nav-items.ts` and `app.routes.ts` (route to the new screen; today every section except the dashboard is a placeholder), and possibly `DevSeeder.cs` (holidays for the quickstart, R8).

**Structure Decision**: follows `ai/steering/structure.md`. The rules are pure Domain functions with no clock or database. Reads go through small ports per module. The single write goes through one store because the deduction must be atomic with the request and the audit row (R2).

## Complexity Tracking

One approved exception to Principle III ("Code in one schema module MUST NOT read another module's tables"). Other judgement calls are in `research.md`: slot rows decide the count (R1), `IdempotencyKey` reuse (R5).

| Violation | Why Needed | Simpler Alternative Rejected Because |
| --- | --- | --- |
| `LeaveApplicationStore` reads and writes `leave.*` and `balances.*` (and appends to `audit.*`) in one transaction | Rule 14 and "days are deducted at submit" need the request, day rows, guarded balance update, ledger entry and audit row to succeed or fail together | Separate per-module stores need a unit-of-work abstraction that is more code than this story justifies. Reads of other modules' reference data (applicant, holidays, leave type) still go through separate ports. **Approved by the owner on 2026-11-13.** |

**Limits of the exception**: it covers this one store only. It does not let other code read across modules. Revisit it in increment 4e, when approve, reject and cancel repeat the same pattern (R2). The owner amended the constitution to 1.2.0 on 2026-11-13 to allow this pattern (Principle III), and the same wording is mirrored in `ai/steering/structure.md`.
