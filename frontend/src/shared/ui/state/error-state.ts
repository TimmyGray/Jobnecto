import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

/**
 * Standardized error-state surface with a Retry affordance. [AC5, UX-DR12]
 *
 * Uses `role="alert"` + `aria-live="assertive"` so the message is announced.
 * Emits `retry` when the user clicks Retry; the host decides what to refetch.
 */
@Component({
  selector: 'ui-error-state',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      class="flex flex-col items-center text-center gap-3 p-8"
      role="alert"
      aria-live="assertive"
    >
      <span class="text-xl" aria-hidden="true">⚠️</span>
      <h2 class="text-lg font-semibold text-text-primary">{{ headline() }}</h2>
      @if (detail()) {
        <p class="text-md text-text-secondary max-w-md">{{ detail() }}</p>
      }
      @if (showRetry()) {
        <button
          type="button"
          class="mt-2 inline-flex items-center rounded-md bg-action-primary px-4 py-2 text-md font-medium text-text-inverse transition-colors duration-standard hover:bg-action-primary-hover focus-visible:outline focus-visible:outline-2 focus-visible:outline-border-focus"
          (click)="retry.emit()"
        >
          {{ retryLabel() }}
        </button>
      }
    </div>
  `,
})
export class ErrorStateComponent {
  /** Headline for the error (e.g. "Something went wrong"). */
  readonly headline = input('Something went wrong');
  /** Optional plain-language detail. Never surface a raw status code. */
  readonly detail = input<string>();
  /** Whether to render the Retry button. */
  readonly showRetry = input(true);
  /** Retry button label. */
  readonly retryLabel = input('Retry');
  /** Emitted when the user clicks Retry. */
  readonly retry = output<void>();
}
