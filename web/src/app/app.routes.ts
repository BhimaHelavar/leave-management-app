import { Routes } from '@angular/router';
import { roleGuard } from './auth/role-guard';
import { NAV_ITEMS } from './layout/nav-items';

const loadPlaceholder = () => import('./features/placeholder/placeholder').then((m) => m.Placeholder);
const loadNotFound = () => import('./features/not-found/not-found').then((m) => m.NotFound);

/**
 * Built from NAV_ITEMS so the links and the routes come from one table (contracts/navigation.md).
 * Every section is a placeholder until 4d to 4f. The guard is a screen convenience only.
 */
export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
  ...NAV_ITEMS.map((item) => ({
    path: item.path,
    canActivate: [roleGuard],
    data: { title: item.label },
    loadComponent: loadPlaceholder,
  })),
  { path: 'not-found', loadComponent: loadNotFound },
  { path: '**', loadComponent: loadNotFound },
];
