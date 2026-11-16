# Learning note: US-06 Manager inbox (SCRUM-13)

Spec: `ai/specs/us06-manager-inbox/`. Read this with the code open.

## 0. Honest status

- Nothing was run in a browser. The Approvals screen is covered only by Vitest specs.
- There is no .NET test project (it comes in Phase 5). The back end was checked by reading the code and by manual calls to the real API (quickstart scenarios A, B, C, F, read only).
- Not tested at all: approved, rejected and cancelled overlap cases (approve and reject are not built yet), a second manager with their own team, half-day overlap against real data, screen reader use.

## 1. What we built

A manager opens the Approvals screen. They see the pending leave requests of their direct reports. The soonest start date is first. Each card shows the person, leave type, dates, days and the balance before and after. It also shows which other reports are away on the same days and "N of team size away".

It is read only. There are no Approve or Reject buttons. That is the next story (increment 4e).

Pieces:

- One endpoint: `GET /api/approvals?page=&pageSize=`.
- One use case: `GetManagerInbox` (Application).
- One pure rule: `TeamOverlap` (Domain).
- Two readers: `DirectReportReader` (people) and `ApprovalRequestReader` (leave), plus the existing `IBalanceReader`.
- One Angular screen `Approvals` and one typed service `ApprovalsService`.
- No migration, no new package, no write of any kind.

## 2. Key concepts

Terms first:

- **Port**: an interface in Application that says what data the use case needs. Infrastructure implements it.
- **IDOR** (insecure direct object reference): reading a record by guessing its id.
- **Offset paging**: skip `(page - 1) * pageSize` rows, take `pageSize`.
- **Pure function**: same input gives the same output. No clock, no database.

### 2.1 Two layers of authorization

- **What it is**: first a role check (may you call this endpoint), then a resource rule (which records may you see).
- **How it works**: `.RequireAuthorization(Policies.RequireManager)` on the endpoint. Then `GetManagerInbox` lists only people whose `ManagerId` is the caller. There is no person id in the URL, so nothing can be probed.
- **Why**: a role alone says "you are a manager". It does not say "this is your report". Security rules 1, 3 and 4 need both.
- **When**: every endpoint that returns other people's data.

### 2.2 The team is looked up, never taken from the token

- **What it is**: the list of direct reports comes from the database on every call.
- **How it works**: `DirectReportReader` runs `ManagerId == managerId && IsActive && Id != managerId`. The manager id comes from the token's `sub` claim only.
- **Why**: a token lives up to 60 minutes. If a report changes manager, the old token would still say the old thing. Security rule 7 forbids changeable facts in the token.
- **When**: any fact that can change (manager, role scope, active flag). Put only stable identity in the token.

### 2.3 Three ports instead of one big query

- **What it is**: the use case calls three readers, one per module (people, leave, balances).
- **How it works**: `GetManagerInbox` gets the team, then a page of pending requests, then the held slots, then one balance ledger per person and leave year on the page. It joins them in memory.
- **Why**: the project rule says one module does not read another module's tables. The cross-module exception covers writes in one transaction, not reads. A team is small, so a few queries are cheap.
- **When to avoid**: for a large team this costs more round trips. Then add a batch method on `IBalanceReader`.

### 2.4 Team overlap as a pure Domain rule

- **What it is**: `TeamOverlap.For(requestId, employeeId, held)` says who else is away on the same slots.
- **How it works**: day rows (`LeaveRequestDay`) exist only for working days of pending or approved requests. Each row is a `HeldSlot(RequestId, EmployeeId, Date, Slot)`. The request's own slots are the rows with its id. Another person overlaps when they hold the same `(Date, Slot)`. Result: the others in id order with their shared dates, and `AwayIfApproved = 1 + others`.
- **Why slots, not date ranges**: ranges would count weekends and holidays inside both ranges. Dates alone would flag a first half against a second half.
- **Why pure**: easy to unit test later, no mocks.
- **Rule from the owner (FR-006)**: the requester is counted. Two reports out on the same dates show "2 of 3 away" on both.

### 2.5 Balance before and after

- **What it is**: two numbers per item.
- **How it works**: days are deducted at submit, so the ledger's `Available` is already net of this request. That is `After`. `Before = After + DaysCount`. If there is no ledger row (Unpaid, or no balance row for that person and year), `Balance` is `null`, not zero.
- **Why**: approving changes nothing, so "after approval" equals the balance now. Showing both matches the sketch "8.0 to 5.0".
- **Edge**: with several pending requests of one type, every item shows the same `After`, and `Before` is that item's days higher.

### 2.6 Stable paging

- **How it works**: order by `StartDate`, then `SubmittedAtUtc`, then `Id`. Then `Skip` and `Take`, plus a `CountAsync` for `totalCount`.
- **Why a full order**: with ties on start date, a partial order can repeat or skip rows between pages. Adding `Id` makes the order total.
- **Overflow guard**: the endpoint caps `page` at 100000 and `pageSize` at 100. The reader computes the skip in `long` and clamps it to `int.MaxValue`.

### 2.7 The Angular screen

- **State**: signals `loading`, `error`, `items`, `page`, `totalCount`. `totalPages`, `hasPrevious` and `hasNext` are `computed`.
- **Why signals**: the app is zoneless. A change must go through a signal to update the screen.
- **Server decides**: the screen phrases `awayIfApproved` and `teamSize` but never recounts them.
- **Error is not empty**: a failed load shows an error with retry, so "could not load" is never mistaken for "nothing waiting".
- **Cancel the old call**: `load()` unsubscribes the previous request, so a late old response cannot overwrite a newer one.
- **Stale last page**: if the page is empty but `totalCount > 0` and the page is past the end (requests were decided meanwhile), it reloads the last valid page.
- **Words, not colour**: overlap text is a sentence.

## 3. Diagram

```text
Browser (Approvals)                         API                           Database
  |  GET /api/approvals?page=1&pageSize=20   |                               |
  |----------------------------------------->| fallback policy: signed in?   |
  |   Bearer token                           | RequireManager policy         |
  |                                          | sub -> managerId              |
  |                                          | validate page, pageSize       |
  |                                          |                               |
  |                                          | GetManagerInbox               |
  |                                          |  1 reports.GetAsync --------->| people.Employee
  |                                          |    (none -> empty page)       |
  |                                          |  2 GetPendingPageAsync ------>| leave.LeaveRequest
  |                                          |  3 GetHeldSlotsAsync -------->| leave.LeaveRequestDay
  |                                          |  4 GetLedgerAsync per person->| balances.*
  |                                          |  5 TeamOverlap.For per item   |
  |<-----------------------------------------| 200 { items, page, pageSize,  |
  |                                          |       totalCount }            |
```

## 4. Worked example (real values from the manual run, 2026-11-14)

Manager: Meera Rao (id 1). Reports: Asha Nair, Ravi Kumar, Sneha Iyer. Team size 3.

- Pending requests: 5 (`totalCount` 5). Sorted by start date: Sneha 35 (2026-12-01 to 12-02), Sneha 36 (12-04), then two starting 12-07, then Sneha 40 (12-10).
- The two starting 12-07: Asha's request 3 (12-07 to 12-09) and Sneha's request 38 (12-07 to 12-08). Same start date, so the earlier submitted is first: Asha's.
- Overlap for request 3: held slots on 12-07 and 12-08 (AM and PM) match Sneha's. 12-09 is Asha only. So Others = Sneha Iyer with `2026-12-07, 2026-12-08`. `AwayIfApproved` = 1 + 1 = 2, `teamSize` 3.
- Request 38 names Asha Nair with the same two dates, also 2 of 3.
- Requests 35, 36, 40: no others, `awayIfApproved` 1.
- Screen text for request 3: "Also away: Sneha Iyer (2026-12-07, 2026-12-08). 2 of 3 of your team would be away if you approve."
- Access: Asha (Employee) got 403, Hema (HR only) 403, no token 401, `page=0` or `pageSize=500` or `pageSize=abc` got 400.
- Reads change nothing: Asha 3.0 pending and 5.0 available before and after five reads.
- The Jira example (exactly 3 pending) was not matched by this data. The rule holds for any count.

## 5. Review findings

Fixed:

- **Page overflow**: a huge `page` times `pageSize` could overflow `int`. Fixed with a `MaxPage` cap in the endpoint and `long` maths with a clamp in the reader.
- **Null balance when there is no balance row**: the use case would fail or show a wrong number. Now `Balance` is `null` when the ledger row or its `Available` is missing.
- **Stale empty last page**: after requests were decided, the current page could be empty while the total was above zero, so the screen said "nothing waiting". Now it reloads the last valid page.
- **Overlapping loads**: a slow old response could overwrite a newer page. Now `load()` cancels the previous subscription.

Left open:

- **No `TeamOverlap` unit tests until Phase 5.** The rule was checked by reading it and by one real overlap in the data.
- **"Leave type {id}" fallback text.** The ledger lists only active types. A request for a type no longer there shows "Leave type 7" and no code. Plain, but not friendly.
- **Wide held-slots range.** `GetHeldSlotsAsync` reads from the earliest start to the latest end on the page. A page with one request in January and one in December reads the whole year of team rows. Fine for a small team, wasteful for a big one.

## 6. Trade-offs and rejected alternatives

| Choice | Rejected | Why |
| --- | --- | --- |
| Three ports composed in code | One SQL join across schemas, or a DB view | Breaks the module rule. A view hides logic from review. Cost: more round trips. |
| `GET /api/approvals` | `/api/me/approvals`, `/api/manager/inbox` | `me` means own record. A new naming scheme for one screen is noise. |
| 403 for non-managers | 404 | Role policies return 403 in this project. It is the same for every caller, so it leaks nothing. There is no id to probe. |
| Role policy plus team lookup | Roles only | A role does not say whose records you may see. |
| Offset paging | Keyset paging | Teams are small. Keyset suits big, fast-changing lists. |
| Compare `(date, slot)` | Compare date ranges or dates only | Ranges count weekends. Dates flag first half vs second half. |
| Balance from the existing ledger | Recompute it | A second derivation could disagree. |
| One balance read per person and year | Per request, or a new batch method | Fewer calls than per request. A batch method is for bigger teams. |
| No team cache | Cached team list | A changed reporting line must apply at once. Data is tiny. |
| Cards and Previous/Next | Table with drawer, infinite scroll | Quicker to decide from cards. Simpler to test. |

## 7. Interview questions

1. **What is the difference between authentication, role authorization and resource authorization?**
   Authentication proves who you are. Role authorization decides which endpoints you may call. Resource authorization decides which records you may touch. You need all three. Example: `RequireManager` lets Meera call `/api/approvals`, but only Asha, Ravi and Sneha come back, because the team is looked up by `ManagerId`.

2. **Why not put the manager's team, or the manager id of an employee, in the JWT?**
   A token is signed and cannot change until it expires. Facts like reporting lines change. A stale token would grant stale access. Put only stable identity in it and look up the rest. Example: the inbox reads `sub` from the token, then `DirectReportReader` queries `people.Employee` on every call.

3. **How do you prevent IDOR here?**
   Never trust an id from the client. Here the endpoint takes no person id at all. The list is built from the caller's own team, so there is nothing to guess. Where an id exists, return 404 for records the caller may not see. Example: `GET /api/approvals` accepts only `page` and `pageSize`.

4. **Why return 403 to a non-manager and not 404?**
   A role policy says "you may not call this". It is the same answer for every caller, so it reveals nothing about data. 404 is for a specific record the caller may not see. Example: Asha and Hema both got 403, and the body says nothing about pending requests.

5. **How do you page a list safely?**
   Use a total order so pages repeat. Cap the page size and the page number. Return the total count. Guard against integer overflow in the skip. Example: order by `StartDate`, `SubmittedAtUtc`, `Id`. `pageSize` 1 to 100, `page` up to 100000, and the skip is computed in `long`.

6. **Why is the overlap rule in Domain and not in the endpoint or the SQL?**
   Business rules belong in Domain or Application. Pure code needs no database or mocks, so it is easy to test, and the Domain depends on nothing. Example: `TeamOverlap.For` takes held slots and returns others plus `AwayIfApproved`. Phase 5 can test it with plain objects.

7. **How do you compose data from several modules without breaking module boundaries?**
   Define one port per module in Application. The use case calls each and joins in memory. Each reader touches only its own schema. Cost: more queries. Example: `IDirectReportReader` (people), `IApprovalRequestReader` (leave), `IBalanceReader` (balances).

8. **What is an N+1 query problem, and how did you limit it?**
   N+1 means one query for a list, then one more per item. Here balances are read once per person and leave year on the page, and held slots are read once for the whole page's date window. Example: 5 items from 3 people cause 3 ledger reads, not 5, and one held-slots read, not 5. A batch method is the next step for big teams.

9. **How do you handle a response that arrives late in an Angular screen?**
   Keep the subscription and unsubscribe before a new call, so an old response cannot overwrite a newer one. Also unsubscribe on destroy. Example: `Approvals.load()` calls `this.current?.unsubscribe()` first, and `DestroyRef.onDestroy` does the same.

10. **Why signals and `computed` in this component, and what happens if you mutate a plain field in a zoneless app?**
    Signals tell Angular exactly what changed. In a zoneless app nothing else triggers a refresh, so a plain field change may not show on screen. `computed` derives values and stays in sync. Example: `totalPages`, `hasPrevious` and `hasNext` are `computed` from `totalCount` and `page`.

## 8. Teach-back

Answer in your own words. Do not copy from this note.

1. Why does the inbox look up the manager's team in the database on every call instead of reading it from the token? Give one thing that could go wrong otherwise.
2. Explain how two reports on the same dates become "2 of 3 away", and why a first half and a second half on the same date do not overlap.
3. A manager is on page 3, then another window approves requests so page 3 no longer exists. What does the screen do, and why is that better than showing "Nothing is waiting"?

### My answers

(empty)

## 9. Exercise (about 20 to 30 minutes, on paper)

Design `TeamOverlap` test cases for Phase 5.

1. Write a table with columns: case name, held slots (RequestId, EmployeeId, Date, Slot), the request under test, expected `Others`, expected `AwayIfApproved`.
2. Cover at least: no overlap, partial overlap, first half against second half, full day against first half, one person with two requests on one date, two others on different dates, a request with no own rows.
3. For each case, say which line of `TeamOverlap.For` it exercises.
4. Pick one case and say what the code returns today. If you find one the code gets wrong, write it down.

Do not run anything. This is a thinking exercise.
