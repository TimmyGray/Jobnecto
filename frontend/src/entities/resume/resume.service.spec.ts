import { describe, expect, it, beforeEach } from 'vitest';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { httpInterceptor } from '@shared/api/http.interceptor';
import { env } from '@shared/config';
import { ResumeService } from './resume.service';
import type { CreateResumeCommand, PagedResumes, ResumeResult } from './model';
import type { ProblemDetails } from '@shared/api';

/** A minimal but structurally complete paged response. */
function pagedResumes(overrides: Partial<PagedResumes> = {}): PagedResumes {
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

describe('ResumeService', () => {
  let service: ResumeService;
  let httpMock: HttpTestingController;

  /** Matches the list request regardless of its query string. */
  const listRequest = () =>
    httpMock.expectOne((req) => req.url === `${env.apiBaseUrl}/resumes`);

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([httpInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    service = TestBed.inject(ResumeService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  it('list() GETs /resumes with credentials and the default page size', () => {
    service.list().subscribe();

    const req = listRequest();
    expect(req.request.method).toBe('GET');
    expect(req.request.withCredentials).toBe(true);
    expect(req.request.params.get('pageSize')).toBe('20');

    req.flush(pagedResumes());
    httpMock.verify();
  });

  it('list() sends the requested page size', () => {
    service.list(5).subscribe();

    const req = listRequest();
    expect(req.request.params.get('pageSize')).toBe('5');

    req.flush(pagedResumes({ pageSize: 5 }));
    httpMock.verify();
  });

  it('list() omits both cursor params when no cursor is given', () => {
    service.list(5).subscribe();

    const req = listRequest();
    expect(req.request.params.has('lastSeenId')).toBe(false);
    expect(req.request.params.has('lastSeenUpdatedAt')).toBe(false);

    req.flush(pagedResumes());
    httpMock.verify();
  });

  it('list() sends both cursor params when a cursor is given', () => {
    service
      .list(5, { lastSeenId: 'r-9', lastSeenUpdatedAt: '2026-09-20T10:00:00Z' })
      .subscribe();

    const req = listRequest();
    expect(req.request.params.get('lastSeenId')).toBe('r-9');
    expect(req.request.params.get('lastSeenUpdatedAt')).toBe('2026-09-20T10:00:00Z');

    req.flush(pagedResumes());
    httpMock.verify();
  });

  it('list() caches the last successful page in a signal', () => {
    expect(service.page()).toBeNull();

    service.list(5).subscribe();
    const page = pagedResumes({ items: [{ id: 'r-1', title: 'Staff Engineer' }], totalCount: 1 });
    listRequest().flush(page);

    expect(service.page()?.totalCount).toBe(1);
    expect(service.page()?.items[0].title).toBe('Staff Engineer');
    httpMock.verify();
  });

  it('list() leaves the cached page untouched when the request fails', () => {
    service.list(5).subscribe();
    listRequest().flush(pagedResumes({ totalCount: 3 }));

    service.list(5).subscribe({ error: () => undefined });
    listRequest().flush({ title: 'Server error', status: 500 }, { status: 500, statusText: 'Server Error' });

    expect(service.page()?.totalCount).toBe(3);
    httpMock.verify();
  });

  it('invalidate() clears the cached page', () => {
    service.list(5).subscribe();
    listRequest().flush(pagedResumes({ totalCount: 2 }));
    expect(service.page()).not.toBeNull();

    service.invalidate();

    expect(service.page()).toBeNull();
    httpMock.verify();
  });

  describe('create()', () => {
    const command: CreateResumeCommand = { title: 'Staff Engineer', skills: ['TypeScript'] };
    const created: ResumeResult = { id: 'r-1', title: 'Staff Engineer', skills: ['TypeScript'] };

    it('POSTs the command to /resumes with credentials', () => {
      service.create(command).subscribe();

      const req = httpMock.expectOne(`${env.apiBaseUrl}/resumes`);
      expect(req.request.method).toBe('POST');
      expect(req.request.withCredentials).toBe(true);
      expect(req.request.body).toEqual(command);

      req.flush(created, { status: 201, statusText: 'Created' });
      httpMock.verify();
    });

    it('invalidates the cached page on success so the next list() call refetches', () => {
      service.list(5).subscribe();
      listRequest().flush(pagedResumes({ totalCount: 1 }));
      expect(service.page()).not.toBeNull();

      service.create(command).subscribe();
      httpMock
        .expectOne(`${env.apiBaseUrl}/resumes`)
        .flush(created, { status: 201, statusText: 'Created' });

      expect(service.page()).toBeNull();
      httpMock.verify();
    });

    it('a stale in-flight list() response cannot resurrect the cache after create() invalidates it', () => {
      // A list() is already in flight (e.g. a dashboard widget) when create()
      // both fires and resolves first — its invalidate() must "win" even
      // though the earlier list() response arrives afterward.
      service.list(5).subscribe();
      const staleListReq = listRequest();

      service.create(command).subscribe();
      httpMock
        .expectOne(`${env.apiBaseUrl}/resumes`)
        .flush(created, { status: 201, statusText: 'Created' });
      expect(service.page()).toBeNull();

      staleListReq.flush(pagedResumes({ totalCount: 3 }));

      expect(service.page()).toBeNull();
      httpMock.verify();
    });

    it('propagates a 400 ProblemDetails untouched and leaves the cache alone', () => {
      service.list(5).subscribe();
      listRequest().flush(pagedResumes({ totalCount: 2 }));

      let error: ProblemDetails | undefined;
      service.create(command).subscribe({ error: (e: ProblemDetails) => (error = e) });
      httpMock.expectOne(`${env.apiBaseUrl}/resumes`).flush(
        { title: 'Validation failed', status: 400, errors: { title: ['title must be at most 500 characters long.'] } },
        { status: 400, statusText: 'Bad Request' },
      );

      expect(error?.status).toBe(400);
      expect(error?.errors?.['title']).toEqual(['title must be at most 500 characters long.']);
      expect(service.page()?.totalCount).toBe(2);
      httpMock.verify();
    });
  });
});
