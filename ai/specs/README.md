# Specs

One folder per feature, named `<id>-<short-name>`, for example `4d-apply-leave`.

```
ai/specs/<id>-<short-name>/
  requirements.md   what the feature must do, as testable criteria
  design.md         how: data, endpoints, flow, files, decisions
  tasks.md          small checklist; each task names its files and how to verify it
```

Start a new one with `/spec-new <id>-<short-name>`. Copy shapes from `_template/`.

## Rules

- Each document has a `Status:` line: `Draft`, `Approved` (with the date) or `Changed after approval`.
- Do not start a document until the previous one is `Approved`.
- Requirements use the "WHEN ... THE SYSTEM SHALL ..." form so each one can become a test.
- Every requirement has an id (`R1`, `R2`...). Design sections and tasks cite the ids they satisfy.
- Out of scope is written down, so nobody builds it by accident.
- If implementation shows the spec is wrong, stop, change the spec, ask for approval again, then continue.
