import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';
import type { ResumeResult } from '@entities/resume';
import { EmptyStateComponent, ErrorStateComponent, SkeletonComponent } from '@shared/ui';

/** How many résumés the panel shows before deferring to the full list. */
const MAX_VISIBLE = 5;

/**
 * "Recent resumes" panel: the most-recently-updated résumés, or this one
 * region's own loading, empty, or error state.
 *
 * The panel owns no fetching — the page hands it data and handles {@link retry} —
 * so a failure here degrades this panel alone and leaves the rest of the
 * dashboard intact.
 */
@Component({
  selector: 'widget-recent-resumes',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, EmptyStateComponent, ErrorStateComponent, SkeletonComponent],
  template: `
    <section
      class="h-full rounded-lg border border-border-default bg-surface p-6"
      aria-labelledby="recent-resumes-heading"
    >
      <h2 id="recent-resumes-heading" class="text-lg font-semibold text-text-primary">
        Recent resumes
      </h2>

      @if (failed()) {
        <ui-error-state
          headline="Couldn't load your resumes"
          detail="The rest of your dashboard is still up to date."
          (retry)="retry.emit()"
        />
      } @else if (loading()) {
        <div class="mt-6">
          <ui-skeleton [lines]="6" height="24px" />
        </div>
      } @else if (visible().length === 0) {
        <ui-empty-state
          icon="📄"
          headline="No resumes yet"
          guidance="Your resume is what generation grounds a cover letter in."
        >
          <a
            routerLink="/resumes"
            class="inline-flex items-center rounded-md border border-border-default px-4 py-2 text-md font-medium text-text-primary transition-colors duration-standard hover:bg-canvas focus-visible:outline focus-visible:outline-2 focus-visible:outline-border-focus"
            >Create your first resume</a
          >
        </ui-empty-state>
      } @else {
        <ul class="mt-4 flex flex-col gap-3">
          @for (resume of visible(); track resume.id ?? $index) {
            <li>
              <a
                data-testid="resume-card"
                [routerLink]="['/resumes', resume.id]"
                class="block rounded-md border border-border-default p-4 transition-colors duration-standard hover:bg-canvas focus-visible:outline focus-visible:outline-2 focus-visible:outline-border-focus"
              >
                <h3 class="text-md font-medium text-text-primary">
                  {{ resume.title || 'Untitled resume' }}
                </h3>
                @if (metaOf(resume); as meta) {
                  <p
                    data-testid="resume-meta"
                    class="mt-1 font-mono text-xs text-text-muted"
                  >
                    {{ meta }}
                  </p>
                }
              </a>
            </li>
          }
        </ul>
      }
    </section>
  `,
})
export class RecentResumesComponent {
  /** The résumés to show, or null while unknown. Already ordered by the server. */
  readonly resumes = input<ResumeResult[] | null>(null);
  /** Whether the résumé page is still loading. */
  readonly loading = input(false);
  /** Whether the résumé load failed. */
  readonly failed = input(false);

  /** Emitted when the user asks to retry the failed résumé load. */
  readonly retry = output<void>();

  /** The résumés actually rendered, capped at {@link MAX_VISIBLE}. */
  protected readonly visible = computed(() => (this.resumes() ?? []).slice(0, MAX_VISIBLE));

  /**
   * Joins the present meta fields with a separator, skipping absent ones so no
   * stray separator is ever rendered.
   * @param resume The résumé whose meta line is being built.
   * @returns The meta line, or an empty string when there is nothing to show.
   */
  protected metaOf(resume: ResumeResult): string {
    return [resume.experience, resume.workLocationType]
      .filter((part): part is string => typeof part === 'string' && part.trim().length > 0)
      .join(' · ');
  }
}
