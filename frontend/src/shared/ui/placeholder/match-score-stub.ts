import { ChangeDetectionStrategy, Component } from '@angular/core';

/**
 * Card-sized, honest placeholder for the AI match score (Tier-3 "coming soon").
 *
 * Matching is not built yet, so this never shows a number — even when the API
 * returns one — and is deliberately non-interactive and neutral-styled: it is a
 * roadmap signal, not an error or a control.
 */
@Component({
  selector: 'ui-match-score-stub',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'block' },
  template: `
    <div
      role="note"
      aria-label="AI match score — not yet available"
      class="inline-flex items-center rounded-md border border-dashed border-border-default p-1"
    >
      <span
        data-testid="match-score-pill"
        aria-hidden="true"
        class="rounded-pill bg-canvas px-2 py-1 font-mono text-xs text-text-muted"
        >AI match · soon</span
      >
      <!-- Some screen readers ignore aria-label on a role="note" in browse mode, so carry the text too. -->
      <span class="sr-only">AI match score — not yet available</span>
    </div>
  `,
})
export class MatchScoreStubComponent {}
