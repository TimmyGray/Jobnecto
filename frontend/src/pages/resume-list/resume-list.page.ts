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
import { ResumeService } from '@entities/resume';
import type { PagedResumes, ResumeResult } from '@entities/resume';
import { nextCursor, toCount } from '@shared/api';
import type { PageCursor } from '@shared/api';
import {
  CursorPaginationComponent,
  EmptyStateComponent,
  ErrorStateComponent,
  PageHeaderComponent,
  SkeletonComponent,
} from '@shared/ui';
import { ResumeListComponent } from '@widgets/resume-list';

/** How many placeholder cards the first-load skeleton reserves space for. */
const SKELETON_CARDS = [0, 1, 2, 3];

/**
 * Keeps the first occurrence of each id. Id-less résumés are always kept — they
 * render as non-link cards and cannot collide on a track key.
 * @param items Résumés to filter.
 * @param seen Ids already on screen; extended in place with the ids kept.
 * @returns The résumés whose id has not been seen before.
 */
function uniqueById(items: ResumeResult[], seen: Set<string>): ResumeResult[] {
  return items.filter((r) => {
    if (!r.id) {
      return true;
    }
    if (seen.has(r.id)) {
      return false;
    }
    seen.add(r.id);
    return true;
  });
}

/** Where the first load stands; load-more has its own flags so loaded cards are never discarded. */
type LoadStatus = 'loading' | 'ready' | 'error';

/**
 * `/resumes` — the caller's résumés as a cursor-paginated card list.
 *
 * Pages are accumulated here rather than read from `ResumeService.page()`,
 * which only ever holds the most recent page. Ownership is entirely
 * server-side (the auth cookie scopes the query); the client sends no user
 * identifier and renders exactly what comes back.
 */
@Component({
  selector: 'page-resume-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink,
    PageHeaderComponent,
    EmptyStateComponent,
    ErrorStateComponent,
    SkeletonComponent,
    CursorPaginationComponent,
    ResumeListComponent,
  ],
  templateUrl: './resume-list.page.html',
})
export class ResumeListPage {
  private readonly resumeService = inject(ResumeService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);

  private readonly list = viewChild(ResumeListComponent);

  /** Placeholder rows for the first-load skeleton. */
  protected readonly skeletonCards = SKELETON_CARDS;

  /** Every résumé loaded so far, in server order. */
  readonly items = signal<ResumeResult[]>([]);
  /** State of the first load. */
  readonly status = signal<LoadStatus>('loading');
  /** Cursor for the next page, or null when there is none. */
  readonly cursor = signal<PageCursor | null>(null);
  /** Total résumés the user owns, or null when the server value is unusable. */
  readonly totalCount = signal<number | null>(null);
  /** True while a load-more request is in flight. */
  readonly loadingMore = signal(false);
  /** True when the last load-more request failed. */
  readonly loadMoreFailed = signal(false);

  /** "N résumé(s)" subtitle, or an empty string while the total is unknown. */
  protected readonly subtitle = computed(() => {
    const total = this.totalCount();
    if (this.status() === 'loading') {
      // A non-breaking space reserves the subtitle line so the header does not jump when the total arrives.
      return '\u00a0';
    }
    if (this.status() !== 'ready' || total === null) {
      return '';
    }
    return total === 1 ? '1 résumé' : `${total} résumés`;
  });

  /** Screen-reader summary re-announced after every successful load. */
  protected readonly announcement = computed(() => {
    if (this.status() !== 'ready' || this.items().length === 0) {
      return '';
    }
    const shown = this.items().length;
    const total = this.totalCount();
    return total === null ? `Showing ${shown} résumés` : `Showing ${shown} of ${total} résumés`;
  });

  constructor() {
    this.loadFirstPage();
  }

  /** Reloads from the first page after a failed first load. */
  retry(): void {
    if (this.status() === 'loading') {
      return;
    }
    this.loadFirstPage();
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
    this.resumeService
      .list(undefined, cursor)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (page) => {
          const firstNewIndex = this.items().length;
          const seen = new Set(this.items().flatMap((r) => (r.id ? [r.id] : [])));
          const fresh = uniqueById(page.items, seen);
          this.items.update((current) => [...current, ...fresh]);
          this.applyPageMeta(page);
          this.loadingMore.set(false);
          this.restoreFocusAfterRender(
            fresh.length > 0 ? firstNewIndex : this.cursor() === null ? firstNewIndex - 1 : null,
          );
        },
        error: () => {
          this.loadingMore.set(false);
          this.loadMoreFailed.set(true);
        },
      });
  }

  /**
   * The Load more button the user just used may disappear with the last page,
   * which would drop focus to <body>; land on a card instead. Skipped when the
   * user has since moved focus somewhere else, so a slow response never steals it.
   * @param index Card to focus, or null to leave focus alone.
   */
  private restoreFocusAfterRender(index: number | null): void {
    if (index === null || index < 0) {
      return;
    }
    afterNextRender(
      () => {
        const active = document.activeElement;
        const pagination = this.host.nativeElement.querySelector('ui-cursor-pagination');
        if (!active || active === document.body || pagination?.contains(active)) {
          this.list()?.focusCard(index);
        }
      },
      { injector: this.injector },
    );
  }

  private loadFirstPage(): void {
    this.status.set('loading');
    this.resumeService
      .list()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (page) => {
          this.items.set(uniqueById(page.items, new Set()));
          this.applyPageMeta(page);
          this.status.set('ready');
        },
        error: () => this.status.set('error'),
      });
  }

  /** Records the cursor and total from a page that just loaded. */
  private applyPageMeta(page: PagedResumes): void {
    this.cursor.set(nextCursor(page));
    this.totalCount.set(toCount(page.totalCount));
  }
}
