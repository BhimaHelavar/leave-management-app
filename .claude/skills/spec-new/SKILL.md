---
name: spec-new
description: "Start a new feature spec, beginning with the requirements document. Use when the owner says \"new spec\", \"spec for X\", or begins a new increment such as 4d."
argument-hint: "[id-and-short-name, for example 4d-apply-leave]"
disable-model-invocation: true
---

Start the spec `$ARGUMENTS`.

1. If `$ARGUMENTS` is empty, ask the owner for an id and a short name (for example `4d-apply-leave`) and stop.
2. Read `ai/specs/INDEX.md` and `ai/specs/README.md`. If a folder `ai/specs/$ARGUMENTS` already exists, tell the owner which documents exist and their status, and ask what to do. Do not overwrite.
3. Ask the owner for a one- or two-sentence goal only if the INDEX and the design documents do not already make it clear.
4. Delegate to the `spec-writer` subagent: create `ai/specs/$ARGUMENTS/` from `ai/specs/_template/`, and write **only** `requirements.md` with `Status: Draft`.
5. Add or update the row in `ai/specs/INDEX.md` with status `Requirements`.
6. Show the owner a short summary: the requirements table, the open questions with their defaults, and out-of-scope items.
7. **Stop.** Ask the owner to approve or change the requirements. When they approve, set `Status: Approved (<date>)` in the file, then offer to write `design.md` next. Do the same approval step for `design.md` and `tasks.md`. Never write the next document before the previous one is approved.
