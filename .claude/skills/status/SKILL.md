---
name: status
description: "Show where the project stands, refresh the spec index, and say what to do next. Use at the start or end of a work session, or when the owner asks \"where are we\"."
disable-model-invocation: true
---

Report the project status.

1. Read `ai/specs/INDEX.md` and, for each spec in progress, its `tasks.md` (count ticked and unticked tasks).
2. Check the real state against the INDEX: for each spec, does the code in the repo match the status? Correct the INDEX where it is out of date (status, notes).
3. Show a compact table: spec, status, progress. Then list:
   - What is done and verified
   - What is written but not yet verified (for example not compiled)
   - What the owner still owes (approvals, teach-back answers, exercises)
   - The next step, with the exact command or skill to run
4. If the owner keeps an SDLC tracker workbook and an `sdlc-tracker-updater` skill is available, offer to run it for the finished sub-phases. Do not edit the workbook without asking.
