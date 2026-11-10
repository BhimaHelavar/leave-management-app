import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { API_URL } from '../api-config';
import { RuntimeConfigService } from '../config/runtime-config';
import { AuthService } from './auth.service';

export const LOGIN_PATH = '/login';

/**
 * Adds the bearer token to calls to our own API only (never to other sites, so the token cannot leak).
 * A 401 from the API means the token is missing, expired or invalid: clear the session and go to sign-in.
 */
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  // The request that loads config.json runs before the API URL is known. Reading API_URL then would
  // throw and show the "configuration could not be loaded" page, so let such requests pass untouched.
  if (inject(RuntimeConfigService).state() !== 'ready') {
    return next(request);
  }

  const apiUrl = inject(API_URL);
  if (!request.url.startsWith(`${apiUrl}/`)) {
    return next(request);
  }

  const auth = inject(AuthService);
  const router = inject(Router);
  const token = auth.accessToken();
  const outgoing = token
    ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : request;

  return next(outgoing).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse && error.status === 401 && token) {
        auth.signOut();
        void router.navigateByUrl(LOGIN_PATH);
      }
      return throwError(() => error);
    }),
  );
};
