import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import {
  HttpTestingController,
  TestRequest,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import type { VacancyListItem } from '@entities/vacancy';
import { httpInterceptor } from '@shared/api/http.interceptor';
import { env } from '@shared/config';
import { VacancyBoardPage } from './vacancy-board.page';

const CURSOR_ID = '3f2b8c1e-0000-4000-8000-0000000000aa';
// Seven fractional digits, exactly as the server serializes it: the client must echo it untouched.
const CURSOR_AT = '2026-10-28T10:00:00.1234567Z';

/** Builds one vacancy, overriding whichever fields the scenario cares about. */
function vacancy(id: string, overrides: Partial<VacancyListItem> = {}): VacancyListItem {
  return { id, title: `Vacancy ${id}`, company: 'Acme', skills: [], ...overrides };
}

interface PageOptions {
  hasNext?: boolean;
  totalCount?: number | string | null;
  lastSeenId?: string | null;
  lastSeenUpdatedAt?: string | null;
}

/** A structurally complete paged body. A next page gets a usable cursor by default. */
function paged(items: VacancyListItem[], options: PageOptions = {}) {
  const hasNext = options.hasNext ?? false;
  return {
    items,
    totalCount: options.totalCount !== undefined ? options.totalCount : items.length,
    lastSeenId: options.lastSeenId !== undefined ? options.lastSeenId : hasNext ? CURSOR_ID : null,
    lastSeenUpdatedAt:
      options.lastSeenUpdatedAt !== undefined
        ? options.lastSeenUpdatedAt
        : hasNext
          ? CURSOR_AT
          : null,
    pageSize: 20,
    hasNext,
  };
}

describe('VacancyBoardPage', () => {
  let fixture: ComponentFixture<VacancyBoardPage>;
  let page: VacancyBoardPage;
  let httpMock: HttpTestingController;
  /** Nodes a test attached outside the fixture; removed even when the test fails. */
  const strays: HTMLElement[] = [];

  const filterUrl = `${env.apiBaseUrl}/vacancies/filter`;
  const expectFilter = (): TestRequest => httpMock.expectOne((req) => req.url === filterUrl);
  const el = (): HTMLElement => fixture.nativeElement;
  const cards = (): HTMLElement[] => Array.from(el().querySelectorAll('widget-vacancy-board article'));
  const titles = (): (string | undefined)[] =>
    cards().map((c) => c.querySelector('h2')?.textContent?.trim());
  const viewLinks = (): (string | null)[] =>
    cards().map((c) => c.querySelector('[data-testid="vacancy-view"]')?.getAttribute('href') ?? null);
  const loadMoreButton = (): HTMLButtonElement | null =>
    el().querySelector('ui-cursor-pagination button');
  const skeleton = (): HTMLElement | null => el().querySelector('[data-testid="board-skeleton"]');
  const announcement = (): string | undefined =>
    el().querySelector('[data-testid="list-announcement"]')?.textContent?.trim();
  const serverError = (req: TestRequest): void =>
    req.flush({ title: 'Server error', status: 500 }, { status: 500, statusText: 'Server Error' });
  const addStray = (node: HTMLElement): HTMLElement => {
    document.body.appendChild(node);
    strays.push(node);
    return node;
  };
  /** Flushes, then renders twice so afterNextRender focus hooks run. */
  const settle = (): void => {
    fixture.detectChanges();
    fixture.detectChanges();
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [VacancyBoardPage],
      providers: [
        provideHttpClient(withInterceptors([httpInterceptor])),
        provideHttpClientTesting(),
        provideRouter([]),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(VacancyBoardPage);
    page = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
  });

  afterEach(() => {
    try {
      httpMock.verify();
    } finally {
      strays.forEach((n) => n.remove());
      strays.length = 0;
      (document.activeElement as HTMLElement | null)?.blur?.();
    }
  });

  describe('first load (AC1, AC5)', () => {
    it('POSTs a browse-mode body carrying only pageSize (no criteria, no sortBy, no cursor)', () => {
      const req = expectFilter();

      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ pageSize: 20 });
      expect(req.request.params.keys()).toEqual([]);
      expect(req.request.withCredentials).toBe(true);
      req.flush(paged([]));
    });

    it('shows a skeleton of four cards in the board grid, as a labelled status region', () => {
      const region = skeleton() as HTMLElement;
      expect(region.getAttribute('role')).toBe('status');
      expect(region.querySelector('.sr-only')?.textContent).toContain('Loading vacancies');
      expect(region.children.length).toBe(5); // sr-only label + 4 placeholder cards
      expect(el().querySelector('ui-empty-state')).toBeNull();
      const skeletonGrid = region.className.trim();

      expectFilter().flush(paged([vacancy('a')]));
      fixture.detectChanges();

      expect(skeleton()).toBeNull();
      expect(el().querySelector('widget-vacancy-board ul')?.className.trim()).toBe(skeletonGrid);
      expect(skeletonGrid).toContain('xl:grid-cols-3');
    });

    it('renders one card per vacancy in server order, deduping repeated ids', () => {
      expectFilter().flush(paged([vacancy('z'), vacancy('a'), vacancy('z'), vacancy('m')]));
      fixture.detectChanges();

      expect(viewLinks()).toEqual(['/vacancies/z', '/vacancies/a', '/vacancies/m']);
    });

    it('renders the header with a single h1 and the vacancies eyebrow', () => {
      expectFilter().flush(paged([vacancy('a')]));
      fixture.detectChanges();

      expect(el().querySelectorAll('h1')).toHaveLength(1);
      expect(el().querySelector('ui-page-header')?.textContent).toContain('Vacancies');
      expect(el().querySelector('h1 .font-serif.italic')).not.toBeNull();
    });

    it('shows the total in the subtitle (singular and plural, string counts coerced)', () => {
      expectFilter().flush(paged([vacancy('a')], { totalCount: '7' }));
      fixture.detectChanges();

      expect(el().querySelector('[data-testid="subtitle"]')?.textContent).toContain('7 vacancies');
    });

    it('uses the singular noun for exactly one vacancy', () => {
      expectFilter().flush(paged([vacancy('a')], { totalCount: 1 }));
      fixture.detectChanges();

      expect(el().querySelector('[data-testid="subtitle"]')?.textContent).toContain('1 vacancy');
      expect(announcement()).toBe('Showing 1 of 1 vacancy');
    });

    it('reserves the subtitle line while loading so the header does not jump', () => {
      expect(el().querySelector('[data-testid="subtitle"]')?.textContent).toContain(' ');
      expectFilter().flush(paged([]));
    });

    it('announces without a total, and omits the subtitle, when totalCount is null', () => {
      expectFilter().flush(paged([vacancy('a'), vacancy('b')], { totalCount: null }));
      fixture.detectChanges();

      expect(announcement()).toBe('Showing 2 vacancies');
      expect(el().querySelector('[data-testid="subtitle"]')).toBeNull();
    });

    it('announces how many are showing in a polite live region', () => {
      expectFilter().flush(paged([vacancy('a'), vacancy('b')], { hasNext: true, totalCount: 40 }));
      fixture.detectChanges();

      const live = el().querySelector('[aria-live="polite"][data-testid="list-announcement"]');
      expect(live?.textContent?.trim()).toBe('Showing 2 of 40 vacancies');
    });
  });

  describe('empty board (AC5)', () => {
    beforeEach(() => {
      expectFilter().flush(paged([]));
      fixture.detectChanges();
    });

    it('shows "No vacancies yet" with guidance and a CTA back to the dashboard', () => {
      const empty = el().querySelector('[data-testid="board-empty"]') as HTMLElement;
      expect(empty).not.toBeNull();
      expect(empty.textContent).toContain('No vacancies yet');
      expect(empty.textContent).toContain('Vacancies from your job sources will appear here.');
      const cta = empty.querySelector('a');
      expect(cta?.textContent?.trim()).toBe('Back to dashboard');
      expect(cta?.getAttribute('href')).toBe('/dashboard');
    });

    it('is distinct from a filter "no results" state', () => {
      expect(el().querySelector('[data-testid="board-no-results"]')).toBeNull();
      expect(el().textContent).not.toMatch(/no results/i);
    });

    it('shows no cards, no load-more and no announcement', () => {
      expect(cards()).toHaveLength(0);
      expect(loadMoreButton()).toBeNull();
      expect(announcement()).toBe('');
    });
  });

  describe('first-load failure (AC5)', () => {
    beforeEach(() => {
      serverError(expectFilter());
      fixture.detectChanges();
    });

    it('shows the error state with Retry, not the empty state or the skeleton', () => {
      expect(el().querySelector('ui-error-state')).not.toBeNull();
      expect(el().querySelector('ui-error-state')?.textContent).toContain("Couldn't load vacancies");
      expect(el().querySelector('[data-testid="board-empty"]')).toBeNull();
      expect(skeleton()).toBeNull();
    });

    it('shows no subtitle', () => {
      expect(el().querySelector('[data-testid="subtitle"]')).toBeNull();
    });

    it('hands focus to the first card once a retried load succeeds', () => {
      (el().querySelector('ui-error-state button') as HTMLButtonElement).click();
      settle();
      expect(document.activeElement).toBe(skeleton());

      expectFilter().flush(paged([vacancy('a'), vacancy('b')]));
      settle();

      expect(document.activeElement).toBe(cards()[0].querySelector('a'));
    });

    it('hands focus to the dashboard CTA when a retried load comes back empty', () => {
      (el().querySelector('ui-error-state button') as HTMLButtonElement).click();
      settle();

      expectFilter().flush(paged([]));
      settle();

      expect(document.activeElement).toBe(el().querySelector('[data-testid="board-empty"] a'));
    });

    it('hands focus to the new Retry button when a retried load fails again', () => {
      (el().querySelector('ui-error-state button') as HTMLButtonElement).click();
      settle();

      serverError(expectFilter());
      settle();

      expect(document.activeElement).toBe(el().querySelector('ui-error-state button'));
    });

    it('does not move focus after a successful first load that was not a retry', () => {
      expect(document.activeElement).toBe(document.body);
    });

    it('retries from the first page, moves focus to the loading region, and recovers', () => {
      const retryButton = el().querySelector('ui-error-state button') as HTMLButtonElement;
      retryButton.focus();
      retryButton.click();
      settle();

      expect(skeleton()).not.toBeNull();
      expect(document.activeElement).toBe(skeleton());

      const req = expectFilter();
      expect(req.request.body).toEqual({ pageSize: 20 });
      req.flush(paged([vacancy('a')]));
      fixture.detectChanges();

      expect(el().querySelector('ui-error-state')).toBeNull();
      expect(cards()).toHaveLength(1);
    });

    it('ignores Retry while a reload is already in flight', () => {
      page.retry();
      page.retry();

      const reqs = httpMock.match((req) => req.url === filterUrl);
      expect(reqs).toHaveLength(1);
      reqs[0].flush(paged([]));
    });
  });

  describe('load more (AC2)', () => {
    beforeEach(() => {
      expectFilter().flush(paged([vacancy('a'), vacancy('b')], { hasNext: true, totalCount: 4 }));
      fixture.detectChanges();
    });

    it('labels the idle button "Load more" and marks it busy while a page is in flight', () => {
      expect(loadMoreButton()?.textContent?.trim()).toBe('Load more');

      loadMoreButton()!.click();
      fixture.detectChanges();

      expect(loadMoreButton()?.getAttribute('aria-busy')).toBe('true');
      expect(loadMoreButton()?.getAttribute('aria-disabled')).toBe('true');
      expect(loadMoreButton()?.textContent?.trim()).toBe('Loading…');
      expectFilter().flush(paged([]));
    });

    it('sends the cursor in the body exactly as received, then appends', () => {
      loadMoreButton()!.click();

      const req = expectFilter();
      expect(req.request.body).toEqual({
        pageSize: 20,
        lastSeenId: CURSOR_ID,
        lastSeenUpdatedAt: CURSOR_AT,
      });
      expect(req.request.params.keys()).toEqual([]);
      req.flush(paged([vacancy('c'), vacancy('d')], { totalCount: 4 }));
      fixture.detectChanges();

      expect(titles()).toEqual(['Vacancy a', 'Vacancy b', 'Vacancy c', 'Vacancy d']);
      expect(announcement()).toBe('Showing 4 of 4 vacancies');
    });

    it('uses the cursor from the latest page for the page after that', () => {
      loadMoreButton()!.click();
      expectFilter().flush(
        paged([vacancy('c')], {
          hasNext: true,
          lastSeenId: 'next-id',
          lastSeenUpdatedAt: '2026-10-01T00:00:00Z',
        }),
      );
      fixture.detectChanges();

      loadMoreButton()!.click();
      const req = expectFilter();
      expect(req.request.body).toEqual({
        pageSize: 20,
        lastSeenId: 'next-id',
        lastSeenUpdatedAt: '2026-10-01T00:00:00Z',
      });
      req.flush(paged([]));
    });

    it('does not render a vacancy twice when a later page repeats an id', () => {
      loadMoreButton()!.click();
      expectFilter().flush(paged([vacancy('b'), vacancy('c'), vacancy('c')]));
      fixture.detectChanges();

      expect(viewLinks()).toEqual(['/vacancies/a', '/vacancies/b', '/vacancies/c']);
    });

    it('keeps id-less vacancies from a later page (they cannot be deduped)', () => {
      loadMoreButton()!.click();
      expectFilter().flush(paged([{ title: 'No id one' }, { title: 'No id two' }]));
      fixture.detectChanges();

      expect(cards()).toHaveLength(4);
      expect(el().innerHTML).not.toContain('/vacancies/undefined');
    });

    it('sends exactly one request when load-more is triggered twice while in flight', () => {
      page.loadMore();
      page.loadMore();

      const reqs = httpMock.match((req) => req.url === filterUrl);
      expect(reqs).toHaveLength(1);
      reqs[0].flush(paged([]));
    });

    it('keeps the loaded cards and shows an inline error with "Try again" when load-more fails', () => {
      loadMoreButton()!.focus();
      loadMoreButton()!.click();
      serverError(expectFilter());
      fixture.detectChanges();

      expect(cards()).toHaveLength(2);
      expect(el().querySelector('ui-error-state')).toBeNull();
      const alert = el().querySelector('[data-testid="load-more-error"]');
      expect(alert?.getAttribute('role')).toBe('alert');
      expect(alert?.textContent).toContain("Couldn't load more vacancies");
      expect(loadMoreButton()?.textContent?.trim()).toBe('Try again');
      expect(document.activeElement).toBe(loadMoreButton());

      loadMoreButton()!.click();
      const retry = expectFilter();
      expect(retry.request.body.lastSeenId).toBe(CURSOR_ID);
      retry.flush(paged([vacancy('c')], { totalCount: 3 }));
      fixture.detectChanges();

      expect(cards()).toHaveLength(3);
      expect(el().querySelector('[data-testid="load-more-error"]')).toBeNull();
    });

    it('leaves focus on the button after a non-final page', () => {
      loadMoreButton()!.focus();
      loadMoreButton()!.click();
      expectFilter().flush(paged([vacancy('c')], { hasNext: true }));
      settle();

      expect(loadMoreButton()).not.toBeNull();
      expect(document.activeElement).toBe(loadMoreButton());
    });

    it('removes the button after the last page and focuses the first new card', () => {
      loadMoreButton()!.focus();
      loadMoreButton()!.click();
      expectFilter().flush(paged([vacancy('c'), vacancy('d')]));
      settle();

      expect(loadMoreButton()).toBeNull();
      expect(document.activeElement).toBe(cards()[2].querySelector('a'));
    });

    it('focuses the last card when the final page adds nothing new', () => {
      loadMoreButton()!.focus();
      loadMoreButton()!.click();
      expectFilter().flush(paged([vacancy('a'), vacancy('b')]));
      settle();

      expect(loadMoreButton()).toBeNull();
      expect(document.activeElement).toBe(cards()[1].querySelector('a'));
    });

    it('does not steal focus if the user moved it elsewhere while loading', () => {
      const elsewhere = addStray(document.createElement('button'));
      loadMoreButton()!.click();
      elsewhere.focus();

      expectFilter().flush(paged([vacancy('c')]));
      settle();

      expect(document.activeElement).toBe(elsewhere);
    });
  });

  describe('cursor edge cases', () => {
    it('shows no load-more when hasNext is true but a cursor field is null', () => {
      expectFilter().flush(
        paged([vacancy('a')], { hasNext: true, lastSeenId: CURSOR_ID, lastSeenUpdatedAt: null }),
      );
      fixture.detectChanges();

      expect(loadMoreButton()).toBeNull();
      expect(cards()).toHaveLength(1);
    });

    it('loadMore() is a no-op with no cursor', () => {
      expectFilter().flush(paged([vacancy('a')]));
      fixture.detectChanges();

      page.loadMore();

      httpMock.expectNone((req) => req.url === filterUrl);
    });
  });

  describe('teardown', () => {
    it('cancels the in-flight first load when the page is destroyed', () => {
      const req = expectFilter();

      fixture.destroy();

      expect(req.cancelled).toBe(true);
    });

    it('cancels an in-flight load-more when the page is destroyed', () => {
      expectFilter().flush(paged([vacancy('a')], { hasNext: true }));
      fixture.detectChanges();
      loadMoreButton()!.click();
      const req = expectFilter();

      fixture.destroy();

      expect(req.cancelled).toBe(true);
    });
  });
});
