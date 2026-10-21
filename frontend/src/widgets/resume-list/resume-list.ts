import { DatePipe, NgTemplateOutlet } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  inject,
  input,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { humanizeEnumMember } from '@entities/resume';
import type { ResumeResult } from '@entities/resume';

/** How many skill chips a card shows before collapsing the rest into "+N". */
const MAX_SKILLS = 5;

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
  imports: [RouterLink, DatePipe, NgTemplateOutlet],
  template: `
    <ul class="grid gap-3 md:grid-cols-2">
      @for (resume of items(); track resume.id || $index) {
        <li class="min-w-0">
          @if (resume.id) {
            <a
              data-testid="resume-card"
              [routerLink]="['/resumes', resume.id]"
              class="block rounded-md border border-border-default bg-surface p-4 transition-colors duration-standard focus-visible:outline focus-visible:outline-2 focus-visible:outline-border-focus hover:bg-canvas"
            >
              <ng-container *ngTemplateOutlet="body; context: { $implicit: resume }" />
            </a>
          } @else {
            <div data-testid="resume-card" tabindex="-1" class="block rounded-md border border-border-default bg-surface p-4 transition-colors duration-standard focus-visible:outline focus-visible:outline-2 focus-visible:outline-border-focus">
              <ng-container *ngTemplateOutlet="body; context: { $implicit: resume }" />
            </div>
          }
        </li>
      }
    </ul>

    <ng-template #body let-resume>
      <h2 class="break-words text-md font-medium text-text-primary">
        {{ displayTitle(resume) }}
      </h2>
      @if (metaOf(resume); as meta) {
        <p data-testid="resume-meta" class="mt-1 font-mono text-xs text-text-muted">
          {{ meta }}
        </p>
      }
      @if (visibleSkills(resume).length > 0) {
        <ul class="mt-3 flex flex-wrap gap-2" aria-label="Skills">
          @for (skill of visibleSkills(resume); track $index) {
            <li
              data-testid="resume-skill"
              class="max-w-full break-all rounded-full border border-border-default px-2 py-0.5 text-xs text-text-secondary"
            >
              {{ skill }}
            </li>
          }
          @if (hiddenSkillCount(resume) > 0) {
            <li
              data-testid="resume-skill"
              class="rounded-full border border-border-default px-2 py-0.5 text-xs text-text-muted"
            >
              +{{ hiddenSkillCount(resume) }}
            </li>
          }
        </ul>
      }
      @if (resume.updatedAt) {
        <p data-testid="resume-updated" class="mt-3 text-xs text-text-muted">
          Updated {{ resume.updatedAt | date: 'mediumDate' }}
        </p>
      }
    </ng-template>
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
    const cards = this.host.nativeElement.querySelectorAll<HTMLElement>(
      ':scope > ul > li > *',
    );
    cards.item(index)?.focus();
  }

  /**
   * Joins the present meta fields with a separator, skipping absent ones so no
   * stray separator is ever rendered.
   * @param resume The résumé whose meta line is being built.
   * @returns The meta line, or an empty string when there is nothing to show.
   */
  protected metaOf(resume: ResumeResult): string {
    return [resume.experience, resume.workLocationType]
      .filter((part): part is string => typeof part === 'string' && part.trim().length > 0)
      .map((part) => humanizeEnumMember(part.trim()))
      .join(' · ');
  }

  /** The card heading, with a placeholder for a missing or whitespace-only title. */
  protected displayTitle(resume: ResumeResult): string {
    return resume.title?.trim() || 'Untitled resume';
  }

  /** The skills shown as chips, capped at {@link MAX_SKILLS}; blank entries are dropped. */
  protected visibleSkills(resume: ResumeResult): string[] {
    return this.usableSkills(resume).slice(0, MAX_SKILLS);
  }

  /** How many skills the "+N" chip stands for. */
  protected hiddenSkillCount(resume: ResumeResult): number {
    return Math.max(0, this.usableSkills(resume).length - MAX_SKILLS);
  }

  private usableSkills(resume: ResumeResult): string[] {
    return (resume.skills ?? []).filter((s) => typeof s === 'string' && s.trim().length > 0);
  }
}
