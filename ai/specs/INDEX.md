# Spec index

Status values: `Planned`, `Requirements`, `Design`, `Tasks`, `In progress`, `Review`, `Done`.

| Spec | What | Status | Notes |
| --- | --- | --- | --- |
| 4a-scaffold | Solution, Docker, CI, health endpoint | Done | Built before specs existed; no spec folder |
| 4b-database | EF Core entities, 13 tables, seed data | Done | Built before specs existed; design is the 3c document |
| [4c-dev-authentication](4c-dev-authentication/requirements.md) | Dev login with JWT, deny-by-default, login screen | In progress | 8 of 10 tasks done (T1 to T8). Code exists: the dev token and dev users endpoints answered in the US-04 check. Waiting for the owner end-to-end check (T9) and wrap-up (T10). The tasks file still says "Draft" at the top |
| [t02-angular-shell](t02-angular-shell/spec.md) | SCRUM-26: app shell, role-aware nav placeholder, health, one deployed environment | In progress | 13 of 30 task boxes ticked (counted by `/status`, not re-verified against code); depends on 4c |
| [us01-sign-in-by-role](us01-sign-in-by-role/tasks.md) | SCRUM-8: sign in and see only what my role allows | In progress | 11 of 14 tasks done. Open: T006 owner browser check, T013 review, T014 learn. Row added by `/status`; code not re-verified here |
| [us02-see-leave-balance](us02-see-leave-balance/tasks.md) | SCRUM-9: see my leave balance per leave type | In progress | 16 of 23 boxes ticked, but T001 is unticked while later tasks are done, so the list needs tidying. Open: browser checks T014, T019, T021, then review T022 and learn T023. The balances endpoint works (used in the US-04 check). Row added by `/status`; PR #4 is merged |
| [us03-apply-leave-working-days](us03-apply-leave-working-days/spec.md) | SCRUM-10: apply for leave with an accurate working-day count, rule checks, preview | Done | T001 to T029 done; PR #5 merged to main. Constitution amended to 1.2.0 for one cross-module store. No .NET tests yet (Phase 5); Angular screens covered by Vitest only, not driven in a browser. Covers the apply part of 4d |
| [us04-apply-half-day](us04-apply-half-day/spec.md) | SCRUM-11: apply for a half day; gap-closing spec on top of us03 (half-day choice disabled, not hidden, on a multi-day range; end-to-end check) | Review | All 7 tasks done. Review done (no High; 3 Medium and Low 4 fixed and re-reviewed; Low 5 to 7 left as they were). Learning note in `ai/learning/us04-apply-half-day.md`; owner teach-back and exercise still to do. Not committed yet. Web only, no back-end change. Scenario A submit and scenario B browser check not run. Open decision: a weekend or holiday half day returns `halfday.invalid`, not `days.none` |
| 4d-apply-leave | Apply for and view own leave | Planned | The apply part is done by us03; viewing own requests remains |
| 4e-approval-workflow | Approve, reject, cancel, audit log, concurrency | Planned | |
| 4f-balances-calendar-admin | Balances, holidays, team calendar, HR and Admin screens | Planned | |
| 4g-later-increments | Redis, RabbitMQ, Entra ID mode | Planned | |

Update this table whenever a spec changes status (the `/status` skill does it).
