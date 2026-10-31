import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  Injector,
  afterNextRender,
  computed,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { VacancyService } from '@entities/vacancy';
import type { PagedVacancies, VacancyListItem } from '@entities/vacancy';
import { nextCursor, toCount } from '@shared/api';
import type { PageCursor } from '@shared/api';
import {
  CursorPaginationComponent,
  EmptyStateComponent,
  ErrorStateComponent,
  PageHeaderComponent,
  SkeletonComponent,
} from '@shared/ui';
import { VacancyBoardComponent } from '@widgets/vacancy-board';

/** How many placeholder cards the first-load skeleton reserves space for. */
const SKELETON_CARDS = [0, 1, 2, 3];

/**
 * Keeps the first occurrence of each id. Id-less vacancies are always kept — they
 * render without actions and cannot collide on a track key.
 * @param items Vacancies to filter.
 * @param seen Ids already on screen; extended in place with the ids kept.
 * @returns The vacancies whose id has not been seen before.
 */
function uniqueById(items: VacancyListItem[], seen: Set<string>): VacancyListItem[] {
  return items.filter((v) => {
    if (!v.id) {
      return true;
    }
    if (seen.has(v.id)) {
      return false;
    }
    seen.add(v.id);
    return true;
  });
}

/** Where the first load stands; load-more has its own flags so loaded cards are never discarded. */
type LoadStatus = 'loading' | 'ready' | 'error';

/**
 * `/vacancies` — the caller's vacancy board as a cursor-paginated card grid.
 *
 * Browse mode only: no criteria and no sort are sent, so the server's default
 * newest-first order applies. Pages are accumulated here; ownership is entirely
 * server-side (the auth cookie scopes the query) and the client renders exactly
 * what comes back.
 */
@Component({
  selector: 'page-vacancy-board',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink,
    PageHeaderComponent,
    EmptyStateComponent,
    ErrorStateComponent,
    SkeletonComponent,
    CursorPaginationComponent,
    VacancyBoardComponent,
  ],
  templateUrl: './vacancy-board.page.html',
})
export class VacancyBoardPage {
  private readonly vacancyService = inject(VacancyService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);

  private readonly board = viewChild(VacancyBoardComponent);
  private readonly skeleton = viewChild<ElementRef<HTMLElement>>('skeleton');

  /** Placeholder cards for the first-load skeleton. */
  protected readonly skeletonCards = SKELETON_CARDS;

  /** Every vacancy loaded so far, in server order. */
  readonly items = signal<VacancyListItem[]>([]);
  /** State of the first load. */
  readonly status = signal<LoadStatus>('loading');
  /** Cursor for the next page, or null when there is none. */
  readonly cursor = signal<PageCursor | null>(null);
  /** Total vacancies on the board, or null when the server value is unusable. */
  readonly totalCount = signal<number | null>(null);
  /** True while a load-more request is in flight. */
  readonly loadingMore = signal(false);
  /** True when the last load-more request failed. */
  readonly loadMoreFailed = signal(false);

  /** "N vacancy/vacancies" subtitle, or an empty string while the total is unknown. */
  protected readonly subtitle = computed(() => {
    const total = this.totalCount();
    if (this.status() === 'loading') {
      // A non-breaking space reserves the subtitle line so the header does not jump when the total arrives.
      return ' ';
    }
    if (this.status() !== 'ready' || total === null) {
      return '';
    }
    return total === 1 ? '1 vacancy' : `${total} vacancies`;
  });

  /**
   * Screen-reader summary of what is loaded. A polite live region only speaks
   * when this text changes, so a load that adds nothing new stays silent.
   */
  protected readonly announcement = computed(() => {
    if (this.status() !== 'ready' || this.items().length === 0) {
      return '';
    }
    const shown = this.items().length;
    const total = this.totalCount();
    const noun = (total ?? shown) === 1 ? 'vacancy' : 'vacancies';
    return total === null ? `Showing ${shown} ${noun}` : `Showing ${shown} of ${total} ${noun}`;
  });

  constructor() {
    this.loadFirstPage();
  }

  /**
   * Reloads from the first page after a failed first load. The Retry button is
   * destroyed by the state change, so focus moves to the loading region instead
   * of dropping to <body>.
   */
  retry(): void {
    if (this.status() === 'loading') {
      return;
    }
    this.loadFirstPage(true);
    this.focusWhenFree(() => this.skeleton()?.nativeElement.focus());
  }

  /**
   * Fetches the next page and appends it. A no-op while a request is already
   * in flight or when there is no next page. A failure leaves the loaded cards
   * in place and surfaces an inline retry.
   */
  loadMore(): void {
    const cursor = this.cursor();
    if (this.loadingMore() || cursor === null) {
      return;
    }

    this.loadingMore.set(true);
    this.loadMoreFailed.set(false);
    this.vacancyService
      .filter(undefined, cursor)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (page) => {
          const firstNewIndex = this.items().length;
          const seen = new Set(this.items().flatMap((v) => (v.id ? [v.id] : [])));
          const fresh = uniqueById(page.items, seen);
          this.items.update((current) => [...current, ...fresh]);
          this.applyPageMeta(page);
          this.loadingMore.set(false);
          if (this.cursor() === null) {
            // The Load more button is gone: land on the first new card, or on the
            // last existing one if this final page added nothing new.
            const target = fresh.length > 0 ? firstNewIndex : firstNewIndex - 1;
            this.focusWhenFree(() => this.board()?.focusCard(target));
          }
        },
        error: () => {
          this.loadingMore.set(false);
          this.loadMoreFailed.set(true);
        },
      });
  }

  /**
   * Runs {@link focus} after the next render, but only if focus has fallen to
   * <body> (the control the user used was removed). If the user has since moved
   * focus somewhere else, a slow response must not steal it.
   * @param focus Moves focus to the intended target.
   */
  private focusWhenFree(focus: () => void): void {
    afterNextRender(
      () => {
        const active = document.activeElement;
        if (!active || active === document.body) {
          focus();
        }
      },
      { injector: this.injector },
    );
  }

  /**
   * Loads the first page, resetting the view to its loading state.
   * @param restoreFocus True after a Retry: the focused skeleton is about to be
   * destroyed, so focus is handed on to whatever replaces it.
   */
  private loadFirstPage(restoreFocus = false): void {
    this.status.set('loading');
    this.vacancyService
      .filter()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (page) => {
          this.items.set(uniqueById(page.items, new Set()));
          this.applyPageMeta(page);
          this.status.set('ready');
          if (restoreFocus) {
            this.focusWhenFree(() => this.focusFirstContent());
          }
        },
        error: () => {
          this.status.set('error');
          if (restoreFocus) {
            this.focusWhenFree(() => this.focusIn('ui-error-state button'));
          }
        },
      });
  }

  /** Focuses the first card, or the empty state's call to action when there are none. */
  private focusFirstContent(): void {
    if (this.items().length > 0) {
      this.board()?.focusCard(0);
    } else {
      this.focusIn('[data-testid="board-empty"] a');
    }
  }

  /** Focuses the first element inside this page matching {@link selector}, if any. */
  private focusIn(selector: string): void {
    this.host.nativeElement.querySelector<HTMLElement>(selector)?.focus();
  }

  /** Records the cursor and total from a page that just loaded. */
  private applyPageMeta(page: PagedVacancies): void {
    this.cursor.set(nextCursor(page));
    this.totalCount.set(toCount(page.totalCount));
  }
}
