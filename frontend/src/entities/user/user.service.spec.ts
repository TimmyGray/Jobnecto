import { describe, expect, it, beforeEach } from 'vitest';
import {
  provideHttpClient,
  withInterceptors,
} from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { httpInterceptor } from '@shared/api/http.interceptor';
import { SKIP_AUTH_REDIRECT } from '@shared/api/auth-redirect';
import { env } from '@shared/config';
import { UserService } from './user.service';

describe('UserService', () => {
  let service: UserService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([httpInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    service = TestBed.inject(UserService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  it('register() POSTs to /users with the registration payload', () => {
    const payload = { loginName: 'daria_k', email: 'daria@example.com', password: 'sup3rsecret' };
    service.register(payload).subscribe();

    const req = httpMock.expectOne(`${env.apiBaseUrl}/users`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(payload);
    expect(req.request.withCredentials).toBe(true);
    req.flush({ id: 'u1', ...payload }, { status: 201, statusText: 'Created' });
    httpMock.verify();
  });

  it('fetchCurrentUser() GETs /users/me and caches the profile in a signal', () => {
    expect(service.isAuthenticated()).toBe(false);

    service.fetchCurrentUser().subscribe();

    const req = httpMock.expectOne(`${env.apiBaseUrl}/users/me`);
    expect(req.request.method).toBe('GET');
    req.flush({ id: 'u1', loginName: 'daria_k', email: 'daria@example.com' });

    expect(service.isAuthenticated()).toBe(true);
    expect(service.profile()?.loginName).toBe('daria_k');
    httpMock.verify();
  });

  it('fetchCurrentUser() defaults to NOT skipping the 401 redirect (a mid-session re-fetch should participate in it)', () => {
    service.fetchCurrentUser().subscribe({ error: () => undefined });

    const req = httpMock.expectOne(`${env.apiBaseUrl}/users/me`);
    expect(req.request.context.get(SKIP_AUTH_REDIRECT)).toBe(false);
    req.flush({ title: 'Unauthorized', status: 401 }, { status: 401, statusText: 'Unauthorized' });
    httpMock.verify();
  });

  it('fetchCurrentUser(true) sets SKIP_AUTH_REDIRECT (post-register/sign-in hydration failure is not a session lapse)', () => {
    service.fetchCurrentUser(true).subscribe({ error: () => undefined });

    const req = httpMock.expectOne(`${env.apiBaseUrl}/users/me`);
    expect(req.request.context.get(SKIP_AUTH_REDIRECT)).toBe(true);
    req.flush({ title: 'Unauthorized', status: 401 }, { status: 401, statusText: 'Unauthorized' });
    httpMock.verify();
  });

  it('signIn() POSTs to /users/sessions with the credentials and does not cache the profile', () => {
    const payload = { identifier: 'daria_dev', password: 'sup3rsecret' };
    service.signIn(payload).subscribe();

    const req = httpMock.expectOne(`${env.apiBaseUrl}/users/sessions`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(payload);
    expect(req.request.withCredentials).toBe(true);
    req.flush({ id: 'u1', loginName: 'daria_dev', accessToken: '' }, { status: 200, statusText: 'OK' });

    expect(service.isAuthenticated()).toBe(false);
    httpMock.verify();
  });

  it('signIn() marks its request with SKIP_AUTH_REDIRECT (a 401 here is a credential failure, not a session lapse)', () => {
    service.signIn({ identifier: 'x', password: 'y' }).subscribe({ error: () => undefined });

    const req = httpMock.expectOne(`${env.apiBaseUrl}/users/sessions`);
    expect(req.request.context.get(SKIP_AUTH_REDIRECT)).toBe(true);
    req.flush({ title: 'Unauthorized', status: 401 }, { status: 401, statusText: 'Unauthorized' });
    httpMock.verify();
  });

  it('refreshToken() POSTs to /users/token/refresh with SKIP_AUTH_REDIRECT set', () => {
    service.refreshToken().subscribe();

    const req = httpMock.expectOne(`${env.apiBaseUrl}/users/token/refresh`);
    expect(req.request.method).toBe('POST');
    expect(req.request.withCredentials).toBe(true);
    expect(req.request.context.get(SKIP_AUTH_REDIRECT)).toBe(true);
    req.flush({ accessToken: '', tokenType: 'Bearer', renewalPolicy: '...' });
    httpMock.verify();
  });

  it('restoreSession() refreshes then hydrates the profile, in order', () => {
    let resolved: unknown;
    service.restoreSession().subscribe((profile) => (resolved = profile));

    const refreshReq = httpMock.expectOne(`${env.apiBaseUrl}/users/token/refresh`);
    refreshReq.flush({ accessToken: '', tokenType: 'Bearer', renewalPolicy: '...' });

    const meReq = httpMock.expectOne(`${env.apiBaseUrl}/users/me`);
    expect(meReq.request.context.get(SKIP_AUTH_REDIRECT)).toBe(true);
    meReq.flush({ id: 'u1', loginName: 'daria_dev' });

    expect(service.isAuthenticated()).toBe(true);
    expect((resolved as { loginName: string }).loginName).toBe('daria_dev');
    httpMock.verify();
  });

  it('restoreSession() propagates a 401 from refresh and never calls /users/me', () => {
    let errored: unknown;
    service.restoreSession().subscribe({ error: (err) => (errored = err) });

    httpMock
      .expectOne(`${env.apiBaseUrl}/users/token/refresh`)
      .flush({ title: 'Unauthorized', status: 401 }, { status: 401, statusText: 'Unauthorized' });

    httpMock.expectNone(`${env.apiBaseUrl}/users/me`);
    expect(errored).toBeDefined();
    expect(service.isAuthenticated()).toBe(false);
    httpMock.verify();
  });

  it('invalidate() clears the cached profile', () => {
    service.fetchCurrentUser().subscribe();
    httpMock.expectOne(`${env.apiBaseUrl}/users/me`).flush({ id: 'u1', loginName: 'x' });
    expect(service.isAuthenticated()).toBe(true);

    service.invalidate();
    expect(service.isAuthenticated()).toBe(false);
    expect(service.profile()).toBeNull();
    httpMock.verify();
  });
});
