# US-06: Review pending requests in a manager inbox

**Jira:** SCRUM-13 · Story · To Do · High · epic SCRUM-7 (Leave Management App: MVP) · SCRUM Sprint 5 (active, started 2026-11-14)
**Code:** PR #8 (open, found with `git ls-remote`; no `gh` CLI) · branch `SCRUM-13-US-06-Review-pending-requests-in-a-manager-inbox` · head `c0016ab` (1 commit "LMS US-06 Review pending") · base `827e3b4` (main, merge of PR #7) · 20 files (+1077, -0)
**Written:** 2026-11-14. I read the code and the design documents. I did not build or run anything. Where I write "the owner's session ran", that result came from the owner's session, not from me.

## What the story is

A manager opens an inbox and sees the pending leave requests of their direct reports. Each item shows the dates, the day count, the balance after approval and who else in the team is away on the same days. The list is sorted by start date, soonest first.

This story is **read only**. The manager cannot approve or reject here. That comes in increment 4e.

**Why it matters:** a manager can decide quickly and knows if approving would leave the team short (requirement BR-05, Must).

**Important at this head:** only part of the story is built. The branch holds the spec folder and the **back end only**. There is no screen. The team-overlap part (criterion 3) is a placeholder.

## What the code delivers

```
Approvals screen (NOT BUILT)
  |
  +-- GET /api/approvals?page=&pageSize=   policy RequireManager
        |   ApprovalEndpoints (reads sub, checks page and pageSize)
        v
      GetManagerInbox (Application)
        |-- IDirectReportReader  -> people.Employee   (the team)
        |-- IApprovalRequestReader -> leave.LeaveRequest (pending page, sorted)
        |-- IBalanceReader (existing) -> balances.*   (before and after)
        |-- TeamOverlap (Domain)  ... written, NOT CALLED yet
        v
      200 { items, page, pageSize, totalCount }
```

1. The endpoint `GET /api/approvals` needs the Manager role (policy `RequireManager`). A user without it gets 403. No token gets 401.
2. The manager id is the token's `sub` claim. The client sends no person id, so nothing can be probed or over-posted.
3. `page` and `pageSize` are read as text and checked: `page` 1 or more, `pageSize` 1 to 100 (default 20). A bad value gives a 400 `ProblemDetails`.
4. `DirectReportReader` reads the team from the database on every call: active people whose `ManagerId` is the caller. The manager is never in their own list.
5. `ApprovalRequestReader` returns one page of Pending requests of those people. The order is start date, then submitted time, then id. A full order makes each page repeatable.
6. For balances, the use case calls the existing `IBalanceReader.GetLedgerAsync` once per person and leave year on the page. Days are deducted at submit, so `Available` is already "after". "Before" is after plus the request's days. Unpaid has no balance, so `balance` is null.
7. Three ports, one per module, keep modules apart: the code does not join `people`, `leave` and `balances` in one query (research R1).
8. `TeamOverlap.For` (Domain) is a pure rule: two people overlap only on the same date and the same half-day slot. It exists, but `GetManagerInbox` does not call it. Each item gets `TeamSize` = number of reports, `AwayIfApproved` = 1, `Others` = empty.

**Example:** Meera Rao (id 1, Manager) calls `GET /api/approvals`. Her reports are Asha Nair (4), Ravi Kumar (5) and Sneha Iyer (6). She gets their pending requests, earliest start first, each with `balance.before` and `balance.after`. Every item says "1 away, none named", even if Asha and Sneha are off on the same day. Asha calls the same URL and gets 403, because she is not a Manager. (Expected behaviour from the code. The endpoint was never called.)

## Acceptance criteria: where each one is delivered

| Criterion (from Jira) | Delivered by | State |
| --- | --- | --- |
| 1. Opening the inbox with 3 pending requests shows 3 items with dates, count, balance after approval and team overlap | Server: `GetManagerInbox.cs`, `ApprovalEndpoints.cs`. Dates, `DaysCount` and balance are in `ApprovalItem`. No screen. Overlap is a placeholder. | Partly (data on the server only, never run) |
| 2. Sorted by start date, soonest first | `ApprovalRequestReader.GetPendingPageAsync` (`OrderBy StartDate`, then `SubmittedAtUtc`, then `Id`) | Done on the server, never run |
| 3. With 2 of 5 reports out for the same dates, each request shows who else is out and how many would be away | `TeamOverlap.cs` has the rule. `GetManagerInbox.cs` returns `AwayIfApproved = 1` and `Others = []` | Not delivered |

The inbox screen (the thing a manager actually opens) is not found in the diff. No file under `web/` changed.

## Code changes

**Back end (`src/`)**

| File | What it does |
| --- | --- |
| `Domain/TeamOverlap.cs` | Pure overlap rule: same date and same slot. Gives other people, their shared dates, and `AwayIfApproved` (requester counts). Not called yet. |
| `Application/Approvals/ApprovalDtos.cs` | Response shapes: `ManagerInboxResponse`, `ApprovalItem`, `BalanceBeforeAfter`, `TeamOverlapInfo`. No reason, no email. |
| `Application/Approvals/IDirectReportReader.cs` | People port: the active direct reports of a manager. |
| `Application/Approvals/IApprovalRequestReader.cs` | Leave port: pending page with total, and held half-day slots of a team. |
| `Application/Approvals/GetManagerInbox.cs` | The use case. Joins the three ports. Read only, writes no audit row. Overlap is the interim value. |
| `Application/DependencyInjection.cs` | Registers the use case (+1 line). |
| `Infrastructure/Persistence/DirectReportReader.cs` | Reads `people.Employee` only. |
| `Infrastructure/Persistence/ApprovalRequestReader.cs` | Reads `leave.*` only. Pending page, count, and held slots. |
| `Infrastructure/DependencyInjection.cs` | Registers the two readers (+3 lines). |
| `Api/Approvals/ApprovalEndpoints.cs` | `GET /api/approvals`, policy `RequireManager`, page checks, 401 and 400 as `ProblemDetails`. |
| `Api/Program.cs` | Maps the endpoint (+2 lines). |

**Docs and tooling**

| File | What it is |
| --- | --- |
| `ai/specs/us06-manager-inbox/` (spec, plan, research, data-model, quickstart, tasks, `contracts/manager-inbox-api.md`, `checklists/requirements.md`) | The approved spec. Read in part: I read `research.md` and `tasks.md` fully, the others not in detail. |
| `ai/specs/INDEX.md` | One line added for this spec. |

There is no front-end change, no database change (no migration, no new package) and no test file.

## Design versus code

| Point | Design says | Code does | Match? |
| --- | --- | --- | --- |
| Path | 3d: `GET /approvals?status=Pending&page=` | `GET /api/approvals?page=&pageSize=`. No `/v1`, no `status` filter: this story lists only pending (research R2) | Differs, on purpose |
| Role | 3d: role M only | Policy `RequireManager` | Yes |
| Wrong role | 3d: 403 when a role never allows the endpoint | Role policy gives 403 | Yes |
| Paging | 3d: `page`, `pageSize` max 100, `{ items, page, pageSize, totalCount }` (ADR 24) | Same shape, default 20, max 100, 400 on bad value | Yes |
| Hidden rows | 3d: not visible means 404 (ADR 26) | No id in the URL, so nothing to hide. Only the caller's reports are listed | Yes (not applicable) |
| Error codes | 3d: rule failure codes in snake_case | No business rule codes here. Only 400, 401, 403 as `ProblemDetails` | Not applicable |
| Names | 3d A7: managers see names | Names of the manager's own reports only | Yes |
| Overlap in the item | 3d detail has `teamAway` (names per date) and `allowedActions` | Item has its own `teamOverlap` (size, away count, names with shared dates). No `allowedActions` | Differs |
| Overlap count | 3a S-06 sketch "1 of 5 out" counts others only | Spec: count includes the requester ("2 of 5"). Rule is written, not wired | Differs, on purpose. Not built at this head |
| Inbox screen S-05 | 3a: list, overlap text, "Approvals (3 pending)", a Review button per item | No screen. Spec says no buttons and no pending count in the heading | Not built. Differs on purpose |
| Empty, loading, error states | 3a: loading, empty, error | Server returns an empty page. Screen states not built | Not built |

**Design documents used:**
- Confluence LMA "Phase 1: Requirements Overview" (id 98443)
- Confluence LMA "Leave-Policy Rules (accepted defaults)" (id 229377)
- Confluence LMA "Phase 3a: UI/UX Design" (id 65835)
- Confluence LMA "Phase 3d: API Design" (id 1277972)
- Repo: `ai/specs/us06-manager-inbox/` (`research.md` and `tasks.md` read fully), `ai/steering/security.md`

**Looked for and not found:** Phase 3e class design and Phase 3f Angular design (not in the LMA space). The Confluence pages above were given to me as excerpts, not read by me.

## State of the tasks

From `tasks.md` at the head:

- Done (ticked): T001 baseline, T002 `TeamOverlap`, T003 DTOs and ports, T004 two readers, T005 use case, T006 endpoint.
- Open: T007 web service, T008 web screen, T009 run scenarios A, B, F, T010 wire the overlap, T011 overlap text on the screen, T012 run scenario C, T013 prove the resource rule, T014 `/ci-check`, T015 `/review`, `/learn`, `/status`.

## Findings and gaps

- **Jira shows To Do** while PR #8 is open with part of the work. Jira and code agree in spirit (the story is not done), but the board does not show that work has started.
- **Criterion 3 is not delivered.** `GetManagerInbox` returns an interim value. A comment in the code says it is replaced in T010. A reader of the API alone would think nobody is ever away together.
- **No screen.** A manager cannot use the inbox yet. Criterion 1 is met only as JSON from an endpoint nobody has called.
- **Jira says 5 reports** in criterion 3. The seeded manager (Meera Rao) has 3 (Asha, Ravi, Sneha). Hema Shetty (HR) and Arjun Das (Admin) have no manager. The Jira example cannot be reproduced with the seed alone.
- **Count rule:** the sketch (1 of 5) and the owner's decision (2 of 5, requester counts) differ. The spec follows the owner. The 3a page was not updated.
- **Cosmetic:** the PR title and commit message "LMS US-06 Review pending" are cut short.
- **What the owner's session ran:** at the start the web checks passed (Prettier clean, 114 of 114 Vitest tests, web build). After T002 to T006 the back-end `dotnet build` gave 0 warnings and 0 errors, and both `dotnet format` checks (style, analyzers) were clean.
- **Not run by anyone:** the API itself, any call to `/api/approvals`, any web test for this screen (none exists), any .NET test (none exist, Phase 5), `/ci-check` after the change, `/review`, `/learn`.
- **Not verified by me:** everything. I read the code, I did not build or run it. I read the four code files and `TeamOverlap.cs` fully. I did not read `IDirectReportReader.cs`, `IApprovalRequestReader.cs` or the two `DependencyInjection.cs` changes in full. I only saw their diff size.
- **Overlap edge not checked:** `TeamOverlap.For` is not covered by any test. Cases in T002 (first half against second half, person with two requests) were read by the owner's session, not run.

## Why it is built this way

| Decision | Chosen | Rejected, and why |
| --- | --- | --- |
| Reading three modules | Three read ports composed in one use case (research R1) | One join across `people`, `leave` and `balances`: fewer queries, but it breaks the module rule. A database view: hides logic from review |
| Route | `GET /api/approvals` (R2) | `/api/me/approvals`: "me" means own record. `/api/manager/inbox`: a new naming scheme for one screen |
| Who is the team | Active people whose `ManagerId` is the caller, read on every call (R3) | Token claim or cached list: a changed reporting line must apply at once. Whole management chain: not asked for |
| Balance figures | One ledger call per person and year; after = `Available`, before = after plus days (R4, R5) | A new batch method on `IBalanceReader`: better for big teams, add later. Recomputing from the ledger: could disagree |
| Overlap rule | Same date and same slot, from day rows (R6) | Compare date ranges: counts weekends and holidays. Compare dates only: flags first half against second half |
| Paging | Offset paging with a full sort order (R7) | Keyset paging: not needed for a team |
| Non-manager | 403 from the role policy (R8) | 404: hides the endpoint, but 403 is the project convention |
| Tests | Keep `TeamOverlap` pure for Phase 5, check by hand now (R11) | A test project now: that is Phase 5 |

## Interview practice

1. **Q:** Why three ports instead of one SQL join? **A:** Each module owns its tables, and code in one module does not read another's. A join would be faster but couples the modules. The use case in Application composes the three ports. A team is small, so extra calls cost little.
2. **Q:** How do you stop a manager seeing someone else's team? **A:** Two layers. The role policy decides who may call. Inside, the team is read from the database using the token's `sub`, and the pending query is filtered on those ids. The client sends no person id.
3. **Q:** Why read `page` and `pageSize` as text? **A:** So a bad value such as `abc` is always a clean 400 `ProblemDetails`, in every environment, instead of a binding error.
4. **Q:** What does "balance after approval" mean when days are deducted at submit? **A:** Approving changes nothing, so "after" equals the available balance now. "Before" is after plus the request's days.
5. **Q:** Why compare slots and not date ranges for overlap? **A:** Ranges would count weekends and holidays. Dates alone would flag a first half against a second half. Day rows exist only for working days and carry the slot.
6. **Q:** Is this story done? **A:** No. The list endpoint with balances and sorting exists. The screen and the overlap wiring are open (T007, T008, T010, T011), and nothing was run against the API.

**Teach-back (answer in your own words):**
1. Why does `GET /api/approvals` return 403 for Asha and not 404? How is that different from a request id that belongs to someone else?
2. `TeamOverlap` is written but `GetManagerInbox` does not call it. What does the endpoint return for the team overlap today, and which task fixes it?
3. A manager has two pending Annual requests. Why do both items show the same `balance.after`, and how does `balance.before` differ?
