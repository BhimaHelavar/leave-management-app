# Implementation Plan: Block requests that break my balance or overlap

**Branch**: `SCRUM-12-US-05-Block-requests-that-break-my-balance-or-overlap` | **Date**: 2026-11-14 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `ai/specs/us05-block-invalid-requests/spec.md` (Jira SCRUM-12)

## Summary

A small back-end change on top of US-03, plus an end-to-end check.

- When a request overlaps the person's own pending or approved request, the refusal message **names the clashing dates** (only the dates inside the requested range, sorted, at most 10 shown, then "and N more").
- The refusal **code stays `leave.overlap`** and the status stays **409**. The balance refusal and every other rule are untouched.
- The apply screen already shows the server's message text, so **no web change** is needed.
- Where the dates come from: the store already reads the person's taken day rows before it saves (`LeaveApplicationStore`). It will return those dates in the refusal instead of throwing them away. The race path (two submits at once, settled by the unique index) re-reads the taken dates after the rollback.

**No migration. No new package. No new endpoint, field or DTO.** One Application helper changes shape, one Infrastructure store changes, one contract table row changes.

## Technical Context

**Language/Version**: C# 14 on .NET 10 (back end). TypeScript on Angular 22 is untouched.

**Primary Dependencies**: EF Core 10, ASP.NET Core minimal APIs. No new NuGet or npm package.

**Storage**: existing tables only. Reads `leave.LeaveRequestDay` (already read by the store). No schema change (see [data-model.md](data-model.md)).

**Testing**: There is still no .NET test project (Phase 5), so the back end is verified by the manual calls in [quickstart.md](quickstart.md), and every report must say so. The message builder is a pure function in Application so it is ready for unit tests later. The web screen already has Vitest cases for the overlap refusal; one case is extended to show a message that names dates.

**Target Platform**: Windows dev PC (LocalDB); API on `http://localhost:5000`.

**Project Type**: web application (API + Angular app). This story touches `src/` only (plus docs).

**Performance Goals**: none beyond US-03 (apply and see the result within 3 seconds). The date lookup reuses the range query the store already runs, so it adds no query on the normal path.

**Constraints**:
- Server is the authority; the screen only shows the text.
- "Now" comes from `TimeProvider` (unchanged). Dates are `DateOnly`.
- The refusal is `ProblemDetails` with no stack trace or SQL (security rule 10). The message holds only calendar dates of the caller's own requests (security rule 12).
- Nothing is saved on a refusal and the transaction rolls back (US-03 FR-014).

**Scale/Scope**: one message builder, one store method, one contract row, tests by hand.

**Dependencies on other specs**: `us03-apply-leave-working-days` (merged, PR #5). Increment 4e (approve, reject, cancel) is not built; see research R5.

## Constitution Check

*GATE: passes before research and again after design.*

| Principle | Result | Why |
| --- | --- | --- |
| I. Server enforces every rule | Pass | The rule and the dates both come from the server. |
| II. Security by default | Pass | No endpoint, role or token change. Only the caller's own rows are read (filtered by employee id). Message holds dates only. |
| III. Dependency rule and modules | Pass | Application builds the message; Infrastructure supplies the dates. No new cross-module read: the store is the one named store (US-03) and already reads `leave.*`. |
| IV. Dates and time | Pass | `DateOnly` end to end. No clock use added. |
| V. Migrations append-only | Pass | None needed. |
| VI. Spec-driven | Pass | Spec approved before this plan; tasks come next and need approval. |
| VII. Verify before done | Pass | Build, web tests and the quickstart run are listed. .NET tests are not available (stated). |

No violations, so the Complexity Tracking table is not needed.

## Project Structure

### Documentation (this feature)

```text
ai/specs/us05-block-invalid-requests/
├── spec.md
├── plan.md              # This file
├── research.md          # Phase 0
├── data-model.md        # Phase 1
├── quickstart.md        # Phase 1
├── contracts/
│   └── overlap-refusal.md   # Phase 1: the one changed message
├── checklists/requirements.md
└── tasks.md             # Next step: /speckit-tasks
```

### Source Code (files this story is expected to touch)

```text
src/LeaveManagement.Application/Leave/
└── LeaveRuleError.cs        # Overlap(...) takes the clashing dates and builds the message (pure function)

src/LeaveManagement.Infrastructure/Persistence/
└── LeaveApplicationStore.cs # keep the clashing dates from the pre-check; re-read them in the race path

web/src/app/features/apply-leave/
└── apply-leave.spec.ts      # extend the overlap case: the shown text names dates (no .ts or .html change)

ai/specs/us03-apply-leave-working-days/contracts/leave-requests-api.md
                             # update the `leave.overlap` message row (see contracts/overlap-refusal.md)
```

Not touched: the endpoint file, `LeavePlanner`, `ApplyForLeave`, the Domain project, migrations, the web screen code.

**Structure Decision**: the wording lives in Application (`LeaveRuleErrors`) with the other messages, as `ai/steering/structure.md` says. Infrastructure only supplies facts (the dates). Domain is not involved.

## Design (how it works)

```text
POST /api/me/leave-requests
        |
        v  LeaveApplicationStore.RunAsync (one transaction)
  1. replay check (idempotency key)                         <- unchanged
  2. read my day rows in [start, end]                       <- already there
        taken = (date, slot) rows I already hold
        clash = taken rows that match a wanted (date, slot)
        |-- clash found -> dates = distinct clash dates, sorted
        |        -> refusal Overlap(dates)   [409 leave.overlap, message names dates]
  3. insert request and day rows
        |-- unique index hit (race) -> rollback
        |        -> replay check, else re-read the taken dates -> Overlap(dates)
  4. guarded balance update, 0 rows -> BalanceInsufficient     <- unchanged
```

The message builder is pure: `Overlap(dates)` returns `You already have a pending or approved leave request on 2026-12-01, 2026-12-02.` With more than 10 dates it shows the first 10, then `and N more`. With an empty list it falls back to the old wording (see research R3). See [research.md](research.md) for the decisions and rejected options.

## Complexity Tracking

Not needed. No constitution violations.
