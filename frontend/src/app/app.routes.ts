import { Routes } from '@angular/router';
import { authGuard } from './auth.guard';

/**
 * Router skeleton. `/sign-up` and `/sign-in` are unguarded; every other
 * route (AR13) is a child of the shell parent route below, which carries
 * `authGuard` once for all of them — an unauthenticated visitor is
 * redirected to `/sign-in` with the attempted path preserved as `returnUrl`
 * (Story 1.4). The shell itself (Story 1.5) renders the sidebar/off-canvas
 * nav around whichever child route is active. Routes whose real page is
 * still backlog (Epics 2-6) point at `ComingSoonStubPage` so no nav
 * destination is a dead end (`prd-demo-mvp.md` §Success Criteria item 6).
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
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('@widgets/app-shell').then((m) => m.AppShellComponent),
    children: [
      {
        path: 'dashboard',
        loadComponent: () =>
          import('@pages/dashboard/dashboard.page').then((m) => m.DashboardPage),
      },
      {
        path: 'profile',
        loadComponent: () =>
          import('@pages/coming-soon-stub/coming-soon-stub.page').then(
            (m) => m.ComingSoonStubPage,
          ),
        data: { title: 'Profile' },
      },
      {
        path: 'resumes',
        loadComponent: () =>
          import('@pages/coming-soon-stub/coming-soon-stub.page').then(
            (m) => m.ComingSoonStubPage,
          ),
        data: { title: 'Resumes' },
      },
      {
        path: 'resumes/:id',
        loadComponent: () =>
          import('@pages/coming-soon-stub/coming-soon-stub.page').then(
            (m) => m.ComingSoonStubPage,
          ),
        data: { title: 'Resumes detail' },
      },
      {
        path: 'educations',
        loadComponent: () =>
          import('@pages/coming-soon-stub/coming-soon-stub.page').then(
            (m) => m.ComingSoonStubPage,
          ),
        data: { title: 'Education' },
      },
      {
        path: 'educations/:id',
        loadComponent: () =>
          import('@pages/coming-soon-stub/coming-soon-stub.page').then(
            (m) => m.ComingSoonStubPage,
          ),
        data: { title: 'Education detail' },
      },
      {
        path: 'vacancies',
        loadComponent: () =>
          import('@pages/coming-soon-stub/coming-soon-stub.page').then(
            (m) => m.ComingSoonStubPage,
          ),
        data: { title: 'Vacancies' },
      },
      {
        path: 'vacancies/:id',
        loadComponent: () =>
          import('@pages/coming-soon-stub/coming-soon-stub.page').then(
            (m) => m.ComingSoonStubPage,
          ),
        data: { title: 'Vacancies detail' },
      },
      {
        path: 'cover-letters',
        loadComponent: () =>
          import('@pages/coming-soon-stub/coming-soon-stub.page').then(
            (m) => m.ComingSoonStubPage,
          ),
        data: { title: 'Cover Letters' },
      },
      {
        path: 'cover-letters/:id',
        loadComponent: () =>
          import('@pages/coming-soon-stub/coming-soon-stub.page').then(
            (m) => m.ComingSoonStubPage,
          ),
        data: { title: 'Cover Letters detail' },
      },
      {
        path: 'settings',
        loadComponent: () =>
          import('@pages/coming-soon-stub/coming-soon-stub.page').then(
            (m) => m.ComingSoonStubPage,
          ),
        data: { title: 'Settings' },
      },
    ],
  },
  { path: '**', redirectTo: 'sign-up' },
];
