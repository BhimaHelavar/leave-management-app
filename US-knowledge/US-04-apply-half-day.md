# US-04: Apply for a half day

**Jira:** SCRUM-11 · Story · To Do · Medium · epic SCRUM-7 (Leave Management App: MVP) · SCRUM Sprint 4 (active)
**Code:** PR #6 (open; found with `git ls-remote`, no `gh`) · branch `SCRUM-11-US-04-Apply-for-a-half-day` · head `06657da` (1 commit "LMS-US-04 halfday leave") · base `0cc3a56` (= main, merge of PR #5) · 13 files (+925, -19)
**Written:** 2026-11-14. I read the code and the design documents. I did not build or run anything. Results marked "the owner's session" were given to me, not run by me.

## What the story is

An employee wants to take only half a day off, so that a full day is not taken from the balance. They pick one date and choose first half or second half. The count is 0.5. If they pick a range of several days, the half-day choice is switched off.

**Why it matters:** a half day is a real need, and the balance must go down by 0.5, not 1 (requirement BR-03, Must; policy rule 11).

**Important:** most of this story was built earlier, in PR #5 (US-03, SCRUM-10). PR #6 changes only the screen: the half-day choice is now always visible and disabled with a hint, instead of hidden. The rest of the PR is docs and tests.

## What the code delivers

```
Apply for leave (/apply-leave)       web: apply-leave.ts / .html       [PR #6 changed .html]
  start = end ?  --yes--> "Length of the day" select enabled (Full / First half / Second half)
                 --no --> select disabled + hint, value reset to None
  |-- GET  /api/me/leave-requests/preview?halfDay=FirstHalf --> LeavePlanner   [PR #5]
  |-- POST /api/me/leave-requests (Idempotency-Key)         --> LeavePlanner   [PR #5]
        WorkingDayCalculator.Count: one day + half = 1 slot (AM or PM) = 0.5
        LeaveRules.FirstViolation: range, weekend, holiday, type not allowing half -> halfday.invalid
        LeaveApplicationStore -> leave.LeaveRequestDay (1 row) -> balances (-0.5) -> audit
  +<-- 200 {daysCount 0.5}   or   201 saved   or   422 halfday.invalid
```

1. **PR #6, screen.** `apply-leave.html` always shows the select "Length of the day". `canChooseHalfDay` (in `apply-leave.ts`, from PR #5) is true only when start and end are the same non-empty date. If it is false, the select gets `[disabled]` and `aria-describedby="half-day-hint"`.
2. **PR #6, hint.** The hint text sits in a `div aria-live="polite"`, so a screen reader can announce it when the dates change. The wrapper div is always in the page; only the text appears and goes. (Live regions work best when they exist before the text changes.)
3. **PR #5, reset.** `afterDatesChanged` sets the half day back to `None` when the dates stop matching. `submit` and `refreshPreview` also send `None` when the choice is not allowed.
4. **PR #5, count.** `WorkingDayCalculator.Count` makes one slot for a single half day (AM for first half, PM for second half). A full day makes two slots. `DaysCount` = slots x 0.5, so one slot is 0.5 and always matches the saved day rows.
5. **PR #5, server rule.** `LeaveRules.FirstViolation` refuses a half day when start is not end, when the leave type has `AllowHalfDay` false, or when the day has no slot (weekend or holiday). The code is `halfday.invalid`, with status 422.
6. **Why both layers.** The screen only shapes the form (golden rule: the server enforces every rule). A client that skips the screen still gets 422.

**Example:** Asha Nair (Employee) opens `/apply-leave`, picks Annual and Tue 2026-12-01 for start and end. The select turns on. She picks First half. The screen calls `GET /api/me/leave-requests/preview` with `halfDay=FirstHalf` and shows 0.5. She changes the end date to 2026-12-02. The select turns off, goes back to Full day, and the hint says a half day is for a single day only. If she called `POST /api/me/leave-requests` directly with a half day on two days, she would get 422 `halfday.invalid`.

## Acceptance criteria: where each one is delivered

| Criterion (from Jira) | Delivered by | State |
| --- | --- | --- |
| AC1. A single date, first half: count is 0.5 | PR #5: `WorkingDayCalculator.Count`, `LeavePlanner`. PR #6: Vitest tests "asks for the FirstHalf/SecondHalf count and shows the 0.5" | Done in code. The owner's session ran preview for FirstHalf and SecondHalf: 0.5. A real submit with the pending +0.5 check was NOT run |
| AC2. A multi-day range: the half-day option is disabled | PR #6: `apply-leave.html` (`[disabled]`, hint), tests in `describe('half-day choice')`. Server backstop from PR #5: `halfday.invalid` | Done in code and Vitest. Not seen in a browser. The owner's session confirmed the server refusal (422) on a two-day range |

Note: PR #5 hid the option on a range. Jira says "disabled". PR #6 closes that gap.

## Code changes

**Front end (`web/src/app/`)**

| File | What it does |
| --- | --- |
| `features/apply-leave/apply-leave.html` | Changed in PR #6. Select always rendered; `[disabled]` and `aria-describedby` when not a single day; hint inside `aria-live="polite"` |
| `features/apply-leave/apply-leave.scss` | Changed in PR #6. Small `.hint` rule under `.field` (the old one only applied under `.preview`) |
| `features/apply-leave/apply-leave.ts` | Not in this diff. From PR #5: `canChooseHalfDay`, reset in `afterDatesChanged`, `None` fallback. Read at the head ref |

**Back end (`src/`)**

No back-end file changed in PR #6. The half-day logic below was built in PR #5 and read at the head ref:

| File | What it does |
| --- | --- |
| `Domain/WorkingDayCalculator.cs` | Pure count. One half slot for a single half day, two slots for a full day |
| `Domain/LeaveRules.cs` | Rule 11: refuses a half day on a range, on a type with `AllowHalfDay` false, or on a day with no slot |
| `Application/Leave/LeavePlanner.cs` | Shared by apply and preview. Passes `type.AllowHalfDay` to the rules |
| `Application/Leave/LeaveRuleError.cs` | Code `halfday.invalid` and its message |

**Database**

No change. No migration.

**Tests**

| File | What it checks |
| --- | --- |
| `features/apply-leave/apply-leave.spec.ts` | Changed in PR #6. Replaces one old test (select removed) with 6 tests in `half-day choice` (visible but disabled before dates and on a range, enabled on one day, reset to None, enabled again, refusal shown) and 3 tests in `a single half day`, two of them run for both halves (preview asks for the half and shows 0.5, submit sends the half, refusal shown). I read the diff, not every helper |

There is no .NET test project (Phase 5). The server behaviour is covered only by manual API calls.

**Docs and tooling**

| File | What it is |
| --- | --- |
| `ai/specs/us04-apply-half-day/` (spec, plan, research, data-model, quickstart, tasks, contracts, checklists) | Spec set for SCRUM-11. I read tasks fully and the rest lightly |
| `ai/learning/us04-apply-half-day.md` | The learning note. Not read |
| `ai/specs/INDEX.md` | Modified. Row for US-04 |

## Design versus code

| Point | Design says | Code does | Match? |
| --- | --- | --- | --- |
| Half-day control | 3a (65835) wireframe: checkbox "Half day" plus two radios | One select "Length of the day" (Full day, First half, Second half). Same behaviour | Differs |
| Single date only | 3a validation and rule 11 (229377): single day only | Select disabled on a range; server refuses | Yes |
| Counts 0.5 | Rule 11 | One slot = 0.5 | Yes |
| halfDay values | 3d A1: None, FirstHalf, SecondHalf | `HalfDay` None, FirstHalf, SecondHalf | Yes |
| Endpoints | 3d: `POST /api/v1/leave-requests/preview` (JSON body) and `POST /api/v1/leave-requests` | `GET /api/me/leave-requests/preview` (query) and `POST /api/me/leave-requests` | Differs |
| Error codes | 3d: snake_case, e.g. `insufficient_balance` | Dotted: `halfday.invalid`, `balance.insufficient` | Differs |
| Status for rule failure | 3d: 422 with stable `code`, Problem Details | 422 with `code` | Yes |
| Weekend or holiday half day | Spec (US-03 and US-04): refused as "no working time" (`days.none`) | Server answers `halfday.invalid` (the rule code says it is done on purpose) | Differs (still refused) |
| Half-day option on a range | Jira AC2: disabled | PR #5 hid it; PR #6 disables it with a hint | Yes after PR #6 |
| Accessibility | 3a: visible label, errors next to the field and announced, no colour alone | Label, hint text linked by `aria-describedby`, live region. Not tested with a screen reader | Partly (unverified) |
| Validation in both layers | ADR 28: Angular and API, rules in the API | Screen shapes, server decides | Yes |
| Half day per type | Seeded types have `AllowHalfDay` | The screen cannot know it; server refuses | Partly |

**Design documents used:**
- Jira SCRUM-11 (facts from the prompt)
- Confluence LMA: Leave-Policy Rules 229377, Phase 1 Requirements 98443, Phase 3a UI/UX 65835, Phase 3d API 1277972 (excerpts from the prompt)
- `ai/specs/us04-apply-half-day/tasks.md` (read in full); `ai/steering/*`; US-03 note

**Looked for and not found:** a separate half-day design page beyond S-03; Phase 3e class design and 3f Angular design (not in the LMA space). Not read in full: the other files in the US-04 spec folder, `ai/learning/us04-apply-half-day.md`.

## State of the tasks

`us04-apply-half-day/tasks.md`: T001 to T007, all ticked (7 of 7).
- Done: T001 baseline; T002 template; T003 screen tests; T004 half-day tests; T005 API checks (partial, see below); T006 `/ci-check`; T007 review, learn, status.
- Open: none in the file. But T005 itself is only partly done. Scenario A (real submit) and B (browser) are not run, and the file says so.

## Findings and gaps

- **Jira says To Do, the code is built and pushed.** All 7 tasks are ticked and PR #6 is open. Jira should probably move to In Progress or Review. Your decision.
- **Most of the story is PR #5, not PR #6.** PR #6 is a small screen change plus tests and docs. Review the two PRs together.
- **The wireframe differs.** Design shows a checkbox plus two radios. Code uses one select. Behaviour is the same. Either the wireframe or the code should be updated.
- **API contract differs from 3d** (path, version prefix, preview verb, snake_case versus dotted codes). This was already found in US-03; the code follows the repo contract and 3d is a draft.
- **Open owner decision: weekend or holiday half day.** The spec says `days.none`; the server returns `halfday.invalid` with a less exact message. It is refused either way. Accept it or open a small change.
- **Not run:** a real half-day submit with the pending +0.5 check (it would leave a request in the owner's LocalDB); the screen in a browser; a screen reader; any .NET test (none exist).
- **What the owner's session ran (I did not):** web format check, 112 of 112 Vitest tests, web build, back-end Release build and format checks, package vulnerability checks, all passed in `/ci-check` on 2026-11-14, before the final commit (the commit has the same files). Real API: preview for one day as FirstHalf and SecondHalf gave `daysCount` 0.5; a half day on a two-day range, on Saturday 2026-12-05 and on Christmas 2026-12-25 gave 422 `halfday.invalid`; balances unchanged.
- **What I could not verify:** all of the above results, the PR page and its checks, and the layout of the new hint.

## Why it is built this way

| Decision | Chosen | Rejected, and why |
| --- | --- | --- |
| Range with half day | Select always visible, disabled, with a hint | Hiding it (PR #5): the user cannot tell the option exists or why it is missing; Jira says "disabled" |
| Announcing the hint | Always-present `aria-live="polite"` wrapper, text inside changes | A live region created together with its text: screen readers may miss it |
| Counting a half day | One slot, `DaysCount` = slots x 0.5 | A separate 0.5 flag: it could disagree with the saved day rows |
| Where the rule lives | Server refuses (`halfday.invalid`); screen only disables | Screen-only check: a direct API call would bypass it (golden rule, ADR 28) |
| Control type | One select | Checkbox plus radios (wireframe): more controls and states to keep in step; not stated in a document why |
| Reset on date change | Back to `None` | Keep the old half choice: a hidden half-day value could be sent on a range |

The "control type" reason is my reading; the spec's `research.md` R4 covers the `[selected]` binding only, and I did not find a design reason for the select.

## Interview practice

1. **Q:** How is a half day stored and counted? **A:** As one half-day slot (AM or PM) in the day rows. `DaysCount` is slots x 0.5, so a half day is 0.5 and always matches the rows.
2. **Q:** Why disable the control and also refuse on the server? **A:** The screen only helps the user. The server enforces every rule, so a direct API call on a range still gets 422 `halfday.invalid`.
3. **Q:** Why disabled with a hint instead of hidden? **A:** A hidden option gives no clue. A disabled one with a linked hint explains why. `aria-describedby` and a polite live region make a screen reader say it too.
4. **Q:** Why does the screen reset the half choice when the dates change? **A:** A half day only means something on one day. Resetting to `None` stops a stale value being sent with a range.
5. **Q:** Why 422 and not 400 for `halfday.invalid`? **A:** The request is well formed but breaks a business rule. The API uses 422 with a stable `code` for rule failures.
6. **Q:** How did you test it with no .NET tests? **A:** Vitest covers the screen (disabled, reset, 0.5 shown, body sent, refusal shown). The server was checked by manual API calls. I say plainly that no .NET test exists yet (Phase 5).

**Teach-back (answer in your own words):**
1. Follow a first-half request for one working day from the select to the database. Which function makes the slot, and what is `DaysCount`?
2. Which parts of "half day only on a single date" are in the screen, and which in the server? What happens if the screen is bypassed?
3. The weekend half day returns `halfday.invalid` while the spec says `days.none`. What would you check before choosing which to change, and who owns the decision?
