# <ID>: <story title>

**Jira:** <KEY> · <type> · <status> · <priority> · epic <KEY> · <sprint>
**Code:** PR #<n> (<open or merged>) · branch `<name>` · head `<hash>` · <N> files (+<added>, -<removed>)
**Written:** <date>. I read the code and the design documents. I did not build or run anything, unless a line below says so.

## What the story is

<3 to 4 short sentences in plain words, as if telling a friend who is not a developer.>

**Why it matters:** <one line>

## What the code delivers

```
<ASCII flow, left to right or top to bottom. Real names: screens, endpoints, tables.
 Maximum 12 lines. No Mermaid: it shows as raw text in many viewers.>
```

1. <Step in plain words. Say what happens, how, and why.>
2. ...

**Example:** <One walk-through with a real person, role and endpoint from this app.>

## Acceptance criteria: where each one is delivered

| Criterion (from Jira) | Delivered by | State |
| --- | --- | --- |
| <criterion 1> | <file or test> | Done / Partly / Not found |

## Code changes

**Back end (`src/`)**

| File | What it does |
| --- | --- |

**Front end (`web/src/app/`)**

| File | What it does |
| --- | --- |

**Database**

| File | What it does |
| --- | --- |

**Tests**

| File | What it checks |
| --- | --- |

**Docs and tooling**

| File | What it is |
| --- | --- |

(Delete a group that has no files. A file I did not read line by line says so.)

## Design versus code

| Point | Design says | Code does | Match? |
| --- | --- | --- | --- |
| <path, role, status code, rule> | <text, with source> | <what the diff shows> | Yes / Differs / Not built |

**Design documents used:** <title and page id or path, one per line>
**Looked for and not found:** <list, or none>

## State of the tasks

- Done: <list>
- Open: <list>

## Findings and gaps

- <Where Jira, the design and the code disagree>
- <Known gaps>
- <What was not verified>

## Why it is built this way

| Decision | Chosen | Rejected, and why |
| --- | --- | --- |

## Interview practice

1. **Q:** ... **A:** ... (3 to 5 of these, short answers)

**Teach-back (answer in your own words):**
1. ...
2. ...
3. ...
