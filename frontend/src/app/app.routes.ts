import { Routes } from '@angular/router';
import { authGuard } from './auth.guard';

/**
 * Router skeleton. `/sign-up` and `/sign-in` are unguarded; every other route
 * carries `authGuard` (Story 1.4) — an unauthenticated visitor is redirected
 * to `/sign-in` with the attempted path preserved as `returnUrl`. The app
 * shell (1.5) and full dashboard (1.6) are still out of scope. [Decision 1.6, AR13]
 */
export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'sign-up' },
  {
    path: 'sign-up',
    loadComponent: () =>
      import('@pages/auth-sign-up/sign-up.page').then((m) => m.SignUpPage),
  },
  {
    path: 'sign-in',
    loadComponent: () =>
      import('@pages/auth-sign-in/sign-in.page').then((m) => m.SignInPage),
  },
  {
    path: 'dashboard',
    canActivate: [authGuard],
    loadComponent: () =>
      import('@pages/dashboard/dashboard.page').then((m) => m.DashboardPage),
  },
  { path: '**', redirectTo: 'sign-up' },
];
