# Spec index

Status values: `Planned`, `Requirements`, `Design`, `Tasks`, `In progress`, `Review`, `Done`.

| Spec | What | Status | Notes |
| --- | --- | --- | --- |
| 4a-scaffold | Solution, Docker, CI, health endpoint | Done | Built before specs existed; no spec folder |
| 4b-database | EF Core entities, 13 tables, seed data | Done | Built before specs existed; design is the 3c document |
| [4c-dev-authentication](4c-dev-authentication/requirements.md) | Dev login with JWT, deny-by-default, login screen | Requirements | Spec drafted, waiting for approval. No 4c code has been delivered yet |
| [t02-angular-shell](t02-angular-shell/spec.md) | SCRUM-26: app shell, role-aware nav placeholder, health, one deployed environment | Requirements | Spec Kit format; spec and plan approved; tasks drafted, waiting for approval; depends on 4c |
| [us03-apply-leave-working-days](us03-apply-leave-working-days/spec.md) | SCRUM-10: apply for leave with an accurate working-day count, rule checks, preview | Review | Spec Kit format; T001 to T029 done, reviewed by the security and code reviewers and fixed; constitution amended to 1.2.0 for one cross-module store; no .NET tests yet (Phase 5); Angular screens covered by Vitest only, not driven in a browser; waiting for owner review; covers the apply part of 4d |
| 4d-apply-leave | Apply for and view own leave | Planned | The apply part is done by us03; viewing own requests remains |
| 4e-approval-workflow | Approve, reject, cancel, audit log, concurrency | Planned | |
| 4f-balances-calendar-admin | Balances, holidays, team calendar, HR and Admin screens | Planned | |
| 4g-later-increments | Redis, RabbitMQ, Entra ID mode | Planned | |

Update this table whenever a spec changes status (the `/status` skill does it).
