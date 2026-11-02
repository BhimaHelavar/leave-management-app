# Decisions

One line per decision, with the document that explains it. The full reasoning and the options considered live in those documents.

## Phase 3b (high-level design)

| # | Decision | Source |
| --- | --- | --- |
| 3 | SQL Server as the database | 3b |
| 4 | Outbox pattern for notifications | 3b |
| 7 | Leave days are `date`, timestamps are UTC | 3b |
| 1-8 | Other decisions: see the 3b document | 3b |

## Phase 3c (database design), ADR 9 to 17

| ADR | Decision |
| --- | --- |
| 9 | `int` or `bigint` identity primary keys, not GUIDs |
| 10 | Balance = immutable ledger plus a guarded balance row (`CHECK Available >= 0`) |
| 11 | Overlap rule: one row per working half-day with a unique index |
| 12 | Days are held at submit, restored on reject or cancel |
| 13 | `DaysCount` is a snapshot on the request |
| 14 | Enums stored as readable `varchar` plus a `CHECK` list |
| 15 | One database schema per module |
| 16 | No cascade deletes, no hard deletes |
| 17 | Audit protection: permissions and a trigger now, hash chain later |

## Phase 3d to 3f (API, class design, Angular design)

Their decisions are in the 3d, 3e and 3f documents. **TODO:** copy the one-line list here.

## Phase 3g (security design)

| Id | Decision |
| --- | --- |
| 3g-1 | JWT bearer validation in the API; `Auth:Mode` switches Dev and Entra ID |
| 3g-2 | Entra ID uses authorization code flow with PKCE via MSAL (increment 4g) |
| 3g-3 | Tokens in memory (Dev) and sessionStorage (Entra ID); BFF with HttpOnly cookies is the production upgrade |
| 3g-4 | Policy-based plus resource-based authorization on the server |
| 3g-5 | Append-only audit log in SQL |

**Numbering note:** the 3g document labels these "ADR-9" to "ADR-13", which collides with the 3c numbers above. Here they are called 3g-1 to 3g-5. Rename them in the 3g document when convenient.

## How to add a decision

Add a row here and a short "Decision" section in the spec's `design.md`: context, options, choice, why, trade-off, when to revisit.
