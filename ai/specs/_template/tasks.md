# <id>: <feature name>: Tasks

Status: Draft
Design: approved on <date>

Progress: 0 of N done.

Rules for tasks: each is small enough to finish and verify in one sitting, names the files it touches, cites the requirement ids it satisfies, and says how to verify it. Do the tasks in order. Tick a box only after its verify step passed.

- [ ] **T1: <title>** (R1)
  - Files: `path`
  - Do: ...
  - Verify: `command` or a manual check

## Done when

- All boxes ticked
- Build and tests pass
- `/review` has no open high or medium findings
- `ai/learning/<id>.md` written
- `ai/specs/INDEX.md` updated
