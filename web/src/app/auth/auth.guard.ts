import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { LOGIN_PATH } from './auth.interceptor';
import { AuthService } from './auth.service';
import { RETURN_URL_PARAM } from './return-url';

/**
 * A person with no session goes to sign-in and the wanted address travels along, so a link such as
 * /approvals/42 comes back to the same page after sign-in. The address holds no token; the sign-in page
 * checks it before using it. A convenience for the screen; the API enforces access too.
 */
export const authGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  if (auth.isSignedIn()) {
    return true;
  }

  const router = inject(Router);
  const login = router.parseUrl(LOGIN_PATH);
  if (state.url && state.url !== '/' && !state.url.startsWith(LOGIN_PATH)) {
    login.queryParams = { [RETURN_URL_PARAM]: state.url };
  }
  return login;
};
