# US-05: Block requests that break my balance or overlap

**Jira:** SCRUM-12 · Story · Done (resolved 2026-11-14) · High · epic SCRUM-7 (Leave Management App: MVP) · SCRUM Sprint 4 (closed 2026-11-14)
**Code:** PR #7 (merged into main, merge commit `827e3b4`) · branch `SCRUM-12-US-05-Block-requests-that-break-my-balance-or-overlap` · head `7e2529c8500db177f84566416b85251a68491f8d` · 13 files (+673, -13)
**Written:** 2026-11-14. I read the code and the design documents. I did not build or run anything. Where I write "the owner's session ran", that is a result given to me, not something I ran.

## What the story is

An employee must not be able to send a request that breaks the rules. If you ask for more days than you have, the request is refused and nothing is saved. If your dates clash with leave you already have, it is refused and the message names the clashing dates.

Most of this already existed. The balance block and the overlap block were built in US-03 (PR #5, SCRUM-10). This story (PR #7) adds the one missing piece: the overlap message now **names the dates**. Before, it only said "one or more of these days".

**Why it matters:** a manager never sees a request that could not be valid, and the employee can see at once what to change.

## What the code delivers

```
Apply screen (S-03)  --POST /api/me/leave-requests-->  LeaveApplicationStore (one DB transaction)
  1. saved answer for this Idempotency-Key?  -> return it
  2. ClashingDatesAsync: my own leave.LeaveRequestDay rows in the range
        clash?  -> 409 leave.overlap  "...request on 2026-12-01, 2026-12-02."   (PR #7)
  3. insert request + day rows   (unique index UX_LeaveRequestDay_Employee_Date_Slot)
        race loser? -> rollback, saved answer? else re-read dates -> 409 leave.overlap
  4. guarded update  Available >= days
        zero rows? -> rollback -> 422 balance.insufficient, nothing saved   (PR #5)
  5. audit row, commit -> 201
```

1. **Balance block (PR #5).** The store does one guarded update: take the days only if `Available >= days`. If no row is changed, it rolls back and answers 422 `balance.insufficient`. It never reads the balance, adds in code and writes it back, so two requests cannot both spend the last days.
2. **Overlap pre-check (PR #5, changed in PR #7).** Before writing, the store reads the person's own day rows in the date range. A clash is a row with the same date and the same slot (AM or PM). The new private method `ClashingDatesAsync` returns the distinct clashing dates in order.
3. **Message builder (PR #7).** `LeaveRuleErrors.Overlap(dates)` is a pure function: distinct, sorted, ISO `yyyy-MM-dd`, at most 10 dates, then "and N more". An empty list keeps the old general text.
4. **Race backstop (PR #5, changed in PR #7).** If two submits race, the unique index refuses the loser's insert. The store rolls back, first checks whether the winner saved an answer for the same key (then it returns that answer), and only then re-reads the dates and builds the 409.
5. **Screen (not changed).** `apply-leave.ts` already shows the server `detail` text in an alert. PR #7 only adds tests for it.

**Example:** Sneha Iyer (dev user, id 6, Employee) holds a pending Annual request for Tue 2026-12-01 to Wed 2026-12-02. She applies for Mon 11-30 to Wed 12-02. The API answers 409 `leave.overlap` with "You already have a pending or approved leave request on 2026-12-01, 2026-12-02." It does not name 11-30. Her balance and request list are unchanged.

## Acceptance criteria: where each one is delivered

| Criterion (from Jira) | Delivered by | State |
| --- | --- | --- |
| 1. With 2 Annual available, asking for 3 days shows "insufficient balance" and saves nothing | Guarded update and rollback in `LeaveApplicationStore.cs` (PR #5); code `balance.insufficient`, 422. The owner's session ran a 9-available / 11-days case: 422, balance unchanged. The exact "2 available, 3 requested" case was not run | Done (closest case run) |
| 2. Overlap with an approved request is rejected naming the clashing dates | `ClashingDatesAsync` + `LeaveRuleErrors.Overlap(dates)` (PR #7). Run with pending requests only: four overlap shapes and a two-request case named the right dates | Partly: approved case could not be run (approve is 4e, not built) |

## Code changes

**Back end (`src/`)**

| File | What it does |
| --- | --- |
| `LeaveManagement.Application/Leave/LeaveRuleError.cs` | `Overlap(dates)` now builds the message from dates (pure function, cap of 10) |
| `LeaveManagement.Infrastructure/Persistence/LeaveApplicationStore.cs` | New `ClashingDatesAsync` (caller's own rows only); both overlap refusals use it; race path re-reads dates after rollback and checks the saved answer first |

**Front end (`web/src/app/`)**

| File | What it does |
| --- | --- |
| `features/apply-leave/apply-leave.spec.ts` | Screen code unchanged. Two new Vitest cases (see Tests) |

**Tests**

| File | What it checks |
| --- | --- |
| `features/apply-leave/apply-leave.spec.ts` | A 409 `leave.overlap` with dates in `detail` shows both dates in the alert and saves nothing. A 409 with no `detail` shows the general overlap wording |

No .NET test exists (Phase 5). Back-end checks were manual API calls.

**Docs and tooling**

| File | What it is |
| --- | --- |
| `ai/specs/us05-block-invalid-requests/` (spec, plan, research, data-model, quickstart, tasks, checklists, `contracts/overlap-refusal.md`) | The spec folder. I read spec, research and tasks in full; the others not read in detail |
| `ai/specs/us03-apply-leave-working-days/contracts/leave-requests-api.md` | The `leave.overlap` message row now has `{dates}` |
| `ai/specs/INDEX.md` | Row for this story (still says "In progress") |

No migration, no new package, no screen change.

## Design versus code

| Point | Design says | Code does | Match? |
| --- | --- | --- | --- |
| Error codes | 3d: snake_case, `leave_overlap`, `insufficient_balance` | Dotted: `leave.overlap`, `balance.insufficient` | Differs |
| Detail text | 3d example: "12 Nov is already covered by request 98." | Names dates only, never a request id (research R6; security rules 4 and 12) | Differs on purpose |
| Path | 3d: `/api/v1/leave-requests` | `/api/me/leave-requests` (known since US-03) | Differs |
| Status codes | 3d: 409 clash with current data, 422 business rule refused | Overlap 409, balance 422 | Yes |
| Rule 5 | Confluence policy page: hard block, unpaid is the escape | Guarded update blocks; Unpaid has no balance (steering rule 5) | Yes |
| Rule 13 | Block overlap with pending or approved | Any request with day rows blocks. Release of rows on reject or cancel is 4e | Partly (not built) |
| S-03 validations | 3a: enough balance, no overlap; errors announced to screen readers | Both enforced on the server; the screen shows the refusal in `role="alert"` (read from the new test, not checked in the HTML) | Yes (not changed here) |
| Validation in both layers | ADR 28: rules in the API | Server decides; screen only shows the text | Yes |
| Day table | 3c holds `LeaveRequestDay` | I could not read the 3c details (only the search hit). I read `LeaveConfigurations.cs` instead: unique index on employee, date, slot | Not compared with 3c |

**Design documents used:**
- Confluence LMA "Leave-Policy Rules (accepted defaults)" id 229377
- Confluence LMA "Phase 1: Requirements Overview" id 98443
- Confluence LMA "Phase 3a: UI/UX Design" id 65835
- Confluence LMA "Phase 3d: API Design" id 1277972
- Confluence LMA "Phase 3c: Database Design" id 1048577 (search hit only, not opened)
- Repo: `ai/specs/us05-block-invalid-requests/`, `ai/specs/us03-apply-leave-working-days/`, `ai/steering/security.md`, `.specify/memory/constitution.md` (named by the caller; not read in detail by me)

**Looked for and not found:** Phase 3e class design and Phase 3f Angular design (not in the LMA space).

## State of the tasks

- Done (ticked): T001 baseline green, T002 message builder and store, T003 contract text, T004 Vitest cases, T005 API checks.
- Open: T006 (scenario A re-check, US2), T007 (exact-balance run), T008 `/ci-check`, T009 `/review`, `/learn`, `/status`.

## Findings and gaps

- **Jira says Done and the PR is merged, but T006 to T009 are open** in `tasks.md`. The review step was not recorded as done. `INDEX.md` still says "In progress". The repo looks out of date or the finish steps were skipped. The owner decides.
- **No learning note** exists for US-05 in `ai/learning` (T009 not done).
- **Approved clash never run.** Approve is 4e. Rule 13 for approved requests, and "rejected or cancelled never clash" (FR-005), are untested. 4e must release the day rows on reject and cancel (hand-off in `INDEX.md`).
- **Spec still says Status: Draft** at the head. The follow-up fixes the caller called R1 (same-date opposite half days), R2 and R3 wording are **not in the spec file**. I found no text about opposite half days in the spec. From the code, the index is on employee, date and slot, and `Slot` is AM or PM, so an AM and a PM request on one date can coexist. I inferred this; I did not run it, and I did not read how a full day is stored.
- **Screen not seen in a browser.** Only the Vitest cases cover it.
- **Test data left in LocalDB** (Sneha Iyer, pending Annual): requests 35, 36, 38, 40. No cancel exists until 4e.
- **Checks the owner's session ran (before the commit):** `dotnet build` 0 warnings 0 errors; both `dotnet format` checks clean; web 114 of 114 Vitest tests; Prettier clean; web build OK. API checks: four overlap shapes named the right dates; two requests named three dates in order; clash plus too-big range named the overlap first; no-clash 11 days with 9 available gave 422; two parallel submits with different keys gave one 201 and one 409 naming dates; same key gave the same 201 twice. Not run: `/ci-check` and the review after the last change, any .NET test. Jira shows one build with unknown result.

## Why it is built this way

| Decision | Chosen | Rejected, and why |
| --- | --- | --- |
| Reuse the US-03 blocks | Change only the message (research R1) | Rebuild the checks: a reviewed rule, risk for no gain |
| Where dates come from | Read the caller's day rows in the range (R2) | Parse the SQL error text: it must never reach the client (security rule 10) and is unreliable |
| Race path | Re-read dates after rollback; empty list falls back to old wording (R3) | Always generic: same clash named or not by timing. Retry the save: hides a real conflict |
| Message shape | Text only, ISO dates, max 10 (R4) | A `clashingDates` field: nobody uses it yet. Friendly dates: needs a server culture choice |
| Approved vs pending | One query for both, because both hold day rows (R5) | Filter by status: day rows carry no status, a join is a second source of truth |
| What is named | Own dates only, no request id, no leave type (R6) | Name the request as 3d does: leaks more data, not needed |
| Testing | Pure builder, manual API run, Vitest (R7) | Add a .NET test project now: Phase 5 |

## Interview practice

1. **Q:** How do you stop two requests spending the last leave days? **A:** One guarded SQL update, `Available >= days`, inside the transaction. If zero rows change, roll back and return 422. No read-then-write, so there is no race.
2. **Q:** Why a pre-check and a unique index for overlap? **A:** The pre-check gives a clear answer in the normal case. The unique index (employee, date, slot) is the last defence when two submits race. The database holds the rule even if the code is wrong.
3. **Q:** Why 409 for overlap and 422 for balance? **A:** 409 means the request clashes with current data. 422 means the request is understood but a business rule refuses it (3d design).
4. **Q:** Why name dates but not the other request? **A:** It helps the user, and dates inside their own range from their own rows leak nothing. A request id or reason adds data that is not needed (security rules 4 and 12).
5. **Q:** Why is `Overlap(dates)` a pure function? **A:** No clock, no database, so it is easy to unit test. It sits in Application, so Domain and the dependency rule are untouched.
6. **Q:** What would you do if the loser of a race finds the winner's saved answer? **A:** Return that answer. With the same idempotency key the second call is a retry, not an overlap.

**Teach-back (answer in your own words):**
1. Walk through what happens, step by step, when two browser tabs submit the same dates with different keys at the same moment.
2. Why does the store check for a saved idempotency answer before it builds an overlap error?
3. Which parts of the rule "block overlap with pending or approved" work today, and which part waits for increment 4e?
