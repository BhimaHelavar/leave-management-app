# Implementation Plan: Apply for a half day

**Branch**: `SCRUM-11-US-04-Apply-for-a-half-day` | **Date**: 2026-11-14 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `ai/specs/us04-apply-half-day/spec.md` (Jira SCRUM-11)

## Summary

A small, front-end-only change on top of US-03.

- On the Apply for leave screen, the "Length of the day" choice is **always rendered**. It is **disabled** with a visible hint when the start and end dates are not the same day, and enabled when they are.
- Everything else the story needs (0.5 count, refusal of a half day on a range, weekend, holiday or a type that does not allow it) already exists in the server code from US-03 and is **not touched**.
- The work is one template change, no new logic in the component beyond a hint, updated Vitest specs, and an end-to-end check against the running API (quickstart).

**No back-end change. No migration. No new package. No new endpoint or DTO.**

## Technical Context

**Language/Version**: TypeScript on Angular 22 (standalone, Signals, zoneless). C# 14 / .NET 10 is untouched.

**Primary Dependencies**: none new. Uses the existing `apply-leave` screen and `LeaveRequestService`.

**Storage**: N/A. No new data (see [data-model.md](data-model.md)).

**Testing**: Vitest (`npm test -- --watch=false`) for the screen. Server behaviour is checked by the manual calls in [quickstart.md](quickstart.md), because there is still no .NET test project (Phase 5). Say this plainly in the tasks and in the final report.

**Target Platform**: Browser, served by `npm start` on `http://localhost:4200`; API on `http://localhost:5000`.

**Project Type**: web application (API + Angular app). This story touches `web/` only.

**Performance Goals**: none beyond US-03. The disabled state is a pure signal derivation, so it updates in the same render as the date change.

**Constraints**:
- The server stays the authority (constitution I). Disabling a control is convenience, not security.
- Labels on inputs and visible state (front-end rules). The disabled control must keep its label and be linked to its hint for assistive technology (FR-007).
- No `innerHTML`, no `any`, new template control flow only.

**Scale/Scope**: one screen, one template block, one component spec file.

**Dependencies on other specs**: `us03-apply-leave-working-days` (merged, PR #5). Its screen, preview and server rules are the base.

## Constitution Check

*GATE: passes before research and again after design.*

| Principle | Result | Why |
| --- | --- | --- |
| I. Server enforces every rule | Pass | No rule moves to the screen. FR-006 keeps the server refusals, and the quickstart proves them with the screen bypassed. |
| II. Security by default | Pass | No endpoint, token, role or data change. |
| III. Dependency rule | Pass | `web/` only. No cross-module store. |
| IV. Dates and time | Pass | No date logic added. Dates stay plain `yyyy-MM-dd` strings. |
| V. Migrations append-only | Pass | None needed. |
| VI. Spec-driven | Pass | Spec approved by the owner before this plan; tasks come next and need approval. |
| VII. Verify before done | Pass | Web build, web tests and the quickstart run are listed. .NET tests are not available (stated). |

No violations, so the Complexity Tracking table is not needed.

## Project Structure

### Documentation (this feature)

```text
ai/specs/us04-apply-half-day/
├── spec.md
├── plan.md              # This file
├── research.md          # Phase 0
├── data-model.md        # Phase 1
├── quickstart.md        # Phase 1
├── contracts/
│   └── apply-screen-half-day.md   # Phase 1: the screen contract (API contract is unchanged)
├── checklists/requirements.md
└── tasks.md             # Next step: /speckit-tasks
```

### Source Code (files this story is expected to touch)

```text
web/src/app/features/apply-leave/
├── apply-leave.html        # always render the half-day field; disabled + hint when not a single day
├── apply-leave.ts          # (only if needed) a hint id/text constant
├── apply-leave.scss        # a small `.hint` rule under `.field` (the existing one is nested under `.preview`)
└── apply-leave.spec.ts     # replace the "hidden" test; add disabled, reset, enabled-again, submit-as-full-day tests
```

Not touched: everything under `src/` (API, Application, Domain, Infrastructure), `leave-request.service.ts`, and the API contract `ai/specs/us03-apply-leave-working-days/contracts/leave-requests-api.md`.

**Structure Decision**: keep the change inside the existing `apply-leave` feature folder, as `ai/steering/structure.md` says (one folder per screen). No new files in `web/` are needed.

## Design (how it works)

```text
start/end dates (signals)
        |
        v
canChooseHalfDay = start != '' && start == end        <- existing computed, reused
        |
        +--> template: <select id="half-day" [disabled]="!canChooseHalfDay()"
        |                aria-describedby="half-day-hint">
        |       hint <p id="half-day-hint"> visible while disabled
        |
        +--> afterDatesChanged(): if !canChooseHalfDay -> halfDay = 'None'   <- existing, reused
        |
        +--> submit()/preview(): halfDay = canChooseHalfDay ? halfDay : 'None' <- existing, reused
```

The existing component already derives, resets and sends the right value. The only behavioural change is in the template: the field stops being removed and becomes disabled instead. See [research.md](research.md) for the decisions and rejected options.

## Complexity Tracking

Not needed. No constitution violations.
