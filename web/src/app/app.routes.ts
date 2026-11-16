import { Routes } from '@angular/router';
import { authGuard } from './auth/auth.guard';
import { roleGuard } from './auth/role-guard';
import { NAV_ITEMS } from './layout/nav-items';

const loadPlaceholder = () =>
  import('./features/placeholder/placeholder').then((m) => m.Placeholder);
const loadNotFound = () => import('./features/not-found/not-found').then((m) => m.NotFound);
const loadLogin = () => import('./features/login/login').then((m) => m.Login);
const loadDashboard = () => import('./features/dashboard/dashboard').then((m) => m.Dashboard);
const loadApplyLeave = () => import('./features/apply-leave/apply-leave').then((m) => m.ApplyLeave);
const loadApprovals = () => import('./features/approvals/approvals').then((m) => m.Approvals);
const loadApprovalDetail = () =>
  import('./features/approvals/approval-detail').then((m) => m.ApprovalDetailPage);

/** The screen each navigation path opens. Paths not built yet open the placeholder. */
// Built so far: dashboard, apply-leave and approvals.
function loaderFor(path: string) {
  switch (path) {
    case 'dashboard':
      return loadDashboard;
    case 'apply-leave':
      return loadApplyLeave;
    case 'approvals':
      return loadApprovals;
    default:
      return loadPlaceholder;
  }
}

/**
 * Built from NAV_ITEMS so the links and the routes come from one table (contracts/navigation.md).
 * Every section except the dashboard and Apply for leave is a placeholder until 4d to 4f. The guards are a screen convenience only:
 * authGuard sends a signed-out person to /login, roleGuard hides sections the roles do not allow.
 */
export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
  { path: 'login', loadComponent: loadLogin },
  ...NAV_ITEMS.map((item) => ({
    path: item.path,
    canActivate: [authGuard, roleGuard],
    data: { title: item.label },
    loadComponent: loaderFor(item.path),
  })),
  {
    // The request page and its deep link. It borrows the Approvals roles (Manager) from the navigation table.
    path: 'approvals/:requestId',
    canActivate: [authGuard, roleGuard],
    data: { navPath: 'approvals', title: 'Request' },
    loadComponent: loadApprovalDetail,
  },
  { path: 'not-found', loadComponent: loadNotFound },
  { path: '**', loadComponent: loadNotFound },
];
