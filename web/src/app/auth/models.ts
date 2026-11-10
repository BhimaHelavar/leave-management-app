export const ROLES = ['Employee', 'Manager', 'HR', 'Admin'] as const;

export type Role = (typeof ROLES)[number];

/** The signed-in person as the shell sees it. Read-only; never holds a token. */
export interface Person {
  id: string;
  name: string;
  roles: readonly Role[];
}

/**
 * Keeps only known roles and always includes Employee (everyone is an Employee).
 * An empty or unknown list therefore becomes `['Employee']`.
 */
export function normalizeRoles(raw: readonly string[]): Role[] {
  const known = ROLES.filter((role) => raw.includes(role));
  return known.includes('Employee') ? known : ['Employee', ...known];
}
