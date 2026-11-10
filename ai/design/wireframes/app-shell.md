# App shell (top bar)

Spec t02. Source: `ai/specs/t02-angular-shell/contracts/navigation.md`. The spec says Phase 3a has no wireframe for the shell itself, so this is a sketch from the written contract.

## Desktop (768 px and wider)

```text
[Skip to main content]   <- first focusable element, visible on focus
+--------------------------------------------------------------------------+
| Leave Management | Dashboard | My requests | Approvals | ...   | Name  v   |
+--------------------------------------------------------------------------+
|                                                                          |
|   Main content (routed view)                                             |
|                                                                          |
+--------------------------------------------------------------------------+
  left: app name, link to /dashboard    right: user menu (person's name)
  middle: links the roles allow; current one has aria-current="page"
```

## Mobile (below 768 px)

```text
+------------------------------------+
| Leave Management     [Menu]  Name  |
+------------------------------------+
| (menu open)                        |
|   Dashboard                        |
|   My requests                      |
|   Approvals                        |
+------------------------------------+
| Main content                       |
+------------------------------------+
```

## Links by role (additive)

| Role | Links |
| --- | --- |
| Employee (everyone) | Dashboard `/dashboard`, My requests `/requests` |
| Manager | Approvals `/approvals` |
| HR | Reports `/reports`, Exceptions `/exceptions`, Audit `/audit` |
| Admin | Leave types `/leave-types`, Holidays `/holidays`, Audit `/audit` |

Audit shows once when a person is both HR and Admin. Hiding a link is a convenience only; the server decides.

## Placeholder for the 8 section paths

```text
+--------------------------------------+
| Approvals                            |
| Coming soon                          |
+--------------------------------------+
```

The dashboard placeholder also shows the API health card (`/health` is not a route).
