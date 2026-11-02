# /ai: how AI works on this project

This folder is the project's memory and process for Claude Code. People and agents both read it.
Claude Code finds a few things only in fixed places, so the setup is split in two:

| Where | What lives there | Why there |
| --- | --- | --- |
| `CLAUDE.md` (repo root) | Short rules, imports the steering files below | Claude Code loads it at every session start |
| `ai/steering/` | Standing facts: product, tech, structure, security, learning | Imported by `CLAUDE.md`, so always in context |
| `ai/specs/` | One folder per feature: requirements, design, tasks | The source of truth for what to build |
| `ai/decisions/` | Index of architecture decisions | Why things are the way they are |
| `ai/learning/` | Study notes and interview Q&A per feature | The learning goal of the project |
| `.claude/agents/` | Specialist subagents | Claude Code only looks here |
| `.claude/skills/` | Slash commands (`/spec-new`, `/spec-implement`, ...) | Claude Code only looks here |
| `.claude/rules/` | Coding rules that load only for matching files | Keeps `CLAUDE.md` short |
| `.claude/settings.json` | Permissions (what Claude may run without asking) | Claude Code only looks here |

## The spec-driven loop

Code is written from an approved spec, never from a loose idea. A human approves at every STOP.

```
  idea
   |
   v
  /spec-new <id-name>  --->  requirements.md   (what, with testable criteria)
                                  |  STOP: you approve
                                  v
                              design.md         (how: endpoints, data, flow, files)
                                  |  STOP: you approve
                                  v
                              tasks.md          (small checklist, each with a verify step)
                                  |  STOP: you approve
                                  v
  /spec-implement <id-name> ---> task 1 -> build + tests -> tick -> task 2 -> ...
                                  |
                                  v
  /review  --->  code-reviewer + security-reviewer (in parallel) -> findings by severity
                                  |
                                  v
  /learn <id-name> ---> ai/learning/<id-name>.md  (what, how, why, when + interview Q&A)
                                  |
                                  v
  /status  --->  ai/specs/INDEX.md updated (and the SDLC tracker if you use it)
```

## Why spec-driven

- The spec is reviewed before code exists, so mistakes are cheap to fix.
- Each task is small and has a verify step, so Claude can check its own work.
- The same spec explains the feature in an interview: requirement, design, trade-off, test.

## Subagents (who does what)

| Agent | Job | Can edit code? |
| --- | --- | --- |
| `spec-writer` | Drafts requirements, design, tasks from the steering docs | Only under `ai/` |
| `backend-engineer` | .NET, EF Core, API | Yes |
| `frontend-engineer` | Angular | Yes |
| `test-engineer` | Tests for what was just built | Tests only |
| `code-reviewer` | Correctness, readability, tests (read-only) | No |
| `security-reviewer` | Auth, validation, secrets, data exposure (read-only) | No |
| `learning-coach` | Study notes and interview Q&A | Only under `ai/learning/` |

## Daily use

1. Open the repo folder in a terminal and start Claude Code (`claude`).
2. `/status` to see where you are.
3. `/spec-new 4d-apply-leave` (or continue an open spec), review and approve each document.
4. `/spec-implement 4d-apply-leave`, then `/review`, then `/learn 4d-apply-leave`.

## Keeping this folder healthy

- Steering files are loaded every session, so keep each under about 100 lines.
- A spec that is finished stays in `ai/specs/` as history. Mark it `Done` in `INDEX.md`.
- If you change a standing decision, update `ai/steering/` and `ai/decisions/README.md` in the same change.
- `CLAUDE.local.md` and `.claude/settings.local.json` are for personal settings and are git-ignored.
