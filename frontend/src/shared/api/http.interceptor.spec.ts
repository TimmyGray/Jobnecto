import { describe, expect, it, beforeEach } from 'vitest';
import {
  HttpClient,
  provideHttpClient,
  withInterceptors,
} from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { httpInterceptor } from './http.interceptor';
import { ProblemDetails } from './problem-details';
import { env } from '@shared/config';

describe('httpInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([httpInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
  });

  it('sets withCredentials: true on every request', () => {
    http.get('/users/me').subscribe();

    const reqUrl = `${env.apiBaseUrl}/users/me`;
    const testReq = httpMock.expectOne(reqUrl);
    expect(testReq.request.withCredentials).toBe(true);
    testReq.flush({});
    httpMock.verify();
  });

  it('prefixes relative URLs with the configured API base URL', () => {
    http.post('/users', { loginName: 'a' }).subscribe();

    const testReq = httpMock.expectOne(`${env.apiBaseUrl}/users`);
    expect(testReq.request.url).toBe(`${env.apiBaseUrl}/users`);
    testReq.flush({}, { status: 201, statusText: 'Created' });
    httpMock.verify();
  });

  it('leaves absolute URLs untouched (still sets withCredentials)', () => {
    http.get('http://localhost:5000/openapi/v1.json').subscribe();

    const testReq = httpMock.expectOne('http://localhost:5000/openapi/v1.json');
    expect(testReq.request.url).toBe('http://localhost:5000/openapi/v1.json');
    expect(testReq.request.withCredentials).toBe(true);
    testReq.flush({});
    httpMock.verify();
  });

  it('normalizes a 400 error into typed ProblemDetails with an errors map', () => {
    let captured: ProblemDetails | undefined;
    http.post('/users', {}).subscribe({
      error: (err: ProblemDetails) => (captured = err),
    });

    httpMock.expectOne(`${env.apiBaseUrl}/users`).flush(
      {
        title: 'Validation failed',
        status: 400,
        errors: { email: ['email must be a valid email address.'] },
      },
      { status: 400, statusText: 'Bad Request' },
    );

    expect(captured).toBeDefined();
    expect(captured!.status).toBe(400);
    expect(captured!.errors).toEqual({
      email: ['email must be a valid email address.'],
    });
    httpMock.verify();
  });

  it('normalizes a 409 error into typed ProblemDetails', () => {
    let captured: ProblemDetails | undefined;
    http.post('/users', {}).subscribe({
      error: (err: ProblemDetails) => (captured = err),
    });

    httpMock.expectOne(`${env.apiBaseUrl}/users`).flush(
      { title: 'Conflict', status: 409, detail: 'Email already registered.' },
      { status: 409, statusText: 'Conflict' },
    );

    expect(captured!.status).toBe(409);
    expect(captured!.errors).toBeUndefined();
    httpMock.verify();
  });
});
