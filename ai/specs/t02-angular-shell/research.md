# Research: Application Shell, Health Check and First Deployment

Date: 2026-11-02. Task T003 checked the Azure free-tier facts below against Microsoft Learn and pricing pages
on 2026-11-02. Each item is marked **Verified** (read on a Microsoft page that day), **Not confirmed** (the page was silent
or unclear) or **Test at creation** (only the Azure portal or CLI can answer it). Region: the owner asked for a region near
Bengaluru and chose **South India (Chennai)** first; fallback Central India (Pune), then Southeast Asia (Singapore).
Versions marked verified were read from the npm registry on 2026-11-02.

## R1. UI library
- **Decision**: Angular Material 22.2.5 with CDK 22.2.5 (verified: peer range `@angular/core ^22 || ^23`, and the repo has core 22.2.x).
- **Rationale**: chosen in Phase 3a; accessible by default; matches WCAG 2.1 AA goal.
- **Alternatives**: plain CSS (more own accessibility work); PrimeNG or Tailwind UI (not in the design, more packages).
- **Gate**: new npm packages need owner approval (CLAUDE.md). Theming API changed between versions, so T005 reads the v22 theming docs first.

## R2. Where the web app is hosted
- **Decision**: Azure Static Web Apps, Free plan. Deploy the built `dist/web/browser` folder.
- **Rationale**: free, global static hosting; `staticwebapp.config.json` gives SPA fallback and response headers, replacing `nginx.conf`.
- **Alternatives**: the existing nginx container on App Service (needs a container registry and paid always-on); Azure Storage static site (no built-in header control).
- **Verified (T003)**: Free plan exists (for personal projects, no SLA): 100 GB bandwidth per month with no overage on Free; 250 MB per environment and 500 MB in total; 15,000 files; 2 custom domains; 3 preview environments; free SSL certificate. A built Angular app with Material is well under 250 MB.
- **Verified, consequence**: "Allowed IP range restrictions" are **unavailable on Free** (Standard only). The static shell therefore cannot be IP-restricted for free; see R7.
- **Not confirmed**: which Azure regions can host a Static Web App. The content is served globally, so the region matters little; the portal lists the choices at creation.

## R3. Where the API is hosted
- **Decision**: Azure App Service, Free (F1) Linux plan, .NET 10 runtime.
- **Rationale**: free; no container registry needed; app settings give configuration without files.
- **Alternatives**: Azure Container Apps (free grant, more setup); Functions (wrong shape for minimal API).
- **Verified (T003)**: F1 is shared compute with **60 CPU minutes per day**, 1 GB RAM, 1 GB storage, no SLA, and is not meant for production use (Azure pricing page). When the daily CPU quota is used up the app is stopped until the quota resets (stated from general knowledge; the pricing page does not say so). The shell's error state (spec US2) already covers a stopped or cold API.
- **Not confirmed**: the pricing page does not say whether F1 offers always-on (assume not) and says nothing about .NET 10. The page also says free and shared plans "do not support SSL"; this most likely means custom-domain SSL, and the default `*.azurewebsites.net` address normally serves HTTPS. Check at creation that the API address answers on HTTPS.
- **Not confirmed, real risk**: .NET 10 on App Service **Linux**. A search found no official confirmation, and one undated forum post reported only .NET 8 on Linux. Check first with `az webapp list-runtimes --os-type linux` and look for a .NET 10 entry. If it is missing, the options are (a) a Windows plan (may not offer F1 in the chosen region, check), (b) publishing the API self-contained so it does not need the platform's .NET 10, or (c) a custom container (needs a registry, which is not free). Decide at T021 with the CLI output, and record it here.
- **Test at creation**: whether F1 can be created in Central India or South India. Run `az appservice plan create --sku F1 --is-linux --location southindia ...` and fall back to `centralindia`, then `southeastasia`. A new or trial subscription may show a quota of 0 for the free SKU; the fix is a quota request in the portal.

## R4. Database
- **Decision**: Azure SQL Database free offer (serverless General Purpose, free for the life of the subscription).
- **Rationale**: same engine as LocalDB, so the existing EF Core migrations apply unchanged.
- **Migrations**: the app does not migrate outside Development (3b decision). Use `dotnet ef migrations script --idempotent`, read it, and apply it as a manual step. Demo seed data is a separate manual step, see research R7.
- **Alternatives**: SQL Server in a container (cost); SQLite (different engine, schema differs).
- **Verified (T003, Microsoft Learn page updated 2026-10-28)**: per database each month 100,000 vCore seconds, 32 GB data and 32 GB backup storage free; up to 10 free databases per subscription (my earlier note of "one" was wrong); available on most subscription types (not Azure for Students Starter); **no SLA**; it **auto-pauses after 15 minutes of inactivity** (fixed, cannot be changed), so the first call after idle is slow and may show "Degraded" in the shell, which is acceptable.
- **Verified, owner decision**: when the monthly free amount runs out choose `AutoPause` (database unavailable until next month, no charge) or `BillOverUsage` (stays online, pays standard rates, and **cannot be switched back**). Use `AutoPause`, so free means free. The command uses `--use-free-limit true --free-limit-exhaustion-behavior AutoPause`; without `--use-free-limit` the database is billable.
- **Verified, caution**: leaving SSMS (including Object Explorer) connected prevents auto-pause and burns the free seconds. Disconnect when done. With "Advanced configuration" the region of the first free database applies to all free databases in the subscription and cannot be changed, so pick the region deliberately.
- **Test at creation**: that South India (or the fallback) offers the free offer. Choose the same region as the API when possible.

## R5. Cost alert
- **Decision**: Azure Cost Management budget on the subscription with email notification at 50% and 100% of a small monthly budget (proposed: 5 USD, owner confirms in T003).
- **Rationale**: free, email only, matches the owner's choice. Budgets alert, they do not stop spending, so the free-tier SKUs are the real guard.
- **Alternatives**: Action Group with SMS (cost, not asked); spending limit (not available on all subscription types).
- **Verified (T003)**: budgets are free; up to 5 thresholds and 5 email addresses; thresholds can be on actual or forecasted cost (range 0.01% to 1000%); budgets are evaluated against cost data **every 24 hours** (data takes 8 to 24 hours to appear) and emails normally arrive within an hour of an evaluation; a **new subscription can take up to 48 hours** before budgets can be created; alerts do not stop spending; add `azure-noreply@microsoft.com` to approved senders so alerts avoid junk mail.
- **Correction of the earlier test idea**: a 0% threshold budget cannot be triggered while the real spend is 0 (free tier), and evaluation is only daily. For SC-005 use an **action group** on the budget and the action group's "Test" feature in Azure Monitor to send a test email at once (Not confirmed here; check in the portal at T027). A real alert will only ever fire if something starts costing money.
- **Owner asked for a free daily spending limit if one exists.** Finding: a budget only alerts; it never stops spending (Microsoft Learn: "Resources aren't affected, and your consumption isn't stopped"), and I found no daily limit on a pay-as-you-go subscription. (A hard "spending limit" exists only on credit-based subscriptions such as a free trial; not checked here.) The free tiers carry their own caps, which are the real guard: F1 stops the app at 60 CPU minutes per day, the free SQL database uses `AutoPause` at the monthly limit, and Static Web Apps Free has no overage billing. A stop-on-budget automation (action group + function) is possible but adds moving parts and is out of scope.
- **Amount (assumed, owner may change)**: because nothing can be capped, use the smallest practical alert: a **1 USD per month** budget, email on actual cost at 50% and 100%, plus a forecast alert. Free-tier usage should show 0 USD, so any alert means something is billing and needs attention.

## R6. Runtime configuration (API URL, one build for all environments)
- **Decision**: the app fetches `/config.json` at startup (before first render) and stores `apiUrl` in a typed object that `api-config.ts` exposes. Local file points to `http://localhost:5000`; the deploy job writes the Azure API URL.
- **Rationale**: one artifact for every environment; no secret involved (the URL is public).
- **Alternatives**: Angular `environment.ts` files (one build per environment); inject into `index.html` at deploy (more fragile).
- **Failure case**: if `config.json` fails to load, show a clear "configuration could not be loaded" page rather than a blank screen.

## R7. Dev sign-in on a deployed environment (resolves the rule 6 conflict)
- **Decision (owner chose)**: introduce an explicit `Demo` environment name. Rule becomes: `Auth:Mode=Dev` is allowed only when the environment is `Development` or `Demo`; otherwise the API refuses to start. `Demo` does not migrate or seed at startup. Demo data is loaded once by a manual step.
- **Access**: restricted to the owner (owner choice). Plan: App Service access restriction to the owner's IP range for the API. Verified (T003): IP restrictions on Static Web Apps need the Standard plan (paid), so on the free plan the static shell stays public but is empty without the API (it only shows an error card). This is the accepted trade-off. App Service access restrictions themselves are not on the F1 pricing page; Test at creation that the F1 plan allows them.
- **Rationale**: keeps the safety net of rule 6 (no accidental Dev mode in a real environment) while allowing a demo.
- **Alternatives**: run as `Development` (loses the guard); skip deployed sign-in (owner declined).
- **Governance**: editing `ai/steering/security.md` rule 6 and the constitution (Principle II text and version 1.1.0) is task T001 and needs owner approval of the exact wording.

## R8. CORS and security headers on Azure
- **Decision**: API `Cors:AllowedOrigin` is set by an App Service setting to the Static Web Apps origin (already read from config in `Program.cs`). Web headers (CSP, nosniff, frame deny, no-referrer) move to `staticwebapp.config.json`, with `connect-src 'self'` plus the API origin.
- **Alternatives**: wildcard CORS (forbidden by rule 13).
- **Note**: the CSP currently hard-codes `localhost:5000` in `nginx.conf`; the local container keeps that file unchanged.

## R9. Routes and guards
- **Decision**: one navigation table (`nav-items.ts`) drives both the top bar links and the route role check, so the two cannot drift. A `CanActivateFn` sends a person without the role to the not-found view (S-12 "Back to dashboard"). Unknown address also goes to S-12.
- **Rationale**: single source of truth for the Phase 3a role table; hiding is UX only (rule 1).

## R10. The identity seam until 4c exists
- **Decision**: `AuthService` exposes `person` (id, name, roles) as signals. Until 4c is built it returns a stub person chosen by a local-only setting so all four role layouts can be seen. 4c replaces the stub with the real `/api/me` result without changing the shell.
- **Rationale**: lets this spec ship and be tested independently of 4c.
- **Risk**: the stub must not exist in the deployed build. It is only active when `config.json` says `devStub: true`, and the deployed `config.json` does not (a test asserts this).

## R11. Accessibility checks
- **Decision**: Material defaults plus: skip-to-content link, visible focus, labelled user menu, `aria-live` on the health card, `lang` on the page. Verify with a manual keyboard pass and a contrast check of the Phase 3a colors (the 3a checklist leaves the contrast check unticked).
- **Contrast results (T005, 2026-11-02, WCAG relative-luminance formula, computed with a small Node script)**: all Phase 3a pairs meet AA (4.5:1 for text).

  | Pair | Ratio |
  |---|---|
  | Primary `#1F4E79` on white (links), and white on primary (buttons, header) | 8.66:1 |
  | Success `#1B6E3C` on white, and white on success | 6.28:1 |
  | Warning text `#7A4B00` on `#FFF3D6` | 6.72:1 |
  | Error `#B3261E` on white, and white on error | 6.54:1 |
  | Neutral `#5F6368` on white | 6.05:1 |

  Not yet checked: borders and focus outlines (3:1 needed) and any other surface color Material derives. Re-check when real screens exist. The 3a checklist item "Colour pairs checked" can be ticked for these five pairs.
- **Theme decision (T005)**: Material 22 uses M3 tokens. The base is the built-in azure palette with `primary`, `on-primary`, `error` and `on-error` overridden by the Phase 3a colors. Limit: Material derives its other tones (for example primary containers and tonal buttons) from the azure palette, not from `#1F4E79`, so they are a slightly different blue. If that matters, generate a palette from `#1F4E79` with the Material theme-color schematic (a later, optional change).
- **Font decision (T005)**: Roboto is named but not downloaded, because the web server's Content-Security-Policy allows only its own origin. Devices without Roboto use the system sans-serif font. Self-hosting Roboto needs the `@fontsource/roboto` package (owner approval) or a font file copied into `web\public`.
- **Alternatives**: axe in Playwright (Phase 3a mentions it; Playwright is a new package, deferred to Phase 5).
