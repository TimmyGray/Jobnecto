import { describe, expect, it, beforeEach } from 'vitest';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { httpInterceptor } from '@shared/api/http.interceptor';
import { env } from '@shared/config';
import { EducationService } from './education.service';
import type { PagedEducations } from './model';

/** A minimal but structurally complete paged response. */
function pagedEducations(overrides: Partial<PagedEducations> = {}): PagedEducations {
  return {
    items: [],
    totalCount: 0,
    lastSeenId: null,
    lastSeenUpdatedAt: null,
    pageSize: 20,
    hasNext: false,
    ...overrides,
  };
}

describe('EducationService', () => {
  let service: EducationService;
  let httpMock: HttpTestingController;

  /** Matches the list request regardless of its query string. */
  const listRequest = () =>
    httpMock.expectOne((req) => req.url === `${env.apiBaseUrl}/educations`);

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([httpInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    service = TestBed.inject(EducationService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  it('list() GETs /educations with credentials and the default page size', () => {
    service.list().subscribe();

    const req = listRequest();
    expect(req.request.method).toBe('GET');
    expect(req.request.withCredentials).toBe(true);
    expect(req.request.params.get('pageSize')).toBe('20');

    req.flush(pagedEducations());
    httpMock.verify();
  });

  it('list() sends the requested page size', () => {
    service.list(5).subscribe();

    const req = listRequest();
    expect(req.request.params.get('pageSize')).toBe('5');

    req.flush(pagedEducations({ pageSize: 5 }));
    httpMock.verify();
  });

  it('list() omits both cursor params when no cursor is given', () => {
    service.list(5).subscribe();

    const req = listRequest();
    expect(req.request.params.has('lastSeenId')).toBe(false);
    expect(req.request.params.has('lastSeenUpdatedAt')).toBe(false);

    req.flush(pagedEducations());
    httpMock.verify();
  });

  it('list() sends both cursor params when a cursor is given', () => {
    service
      .list(5, { lastSeenId: 'e-4', lastSeenUpdatedAt: '2026-09-18T08:30:00Z' })
      .subscribe();

    const req = listRequest();
    expect(req.request.params.get('lastSeenId')).toBe('e-4');
    expect(req.request.params.get('lastSeenUpdatedAt')).toBe('2026-09-18T08:30:00Z');

    req.flush(pagedEducations());
    httpMock.verify();
  });

  it('list() caches the last successful page in a signal', () => {
    expect(service.page()).toBeNull();

    service.list(5).subscribe();
    listRequest().flush(
      pagedEducations({ items: [{ id: 'e-1', title: 'BSc Computer Science' }], totalCount: 1 }),
    );

    expect(service.page()?.totalCount).toBe(1);
    expect(service.page()?.items[0].title).toBe('BSc Computer Science');
    httpMock.verify();
  });

  it('list() leaves the cached page untouched when the request fails', () => {
    service.list(5).subscribe();
    listRequest().flush(pagedEducations({ totalCount: 2 }));

    service.list(5).subscribe({ error: () => undefined });
    listRequest().flush(
      { title: 'Server error', status: 500 },
      { status: 500, statusText: 'Server Error' },
    );

    expect(service.page()?.totalCount).toBe(2);
    httpMock.verify();
  });

  it('invalidate() clears the cached page', () => {
    service.list(5).subscribe();
    listRequest().flush(pagedEducations({ totalCount: 2 }));
    expect(service.page()).not.toBeNull();

    service.invalidate();

    expect(service.page()).toBeNull();
    httpMock.verify();
  });
});
