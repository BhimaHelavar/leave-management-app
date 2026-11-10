import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { LOGIN_PATH } from './auth.interceptor';
import { AuthService } from './auth.service';

/** A person with no session goes to sign-in. A convenience for the screen; the API enforces it too. */
export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  return auth.isSignedIn() ? true : inject(Router).parseUrl(LOGIN_PATH);
};
