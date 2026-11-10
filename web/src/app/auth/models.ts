export const ROLES = ['Employee', 'Manager', 'HR', 'Admin'] as const;

export type Role = (typeof ROLES)[number];

/** The signed-in person as the shell sees it. Read-only; never holds a token. */
export interface Person {
  id: string;
  name: string;
  roles: readonly Role[];
}

/** One entry of `GET /api/dev/users` (development sign-in only). */
export interface DevUser {
  id: number;
  fullName: string;
  email: string;
  roles: string[];
}

/** Response of `POST /api/dev/token`. */
export interface TokenResponse {
  accessToken: string;
  expiresAtUtc: string;
  user: { id: number; name: string; email: string; roles: string[] };
}

/**
 * Keeps only known roles and always includes Employee (everyone is an Employee).
 * An empty or unknown list therefore becomes `['Employee']`.
 */
export function normalizeRoles(raw: readonly string[]): Role[] {
  const known = ROLES.filter((role) => raw.includes(role));
  return known.includes('Employee') ? known : ['Employee', ...known];
}
