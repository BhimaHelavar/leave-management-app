# Research: Apply for leave with an accurate working-day count

Each item: **Decision**, **Why**, **Alternative rejected**. Items marked **Owner** need an answer before tasks are written.

## R1. How the day count is built

**Decision**: Build the list of working half-day *slots* first, then count. A full working day gives two slots (AM and PM); a half day gives one. `DaysCount` = number of slots x 0.5. The same list becomes the `LeaveRequestDay` rows.

**Why**: The table `LeaveRequestDay` already has one row per half-day slot and a unique index on (EmployeeId, LeaveDate, Slot). Deriving the count from the same list as the rows means the count and the rows cannot disagree, and overlap on a half day works (AM on one request, PM on another is allowed).

**Alternative rejected**: count days as an integer and write rows separately. Two sources of truth can drift.

## R2. One store for the cross-module write

**Decision**: One port, `ILeaveApplicationStore`, implemented in Infrastructure, does the whole write in one database transaction: idempotency check, overlap check, insert request, insert day rows, guarded balance update, insert ledger `Deduction`, insert audit row, save idempotency answer. Reads (applicant, holidays, leave type) stay in three small ports.

**Why**: Security rule 14 and the product rule "days are deducted at submit" need the request, deduction and audit to succeed or fail together. `structure.md` forbids one module reading another's tables, but a single atomic write needs one unit of work. A single store keeps that exception in one named place.

**Owner decision**: constitution Principle III was amended to 1.2.0 on 2026-11-13 (analysis finding C1, option a) to allow one named cross-module store in a single transaction. This is that store. See Complexity Tracking in `plan.md`.

**Alternative rejected**: three stores plus a unit-of-work abstraction. More code for the same effect at this size. Revisit when 4e adds approve and cancel and the same pattern repeats three times.

## R3. Rule 7 notice and rule 8 maximum (**Owner** to confirm)

**Decision (default used in this plan)**:
- **Rule 7**: notice = number of working days strictly between today and the start date. Apply on Mon for Fri gives Tue, Wed, Thu = 3: allowed. Apply on Tue for Fri gives 2: refused. The minimum comes from `LeaveType.MinNoticeWorkingDays` (3 for Annual, 0 for the others), not from a hard-coded "Annual" check.
- **Rule 8**: length = calendar days from start to end inclusive (end - start + 1), compared with `LeaveType.MaxConsecutiveDays` (15 for Annual, null = no limit).

**Why**: The rules are stored on the leave type, so admins can change them (rule 1). The calendar-day reading of rule 8 is the safer one: it stops a person using weekends to stretch a long absence past 15 days without HR.

**Alternative rejected**: working-day length for rule 8 (more lenient). Choose this if HR means "15 days of leave". Changing it later only changes one function in `LeaveRules`.

## R4. "Today" and the person's time zone

**Decision**: Take the instant from `TimeProvider`, convert to the person's location time zone (`Location.TimeZoneId`, Asia/Kolkata), and use that date for "is this in the past" and for the notice count.

**Why**: Near midnight UTC the calendar date in Bengaluru is a day ahead. A UTC date would let someone apply for "today" late in the evening, or refuse a valid request early in the morning.

**Note**: `GetMyBalances` (US-02) uses the UTC date for the leave year. It only matters on 31 December and 1 January. Record this for the owner; do not change US-02 inside this story.

**Risk**: time zone ids must resolve on the host. `Asia/Kolkata` works on Windows with ICU in .NET 10. If it throws on the Demo host, fall back with a clear error, not a silent UTC.

## R5. Double submit

**Decision**: Require an `Idempotency-Key` header (1 to 64 URL-safe characters) on `POST`. The store checks `leave.IdempotencyKey` for (EmployeeId, key) inside the transaction. Same key and same request hash: return the saved answer. Same key and a different hash: 422. The row is written in the same transaction.

**Why**: The table and the 4b design already exist for this (FR-017, SC-005). The unique index on slots is the second line: two parallel submits with different keys for the same dates still cannot both win (catch SQL error 2601 or 2627 and answer 409).

**Same key in parallel**: two requests with the same key can both pass the first check. The second then fails on the key row's primary key (or on the overlap index first). The store rolls back and returns the winner's saved answer, so the retry sees the original 201, not a 409. Without this, a double click would show an error after a successful save.

**Alternative rejected**: rely only on the unique index. It stops a duplicate request but not a double deduction on a retried identical call that happens to be re-ordered, and gives the user an error instead of the original answer.

## R6. Freed days after reject or cancel (hand-off to 4e)

**Decision**: Record now, decide in 4e. The unique index on `LeaveRequestDay` blocks overlap whatever the request status. Rule 13 only blocks **pending or approved** requests. So when 4e rejects or cancels a request it must free those days (delete its `LeaveRequestDay` rows) in the same transaction, or the person cannot re-apply for the same dates.

**Why**: The data model design (3c) assumed this. It conflicts with "no hard deletes" in `database-efcore.md`, which targets entities, not these derived slot rows. **Owner**: confirm that deleting day rows is acceptable, or choose a filtered unique index that ignores rejected and cancelled requests (needs a migration and a status copy on the day row).

**Effect on this story**: none. Nothing here can reject or cancel.

## R7. Ledger and the dashboard figures

**Decision**: Write one ledger row per request: `EntryType = Deduction`, `Days = -DaysCount`, `RequestId` set, `CreatedByEmployeeId` = the applicant. Skip both the update and the ledger row for types with no balance (`AccrualMode = None`, Unpaid).

**Why**: US-02 derives "pending" from ledger Deduction and Restore entries and from pending requests. A missing ledger row would make the dashboard disagree with the stored balance. The guarded update is `UPDATE LeaveBalance SET Available = Available - @d WHERE EmployeeId = @e AND LeaveTypeId = @t AND LeaveYear = @y AND Available >= @d`. Zero rows changed means "insufficient balance" (422), including when no balance row exists. The `CHECK (Available >= 0)` is the second line.

**Alternative rejected**: read the balance, subtract in code, write back. Two parallel requests could both pass (forbidden by `database-efcore.md`).

## R8. Holidays for the demo and the tests

**Decision**: Use the existing `Holiday` table, filtered by the applicant's `LocationId`. Holiday maintenance (4f) stays out of scope. `DevSeeder` already adds holidays; the quickstart must name which dates are seeded. If the Jira example dates (Fri 3 Oct to Tue 7 Oct, holiday Mon 6 Oct) are not seeded for a future year, add them to `DevSeeder` as an idempotent top-up and say so in the task. The top-up adds two demo holidays: a Monday (H) and a Wednesday (W) in a different week, so the quickstart counting table can cover both.

**Why**: SC-001 needs a known holiday on a known Monday. Test with real dates in a year the app accepts (back-dating is refused, so the range must be in the future).

**Note**: 3 Oct to 7 Oct is a Fri-to-Tue pair only in 2025 (and 2031). The check uses any future Fri-to-Tue pair with a holiday on the Monday.

## R9. Rules that apply but are not enforced here

**Decision**: Probation (rule 6) and the sick medical certificate (rule 9) are not enforced in this story, per the spec's assumptions. `LeaveType.ProbationBlockMonths` is 3 for Annual, so a new joiner can currently apply for Annual in their first 3 months.

**Why**: Separate stories. **Owner**: confirm that gap is acceptable until those stories, or pull rule 6 into this one (small: `JoinDate` is on `Employee`).

## R10. Errors and status codes

**Decision**: 400 for a malformed request (missing key, bad date text, unknown enum). 422 for a rule failure, with a `code` extension so the screen can show the right message (list in the contract). 409 for an overlap, whether found by the check or the unique index. 401 as for the other `/api/me` endpoints. No 403 and no 404: the person can only act on themselves, and an unknown or inactive leave type is a 422.

**Why**: matches `backend-dotnet.md` (409 conflicts, 422 rule failures). Messages never contain SQL text or other people's data.

## R11. Angular screen

**Decision**: A new standalone `ApplyLeave` component with a typed `LeaveRequestService`. Signals for form values, the preview, the loading state and the error. The preview calls the server (debounced by waiting until type and both dates are valid); the screen never counts days itself. The leave types for the dropdown come from `BalanceService` (already returns types), so no new endpoint is needed. Half-day choice appears only when start equals end and the type allows it.

**Why**: FR-018 requires the preview to equal the saved count; the only way to guarantee that is to ask the server. Reusing the balances call avoids a new endpoint.

**Alternative rejected**: count in the browser. It would duplicate the holiday and weekend logic and drift (breaks "server decides").
