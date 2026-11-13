# US-knowledge

One note per Jira story, written in simple words: what the story asks, what was built, how it works and why. Use it to learn the project and to prepare interview answers.

## How to create or refresh a note

In Claude Code, from the repository root:

```
/us-explain SCRUM-8          one story by Jira key
/us-explain US-01            one story by its id
/us-explain all              every story that is not "To Do" and has no note yet
```

The skill (`.claude/skills/us-explain/SKILL.md`) reads the story from Jira and the code changes from git, then asks the `story-explainer` agent (`.claude/agents/story-explainer.md`) to write the note from `_template.md`. It does not change Jira or code, and it does not commit. Review the note, then commit it yourself.

## How to read the notes

Open `US-knowledge\index.html` in a browser. Each note is a normal web page with tables, a contents bar and a dark mode. The `.md` files are the source; the `.html` files are generated. To rebuild all pages after any change, run this from the repository root:

```
node US-knowledge/build-html.js
```

## What every note contains

1. What the story is, in simple words
2. What the code delivers: an ASCII flow, numbered steps and one example
3. Acceptance criteria and where each one is delivered
4. Code changes, file by file, grouped as back end, front end, tests and docs
5. Design versus code: where the design documents and the code agree or differ
6. State of the tasks
7. Findings and gaps, including what was not verified
8. Why it is built this way, and what was rejected
9. Interview practice and teach-back questions

## Index

| Id | Story | Jira status | Code | Note | Written |
| --- | --- | --- | --- | --- | --- |
| US-01 | Sign in and see only what my role allows (SCRUM-8) | In Progress | PR #2 (open) | [US-01-sign-in-by-role.md](US-01-sign-in-by-role.md) | 2026-11-10 |
| US-02 | See my leave balance per leave type (SCRUM-9) | Done | No PR; uncommitted on branch `SCRUM-9-US-02-See-my-leave-balance-per-leave-type` (base d91c678) | [US-02-see-leave-balance.md](US-02-see-leave-balance.md) | 2026-11-10 |
| US-03 | Apply for leave with an accurate working-day count (SCRUM-10) | To Do | No PR; commit 790f8f6 pushed on branch `SCRUM-10-US-03-Apply-for-leave-with-an-accurate-working-day-count` (base 9390de5) | [US-03-apply-leave-working-days.md](US-03-apply-leave-working-days.md) | 2026-11-13 |
