---
name: ci-check
model: haiku
description: "Run the same checks as the GitHub Actions workflow (.github/workflows/checks.yml) on the owner's PC before a commit or push: build, coding standards, unit tests, repo guard and package vulnerabilities for src and web. Use when the owner says \"run the CI checks\", \"is it safe to push\", or before opening a pull request."
---

Run the CI checks locally and report the truth about each one. This skill only checks and reports. It never fixes, formats, installs or commits by itself.

Keep this list in step with `.github/workflows/checks.yml`. If the workflow changed, follow the workflow and tell the owner this skill is out of date.

## Rules

- Run from the repo root. Commands must work on Windows (the Bash tool here is Git Bash; give the owner PowerShell or Command Prompt forms when you tell them what to run).
- Run every step even if an earlier one fails, so the owner sees all problems at once.
- If the build fails with "file is locked by LeaveManagement.Api.exe", the API is running. Tell the owner to stop it (one copy only), then rerun. Do not kill their process unless they say so.
- Never run `npm run format`, `dotnet format` without `--verify-no-changes`, or any command that edits files. If a standards check fails, show the files and offer the fix command; wait for a yes.
- Do not add packages, delete data or touch the database.
- Say plainly what was skipped and why. Never mark a step PASS that you did not run.

## Steps

### 1. Repo guard
- `git ls-files`: fail if any tracked path matches `(^|/)\.env($|\.)` other than `.env.example` or `.env.sample`, or ends in `.pfx`, `.p12`, `.pem` or `.key`.
- Search `web/src` for `localStorage` or `sessionStorage` calls (`setItem` or `getItem`) that mention `token` or `jwt`. Any hit fails (security rule 7).

### 2. API (src)
1. `dotnet restore LeaveManagement.slnx`
2. `dotnet build LeaveManagement.slnx --no-restore -c Release` (warnings are errors by `Directory.Build.props`)
3. Coding standards, read-only:
   - `dotnet format style LeaveManagement.slnx --verify-no-changes --severity warn --no-restore`
   - `dotnet format analyzers LeaveManagement.slnx --verify-no-changes --severity warn --no-restore`
4. Unit tests: if a `*Tests.csproj` or `*Test.csproj` is tracked, run `dotnet test LeaveManagement.slnx --no-build -c Release`. Otherwise report "NOT RUN: no .NET test project yet (Phase 5)".

### 3. Web
Run in `web`. If `node_modules` is missing, report it and tell the owner to run `npm ci`; do not install.
1. `npm run format:check`
2. `npm run test:ci`
3. `npm run build`

### 4. Packages
- API: `dotnet list LeaveManagement.slnx package --vulnerable --include-transitive`. Fail if the output contains "has the following vulnerable packages".
- Web: `cd web` then `npm run audit:ci`. This needs network access; if it cannot reach the registry, report "NOT RUN" and say why.

## Report

Show one table, then the details of every failure.

| Step | Result | Notes |
| --- | --- | --- |
| Repo guard | PASS / FAIL / NOT RUN | |
| API build | | |
| API standards | | |
| API unit tests | | |
| Web standards | | |
| Web unit tests | | |
| Web build | | |
| API packages | | |
| Web packages | | |

End with one line: **Safe to push** only if every step is PASS or an honest NOT RUN that CI will cover (name it). Otherwise **Not ready**, followed by the first fix to try and the exact command, for example `cd web` then `npm run format` for Prettier. Mention that CI runs on Linux, so a check that only fails there (for example line endings) cannot be seen locally.
