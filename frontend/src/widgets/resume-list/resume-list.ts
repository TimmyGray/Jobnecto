import { ChangeDetectionStrategy, Component, ElementRef, inject, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import type { ResumeResult } from '@entities/resume';
import { ResumeCardComponent } from './resume-card';

/**
 * Presentational list of résumé cards: one column on small screens, two from
 * `md` up, never a table (UX-DR16).
 *
 * It owns no fetching or pagination — the page hands it the accumulated items.
 * A résumé without an `id` renders as a plain card rather than a link, so the
 * list can never navigate to `/resumes/undefined`.
 */
@Component({
  selector: 'widget-resume-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, ResumeCardComponent],
  template: `
    <ul class="grid gap-3 md:grid-cols-2">
      @for (resume of items(); track resume.id || $index) {
        <li class="min-w-0">
          @if (resume.id) {
            <a
              data-testid="resume-card"
              [routerLink]="['/resumes', resume.id]"
              class="block rounded-md border border-border-default bg-surface p-4 transition-colors duration-standard hover:bg-canvas focus-visible:outline focus-visible:outline-2 focus-visible:outline-border-focus"
            >
              <widget-resume-card [resume]="resume" />
            </a>
          } @else {
            <div
              data-testid="resume-card"
              tabindex="-1"
              class="block rounded-md border border-border-default bg-surface p-4 focus-visible:outline focus-visible:outline-2 focus-visible:outline-border-focus"
            >
              <widget-resume-card [resume]="resume" />
            </div>
          }
        </li>
      }
    </ul>
  `,
})
export class ResumeListComponent {
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);

  /** The résumés to render, already ordered by the server and accumulated by the page. */
  readonly items = input<ResumeResult[]>([]);

  /**
   * Moves keyboard focus to the card at {@link index}. The page calls this
   * after a load-more so focus is not dropped when the button that was just
   * activated disappears at the end of the list.
   * @param index Zero-based position in {@link items}.
   */
  focusCard(index: number): void {
    const cards = this.host.nativeElement.querySelectorAll<HTMLElement>(':scope > ul > li > *');
    cards.item(index)?.focus();
  }
}
