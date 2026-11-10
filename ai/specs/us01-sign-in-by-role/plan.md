# Implementation Plan: Sign in and see only what my role allows

**Branch**: `SCRUM-8-US-01-Sign-in-and-see-only-what-my-role-allows` | **Date**: 2026-11-10 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `ai/specs/us01-sign-in-by-role/spec.md` (Jira SCRUM-8)

## Summary

Sign-in, the session token, deny-by-default and the role policies belong to spec `4c-dev-authentication` (decision FR-011). This plan adds the three things 4c does not have: (1) the server answers **403** when a signed-in person lacks the role, and (2) **writes one append-only audit row** for that refusal (minimal audit writer, FR-007), and (3) it **proves** the role-based navigation and the "land on my dashboard" outcome against the SCRUM-8 acceptance criteria. The refusal and its audit row come from one hook in the authorization pipeline, so no endpoint has to remember to audit.

## Technical Context

**Language/Version**: C# 14 on .NET 10; TypeScript 6 on Angular 22 (standalone, Signals, zoneless)

**Primary Dependencies**: ASP.NET Core authorization (`IAuthorizationMiddlewareResultHandler`), EF Core 10. No new NuGet or npm package.

**Storage**: SQL Server; existing table `audit.AuditEvent`. No schema change, no migration.

**Testing**: Vitest for the web app. Back-end automated tests arrive in Phase 5, so the back end is verified by the manual checks in `quickstart.md`.

**Target Platform**: Windows dev PC (LocalDB), later a restricted Demo environment

**Project Type**: web application (API + Angular app)

**Performance Goals**: not a concern at this scale; a refusal adds one small insert

**Constraints**: audit row holds ids and values only (security rule 12); time from `TimeProvider`; errors are `ProblemDetails`

**Scale/Scope**: one office, tens of users; one new endpoint, one new service, one handler

**Dependencies on other specs** (must exist before the tasks can be done):
- `4c-dev-authentication`: JWT validation, fallback policy, `Policies` with `RequireAdmin` etc., `/api/me`, the sign-in screen. Not implemented on this branch yet.
- `t02-angular-shell` (SCRUM-26, on its own branch): top bar and `nav-items.ts` with the role-to-link table. Merge it before the front-end verification task.

## Constitution Check

*GATE: passes before research and after design.*

| Principle | Result |
| --- | --- |
| I Server enforces every rule | Pass: the 403 and the audit are server side; navigation is convenience only |
| II Security by default | Pass: deny by default comes from 4c; 403 for a missing role, 404 rule for records unchanged; audit row has ids only |
| III Dependency rule | Pass: `IAuditWriter` in Application, implementation in Infrastructure, handler in Api |
| IV Dates and time | Pass: `TimeProvider` for the audit timestamp |
| V Migrations append-only | Pass: no migration |
| VI Spec-driven | Pass once the owner approves requirements, design and tasks |
| VII Verify before done | Applies at implementation; back-end tests not available until Phase 5, said plainly |
| VIII Ask first | Pass: no package added |

Note: this branch has constitution v1.0.0. The `Demo` environment wording in security rule 6 lives on the SCRUM-26 branch; FR-010 here only says "Development or Demo" and relies on 4c.

## Project Structure

### Documentation (this feature)

```text
ai/specs/us01-sign-in-by-role/
├── spec.md
├── plan.md              # this file
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── admin-probe-api.md
│   └── audit-access-denied.md
├── checklists/requirements.md
└── tasks.md             # created later by /speckit-tasks
```

### Source Code (repository root)

```text
src/
├── LeaveManagement.Application/
│   └── Auditing/
│       └── IAuditWriter.cs            # new: interface + AuditRecord
├── LeaveManagement.Infrastructure/
│   └── Persistence/
│       └── AuditWriter.cs             # new: inserts audit.AuditEvent
└── LeaveManagement.Api/
    ├── Auth/
    │   ├── AuditingAuthorizationResultHandler.cs   # new: on Forbidden, audit then 403 ProblemDetails
    │   └── AuthSetup.cs                            # 4c file: register the handler (one line)
    └── Admin/
        └── AdminEndpoints.cs          # new: GET /api/admin/ping, RequireAdmin
web/src/app/
└── layout/nav-items.spec.ts           # T-02 file: add a case per role if missing (no new screen)
```

**Structure Decision**: follows `ai/steering/structure.md`. The only Application type is the interface, so Infrastructure can write the row and Api never touches `DbContext` for auditing. Wireframes and graphs: reuse `ai/design/` from the SCRUM-26 branch once merged; this spec adds no new screen.

## Complexity Tracking

No constitution violations. One judgement call is recorded in `research.md` (an Admin-only probe endpoint).
