# Research: Apply for a half day

No `NEEDS CLARIFICATION` items were left in the spec. The questions below are design choices checked against the existing code.

## R1. Is the 0.5 count and the server refusal already built?

- **Decision**: Yes. Do not change the back end.
- **Evidence**: `LeavePlanner` passes `halfDay` into `WorkingDayCalculator.Count` and the leave-type `AllowHalfDay` rule. `LeaveRuleError` has `HalfDayInvalid`. The screen maps `halfday.invalid` to a message. US-03 spec FR-004 and scenario 4 cover it.
- **Rationale**: Changing a working, reviewed rule for a presentation gap would add risk for no benefit.
- **Alternatives considered**: Add a new server rule for "disabled" — rejected, "disabled" is a screen state, not a rule.

## R2. Hide versus disable

- **Decision**: Always render the "Length of the day" select. Set `disabled` when `canChooseHalfDay()` is false.
- **Rationale**: Matches the Jira criterion word for word. A disabled control tells the person the option exists and why it is unavailable. A hidden one looks like a missing feature.
- **Alternatives considered**: Keep hiding it (current behaviour) — rejected, fails criterion 2 as written and was the reason for this story. Show it enabled and show an error on submit — rejected, worse experience and the screen would let a bad value through.

## R3. How to explain why it is disabled

- **Decision**: A visible hint paragraph with a fixed `id`, linked to the select with `aria-describedby`. Shown only while the select is disabled. Text: "A half day applies to a single day only. Choose the same start and end date to use it."
- **Rationale**: A native disabled select gives no reason, and a tooltip (`title`) is not available on touch or to many screen readers. A visible hint works without colour or hover (FR-007).
- **Alternatives considered**: `title` tooltip only — rejected, not reliable. A disabled select with no text — rejected, unexplained.

## R4. Keeping the signal and the control in step

- **Decision**: Keep the existing behaviour: `afterDatesChanged` sets `halfDay` to `None` whenever `canChooseHalfDay()` is false, and `submit` and `preview` send `None` unless the dates are one day. Because the select now stays in the DOM, the `[selected]` bindings on its options must re-evaluate so the control visibly returns to "Full day".
- **Rationale**: Previously the select was destroyed and rebuilt, so its displayed value could not drift. With a persistent select, a user-picked option can differ from the signal. The options already bind `[selected]` to the signal, so a reset to `None` updates the display. A unit test pins this (spec FR-003).
- **Alternatives considered**: Bind `[value]` on the select — works too, but the existing options use `[selected]`; keep one style and add the test instead. Recreate the control with `@if` — that is the hide behaviour we are removing.

## R5. Disabled control and the "no dates yet" state

- **Decision**: Disabled whenever `canChooseHalfDay()` is false. This includes before any date is chosen. The same hint applies.
- **Rationale**: One rule, easy to explain and test. A half day needs one chosen day, so it is correctly unavailable until then.
- **Alternatives considered**: Enable it until two different dates are picked — rejected, would allow a half day with an empty date and need a second rule.

## R6. Testing without a .NET test project

- **Decision**: Cover the screen with Vitest (`HttpTestingController` is already in the spec harness). Cover the server side with the manual quickstart calls, including a direct call that bypasses the screen (SC-003). State plainly that no .NET tests exist yet.
- **Rationale**: Honest about what is verified, matches the US-03 approach and the `tech.md` verification rule.
- **Alternatives considered**: Add a .NET test project now — rejected, that is Phase 5 and outside this story.
