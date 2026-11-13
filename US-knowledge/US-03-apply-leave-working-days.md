# US-03: Apply for leave with an accurate working-day count

**Jira:** SCRUM-10 · Story · To Do · High · epic SCRUM-7 (Leave Management App: MVP) · SCRUM Sprint 4 (active)
**Code:** No pull request, no commit. Uncommitted work on branch `SCRUM-10-US-03-Apply-for-leave-with-an-accurate-working-day-count`, base `9390de5` (same as `main`), head = working tree · 12 modified files (+68, -12) and 22 new code and test files (about 2,100 lines, not counted as added lines) plus the new spec folder, all for the working tree
**Written:** 2026-11-13. I read the code and the design documents. I did not build or run anything, and this note was not checked by running the build or the tests. Where I write "the owner's session ran", that is a result the owner's session gave me, not something I ran.

## What the story is

An employee picks a leave type and two dates. The server counts only the real working days: Monday to Friday, minus the office holidays. It then checks the policy rules, saves the request as Pending, and takes the days out of the balance.

The screen can also ask the server for a preview count before you send. The screen never counts days itself.

**Why it matters:** the balance is only right if the day count is right (requirement BR-03, Must; policy rules 7, 8, 10, 13).

## What the code delivers

```
Apply for leave (/apply-leave)
  |-- GET  /api/me/leave-requests/preview ----> PreviewWorkingDays --+
  |-- POST /api/me/leave-requests + Idempotency-Key -> ApplyForLeave |
  |                                                      |          v
  |                                                      +--> LeavePlanner (shared)
  |                              IApplicantReader / ILeaveTypeReader / IHolidayReader
  |                              WorkingDayCalculator + LeaveRules (Domain, pure)
  |                                                      |
  |                            LeaveApplicationStore (ONE transaction)
  |   leave.LeaveRequest -> leave.LeaveRequestDay (unique) -> balances.LeaveBalance (guarded)
  |   -> balances.LeaveBalanceEntry (Deduction) -> audit.AuditEvent -> leave.IdempotencyKey
  +<-- 201 {id, daysCount, status Pending, ...}  or  409 / 422 with a `code`
```

1. The screen sends type, dates, half day and reason. The person is the token's `sub` claim. There is no employee id, status or day count in the body, so a client cannot set them.
2. `LeavePlanner` is the shared step for apply and preview. It reads the person, the leave type and the holidays, then counts and checks the rules. One place means the preview can never disagree with a submit.
3. `WorkingDayCalculator` loops over the dates. It skips Saturday, Sunday and holidays. It returns half-day "slots" (AM and PM). Days = slots times 0.5, so the count always equals the saved day rows. A weekend between two leave days is not counted (sandwich rule off, rule 10).
4. `LeaveRules.FirstViolation` returns the first broken rule: past date, bad half day, no working days, notice too short (rule 7), too many days in a row (rule 8).
5. "Today" is the person's local date (Asia/Kolkata), not the UTC date, taken from `TimeProvider`.
6. `LeaveApplicationStore` saves everything in one transaction. First it looks for a saved answer for the same Idempotency-Key. Then it checks overlap (rule 13). Then it inserts the request, the day rows, one guarded balance update, the ledger entry, the audit row and the saved answer.
7. The balance step is one statement: take days off only `WHERE Available >= days`. Zero rows changed means "not enough balance", and the transaction is rolled back. A balance never goes below zero (rule 5).
8. The Angular `ApplyLeave` screen calls the preview after every change, shows the count, the counted dates and the skipped holidays, and sends the form with a fresh key. A double click reuses the key, so it cannot save twice.

**Example:** Asha Nair (Employee, Bengaluru) picks Annual, Fri 3 Oct to Tue 7 Oct. The preview calls `GET /api/me/leave-requests/preview` and shows 3 working days (Fri, Mon, Tue). If Mon 6 Oct is a Bengaluru holiday, it shows 2 and lists Mon as not counted. She clicks Apply. `POST /api/me/leave-requests` returns 201 with `daysCount` 2.0 and `status` Pending. Her Annual `Available` drops by 2 and a Deduction row of -2 is written. If she asks for 16 calendar days in a row, she gets 422 `max.consecutive`. If a day overlaps an earlier request, she gets 409 `leave.overlap`.

## Acceptance criteria: where each one is delivered

| Criterion (from Jira) | Delivered by | State |
| --- | --- | --- |
| AC1. Fri 3 Oct to Tue 7 Oct, no holidays: 3 working days | `WorkingDayCalculator.Count`, `LeavePlanner`, `LeaveRequestEndpoints` | Done in code. The owner's session ran the API and got 3.0. No back-end test |
| AC2. Holiday on Mon 6 Oct in my location: 2 | Same, plus `HolidayReader` and `DemoHolidays` (demo Monday holiday) | Done in code. The owner's session ran the API with a demo Monday holiday and got 2.0. No back-end test |

The exact dates 3 to 7 Oct were not used. The session used equivalent dates (Friday to Tuesday, and a demo Monday holiday). I did not check which dates.

## Code changes

**Back end (`src/`)**

| File | What it does |
| --- | --- |
| `Api/Leave/LeaveRequestEndpoints.cs` | New. `POST /api/me/leave-requests` and `GET /api/me/leave-requests/preview`. Checks the key, body size (4 KB), dates 2000 to 2100, reason up to 500. Maps 409, 422 |
| `Api/Program.cs` | Modified. Adds `app.MapLeaveRequestEndpoints()` |
| `Domain/WorkingDayCalculator.cs` | New. Pure count of slots, working dates and skipped holidays |
| `Domain/LeaveRules.cs` | New. Pure rules: structure, past date, half day, no days, notice, max days |
| `Application/Leave/LeavePlanner.cs` | New. Shared steps of apply and preview. `LocalToday` turns the clock into the person's date |
| `Application/Leave/ApplyForLeave.cs` | New. Use case: replay check, plan, save. Hashes the body (SHA-256) |
| `Application/Leave/PreviewWorkingDays.cs` | New. Use case: same planner, writes nothing |
| `Application/Leave/LeaveDtos.cs`, `LeaveRuleError.cs` | New. Request and response records, error codes and messages |
| `Application/Leave/IApplicantReader.cs`, `IHolidayReader.cs`, `ILeaveTypeReader.cs`, `ILeaveApplicationStore.cs` | New. The four ports. I read only the store port's use, not each file line by line |
| `Application/DependencyInjection.cs`, `Infrastructure/DependencyInjection.cs` | Modified. Register the planner, two use cases, three readers and the store |
| `Infrastructure/Persistence/ApplicantReader.cs`, `HolidayReader.cs`, `LeaveTypeReader.cs` | New. Read-only (`AsNoTracking`) queries, one module each |
| `Infrastructure/Persistence/LeaveApplicationStore.cs` | New. The one transaction that writes `leave`, `balances` and `audit` |
| `Infrastructure/Persistence/DemoHolidays.cs` | New. Development only: a demo Monday and Wednesday holiday, at least 14 days ahead |
| `Infrastructure/Persistence/DatabaseInitializer.cs` | Modified. Calls `DemoHolidays` after the demo balances |

**Front end (`web/src/app/`)**

| File | What it does |
| --- | --- |
| `features/apply-leave/apply-leave.ts`, `.html` | New. Signals for the form, preview, saved and error. Stale preview answers are dropped. A new key after each form change |
| `features/apply-leave/apply-leave.scss` | New. Styles. Not read in detail |
| `features/apply-leave/leave-request.service.ts` | New. Typed `apply()` and `preview()` using `API_URL` |
| `layout/nav-items.ts` | Modified. New "Apply for leave" link for every role |
| `app.routes.ts` | Modified. `loaderFor()` opens `ApplyLeave` for `apply-leave`; other paths stay on the placeholder |

**Database**

No migration and no new table. The code uses tables from the earlier database work (4b): `leave.LeaveRequest`, `leave.LeaveRequestDay`, `leave.IdempotencyKey`, `balances.*`, `audit.AuditEvent`. I did not open the table configurations, so the unique index on the day rows is taken from design 3c, not from the code.

**Tests**

| File | What it checks |
| --- | --- |
| `features/apply-leave/apply-leave.spec.ts` | The screen (15 tests counted by `it(`). I did not read it line by line |
| `features/apply-leave/leave-request.service.spec.ts` | Success and a 422 for apply and preview (5 tests) |
| `app.routes.spec.ts`, `layout/nav-items.spec.ts`, `layout/top-bar.spec.ts` | Modified. The new route and the new link in each role's list |

There is no .NET test project (Phase 5). The owner's session ran 17 web test files, 102 tests, all passing, and a web build.

**Docs and tooling**

| File | What it is |
| --- | --- |
| `ai/specs/us03-apply-leave-working-days/` (spec, plan, research, data-model, quickstart, tasks, contracts, checklists) | Spec set for SCRUM-10. I read tasks and research in part, the others lightly |
| `ai/learning/us03-apply-leave-working-days.md` | The learning note. Not read |
| `ai/specs/INDEX.md` | Modified. Adds a row for US-03, status Review |
| `ai/steering/structure.md`, `.specify/memory/constitution.md` | Modified. Allow one named cross-module store (constitution 1.2.0) |

## Design versus code

| Point | Design says | Code does | Match? |
| --- | --- | --- | --- |
| Path | 3d (1277972): `/api/v1/leave-requests` | `/api/me/leave-requests`, no version, like `/api/me/balances` | Differs |
| Preview call | 3d: `POST /leave-requests/preview` | `GET /api/me/leave-requests/preview` with a query string | Differs |
| Preview, rule failures | 3d: 200 with `warnings`, plus `availableBefore` and `availableAfter` | Rule failures are 422 with a `code`. No balance numbers. Returns `daysCount`, `workingDays`, `skippedHolidays` (dates only) | Differs |
| Error codes | 3d: `leave_overlap`, `insufficient_balance`, `notice_too_short`, `max_days_exceeded`, `year_end_span`, `idempotency_key_reused` | `leave.overlap`, `balance.insufficient`, `notice.short`, `max.consecutive`, `year.cross`, `idempotency.mismatch` | Differs |
| Status codes | 409 overlap, 422 other rules | 409 overlap, 422 other rules | Yes |
| Missing Idempotency-Key | 3d: 428 `precondition_required` | 400 with a plain `detail`, no `code` | Differs |
| 400 body | 3d: `validation_failed` with an `errors` map | Plain `detail` text | Differs |
| Submit response | 3d: 201, Location, ETag, leaveType object, reason, `submittedAtUtc`, `availableAfter` | 201 with id, leaveTypeId, dates, halfDay, daysCount, status, leaveYear. No Location, ETag or the rest | Differs |
| Body has no employee, status, count | 3d: server decides | Body record has none of them. Person from `sub` | Yes |
| Same key, other body | 3d: 422 | 422 `idempotency.mismatch` | Yes |
| Submit order | 3c: request, day rows, guarded balance, ledger Deduction, audit, outbox | Same order, plus the saved key. No outbox row | Partly |
| Outbox email to manager | 3c: written at submit | Not written (notifications are 4g) | Not built |
| Guarded update | 3c: `UPDATE ... WHERE Available >= @days` | `ExecuteUpdateAsync` with `Available >= days` in one transaction | Yes |
| Overlap | 3c: unique (EmployeeId, LeaveDate, Slot) | A pre-check for a clear answer, and the unique index (from the design) as backstop | Yes |
| Rule 7, 8 | Policy: "3 working days", "15 days" | Notice = working days strictly between today and start. Max = calendar days. Owner has not confirmed (research R3) | Differs (an assumption) |
| Rule 6 probation, rule 9 certificate | Policy: enforced | Not enforced in this story | Not built |
| Screen S-03 | 3a (65835): half-day checkbox plus radios, live summary with "balance after", Cancel button, Apply on the dashboard | A half-day dropdown only when start equals end, no "balance after", no Cancel, link in the top bar | Partly |
| Half-day support per type | Seeded types have `AllowHalfDay` | The screen cannot know it; the server refuses | Partly |
| Auth and over-posting | Security 2, 4, 8 | `sub` only, own data only, separate DTOs | Yes |
| Audit | NFR: every state change audited, no free text | `leave.apply` row with ids, dates, days. No reason | Yes |
| Time | `TimeProvider`, `DateOnly` | Both used | Yes |

**Design documents used:**
- Jira SCRUM-10 (facts from the prompt)
- Confluence LMA: Leave-Policy Rules 229377, Phase 1 Requirements 98443, NFR 262145, Phase 3a UI/UX 65835, Phase 3c Database 1048577, Phase 3d API 1277972 (excerpts from the prompt; 3d is "drafted, awaiting answers")
- `ai/steering/*`, `.specify/memory/constitution.md` 1.2.0, `ai/specs/us03-apply-leave-working-days/` (tasks, research)

**Looked for and not found:** class design (3e), Angular design (3f) and security design (3g) in the LMA space. Read only as titles or outline: 3c Part 2 (1179649). Not read: 3b HLD (1277954), interview Q&A (294913), data protection (98460).

## State of the tasks

`us03-apply-leave-working-days/tasks.md`: T001 to T029, all ticked (29 of 29). I counted the ticked lines I could see; the long lines were cut in my search, so I did not read each one.
- Done: T001 to T029 (baseline, domain, readers, store, endpoints, screen, preview, full checks, reviews, INDEX and `/learn`)
- Open: none in the file. Outside the file: no commit, no pull request, `/status` not run, and no browser check of the screens.

`INDEX.md` says "T001 to T028 done". The tasks file ticks T029 too. The INDEX line is a little behind.

## Findings and gaps

- **Jira says To Do, the code is built.** All 29 tasks are ticked, but nothing is committed and there is no pull request. The Jira status and the real state do not match.
- **Contract differs from 3d** in path and version, preview verb and failure style, error code spelling, 400 versus 428, 400 body, and the 201 response. The code follows `contracts/leave-requests-api.md` in the repo. 3d is still a draft, so it may be the one to update. Your decision.
- **No "balance after" on the screen.** The wireframe shows it. The preview returns no balance numbers.
- **Rule 6 (probation) and rule 9 (medical certificate) are not enforced.** A new joiner can apply for Annual in the first 3 months. The spec lists this as out of scope (research R9).
- **Rules 7 and 8 use the owner's assumption** (research R3, not confirmed). Notice counts working days strictly between today and the start. The maximum counts calendar days, weekends included.
- **Rule 8 is a refusal.** The design says longer leave goes to an HR exception flow after the MVP. The code only refuses with "Longer leave needs HR".
- **Freed days are not built.** Design 3c says day rows are deleted on reject and cancel. That is increment 4e. Until then the unique index would block re-applying for days of a rejected or cancelled request.
- **Tests are thin.** No .NET test project, so the NFR "90%+ unit-test coverage on domain rules" is not met. The latency NFR (p95 under 600 ms at 50 users) was not load tested. The owner's session only timed single calls (under 0.1 s).
- **The Angular screens were never driven in a browser.** Only Vitest covers them.
- **Earlier spec out of date.** The new nav link changes the table in `ai/specs/t02-angular-shell/contracts/navigation.md`, which was not updated.
- **Local dev data.** `DemoHolidays` adds demo holidays on every Development start. The owner's checks left about 25 test requests in the dev database for demo users.
- **Demo holidays use the UTC date** for "today" (`DemoHolidays.cs`), while the rules use the person's local date. It only picks demo dates 14 days ahead, so the effect is small.
- **What the owner's session ran** (I did not run it): back-end build with 0 warnings and 0 errors; web tests 102 passing; web build; manual API calls for both Jira examples, 10 counting ranges, half days, each rule, replay, parallel requests, preview equals saved count, audit rows without reason text, and ledger reconciliation with no mismatch.
- **What I could not verify:** any of the above results, GitHub (the `gh` tool is missing here), and the table configurations.

## Why it is built this way

| Decision | Chosen | Rejected, and why |
| --- | --- | --- |
| Counting unit | Half-day slots; `DaysCount` = slots x 0.5 | Whole-day integers: cannot hold half days and could drift from the saved rows |
| Apply and preview | One `LeavePlanner` used by both | A copy in the screen or in each use case: the two could disagree (3d learning point 12) |
| Cross-module write | One named store, allowed by constitution 1.2.0 | Several ports and several saves: the request, deduction and audit row could half-succeed |
| Balance change | One guarded `UPDATE ... WHERE Available >= days` | Read, subtract in code, write back: two requests could overspend |
| Safe retry | Idempotency-Key plus a hash of the body, and a unique index on day rows | No key: a double click saves twice |
| Order of checks | Replay check before the rules | Rules first: a retry after midnight would be refused for a request that is already saved |
| "Today" | The person's local date from `TimeProvider` | UTC date: wrong for Bengaluru near midnight |
| Cheap checks first | Structure (end before start, year cross) before reading holidays | Count first: a huge range would cost work before being refused |

## Interview practice

1. **Q:** How do you count working days? **A:** Loop over the dates, skip Saturday, Sunday and holidays of the person's location. Count half-day slots and multiply by 0.5. The function is pure (no clock, no database), so it is easy to test.
2. **Q:** How do you stop a balance going below zero when two requests arrive together? **A:** One atomic `UPDATE` that subtracts only when `Available >= days`. If zero rows change, there was not enough, and the transaction rolls back. A database `CHECK` is the second line of defence.
3. **Q:** How do you make a double click safe? **A:** The screen sends an Idempotency-Key. The server saves the answer with the key and a hash of the body. The same key and body returns the saved 201. The same key with a different body is refused with 422.
4. **Q:** Why does preview share code with apply? **A:** Both run `LeavePlanner`. The screen never has its own copy of the day rule, so the count shown is the count a submit saves.
5. **Q:** Why 409 for overlap and 422 for the other rules? **A:** Overlap is a conflict with another record. The other failures mean the request is understood but breaks a rule.

**Teach-back (answer in your own words):**
1. Walk through AC2 step by step: which function counts, which reads the holiday, and what is saved in each table?
2. Two identical submits arrive at the same moment. Which three safeguards stop a double save, and in what order do they act?
3. The code uses `/api/me/leave-requests` and 422 for preview failures, but design 3d says `/api/v1/leave-requests` and 200 with `warnings`. What would you check or decide before changing either side?
