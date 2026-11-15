# Quickstart: check approve and reject end to end

Run from the repo root in PowerShell or Command Prompt. Run the API from one place only (Visual Studio F5 **or** `dotnet run`).

## Prerequisites

1. API: `dotnet run --project src\LeaveManagement.Api` (http://localhost:5000, Development, migrates and seeds). Web (for the screen check): `cd web` then `npm start` (http://localhost:4200).
2. Seeded people: **Meera Rao** (id 1, Manager) with reports Asha Nair (4), Ravi Kumar (5), Sneha Iyer (6). Hema Shetty (2, HR) and Arjun Das (3, Admin) have no manager. Read `http://localhost:5000/api/dev/users` first; ids can differ.
3. Pending requests from the earlier checks may still be in LocalDB (Sneha ids 35, 36, 38, 40 and some of Asha's). **Deciding a request is permanent and there is no undo.** Use requests created for this check, or ask the owner which existing ones may be decided.

> **These checks write data.** Every successful decision changes a request (and, for a rejection, a balance, day rows, an audit row and an outbox row), with no way to reverse it in this story. Ask the owner before running scenarios B to F, name the requests that will be decided, and report exactly what was changed. The refusal checks and the reads write nothing.

Get a token (PowerShell), without printing it:

```text
$token = (Invoke-RestMethod -Method Post -Uri http://localhost:5000/api/dev/token -ContentType "application/json" -Body '{"employeeId":1}').accessToken
$h = @{ Authorization = "Bearer $token" }
```

Check the field name in the response if `accessToken` is empty (see `ai/specs/4c-dev-authentication`). Use employee id 4, 5 or 6 for an Employee token and 2 for HR.

## Automated checks

```text
dotnet build LeaveManagement.slnx
cd web
npm test -- --watch=false
npm run build
npm run format:check
```

Expected: build with 0 warnings and 0 errors, all web tests pass. **Not covered:** the server rules (no .NET test project yet).

## Scenario A: refusals that write nothing (read-only effect)

Use a pending request id `R` of one of Meera's reports (read it from `GET /api/approvals`). Write the status code after each call.

| Call | Expect |
| --- | --- |
| `POST /api/approvals/R/reject` with `{}` | 422 `comment.required` |
| `POST /api/approvals/R/reject` with `{"comment":"   "}` | 422 `comment.required` |
| `POST /api/approvals/R/reject` with a 501 character comment | 400 |
| `POST /api/approvals/R/reject` with `not json` | 400 |
| Asha's token (Employee) on any of the three calls | 403 |
| Hema's token (HR, no Manager role) | 403 |
| No `Authorization` header | 401 |
| Meera, `GET /api/approvals/999999` | 404 |
| After all of the above, `GET /api/approvals/R` | still `Pending`, same balance, same request count |

## Scenario B: reject with a comment (decides one request)

1. Note the owner's Annual `available` (`GET /api/me/balances` with their token) and the request's `daysCount`.
2. Meera: `POST /api/approvals/R/reject` with `{"comment":"Release week, please move the dates."}`. Expect 200, `status` `Rejected`, `decision.comment` as sent, `allowedActions` `[]`.
3. Owner's balance: `available` is higher by exactly `daysCount`; `pending` is lower by the same.
4. The owner applies again for the same dates (a new `Idempotency-Key`, see the US-03 contract). Expect 201 (the dates are free).
5. Database (read-only look, optional): the request's `LeaveRequestDay` rows are gone; one `Restore` ledger entry exists for the request; one `leave.reject` audit row and one `leave.decided` outbox row exist; neither holds the comment.

## Scenario C: approve (decides one request)

1. Note the owner's Annual `available`, `pending`, `used`.
2. Meera: `POST /api/approvals/R2/approve` with no body, or `{"comment":"Enjoy."}`. Expect 200, `status` `Approved`.
3. Owner's balance: `available` unchanged; `pending` lower by `daysCount`; `used` higher by the same.
4. The owner applies for overlapping dates. Expect 409 `leave.overlap` (an approved request still holds its days) and the message names the dates.

## Scenario D: a second decision, and two at once

1. Decide the same request again (approve or reject). Expect 409 `request.not_pending` and nothing changes.
2. Two decisions together on a pending request `R3`: start two jobs (`Start-Job`) that call approve and reject at the same time. Expect exactly one 200 and one 409, and one audit row and one outbox row for the request.

## Scenario E: the wrong manager

The seeded data has one manager. Report NOT RUN for "another manager's report gets 404" unless a second manager and a report are created for the check (the owner decides). The following can be run: Meera deciding her **own** request (create a pending request for Meera first, only if the owner agrees) returns 404.

## Scenario F: Unpaid request

Decide a pending Unpaid request (create one only if the owner agrees). Expect: reject restores nothing and writes no ledger entry; approve changes nothing. Otherwise report NOT RUN.

## Scenario G: the screen and the link

If a browser can be driven:

1. Signed out, open `http://localhost:4200/approvals/R` (a pending request of Meera's report). Expect the sign-in page; sign in as Meera. Expect to land on that request with Approve and Reject buttons.
2. Choose Reject with an empty comment: the screen asks for one and sends nothing. Write a comment and send: the page shows Rejected and no buttons.
3. Open the link for a decided request: the outcome and no buttons.
4. Open the link for a request of someone who is not Meera's report, and for an id that does not exist: the same not-found page.
5. Try a sign-in return address that is not a path inside the app (for example `//example.com`): the dashboard opens, not that address.

If a browser cannot be driven, say these were covered only by the Vitest specs.

## Record the result

Write what ran and what did not (for example "browser not driven", "second manager not created", "Unpaid not run", "no .NET tests") into the task notes, with the exact requests decided. Do not claim a check that was not run.
