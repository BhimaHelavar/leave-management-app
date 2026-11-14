# Research: Block requests that break my balance or overlap

No `NEEDS CLARIFICATION` items were left in the spec. The questions below are design choices checked against the existing code (`LeaveApplicationStore.cs`, `LeaveRuleError.cs`, the US-03 contract).

## R1. Are the balance block and the overlap block already built?

- **Decision**: Yes for both. Only the overlap message changes.
- **Evidence**: The store runs a guarded update `Available >= days` and returns `BalanceInsufficient` on zero rows, after rolling back (steps 4 and the rollback). It checks the person's taken day rows before writing and returns `Overlap`, and the unique index `(EmployeeId, LeaveDate, Slot)` settles races. Both refuse with nothing saved.
- **Rationale**: Re-building a reviewed rule would add risk for no gain.
- **Alternatives considered**: Move the balance check before the insert so a refused request never writes a row — rejected, the rollback already guarantees nothing is saved and the guarded update is the safe way to test a balance without a race.

## R2. How to give the dates to the message

- **Decision**: The store keeps the dates it already finds. `taken` is already loaded for the range; the clash is `taken` rows that match a wanted `(date, slot)`. The distinct dates of those rows, sorted, go into `LeaveRuleErrors.Overlap(dates)`.
- **Rationale**: No extra query on the normal path; the dates are already in memory.
- **Alternatives considered**: Run a second query for the dates only when refusing — works but repeats work the store already did. Return the dates from the unique-index exception text — rejected, SQL error text must never reach the client (security rule 10) and is not reliable to parse.

## R3. The race path (two submits at once)

- **Decision**: When the unique index refuses the insert, the store already rolls back and checks for a saved answer. If there is none, it now re-reads the person's taken day rows in the range (on the clean context after the rollback) and builds `Overlap(dates)` from the rows that match the wanted slots. If that read finds nothing (the other request vanished in between), it uses `Overlap` with an empty list, which keeps the old generic wording.
- **Rationale**: Keeps FR-010 (the guarantee does not weaken) and still names dates in the common race result.
- **Alternatives considered**: Always generic in the race path — simpler but inconsistent: the same clash would be named or not depending on timing. Retry the whole save — rejected, it hides a real conflict and can loop.

## R4. Shape of the message and a machine-readable field

- **Decision**: Message only. Text: `You already have a pending or approved leave request on 2026-12-01, 2026-12-02.` Dates are ISO `yyyy-MM-dd`, the same style the screen already shows for counted days. At most 10 dates, then `and N more`. No new field on the refusal.
- **Rationale**: The apply screen already shows the server's `detail` text, so the message alone satisfies the story with no web change. ISO dates are unambiguous and already used on screen. The cap keeps a long clash (types with no maximum) readable.
- **Alternatives considered**: Add a `clashingDates` array to the ProblemDetails extensions — more useful for a later "highlight these days" feature, but it adds a contract field nobody consumes yet. Rejected for now (YAGNI); easy to add later without breaking the message. Friendly dates like `1 Dec 2026` — nicer to read but needs a culture choice on the server; deferred.

## R5. Overlap with an approved request

- **Decision**: No special case. The rule treats pending and approved alike because both hold day rows. Rejected and cancelled requests must not clash because **their day rows are released** (US-03 research R6 hand-off for 4e: free the days on reject and cancel).
- **Rationale**: The same query covers both statuses with one rule.
- **Honest limit**: Approve, reject and cancel are increment 4e and are not built. So today an approved request cannot be created, and a rejected or cancelled one cannot exist. FR-005 (rejected and cancelled never clash) holds by design but cannot be run until 4e. The quickstart checks the rule with a **pending** request and says so. When 4e is built it must delete or release the day rows, and its spec should name this test.
- **Alternatives considered**: Filter by status in the query — rejected, the day rows do not carry a status, and a join would be a second source of truth for what "holds a day".

## R6. Personal data in the message

- **Decision**: The message holds calendar dates only, and only dates of the caller's own requests inside the requested range. The query is already filtered by the employee id from the token.
- **Rationale**: Security rules 4 and 12. No other person's leave is read or shown, and no names or reasons appear.
- **Alternatives considered**: Name the clashing request id or leave type — rejected, not needed by the story and adds data to a message.

## R7. Testing without a .NET test project

- **Decision**: Keep the message builder a pure function in Application so unit tests can cover it in Phase 5 (empty list, one date, ten dates, eleven dates, unsorted input). Verify now with the manual API calls in the quickstart, including the exact Jira examples. Extend one existing Vitest case to prove the screen shows a message with dates. Say plainly that no .NET tests exist.
- **Rationale**: Same honest approach as US-03 and US-04.
- **Alternatives considered**: Add a .NET test project now — rejected, that is Phase 5 and outside this story.
