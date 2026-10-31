import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import type { VacancyListItem } from '@entities/vacancy';
import { humanizeEnumMember } from '@shared/lib';
import { MatchScoreStubComponent } from '@shared/ui';

/** How many skill chips a card shows before collapsing the rest into "+N". */
const MAX_SKILLS = 5;

/** Keeps a value only when it is a non-blank string, trimmed. */
function present(value: string | null | undefined): string | null {
  return typeof value === 'string' && value.trim().length > 0 ? value.trim() : null;
}

/**
 * One vacancy on the board: title, company · location, skill chips, work-type
 * chip, the match-score placeholder, and the Generate / View actions.
 *
 * The card is an `<article>` rather than a link: it holds two actions, and
 * wrapping them in a third link would nest interactive content. A vacancy with
 * no id renders without actions (and is focusable programmatically only), so
 * the board can never link to `/vacancies/undefined`.
 */
@Component({
  selector: 'widget-vacancy-card',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, MatchScoreStubComponent],
  host: { class: 'block h-full' },
  template: `
    <article
      [attr.tabindex]="vacancy().id ? null : -1"
      class="flex h-full flex-col rounded-md border border-border-default bg-surface p-4 focus-visible:outline focus-visible:outline-2 focus-visible:outline-border-focus"
    >
      <h2 class="break-words text-md font-medium text-text-primary">{{ title() }}</h2>
      @if (meta(); as meta) {
        <p data-testid="vacancy-meta" class="mt-1 font-mono text-xs text-text-muted">{{ meta }}</p>
      }

      <div class="mt-3 flex flex-wrap items-center gap-2">
        @if (workType(); as workType) {
          <span
            data-testid="vacancy-work-type"
            class="rounded-pill bg-canvas px-2 py-1 text-xs text-text-secondary"
            >{{ workType }}</span
          >
        }
        <ui-match-score-stub />
      </div>

      @if (visibleSkills().length > 0) {
        <ul class="mt-3 flex flex-wrap gap-2" aria-label="Skills">
          @for (skill of visibleSkills(); track $index) {
            <li
              data-testid="vacancy-skill"
              class="max-w-full break-all rounded-pill border border-border-default px-2 py-1 text-xs text-text-secondary"
            >
              {{ skill }}
            </li>
          }
          @if (hiddenSkillCount() > 0) {
            <li
              data-testid="vacancy-skill-overflow"
              class="rounded-pill border border-border-default px-2 py-1 text-xs text-text-muted"
            >
              <span aria-hidden="true">+{{ hiddenSkillCount() }}</span>
              <span class="sr-only"
                >and {{ hiddenSkillCount() }} more {{ hiddenSkillCount() === 1 ? 'skill' : 'skills' }}</span
              >
            </li>
          }
        </ul>
      }

      @if (vacancy().id; as id) {
        <div class="mt-auto flex flex-wrap gap-2 pt-4">
          <a
            data-testid="vacancy-generate"
            [routerLink]="['/vacancies', id]"
            [attr.aria-label]="'Generate cover letter for ' + title()"
            class="inline-flex min-h-[44px] items-center justify-center rounded-md px-4 text-md font-medium transition-colors duration-standard focus-visible:outline focus-visible:outline-2 focus-visible:outline-border-focus bg-action-primary text-text-inverse hover:bg-action-primary-hover"
            >Generate</a
          >
          <a
            data-testid="vacancy-view"
            [routerLink]="['/vacancies', id]"
            [attr.aria-label]="'View ' + title()"
            class="inline-flex min-h-[44px] items-center justify-center rounded-md px-4 text-md font-medium transition-colors duration-standard focus-visible:outline focus-visible:outline-2 focus-visible:outline-border-focus border border-border-strong bg-surface text-text-primary hover:bg-canvas"
            >View</a
          >
        </div>
      }
    </article>
  `,
})
export class VacancyCardComponent {
  /** The vacancy this card describes. */
  readonly vacancy = input.required<VacancyListItem>();

  /** The card heading, with a placeholder for a missing or whitespace-only title. */
  protected readonly title = computed(() => present(this.vacancy().title) ?? 'Untitled vacancy');

  /** "Company · Location", skipping absent parts so no stray separator renders. */
  protected readonly meta = computed(() => {
    const location = present(this.vacancy().location);
    return [present(this.vacancy().company), location && humanizeEnumMember(location)]
      .filter((part): part is string => part !== null)
      .join(' · ');
  });

  /** Humanized work-location type, or null when the vacancy has none. */
  protected readonly workType = computed(() => {
    const value = present(this.vacancy().workLocationType);
    return value && humanizeEnumMember(value);
  });

  /** Skills with blank entries dropped, so they neither render nor inflate the "+N" count. */
  private readonly usableSkills = computed(() =>
    (this.vacancy().skills ?? []).filter((s) => typeof s === 'string' && s.trim().length > 0),
  );

  /** The skills shown as chips, capped at {@link MAX_SKILLS}. */
  protected readonly visibleSkills = computed(() => this.usableSkills().slice(0, MAX_SKILLS));

  /** How many skills the "+N" chip stands for. */
  protected readonly hiddenSkillCount = computed(() =>
    Math.max(0, this.usableSkills().length - MAX_SKILLS),
  );
}
