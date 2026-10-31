import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import type { PageCursor } from '@shared/api';
import type { FilterVacanciesQuery, PagedVacancies } from './model';

/** Server-side default; the API caps anything above 100. */
const DEFAULT_PAGE_SIZE = 20;

/**
 * Vacancy entity service (Decision 1: Angular Signals + injectable services —
 * no NgRx, no TanStack).
 *
 * Holds no cache: the board accumulates pages itself. URLs are relative; the
 * HTTP interceptor prefixes the API base and sets `withCredentials`, so results
 * are owner-scoped by the auth cookie and no user id is ever sent.
 */
@Injectable({ providedIn: 'root' })
export class VacancyService {
  private readonly http = inject(HttpClient);

  /**
   * Fetches one page of the caller's vacancies in browse mode: no criteria and
   * no `sortBy`, so the server's default `CreatedAt DESC, Id DESC` order applies.
   *
   * The cursor travels in the JSON body, not the query string. Under the default
   * sort the server fills `lastSeenUpdatedAt` with the last item's `createdAt`,
   * so the cursor must be echoed back exactly as received, never rebuilt from an
   * item.
   * @param pageSize How many vacancies to request; the server caps this at 100.
   * @param cursor Both cursor fields from a previous page, to fetch the next one.
   * @returns The cursor-paginated page.
   */
  filter(pageSize: number = DEFAULT_PAGE_SIZE, cursor?: PageCursor): Observable<PagedVacancies> {
    const body: FilterVacanciesQuery = { pageSize, ...cursor };
    return this.http.post<PagedVacancies>('/vacancies/filter', body);
  }
}
