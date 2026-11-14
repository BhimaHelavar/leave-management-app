<!--
Sync Impact Report (temporary; remove before commit)


Version change: 1.1.0 -> 1.2.0 (MINOR: materially expanded guidance)
Modified principles: III (adds one exception to the schema-module rule: a single cross-module
  write store inside one transaction)
Added sections: none
Removed sections: none
Deferred TODOs: none
Source: owner decision for SCRUM-10 / US-03 (analysis finding C1, option a), 2026-11-13;
  mirrored in ai/steering/structure.md.
Earlier: 1.0.0 -> 1.1.0 for SCRUM-26 (Demo environment); mirrors security.md rule 6.
-->
# Leave Management (LMS) Constitution

This constitution distills the standing rules of the project. The detailed rules live in
`CLAUDE.md` and `ai/steering/`; those files stay the single source of truth for detail.
If this file and a steering file disagree, stop and ask the owner.

## Core Principles

### I. The Server Enforces Every Rule
Every business and authorization rule MUST be enforced in the API. The Angular app only shapes
the screen. Database constraints stay as the second line of defence.
Rationale: a browser can be bypassed; only the server is trusted. (`security.md` rules 1, 8, 9)

### II. Security by Default
Every endpoint MUST require a signed-in user unless it is marked `AllowAnonymous` on purpose.
Authorization has two layers: role policies and resource rules. A record the caller may not see
MUST return 404, not 403. Errors MUST be `ProblemDetails` with no stack traces or SQL. Secrets
MUST NOT be in the repo, and `.env` files MUST NOT be read or printed. Every apply, approve,
reject, cancel and balance change MUST write an append-only audit row in the same transaction.
Dev sign-in MUST run only in the `Development` or `Demo` environment, and a Demo deployment MUST
be access-restricted to the owner.
Detail: `ai/steering/security.md` (14 rules; a review fails if one is broken).

### III. Clean Architecture Dependency Rule
Dependencies point inward: Api and Infrastructure -> Application -> Domain. Domain MUST NOT
reference EF Core, ASP.NET or any package. Changes stay small and in the layer where they belong.
Code in one schema module MUST NOT read another module's tables.
One exception: a single Application-level store MAY write across modules in one database
transaction when a rule needs that atomicity (for example a leave request, its balance deduction
and its audit row). Each such store MUST be named in its spec. Reference data from other modules
MUST still be read through ports.
Detail: `ai/steering/structure.md`.

### IV. Correct Dates and Time
Leave days MUST be `DateOnly`. Timestamps MUST be UTC. "Now" MUST come from `TimeProvider`,
never `DateTime.UtcNow`.

### V. Migrations Are Append-Only
An applied migration MUST NOT be edited; add a new one. The generated SQL MUST be read first.

### VI. Spec-Driven Development
Feature code MUST NOT be written without an approved spec in `ai/specs/<id>-<name>/`
(requirements, then design, then tasks), with owner approval after each. Tasks are done one at a
time, in order. If the spec is wrong, stop, fix it, and get approval again.

### VII. Verify Before Claiming Done
Before a task is called done, the build and the tests for the touched files MUST be run and the
result reported. Anything not built, run or verified MUST be said plainly. A check that was not
done MUST NOT be claimed. Detail: `ai/steering/tech.md`.

### VIII. Ask First, Teach Always
The agent MUST ask the owner before adding a NuGet or npm package, deleting data, dropping the
database, or running anything destructive. This is also a learning project: explanations go
plain words first (what, how, why, when), and each finished spec produces
`ai/learning/<spec-id>.md`. The agent MUST NOT do the owner's teach-back or exercise.
Detail: `ai/steering/learning.md`.

## Technology and Security Constraints

- Stack: .NET 10, EF Core 10, SQL Server, Angular 22 (standalone, Signals, zoneless).
- Auth is JWT bearer with one switch (`Auth:Mode`); business code MUST NOT read it. Dev mode MUST
  refuse to start outside the `Development` or `Demo` environment; `Demo` MUST NOT migrate or
  seed at startup.
- Personal data is limited to name, email and role; audit and outbox rows hold ids and values only.
- Commands given to the owner MUST work in Command Prompt or PowerShell on Windows.
- Policy rules are the 15 accepted rules in `ai/steering/product.md`.

## Development Workflow

- Skills: `/spec-new`, `/spec-implement`, `/review`, `/learn`, `/status`. Spec Kit commands
  (`/speckit-*`) may be used, but `ai/specs/` and its approval gates remain the project's record.
- Each spec finishes with `/review`, `/learn` and `/status`; status lives in `ai/specs/INDEX.md`.
- Steering files are loaded through `CLAUDE.md`; coding rules load from `.claude/rules/`.

## Governance

This constitution summarizes and does not override the steering files. Amendments require the
owner's approval and a version bump: MAJOR for removing or redefining a principle, MINOR for a new
principle or materially expanded guidance, PATCH for wording fixes. A change to a steering rule
MUST be mirrored here in the same change. Every review (`/review`) MUST check compliance with
these principles.

**Version**: 1.2.0 | **Ratified**: 2026-11-02 | **Last Amended**: 2026-11-13
