import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { humanizeEnumMember } from '@entities/resume';
import type { ResumeResult } from '@entities/resume';

/** How many skill chips a card shows before collapsing the rest into "+N". */
const MAX_SKILLS = 5;

/**
 * The content of one résumé card: title, meta line, skill chips, updated date.
 *
 * The surrounding link (or plain container, for a résumé with no id) belongs to
 * {@link ResumeListComponent}; keeping the body here gives the template a typed
 * input instead of an untyped `ng-template` context.
 */
@Component({
  selector: 'widget-resume-card',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe],
  host: { class: 'block' },
  template: `
    <h2 class="break-words text-md font-medium text-text-primary">{{ title() }}</h2>
    @if (meta(); as meta) {
      <p data-testid="resume-meta" class="mt-1 font-mono text-xs text-text-muted">{{ meta }}</p>
    }
    @if (visibleSkills().length > 0) {
      <ul class="mt-3 flex flex-wrap gap-2" aria-label="Skills">
        @for (skill of visibleSkills(); track $index) {
          <li
            data-testid="resume-skill"
            class="max-w-full break-all rounded-pill border border-border-default px-2 py-1 text-xs text-text-secondary"
          >
            {{ skill }}
          </li>
        }
        @if (hiddenSkillCount() > 0) {
          <li
            data-testid="resume-skill-overflow"
            class="rounded-pill border border-border-default px-2 py-1 text-xs text-text-muted"
          >
            <span aria-hidden="true">+{{ hiddenSkillCount() }}</span>
            <span class="sr-only">and {{ hiddenSkillCount() }} more skills</span>
          </li>
        }
      </ul>
    }
    @if (resume().updatedAt) {
      <p data-testid="resume-updated" class="mt-3 text-xs text-text-muted">
        Updated {{ resume().updatedAt | date: 'mediumDate' }}
      </p>
    }
  `,
})
export class ResumeCardComponent {
  /** The résumé this card describes. */
  readonly resume = input.required<ResumeResult>();

  /** The card heading, with a placeholder for a missing or whitespace-only title. */
  protected readonly title = computed(() => this.resume().title?.trim() || 'Untitled resume');

  /**
   * Joins the present meta fields with a separator, skipping absent ones so no
   * stray separator is ever rendered.
   */
  protected readonly meta = computed(() =>
    [this.resume().experience, this.resume().workLocationType]
      .filter((part): part is string => typeof part === 'string' && part.trim().length > 0)
      .map((part) => humanizeEnumMember(part.trim()))
      .join(' · '),
  );

  /** Skills with blank entries dropped, so they neither render nor inflate the "+N" count. */
  private readonly usableSkills = computed(() =>
    (this.resume().skills ?? []).filter((s) => typeof s === 'string' && s.trim().length > 0),
  );

  /** The skills shown as chips, capped at {@link MAX_SKILLS}. */
  protected readonly visibleSkills = computed(() => this.usableSkills().slice(0, MAX_SKILLS));

  /** How many skills the "+N" chip stands for. */
  protected readonly hiddenSkillCount = computed(() =>
    Math.max(0, this.usableSkills().length - MAX_SKILLS),
  );
}
