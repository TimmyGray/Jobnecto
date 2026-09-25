import { describe, expect, it, beforeEach, vi } from 'vitest';
import { HttpClient, HttpContext, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { httpInterceptor, SKIP_AUTH_REDIRECT, ProblemDetails } from '@shared/api';
import { env } from '@shared/config';
import { UserService } from '@entities/user';
import { authRedirectInterceptor } from './auth-redirect.interceptor';

describe('authRedirectInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let router: Router;
  let userService: UserService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authRedirectInterceptor, httpInterceptor])),
        provideHttpClientTesting(),
        provideRouter([]),
      ],
    });
    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
    userService = TestBed.inject(UserService);
  });

  it('invalidates the profile and navigates to /sign-in with the current URL as returnUrl on a 401 (AC3)', () => {
    vi.spyOn(router, 'url', 'get').mockReturnValue('/resumes');
    const navSpy = vi.spyOn(router, 'navigate').mockResolvedValue(true);
    const invalidateSpy = vi.spyOn(userService, 'invalidate');

    http.get('/resumes').subscribe({ error: () => undefined });
    httpMock
      .expectOne(`${env.apiBaseUrl}/resumes`)
      .flush({ title: 'Unauthorized', status: 401 }, { status: 401, statusText: 'Unauthorized' });

    expect(invalidateSpy).toHaveBeenCalled();
    expect(navSpy).toHaveBeenCalledWith(['/sign-in'], { queryParams: { returnUrl: '/resumes' } });
    httpMock.verify();
  });

  it('does not redirect or invalidate when the request carries SKIP_AUTH_REDIRECT', () => {
    const navSpy = vi.spyOn(router, 'navigate').mockResolvedValue(true);
    const invalidateSpy = vi.spyOn(userService, 'invalidate');

    http
      .post('/users/sessions', {}, { context: new HttpContext().set(SKIP_AUTH_REDIRECT, true) })
      .subscribe({ error: () => undefined });
    httpMock
      .expectOne(`${env.apiBaseUrl}/users/sessions`)
      .flush({ title: 'Unauthorized', status: 401 }, { status: 401, statusText: 'Unauthorized' });

    expect(navSpy).not.toHaveBeenCalled();
    expect(invalidateSpy).not.toHaveBeenCalled();
    httpMock.verify();
  });

  it('still surfaces the normalized ProblemDetails to the caller on a redirect-triggering 401', () => {
    vi.spyOn(router, 'navigate').mockResolvedValue(true);
    let captured: ProblemDetails | undefined;

    http.get('/resumes').subscribe({ error: (err: ProblemDetails) => (captured = err) });
    httpMock
      .expectOne(`${env.apiBaseUrl}/resumes`)
      .flush({ title: 'Unauthorized', status: 401 }, { status: 401, statusText: 'Unauthorized' });

    expect(captured!.status).toBe(401);
    httpMock.verify();
  });

  it('does not navigate on a non-401 error', () => {
    const navSpy = vi.spyOn(router, 'navigate').mockResolvedValue(true);

    http.get('/resumes').subscribe({ error: () => undefined });
    httpMock
      .expectOne(`${env.apiBaseUrl}/resumes`)
      .flush({ title: 'Not Found', status: 404 }, { status: 404, statusText: 'Not Found' });

    expect(navSpy).not.toHaveBeenCalled();
    httpMock.verify();
  });

  it('does not re-navigate (or clobber returnUrl) on a 401 while already on /sign-in — guards the concurrent-401 race', () => {
    vi.spyOn(router, 'url', 'get').mockReturnValue('/sign-in');
    const navSpy = vi.spyOn(router, 'navigate').mockResolvedValue(true);

    http.get('/some-widget-call').subscribe({ error: () => undefined });
    httpMock
      .expectOne(`${env.apiBaseUrl}/some-widget-call`)
      .flush({ title: 'Unauthorized', status: 401 }, { status: 401, statusText: 'Unauthorized' });

    expect(navSpy).not.toHaveBeenCalled();
    httpMock.verify();
  });

  it('reads status off a raw HttpErrorResponse when registered without httpInterceptor ahead of it', () => {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authRedirectInterceptor])),
        provideHttpClientTesting(),
        provideRouter([]),
      ],
    });
    const rawHttp = TestBed.inject(HttpClient);
    const rawHttpMock = TestBed.inject(HttpTestingController);
    const rawRouter = TestBed.inject(Router);
    vi.spyOn(rawRouter, 'url', 'get').mockReturnValue('/resumes');
    const navSpy = vi.spyOn(rawRouter, 'navigate').mockResolvedValue(true);

    rawHttp.get(`${env.apiBaseUrl}/resumes`).subscribe({ error: () => undefined });
    rawHttpMock
      .expectOne(`${env.apiBaseUrl}/resumes`)
      .flush({ title: 'Unauthorized' }, { status: 401, statusText: 'Unauthorized' });

    expect(navSpy).toHaveBeenCalledWith(['/sign-in'], { queryParams: { returnUrl: '/resumes' } });
    rawHttpMock.verify();
  });
});
