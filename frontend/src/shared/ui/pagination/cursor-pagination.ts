import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

/**
 * Load-more control for cursor-paginated lists (never page numbers, AR16).
 *
 * Renders nothing when there is no next page. The host owns the cursor and the
 * fetch; this component only reports the user's intent and reflects whether a
 * request is already in flight. While loading the button is `aria-disabled`
 * rather than `disabled`, so it keeps keyboard focus instead of dropping it to
 * `<body>` mid-request.
 */
@Component({
  selector: 'ui-cursor-pagination',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (hasNext()) {
      <div class="mt-6 flex justify-center">
        <button
          type="button"
          class="inline-flex items-center rounded-md border border-border-default bg-surface px-4 py-2 text-md font-medium text-text-primary transition-colors duration-standard hover:bg-canvas focus-visible:outline focus-visible:outline-2 focus-visible:outline-border-focus aria-disabled:cursor-not-allowed aria-disabled:opacity-60 aria-disabled:hover:bg-surface"
          [attr.aria-disabled]="loading()"
          [attr.aria-busy]="loading()"
          (click)="onClick()"
        >
          {{ loading() ? 'Loading…' : label() }}
        </button>
      </div>
    }
  `,
})
export class CursorPaginationComponent {
  /** Whether the server reported another page after the last one loaded. */
  readonly hasNext = input(false);
  /** Whether a load-more request is currently in flight. */
  readonly loading = input(false);
  /** Button label while idle. */
  readonly label = input('Load more');

  /** Emitted when the user asks for the next page. Never emitted while {@link loading}. */
  readonly loadMore = output<void>();

  /** Forwards a click as {@link loadMore}, guarding against a double request. */
  onClick(): void {
    if (!this.loading()) {
      this.loadMore.emit();
    }
  }
}
