import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import {
  HttpTestingController,
  TestRequest,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import type { ResumeResult } from '@entities/resume';
import { httpInterceptor } from '@shared/api/http.interceptor';
import { env } from '@shared/config';
import { ResumeListPage } from './resume-list.page';

const CURSOR_ID = '3f2b8c1e-0000-4000-8000-0000000000aa';
const CURSOR_AT = '2026-09-28T10:00:00Z';

/** Builds one résumé, overriding whichever fields the scenario cares about. */
function resume(id: string, overrides: Partial<ResumeResult> = {}): ResumeResult {
  return { id, title: `Résumé ${id}`, skills: [], ...overrides };
}

interface PageOptions {
  hasNext?: boolean;
  totalCount?: number | string;
  lastSeenId?: string | null;
  lastSeenUpdatedAt?: string | null;
}

/** A structurally complete paged body. A next page gets a usable cursor by default. */
function paged(items: ResumeResult[], options: PageOptions = {}) {
  const hasNext = options.hasNext ?? false;
  return {
    items,
    totalCount: options.totalCount ?? items.length,
    lastSeenId: options.lastSeenId !== undefined ? options.lastSeenId : hasNext ? CURSOR_ID : null,
    lastSeenUpdatedAt:
      options.lastSeenUpdatedAt !== undefined ? options.lastSeenUpdatedAt : hasNext ? CURSOR_AT : null,
    pageSize: 20,
    hasNext,
  };
}

describe('ResumeListPage', () => {
  let fixture: ComponentFixture<ResumeListPage>;
  let page: ResumeListPage;
  let httpMock: HttpTestingController;

  const listUrl = `${env.apiBaseUrl}/resumes`;
  const expectList = (): TestRequest => httpMock.expectOne((req) => req.url === listUrl);
  const el = (): HTMLElement => fixture.nativeElement;
  const cards = (): HTMLElement[] =>
    Array.from(el().querySelectorAll('[data-testid="resume-card"]'));
  const loadMoreButton = (): HTMLButtonElement | null =>
    el().querySelector('ui-cursor-pagination button');
  const announcement = (): string | undefined =>
    el().querySelector('[data-testid="list-announcement"]')?.textContent?.trim();
  const serverError = (req: TestRequest): void =>
    req.flush({ title: 'Server error', status: 500 }, { status: 500, statusText: 'Server Error' });

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ResumeListPage],
      providers: [
        provideHttpClient(withInterceptors([httpInterceptor])),
        provideHttpClientTesting(),
        provideRouter([]),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ResumeListPage);
    page = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
  });

  afterEach(() => httpMock.verify());

  describe('first load', () => {
    it('requests the first page with the default page size and no cursor', () => {
      const req = expectList();

      expect(req.request.method).toBe('GET');
      expect(req.request.params.get('pageSize')).toBe('20');
      expect(req.request.params.has('lastSeenId')).toBe(false);
      expect(req.request.params.has('lastSeenUpdatedAt')).toBe(false);
      req.flush(paged([]));
    });

    it('shows a skeleton of cards (no empty state, no list) while loading', () => {
      expect(el().querySelector('[data-testid="resume-skeleton"]')).not.toBeNull();
      expect(el().querySelector('ui-empty-state')).toBeNull();
      expect(cards()).toHaveLength(0);
      expectList().flush(paged([]));
    });

    it('reserves the subtitle line while loading so the header does not jump', () => {
      expect(el().querySelector('[data-testid="subtitle"]')).not.toBeNull();
      expectList().flush(paged([]));
    });

    it('replaces the skeleton with cards once loaded', () => {
      expectList().flush(paged([resume('a'), resume('b')]));
      fixture.detectChanges();

      expect(el().querySelector('[data-testid="resume-skeleton"]')).toBeNull();
      expect(cards()).toHaveLength(2);
    });

    it('renders exactly one h1', () => {
      expectList().flush(paged([resume('a')]));
      fixture.detectChanges();

      expect(el().querySelectorAll('h1')).toHaveLength(1);
    });

    it('preserves server order and dedupes repeated ids on the first page', () => {
      expectList().flush(paged([resume('z'), resume('a'), resume('z'), resume('m')]));
      fixture.detectChanges();

      expect(cards().map((c) => c.getAttribute('href'))).toEqual([
        '/resumes/z',
        '/resumes/a',
        '/resumes/m',
      ]);
    });

    it('shows the total in the subtitle, singular', () => {
      expectList().flush(paged([resume('a')], { totalCount: 1 }));
      fixture.detectChanges();

      const subtitle = el().querySelector('[data-testid="subtitle"]')?.textContent;
      expect(subtitle).toContain('1 résumé');
      expect(subtitle).not.toContain('1 résumés');
    });

    it('coerces a string totalCount for the subtitle', () => {
      expectList().flush(paged([resume('a')], { totalCount: '7' }));
      fixture.detectChanges();

      expect(el().querySelector('[data-testid="subtitle"]')?.textContent).toContain('7 résumés');
    });

    it('omits the subtitle when totalCount is malformed', () => {
      expectList().flush({ ...paged([resume('a')]), totalCount: 'abc' });
      fixture.detectChanges();

      expect(el().querySelector('[data-testid="subtitle"]')).toBeNull();
    });

    it('announces without a total, and omits the subtitle, when totalCount is null', () => {
      expectList().flush({ ...paged([resume('a'), resume('b')]), totalCount: null });
      fixture.detectChanges();

      expect(announcement()).toBe('Showing 2 résumés');
      expect(el().querySelector('[data-testid="subtitle"]')).toBeNull();
    });
  });

  describe('empty state (AC2)', () => {
    beforeEach(() => {
      expectList().flush(paged([]));
      fixture.detectChanges();
    });

    it('shows "No resumes yet" with a CTA to /resumes/new', () => {
      expect(el().textContent).toContain('No resumes yet');
      expect(el().querySelector('ui-empty-state a')?.getAttribute('href')).toBe('/resumes/new');
    });

    it('does not also show the header primary action (one primary action per page)', () => {
      expect(el().querySelector('[data-testid="header-create"]')).toBeNull();
    });

    it('shows no load-more control', () => {
      expect(loadMoreButton()).toBeNull();
    });

    it('announces nothing for an empty list', () => {
      expect(announcement()).toBe('');
    });
  });

  describe('populated list', () => {
    it('offers the header create action when there are résumés', () => {
      expectList().flush(paged([resume('a')]));
      fixture.detectChanges();

      expect(el().querySelector('[data-testid="header-create"]')?.getAttribute('href')).toBe(
        '/resumes/new',
      );
      expect(el().querySelector('ui-empty-state')).toBeNull();
    });

    it('announces how many are showing in a polite live region', () => {
      expectList().flush(paged([resume('a'), resume('b')], { hasNext: true, totalCount: 5 }));
      fixture.detectChanges();

      const live = el().querySelector('[aria-live="polite"][data-testid="list-announcement"]');
      expect(live?.textContent?.trim()).toBe('Showing 2 of 5 résumés');
    });

    it('shows no load-more control when hasNext is false', () => {
      expectList().flush(paged([resume('a')]));
      fixture.detectChanges();

      expect(loadMoreButton()).toBeNull();
    });
  });

  describe('load more (AC1)', () => {
    beforeEach(() => {
      expectList().flush(paged([resume('a'), resume('b')], { hasNext: true, totalCount: 4 }));
      fixture.detectChanges();
    });

    it('shows the load-more button when hasNext is true', () => {
      expect(loadMoreButton()).not.toBeNull();
    });

    it('sends both cursor fields and the page size, then appends rather than replaces', () => {
      loadMoreButton()!.click();

      const req = expectList();
      expect(req.request.params.get('lastSeenId')).toBe(CURSOR_ID);
      expect(req.request.params.get('lastSeenUpdatedAt')).toBe(CURSOR_AT);
      expect(req.request.params.get('pageSize')).toBe('20');
      req.flush(paged([resume('c'), resume('d')], { totalCount: 4 }));
      fixture.detectChanges();

      expect(cards().map((c) => c.querySelector('h2')?.textContent?.trim())).toEqual([
        'Résumé a',
        'Résumé b',
        'Résumé c',
        'Résumé d',
      ]);
    });

    it('hides the button after the last page', () => {
      loadMoreButton()!.click();
      expectList().flush(paged([resume('c'), resume('d')]));
      fixture.detectChanges();

      expect(loadMoreButton()).toBeNull();
    });

    it('updates the announcement after a load-more', () => {
      loadMoreButton()!.click();
      expectList().flush(paged([resume('c'), resume('d')], { totalCount: 4 }));
      fixture.detectChanges();

      expect(announcement()).toBe('Showing 4 of 4 résumés');
    });

    it('uses the cursor from the latest page for the page after that', () => {
      loadMoreButton()!.click();
      expectList().flush(
        paged([resume('c')], {
          hasNext: true,
          lastSeenId: 'next-id',
          lastSeenUpdatedAt: '2026-09-01T00:00:00Z',
        }),
      );
      fixture.detectChanges();

      loadMoreButton()!.click();
      const req = expectList();
      expect(req.request.params.get('lastSeenId')).toBe('next-id');
      expect(req.request.params.get('lastSeenUpdatedAt')).toBe('2026-09-01T00:00:00Z');
      req.flush(paged([]));
    });

    it('does not render a résumé twice if a later page repeats an id', () => {
      loadMoreButton()!.click();
      expectList().flush(paged([resume('b'), resume('c')]));
      fixture.detectChanges();

      expect(cards()).toHaveLength(3);
    });

    it('dedupes repeated ids within a single later page', () => {
      loadMoreButton()!.click();
      expectList().flush(paged([resume('c'), resume('c'), resume('d')]));
      fixture.detectChanges();

      expect(cards().map((c) => c.getAttribute('href'))).toEqual([
        '/resumes/a',
        '/resumes/b',
        '/resumes/c',
        '/resumes/d',
      ]);
    });

    it('keeps id-less résumés from a later page (they cannot be deduped)', () => {
      loadMoreButton()!.click();
      expectList().flush(paged([{ title: 'No id one' }, { title: 'No id two' }]));
      fixture.detectChanges();

      expect(cards()).toHaveLength(4);
    });

    it('sends exactly one request when load-more is triggered twice while in flight', () => {
      page.loadMore();
      page.loadMore();

      const reqs = httpMock.match((req) => req.url === listUrl);
      expect(reqs).toHaveLength(1);
      reqs[0].flush(paged([]));
    });

    it('marks the button busy and aria-disabled while loading, keeping focus on it', () => {
      document.body.appendChild(el());
      loadMoreButton()!.focus();
      loadMoreButton()!.click();
      fixture.detectChanges();

      expect(loadMoreButton()?.getAttribute('aria-busy')).toBe('true');
      expect(loadMoreButton()?.getAttribute('aria-disabled')).toBe('true');
      expect(loadMoreButton()?.textContent?.trim()).toBe('Loading…');
      expect(document.activeElement).toBe(loadMoreButton());
      expectList().flush(paged([]));
      el().remove();
    });

    it('keeps the loaded cards and relabels the same button "Try again" when load-more fails', () => {
      document.body.appendChild(el());
      loadMoreButton()!.focus();
      loadMoreButton()!.click();
      serverError(expectList());
      fixture.detectChanges();

      expect(cards()).toHaveLength(2);
      expect(el().querySelector('ui-error-state')).toBeNull();
      const alert = el().querySelector('[data-testid="load-more-error"]');
      expect(alert?.getAttribute('role')).toBe('alert');
      expect(alert?.textContent).toContain("Couldn't load more");
      expect(loadMoreButton()?.textContent?.trim()).toBe('Try again');
      expect(el().querySelectorAll('ui-cursor-pagination button')).toHaveLength(1);
      // Focus stays on the one button across the failure, so a keyboard user keeps their place.
      expect(document.activeElement).toBe(loadMoreButton());

      loadMoreButton()!.click();
      const retry = expectList();
      expect(retry.request.params.get('lastSeenId')).toBe(CURSOR_ID);
      retry.flush(paged([resume('c')], { totalCount: 3 }));
      fixture.detectChanges();

      expect(cards()).toHaveLength(3);
      expect(el().querySelector('[data-testid="load-more-error"]')).toBeNull();
      el().remove();
    });

    it('moves focus to the first newly loaded card', () => {
      document.body.appendChild(el());
      loadMoreButton()!.click();
      expectList().flush(paged([resume('c'), resume('d')]));
      fixture.detectChanges();
      fixture.detectChanges();

      expect(document.activeElement).toBe(cards()[2]);
      el().remove();
    });

    it('does not steal focus if the user moved it elsewhere while loading', () => {
      document.body.appendChild(el());
      const elsewhere = document.createElement('button');
      document.body.appendChild(elsewhere);
      loadMoreButton()!.click();
      elsewhere.focus();

      expectList().flush(paged([resume('c')]));
      fixture.detectChanges();
      fixture.detectChanges();

      expect(document.activeElement).toBe(elsewhere);
      elsewhere.remove();
      el().remove();
    });

    it('lands focus on the last card when the final page adds nothing new and the button disappears', () => {
      document.body.appendChild(el());
      loadMoreButton()!.focus();
      loadMoreButton()!.click();
      expectList().flush(paged([resume('a'), resume('b')]));
      fixture.detectChanges();
      fixture.detectChanges();

      expect(loadMoreButton()).toBeNull();
      expect(document.activeElement).toBe(cards()[1]);
      el().remove();
    });
  });

  describe('cursor edge cases', () => {
    it('shows no load-more when hasNext is true but a cursor field is null', () => {
      expectList().flush(
        paged([resume('a')], { hasNext: true, lastSeenId: null, lastSeenUpdatedAt: CURSOR_AT }),
      );
      fixture.detectChanges();

      expect(loadMoreButton()).toBeNull();
      expect(cards()).toHaveLength(1);
    });

    it('loadMore() is a no-op with no cursor', () => {
      expectList().flush(paged([resume('a')]));
      fixture.detectChanges();

      page.loadMore();

      httpMock.expectNone((req) => req.url === listUrl);
    });
  });

  describe('first-load failure', () => {
    beforeEach(() => {
      serverError(expectList());
      fixture.detectChanges();
    });

    it('shows the error state with Retry, not an empty state', () => {
      expect(el().querySelector('ui-error-state')).not.toBeNull();
      expect(el().querySelector('ui-empty-state')).toBeNull();
      expect(el().querySelector('[data-testid="resume-skeleton"]')).toBeNull();
    });

    it('ignores Retry while a reload is already in flight', () => {
      page.retry();
      page.retry();

      const reqs = httpMock.match((req) => req.url === listUrl);
      expect(reqs).toHaveLength(1);
      reqs[0].flush(paged([]));
    });

    it('retries from the beginning and recovers', () => {
      (el().querySelector('ui-error-state button') as HTMLButtonElement).click();
      fixture.detectChanges();
      expect(el().querySelector('[data-testid="resume-skeleton"]')).not.toBeNull();

      const req = expectList();
      expect(req.request.params.has('lastSeenId')).toBe(false);
      req.flush(paged([resume('a')]));
      fixture.detectChanges();

      expect(el().querySelector('ui-error-state')).toBeNull();
      expect(cards()).toHaveLength(1);
    });
  });

  describe('ownership (AC4)', () => {
    it('never sends a user identifier and renders exactly what the server returned', () => {
      const req = expectList();
      const keys = req.request.params.keys();
      expect(keys.every((k) => ['pageSize', 'lastSeenId', 'lastSeenUpdatedAt'].includes(k))).toBe(
        true,
      );
      expect(req.request.url).not.toMatch(/user/i);
      expect(req.request.withCredentials).toBe(true);
      req.flush(paged([resume('mine-2'), resume('mine-1')]));
      fixture.detectChanges();

      expect(cards().map((c) => c.getAttribute('href'))).toEqual([
        '/resumes/mine-2',
        '/resumes/mine-1',
      ]);
    });
  });

  describe('teardown', () => {
    it('cancels the in-flight first load when the page is destroyed', () => {
      const req = expectList();

      fixture.destroy();

      expect(req.cancelled).toBe(true);
    });

    it('cancels an in-flight load-more when the page is destroyed', () => {
      expectList().flush(paged([resume('a')], { hasNext: true }));
      fixture.detectChanges();
      loadMoreButton()!.click();
      const req = expectList();

      fixture.destroy();

      expect(req.cancelled).toBe(true);
    });
  });
});
