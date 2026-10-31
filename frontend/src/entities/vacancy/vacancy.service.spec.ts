import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { httpInterceptor } from '@shared/api/http.interceptor';
import type { ProblemDetails } from '@shared/api';
import { env } from '@shared/config';
import type { PagedVacancies } from './model';
import { VacancyService } from './vacancy.service';

/** A minimal but structurally complete paged response. */
function pagedVacancies(overrides: Partial<PagedVacancies> = {}): PagedVacancies {
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

describe('VacancyService', () => {
  let service: VacancyService;
  let httpMock: HttpTestingController;

  const filterUrl = `${env.apiBaseUrl}/vacancies/filter`;
  const filterRequest = () => httpMock.expectOne((req) => req.url === filterUrl);

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(withInterceptors([httpInterceptor])), provideHttpClientTesting()],
    });
    service = TestBed.inject(VacancyService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('filter() POSTs /vacancies/filter with credentials and only the default page size', () => {
    service.filter().subscribe();

    const req = filterRequest();
    expect(req.request.method).toBe('POST');
    expect(req.request.withCredentials).toBe(true);
    expect(req.request.body).toEqual({ pageSize: 20 });
    expect(req.request.params.keys()).toEqual([]);
    req.flush(pagedVacancies());
  });

  it('filter() honours a custom page size', () => {
    service.filter(5).subscribe();

    const req = filterRequest();
    expect(req.request.body).toEqual({ pageSize: 5 });
    req.flush(pagedVacancies());
  });

  it('filter() sends both cursor fields in the body, never as query params', () => {
    service
      .filter(20, { lastSeenId: 'id-1', lastSeenUpdatedAt: '2026-10-01T10:00:00.1234567Z' })
      .subscribe();

    const req = filterRequest();
    expect(req.request.body).toEqual({
      pageSize: 20,
      lastSeenId: 'id-1',
      lastSeenUpdatedAt: '2026-10-01T10:00:00.1234567Z',
    });
    expect(req.request.params.keys()).toEqual([]);
    expect(req.request.urlWithParams).toBe(filterUrl);
    req.flush(pagedVacancies());
  });

  it('filter() emits the page the server returned', () => {
    let result: PagedVacancies | undefined;
    service.filter().subscribe((page) => (result = page));

    const body = pagedVacancies({ items: [{ id: 'v-1', title: 'Engineer' }], totalCount: 1 });
    filterRequest().flush(body);

    expect(result).toEqual(body);
  });

  it('filter() propagates errors', () => {
    let status: number | undefined;
    service.filter().subscribe({ error: (err: ProblemDetails) => (status = err.status) });

    filterRequest().flush(
      { title: 'Server error', status: 500 },
      { status: 500, statusText: 'Server Error' },
    );

    expect(status).toBe(500);
  });
});
