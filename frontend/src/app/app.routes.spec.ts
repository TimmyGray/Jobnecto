import { describe, expect, it } from 'vitest';
import { routes } from './app.routes';
import { authGuard } from './auth.guard';

describe('routes', () => {
  it('guards the dashboard route with authGuard (Story 1.4 AC2)', () => {
    const dashboard = routes.find((r) => r.path === 'dashboard');
    expect(dashboard?.canActivate).toEqual([authGuard]);
  });

  it('leaves sign-up and sign-in unguarded', () => {
    for (const path of ['sign-up', 'sign-in']) {
      const route = routes.find((r) => r.path === path);
      expect(route?.canActivate).toBeUndefined();
    }
  });
});
