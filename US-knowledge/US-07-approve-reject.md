# US-07: Approve or reject a request with a comment

**Jira:** SCRUM-14 · Story · In Progress · High · epic SCRUM-7 · SCRUM Sprint 5 (2026-11-14 to 2026-11-16)
**Code:** no PR of its own (merged inside PR #9, story US-06's branch) · branch `SCRUM-13-US-06-inbox-screen` · head `2b06a4f`, main `b8e2c64` · 45 files (+3325, -83, counted from `0acdbf3` to `origin/main`, so a few lines may belong to US-06 follow-ups)
**Written:** 2026-11-16. I read the code and the design documents. I did not build or run anything. The main agent reports a clean back-end build, `dotnet format style`, 174 web tests, web format check and web build on this code. Nothing was run against the real API or database.

## What the story is

A manager opens a pending leave request from one of their direct reports. They approve it or reject it. A rejection must have a comment, so the employee knows why. The manager can also reach the request from a link in an email, after signing in.

**Why it matters:** this closes the leave loop. The employee gets an answer, and a rejected request gives the days back.

## What the code delivers

```
Manager browser                 API (Manager role)               SQL Server (one transaction)
approvals/:requestId  --GET-->  /api/approvals/{id}   -> 404 if missing or not a direct report
 [Approve] [Reject]   --POST->  /api/approvals/{id}/approve | /reject
                                 DecideLeaveRequest: comment rule, team check, pending check
                                   -> LeaveDecisionStore.DecideAsync
                                        1 UPDATE leave.LeaveRequest ... WHERE Status = Pending
                                        2 (reject) DELETE leave.LeaveRequestDay rows
                                        3 (reject, has balance) balances.LeaveBalance Available + days
                                          + LeaveBalanceEntry (Restore)
                                        4 audit.AuditEvent row   5 notify.OutboxMessage 'leave.decided'
                                        6 SaveChanges + Commit   (0 rows in step 1 = rollback, 409)
```

1. **The rule on the comment** is in `Domain/LeaveDecision.cs`. A comment is trimmed. Too long (over 500) is checked first. Then a rejection with no comment is refused. An approval comment is optional.
2. **The use case** `DecideLeaveRequest` runs the checks in order: comment, then "is this a request of my direct report" (the team is read from the database, never from the token), then "is it pending".
3. **The store** `LeaveDecisionStore` does all the writes in one transaction. This is the "named cross-module store" the project rules allow, because the status, the restored days, the audit row and the outbox row must all succeed or all fail.
4. **The guarded update** is `UPDATE ... WHERE Id = @id AND Status = 'Pending'`. If two decisions race, only one changes a row. The other gets zero rows, rolls back and the API answers 409.
5. **Reject** deletes the day rows (so the dates are free again) and adds the saved `DaysCount` back to `Available` with one atomic update. It also writes a `Restore` ledger entry. Unpaid has no balance, so it skips this.
6. **Approve** changes only the request. The days were already taken when the employee applied (US-03). They move from "pending" to "used" because the dashboard derives that from request status.
7. **Audit and outbox rows hold ids and numbers only.** The comment is stored once, on the request (`DecisionComment`). This follows security rule 12 (no free text in audit or outbox).
8. **The deep link.** `authGuard` sends a signed-out person to `/login?returnUrl=/approvals/42`. `Login` calls `safeReturnUrl` and goes there after sign-in. The page shows Approve and Reject only when the server says `allowedActions` has them.

**Example:** Priya (Employee) applied for 3 days of Annual leave, request 42. Her manager Ravi opens `/approvals/42` from a link while signed out. He signs in and lands on the page. He taps Reject with no comment: the API answers 422 `comment.required` and nothing changes. He writes "Release freeze, please move to next week" and rejects. The request becomes Rejected, Priya's Available goes up by 3, her dates are free, an audit row and an outbox row `leave.decided` exist. If Ravi's colleague Meera (a manager of someone else) calls `POST /api/approvals/42/approve`, she gets 404, the same as for a request that does not exist.

## Acceptance criteria: where each one is delivered

| Criterion (from Jira) | Delivered by | State |
| --- | --- | --- |
| 1. Reject without a comment is blocked | `LeaveDecision.CheckComment`, `DecideLeaveRequest` (422 `comment.required`), `approval-detail.ts` | Done in code; not run against the API (T009 open) |
| 2a. Approve: status becomes Approved | `LeaveDecisionStore` step 1 | Done in code; not run (T011 open) |
| 2b. Approve: balance is deducted | Days are deducted at submit (US-03), not at approve. Approve changes nothing in `Available`. See Findings | Partly (by design, differs from Jira wording) |
| 2c. Employee is emailed | Only an outbox row `leave.decided` is written. No sending | Not done (email is increment 4g, spec FR-012) |
| 3. Deep link lands on the request with Approve and Reject, after sign-in | `auth.guard.ts`, `return-url.ts`, `login.ts`, route `approvals/:requestId`, `approval-detail.*` | Done in code; browser check not done (T019 open). No email carries the link yet |

## Code changes

**Back end (`src/`)**

| File | What it does |
| --- | --- |
| `Domain/LeaveDecision.cs` | Pure comment rule: trim, max 500, reject needs a comment |
| `Application/Approvals/DecideLeaveRequest.cs` | Use case and result type; the endpoint only maps results to status codes |
| `Application/Approvals/GetApprovalDetail.cs` | One request of a direct report; null for missing or not a report |
| `Application/Approvals/ApprovalItemBuilder.cs` | Item building moved out of the inbox so inbox and detail show the same numbers |
| `Application/Approvals/ILeaveDecisionStore.cs`, `IApprovalRequestReader.cs`, `ApprovalDtos.cs` | Ports and shapes (detail, decision block, `allowedActions`) |
| `Application/Approvals/GetManagerInbox.cs` | Now uses the shared builder (-58 lines) |
| `Application/Leave/LeaveRuleError.cs`, `DependencyInjection.cs` | Error codes `comment.required`, `request.not_pending`; DI registration |
| `Infrastructure/Persistence/LeaveDecisionStore.cs` | The one-transaction decision (diagram above) |
| `Infrastructure/Persistence/ApprovalRequestReader.cs`, `DependencyInjection.cs` | `GetByIdAsync` and registration |
| `Api/Approvals/ApprovalEndpoints.cs` | `GET /api/approvals/{id}`, `POST /{id}/reject`, `POST /{id}/approve`; hand-read body with 4 KB cap |

**Front end (`web/src/app/`)**

| File | What it does |
| --- | --- |
| `features/approvals/approval-detail.ts`, `.html`, `.scss` | The request page: details, buttons, comment box, error messages |
| `features/approvals/approvals.service.ts` | Typed calls for detail, approve, reject |
| `features/approvals/approvals.html`, `.ts`, `.scss` | Inbox screen (from US-06; part of the diff because the base is `0acdbf3`). The html has no link to the detail page |
| `auth/return-url.ts` | `safeReturnUrl`: only an in-app path is accepted |
| `auth/auth.guard.ts`, `features/login/login.ts` | Keep `returnUrl` and use it after sign-in |
| `auth/role-guard.ts`, `app.routes.ts` | Route `approvals/:requestId` borrows the `approvals` navigation roles (Manager) |

**Database:** no migration. The columns (`DecidedByEmployeeId`, `DecidedAtUtc`, `DecisionComment`, outbox, audit) already existed (spec data-model).

**Tests:** web specs `approval-detail.spec.ts`, `approvals.service.spec.ts`, `return-url.spec.ts`, `auth.guard.spec.ts`, `role-guard.spec.ts`, `app.routes.spec.ts`, `login.spec.ts` (I did not read them line by line). No .NET test project exists (Phase 5), so the rule, use case and store have no automated test.

**Docs and tooling:** `ai/specs/us07-approve-reject/` (spec, plan, research, data-model, `contracts/decision-api.md`, quickstart, tasks, checklist), `ai/specs/INDEX.md`, `ai/learning/us06-manager-inbox.md`.

## Design versus code

| Point | Design says | Code does | Match? |
| --- | --- | --- | --- |
| Who may decide | Manager role, direct reports only, never own (product.md, 3d, rule 14 area) | `RequireManager` policy, then team read from DB; own request is never in own team, so 404 | Yes |
| Not your report | Return 404, not 403 (security rule 4) | Missing and not-a-report both 404 | Yes |
| Reject needs comment | Requirements BR-06 | 422 `comment.required`; over 500 chars is 400 | Yes |
| Error codes | 3d uses snake_case codes | Dotted codes (`request.not_pending`), same as US-03 (research R10) | Differs, chosen on purpose |
| Concurrency | 3d ADR 23: `If-Match` ETag on decisions | Guarded update on `Status = Pending`, no ETag (research R3) | Differs, chosen on purpose |
| Days on approve | Jira: "balance is deducted" | Nothing changes; days already held since submit (spec FR-004, research R1) | Differs from Jira |
| Days on reject | Product rules: restored on reject | Atomic `Available + days`, ledger `Restore`, day rows deleted | Yes |
| Audit in same transaction | Security rule 14 | `audit.WriteAsync` joins the same `DbContext` transaction | Yes |
| No names or free text in audit/outbox | Security rule 12 | Ids, outcome, days only; comment stays on the request | Yes |
| Employee emailed | Jira criterion 2 | Outbox row only | Not built (4g) |
| Idempotency key | Apply uses one | None; a repeat gets 409 (research R11) | Differs, chosen on purpose |

**Design documents used:** repo `ai/specs/us07-approve-reject/` (spec, research, tasks, plan, data-model read; `contracts/decision-api.md` and `quickstart.md` not read in detail); `ai/steering/security.md`; `ai/steering/product.md`; `US-knowledge/US-06-manager-inbox.md`. Confluence (Requirements 98443, Leave-Policy Rules 229377, UI/UX 65835, DB 1048577 and 1179649, API 3d 1277972, HLD 1277954) was not read in full by me or by the caller; I rely on how the repo spec quotes them.
**Looked for and not found:** Confluence pages for 3e, 3f and 3g.

## State of the tasks

- Done (ticked at `origin/main`): T001 to T008, T010, T012, T015 to T018.
- Open: T009 and T011 (run scenarios A to D against the real API), T013 (prove the resource rule), T014 (check rows with read-only queries), T019 (browser scenario G), T020 (`/ci-check`), T021 (`/review`, `/learn`, `/status`). The run tasks change real data and need the owner's OK.

## Findings and gaps

- **Jira "balance is deducted" on approve.** The project deducts at submit and restores on reject. Approve only moves days from pending to used. The code follows the spec. Jira's wording looks out of date. The owner should confirm and fix the Jira text.
- **Jira "employee is emailed".** Only the outbox row is written. Email delivery is increment 4g (spec FR-012, accepted by the owner). The criterion is not met until 4g.
- **Deep link has no source yet.** No email exists, so nobody gets the link. The route and return-URL flow exist.
- **Inbox cards do not link to the detail page.** `approvals.html` has no link or route to `approvals/:id`. A manager can only reach the page by typing or following a URL. The US-06 note flagged this.
- **Jira shows no PR.** The code went to main inside PR #9 under SCRUM-13. The branch `SCRUM-14-US-07-...` still points at `0acdbf3`. The Jira Development panel will stay empty.
- **INDEX is out of date.** `ai/specs/INDEX.md` at main says "T001 to T007 done, T008 next". Tasks show more done.
- **Not verified:** any real call to the API or database, any browser run, any .NET test (none exist), the 409 race behaviour, the `Restore` ledger rows, the `dotnet format analyzers` exit code. Resource-rule proof (T013) is open.
- **Possible weak spot (my reading, not tested):** if the balance row is missing on reject, the store rolls back and throws, so the manager sees a server error. That is deliberate (never lose days silently).

## Why it is built this way

| Decision | Chosen | Rejected, and why |
| --- | --- | --- |
| One transaction across modules | Named `LeaveDecisionStore` | Separate saves with compensation: a crash leaves a decided request with days not restored (R2) |
| Notification | Outbox row in the same transaction | Event after commit: lost if the process dies after commit (R2) |
| Only one decision wins | Guarded `UPDATE ... WHERE Status = Pending` | Load then `SaveChanges` with concurrency exception: more code. ETag `If-Match`: nothing else can edit a pending request (R3) |
| Reject frees dates | Delete the day rows | Keep rows and filter by status: unique index would still block, needs a migration (R4) |
| Days to restore | Saved `DaysCount` snapshot | Recalculate from day rows (R5) |
| Not-your-report answer | 404 for both cases | 403 confirms the id exists (R6) |
| Comment storage | Only on the request | Copy into outbox: breaks rule 12 (R7) |
| Return address after sign-in | `returnUrl` query checked by `safeReturnUrl` | Cookie or storage for the wanted URL: adds state and breaks the "token in memory only" spirit (R12) |
| Idempotency | None; repeat gives 409 | Key like apply: more machinery, no harm from a repeat (R11) |

## Interview practice

1. **Q:** Two managers (or a manager and the employee's cancel) act at the same moment. How do you stop a double decision? **A:** One guarded `UPDATE ... WHERE Status = 'Pending'`. The database lets only one writer match the row. Zero rows changed means someone was first, so roll back and return 409. No lock held in code, no extra read.
2. **Q:** Why 404 and not 403 when the request is not your report's? **A:** 403 says "this exists but you may not". An attacker could probe ids. 404 for missing and forbidden looks the same (IDOR defence, security rule 4). The role policy still gives 403 to non-managers, which leaks nothing about ids.
3. **Q:** What is an open redirect and how do you stop it here? **A:** A sign-in link like `/login?returnUrl=https://evil.site` that sends the user away after login. `safeReturnUrl` accepts only a path with one leading `/`, no `//`, no backslash, no control characters, and never `/login`. Anything else goes to `/dashboard`.
4. **Q:** What is the outbox pattern? **A:** Write the "send this message" row in the same transaction as the business change. A separate worker sends it later. If the transaction fails, no message exists. If the process dies after commit, the row is still there. It avoids the dual-write problem. Here the worker is 4g.
5. **Q:** Clean Architecture says modules do not touch each other's tables. Why does `LeaveDecisionStore` write to four schemas? **A:** The decision, restored days, audit and outbox need one atomic commit. The constitution allows one named store for that, and the spec must name it. The port is in Application and the implementation in Infrastructure, so the dependency rule holds.
6. **Q:** Why is the comment not in the audit row? **A:** Audit and outbox are append-only and long-lived. Free text can hold personal data. They keep ids, outcome and days. The comment lives on the request, and the email builder reads it from there.
7. **Q:** Why `ExecuteUpdateAsync` and not load-modify-save for the balance? **A:** `Available = Available + days` runs inside SQL, so two changes cannot overwrite each other from a stale read.

**Teach-back (answer in your own words):**
1. Walk through what the database does when two decisions arrive for the same pending request. What does each caller see?
2. Jira says "balance is deducted" on approve. Explain what really happens to the balance on approve and on reject, and why.
3. A manager who is signed out opens `/approvals/42`. Trace every step until the page shows, and say where the redirect is checked.

**Exercise (paper):** Write down the four rows that exist after Ravi rejects request 42 (table, key columns, values). Then say which of them would be missing if the transaction rolled back at step 3.
