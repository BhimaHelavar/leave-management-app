---
name: spec-implement
description: "Implement the tasks of an approved spec one at a time, verifying and ticking each. Use when the owner says \"implement\", \"build it\" or \"continue\" for a spec whose requirements, design and tasks are approved."
argument-hint: "[spec folder name, for example 4c-dev-authentication]"
disable-model-invocation: true
---

Implement the spec `$ARGUMENTS`.

1. Read `ai/specs/$ARGUMENTS/requirements.md`, `design.md` and `tasks.md`. If any of them does not say `Approved`, stop and tell the owner which one still needs approval.
2. Set the spec's status to `In progress` in `ai/specs/INDEX.md`.
3. Find the first unticked task. Show its title, files and verify step in one or two lines.
4. Delegate the task: back-end files go to the `backend-engineer` subagent, files under `web/` go to the `frontend-engineer` subagent. A task that needs both is split by layer. Give the subagent the spec folder name and the exact task text.
5. When the subagent reports, check its claims: run the task's verify step yourself if you can (`dotnet build`, `npm test -- --watch=false`, `npm run build`). If it cannot run here, say so.
6. If the verify step passed, tick the box in `tasks.md` and update the `Progress:` line. If not, fix it or report the blocker and stop. Never tick a task that did not pass its verify step.
7. If a task needs an action from the owner (stop a running API, create a migration, click through a screen), give simple numbered steps and stop until they confirm.
8. Continue to the next task. After every 3 tasks, or at the end, give a short status and ask whether to continue.
9. When all tasks are ticked, run the `test-engineer` subagent for any requirement without a test, then tell the owner to run `/review`.

If the spec turns out to be wrong or incomplete, stop, explain, propose the change to the spec and wait for approval. Do not work around it in code.
