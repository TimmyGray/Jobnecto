import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  effect,
  input,
  output,
  untracked,
  viewChild,
} from '@angular/core';

let nextId = 0;

/**
 * Modal confirmation for destructive (or otherwise irreversible) actions —
 * never a one-click irreversible action (UX-DR15).
 *
 * Built on the native `<dialog>`: `showModal()` supplies the focus trap, the
 * inert background and top-layer stacking. The host owns the open state through
 * {@link open}; Escape, the backdrop and Cancel all emit {@link cancel} and the
 * host decides whether to close, so the dialog can never close itself behind
 * the host's back. Cancel is first in the DOM and takes initial focus so the
 * destructive action is never the default; when the dialog closes, focus goes
 * back to whatever opened it.
 *
 * While {@link busy}, every way out is ignored and Confirm is `aria-disabled`
 * (not `disabled`) so keyboard focus is not dropped mid-request.
 */
@Component({
  selector: 'ui-confirm-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <dialog
      #dialog
      class="m-auto w-full max-w-md rounded-lg border border-border-default bg-surface p-0 text-text-primary backdrop:bg-[color:var(--color-text-primary)] backdrop:opacity-40"
      [attr.aria-labelledby]="titleId"
      [attr.aria-describedby]="message() ? descriptionId : null"
      (cancel)="onNativeCancel($event)"
      (close)="onNativeClose()"
      (mousedown)="onDialogMouseDown($event)"
      (click)="onDialogClick($event)"
    >
      <div class="flex flex-col gap-3 p-6">
        <h2 [id]="titleId" class="text-lg font-semibold text-text-primary">{{ headline() }}</h2>
        <p class="sr-only" role="status" aria-live="polite">{{ busy() ? busyMessage() : '' }}</p>
        @if (message()) {
          <p [id]="descriptionId" class="text-md text-text-secondary">{{ message() }}</p>
        }
        <div class="mt-3 flex justify-end gap-3">
          <button
            #cancelButton
            type="button"
            autofocus
            class="inline-flex items-center rounded-md border border-border-default bg-surface px-4 py-2 text-md font-medium text-text-primary transition-colors duration-standard hover:bg-canvas focus-visible:outline focus-visible:outline-2 focus-visible:outline-border-focus"
            (click)="requestCancel()"
          >
            {{ cancelLabel() }}
          </button>
          <button
            type="button"
            [class]="confirmClasses()"
            [attr.aria-disabled]="busy()"
            [attr.aria-busy]="busy()"
            (click)="onConfirm()"
          >
            {{ confirmLabel() }}
          </button>
        </div>
      </div>
    </dialog>
  `,
})
export class ConfirmDialogComponent {
  /** Whether the dialog is showing. The host owns this; the dialog never closes itself. */
  readonly open = input(false);
  /** Short question or statement of the action, e.g. "Delete this résumé?". */
  readonly headline = input.required<string>();
  /** Optional one-line consequence, e.g. "This can't be undone from the app." */
  readonly message = input('');
  /** Label of the confirming button; swap it (e.g. "Deleting…") while {@link busy}. */
  readonly confirmLabel = input('Confirm');
  /** Label of the cancelling button. */
  readonly cancelLabel = input('Cancel');
  /** Styles the confirming button with the danger token instead of the primary one. */
  readonly destructive = input(false);
  /** True while the confirmed action is in flight; every exit and a second confirm are ignored. */
  readonly busy = input(false);
  /** Announced to screen readers while {@link busy}, e.g. "Deleting résumé…". */
  readonly busyMessage = input('Working…');
  /**
   * Element to give focus back to when the dialog closes. Pass it when the
   * opening control may not be `document.activeElement` at open time (Safari
   * and Firefox on macOS do not focus a button on click).
   */
  readonly returnFocusTo = input<HTMLElement | null>(null);

  /** Emitted when the user confirms. Never emitted while {@link busy}. */
  readonly confirm = output<void>();
  /** Emitted for Cancel, Escape and a backdrop click. Never emitted while {@link busy}. */
  readonly cancel = output<void>();

  protected readonly titleId = `ui-confirm-dialog-title-${nextId}`;
  protected readonly descriptionId = `ui-confirm-dialog-desc-${nextId++}`;

  private readonly dialog = viewChild.required<ElementRef<HTMLDialogElement>>('dialog');
  private readonly cancelButton = viewChild.required<ElementRef<HTMLButtonElement>>('cancelButton');
  private opener: HTMLElement | null = null;
  private pressStartedOnBackdrop = false;

  protected readonly confirmClasses = () =>
    [
      'inline-flex items-center rounded-md px-4 py-2 text-md font-medium text-text-inverse transition-colors duration-standard',
      'focus-visible:outline focus-visible:outline-2 focus-visible:outline-border-focus',
      'aria-disabled:cursor-not-allowed aria-disabled:opacity-60',
      this.destructive()
        ? 'bg-status-danger hover:opacity-90'
        : 'bg-action-primary hover:bg-action-primary-hover',
    ].join(' ');

  constructor() {
    effect(() => {
      const open = this.open();
      untracked(() => (open ? this.show() : this.hide()));
    });
  }

  /** Forwards the Confirm button as {@link confirm}, ignoring it while busy. */
  protected onConfirm(): void {
    if (!this.busy()) {
      this.confirm.emit();
    }
  }

  /** Emits {@link cancel} unless busy. */
  protected requestCancel(): void {
    if (!this.busy()) {
      this.cancel.emit();
    }
  }

  /**
   * Handles the native `cancel` event (Escape). It is always prevented so the
   * browser does not close the dialog on its own; the host closes it by
   * flipping {@link open}.
   */
  protected onNativeCancel(event: Event): void {
    event.preventDefault();
    this.requestCancel();
  }

  /**
   * The native `close` event. Our own {@link hide} also triggers it, but then
   * {@link open} is already false. If it fires while the host still thinks the
   * dialog is open, something closed it behind the host's back (Chrome closes a
   * dialog on a second Escape even though the first was prevented): report it as
   * a cancel, or put the dialog back if it must stay up because it is busy.
   */
  protected onNativeClose(): void {
    if (!this.open()) {
      return;
    }
    if (this.busy()) {
      this.show();
    } else {
      this.cancel.emit();
    }
  }

  /** Remembers where a press began, so a drag that starts inside and ends on the backdrop is not a backdrop click. */
  protected onDialogMouseDown(event: MouseEvent): void {
    this.pressStartedOnBackdrop = event.target === this.dialog().nativeElement;
  }

  /** A click that both began and ended on the dialog element itself landed on the backdrop, not the content. */
  protected onDialogClick(event: MouseEvent): void {
    const onBackdrop = event.target === this.dialog().nativeElement && this.pressStartedOnBackdrop;
    this.pressStartedOnBackdrop = false;
    if (onBackdrop) {
      this.requestCancel();
    }
  }

  private show(): void {
    const dialog = this.dialog().nativeElement;
    if (dialog.open) {
      return;
    }
    this.opener ??=
      this.returnFocusTo() ??
      (document.activeElement instanceof HTMLElement ? document.activeElement : null);
    dialog.showModal();
    this.cancelButton().nativeElement.focus();
  }

  private hide(): void {
    const dialog = this.dialog().nativeElement;
    if (!dialog.open) {
      return;
    }
    dialog.close();
    // The opener can have been removed while the dialog was up (e.g. the page swapped to an error state).
    if (this.opener?.isConnected) {
      this.opener.focus();
    }
    this.opener = null;
  }
}
