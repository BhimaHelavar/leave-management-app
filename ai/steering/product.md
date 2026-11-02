# Product

**Leave Management (LMS)**: employees apply for leave, managers approve it, HR manages policy and balances, admins manage users. MVP is one office location (Bengaluru, Asia/Kolkata).

## Roles

Everyone is an **Employee**. A person can also hold **Manager**, **HR**, **Admin** (roles are additive).

| Action | Employee | Manager | HR | Admin |
| --- | --- | --- | --- | --- |
| Apply, view, cancel own leave | yes | yes | yes | yes |
| View team's leave and calendar | no | direct reports | all | all |
| Approve or reject leave | no | direct reports, never own | escalations and manager-less requests, logged | no |
| Adjust balances, leave types, holidays | no | no | yes | yes |
| Manage users and roles | no | no | no | yes |
| View audit log | no | no | yes | yes |

## Accepted policy rules (the 15 rules)

1. Leave types are admin-configurable; seven are seeded (Annual, Sick, Casual, Comp-off, Unpaid, Maternity, Paternity).
2. Annual accrues monthly (18 days a year, 1.5 a month).
3. Joiners get a pro-rata amount by completed month, rounded to the nearest 0.5.
4. Carry-forward: Annual only, up to 10 days, expires 31 March.
5. A balance can never go below zero (hard block). Unpaid has no balance.
6. Probation: Annual cannot be used in the first 3 months.
7. Notice: Annual needs 3 working days.
8. Maximum consecutive days: Annual 15; longer needs HR.
9. Sick leave over 2 days needs a medical certificate.
10. Sandwich rule is off in the MVP.
11. A half day is first or second half, a single day, counts 0.5.
12. Cancel: employee before start; HR after start.
13. A person cannot have overlapping pending or approved requests.
14. If the approver is absent, HR approves on behalf.
15. Maternity and paternity are separate leave types; HR grants the entitlement.

Other facts: a request never crosses a leave year (the employee submits two); days are deducted at submit and restored on reject or cancel; a rejection needs a comment; `DaysCount` on a request is a snapshot and is never recalculated.

## Planned increments

4a scaffold, 4b database (done), 4c dev login, 4d apply and view own leave, 4e approval workflow and audit log, 4f balances, holidays, calendar, HR and Admin screens, 4g Redis, RabbitMQ notifications, Entra ID mode. Status lives in `ai/specs/INDEX.md`.

## Source documents

Requirements, UI/UX (12 screens), HLD, database (3c), API (3d), class design (3e), Angular design (3f) and security design (3g) were written in earlier phases as Claude Docs and in Confluence (space LMA). When this file and those documents disagree, ask the owner before choosing.
