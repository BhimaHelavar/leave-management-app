---
name: us-explain
description: "Explain a Jira story in simple words and save it as a note in US-knowledge/. The theory comes from the Jira story and the design documents (Confluence space LMA and the repo's ai/ folder); the code explanation comes from the open pull request or the story's branch. Use when the owner says \"explain SCRUM-8\", \"what is done for US-01\", \"explain this story\" or \"explain all stories\"."
argument-hint: "[Jira key like SCRUM-8, story id like US-01, or: all]"
disable-model-invocation: true
---

Explain the story `$ARGUMENTS` and save the note in `US-knowledge/`.

Two sources, two jobs:
- **Theory (what and why)** comes from the Jira story and the design documents.
- **Code (what was really built)** comes from the open pull request, or the branch, or the merged change.

This skill reads and writes notes only. It never edits Jira or Confluence, never changes code, never switches branches, never commits and never pushes.

## 1. Pick the stories

- A Jira key (`SCRUM-8`): that story.
- A story id (`US-01`, `T-02`): find it with a Jira search on the summary, for example JQL `project = SCRUM AND summary ~ "US-01"`.
- `all`: every Story or Task whose status is not `To Do` and that has no note in `US-knowledge/` yet. List them, then do them one at a time.
- Empty argument: ask the owner which story, and stop.

## 2. Theory part A: read the Jira story (you do this, not the subagent)

Use the Jira tools of the Atlassian connection. If they are not connected, say so and stop. Never guess what a story says. Offer to continue if the owner pastes the story text.

Collect: key, summary, type, status, priority, labels, parent epic, sprint, description with the acceptance criteria, subtasks, comments, the requirement id in the description (for example BR-01) and the linked pull requests (Development field).

## 3. Theory part B: read the design documents

Read the parts that matter for this story, not everything. Start with the outline of a page, then read only the relevant sections.

1. **Links in the story.** Open every Confluence link in the Jira description first.
2. **Confluence space `LMA`.** Use the Confluence search with a CQL such as `space = "LMA" AND type = page ORDER BY title ASC` to list the pages, then pick the ones that apply. Normally:
   - Phase 1 requirements (the business requirement for the story's BR id, the policy rules, the non-functional requirements);
   - Phase 3a UI/UX (the screens and flows the story touches);
   - Phase 3b high-level design (modules, layers);
   - Phase 3c database design (tables, constraints);
   - Phase 3d API design (endpoints, roles, error format, decisions);
   - later design pages if they exist (class design, Angular design, security design).
   Also search the key words of the story, for example `space = "LMA" AND text ~ "audit"`.
3. **The repo.** Read `ai/specs/<spec folder for this story>/` (spec, plan, research, data-model, contracts, quickstart, tasks), `ai/design/`, `ai/steering/` and `ai/decisions/`. Find the spec folder by searching `ai/specs` for the Jira key or the story id.
4. Write down the list of documents you used (title and page id, or path) and the ones you looked for and did not find. The note shows both.

## 4. Code part: open pull request first

Work read-only, without changing the owner's checkout. Use refs, `git diff` and `git show`, never `git checkout`.

1. **Open pull request.** Run `gh pr list --state open --search "<KEY>"`. If `gh` cannot be used, use `gh api repos/<owner>/<repo>/pulls?state=open` or `git ls-remote origin "refs/pull/*"`. Also use the pull request count in the Jira Development field as a hint.
   - Found: base = the pull request's base branch, head = its head. Get the head with `git fetch origin pull/<n>/head`. Use `git diff --stat <base>...FETCH_HEAD`, the commit list, and read the changed files with `git show FETCH_HEAD:<path>`.
2. **Branch without a pull request.** `git branch -a --list "*<KEY>*"` and `git ls-remote --heads origin "*<KEY>*"`. If found, base = `main`, head = that branch.
3. **Already merged.** `gh pr list --state merged --search "<KEY>"` or `git log --all --oneline --grep="<KEY>"`. Use the merge commit and its first parent.
4. **Nothing found.** Write the note with "No code found for this story yet" and list where you looked.

A story can be spread over several branches (for example a task branch it depends on). List them all and say which one you used. Record the head commit hash you used.

## 5. Write the note

Delegate to the `story-explainer` subagent. Give it:
- the Jira facts from step 2, copied in (it has no Jira access);
- the design excerpts from step 3, copied in (it has no Confluence access), with the source of each excerpt; it can read repo documents itself;
- the pull request number, base and head refs, head hash, commit list and spec folder from step 4;
- the output path `US-knowledge/<ID>-<short-slug>.md` (ID from the summary, for example `US-01-sign-in-by-role.md`; a task becomes `T-02-angular-shell.md`);
- the instruction to follow `US-knowledge/_template.md`.

If a note for this story already exists and its recorded head hash and the Jira status have not changed, tell the owner it is up to date and stop. Otherwise write it again.

For `all`, run one subagent at a time.

## 6. Update the index

Add or update the row for this story in `US-knowledge/README.md`: id, title, Jira status, pull request, note link, date written. Keep the table sorted by id.

## 7. Build the readable pages

Run `node US-knowledge/build-html.js` from the repository root. It turns every note in `US-knowledge/` into an `.html` page next to it (and `README.md` into `index.html`). The `.md` file stays the source; never edit the `.html` by hand. If `node` is missing, say so and tell the owner the `.md` note is still complete.

## 8. Tell the owner

Reply in simple words, in this order: what the story is, what is done, what is not done or not verified. Add one small diagram only if it helps. Give the path of the note and say: open `US-knowledge\index.html` in a browser to read it as a normal page.

State clearly:
- what came from Jira, from the design documents and from the code, and what you could not check (for example "the build and tests were not run");
- every place where the design documents and the code differ, as findings;
- the commands to commit the note, written for Windows, but do not run them. Commit only if the owner asks.

If Jira and the code disagree (for example the story says audited but no audit code exists, or Jira says Done but the pull request is open), say it as a finding. Do not edit Jira.
