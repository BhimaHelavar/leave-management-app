# Contract: navigation and routes

Source: Phase 3a "Figma-ready spec, Navigation" and screen list. Hiding a link is a convenience only; the server decides.

## Links by role (additive)

| Role | Links added |
|---|---|
| Employee (everyone) | Dashboard `/dashboard`, My requests `/requests` |
| Manager | Approvals `/approvals` |
| HR | Reports `/reports`, Exceptions `/exceptions`, Audit `/audit` |
| Admin | Leave types `/leave-types`, Holidays `/holidays`, Audit `/audit` |

Audit appears once when a person holds both HR and Admin.

## Routes

| Path | View | Allowed roles |
|---|---|---|
| `/` | redirect to `/dashboard` | any signed-in |
| the 8 paths above | shared placeholder ("Coming soon", names the section) | per table |
| `/health` is NOT a route | the health card is shown on the dashboard placeholder | any signed-in |
| anything else, or a path the person's roles do not allow | not-found view S-12 with "Back to dashboard" | any |

Sign-in route and the redirect to it for a person without a session belong to 4c.

## Top bar

- Left: app name "Leave Management" (a link to `/dashboard`).
- Middle: the visible links; the current one marked with `aria-current="page"`.
- Right: user menu showing the person's name.
- Below 768 px the links collapse into a menu button; every control is keyboard reachable and labelled.
- A "skip to main content" link is the first focusable element.
