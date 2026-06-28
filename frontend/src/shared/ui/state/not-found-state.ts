import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

/**
 * Standardized not-found surface with a back-to-list affordance. Used on detail
 * routes when the server returns 404. [AC5, UX-DR12; FE Guide §8.2]
 */
@Component({
  selector: 'ui-not-found-state',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex flex-col items-center text-center gap-3 p-8" role="status">
      <span class="text-xl" aria-hidden="true">🔍</span>
      <h2 class="text-lg font-semibold text-text-primary">{{ headline() }}</h2>
      @if (guidance()) {
        <p class="text-md text-text-secondary max-w-md">{{ guidance() }}</p>
      }
      <button
        type="button"
        class="mt-2 inline-flex items-center rounded-md border border-border-default bg-surface px-4 py-2 text-md font-medium text-text-primary transition-colors duration-standard hover:border-border-strong focus-visible:outline focus-visible:outline-2 focus-visible:outline-border-focus"
        (click)="back.emit()"
      >
        {{ backLabel() }}
      </button>
    </div>
  `,
})
export class NotFoundStateComponent {
  /** Headline (e.g. "We couldn't find that"). */
  readonly headline = input("We couldn't find that");
  /** Optional one-line guidance. */
  readonly guidance = input<string>('It may have been removed or never existed.');
  /** Back button label. */
  readonly backLabel = input('Back to list');
  /** Emitted when the user clicks the back affordance. */
  readonly back = output<void>();
}
