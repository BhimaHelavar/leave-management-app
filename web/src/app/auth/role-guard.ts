import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { canOpen } from '../layout/nav-items';
import { AuthService } from './auth.service';

/** Unknown paths and paths a person may not open both land here (screen S-12). */
export const NOT_FOUND_PATH = '/not-found';

/**
 * A route is allowed only if its path is in the navigation table and the person holds one of its roles.
 * Anything else, including a person with no session, goes to the not-found view, which does not say
 * whether the page exists. This is a convenience for the screen; the API enforces the same rules.
 */
export const roleGuard: CanActivateFn = (route) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const path = route.routeConfig?.path ?? '';
  return canOpen(path, auth.roles()) ? true : router.parseUrl(NOT_FOUND_PATH);
};
