# Security rules (from the 3g design)

These apply to every feature. A review fails if one is broken.

1. **The server decides.** The Angular app hides buttons for convenience only. Every rule is enforced in the API.
2. **Deny by default.** A fallback policy requires a signed-in user on every endpoint. Public endpoints say `AllowAnonymous` on purpose (`/health`, the dev sign-in endpoints).
3. **Two layers of authorization.** Role policies decide which endpoints you may call. Resource rules decide which records you may touch ("your direct reports", "not your own request"). Roles alone are not enough.
4. **No IDOR.** Never trust an id from the client. Check the caller's right to that record. Return 404, not 403, for a record the caller may not see, so ids cannot be probed.
5. **One switch for auth.** Dev mode and Entra ID mode produce the same claims (`sub`, `name`, `email`, `role`). Business code never reads `Auth:Mode`.
6. **Dev mode is dev only.** The API refuses to start with `Auth:Mode = Dev` outside Development. The dev endpoints exist only in Dev mode.
7. **Tokens.** Short lifetime (60 minutes, no refresh in Dev). In the browser, the token lives in memory only. Never log tokens or full request bodies. Never put the manager or other changeable facts in the token; look them up.
8. **Validation.** Separate DTOs from entities so a client cannot set `Status`, `Role` or ids (over-posting). Validate dates, half days and overlaps on the server.
9. **Database safety.** EF Core parameterized queries only. Raw SQL only if parameterized. Constraints stay in the database as the second line of defence.
10. **Errors.** `ProblemDetails` responses with no stack traces or SQL text. Details go to logs.
11. **Secrets.** Never in the repo or an image. Local: user-secrets or environment variables. CI: GitHub Actions secrets. The Dev signing key in `appsettings.Development.json` is a throwaway value, not a secret.
12. **Personal data.** Store name, email, role only. Audit and outbox rows hold ids and values, never names, emails or free-text reasons.
13. **Browser.** CORS allows exactly one origin. Security headers on every response. No `innerHTML` with user text in Angular.
14. **Audit.** Every apply, approve, reject, cancel and balance change writes an append-only audit row in the same transaction.

Known later work: BFF with HttpOnly cookies (production hardening), per-user rate limit on the whole API, insert-only audit permissions and trigger (4e), Entra ID with PKCE (4g).
