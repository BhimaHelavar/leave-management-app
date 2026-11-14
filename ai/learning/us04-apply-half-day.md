# US-04 (SCRUM-11): Apply for a half day

## 1. What we built

US-03 already counted a single half day as 0.5 on the server. The server also already refused a half day on a range, a weekend or a holiday. What was missing was the Jira wording: "when I choose a multi-day range, the half-day option is **disabled**".

The old screen **hid** the "Length of the day" choice for a range. Now the choice is always on the page. It is disabled, with a short hint, unless the start and end date are the same day. Then it is enabled.

The change is small and only in the Angular screen: `apply-leave.html`, a `.hint` rule in `apply-leave.scss`, and Vitest specs. `apply-leave.ts` was **not changed**. No back-end change, no migration, no new package.

## What was and was not verified

Read this first. An interviewer will ask how you tested it.

- **Vitest specs** cover the screen: disabled, hint, reset to full day, enabled again, what is sent, and how a refusal is shown. They use fake services, so they never touch the real API.
- **Scenario A (0.5 count): PARTIAL.** The preview for one day as `FirstHalf` and as `SecondHalf` returned `daysCount` 0.5 from the running API. The **real submit and the "pending +0.5" check were NOT run**. A submit would leave a pending request in the owner's LocalDB, and that needs the owner's OK.
- **Scenario B (disabled and reset in a browser): NOT RUN.** No browser was driven. It is covered only by the Vitest specs.
- **Scenario C (half day on a range): PASS.** Preview and `POST /api/me/leave-requests` both returned 422 `halfday.invalid`. Annual pending stayed 3.0.
- **Scenario D (weekend and holiday): PASS with a note.** Saturday 2026-12-05 and Christmas 2026-12-25 were refused with 422, but the code was `halfday.invalid`, not `days.none`.
- **No .NET tests exist.** The test project is planned for Phase 5. Server checks were manual calls.
- **`aria-live` was not tested with a real screen reader.** The tests only check that the attributes are in the HTML. Whether it is announced well is unknown.
- **Open owner decision:** the spec says a half day on a weekend or holiday is refused "as a day with no working time" (`days.none`). The server answers `halfday.invalid`, whose text is "A half day must be a single working day, and this leave type must allow half days." It is refused either way, so no rule is broken. The message is just less exact. Accept it, or open a small change.

## 2. Key concepts

### 2.1 Hide versus disable

**What it is.** *Hiding* removes the control from the page. *Disabling* keeps it visible but not usable. In HTML, a disabled `<select>` cannot be focused with a click and is not submitted with a form.

**How it works.** The template always renders the select and binds `[disabled]="!canChooseHalfDay()"`. `canChooseHalfDay` is an existing `computed` signal: `startDate() !== '' && startDate() === endDate()`. When a date signal changes, the computed changes, and Angular updates the `disabled` property in the same render.

**Why.** It matches the Jira criterion word for word. A disabled control also teaches the person that the option exists and that something about their dates blocks it. A hidden control looks like a missing feature. A native disabled select gives no reason, so we added a hint.

**When and where.** Disable when the option exists but is not valid right now, and you can explain why. Hide when the option makes no sense at all for this person (for example an admin-only action). Never treat either as security. The server still refuses.

**Edge case.** Before any date is chosen the control is also disabled (research R5). One rule, easy to test: a half day needs one chosen day.

### 2.2 The hint, `aria-describedby` and `aria-live`

**What they are.** *ARIA* attributes give extra information to assistive technology such as screen readers.
- `aria-describedby="half-day-hint"` links the select to the hint paragraph. A screen reader reads the hint after the label.
- `aria-live="polite"` marks a region whose changes are announced when the reader is idle.

**How it works in the code.**
- The hint `<p id="half-day-hint" class="hint">` is inside `@if (!canChooseHalfDay())`.
- The select has `[attr.aria-describedby]="canChooseHalfDay() ? null : 'half-day-hint'"`. When the select is enabled, the attribute is removed, so it never points at an id that does not exist.
- The `@if` sits inside a `<div aria-live="polite">` that is **always** in the DOM. This was a review finding after T003. A live region works only if it exists before its content changes. If the region were created together with the hint, many readers would not announce it.

**Why.** FR-007 asks that the state is understandable without colour alone. Visible text works without hover, touch or colour. A `title` tooltip was rejected: it is not reliable on touch or with many screen readers.

**When and where.** Use `aria-describedby` for help text or error text tied to one field. Use `aria-live` for status that changes without a page move. Use `polite`, not `assertive`, unless it is urgent.

**Honesty.** The attributes are tested. The real spoken result is **not** tested.

### 2.3 `[selected]` on options versus `[value]` on the select

**What it is.** There are two ways to show the chosen option from a signal. Bind `[selected]` on each `<option>`, or bind `[value]` on the `<select>`.

**How it works here.** Each option has `[selected]="halfDay() === 'FirstHalf'"` and so on. When `afterDatesChanged` sets `halfDay` to `'None'`, the Full day option gets `selected = true` and the display returns to "Full day".

**Why this matters now.** Before, the select was destroyed and rebuilt by `@if`, so its display could not drift from the signal. Now the select stays in the DOM. The person can pick First half, and the control then holds that value. If nothing re-applied the signal, the display could stay on First half while the signal says `None`. The test "goes back to full day and is disabled when the end date moves to a later day" pins this: it picks `FirstHalf`, moves the end date, and expects the select value to be `None`.

**Why not `[value]`.** It would also work. The existing options already used `[selected]`. We kept one style and added a test (research R4). One known quirk: with `[value]` on a select, the options must exist before the value is applied, which can be an ordering trap.

**When and where.** Whenever a signal and a native control can both change, add a test that the signal wins after a reset.

### 2.4 Reuse of the logic that already existed

**What it is.** The component already had the three pieces this story needs.
- `canChooseHalfDay` computed: when is a half day allowed.
- `afterDatesChanged()`: if not allowed, set `halfDay` to `None`.
- `submit()` and `refreshPreview()`: send `this.canChooseHalfDay() ? this.halfDay() : 'None'`.

**How it works.** Together they give a safety net. Even if the control showed a stale value, the request body would still carry `None` for a range. FR-004 and the "never send a stale half-day choice" edge case are met by code written in US-03.

**Why.** The smallest change is the safest. The only behaviour change is in the template. The plan says "no new logic in the component". Adding a second rule in the component would give two places that must agree.

**When and where.** Before you add code, find out what already exists. A spec that lists "already delivered" items (as this one does) avoids building things twice.

### 2.5 The server stays the authority

**What it is.** Golden rule 1: the server enforces every rule. The screen only shapes the screen.

**How it works.** The disabled select can be bypassed by anyone with dev tools or `Invoke-RestMethod`. Scenario C proved that: a direct `POST` with a half day on a range returned 422 `halfday.invalid`, and the balance did not change. The screen maps `halfday.invalid` to a readable message, and uses the server's `detail` first.

**Why.** A client-side rule is a convenience. Only the server can guarantee data is valid. Also, the server knows whether a leave type allows half days. The screen does not, so it does not guess: the refusal explains it.

**When and where.** Always. Mirror a rule on the screen for a better experience, but never replace the server check.

## 3. Diagram and worked example

```text
 start date, end date (signals)
        |
        v
 canChooseHalfDay = start != '' && start == end        (computed, from US-03)
        |
        +--> select#half-day  [disabled] = !canChooseHalfDay
        |       aria-describedby = 'half-day-hint'  (only while disabled)
        |
        +--> <div aria-live="polite">  (always in the page)
        |       @if (!canChooseHalfDay) <p id="half-day-hint">...</p>
        |
        +--> onStartDate/onEndDate -> afterDatesChanged()
        |       if !canChooseHalfDay -> halfDay = 'None'
        |       -> refreshPreview()  (server counts)
        |
        +--> submit(): halfDay = canChooseHalfDay ? halfDay : 'None'
                              |
                              v
                     POST /api/me/leave-requests
                              |
                  server re-checks everything (422 halfday.invalid
                  if range, weekend, holiday or type not allowed)
```

**Worked example (values from the tests and the T005 run).** Leave type Annual, Tuesday 2026-12-01.

1. Page opens. No dates. Select is disabled, hint is shown.
2. Start 2026-12-01, end 2026-12-01. `canChooseHalfDay` is true. Select is enabled, hint is gone, `aria-describedby` is removed.
3. Person picks First half. The screen asks the server for a preview with `halfDay: 'FirstHalf'`. The server answers `daysCount` 0.5 and `workingDays: ['2026-12-01']`. The screen shows "Working days counted: 0.5".
4. Person changes the end date to 2026-12-02. `canChooseHalfDay` turns false. `afterDatesChanged` sets `halfDay` to `None`. The select is disabled and shows "Full day". The hint appears and is announced politely. The next preview is sent with `halfDay: 'None'`.
5. Person sets the end date back to 2026-12-01. The select is enabled again and starts at "Full day", not at First half.
6. Direct call that skips the screen: a half day on 2026-12-01 to 2026-12-02 gets 422 `halfday.invalid`. Annual pending stays 3.0.

## 4. Trade-offs and rejected alternatives

| Choice | Chosen | Rejected | Why |
| --- | --- | --- | --- |
| Hide or disable | Disable, always render | Keep hiding | Fails the Jira criterion as written and looks like a missing feature. |
| Late error | Disable up front | Show enabled, error on submit | Worse experience. The screen would let a bad value through. |
| Explain the state | Visible hint linked by `aria-describedby` | `title` tooltip, or no text | Tooltips fail on touch and many readers. No text leaves it unexplained. |
| Announce the change | Always-present `aria-live="polite"` wrapper | Live region created with the hint | A region that appears together with its text is often not announced. |
| Keep control and signal in step | Keep `[selected]` and add a test | Bind `[value]` on the select, or recreate the control with `@if` | One style is kept. Recreating the control is the hide behaviour we removed. |
| Before dates exist | Disabled | Enabled until two different dates | Would allow a half day with an empty date and need a second rule. |
| "Disabled" as a rule | Screen state only | A new server rule | Disabled is not a rule. Server rules did not change. |
| Testing the server | Manual calls from `quickstart.md` | New .NET test project now | That is Phase 5, outside this story. |

**Cost of what we chose.** A persistent control means the signal and the DOM can disagree, so we need a test for the reset. The hint adds one more element and one more id to keep correct.

## 5. When to use or avoid

- **Disable with a hint** when the person can fix the cause themselves (here, by choosing one date). **Avoid** it when the reason is private or the control would stay disabled forever. Remove it in that case.
- **`aria-live`** for short status text. **Avoid** it on big regions that change often. It will be noisy.
- **Always-present wrapper** for live regions. **Avoid** adding the live attribute on the element that you create and fill in the same render.
- **Reusing component logic** when the spec shows it already exists. **Avoid** adding a parallel rule.
- **Trust the screen?** Never. Screen rules are hints. Server rules are the rules.

## 6. Interview questions

**1. Hide or disable a control, and when?**
Disable when the option exists but is not valid now and you can say why. Hide when it never applies to this person. Disabling tells the user what is possible. In this app the half-day select is disabled with a hint for a date range, because Jira asked for "disabled" and a hidden option looks like a missing feature.

**2. Is a disabled button or select a security measure?**
No. Anyone can change the DOM or call the API directly. The server must enforce the rule. In this app I sent a half day on a range straight to `POST /api/me/leave-requests` and got 422 `halfday.invalid`, with the balance unchanged.

**3. How do you make a disabled control understandable for screen-reader users?**
Keep a visible label, add a visible hint, and link it with `aria-describedby`. Do not rely on colour or on a `title` tooltip. Here the select points to `half-day-hint`, and the attribute is removed when the select is enabled so it never points to a missing id.

**4. How does `aria-live` work, and what is the common mistake?**
A live region announces changes to its content. The region must already exist before the content changes, or it may not be announced. `polite` waits for the reader to be idle. In this app the `<div aria-live="polite">` is always in the page, and only the hint inside it comes and goes. I have not tested it with a real screen reader, so I only know the attributes are present.

**5. What is the difference between `[selected]` on options and `[value]` on a select in Angular?**
`[selected]` sets each option's state. `[value]` sets the select's value as a whole, and depends on the options already existing. Both work. The point is that a persistent control can drift from its signal. In this app, resetting `halfDay` to `None` re-selects "Full day", and a Vitest spec checks that the select value is `None` after the end date moves.

**6. Why use a `computed` signal here instead of a method or a stored flag?**
A `computed` is derived from the date signals, cached, and always consistent with them. A stored flag could be forgotten when a date changes. In this app `canChooseHalfDay` drives the disabled state, the reset, the preview body and the submit body from one definition.

**7. The user picks First half, then changes the end date. What is sent, and why is it safe?**
`None`. Three layers agree. `afterDatesChanged` resets the signal, the select shows Full day, and `submit` and `refreshPreview` send `None` when `canChooseHalfDay()` is false. If one layer failed, the others and the server would still be correct. Here the end date moving from 2026-12-01 to 2026-12-02 sends `halfDay: 'None'`.

**8. Why was no back-end change made?**
The 0.5 count and the refusals were already built and reviewed in US-03. "Disabled" is a screen state, not a business rule. Changing working rules for a presentation gap adds risk for no gain. A back-end gap would have meant stopping and updating the spec first. In this app only the template, the SCSS and the specs changed.

**9. What did you not verify, and how do you say so?**
State it plainly. Here: no browser run for the disabled and reset scenario, no real submit for the pending +0.5 check, no .NET tests, no real screen reader. What was verified: Vitest specs, and API calls that returned 0.5 for preview and 422 for a range, a weekend and a holiday.

**10. The server returns `halfday.invalid` for a half day on a Saturday, not `days.none`. Is that a bug?**
Not a rule break: the request is refused. It is a message mismatch with the spec wording. It needs an owner decision: accept it, or change the server to return `days.none` for this case. A server change means updating the spec and getting approval again. In this app, 2026-12-05 and 2026-12-25 both returned 422 `halfday.invalid`.

## 7. Teach-back

Answer in your own words. Do not copy the notes.

1. Explain why the half-day select is disabled and not hidden, and why it is still not a security feature. What proves that on this project?
2. Walk through what happens, step by step, when someone picks First half on 2026-12-01 and then moves the end date to 2026-12-02. Name the signal, the method and what is sent.
3. Explain why the `aria-live` wrapper must always be in the page, and say what you could not check about it.

### My answers

(empty)

## 8. Exercise

About 20 minutes. Paper, then a small hands-on step.

**Task.** Add a new test case on paper first, then in code.

1. On paper: write a table of four date pairs (empty and empty, same day, two different days, end before start such as start 2026-12-02 and end 2026-12-01). For each, write what `canChooseHalfDay()` returns, whether the select is disabled, whether the hint shows, and what `halfDay` is sent.
2. Look at the fourth row. Decide whether the current hint text is still correct. Write one sentence about it.
3. Hands-on: in `web`, open `src\app\features\apply-leave\apply-leave.spec.ts`. Write one Vitest test for the pair you were least sure about, using the existing `render` and `change` helpers. Run it:

```text
cd web
npm test -- --watch=false
```

4. Break it on purpose: in `apply-leave.html` temporarily remove the `aria-live` wrapper line and see which test fails. Put it back afterwards.

Do not change the server. If you find a gap there, write it down as a question for the owner.
