# Contract: runtime configuration

## Web: `/config.json` (served next to the app)

```json
{ "apiUrl": "http://localhost:5000", "devStub": true }
```

- `apiUrl`: required, absolute URL, no trailing slash. The HTTP interceptor (4c) adds the token only to calls under this URL.
- `devStub`: optional; true only in the local file. The deployed file omits it (a test checks that the deploy output has no `devStub: true`).
- Loaded once before the first render. A missing or invalid file shows a configuration error page.
- Contains no secret. It is public by design.

## API: settings (environment variables or App Service settings)

| Setting | Local | Azure Demo |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Development` | `Demo` |
| `Auth__Mode` | `Dev` | `Dev` |
| `Cors__AllowedOrigin` | `http://localhost:4200` | the Static Web Apps origin, exactly one |
| `ConnectionStrings__Default` | LocalDB | Azure SQL (App Service connection string, never in the repo) |

Startup rules: invalid `Auth__Mode` fails (existing). `Dev` outside `Development` or `Demo` fails (new, FR-008).

## Deploy secrets (GitHub Actions secrets, never in the repo)

Static Web Apps deployment token; App Service publish profile or federated credential; Azure SQL connection string is set in Azure, not in the workflow.
