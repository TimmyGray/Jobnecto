import { describe, expect, it } from 'vitest';
import { Route } from '@angular/router';
import { routes } from './app.routes';
import { authGuard } from './auth.guard';

const GUARDED_CHILD_PATHS = [
  'dashboard',
  'profile',
  'resumes',
  'resumes/:id',
  'educations',
  'educations/:id',
  'vacancies',
  'vacancies/:id',
  'cover-letters',
  'cover-letters/:id',
  'settings',
];

function findShellRoute(): Route {
  const shell = routes.find((r) => r.path === '' && r.children);
  if (!shell) {
    throw new Error('Expected a shell parent route with children');
  }
  return shell;
}

describe('routes', () => {
  it('leaves sign-up and sign-in unguarded and outside the shell', () => {
    for (const path of ['sign-up', 'sign-in']) {
      const route = routes.find((r) => r.path === path);
      expect(route?.canActivate).toBeUndefined();
    }
  });

  it('guards the shell parent route with authGuard (AR13, FR6)', () => {
    const shell = findShellRoute();
    expect(shell.canActivate).toEqual([authGuard]);
  });

  it('registers every AR13 guarded route as a child of the shell', () => {
    const shell = findShellRoute();
    const childPaths = shell.children?.map((c) => c.path);
    for (const path of GUARDED_CHILD_PATHS) {
      expect(childPaths).toContain(path);
    }
  });

  it('does not guard the shell children individually (the parent guard covers them)', () => {
    const shell = findShellRoute();
    for (const child of shell.children ?? []) {
      expect(child.canActivate).toBeUndefined();
    }
  });

  it('gives every non-dashboard child route a human-readable title in route data', () => {
    const shell = findShellRoute();
    const nonDashboard = shell.children?.filter((c) => c.path !== 'dashboard') ?? [];
    expect(nonDashboard.length).toBeGreaterThan(0);
    for (const child of nonDashboard) {
      expect(typeof child.data?.['title']).toBe('string');
      expect((child.data?.['title'] as string).length).toBeGreaterThan(0);
    }
  });

  it('still falls back unknown URLs to sign-up', () => {
    const wildcard = routes.find((r) => r.path === '**');
    expect(wildcard?.redirectTo).toBe('sign-up');
  });
});
