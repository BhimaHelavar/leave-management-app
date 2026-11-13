# US-03 (SCRUM-10): Apply for leave with an accurate working-day count

## 1. What we built

A signed-in person can apply for leave. They pick a leave type, a start date, an end date, and (for a single day) an optional half day.

The server counts the working days. Weekends and the location's holidays are not counted. It checks the policy rules (notice, maximum length, past dates, leave year, overlap, balance). If all pass, it saves the request as Pending and takes the days from the balance. All of that happens in one database transaction.

The form also shows a preview of the count. The preview calls the server. It does not count in the browser.

There is no database migration. The tables came from increment 4b.

## What was and was not verified

Read this first. An interviewer will ask how you tested it.

- **No .NET test project exists.** It is planned for Phase 5. The Domain code is pure, so it is easy to unit test later. Back-end checks were manual, from `quickstart.md`.
- **The Angular screens** are covered by Vitest specs (with `HttpTestingController`). They were **never driven in a browser**.
- **Page timing** (the 3-second goal, SC-003) was measured **only at the API level**, not in the browser.
- **The review fixes** (section 6) were checked against the **running API**. They were not covered by automated tests.
- Not built here: probation (rule 6), the sick medical certificate (rule 9), and freeing day rows on reject or cancel (increment 4e).

## 2. Key concepts

### 2.1 Counting from half-day slots

**What it is.** A *slot* is half a working day: morning (AM) or afternoon (PM). A full working day is two slots. A half day is one slot. `DaysCount` is the number of slots times 0.5.

**How it works.** `WorkingDayCalculator.Count` loops from start to end. It skips Saturday and Sunday. It skips holidays and records them as "skipped". For every other date it adds slots. If the request is a half day on a single date, it adds one slot (AM for `FirstHalf`, PM for `SecondHalf`). Otherwise it adds AM and PM. The same slot list becomes the `LeaveRequestDay` rows.

**Why slots, not integers.** The table `LeaveRequestDay` has one row per slot and a unique index on (EmployeeId, LeaveDate, Slot). If the count came from the same list as the rows, the two cannot disagree. It also makes half-day overlap right: AM on one request and PM on another is allowed. Counting an integer and writing rows separately gives two sources of truth that can drift.

**When and where.** Use this idea whenever a total must match its detail rows. Here it lives in Domain, as a pure function: no clock, no database.

**Sandwich rule off (rule 10).** A weekend or holiday between two leave days never counts. The loop does not look at its neighbours, so the rule is off by construction. A holiday on a Saturday is already skipped as a weekend, so it never reduces the count twice.

### 2.2 Rules stored per leave type

**What it is.** The numbers behind rules 7 and 8 are columns on `LeaveType`: `MinNoticeWorkingDays` (Annual 3, others 0) and `MaxConsecutiveDays` (Annual 15, others null = no limit).

**How it works.** `LeaveRules.FirstViolation` reads these numbers from its input. There is no `if (type == Annual)`.
- **Notice (rule 7)** = working days strictly between today and the start date. `WorkingDaysBetween` stops counting once it reaches the minimum.
- **Maximum (rule 8)** = calendar days, `End - Start + 1`. Weekends and holidays inside the range count toward the limit.

**Why.** Leave types are admin-configurable (rule 1). Code that hard-codes "Annual" would need a deploy to change a policy. The calendar-day reading of rule 8 is the safer one: nobody can use weekends to stretch a long absence past 15 days without HR. The other reading (working days) is more lenient. It is an owner decision recorded in `research.md` R3, and changing it touches one function.

**When and where.** Policy that business users may change belongs in data. A rule the database must always hold (balance not below zero) belongs in a CHECK constraint as well.

**Order of checks.** The first failure wins: type unknown, date range, year cross, date past, half day, no days, notice, maximum. Overlap and balance come later, at save time.

### 2.3 "Today" in the person's time zone

**What it is.** `LocalToday.For` takes the instant from `TimeProvider`, then converts it to the time zone of the person's location (`Location.TimeZoneId`, Asia/Kolkata).

**How it works.** It looks up the zone, converts the UTC instant, and takes the date part. If the zone id cannot be resolved on the host, it throws. The exception handler logs it and answers 500 with no detail. It never silently falls back to UTC.

**Why.** Bengaluru is UTC+5:30. At 20:00 UTC on 12 Nov it is 01:30 on 13 Nov there. A UTC date would say "today" is 12 Nov. The person could then apply for a day that already started, or be refused wrongly. The silent fallback was rejected because it would be wrong for some people and nobody would notice.

**When and where.** Any rule about "today" or "in the past" must use the user's date. The Domain has no clock, so the Application layer passes `Today` in. That also makes the rules testable with fixed dates.

**Known gap.** `GetMyBalances` (US-02) uses the UTC date for the leave year. It only matters on 31 December and 1 January. It was not changed in this story.

### 2.4 The server decides; preview uses the same planner

**What it is.** `LeavePlanner` holds the steps that Apply and Preview share: read the person, the leave type and the holidays; refuse a bad structure; count; check the rules.

**How it works.** `ApplyForLeave` calls the planner, then the store. `PreviewWorkingDays` calls the planner and returns the count, the working dates and the skipped holidays. Preview writes nothing and writes no audit row. It does not check overlap or balance. Only a submit decides those.

**Why.** The Jira story says the balance must be right. If the preview used different code, it could show 3 and save 2. One planner means one answer. The request DTO has no employee id, status or count, so a client cannot over-post them. The person comes from the `sub` claim.

**When and where.** Whenever a screen shows a number the server will later decide, ask the server and reuse the code path. Do not copy the logic into the browser.

### 2.5 One transaction, and the guarded UPDATE

**What it is.** `LeaveApplicationStore.SaveAsync` does the whole write inside one database transaction.

**How it works.** In order:
1. Look for a saved idempotency answer.
2. Overlap pre-check on `LeaveRequestDay`, for a friendly message.
3. Insert the `LeaveRequest` (Status always Pending).
4. Insert one `LeaveRequestDay` per slot.
5. Guarded balance update (skipped for a type with no balance, such as Unpaid).
6. Insert a ledger entry (`Deduction`, `Days = -DaysCount`).
7. Write the audit row (`leave.apply`: ids, dates, days. Never the reason, name or email).
8. Insert the idempotency row with the saved answer, then commit.

If any step fails, everything rolls back. Request, day rows, deduction, ledger entry, audit row and saved answer all succeed together or not at all.

**The guarded UPDATE.** In plain words: "subtract the days, but only if enough are left, and tell me if you did".

```sql
UPDATE balances.LeaveBalance
SET Available = Available - @days
WHERE EmployeeId = @e AND LeaveTypeId = @t AND LeaveYear = @y
  AND Available >= @days
```

EF Core writes this with `ExecuteUpdateAsync`. Zero rows changed means not enough balance (or no balance row). Then the store rolls back and answers 422.

**Why not read-then-write.** If you read `Available = 5`, subtract 3 in code and write back 2, two parallel requests can both read 5 and both write 2. One deduction is lost. In a guarded UPDATE the database checks and changes in one step. SQL Server locks the row, so the second request sees the new value. A CHECK constraint (`Available >= 0`) is the second line of defence.

**When and where.** Use the guarded UPDATE for any counter with a floor: balances, stock, seats. Use one transaction when several writes must agree.

### 2.6 Overlap: the unique index as backstop

**What it is.** The unique index `UX_LeaveRequestDay_Employee_Date_Slot` on (EmployeeId, LeaveDate, Slot) stops two requests from holding the same slot.

**How it works.** The pre-check query finds the usual overlap and returns a clear 409 `leave.overlap`. The index handles the race: two submits with different keys for the same dates both pass the pre-check. Both try to insert the day rows. One hits SQL error 2601 or 2627. The store rolls back and answers 409.

**Why both.** The check gives a good message. The index gives a guarantee. A check alone is not safe under concurrency.

**Hand-off to 4e.** The index blocks overlap whatever the request status. Rule 13 only blocks pending or approved requests. So when 4e rejects or cancels, it must free that request's day rows, or the person cannot re-apply. Not built here.

### 2.7 Idempotency keys

**What it is.** An *idempotent* operation gives the same result if you repeat it. The client sends an `Idempotency-Key` header (1 to 64 characters: letters, digits, `-`, `_`). The server saves the first answer under (EmployeeId, key). A repeat returns that answer and does not save or deduct again.

**How it works.**
- Same key and same body hash: return the saved 201.
- Same key and a different body: 422 `idempotency.mismatch`. The hash is SHA-256 of type, dates, half day and trimmed reason.
- The lookup is always filtered by the person, so one person cannot collide with another's key.

**The same-key race.** Two identical requests arrive together. Both pass the first lookup. Then one of three things can happen to the loser:
- It finds its own days taken (the winner committed). The store now looks for the saved answer for this key first. If found, it returns it.
- It hits the day index at insert. Same fix: roll back, look for the winner's answer.
- It hits the primary key of the idempotency row. Same fix.

Only when no answer exists for the key is it a real overlap (409). A double click therefore sees the original 201 and not an error after a successful save.

**Why the replay is checked before the rules.** `ApplyForLeave` looks for a saved answer before it plans. Suppose a request succeeded and the response was lost. The person retries after midnight, or the leave type was switched off meanwhile. The rules would now refuse a request that is already saved, and the person would send it again with a new key. The replay check first avoids that. An inactive person still gets 401 before anything, so a deactivated account sees none of its old answers.

**When and where.** Any POST that creates something or moves money, where a client may retry. The Angular screen keeps one key per form state and makes a new key when any field changes.

### 2.8 Constitution 1.2.0 and the one cross-module store

**What it is.** `structure.md` says code in one module does not read another module's tables. But this write touches `leave.*`, `balances.*` and `audit.*` and must be atomic.

**How it works.** Constitution Principle III was amended to 1.2.0 to allow one named cross-module store used in a single transaction. That is `LeaveApplicationStore`. Reads stay in three small ports (applicant, holidays, leave type), each reading only its own module. The store shares the scoped `LeaveDbContext` with `AuditWriter`, so the audit row joins the same transaction.

**Why.** One atomic write needs one unit of work. Keeping the exception in one named place keeps the rule meaningful.

**Rejected.** Three stores plus a unit-of-work abstraction. It is more code for the same effect at this size. Revisit when 4e repeats the pattern for approve and cancel.

### 2.9 Cheap checks before the expensive work

**What it is.** `LeaveRules.StructureViolation` checks only the dates: end before start, or start and end in different years.

**How it works.** The planner runs it before reading holidays or counting. A passing range is inside one calendar year, so at most 366 days. The endpoint also limits dates to 2000 to 2100, limits the reason to 500 characters, and limits the body to 4096 bytes.

**Why.** Without it, a range such as year 2000 to 9999 would make the server read holidays and loop over millions of days, only to refuse it afterwards. Validate cheap structure first; spend resources only on plausible input.

### 2.10 Malformed body: 500 in Development, and the fix

**What happened.** If the framework binds the body itself, a malformed JSON body throws. In Development, the developer exception behaviour turns that into a 500 instead of a 400.

**Local fix in the code.** The endpoint reads the body itself in `ReadBodyAsync` and catches `JsonException` and `BadHttpRequestException`. A wrong shape, an unknown `halfDay` text, a bad date, no body, or a body over the limit all return 400 in every environment. The preview query is parsed the same way. The same method sets `MaxRequestBodySize` to 4096 bytes. The framework default is about 30 MB.

### 2.11 The Angular screen

**What it is.** A standalone `ApplyLeave` component with a typed `LeaveRequestService`. Form values, the preview, loading and error states are signals. `canChooseHalfDay` and `canSubmit` are `computed`. Leave types come from the existing `BalanceService`, so there is no new endpoint.

**Stale preview guard.** Each preview call increments a counter, `previewCall`, and remembers its number. When an answer arrives, it is dropped if its number is not the latest. A slow reply for old dates then cannot overwrite the count for the dates now on screen. After a successful submit, the counter is bumped again so an answer still in flight is forgotten.

**Idempotency key.** A `crypto.randomUUID()` key is kept for retries of the same form. Any change to the form gives a new key.

**No counting in the browser.** The browser duplicating weekend and holiday logic would drift from the server.

**Errors.** The screen shows the server's `detail` when it has a known `code`, with a local map as fallback. Error text has a `role="alert"`.

## 3. Diagram

```text
Angular form                          API (minimal endpoint)                    SQL Server
------------                          ----------------------                    ----------
pick type + dates
  |-- GET /preview ------------------> read query (400 if bad) ---+
  |                                                               |
  |                                                          LeavePlanner  <-- shared
  |                                                               |
  |<-- {daysCount, workingDays, skipped}  (no write, no audit) ---+
  |
click Submit (Idempotency-Key)
  |-- POST /api/me/leave-requests ---> key ok? body ok? shape ok? (400)
  |                                    ApplyForLeave
  |                                      1. applicant active?  no -> 401
  |                                      2. saved answer for key? yes -> return it
  |                                      3. LeavePlanner:
  |                                           type active? -> structure check
  |                                           -> holidays -> Count (slots)
  |                                           -> rules (422 + code)
  |                                      4. LeaveApplicationStore
  |                                                                   BEGIN
  |                                                                    overlap pre-check      (409)
  |                                                                    INSERT LeaveRequest
  |                                                                    INSERT LeaveRequestDay (unique idx -> 409)
  |                                                                    UPDATE balance WHERE Available >= days
  |                                                                          (0 rows -> 422)
  |                                                                    INSERT ledger Deduction
  |                                                                    INSERT AuditEvent
  |                                                                    INSERT IdempotencyKey
  |                                                                   COMMIT
  |<-- 201 {id, daysCount, status: Pending, leaveYear}
```

## 4. Worked example

Use the Jira example. Fri 3 Oct to Tue 7 Oct, holiday on Mon 6 Oct. Annual leave. (Those dates are in the past, so real checks use any future Friday-to-Tuesday range. The app has two demo holidays for that: a Monday and a Wednesday at least 14 days ahead, logged at startup.)

1. Dates loop: Fri 3 (working), Sat 4 and Sun 5 (weekend, skip), Mon 6 (holiday, skipped list), Tue 7 (working).
2. Slots: Fri AM, Fri PM, Tue AM, Tue PM = 4 slots. `DaysCount` = 4 x 0.5 = **2.0**. Without the holiday it is 6 slots = **3.0**.
3. Rule 8: `End - Start + 1` = 5 calendar days. The Annual limit is 15, so it passes.
4. Rule 7 (notice): suppose today is Mon 29 Sep. Working days strictly between 29 Sep and 3 Oct are Tue 30, Wed 1, Thu 2 = 3. Annual needs 3, so it is allowed. If today were Tue 30 Sep, the count would be 2, so `notice.short` (422).
5. Save: one request row (Pending, `DaysCount` 2.0), 4 day rows, `Available` drops by 2.0 only if it was at least 2.0, one ledger row with `Days = -2.0`, one audit row, one idempotency row. Response: 201 with `daysCount` 2.0.
6. A retry with the same key and body returns the same 201 and deducts nothing more.
7. A second request for Mon 6 to Tue 7 with another key: the pre-check finds Tue AM and PM taken. 409 `leave.overlap`.

Half day: Tue 7 Oct only, `SecondHalf`: one slot (Tue PM), `DaysCount` 0.5.

## 5. Trade-offs and rejected alternatives

| Decision | Chosen | Rejected | Cost of the choice |
| --- | --- | --- | --- |
| Count | Slots, rows and count from one list | Integer count plus separate rows | A little more data per request |
| Balance write | Guarded UPDATE | Read, subtract in code, write | Raw-ish update; must handle 0 rows |
| Overlap | Pre-check plus unique index | Check only, or index only | Two mechanisms to explain |
| Cross-module write | One named store (constitution 1.2.0) | Three stores plus a unit-of-work abstraction | An accepted exception to a rule; revisit in 4e |
| Rule 8 | Calendar days | Working days | Stricter for the person; HR must confirm |
| Today | Person's time zone | UTC date | Needs the zone id to resolve on the host |
| Zone failure | 500 and log | Fall back to UTC | A visible failure instead of a wrong date |
| Preview | Server call | Count in the browser | An HTTP call per change; always right |
| Replay | Before the rules | After the rules | One extra read per request |
| Idempotency | Key table | Unique index only | Extra table and cleanup job later |
| Rules | Columns on `LeaveType` | Hard-coded Annual | A read per request |

## 6. What the reviews found and how it was fixed

The security and code reviews found four problems. Each fix was checked against the running API. There are no automated tests for them yet.

1. **Same-key race returned 409.** Two identical requests together: the loser failed on the day index and answered `leave.overlap`, even though the winner had saved the answer. Fix: after rollback, look for the saved answer for this key and return it. 409 only when none exists.
2. **Replay was behind the rule checks.** A retry after midnight or after the type was switched off would be refused. Fix: `ApplyForLeave` checks the saved answer before planning.
3. **No cap before counting a huge range.** A range like 2000 to 9999 could make the server loop and read holidays before refusing. Fix: `StructureViolation` runs before the holiday read and count, and the endpoint limits dates to 2000 to 2100.
4. **No request body size limit.** The framework would read up to about 30 MB before the reason limit applied. Fix: the endpoint sets the limit to 4096 bytes and answers 400 over it.

## 7. Interview questions

**1. Why store half-day slots instead of an integer day count?**
Slots give one source of truth. The count is slots times 0.5, and the same list becomes the day rows, so they cannot disagree. It also lets AM and PM be taken by different requests. Example: Fri to Tue with a Monday holiday gives 4 slots, so 2.0 days, and 4 `LeaveRequestDay` rows.

**2. How do you stop two parallel requests from overdrawing a balance?**
Use one atomic UPDATE with the condition in the WHERE clause, and check the rows changed. Never read, add in code and write back. A CHECK constraint is the backstop. Example: `UPDATE ... SET Available = Available - 2.0 WHERE ... AND Available >= 2.0`. If 0 rows change, the API answers 422 `balance.insufficient`.

**3. What is idempotency and how did you implement it?**
A repeat of the same operation must have the same effect as one call. The client sends a key. The server stores the key, a body hash and the response in the same transaction as the request. A repeat returns the saved response. A different body with the same key is refused. Example: a double click on Submit sends the same `Idempotency-Key` and gets the original 201, with one deduction.

**4. Two identical requests arrive at the same time. What happens?**
Both can pass the first lookup. The loser then fails on a unique index (the day rows or the key row). It rolls back, looks up the winner's saved answer and returns it. Only if there is no answer is it a 409. Example: review finding 1. The loser first returned 409; it now returns the saved 201.

**5. Why check the saved answer before the business rules?**
A saved answer means the request already succeeded. Rules that depend on the clock or on settings may now fail for a request that is saved. The person would resubmit under a new key. Example: a retry after midnight, when the notice check would now fail for Annual leave.

**6. Why is "today" taken in the user's time zone?**
The calendar date depends on the zone. Using UTC can allow or refuse the wrong day. The time comes from `TimeProvider`, which also makes it testable. Example: 20:00 UTC on 12 Nov is 01:30 on 13 Nov in Bengaluru, so "today" for a Bengaluru employee is 13 Nov.

**7. Why does the preview call the server instead of counting in Angular?**
The server decides every rule. A second copy of the weekend and holiday logic would drift. The preview shares `LeavePlanner` with submit, so it equals the saved count. Example: `GET /api/me/leave-requests/preview` returns `daysCount` 2.0 and `skippedHolidays: [Mon 6 Oct]`, and submit saves 2.0.

**8. Your Domain has no clock and no database. Why does that matter?**
Pure functions are easy to test and cannot hide a dependency. The Application layer passes in today and the holidays. The dependency rule stays intact. Example: `LeaveRules.FirstViolation` gets `Today` in its input record, so the Mon-for-Fri versus Tue-for-Fri notice cases can be tested with fixed dates in Phase 5.

**9. Why 409 for an overlap and 422 for the others?**
409 means a conflict with existing state (another request). 422 means the request is well formed but breaks a business rule. 400 means malformed. Example: overlap is 409 `leave.overlap`; a short notice is 422 `notice.short`; a bad date text is 400.

**10. The transaction spans three modules. Isn't that a design smell?**
Yes, it is an exception, so it was made explicit. A single atomic write needs one unit of work. The constitution was amended to allow one named store, with reads kept in small per-module ports. The alternative of three stores plus a unit-of-work abstraction was rejected for now as too much code. Example: `LeaveApplicationStore` writes `leave.*`, `balances.*` and `audit.*` in one commit.

## 8. Teach-back (answer in your own words)

1. Walk through what happens, step by step, from clicking Submit to the 201 response, and say which steps are in the transaction and why.
2. Explain why a double click and two tabs with different keys are handled differently. What does each one return, and what protects it?
3. Why is the rule for notice a count of working days "strictly between", and why does the maximum use calendar days? What would change if HR wanted the maximum in working days?

### My answers

(Write your answers here.)

## 9. Exercise (about 20 to 30 minutes, on paper)

Hand-count five ranges using this app's rules. Assume Annual (notice 3, max 15) and holiday on Wed 15 Oct and Mon 20 Oct.

1. Fri 10 Oct to Tue 14 Oct.
2. Tue 14 Oct to Thu 16 Oct.
3. Sat 18 Oct to Sun 19 Oct.
4. Fri 17 Oct to Tue 21 Oct, applied for on Thu 9 Oct.
5. Mon 20 Oct, `FirstHalf`.

For each, write the slot list, `DaysCount`, the first rule broken (if any), and the code the API would return. Then check your answers by calling the preview endpoint on a future range with the demo holidays, and note any difference.

Stretch: draw the timeline of two parallel POSTs with the same key (both pass step 1, one commits first) and mark where the loser fails and where it finds the winner's answer.
