import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ToastService } from './toast.service';

/**
 * Renders the {@link ToastService}'s active toasts in an `aria-live="polite"`
 * region. Mounted locally by whichever page needs it (Story 2.1 Decision
 * Record: not global `AppShellComponent` chrome yet — promote it there once a
 * second consumer needs a toast).
 */
@Component({
  selector: 'ui-toast-host',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div role="status" aria-live="polite" class="pointer-events-none fixed bottom-4 right-4 z-toast flex flex-col gap-2">
      @for (toast of toastService.toasts(); track toast.id) {
        <div
          data-testid="toast"
          class="pointer-events-auto flex items-center gap-3 rounded-md border px-4 py-3 shadow-md"
          [class.border-status-success]="toast.tone === 'success'"
          [class.bg-surface]="true"
          [class.text-status-success]="toast.tone === 'success'"
          [class.border-status-danger]="toast.tone === 'error'"
          [class.text-status-danger]="toast.tone === 'error'"
        >
          <span class="text-sm font-medium">{{ toast.message }}</span>
          <button
            type="button"
            data-testid="toast-close"
            [attr.aria-label]="'Dismiss: ' + toast.message"
            (click)="toastService.dismiss(toast.id)"
            class="text-text-muted hover:text-text-primary"
          >
            &times;
          </button>
        </div>
      }
    </div>
  `,
})
export class ToastHostComponent {
  protected readonly toastService = inject(ToastService);
}
