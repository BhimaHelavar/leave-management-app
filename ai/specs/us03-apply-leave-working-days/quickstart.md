# Quickstart: verify US-03 end to end

This guide says **what to check and what you should see**. It holds no implementation code. Nothing here has been run yet: the feature is not built.

## Prerequisites

- API running from one place only (Visual Studio F5 **or** `dotnet run --project src\LeaveManagement.Api`), Development environment, LocalDB, dev sign-in.
- Web app running: in `web`, `npm start`.
- A dev person with enough Annual balance (the US-02 demo scenarios give some). Check the dashboard first and write down Annual available.
- Dates must be in the future (back-dating is refused). Pick a future **Friday to Tuesday** range. If your range has no holiday on the Monday, add one to the seed data as the task says (research R8), so scenario 2 is testable.
- Tip: calls need a bearer token from the dev sign-in endpoint and a fresh `Idempotency-Key` per new application. SSMS can confirm the database rows.

## Automated checks (what exists today)

| Check | Command (from repo root unless noted) | Expect |
| --- | --- | --- |
| Build back end | `dotnet build LeaveManagement.slnx` | 0 warnings, 0 errors |
| Web tests (in `web`) | `npm test -- --watch=false` | all pass, including the new `apply-leave` and `leave-request.service` specs |
| Build web (in `web`) | `npm run build` | succeeds |

There is no .NET test project yet (Phase 5). The back-end rules below are checked by hand.

## Manual scenarios

| # | Scenario | Do | Expect |
| --- | --- | --- | --- |
| 1 | Jira example 1 | Apply Annual, a Fri to the following Tue, no holiday | 201, `daysCount` 3.0, status Pending |
| 2 | Jira example 2 | Same range with a holiday on the Monday | `daysCount` 2.0 |
| 3 | Weekend not counted | Apply Fri to Mon | 2.0 (Fri, Mon); Saturday and Sunday not counted |
| 4 | Half day | Apply one working day with `SecondHalf` | 0.5, one `LeaveRequestDay` row with slot PM |
| 5 | Balance and dashboard | Reload the dashboard after scenario 1 | Annual pending up by 3, available down by 3 |
| 6 | Preview equals saved | Preview scenario 1 dates, then submit | Preview `daysCount` equals the saved value |
| 7 | Short notice | Apply Annual starting on the next working day (not a weekend, or you get `days.none`) | 422 `notice.short`, balance unchanged |
| 8 | Too long | Apply Annual for 16 calendar days | 422 `max.consecutive`, message says HR |
| 9 | Overlap | Apply again for any day already requested | 409 `leave.overlap` |
| 10 | Not enough balance | Apply for more days than available | 422 `balance.insufficient`; `Available` still >= 0 |
| 11 | Crosses a year | Start 30 Dec, end 2 Jan | 422 `year.cross` |
| 12 | Weekend only | Apply Sat to Sun | 422 `days.none` |
| 13 | Past date | Start yesterday | 422 `date.past` |
| 14 | End before start | Reverse the dates | 422 `date.range` |
| 15 | Double submit | Send scenario 1 twice with the **same** key | Both return the same 201; one request row, one deduction |
| 16 | Parallel submit | Send two requests for the same dates at once, with different keys. Use two Command Prompt or PowerShell windows and send both within a second, or a small PowerShell script that starts two `Invoke-RestMethod` jobs. If you cannot do it, say so in the report. | One 201, one 409; one deduction |
| 17 | Unpaid | Apply Unpaid | 201, no balance row changed, no `Deduction` ledger row |
| 18 | Over-posting | Add `status`, `employeeId`, `daysCount` to the body | Ignored: saved row is Pending, owned by the caller, with the server count |
| 19 | No token | Call without a token | 401 |
| 20 | Speed (SC-003) | Time from pressing Submit to the dashboard showing the new pending figure (browser dev tools Network tab or a stopwatch). Do it 3 times and write down the slowest. | Under 3 seconds |
| 21 | Parallel, same key | Send the same request twice at once with the **same** key | Both calls return the same 201; one request, one deduction |

## Counting table (SC-002)

Use real future dates. "H" means the demo Monday holiday from the seed data (see the log line at API start). Hand-count each row first, then compare with the preview and the saved `daysCount`.

| # | Range | Expect |
| --- | --- | --- |
| 1 | Fri to the next Tue, no holiday | 3.0 |
| 2 | Fri to the next Tue, with H as the Monday | 2.0 |
| 3 | Mon to Fri, no holiday | 5.0 |
| 4 | Mon to Fri, with W (the demo Wednesday holiday) in that week | 4.0 |
| 5 | Fri to the next Mon | 2.0 |
| 6 | Mon to the next Mon (8 calendar days) | 6.0 |
| 7 | One Wednesday, full day | 1.0 |
| 8 | One Wednesday, `FirstHalf` | 0.5 |
| 9 | Sat to Sun | refused, `days.none` |
| 10 | A single day that is H (the demo Monday holiday) | refused, `days.none` |
| 11 | Fri to Mon with a holiday on the Saturday in between (only if the data has a holiday on a weekend; otherwise skip and say so) | 2.0, not 1.5 |

Rows 1 to 10 are required for SC-002 (at least 10 ranges). The seed data gets two demo holidays (task T012): H on a Monday and W on a Wednesday in a different week. Both dates are written to the API log at startup.

## Database checks (SSMS)

After scenario 1:

- `leave.LeaveRequest`: one new row, `DaysCount` 3.0, Status `Pending`, `DecidedAtUtc` null.
- `leave.LeaveRequestDay`: 6 rows (3 days x AM and PM).
- `balances.LeaveBalance`: `Available` down by 3.0.
- `balances.LeaveBalanceEntry`: one `Deduction` row, `Days` -3.0, `RequestId` set.
- `audit.AuditEvent`: one `leave.apply` row; open `DetailJson` and confirm it has **no** reason text, name or email.
- `leave.IdempotencyKey`: one row for your key.

After any 4xx scenario: none of the tables above gained a row, and `Available` did not change.

## Not verified until built

Every item above. When the tasks are done, report exactly which scenarios you ran and which you could not (for example the parallel submit in scenario 16 may need a small script).
