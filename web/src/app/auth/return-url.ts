/** Where a person lands after sign-in when no safe return address was given. */
export const DEFAULT_RETURN_URL = '/dashboard';

/** The query parameter that carries the wanted address to the sign-in page. */
export const RETURN_URL_PARAM = 'returnUrl';

/**
 * Accepts a return address only if it is a path inside this app, so a crafted sign-in link cannot send a
 * person to another site (open redirect). One leading `/`, no `//`, no scheme, no backslash and no control
 * character or space; anything else opens the dashboard. The sign-in page itself is never a return address.
 */
export function safeReturnUrl(value: string | null | undefined): string {
  if (
    typeof value !== 'string' ||
    !value.startsWith('/') ||
    value.startsWith('//') ||
    value.includes('\\') ||
    /[\u0000- \u007f]/.test(value) ||
    value === '/login' ||
    value.startsWith('/login?') ||
    value.startsWith('/login/')
  ) {
    return DEFAULT_RETURN_URL;
  }

  return value;
}
