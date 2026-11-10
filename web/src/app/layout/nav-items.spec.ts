import { Role } from '../auth/models';
import { NAV_ITEMS, canOpen, visibleItems } from './nav-items';

const labels = (roles: readonly Role[]) => visibleItems(roles).map((item) => item.label);

describe('visibleItems', () => {
  it('shows Employee links only to an Employee', () => {
    expect(labels(['Employee'])).toEqual(['Dashboard', 'My requests']);
  });

  it('adds Approvals for a Manager', () => {
    expect(labels(['Employee', 'Manager'])).toEqual(['Dashboard', 'My requests', 'Approvals']);
  });

  it('adds Reports, Exceptions and Audit for HR', () => {
    expect(labels(['Employee', 'HR'])).toEqual([
      'Dashboard',
      'My requests',
      'Reports',
      'Exceptions',
      'Audit',
    ]);
  });

  it('adds Leave types, Holidays and Audit for an Admin', () => {
    expect(labels(['Employee', 'Admin'])).toEqual([
      'Dashboard',
      'My requests',
      'Leave types',
      'Holidays',
      'Audit',
    ]);
  });

  it('shows Audit once for a person who is both HR and Admin', () => {
    const result = labels(['Employee', 'Manager', 'HR', 'Admin']);

    expect(result.filter((label) => label === 'Audit').length).toBe(1);
    expect(result.length).toBe(NAV_ITEMS.length);
  });

  it('shows nothing when there are no roles', () => {
    expect(visibleItems([])).toEqual([]);
  });
});

describe('canOpen', () => {
  it('allows a role listed for the path', () => {
    expect(canOpen('approvals', ['Employee', 'Manager'])).toBe(true);
    expect(canOpen('audit', ['Employee', 'Admin'])).toBe(true);
  });

  it('blocks a role that is not listed for the path', () => {
    expect(canOpen('approvals', ['Employee'])).toBe(false);
    expect(canOpen('leave-types', ['Employee', 'HR'])).toBe(false);
  });

  it('blocks a path that is not in the table', () => {
    expect(canOpen('secret', ['Employee', 'Manager', 'HR', 'Admin'])).toBe(false);
  });
});
