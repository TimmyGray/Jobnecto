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
