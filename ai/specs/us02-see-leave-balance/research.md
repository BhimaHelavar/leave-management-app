# Research: See my leave balance per leave type

Each item: decision, why, what was rejected.

## R1. Derive accrued, used and pending; do not store them

**Finding**: `LeaveBalance` stores only `Available`, and it is already net of pending days because days are deducted at submit (product.md). The ledger (`LeaveBalanceEntry`) holds one signed row per change, with types Grant, Accrual, CarryForward, Expiry, Deduction, Restore, Adjustment.

**Decision**:
- `available` = `LeaveBalance.Available` (the guarded current value).
- `pending` = sum of `DaysCount` of the person's `Pending` requests of that type in the leave year.
- `held` = `-(sum of Deduction and Restore entries)` = days taken out for requests that are pending or approved (a reject or cancel adds a Restore, so it falls out).
- `used` = `held - pending` (approved requests).
- `accrued` = `available + held` = the sum of Grant, Accrual, CarryForward, Expiry and Adjustment entries (the net entitlement, after expiry).

Check against the Jira examples: 10 accrued, 2 approved, 0 pending gives available 8, used 2, pending 0. With a pending request of 3 (ledger Deduction 3 on top), available 5, pending 3, used 2, accrued 10. Both match.

**Why**: no new column and no migration, and the numbers cannot drift from the ledger (golden rule: constraints and the ledger stay the source of truth). The rejected option, adding `Accrued/Used/Pending` columns, would need a migration, and every balance change would have to update four columns in step.

**Rejected**: calculating on the screen (breaks FR-010 and security rule 1).

## R2. Two ports, not one join

`structure.md` says code in one module does not read another module's tables. Pending days live in `leave.LeaveRequest`; balances live in `balances.*`.

**Decision**: `IBalanceReader` (balances module) and `IPendingLeaveReader` (leave module), both defined in Application and implemented in Infrastructure. The use case `GetMyBalances` merges the results in memory (at most seven rows).

**Rejected**: one LINQ join across both schemas. It is shorter, but it breaks the module rule that this project uses to keep a later split possible.

## R3. Current leave year and "today"

From `TimeProvider`: `year = today.Year`. The leave year is the calendar year (seed data and `LeaveRequest.LeaveYear` both use it). Showing past years is out of scope (spec assumption).

## R4. Which leave types are shown

- Show a card for each active leave type that has a `LeaveBalance` row for the year **or** whose accrual mode is not `None`.
- Unpaid has `AccrualMode.None` and no balance: it is returned with `hasBalance = false` and no numbers, and the screen shows "No balance, unpaid". (FR-006)
- A type with a zero-day row shows 0 (spec edge case).

**Alternative**: hide Unpaid entirely. Rejected, because a person who sees no Unpaid card may think the type does not exist. Open for the owner to overrule.

## R5. Carry-forward part (FR-007)

Policy says only: Annual, up to 10 days, expires 31 March (rule 4). It does not say whether carry-forward days are used first.

**Decision (assumption, to confirm)**: carry-forward days are used first. The carry-forward part still available = `max(0, CarryForwardGranted - held)`, capped at `available`, where `CarryForwardGranted` is the sum of `CarryForward` ledger entries whose `ExpiresOn` is on or after today. The expiry date is the earliest such `ExpiresOn`. If there are none, the fields are null.

**Why**: it is the usual company policy and it is the safe one for the employee: the screen warns about days that will lapse. If the owner prefers "used last", only this rule changes (one Domain function).

**Rejected**: showing carry-forward as a separate leave type. It is part of the Annual balance, not a type.

## R6. Demo data, so the feature can be seen and verified

4d and 4e do not exist, so nobody can submit or approve a request in the app. `DevSeeder` writes only opening Grant rows (Annual 12, Sick 10, Casual 6) and returns early if any employee exists, so an existing dev database will not get new rows.

**Decision**: add a small, idempotent demo top-up (new method called from `DatabaseInitializer` in Development only) that, for one demo person, adds the Jira scenario: Annual with Grant 10, one Approved request of 2 days and one Pending request of 3 days, plus the matching Deduction ledger rows and a `LeaveBalance` of 5. It adds rows only when that person's scenario is missing. A second person gets a carry-forward example.

**Owner decision needed**: this is the least destructive path (adds rows, no reset). Resetting the database to re-run `DevSeeder` would be a destructive action and is not proposed.

**Alternative**: wait for 4d and 4e and verify through the app. Rejected for now: it blocks this story on two later increments (the spec assumption asks for this decision).

## R7. Endpoint shape and authorization

`GET /api/me/balances`, no parameters. The employee id is taken from the `sub` claim, as `/api/me` does. No role policy is needed (everyone is an Employee); the fallback policy requires sign-in. Because there is no id in the URL, there is nothing to probe (security rule 4). Mapped in its own file with a `Map...` extension (backend rule).

**Rejected**: `GET /api/employees/{id}/balances`. It needs a resource rule and invites ID probing, and this story only needs "my own". A manager's team view comes later (out of scope).

## R8. Front end

A standalone `Dashboard` component replaces the placeholder for the `dashboard` path only. A typed `BalanceService` calls the endpoint through the existing `api-config.ts` base URL and interceptor. State is signals (`loading`, `error`, `balances`); template uses `@if`/`@for`. Visible loading and error states (rule: usable screens). A failed load shows an error, never zeros (spec edge case).

## R9. Rounding and display

Values are `decimal` with one decimal place on the server (column precision 5,1) and are sent as JSON numbers. The screen prints them as received (7.5, not 8). No rounding in the client.
