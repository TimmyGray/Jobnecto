import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/**
 * Standardized page-structure header: mono eyebrow label, one `h1` (project
 * the heading content, optionally including a serif-italic accent word),
 * optional subtitle, and an optional top-right primary action.
 * [AC2, UX-DR14, UX-DR15]
 *
 * Usage:
 *   <ui-page-header eyebrow="Dashboard" subtitle="You're all caught up.">
 *     Welcome, <span class="font-serif italic text-brand-accent">Ada</span>
 *     <button uiButton primaryAction>Create resume</button>
 *   </ui-page-header>
 */
@Component({
  selector: 'ui-page-header',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="mb-6 flex items-start justify-between gap-4">
      <div>
        <p
          data-testid="eyebrow"
          class="mb-2 font-mono text-xs uppercase tracking-wide text-text-muted"
        >
          {{ eyebrow() }}
        </p>
        <h1 class="text-xl font-semibold text-text-primary">
          <ng-content></ng-content>
        </h1>
        @if (subtitle()) {
          <p data-testid="subtitle" class="mt-2 text-md text-text-secondary">
            {{ subtitle() }}
          </p>
        }
      </div>
      <ng-content select="[primaryAction]"></ng-content>
    </div>
  `,
})
export class PageHeaderComponent {
  /** Mono uppercase eyebrow label shown above the heading. */
  readonly eyebrow = input.required<string>();
  /** Optional one-line subtitle shown below the heading. */
  readonly subtitle = input<string>('');
}
