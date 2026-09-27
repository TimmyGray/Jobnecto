import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { SkeletonComponent } from '@shared/ui';

/**
 * The dashboard's resource-count row: profile completeness, résumé and
 * education totals, and a placeholder for cover letters.
 *
 * Each tile reflects its own source's state, so one failed load degrades one
 * tile and leaves the rest showing real numbers. Counts come from the paged
 * responses' `totalCount`, not from the number of items on the first page.
 */
@Component({
  selector: 'widget-resource-counts',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [SkeletonComponent],
  template: `
    <dl
      class="grid grid-cols-2 gap-px overflow-hidden rounded-lg border border-border-default bg-border-default lg:grid-cols-4"
    >
      <div data-testid="tile-profile" class="bg-surface p-5">
        <dt
          class="mb-2 font-mono text-xs uppercase tracking-wide text-text-muted"
        >
          Profile complete
        </dt>
        <dd class="text-xxl font-semibold text-text-primary">
          @if (profileLoading()) {
            <ui-skeleton [lines]="1" height="32px" width="60%" lastWidth="60%" />
          } @else if (profileFailed() || completenessPercent() === null) {
            <span class="text-md font-normal text-text-secondary">Unavailable</span>
          } @else {
            {{ completenessPercent() }}%
          }
        </dd>
      </div>

      <div data-testid="tile-resumes" class="bg-surface p-5">
        <dt
          class="mb-2 font-mono text-xs uppercase tracking-wide text-text-muted"
        >
          Active resumes
        </dt>
        <dd class="text-xxl font-semibold text-text-primary">
          @if (resumesLoading()) {
            <ui-skeleton [lines]="1" height="32px" width="50%" lastWidth="50%" />
          } @else if (resumesFailed() || resumeCount() === null) {
            <span class="text-md font-normal text-text-secondary">Unavailable</span>
          } @else {
            {{ resumeCount() }}
          }
        </dd>
      </div>

      <div data-testid="tile-educations" class="bg-surface p-5">
        <dt
          class="mb-2 font-mono text-xs uppercase tracking-wide text-text-muted"
        >
          Education
        </dt>
        <dd class="text-xxl font-semibold text-text-primary">
          @if (educationsLoading()) {
            <ui-skeleton [lines]="1" height="32px" width="50%" lastWidth="50%" />
          } @else if (educationsFailed() || educationCount() === null) {
            <span class="text-md font-normal text-text-secondary">Unavailable</span>
            <button
              type="button"
              data-testid="retry-educations"
              class="ml-2 rounded-sm text-md font-normal text-brand-accent underline underline-offset-4 hover:text-brand-accent-hover focus-visible:outline focus-visible:outline-2 focus-visible:outline-border-focus"
              (click)="retryEducations.emit()"
            >
              Retry
            </button>
          } @else {
            {{ educationCount() }}
          }
        </dd>
      </div>

      <div data-testid="tile-cover-letters" class="bg-surface p-5">
        <dt
          class="mb-2 font-mono text-xs uppercase tracking-wide text-text-muted"
        >
          Cover letters
        </dt>
        <dd class="text-xxl font-semibold text-text-muted">
          <span aria-hidden="true">—</span>
          <span class="sr-only">not available yet</span>
        </dd>
      </div>
    </dl>
  `,
})
export class ResourceCountsComponent {
  /** Profile completeness as a whole percentage, or null while unknown. */
  readonly completenessPercent = input<number | null>(null);
  /** Whether the profile is still loading. */
  readonly profileLoading = input(false);
  /** Whether the profile load failed. */
  readonly profileFailed = input(false);

  /** Total résumés the caller owns (`totalCount`), or null while unknown. */
  readonly resumeCount = input<number | null>(null);
  /** Whether the résumé page is still loading. */
  readonly resumesLoading = input(false);
  /** Whether the résumé load failed. */
  readonly resumesFailed = input(false);

  /** Total education records the caller owns (`totalCount`), or null while unknown. */
  readonly educationCount = input<number | null>(null);
  /** Whether the education page is still loading. */
  readonly educationsLoading = input(false);
  /** Whether the education load failed. */
  readonly educationsFailed = input(false);

  /**
   * Emitted when the user retries the failed education load. This tile is the
   * education count's only surface, so unlike the profile and résumé tiles —
   * whose panels below carry their own Retry — the retry belongs here.
   */
  readonly retryEducations = output<void>();
}
