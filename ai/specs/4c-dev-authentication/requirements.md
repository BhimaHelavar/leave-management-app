# 4c: Dev authentication: Requirements

Status: Draft (written from the 3g security design; needs the owner's approval)
Spec: 4c-dev-authentication
Depends on: 4b-database

## Goal

Let a developer sign in as any demo person without a password, so every later feature can be built and tested against real roles. The API must already work the way it will with Entra ID: it only validates a token and reads claims.

## User stories

- As a developer, I want to pick a demo person on a login screen, so that I can test the app as an Employee, Manager, HR or Admin.
- As the owner, I want every endpoint closed by default, so that a forgotten check cannot expose data.

## Requirements

| Id | Requirement | How it will be checked |
| --- | --- | --- |
| R1 | WHEN a person opens the web app without a session THE SYSTEM SHALL show the login screen listing the active demo people with their roles | Screen check; web test |
| R2 | WHEN a person is picked THE SYSTEM SHALL return a signed JWT valid for 60 minutes with claims `sub` (employee id), `name`, `email` and `role` (one entry per role; everyone has `Employee`) and the web app SHALL show the home screen | API call; web test |
| R3 | WHEN a request has no valid token and the endpoint is not marked public THE SYSTEM SHALL answer 401 | API call without a token to `/api/me` |
| R4 | WHEN a token is expired, tampered with, or has the wrong issuer or audience THE SYSTEM SHALL answer 401, and the web app SHALL clear the session and go to the login screen | API call; web interceptor test |
| R5 | THE SYSTEM SHALL define the policies `RequireManager`, `RequireHR` and `RequireAdmin`, and `GET /api/me` SHALL return the signed-in person's id, name, email and roles read from the token | API call as each demo person |
| R6 | WHEN `Auth:Mode` is `Dev` and the environment is not Development THE SYSTEM SHALL refuse to start; WHEN `Auth:Mode` is `EntraId` THE SYSTEM SHALL fail at startup with a message that it arrives in 4g | Start-up check |
| R7 | WHEN more than 30 requests per minute come from one client address to the dev endpoints THE SYSTEM SHALL answer 429 | Manual or integration test |
| R8 | THE SYSTEM SHALL keep the token in memory only, so a page reload signs the person out | Web test; reload check |
| R9 | WHEN a person is inactive or unknown THE SYSTEM SHALL NOT issue a token (404) | API call |
| R10 | THE SYSTEM SHALL keep `/health` public | API call without a token |

## Edge cases and errors

| Case | Expected behaviour |
| --- | --- |
| Dev signing key shorter than 32 characters | API refuses to start with a clear message |
| API not running when the login screen opens | Screen shows "could not load people" and a retry button |
| Token expires while the app is open | Next API call returns 401; app signs out and shows login |
| Browser tab reloaded | Signed out (by design, R8) |

## Out of scope

- Refresh tokens, "remember me", logout on the server
- Entra ID sign-in (4g)
- Leave rules and resource-based authorization (4d, 4e)
- Per-user rate limit for the whole API
- Back-end test project (Phase 5); 4c is verified by manual API checks and front-end tests

## Open questions

1. Should the login screen hide the Admin demo person? Default: show all six people.
2. Should the dev token endpoints also be turned off by a setting while in Development? Default: no, `Auth:Mode` is the switch.

## Learning goal

Explain authentication versus authorization, what a JWT contains and how the API validates it, why Dev and Entra ID share the same claims, why the token is kept in memory, and what a fallback policy does.
