import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/**
 * Standardized empty-state surface: icon glyph + headline + one-line guidance
 * with a primary CTA content slot. [AC5, UX-DR12]
 *
 * Project content into the CTA slot:
 *   <ui-empty-state headline="No resumes yet" guidance="Create your first one.">
 *     <button uiButton>Create resume</button>
 *   </ui-empty-state>
 */
@Component({
  selector: 'ui-empty-state',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex flex-col items-center text-center gap-3 p-8" role="status">
      <span class="text-xl" aria-hidden="true">{{ icon() }}</span>
      <h2 class="text-lg font-semibold text-text-primary">{{ headline() }}</h2>
      @if (guidance()) {
        <p class="text-md text-text-secondary max-w-md">{{ guidance() }}</p>
      }
      <div class="mt-2">
        <ng-content></ng-content>
      </div>
    </div>
  `,
})
export class EmptyStateComponent {
  /** Decorative glyph shown above the headline. */
  readonly icon = input('📭');
  /** Required short headline (e.g. "No resumes yet"). */
  readonly headline = input.required<string>();
  /** Optional one-line guidance under the headline. */
  readonly guidance = input<string>();
}
