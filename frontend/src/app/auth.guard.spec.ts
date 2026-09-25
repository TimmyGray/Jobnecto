import { describe, expect, it, beforeEach } from 'vitest';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, Router, RouterStateSnapshot, UrlTree, provideRouter } from '@angular/router';
import { httpInterceptor } from '@shared/api';
import { env } from '@shared/config';
import { UserService } from '@entities/user';
import { authGuard } from './auth.guard';

describe('authGuard', () => {
  let httpMock: HttpTestingController;
  let userService: UserService;
  let router: Router;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([httpInterceptor])),
        provideHttpClientTesting(),
        provideRouter([]),
      ],
    });
    httpMock = TestBed.inject(HttpTestingController);
    userService = TestBed.inject(UserService);
    router = TestBed.inject(Router);
  });

  function runGuard(url = '/resumes') {
    const route = {} as ActivatedRouteSnapshot;
    const state = { url } as RouterStateSnapshot;
    return TestBed.runInInjectionContext(() => authGuard(route, state));
  }

  it('allows navigation synchronously when a profile is already cached, without any HTTP call (AC2)', () => {
    userService.fetchCurrentUser().subscribe();
    httpMock.expectOne(`${env.apiBaseUrl}/users/me`).flush({ id: 'u1', loginName: 'daria_dev' });
    httpMock.verify();

    const result = runGuard('/resumes');
    expect(result).toBe(true);
    httpMock.expectNone(`${env.apiBaseUrl}/users/token/refresh`);
  });

  it('restores the session on a cold check (no cached profile) and allows navigation on success (AC1, AC2)', async () => {
    const result = runGuard('/resumes');
    expect(result).not.toBe(true);
    const resolvedPromise = toPromise(result);

    httpMock
      .expectOne(`${env.apiBaseUrl}/users/token/refresh`)
      .flush({ accessToken: '', tokenType: 'Bearer', renewalPolicy: '...' });
    httpMock.expectOne(`${env.apiBaseUrl}/users/me`).flush({ id: 'u1', loginName: 'daria_dev' });

    const resolved = await resolvedPromise;
    expect(resolved).toBe(true);
    expect(userService.isAuthenticated()).toBe(true);
    httpMock.verify();
  });

  it('redirects to /sign-in with returnUrl set to the attempted route when restore fails (AC2)', async () => {
    const result = runGuard('/resumes');
    const resolvedPromise = toPromise(result);

    httpMock
      .expectOne(`${env.apiBaseUrl}/users/token/refresh`)
      .flush({ title: 'Unauthorized', status: 401 }, { status: 401, statusText: 'Unauthorized' });
    httpMock.expectNone(`${env.apiBaseUrl}/users/me`);

    const resolved = await resolvedPromise;
    expect(resolved).toBeInstanceOf(UrlTree);
    const tree = router.serializeUrl(resolved as UrlTree);
    expect(tree).toBe('/sign-in?returnUrl=%2Fresumes');
    httpMock.verify();
  });

  /** Normalizes the guard's boolean | Observable<boolean | UrlTree> return into a Promise. */
  async function toPromise(result: ReturnType<typeof authGuard>) {
    if (result === true || result === false || result instanceof UrlTree) {
      return result;
    }
    return new Promise((resolve) => {
      (result as import('rxjs').Observable<boolean | UrlTree>).subscribe(resolve);
    });
  }
});
