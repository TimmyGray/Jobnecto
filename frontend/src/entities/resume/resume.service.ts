import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { PageCursor } from '@shared/api';
import { CreateResumeCommand, PagedResumes, ResumeResult } from './model';

/** Server-side default; the API caps anything above 100. */
const DEFAULT_PAGE_SIZE = 20;

/**
 * Signal-backed résumé entity service (Decision 1.2/1.3: Angular Signals +
 * injectable services with a thin cache — no NgRx, no TanStack).
 *
 * URLs are relative; the HTTP interceptor prefixes the API base and sets
 * `withCredentials` so the auth cookie is carried. Results are owner-scoped
 * by that cookie — there is no user-id parameter.
 */
@Injectable({ providedIn: 'root' })
export class ResumeService {
  private readonly http = inject(HttpClient);

  /** Backing signal for the most recent successful page. */
  private readonly pageSignal = signal<PagedResumes | null>(null);

  /**
   * Bumped by every {@link list} call and by {@link invalidate}. A list()
   * response only applies if its own captured generation is still current —
   * otherwise an in-flight list() that resolves *after* a later invalidate()
   * (e.g. from create()) would silently resurrect the stale cached page.
   */
  private generation = 0;

  /** Read-only view of the most recent successful page (null before the first load). */
  readonly page = this.pageSignal.asReadonly();

  /**
   * Fetches a page of the caller's résumés, most-recently-updated first
   * (the server orders `UpdatedAt DESC, Id DESC` — no client-side sort needed).
   * @param pageSize How many résumés to request; the server caps this at 100.
   * @param cursor Both cursor fields from a previous page, to fetch the next one.
   * @returns The cursor-paginated page. A successful page is cached in {@link page}.
   */
  list(pageSize: number = DEFAULT_PAGE_SIZE, cursor?: PageCursor): Observable<PagedResumes> {
    let params = new HttpParams().set('pageSize', pageSize);
    if (cursor) {
      params = params
        .set('lastSeenId', cursor.lastSeenId)
        .set('lastSeenUpdatedAt', cursor.lastSeenUpdatedAt);
    }

    const requestGeneration = ++this.generation;
    return this.http.get<PagedResumes>('/resumes', { params }).pipe(
      tap((page) => {
        if (requestGeneration === this.generation) {
          this.pageSignal.set(page);
        }
      }),
    );
  }

  /**
   * Creates a résumé for the current authenticated user.
   * @param command The résumé fields to create.
   * @returns The created résumé. On success, invalidates the cached page so
   * the next {@link list} call refetches rather than serving a stale page.
   */
  create(command: CreateResumeCommand): Observable<ResumeResult> {
    return this.http
      .post<ResumeResult>('/resumes', command)
      .pipe(tap(() => this.invalidate()));
  }

  /** Clears the cached page so the next read refetches, and fences off any in-flight list(). */
  invalidate(): void {
    this.generation++;
    this.pageSignal.set(null);
  }
}
