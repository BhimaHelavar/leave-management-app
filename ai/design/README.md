# /ai/design: graphs and wireframes

All design visuals live here. A `design.md` in a spec explains one feature; this folder is the picture of the whole system and the screens.

## Formats

| What | Format | Why |
| --- | --- | --- |
| Graphs (architecture, ER, sequence, state) | Mermaid in a fenced `mermaid` block | Plain text, diffs in git, renders on GitHub and in the VS Code Markdown preview |
| Wireframes | ASCII boxes in a fenced `text` block | Plain text, no tool needed, easy to review in a pull request |

A generated code graph (Graphify) is a possible later addition. It is not part of this folder yet.

## Index

| File | Shows | Source |
| --- | --- | --- |
| [architecture/system-context.md](architecture/system-context.md) | People, browser, API, database, later services | `ai/steering/tech.md` |
| [architecture/layers.md](architecture/layers.md) | Project dependency rule and web app shape | `ai/steering/structure.md` |
| [architecture/data-model.md](architecture/data-model.md) | Tables and links across the 5 schemas | `src/LeaveManagement.Domain`, EF configurations |
| [flows/dev-auth.md](flows/dev-auth.md) | Dev sign-in and token use | spec 4c |
| [flows/leave-lifecycle.md](flows/leave-lifecycle.md) | Leave request states and apply checks | `ai/steering/product.md` |
| [wireframes/README.md](wireframes/README.md) | Screen list and status | Phase 3a (external) |
| [wireframes/login.md](wireframes/login.md) | Login screen | spec 4c |
| [wireframes/app-shell.md](wireframes/app-shell.md) | Top bar, desktop and mobile | spec t02 |
| [wireframes/not-found.md](wireframes/not-found.md) | Not-found view (S-12) | spec t02 |

## Rules for new design work

1. Every `design.md` that changes the structure, a flow or a screen links to or adds a file here.
2. Every UI change has a wireframe in `wireframes\`. Draw mobile too when the layout changes below 768 px.
3. Keep names equal to the code (entity, class and route names). Do not invent fields.
4. Add the new file to the index above in the same change.
5. Say what is not drawn yet instead of guessing.
