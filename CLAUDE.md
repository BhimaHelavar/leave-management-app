# Leave Management (LMS)

Employee leave management web app: Angular 22 + .NET 10 + SQL Server. A solo portfolio project with a second goal: the owner is learning to build and explain senior-level full-stack work. Build it the way a good senior team would, and explain the why.

## How we work: spec-driven

Do not write feature code without an approved spec in `ai/specs/<id>-<name>/`.

1. `requirements.md`, then `design.md`, then `tasks.md`. **Stop for the owner's approval after each one.**
2. Implement one task at a time, in order. Run the build and tests for the files you touched. Tick the box only after the verify step passes.
3. If the spec turns out to be wrong, stop, fix the spec, get approval again, then continue.
4. Finish with `/review`, `/learn`, `/status`.

Skills: `/spec-new`, `/spec-implement`, `/review`, `/learn`, `/status`. Agents: `spec-writer`, `backend-engineer`, `frontend-engineer`, `test-engineer`, `code-reviewer`, `security-reviewer`, `learning-coach`. Full guide: `ai/README.md`. Current status: `ai/specs/INDEX.md`.

## Golden rules

- The server enforces every rule. The Angular app only shapes the screen.
- Never put secrets in the repo. Never read or print `.env` files.
- Never edit a migration that has been applied. Add a new one. Read the generated SQL first.
- Leave days are `DateOnly`; timestamps are UTC. Take "now" from `TimeProvider`, not `DateTime.UtcNow`.
- Ask before adding a NuGet or npm package, deleting data, dropping the database, or running anything destructive.
- Keep changes small and in the layer where they belong (`ai/steering/structure.md`).
- The owner works on Windows. Give commands that work in Command Prompt or PowerShell.
- Say plainly what you could not build, run or verify. Never claim a check you did not do.

## Standing context

@ai/steering/product.md
@ai/steering/tech.md
@ai/steering/structure.md
@ai/steering/security.md
@ai/steering/learning.md

Coding rules for each part load automatically from `.claude/rules/` when you work on matching files.
