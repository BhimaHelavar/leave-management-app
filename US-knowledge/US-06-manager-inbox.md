# US-06: Review pending requests in a manager inbox

 **Jira:** SCRUM-13 · Story · Done (resolved 2026-11-14) · High · epic SCRUM-7 · SCRUM Sprint 5
**Code:** PR #8 and PR #9 (both merged) · branch `SCRUM-13-US-06-inbox-screen` · head `2b06a4f` (main is at merge `b8e2c64`) · 60 files (+4620, -17) from `827e3b4` to `2b06a4f`
**Written:** 2026-11-16. I read the code and the repo design documents. I did not build or run anything. The web tests were not run by me.

## What the story is

A manager opens one screen and sees every pending leave request from their direct reports. Each request shows who asked, the leave type, the dates, the number of days and the balance before and after. It also shows who else in the team is away on the same days. The list is sorted by start date, soonest first.

The manager can only look in this story. Approve and reject belong to US-07.

**Why it matters:** The manager can decide from one screen and still protect team cover (requirement BR-05, Must).

## What the code delivers

```
Angular Approvals screen (route approvals, Manager only)
   -> ApprovalsService.getApprovals(page, 20)
   -> GET /api/approvals?page=&pageSize=      (policy RequireManager, sub = manager id)
        -> GetManagerInbox
             1 IDirectReportReader      people.Employee   -> the team
             2 IApprovalRequestReader   leave.LeaveRequest -> one pending page (sorted)
             3 ApprovalItemBuilder
                 IBalanceReader.GetLedgerAsync  balances.* -> before / after
                 IApprovalRequestReader.GetHeldSlotsAsync   leave.LeaveRequestDay
                 TeamOverlap.For (pure Domain rule)         -> who else is away
   <- ManagerInboxResponse { items, page, pageSize, totalCount }
```

1. The route `approvals` is guarded by the sign-in guard and the role guard. The screen is a lazy-loaded standalone component.
2. The endpoint reads the manager id from the token `sub` claim only. It accepts `page` (1 to 100000) and `pageSize` (1 to 100, default 20). No person id comes from the client.
3. `GetManagerInbox` reads the team from the database on every call. The token never carries the team (security rule 7). No reports means an empty page.
4. The leave reader returns pending requests of those people, ordered by start date, then submitted time, then id. The order is full, so every page is repeatable.
5. `ApprovalItemBuilder` adds the numbers. Days are deducted at submit, so the ledger's `Available` is already the "after" figure. "Before" is "after" plus this request's days. No balance row (Unpaid) means no balance, not zero.
6. For overlap, the builder reads the team's held half-day slots once for the page's date window. `TeamOverlap.For` finds people holding the same date and the same slot. `awayIfApproved` is 1 plus the number of others, so the requester counts.
7. The screen only shows the figures. It builds the overlap sentence but never recounts.

**Example:** Meera Rao (Manager, id 1) has three reports. Sneha asked for Annual on 2026-12-01 to 12-02. Asha asked for 2026-12-02 only. Meera opens Approvals. Sneha's card says "Also away: Asha Nair (2026-12-02). 2 of 3 of your team would be away if you approve." Asha, an Employee, gets 403 from the same endpoint.

## Acceptance criteria: where each one is delivered

| Criterion (from Jira) | Delivered by | State |
| --- | --- | --- |
| 1. 3 pending requests show 3 items with dates, count, balance after, team overlap | `GetManagerInbox.cs`, `ApprovalItemBuilder.cs`, `approvals.html` (dates, days, before/after, overlap sentence) | Done (read in code; not run in a browser) |
| 2. Sorted by start date, soonest first | `ApprovalRequestReader.GetPendingPageAsync` (`StartDate`, `SubmittedAtUtc`, `Id`) | Done |
| 3. Who else is out and how many of the team would be away | `TeamOverlap.cs`, `ApprovalItemBuilder.cs`, `overlapText()` in `approvals.ts` | Partly: pending requests only. Approved requests count in the code, but approving is US-07 and was not exercised |

## Code changes

**Back end (`src/`)** (US-06 files; US-07 files in the same PR are listed under Findings)

| File | What it does |
| --- | --- |
| `Api/Approvals/ApprovalEndpoints.cs` | `GET /api/approvals`: policy, `sub`, query checks, 400 and 401 as ProblemDetails. The same file also has US-07 endpoints |
| `Api/Program.cs` | Calls `MapApprovalEndpoints` |
| `Application/Approvals/GetManagerInbox.cs` | The use case: team, page, items |
| `Application/Approvals/ApprovalItemBuilder.cs` | Balance before and after and overlap. Shared with the US-07 detail use case |
| `Application/Approvals/ApprovalDtos.cs` | Response records. No reason and no email |
| `Application/Approvals/IDirectReportReader.cs` | Port to the people module |
| `Application/Approvals/IApprovalRequestReader.cs` | Port to the leave module: pending page, held slots (also `GetByIdAsync` for US-07) |
| `Domain/TeamOverlap.cs` | Pure overlap rule: same date and same slot. No clock, no database |
| `Infrastructure/Persistence/DirectReportReader.cs` | Active people whose `ManagerId` is the caller, never the caller |
| `Infrastructure/Persistence/ApprovalRequestReader.cs` | `AsNoTracking` queries on `leave.*`, offset paging, `Math.Min` against overflow |
| `Application` and `Infrastructure` `DependencyInjection.cs` | Register the readers and use cases |

**Front end (`web/src/app/`)**

| File | What it does |
| --- | --- |
| `features/approvals/approvals.service.ts` | Typed `getApprovals(page, pageSize)`. The component never calls `HttpClient` |
| `features/approvals/approvals.ts` and `.html` and `.scss` | Signals for loading, error, items and page. Cancels an older call. Reloads the last valid page if the page vanished |
| `app.routes.ts` | `approvals` opens the real screen, not the placeholder |

**Tests**

| File | What it checks |
| --- | --- |
| `features/approvals/approvals.spec.ts` | Loading, list fields, no balance line, empty, error with retry, three overlap texts, paging, stale loads, no Approve button |
| `features/approvals/approvals.service.spec.ts` | URL and params, errors passed on (US-07 cases are in the same file) |

There is no .NET test project (planned for Phase 5), so the overlap rule and the readers have no automated test.

**Docs and tooling**

| File | What it is |
| --- | --- |
| `ai/specs/us06-manager-inbox/` | spec, plan, research, data-model, contracts, quickstart, tasks, checklist |
| `ai/learning/us06-manager-inbox.md` | Earlier learning note (I wrote my own explanation here) |
| `ai/specs/INDEX.md` | Status row for US-06 |

## Design versus code

| Point | Design says | Code does | Match? |
| --- | --- | --- | --- |
| Path and policy | `GET /api/approvals`, `RequireManager` (contract, research R2) | Same | Yes |
| Roles | Manager sees direct reports only (product.md) | Team read from `people.Employee` per call | Yes |
| Sort | Start date, then submitted, then id (contract) | Same in `ApprovalRequestReader` | Yes |
| Errors | 400 bad paging, 401, 403, no 404 (contract) | 400 and 401 in the endpoint, 403 from the policy | Yes (read, not run) |
| Balance | `before = after + daysCount`, null for no balance | Same in `ApprovalItemBuilder` | Yes |
| Overlap | Same date and same slot, requester counts | `TeamOverlap.For` | Yes |
| Overlap sources | Pending and approved requests | Reads day rows. They exist only for pending and approved | Yes, approved untested |
| No Approve button | Contract: none in this story | None in `approvals.html` | Yes |
| Link to the request page | Not in the US-06 contract | The US-07 route `approvals/:requestId` exists, but `approvals.html` has no link to it | Not built |
| Confluence 3a screen S-05 | Inbox with a drawer in the sketch | Cards instead of a table and drawer (research R10) | Differs on purpose |

**Design documents used:** repo `ai/specs/us06-manager-inbox/` (spec, research, contracts, tasks), `ai/steering/*`, `ai/learning/us06-manager-inbox.md`. The spec quotes the Confluence pages. Confluence pages I did not read in full: Requirements Overview (98443, BR-05), Leave-Policy Rules (229377), 3a UI/UX (65835), 3b HLD (1277954), 3c Database (1048577), 3d API (1277972), Non-Functional (262145). `ai/design/` and `ai/decisions/` exist but hold nothing specific to this story (decisions has only a README). I did not read the design files in detail.
**Looked for and not found:** class design (3e), Angular design (3f), security design (3g) as separate Confluence pages.

## State of the tasks

- Done: all 15 (T001 to T015) are ticked in `tasks.md`. This includes the overlap (T010, T011).
- Open: none in the file. Per INDEX, the owner teach-back and the exercise are still to do.

## Findings and gaps

- **Jira says Done, INDEX says Review.** `ai/specs/INDEX.md` lists US-06 as "Review" with the owner's teach-back not done. Jira was resolved 2026-11-14. The statuses disagree. I do not decide which is right.
- **PR #9 carries two stories.** It adds the US-06 screen and the first US-07 back end (`DecideLeaveRequest`, `GetApprovalDetail`, `ILeaveDecisionStore`, `LeaveDecision`, `LeaveDecisionStore`, `approval-detail.*`, route and guard changes, `us07` spec). This makes review and history harder.
- **Criterion 3 is wired.** The task file once says the overlap was an interim value (`others` empty). At `2b06a4f` the builder calls `TeamOverlap.For`, so that note is out of date.
- **Approved requests as overlap are not tested.** Approve and reject were not buildable when US-06 was checked. The code counts any day row, which exists for pending and approved only. This is reasoned from code and comments, not seen running.
- **Inbox cards do not link to the detail page.** I found no `routerLink` in `approvals.html`. A manager cannot reach the US-07 page from the inbox yet (this may be a US-07 task).
- **Open Low review findings** (from INDEX): no `TeamOverlap` unit tests, the fallback text "Leave type {id}", a wide held-slots date range.
- **Not verified:** I did not build, run the API, open a browser or run any test. NOT RUN per INDEX: browser check, second-manager cases (another manager's reports never show), approved, rejected or cancelled overlap. The Jira example (3 pending, 2 of 5 overlapping) is matched only if the data has it.
- The Jira Development field shows 1 of 2 builds successful and 1 unknown. I did not check which.

## Why it is built this way

| Decision | Chosen | Rejected, and why |
| --- | --- | --- |
| Joining three modules | Three read ports composed in one use case | One SQL join or a view: breaks the module rule (research R1) |
| Route name | `/api/approvals` | `/api/me/approvals` (me means own record), `/api/manager/inbox` (new naming) (R2) |
| Team | Read from the database every call | Team in the token or a cache: a changed reporting line must apply at once (R3, rule 7) |
| Overlap | Same date and same slot, pure Domain rule | Compare date ranges (counts weekends) or dates only (flags first half vs second half) (R6) |
| Balance reads | One ledger read per person and leave year | A new batch method: later, if teams grow (R4) |
| Paging | Offset paging, full sort key | Keyset paging: not needed for a team (R7) |
| Non-manager | 403 from the role policy | 404: role refusals use 403 by convention (R8) |
| Screen | List of cards | Table with drawer: heavier than needed (R10) |
| Tests | Pure rule now, .NET tests in Phase 5 | A test project now: outside the story (R11) |

## Interview practice

1. **Q:** Why three ports and not one SQL join? **A:** Each module owns its tables and code does not read another module's tables. A use case in Application composes three small ports. The cost is more round trips, which is fine for a small team.
2. **Q:** How do you stop a manager from seeing another manager's requests? **A:** Two layers. The `RequireManager` policy decides who can call. The use case then reads the caller's reports from the database using the `sub` claim, and filters the pending query on those ids. The client cannot send a person id.
3. **Q:** Why is the team not in the JWT? **A:** A token lives up to 60 minutes. A reporting line can change sooner. Looking it up on each call is correct and cheap.
4. **Q:** How is team overlap computed? **A:** The pure `TeamOverlap.For` takes the team's held half-day slots. A person overlaps only with the same date and the same slot. Day rows exist for working days only, so weekends and holidays never overlap.
5. **Q:** Why is "before" computed and not stored? **A:** Days are deducted at submit. The ledger's `Available` is already after this request. Before is after plus `DaysCount`, so one source of truth is used.
6. **Q:** What problems can offset paging have here, and how were they handled? **A:** A full sort key keeps pages repeatable. A page can vanish when requests are decided meanwhile, so the screen reloads the last valid page. The server caps `page` to avoid integer overflow.
7. **Q:** Why does the component cancel the previous HTTP call? **A:** A slow older response must not overwrite a newer one. It unsubscribes before each load.
8. **Q:** Why no unit tests for the overlap rule yet? **A:** No .NET test project exists until Phase 5. The rule is pure, so it is easy to test then. This is a stated gap, not a hidden one.

**Teach-back (answer in your own words):**
1. Walk through what happens from opening the Approvals screen to a card showing "2 of 3 of your team would be away". Name the endpoint, the three ports and where the number is computed.
2. A report's request is approved and another report asks for the same day. Does the overlap count the approved one? Say why, and say what has not been tested.
3. Why does the server return 403 for an Employee and an empty list for a manager with no reports? What would be wrong with 404?

**Exercise (paper):** Draw a team of 4 with these held slots on 2026-12-01 and 02: Sneha full day on both days, Asha first half on 12-01, Ravi second half on 12-01, Dev none. Work out `others` and `awayIfApproved` for Sneha's and for Asha's request. Then run `dotnet build LeaveManagement.slnx` and read `TeamOverlap.cs` to check your result.
