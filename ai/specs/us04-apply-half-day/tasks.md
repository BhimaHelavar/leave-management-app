# Tasks: Apply for a half day

**Input**: Design documents from `ai/specs/us04-apply-half-day/` (Jira SCRUM-11)

**Prerequisites**: spec.md, plan.md, research.md, data-model.md, contracts/apply-screen-half-day.md, quickstart.md. **Owner approval of these tasks is needed** before T001 starts.

**Tests**: Front-end tasks include Vitest specs because `.claude/rules/frontend-angular.md` requires a spec covering a success path and a failure path. There is still no .NET test project (Phase 5) and this story changes no back-end code, so the server is checked only by the manual calls in `quickstart.md`. Every report must say so.

**Organization**: grouped by user story. Do one task at a time, in order. Tick a box only after its verify step passes. All work is in `web/src/app/features/apply-leave/`. Commands work in Command Prompt or PowerShell; run `npm` commands in `web`.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: can run in parallel (different files, no dependency on an unfinished task)
- **[Story]**: US1 or US2 (from spec.md)

## Phase 1: Setup

- [X] T001 Confirm the baseline is green before any change. Verify: in `web`, `npm run format:check`, `npm test -- --watch=false` and `npm run build` succeed. Record any failure that already exists, so it is not blamed on this story. No back-end build is needed because no file under `src` changes, but run `dotnet build LeaveManagement.slnx` once if the API will be started for T008.

---

## Phase 2: Foundational

None. The story reuses the existing `canChooseHalfDay` computed, the reset in `afterDatesChanged`, and the `None` fallback in `submit` and `refreshPreview`. No new shape, port or service is needed.

---

## Phase 3: User Story 1 - See the half-day option disabled for a multi-day range (Priority: P1) MVP

**Goal**: the "Length of the day" choice is always visible; it is disabled with an explained hint unless start and end are the same day (spec FR-001 to FR-004, FR-007; research R2, R3, R4, R5).

**Independent Test**: open the form, pick two different dates, see a disabled choice with the hint; pick one date for both, see it enabled.

- [X] T002 [US1] In `web/src/app/features/apply-leave/apply-leave.html`, stop wrapping the half-day field in `@if (canChooseHalfDay())`. Always render the `<div class="field">` with the label "Length of the day" and `<select id="half-day">` with its three options (Full day `None`, First half `FirstHalf`, Second half `SecondHalf`, each keeping its `[selected]="halfDay() === ..."` binding). Add `[disabled]="!canChooseHalfDay()"` to the select. Below it, inside `@if (!canChooseHalfDay())`, add `<p id="half-day-hint" class="hint">A half day applies to a single day only. Choose the same start and end date to use it.</p>`, and bind `[attr.aria-describedby]="canChooseHalfDay() ? null : 'half-day-hint'"` on the select (contract table in `contracts/apply-screen-half-day.md`). Use the new control flow only; no `innerHTML`. The existing `.hint` style in `apply-leave.scss` is nested under `.preview`, so a small `.hint` rule was added under `.field` (found during T002). Do not change `apply-leave.ts` unless the template cannot work without it, and say so if it must. Verify: `npm run build` succeeds.
- [X] T003 [US1] In `web/src/app/features/apply-leave/apply-leave.spec.ts`, replace the test "offers the half-day choice only when start and end are the same day" (it asserts the select is removed, which is now wrong) with tests that cover FR-001 to FR-004 and FR-007, using the existing `render` and `change` helpers:
  1. Before any date is chosen, `#half-day` exists, is disabled, and `#half-day-hint` is shown.
  2. Start `2026-12-01` and end `2026-12-02`: `#half-day` exists and is disabled, the hint is shown, and the select's `aria-describedby` is `half-day-hint`.
  3. Start and end both `2026-12-01`: `#half-day` is enabled and the hint is not in the page.
  4. Choose `FirstHalf` on a single day, then change the end date to `2026-12-02`: the select is disabled and its displayed value is `None` (this pins research R4).
  5. After 4, set the end date back to `2026-12-01`: the select is enabled and its value is `None`.
  6. Submit failure path: with a range chosen and the choice disabled, submit and check the `apply` spy received `halfDay: 'None'` (FR-004); then make `apply` return `throwError(...)` with a `halfday.invalid` problem and check the screen shows the existing refusal message for it (the server stays the authority).
  Verify: in `web`, `npm test -- --watch=false` passes, including the new tests, and `npm run format:check` is clean.

**Checkpoint**: User Story 1 works and is covered by Vitest. The browser has not yet been driven (see T007).

---

## Phase 4: User Story 2 - A single half day counts 0.5 (Priority: P1)

**Goal**: prove the 0.5 count end to end for first and second half, and the server refusals, using what US-03 already built (spec FR-005, FR-006).

**Independent Test**: apply for one future working day as first half and see 0.5 in the preview and in the saved request.

- [X] T004 [US2] In `web/src/app/features/apply-leave/apply-leave.spec.ts`, add tests for FR-005 using the existing helpers (extend them only if needed):
  1. Same start and end date, choose `FirstHalf`: the `preview` spy receives `halfDay: 'FirstHalf'`, and the page shows the count the spy returns (0.5).
  2. Same for `SecondHalf`, and on submit the `apply` spy receives `halfDay: 'SecondHalf'` with the same start and end date.
  3. Failure path: `apply` returns a `halfday.invalid` refusal for a single day (for example a type that does not allow half days) and the page shows the refusal message.
  If an equivalent test already exists in the file, extend it rather than duplicate it and say which one. Verify: `npm test -- --watch=false` passes and `npm run format:check` is clean.
- [X] T005 [US2] Run the quickstart against the real API and report each scenario as PASS, FAIL or NOT RUN. Start the API from one place only (`dotnet run --project src\LeaveManagement.Api`) and the web app (`npm start` in `web`). Scenarios in `quickstart.md`: A (Jira criterion 1, 0.5 and pending +0.5), B (Jira criterion 2, disabled and reset), C (direct API call, half day on a range, expect 422 `halfday.invalid`, balance unchanged), D (weekend or holiday half day refused). If the browser cannot be driven in this environment, say that scenarios A and B were not run in a browser and are covered only by the Vitest specs. Scenario C and D can be run with PowerShell without a browser. Do not claim a scenario you did not run. Verify: the report lists all four scenarios with their result.

**Checkpoint**: both Jira criteria are checked, with a plain statement of what was and was not run.

---

## Phase 5: Polish and finish

- [X] T006 Run `/ci-check` (build, standards, tests, repo guard, packages). Verify: every step is PASS or an honest NOT RUN that CI covers (the missing .NET test project is expected).
- [X] T007 Run the finish steps from `CLAUDE.md`: `/review` (code-reviewer for the web change; the security reviewer is only needed to confirm no endpoint, token or data changed), then `/learn` for `ai/learning/us04-apply-half-day.md`, then `/status` to update `ai/specs/INDEX.md`. Do not write the owner's teach-back answers or exercise. Verify: review findings are fixed or listed, the notes file exists, and the INDEX row is updated.

---

## Dependencies and order

```text
T001 -> T002 -> T003 -> T004 -> T005 -> T006 -> T007
```

No task is marked [P]: T003 and T004 edit the same spec file, and T002 must exist before either test can pass.

## Requirement coverage

| Requirement | Task |
| --- | --- |
| FR-001, FR-002, FR-007 | T002, T003 |
| FR-003 | T003 (tests 4, 5) |
| FR-004 | T003 (test 6) |
| FR-005 | T004, T005 |
| FR-006 | T003 (test 6), T005 (C, D) |
| SC-001 | T003 |
| SC-002 | T005 (A, with a second-half run) |
| SC-003 | T005 (C) |
| SC-004 | T005 |

## T005 result (2026-11-14, API run with `dotnet run`, dev user Asha Nair, Annual, Tuesday 2026-12-01)

| Scenario | Result | What was run |
| --- | --- | --- |
| A, 0.5 count | PARTIAL | Preview for one day as `FirstHalf` and as `SecondHalf` returned `daysCount` 0.5 (HTTP 200). **NOT RUN:** the real submit and the pending +0.5 check, because it would leave a pending request in the owner's LocalDB. Needs the owner's OK. |
| B, disabled and reset in the screen | NOT RUN in a browser | No browser was driven. Covered only by the Vitest specs (T003). |
| C, half day on a range | PASS | Preview and `POST /api/me/leave-requests` both returned 422 `halfday.invalid`. Balances unchanged (Annual pending stayed 3.0). |
| D, weekend and holiday half day | PASS with a note | Saturday 2026-12-05 and Christmas 2026-12-25 were refused with 422. The code is `halfday.invalid`, not `days.none`; see the note below. |

Not verified: no .NET tests exist; the web screen was not seen in a browser.

## Notes

- Scenario D finding: spec US-03 and this spec say a half day on a weekend or holiday is refused "as a day with no working time", but the server answers `halfday.invalid` ("A half day must be a single working day, and this leave type must allow half days."). It is still refused, so no rule is broken. The message is less exact than the spec wording. Owner decision: accept the current message, or open a small change to return `days.none` for that case.

- Back end is untouched. If T005 finds a server gap, stop, update the spec and plan, and get approval again (constitution VI).
- The one design choice to confirm before T002: keeping the existing `[selected]` option bindings rather than binding the select's `[value]` (research R4). T003 test 4 proves the displayed value resets.
- Not verifiable here without a browser: how the disabled control and hint look and read with a screen reader. T005 reports this honestly.
