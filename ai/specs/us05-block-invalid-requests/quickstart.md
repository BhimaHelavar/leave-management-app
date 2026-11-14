# Quickstart: check the block rules end to end

Run from the repo root in PowerShell or Command Prompt. Run the API from one place only (Visual Studio F5 **or** `dotnet run`).

## Prerequisites

1. API: `dotnet run --project src\LeaveManagement.Api` (http://localhost:5000, Development, migrates and seeds).
2. Pick a dev user and note the id from `http://localhost:5000/api/dev/users`. Read `GET /api/me/balances` first and pick one with **at least 7 Annual days available** (scenarios B, C and F take 2 + 1 + 2 = 5 days as pending). Say which user and how many days were taken.
3. Use the dates written below (December 2026, Monday to Friday, more than 3 working days ahead). Check the chosen user has no request on them.

> **These checks write data.** Scenarios B, C and F create **pending requests** in the owner's LocalDB, and there is no cancel or delete yet (increment 4e). Ask the owner before running them and tell the owner exactly which requests were created. Scenario A and every refusal write nothing.

Get a token (PowerShell), without printing it:

```text
$token = (Invoke-RestMethod -Method Post -Uri http://localhost:5000/api/dev/token -ContentType "application/json" -Body '{"employeeId":6}').accessToken
$h = @{ Authorization = "Bearer $token" }
```

Check the field name in the response if `accessToken` is empty (see `ai/specs/4c-dev-authentication`).

## Automated checks

```text
dotnet build LeaveManagement.slnx
cd web
npm test -- --watch=false
npm run build
npm run format:check
```

Expected: build with 0 warnings and 0 errors, all web tests pass. **Not covered:** the server rules (no .NET test project yet).

## Scenario A: Jira criterion 1, insufficient balance, nothing saved (writes nothing)

1. Read the balance: `Invoke-RestMethod -Uri http://localhost:5000/api/me/balances -Headers $h`. Note Annual `available`, say N.
2. Apply for more working days than N (a range of N + 1 working days, Annual, 3 or more days ahead). Use a new `Idempotency-Key` header each time.
3. Expect: 422 `balance.insufficient`, `You do not have enough balance for these days.`
4. Read the balance again. Expect: `available`, `pending` and `used` are unchanged.
5. Exact Jira example (2 available, ask for 3) needs a person with exactly 2 Annual available. If no seeded user has that, say so and run the closest case, as stated in the report.
6. Rule order (spec FR-008): if scenario B has been run, apply for a range that clashes **and** needs more days than available. Expect the overlap refusal, not the balance one. If it cannot be set up safely, report NOT RUN.

## Scenario B: Jira criterion 2, overlap names the dates (creates one pending request, E1)

1. Create **E1**: a pending Annual request for Tue 2026-12-01 to Wed 2026-12-02. Expect 201.
2. Refusals to check (each writes nothing, expect 409 `leave.overlap`, balance and requests unchanged):

| Shape | Apply for | Expected dates in `detail` |
| --- | --- | --- |
| Start clash | Mon 2026-11-30 to Tue 2026-12-01 | `2026-12-01` |
| End clash | Wed 2026-12-02 to Thu 2026-12-03 | `2026-12-02` |
| Single day | Wed 2026-12-02 | `2026-12-02` |
| New range contains E1 | Mon 2026-11-30 to Wed 2026-12-02 | `2026-12-01, 2026-12-02` (not `2026-11-30`) |

3. **Approved request:** approving is not built, so this case cannot be created. Report it as NOT RUN, covered by the same rule and the same rows.

## Scenario C: two separate clashing requests (creates a second pending request, E2)

1. Create **E2**: a pending request for Fri 2026-12-04.
2. Apply for Mon 2026-11-30 to Fri 2026-12-04. Expect: 409 naming `2026-12-01, 2026-12-02, 2026-12-04` in date order.

## Scenario D: exactly the available balance is accepted (creates a pending request)

Only if the owner agrees: a request that uses exactly the available days is accepted and takes available to 0. Otherwise report NOT RUN. Also check that Unpaid skips the balance check if a safe case exists.

## Scenario E: the screen shows the message

If a browser can be driven: open `http://localhost:4200`, sign in as the same user, apply for Mon 2026-11-30 to Wed 2026-12-02 and check the alert shows `2026-12-01, 2026-12-02`. If not, say it was covered only by the Vitest case.

## Scenario F: two submits at once (creates one pending request, F1; owner must agree)

Use free dates, Mon 2026-12-07 to Tue 2026-12-08, for both calls.

1. **Different keys:** start two `Invoke-RestMethod` POSTs together with PowerShell jobs (`Start-Job`), each with its own `Idempotency-Key`. Expect exactly one 201 and one 409 `leave.overlap`. The 409 names the dates when they can be read, otherwise it has the general wording. Both are acceptable. Exactly one request exists afterwards.
2. **Same key:** repeat the same body and key after step 1 or in parallel on other free dates. Expect the same 201 answer both times and one request only.
3. If the owner does not agree, or the jobs cannot be run safely, report NOT RUN.

## Record the result

Write what ran and what did not (for example "browser not driven", "approved case not creatable", "race not run", "no .NET tests") into the task notes. Do not claim a check that was not run.
