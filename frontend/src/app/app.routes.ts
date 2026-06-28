import { Routes } from '@angular/router';

/**
 * Router skeleton for Story 1.1. Only `/sign-up` (unguarded) and a minimal
 * authenticated `/dashboard` landing stub exist. The guard system (1.4), app
 * shell (1.5), and full dashboard (1.6) are out of scope — all routes except
 * `/sign-up` and `/sign-in` will be guarded later. [Decision 1.6, AR13]
 */
export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'sign-up' },
  {
    path: 'sign-up',
    loadComponent: () =>
      import('@pages/auth-sign-up/sign-up.page').then((m) => m.SignUpPage),
  },
  {
    path: 'dashboard',
    loadComponent: () =>
      import('@pages/dashboard/dashboard.page').then((m) => m.DashboardPage),
  },
  { path: '**', redirectTo: 'sign-up' },
];
