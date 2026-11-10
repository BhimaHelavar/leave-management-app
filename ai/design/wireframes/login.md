# Login (Dev mode people picker)

Spec 4c. A sketch from the spec text, not from the Phase 3a wireframe (not in the repo). Dev mode only.

```text
+------------------------------------------------+
|                 Leave Management               |
|                                                |
|   Sign in as (development only)                |
|                                                |
|   +------------------------------------------+ |
|   | Person A          Employee, Manager      | |
|   +------------------------------------------+ |
|   | Person B          Employee               | |
|   +------------------------------------------+ |
|   | Person C          Employee, HR           | |
|   +------------------------------------------+ |
|   | ...                                      | |
|   +------------------------------------------+ |
|                                                |
|   Could not load people            [Try again] |
+------------------------------------------------+
```

| Element | Behaviour |
| --- | --- |
| Person row | Click or Enter: `POST /api/dev/token`, keep token in memory, go to `/` |
| List | From `GET /api/dev/users`; shows name and roles only |
| Error line | Shown only when the list fails to load; Try again reloads it |

Names above are placeholders, not the seeded people.
