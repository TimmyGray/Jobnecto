import { describe, expect, it, beforeEach } from 'vitest';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { httpInterceptor } from '@shared/api';
import { env } from '@shared/config';
import { UserService } from '@entities/user';
import { routes } from './app.routes';

/**
 * Exercises the *real* `routes` config end-to-end (not a synthetic
 * `provideRouter([...])` stand-in as `app-shell.spec.ts` uses) — proving the
 * shell actually renders its routed children in production wiring, and that
 * the guard on the shell parent still blocks an unauthenticated visitor.
 */
describe('routes (integration, real config)', () => {
  let httpMock: HttpTestingController;
  let userService: UserService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([httpInterceptor])),
        provideHttpClientTesting(),
        provideRouter(routes),
      ],
    });
    httpMock = TestBed.inject(HttpTestingController);
    userService = TestBed.inject(UserService);
  });

  function authenticate() {
    userService.fetchCurrentUser().subscribe();
    httpMock.expectOne(`${env.apiBaseUrl}/users/me`).flush({ id: 'u1', loginName: 'ada' });
  }

  it('renders the shell + real DashboardPage at /dashboard for an authenticated user', async () => {
    authenticate();
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/dashboard');
    harness.fixture.detectChanges();

    expect(
      harness.fixture.nativeElement.querySelector('nav[aria-label="Primary"]'),
    ).not.toBeNull();
    expect(harness.fixture.nativeElement.textContent).toContain('Welcome');
  });

  it('renders the shell + ComingSoonStubPage at /profile for an authenticated user', async () => {
    authenticate();
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/profile');
    harness.fixture.detectChanges();

    const text: string = harness.fixture.nativeElement.textContent;
    expect(text).toContain('Profile');
    expect(text).toContain('Coming soon');
  });

  it('redirects an unauthenticated visitor hitting /resumes to /sign-in with returnUrl preserved', async () => {
    const harness = await RouterTestingHarness.create();
    const navigation = harness.navigateByUrl('/resumes');
    await new Promise((resolve) => setTimeout(resolve, 0));

    httpMock
      .expectOne(`${env.apiBaseUrl}/users/token/refresh`)
      .flush({ title: 'Unauthorized', status: 401 }, { status: 401, statusText: 'Unauthorized' });
    await navigation;

    const router = TestBed.inject(Router);
    expect(router.url).toBe('/sign-in?returnUrl=%2Fresumes');
  });
});
