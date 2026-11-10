# Implementation Plan: Application Shell, Health Check and First Deployment

**Branch**: `n/a` (work on `main`) | **Date**: 2026-11-02 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `ai/specs/t02-angular-shell/spec.md` (Jira SCRUM-26)

## Summary

Turn the 4a scaffold (a single health card) into a real app shell: Angular Material theme, routing, a top bar with
role-based placeholder links, a not-found view, and the health card moved into the shell. Make the API URL and
sign-in mode configuration-driven, add an explicit `Demo` environment so dev sign-in can run on one restricted Azure
environment, and deploy it (Static Web Apps for the web app, App Service for the API, Azure SQL free database, budget
email alert). Sign-in itself stays in spec 4c; the shell consumes it through one `AuthService` seam and uses a stub
person until 4c lands.

Wireframes: [app shell](../../design/wireframes/app-shell.md), [not found](../../design/wireframes/not-found.md).

## Technical Context

**Language/Version**: TypeScript 6.0, Angular 22.2 (standalone, signals, zoneless); C# 14 / .NET 10 for the small API changes

**Primary Dependencies**: existing Angular packages; NEW `@angular/material` and `@angular/cdk` (22.2.5, same major as core; owner approval required, see research R1); no new NuGet packages

**Storage**: Azure SQL Database free offer (existing EF Core 10 schema); no schema change in this feature

**Testing**: Vitest through `ng test` with `HttpTestingController` for the web app; manual API checks for the Demo-environment guard (the back-end test project arrives in Phase 5)

**Target Platform**: browsers (desktop and phone); Azure free tier (Static Web Apps, App Service, SQL Database)

**Project Type**: web application (Angular front end + .NET API, existing layout)

**Performance Goals**: shell visible within 3 s on a normal connection (spec SC-001); no budget beyond that

**Constraints**: free tier only; budget email alert; WCAG 2.1 AA; no horizontal scroll at 320 px; no secrets in repo or web files

**Scale/Scope**: one deployed environment, a handful of demo users, 8 placeholder routes plus not-found

## Constitution Check

*GATE: passed with one governance item (see III row). Re-checked after Phase 1 design: unchanged.*

| Principle | Status | Notes |
|---|---|---|
| I. Server enforces every rule | Pass | Nav hiding is convenience only (FR-004). Route guards are UX; no data is shown yet. |
| II. Security by default | Pass with item | Deployed API is access-restricted (owner choice). The `/health` endpoint stays public. **Dev auth on Azure needs the `Demo` environment, which amends security rule 6** (task T001, owner approval, constitution MINOR bump). |
| III. Clean Architecture | Pass | API changes live in `Api` only (startup guard, CORS origin from config). No Domain or Application change. |
| IV. Dates and time | Note | Existing `/health` uses `DateTimeOffset.UtcNow`; golden rule says `TimeProvider`. Fixing it is a small in-scope change because the shell displays that value (task T007). |
| V. Migrations append-only | Pass | No migration. Deployed DB migrates through a separate step (see research R4). |
| VI. Spec-driven | Pass | Spec approved 2026-11-02. |
| VII. Verify before done | Pass | Quickstart lists what is run; deployment steps that cannot be run here are marked unverified. |
| VIII. Ask first | Pass | Material/CDK packages and the steering amendment are explicit approval tasks (T001, T002). |

## Project Structure

### Documentation (this feature)

```text
ai/specs/t02-angular-shell/
├── spec.md
├── plan.md              # This file
├── research.md          # Phase 0
├── data-model.md        # Phase 1
├── quickstart.md        # Phase 1
├── contracts/
│   ├── health-api.md
│   ├── navigation.md
│   └── runtime-config.md
├── checklists/requirements.md
└── tasks.md             # /speckit-tasks (not created here)
```

### Source Code (repository root)

```text
web/
├── public/config.json                  # runtime settings (apiUrl); replaced per environment at deploy
├── staticwebapp.config.json            # Azure Static Web Apps: fallback route + security headers
├── src/
│   ├── styles.scss                     # Material theme (Phase 3a tokens)
│   └── app/
│       ├── app.ts / app.html           # becomes <header> + <router-outlet> (health card moves out)
│       ├── app.routes.ts               # NEW: routes from contracts/navigation.md, not-found
│       ├── app.config.ts               # provideRouter, config loader (APP_INITIALIZER-style)
│       ├── api-config.ts               # reads the loaded config instead of a constant
│       ├── auth/
│       │   ├── auth.service.ts         # NEW seam: current person (name, roles) as signals; stub until 4c
│       │   └── role-guard.ts           # NEW CanActivateFn (UX only)
│       ├── layout/
│       │   ├── top-bar.ts/.html/.scss  # app name, role links, user menu
│       │   └── nav-items.ts            # NEW single table: link -> required roles
│       ├── features/
│       │   ├── placeholder/            # one reusable "coming soon" page
│       │   ├── health/                 # health card (moved from app.html), uses health.service.ts
│       │   └── not-found/              # S-12
│       └── health.service.ts           # existing, unchanged contract
src/LeaveManagement.Api/
├── Program.cs                          # Demo-environment guard (FR-008); health time via TimeProvider
.github/workflows/
└── deploy.yml                          # NEW, manual trigger only; uses GitHub Actions secrets
```

**Structure Decision**: keep the existing layout (`web/`, `src/LeaveManagement.*`). Follow `structure.md`: one folder per
screen under `features/`, no `.component` suffixes, `auth/` for the identity seam. One shared `placeholder` screen
instead of eight empty folders, because the real screens arrive in 4d to 4f.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| Runtime `config.json` for the API URL | One build must run locally and on Azure; the current constant is hard-coded to `localhost:5000` | Build-time environment files would need a separate build per environment and bake the API address into the bundle |
