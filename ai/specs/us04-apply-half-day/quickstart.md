# Quickstart: check the half-day story end to end

Run from the repo root in PowerShell or Command Prompt. Run the API from one place only (Visual Studio F5 **or** `dotnet run`).

## Prerequisites

1. API: `dotnet run --project src\LeaveManagement.Api` (http://localhost:5000, Development, migrates and seeds).
2. Web: `cd web` then `npm start` (http://localhost:4200).
3. Sign in with a dev user who has Annual balance. Pick a future Monday to Friday for the dates (notice rule: Annual needs 3 working days).

## Automated checks (screen)

```text
cd web
npm test -- --watch=false
npm run build
npm run format:check
```

Expected: all tests pass, build completes, Prettier is clean. Not covered by these: the server (no .NET test project yet).

## Scenario A: Jira criterion 1, single date, first half

1. Open Apply for leave. Choose Annual. Set start and end to the same future working day.
2. "Length of the day" is enabled. Choose First half.
3. Expect: preview "Working days counted: 0.5".
4. Submit. Expect: saved request counts 0.5, and the dashboard pending figure rose by 0.5.

## Scenario B: Jira criterion 2, multi-day range

1. Choose Annual. Set start to a day and end to a later day.
2. Expect: "Length of the day" is visible, disabled, showing Full day, with the hint text.
3. Set the end date back to the start date. Expect: enabled again, starting at Full day.
4. Pick Second half, then move the end date later. Expect: disabled and back to Full day.

## Scenario C: server refuses what the screen would not send (SC-003)

Call the API directly with a half day on a range. Get a token from the dev sign-in endpoint (see `ai/specs/4c-dev-authentication`), then PowerShell:

```text
$h = @{ Authorization = "Bearer $token"; "Idempotency-Key" = [guid]::NewGuid().ToString() }
$body = '{"leaveTypeId":1,"startDate":"2026-12-07","endDate":"2026-12-08","halfDay":"FirstHalf"}'
Invoke-RestMethod -Method Post -Uri http://localhost:5000/api/me/leave-requests -Headers $h -ContentType "application/json" -Body $body
```

Expect: HTTP 422, code `halfday.invalid`, and no balance change. Check the exact route, field names and header in `ai/specs/us03-apply-leave-working-days/contracts/leave-requests-api.md` first; adjust the command if they differ.

## Scenario D: weekend or holiday half day

Choose a single Saturday (or a seeded holiday) and First half. Expect: a refusal for a day with no working time.

## Record the result

Write what ran and what did not (for example "browser not driven" or "no .NET tests") into the task notes. Do not claim a check that was not run.
