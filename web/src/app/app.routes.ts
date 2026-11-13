import { Routes } from '@angular/router';
import { authGuard } from './auth/auth.guard';
import { roleGuard } from './auth/role-guard';
import { NAV_ITEMS } from './layout/nav-items';

const loadPlaceholder = () =>
  import('./features/placeholder/placeholder').then((m) => m.Placeholder);
const loadNotFound = () => import('./features/not-found/not-found').then((m) => m.NotFound);
const loadLogin = () => import('./features/login/login').then((m) => m.Login);
const loadDashboard = () => import('./features/dashboard/dashboard').then((m) => m.Dashboard);
const loadApplyLeave = () =>
  import('./features/apply-leave/apply-leave').then((m) => m.ApplyLeave);

/** The screen each navigation path opens. Paths not built yet open the placeholder. */
function loaderFor(path: string) {
  switch (path) {
    case 'dashboard':
      return loadDashboard;
    case 'apply-leave':
      return loadApplyLeave;
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
  { path: 'not-found', loadComponent: loadNotFound },
  { path: '**', loadComponent: loadNotFound },
];
