# Quickstart: check the manager inbox end to end

Run from the repo root in PowerShell or Command Prompt. Run the API from one place only (Visual Studio F5 **or** `dotnet run`).

## Prerequisites

1. API: `dotnet run --project src\LeaveManagement.Api` (http://localhost:5000, Development, migrates and seeds).
2. Web (for the screen check): `cd web` then `npm start` (http://localhost:4200).
3. Seeded people (check `http://localhost:5000/api/dev/users`): **Meera Rao** (id 1, Manager) with three direct reports: Asha Nair (id 4), Ravi Kumar (id 5), Sneha Iyer (id 6). Hema Shetty (id 2, HR) and Arjun Das (id 3, Admin) have no manager. Ids may differ on another database; read them first.
4. Existing pending requests in the owner's LocalDB (from the US-05 checks, if still there): Sneha ids 35 (2026-12-01 to 12-02), 36 (2026-12-04), 38 (2026-12-07 to 12-08), 40 (2026-12-10). Asha also had pending leave. Read the inbox first to see what is really there.

> **Reading writes nothing.** Scenarios A, B, D, E and F only read. Scenario C needs **new pending requests** (a request by Asha or Ravi on the same dates as Sneha's), which write to the owner's LocalDB with no way to remove them until 4e. Ask the owner before running C, name the requests, and report exactly what was created.

Get a token (PowerShell), without printing it. Use employee id 1 for the manager:

```text
$token = (Invoke-RestMethod -Method Post -Uri http://localhost:5000/api/dev/token -ContentType "application/json" -Body '{"employeeId":1}').accessToken
$h = @{ Authorization = "Bearer $token" }
```

If `accessToken` is empty, check the field name in the response (see `ai/specs/4c-dev-authentication`).

## Automated checks

```text
dotnet build LeaveManagement.slnx
cd web
npm test -- --watch=false
npm run build
npm run format:check
```

Expected: build with 0 warnings and 0 errors, all web tests pass. **Not covered:** the server rules (no .NET test project yet).

## Scenario A: the list, sorting and fields (reads only)

1. `Invoke-RestMethod -Uri "http://localhost:5000/api/approvals" -Headers $h`.
2. Expect: 200, every item is `Pending`, belongs to Asha, Ravi or Sneha, has dates, `daysCount`, `balance` and `teamOverlap`; `startDate` never goes down from one item to the next; equal start dates are ordered by `submittedAtUtc`.
3. `totalCount` equals the number of pending requests of the three reports (count them in the database or from the earlier checks) and the Jira example holds: with 3 pending requests from reports you see 3 items. If the data does not give exactly that, say so and report the closest case.
4. `balance.before` minus `balance.after` equals `daysCount` for each item.
5. No `reason`, email or other field outside the contract appears.

## Scenario B: who may see it (reads only)

| Caller | Expect |
| --- | --- |
| Meera (Manager), the call above | 200, only her three reports' requests, none of her own |
| Asha (Employee only), token for employee id 4 | 403, no list |
| Hema (HR only, no Manager role), token for employee id 2 | 403 |
| No `Authorization` header | 401 |
| Meera with `?page=0` or `?pageSize=500` or `?pageSize=abc` | 400 |

Run `Invoke-WebRequest` with `-SkipHttpErrorCheck` (or catch the exception) to read the status code.

## Scenario C: team overlap (creates pending requests; owner must agree)

1. With Sneha's request 35 on 2026-12-01 to 12-02 present, create a pending Annual request for Asha (token for id 4) on 2026-12-02 only, with a new `Idempotency-Key` (see the US-03 contract). Expect 201.
2. Read the manager inbox. Expect: Sneha's request names Asha with the shared date `2026-12-02`; Asha's request names Sneha with `2026-12-02`; `awayIfApproved` is 2 and `teamSize` is 3 on both.
3. A request with no overlap (for example Sneha's 2026-12-10) shows `others: []` and `awayIfApproved` 1.
4. Half days (optional, only if the owner agrees to one more request): Ravi first half and Asha second half on one free date show **no** overlap; a full day for Asha on the same date as Ravi's first half does.
5. Rejected, cancelled and approved cases cannot be created (4e not built). Report them as NOT RUN.

## Scenario D: a manager with no reports (reads only)

Use a token for a person with the Manager role and no reports, if one exists. The seeded data has none; report NOT RUN unless one is created for the check (the owner decides).

## Scenario E: the screen

If a browser can be driven: open `http://localhost:4200`, choose Meera, open **Approvals**, and check the loading state, the list, the overlap text in words, paging buttons (shrink `pageSize` is not available on the screen, so paging is covered by Vitest), and that an Employee (Asha) does not see the Approvals link and is sent to the not-found view if she types `/approvals`. If not, say it was covered only by the Vitest specs.

## Scenario F: reading changes nothing (reads only)

Note the Annual balances of Sneha, Asha and Ravi and the request counts before and after several reads. Expect: identical.

## Record the result

Write what ran and what did not (for example "browser not driven", "overlap not created", "approved case not creatable", "no .NET tests") into the task notes. Do not claim a check that was not run.
