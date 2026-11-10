# Research: Sign in and see only what my role allows

No open "NEEDS CLARIFICATION" remain. Each decision below states the choice, why, and what was rejected.

## D1: Where the 403 and the audit happen

- **Decision**: One `IAuthorizationMiddlewareResultHandler` in Api. When the authorization result is `Forbidden` (signed in, wrong role) it writes the audit row, then returns a 403 `ProblemDetails`. All other results go to the framework's default handler.
- **Rationale**: Every endpoint protected by a named policy is covered automatically, including ones added in 4d to 4f. Nobody has to remember an audit call. It runs in the same pipeline step that decides the 403.
- **Alternatives rejected**: (a) audit inside each endpoint, easy to forget and breaks "deny by default"; (b) a general middleware that inspects status codes after the fact, it cannot tell a role refusal from a resource-rule 403 and sees no policy name; (c) an MVC filter, minimal APIs do not use them.

## D2: 401 versus 403 and the audit

- **Decision**: Not signed in or bad token: 401, **not audited** (no known actor; noisy). Signed in but missing role: 403, audited. A record the person may not see: 404, as security rule 4 says (handled later by resource rules).
- **Rationale**: Matches SCRUM-8 ("Employee opens /admin, gets 403, audited") and the existing rules.
- **Alternatives rejected**: auditing 401s (anonymous noise, a way to fill the table).

## D3: What the audit row contains

- **Decision**: `Action = AccessDenied`, `EntityType = Endpoint`, `EntityId = route pattern` (for example `/api/admin/ping`, not the raw URL), `ActorEmployeeId` from the `sub` claim, `DetailJson = {"policy":"RequireAdmin"}`, `CorrelationId` = trace id.
- **Rationale**: ids and values only (rule 12). Using the route pattern avoids storing query strings or ids typed by the client.
- **Alternatives rejected**: storing the full URL or roles list (may carry free text; roles can be looked up).

## D4: How the row is written

- **Decision**: `IAuditWriter.WriteAsync(AuditRecord, CancellationToken)` in Application; Infrastructure inserts with its own `SaveChanges`. Time from `TimeProvider`. If the insert fails, log the id and still return the 403.
- **Rationale**: A refusal has no business transaction to join. Later business audit rows (4e) will use the same writer inside their own transaction by passing the same `DbContext` scope.
- **Alternatives rejected**: failing the request when audit fails (turns an audit outage into a lock-out); logging only (not the append-only record the rules require).
- **Known gap**: insert-only database permissions and the trigger are 4e work (per `security.md`), so today the table is append-only by convention only.

## D5: An Admin-only endpoint to prove the rule

- **Decision**: Add `GET /api/admin/ping` protected by `RequireAdmin`, returning 204. It is a probe until real Admin features (leave types, holidays) exist, then it can be removed.
- **Rationale**: SCRUM-8 says "open /admin"; the API has no admin endpoint yet, and the shell has no `/admin` path. Without a probe there is nothing to check the 403 against.
- **Alternatives rejected**: test-only endpoint (needs the Phase 5 test project); waiting for 4f (acceptance criterion could not be shown now).

## D6: Navigation

- **Decision**: No new web code. T-02 already derives links from the roles in `/api/me` using the table in `ai/specs/t02-angular-shell/contracts/navigation.md`. This spec only adds missing role cases to the existing unit test and checks them by hand.
- **Rationale**: Avoids two sources for the role-to-link table.

## D7: Dependencies and order

- **Decision**: Implement 4c first, merge T-02, then this spec. If 4c is delayed, tasks for D1 to D5 cannot start because `Policies` and the JWT setup come from 4c.
