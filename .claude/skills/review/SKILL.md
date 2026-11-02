---
name: review
description: "Review the current changes with the code reviewer and the security reviewer in parallel and report findings by severity. Use after implementing a task or a spec, before calling it done."
argument-hint: "[optional spec folder name]"
disable-model-invocation: true
---

Review the current work. Spec (optional): `$ARGUMENTS`.

1. Work out what changed: `git status` and `git diff HEAD` if this is a git repository; otherwise the files named in the spec's tasks.
2. Start the `code-reviewer` and `security-reviewer` subagents **in parallel**, each with the spec folder (if known) and the list of changed files.
3. Merge their reports into one list: **High**, **Medium**, **Low**. Remove duplicates. For each finding keep the file, line, problem and suggested fix.
4. Show the owner the merged list. Do **not** change code in this skill. Ask which findings to fix; fix them through the right engineer subagent, then re-run only the reviewer that raised them.
5. A spec is not done while a High or Medium finding is open. Say so clearly.
