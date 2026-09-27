import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { EducationService } from '@entities/education';
import { ResumeService } from '@entities/resume';
import type { ResumeResult } from '@entities/resume';
import { UserService } from '@entities/user';
import { PageHeaderComponent } from '@shared/ui';
import { ProfileCompletenessComponent, profileCompleteness } from '@widgets/profile-completeness';
import { RecentResumesComponent } from '@widgets/recent-resumes';
import { ResourceCountsComponent } from '@widgets/resource-counts';

/** Enough résumés for the "recent" panel; the counts come from `totalCount`. */
const FIRST_PAGE_SIZE = 5;

/**
 * Coerces a paged response's `totalCount` (typed `number | string`) to a real
 * number, or `null` if the body is missing or malformed. A bare `Number(...)`
 * would let a missing field become `NaN`, which fails every `=== null` guard
 * and renders as the literal text "NaN".
 * @param totalCount The raw `totalCount` field from a paged response.
 * @returns A finite number, or null when the value can't be coerced to one.
 */
function toCount(totalCount: unknown): number | null {
  const value = Number(totalCount);
  return Number.isFinite(value) ? value : null;
}

/**
 * `/dashboard` — the orientation view a user lands on after authenticating.
 *
 * Each region owns its own load state and is requested independently, so one
 * failing source degrades only its own widget and never the whole page. The
 * profile is normally already hydrated by `authGuard`; it is fetched here only
 * on the cold path where that signal is still empty.
 */
@Component({
  selector: 'page-dashboard',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink,
    PageHeaderComponent,
    ResourceCountsComponent,
    ProfileCompletenessComponent,
    RecentResumesComponent,
  ],
  templateUrl: './dashboard.page.html',
})
export class DashboardPage {
  private readonly userService = inject(UserService);
  private readonly resumeService = inject(ResumeService);
  private readonly educationService = inject(EducationService);

  /** The hydrated profile signal owned by the user entity service. */
  readonly profile = this.userService.profile;
  /** True while the cold-path profile fetch is in flight. */
  readonly profileLoading = signal(false);
  /** True when the cold-path profile fetch failed. */
  readonly profileFailed = signal(false);

  /** The most-recently-updated résumés, or null until the first page arrives. */
  readonly resumes = signal<ResumeResult[] | null>(null);
  /** Total résumés the user owns, from the paged response's `totalCount`. */
  readonly resumeCount = signal<number | null>(null);
  /** True while the résumé page is in flight. */
  readonly resumesLoading = signal(false);
  /** True when the résumé page failed to load. */
  readonly resumesFailed = signal(false);

  /** Total education records the user owns, from the paged response's `totalCount`. */
  readonly educationCount = signal<number | null>(null);
  /** True while the education page is in flight. */
  readonly educationsLoading = signal(false);
  /** True when the education page failed to load. */
  readonly educationsFailed = signal(false);

  /** Completeness percentage for the count tile, null while the profile is unknown. */
  readonly completenessPercent = computed(() => {
    const profile = this.profile();
    return profile === null ? null : profileCompleteness(profile).percent;
  });

  /** The name to greet the user by, falling back to a neutral greeting. */
  readonly greetingName = computed(() => this.profile()?.loginName ?? '');

  constructor() {
    // Three independent loads, deliberately not combined: a combinator would
    // let one failure cancel the others and break partial-failure tolerance.
    this.loadProfile();
    this.loadResumes();
    this.loadEducations();
  }

  /** Refetches the profile after a failed cold-path load. */
  retryProfile(): void {
    this.loadProfile(true);
  }

  /** Refetches the first page of résumés after a failure. */
  retryResumes(): void {
    this.loadResumes();
  }

  /** Refetches the first page of education records after a failure. */
  retryEducations(): void {
    this.loadEducations();
  }

  /**
   * Hydrates the profile only when it is not already in the signal. `authGuard`
   * fetches it before the route activates, so on every normal entry this is a
   * no-op rather than a duplicate round-trip.
   * @param force Whether to fetch even though a profile may already be cached.
   */
  private loadProfile(force = false): void {
    if (!force && this.profile() !== null) {
      return;
    }

    this.profileLoading.set(true);
    this.profileFailed.set(false);
    this.userService.fetchCurrentUser().subscribe({
      next: () => this.profileLoading.set(false),
      error: () => {
        this.profileLoading.set(false);
        this.profileFailed.set(true);
      },
    });
  }

  private loadResumes(): void {
    this.resumesLoading.set(true);
    this.resumesFailed.set(false);
    this.resumeService.list(FIRST_PAGE_SIZE).subscribe({
      next: (page) => {
        this.resumes.set(page.items);
        this.resumeCount.set(toCount(page.totalCount));
        this.resumesLoading.set(false);
      },
      error: () => {
        this.resumesLoading.set(false);
        this.resumesFailed.set(true);
      },
    });
  }

  private loadEducations(): void {
    this.educationsLoading.set(true);
    this.educationsFailed.set(false);
    this.educationService.list(FIRST_PAGE_SIZE).subscribe({
      next: (page) => {
        this.educationCount.set(toCount(page.totalCount));
        this.educationsLoading.set(false);
      },
      error: () => {
        this.educationsLoading.set(false);
        this.educationsFailed.set(true);
      },
    });
  }
}
