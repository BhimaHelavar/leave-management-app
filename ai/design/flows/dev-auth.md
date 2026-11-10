# Dev sign-in flow (spec 4c)

Source: `ai/specs/4c-dev-authentication/design.md` (still a draft). Update both together.

```mermaid
sequenceDiagram
    actor U as Person
    participant B as Angular app
    participant A as API
    participant D as Database

    U->>B: open /login
    B->>A: GET /api/dev/users
    A->>D: read Employee, EmployeeRole
    D-->>A: active people + roles
    A-->>B: list of id, fullName, email, roles
    U->>B: pick a person
    B->>A: POST /api/dev/token {employeeId}
    A->>D: find active person
    A-->>B: accessToken (60 min), expiresAtUtc, user
    Note over B: token kept in memory (signal)
    B->>A: GET /api/me + Bearer token
    A->>A: validate signature, issuer, audience, expiry
    A-->>B: id, name, email, roles
    alt any call returns 401
        B->>B: clear session
        B->>U: go to /login
    end
```

## Token claims

`sub` (employee id), `name`, `email`, `role` (one per role), `iss`, `aud`, `iat`, `nbf`, `exp`. The manager is not in the token; it is looked up.

## Guard rails

- Dev endpoints exist only when `Auth:Mode = Dev`; the API refuses to start in Dev mode outside Development or Demo.
- A fallback policy requires a signed-in user on every endpoint except `/health` and the dev sign-in endpoints.
