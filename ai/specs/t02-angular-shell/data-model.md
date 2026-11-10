# Data Model: Application Shell

No database tables, columns or migrations change in this feature. These are front-end view models and settings only.

## Person (read-only view of the signed-in user)

| Field | Type | Source | Notes |
|---|---|---|---|
| id | string | sign-in claim `sub` (4c) | never shown |
| name | string | claim `name` | shown in the user menu |
| roles | `Role[]` | claims `role` | `Employee` always present; additive |

`Role` = `Employee` \| `Manager` \| `HR` \| `Admin`. Validation: an empty or unknown role list is treated as `['Employee']`.

## NavItem (single table in `layout/nav-items.ts`)

| Field | Type | Notes |
|---|---|---|
| label | string | link text from Phase 3a |
| path | string | route path |
| roles | `Role[]` | person needs ANY of these roles (Employee items list all four) |

Derived: visible items = items where `roles` intersects `person.roles`, de-duplicated by `path` (so Audit shows once for a person who is both HR and Admin).

## HealthReport (existing `Health` interface)

| Field | Type | Notes |
|---|---|---|
| status | string | `Healthy` or `Degraded` |
| authMode | string | `Dev` or `EntraId` |
| database | string | `Connected` or `Unavailable` |
| environment | string | `Development`, `Demo`, ... |
| timeUtc | string | ISO timestamp |

UI state machine: `loading -> ok | error`; "Check again" returns to `loading`.

## RuntimeConfig (`/config.json`)

| Field | Type | Notes |
|---|---|---|
| apiUrl | string | required, absolute http(s) URL, no trailing slash |
| devStub | boolean | optional, default false; true only in local development (R10) |

State: `loading -> ready | failed`. `failed` shows the configuration error page.

## Environment (operational, not stored in code)

| Name | Sign-in mode | Migrates at startup | Reachable by |
|---|---|---|---|
| Development (local) | Dev | yes, with demo seed | owner machine |
| Demo (Azure) | Dev | no | owner only (access restriction) |
