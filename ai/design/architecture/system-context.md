# System context

Who and what talks to whom. Solid lines exist today; dashed lines are planned (increment 4g).

```mermaid
flowchart LR
    emp([Employee])
    mgr([Manager])
    hr([HR])
    adm([Admin])

    subgraph Browser
        web[Angular 22 app<br/>localhost:4200]
    end

    subgraph Server
        api[ASP.NET Core API<br/>localhost:5000]
        db[(SQL Server<br/>LocalDB)]
    end

    emp --> web
    mgr --> web
    hr --> web
    adm --> web
    web -- "HTTP + JWT bearer" --> api
    api -- "EF Core" --> db

    entra[[Entra ID]]
    redis[(Redis cache)]
    mq[[RabbitMQ]]
    api -. "4g: token validation" .-> entra
    api -. "4g" .-> redis
    api -. "4g: outbox publisher" .-> mq
```

## Notes

- Roles are additive: everyone is an Employee; a person may also be Manager, HR or Admin.
- Auth has one switch (`Auth:Mode` = `Dev` or `EntraId`). Business code never reads it.
- The browser holds the token in memory only.
- CORS allows exactly one origin (the Angular app).
