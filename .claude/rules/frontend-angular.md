---
paths:
  - "web/src/**/*.ts"
  - "web/src/**/*.html"
  - "web/src/**/*.scss"
---

# Front-end rules (Angular 22)

- Standalone components only. State is held in signals; derive with `computed`. The app is zoneless, so a change must go through a signal to update the screen.
- Use `inject()`, not constructor injection. Use the new template control flow (`@if`, `@for` with `track`, `@switch`), never `*ngIf` or `*ngFor`.
- File names have no `.component` suffix: `login.ts`, `login.html`, `login.scss`, `login.spec.ts`. The class name is `Login`.
- Guards and interceptors are functions (`CanActivateFn`, `HttpInterceptorFn`) registered in `app.config.ts` and `app.routes.ts`.
- All HTTP goes through typed services in the feature or `auth` folder. Components do not call `HttpClient` directly. No `any`.
- The API base URL comes from `api-config.ts`. The interceptor only adds the token to calls to that URL.
- The access token lives in memory (a signal in `AuthService`) only. Never put it in `localStorage`, `sessionStorage` or a cookie in Dev mode.
- Hiding a button is not security. The API must enforce the same rule.
- Never use `innerHTML` or `bypassSecurityTrust*` with user text.
- Tests use Vitest with `HttpTestingController`. Each component or service gets a spec that covers the success path and one failure path. Run `npm test -- --watch=false` and `npm run build` before saying a task is done.
- Make screens usable: labels on inputs, buttons for actions, visible loading and error states.
- No new npm package without asking the owner.
