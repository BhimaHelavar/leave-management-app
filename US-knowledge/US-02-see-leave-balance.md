# US-02: See my leave balance per leave type

**Jira:** SCRUM-9 · Story · Done · High · epic SCRUM-7 (Leave Management App: MVP) · SCRUM Sprint 4 (active)
**Code:** no pull request · branch `SCRUM-9-US-02-See-my-leave-balance-per-leave-type` · base `d91c678` (same as `main`) plus the uncommitted working tree · 28 files (6 modified: +15, -3; 22 new, not counted in lines)
**Written:** 2026-11-10. I read the code and the design documents. I did not build or run anything, and this skill did not run the build or the tests. The branch has zero commits of its own, so "the code" below is the owner's working tree on top of `d91c678`.

## What the story is

An employee opens the dashboard and sees one card for each leave type. Each card shows how many days were accrued (earned), used, pending (waiting for a decision) and available (still free to take).

If you have 10 Annual days and 2 are used, the card says 8 available and 0 pending. If you also have a 3-day request waiting, it says 5 available and 3 pending.

**Why it matters:** it answers the employee's first question, "Is my balance right?" (requirement BR-02, Must).

## What the code delivers

```
Dashboard (/dashboard) --GET /api/me/balances + Bearer--> BalanceEndpoints
   |                                                         | sub claim -> employeeId
   |                                                    GetMyBalances (use case)
   |                          +------------------------------+-----------------------+
   |                  IBalanceReader                                       IPendingLeaveReader
   |             BalanceReader (balances.*)                              PendingLeaveReader (leave.*)
   |        LeaveType + LeaveBalance + LeaveBalanceEntry                    LeaveRequest (Pending)
   |                          +------------------+----------------------------+
   |                                  BalanceSummary.Derive (Domain)
   +<-- 200 { leaveYear, balances[ {accrued, used, pending, available, ...} ] }
   cards, or an error box with "Try again" (never zeros)
```

**The key idea: ledger and balance row.**

- `balances.LeaveBalance` holds one row per person, type and year, with a stored `Available`. This is the fast number.
- `balances.LeaveBalanceEntry` is the ledger: an append-only list of signed movements (Grant, Deduction, Restore, CarryForward, Adjustment). Nothing in it is edited.
- Both are written in one transaction, so `Available` always equals the sum of the ledger (design 3c, option C).
- Days are taken out ("held") when a request is submitted, not when it is approved. So `Available` already excludes pending days.

**How the four figures are derived** (`BalanceSummary.Derive`, nothing extra is stored):

- `held` = minus the sum of Deduction and Restore entries. It covers pending plus approved days.
- `accrued` = `available + held`
- `used` = `held - pending`
- `pending` = the sum of `DaysCount` of Pending requests (from the `leave` module)
- `available` = the stored value, so `accrued - used - pending = available` always holds.

1. The Dashboard calls `GET /api/me/balances`. The caller is the token's `sub` claim. There is no id in the URL or body, so there is nothing to probe.
2. `GetMyBalances` takes "today" from `TimeProvider`, sets the leave year to today's year, and calls two ports (interfaces). One reads the `balances` tables, the other reads `leave.LeaveRequest`. The use case joins them, so no module reads the other's tables.
3. A type with no balance row and `AccrualMode.None` (Unpaid) returns `hasBalance: false` with all numbers null. A type that accrues but has no row yet shows zeros.
4. Carry-forward: `CarryForwardRemaining = min(available, max(0, carryForwardGranted - held))`. It assumes carry-forward days are used first. Only live entries count (`ExpiresOn` is today or later).
5. The Angular `Dashboard` shows a loading line, an error box with a retry button, or the cards. It shows numbers exactly as received.

**Example:** Asha Nair (demo data) has Annual 10 accrued. She has an approved 2-day request and a pending 3-day request. The ledger has Deduction -2 and Deduction -3, so `held` = 5. `LeaveBalance.Available` = 5. Pending from `leave.LeaveRequest` = 3. The card shows accrued 10 (5+5), used 2 (5-3), pending 3, available 5. Ravi Kumar has only the approved 2 days: held 2, pending 0, so accrued 10, used 2, available 8.

## Acceptance criteria: where each one is delivered

| Criterion (from Jira) | Delivered by | State |
| --- | --- | --- |
| AC1. 10 Annual accrued, 2 used: dashboard shows 8 available and 0 pending | `BalanceSummary.Derive`, `GetMyBalances`, `Dashboard`; demo person Ravi Kumar in `DemoBalanceScenarios.cs`. Web test: `dashboard.spec.ts` (figures from a stubbed service) | Partly. Code and demo data exist. Nobody has run the API and checked it (T014 open). Not covered by a back-end test |
| AC2. A pending request of 3 days: available shows 5 | Same, with Asha Nair's demo data (pending 3, available 5). `dashboard.spec.ts` checks available 5 and pending 3 on the screen | Partly. Same reason |

## Code changes

**Back end (`src/`)**

| File | What it does |
| --- | --- |
| `Api/Balances/BalanceEndpoints.cs` | New. `GET /api/me/balances`; id from `sub`; 401 ProblemDetails if `sub` is not a number; no role policy |
| `Api/Program.cs` | Modified. Adds `app.MapBalanceEndpoints()` |
| `Application/Balances/BalanceDtos.cs` | New. `MyBalancesResponse` and `LeaveTypeBalance` (nullable numbers) |
| `Application/Balances/GetMyBalances.cs` | New. The use case: year, two reads, derive figures, Unpaid and carry-forward handling |
| `Application/Balances/IBalanceReader.cs`, `IPendingLeaveReader.cs` | New. The two ports, one per module |
| `Application/DependencyInjection.cs`, `Infrastructure/DependencyInjection.cs` | Modified. Register the use case and the two readers (scoped) |
| `Domain/BalanceSummary.cs` | New. Pure maths: `Derive` and `CarryForwardRemaining`. Uses `decimal` |
| `Infrastructure/Persistence/BalanceReader.cs` | New. Three `AsNoTracking` queries (types, balances, entries), merged in memory |
| `Infrastructure/Persistence/PendingLeaveReader.cs` | New. Sums `DaysCount` of Pending requests per leave type for the year |
| `Infrastructure/Persistence/DemoBalanceScenarios.cs` | New. Demo top-up for Ravi, Asha and Meera Rao (carry-forward) |
| `Infrastructure/Persistence/DatabaseInitializer.cs` | Modified. Calls the demo top-up after `DevSeeder` |

**Front end (`web/src/app/`)**

| File | What it does |
| --- | --- |
| `features/dashboard/balance.service.ts` | Typed `getMyBalances()` using `API_URL` |
| `features/dashboard/dashboard.ts`, `.html`, `.scss` | Signals `loading`, `error`, `balances`; cards; error with retry. `.scss` not read in detail |
| `app.routes.ts` | Modified. The `dashboard` path loads `Dashboard`; the others stay on the placeholder |

**Database**

No migration and no new table. The code reads existing tables. Only demo rows are added at start-up in Development.

**Tests**

| File | What it checks |
| --- | --- |
| `features/dashboard/balance.service.spec.ts` | Success and error (2 tests) |
| `features/dashboard/dashboard.spec.ts` | Figures, carry-forward line, no carry-forward line, no-balance card, error with no figures (5 tests) |
| `app.routes.spec.ts` | Modified. Stubs `BalanceService` so the routing tests do not load data |

There is no .NET test project (Phase 5). Back-end checks are manual (`quickstart.md`).

**Docs and tooling**

| File | What it is |
| --- | --- |
| `ai/specs/us02-see-leave-balance/` (spec, plan, research, data-model, quickstart, tasks, `contracts/balances-api.md`, `checklists/requirements.md`) | Spec set for SCRUM-9. I read `tasks.md` and the contract in full; the others only in part |

## Design versus code

| Point | Design says | Code does | Match? |
| --- | --- | --- | --- |
| Path | 3d (1277972): `GET /api/v1/me/balances?year=` | `GET /api/me/balances`, no `year` (current year only). The existing `/api/me` also has no `/v1` | Differs (follows the project convention) |
| Response shape | 3d: `{year, items[{leaveType{id,code}, granted, used, held, available}]}` | `{leaveYear, balances[{leaveTypeId, code, name, hasBalance, accrued, used, pending, available, carryForwardDays, carryForwardExpiresOn}]}` | Differs |
| Meaning of `used` | 3d A2: used and held come from the ledger | `used = held - pending`; `pending` comes from `leave.LeaveRequest` | Differs (matches 3c: "3 pending is the sum of pending requests") |
| Roles | 3d: all roles | No role policy; the fallback policy needs sign-in | Yes |
| Caller from token | 3d conventions | `sub` claim only | Yes |
| Errors | 3d: Problem Details with `code` and `traceId` | `Results.Problem` (401). I did not check for `code` | Partly |
| Ledger history | 3d: `GET /me/balances/{id}/entries` | Not built | Not built |
| Dashboard parts | 3a (65835): cards, recent requests list, Apply button; loading, empty and error states | Cards, loading and error only. No recent requests, no Apply button, no skeleton, no empty state | Partly (rest belongs to 4d) |
| Unpaid | 3c: no balance row | `hasBalance: false`, text "No balance, unpaid" | Yes |
| Balance never below zero | 3c: `CHECK Available >= 0` | Read only; the demo uses one atomic `ExecuteUpdate` | Yes |
| Module rule | Structure: no reads across modules | Two ports, one per schema; joined in the use case | Yes |
| Time | Golden rule: `TimeProvider`, `DateOnly` | `GetMyBalances` and the demo use `TimeProvider`; days are `DateOnly` | Yes |

**Design documents used:**
- Jira SCRUM-9 (facts from the prompt)
- Confluence LMA 98443 Phase 1 Requirements, 65835 Phase 3a UI/UX, 1048577 Phase 3c Database, 1277972 Phase 3d API (excerpts from the prompt; 3d is "drafted, awaiting answers")
- `ai/steering/*`; `ai/specs/us02-see-leave-balance/tasks.md` and `contracts/balances-api.md`

**Looked for and not found:** class-design, Angular-design and security-design pages in the LMA space. Not opened: Leave-Policy Rules (229377), NFR (262145), 3b HLD (1277954), 3c Part 2 (1179649), Data-Protection Notes (98460).

## State of the tasks

`us02-see-leave-balance/tasks.md` (23 tasks): 15 ticked, 8 open.
- Done: T002 to T013, T015 to T018
- Open: T001 (baseline check), T014 (browser and API checks 1, 2, 3, 6 to 9), T019 (checks 4 and 5), T020 (full build and tests), T021 (full quickstart), T022 (security and code review), T023 (update INDEX, `/learn`)

`ai/specs/INDEX.md` has no row for US-02.

## Findings and gaps

- **Jira says Done, the evidence does not.** There is no pull request and no commit on the branch. All work is uncommitted. T014, T019 and T020 to T023 are open. Review, `/learn` and `/status` (CLAUDE.md) are not done.
- **Contract differs from 3d** (path, field names, `used` meaning, no `year` parameter). The repo contract `balances-api.md` is what the code follows. 3d is still a draft, so it may be the one to update. Your decision.
- **Tasks versus contract and code:** T009 says "404 when `sub` is missing or not an active employee". The contract and the code return 401 for a bad `sub`, and the code does not check that the employee is active. The contract accepts this (same as `/api/me`, up to 60 minutes). `tasks.md` is out of date.
- **Dashboard is balances only.** No recent requests, Apply button, skeleton or empty state (3a). Likely 4d.
- **Dates on the card** show the raw `yyyy-MM-dd` string, not a formatted date (`dashboard.html`). T018 asked for a formatted date.
- **Leave year uses UTC today.** The office is Asia/Kolkata, so around New Year the year could differ for a few hours. I did not find a stated rule.
- **Demo data** (`DemoBalanceScenarios`): called only from the Development branch in `Program.cs` (`seedDemoData: true`), so Demo never seeds (rule 6). It is idempotent through a note marker per person and year. It needs the opening balance row from `DevSeeder` and skips if missing. It writes entry types Adjustment and Deduction directly, a shortcut until 4d and 4e exist. The ledger sum was designed to equal `Available`, but I did not run the reconciliation.
- **Security checks I read in the code:** id from the token only, `AsNoTracking` on every read, parameterized LINQ, no personal data logged. The screen shows an error, not zeros, on failure. Role policy tests do not exist (no .NET tests).
- **Not verified:** build, tests, API call, browser, performance (under 2 s), the `year` and ledger-history parts of 3d.

## Why it is built this way

| Decision | Chosen | Rejected, and why |
| --- | --- | --- |
| Stored figures | Store only `Available`; derive accrued and used (research R1) | Store all four: they could drift from the ledger |
| Where days are held | At submit (3c) | At approval: two pending requests could overspend |
| Cross-module read | Two ports joined in the use case | One query across `leave` and `balances`: breaks the module rule |
| Queries | Three small queries merged in memory | One large grouped join: harder to read, a handful of rows only |
| Carry-forward | Used first, shown while remaining (research R5, owner confirmed) | Not shown: the employee would not see what lapses on 31 March |
| Demo data | Idempotent top-up, no reset (R6) | Reset the database: destructive |
| Who is asked about | `sub` claim only | An id in the URL: invites IDOR (rule 4) |

## Interview practice

1. **Q:** Why a ledger and a balance row? **A:** The ledger is the history and is never edited, so every change can be explained. The row gives a fast read and a database check `Available >= 0`. Both are written in one transaction, and a reconciliation query checks they agree.
2. **Q:** Why is available already net of pending days? **A:** Days are deducted at submit and restored on reject or cancel. So pending days are already out of `Available`. The code adds them back to `accrued` and splits them out as `pending`.
3. **Q:** How is `used` worked out? **A:** `held` is minus the sum of Deduction and Restore entries (pending plus approved). Used is `held - pending`.
4. **Q:** How do you avoid reading another module's tables? **A:** Each module has its own reader behind an interface in Application. The use case combines the answers.
5. **Q:** Why no employee id in the route? **A:** The caller is the `sub` claim. There is no id to guess, so no IDOR (security rule 4).
6. **Q:** Why does the screen show an error and not zeros? **A:** Zero means "no leave left". An error means "could not load". Mixing them would mislead the employee.

**Teach-back (answer in your own words):**
1. Work through AC2 with the ledger rows, and show how the card gets accrued 10, used 2, pending 3, available 5.
2. Why does `GetMyBalances` call two ports instead of one query across both schemas, and what would break if it did?
3. The API path differs from the 3d design and Jira says Done with no pull request. What would you do before calling this story finished?
