import { Role } from '../auth/models';

export interface NavItem {
  label: string;
  /** Route path without a leading slash. */
  path: string;
  /** The person needs ANY of these roles to see the link and open the route. */
  roles: readonly Role[];
}

const EVERYONE: readonly Role[] = ['Employee', 'Manager', 'HR', 'Admin'];

/**
 * The single table behind both the top bar links and the route guard, so they cannot drift.
 * Source: Phase 3a "Figma-ready spec, Navigation"; see contracts/navigation.md.
 * Order is the display order. Audit is listed once and carries both roles that may see it.
 * Hiding a link is a convenience only; the server decides (security rule 1).
 */
export const NAV_ITEMS: readonly NavItem[] = [
  { label: 'Dashboard', path: 'dashboard', roles: EVERYONE },
  { label: 'My requests', path: 'requests', roles: EVERYONE },
  { label: 'Approvals', path: 'approvals', roles: ['Manager'] },
  { label: 'Reports', path: 'reports', roles: ['HR'] },
  { label: 'Exceptions', path: 'exceptions', roles: ['HR'] },
  { label: 'Leave types', path: 'leave-types', roles: ['Admin'] },
  { label: 'Holidays', path: 'holidays', roles: ['Admin'] },
  { label: 'Audit', path: 'audit', roles: ['HR', 'Admin'] },
];

/** Items a person with these roles may see. Each path appears at most once. */
export function visibleItems(roles: readonly Role[]): NavItem[] {
  return NAV_ITEMS.filter((item) => item.roles.some((role) => roles.includes(role)));
}

/** Whether a person with these roles may open the route with this path. */
export function canOpen(path: string, roles: readonly Role[]): boolean {
  const item = NAV_ITEMS.find((candidate) => candidate.path === path);
  return item !== undefined && item.roles.some((role) => roles.includes(role));
}
