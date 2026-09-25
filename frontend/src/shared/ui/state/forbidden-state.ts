import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

/**
 * Standardized "forbidden" surface: a cross-user or otherwise-disallowed
 * access attempt (`403`), rendered as an explain + safe-recovery CTA per the
 * Journey 4 recovery model (never a raw status code, never the requested
 * resource's data). [Story 1.4 AC4, UX-DR12; AR15]
 */
@Component({
  selector: 'ui-forbidden-state',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex flex-col items-center text-center gap-3 p-8" role="status">
      <span class="text-xl" aria-hidden="true">🔒</span>
      <h2 class="text-lg font-semibold text-text-primary">{{ headline() }}</h2>
      @if (guidance()) {
        <p class="text-md text-text-secondary max-w-md">{{ guidance() }}</p>
      }
      <button
        type="button"
        class="mt-2 inline-flex items-center rounded-md border border-border-default bg-surface px-4 py-2 text-md font-medium text-text-primary transition-colors duration-standard hover:border-border-strong focus-visible:outline focus-visible:outline-2 focus-visible:outline-border-focus"
        (click)="recover.emit()"
      >
        {{ recoverLabel() }}
      </button>
    </div>
  `,
})
export class ForbiddenStateComponent {
  /** Headline (e.g. "You can't access this"). */
  readonly headline = input("You can't access this");
  /** Optional one-line guidance. Never names the resource owner or leaks cross-user data. */
  readonly guidance = input<string>(
    'This may belong to someone else, or you may not have permission.',
  );
  /** Recovery CTA label. */
  readonly recoverLabel = input('Back to safety');
  /** Emitted when the user clicks the recovery CTA. */
  readonly recover = output<void>();
}
