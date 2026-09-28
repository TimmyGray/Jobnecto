import { Injectable, signal } from '@angular/core';

/** Visual/semantic tone of a toast. */
export type ToastTone = 'success' | 'error';

/** A single active toast. */
export interface Toast {
  id: number;
  message: string;
  tone: ToastTone;
}

/** How long a toast stays visible before it auto-dismisses. */
const AUTO_DISMISS_MS = 4000;

let nextId = 0;

/**
 * Signal-backed transient-notification service (Story 2.1 Decision Record:
 * hand-rolled, no spartan-ng `sonner` yet). {@link ToastHostComponent} renders
 * whatever this service holds; nothing else needs to know a host exists.
 */
@Injectable({ providedIn: 'root' })
export class ToastService {
  private readonly toastsSignal = signal<Toast[]>([]);
  private readonly timers = new Map<number, ReturnType<typeof setTimeout>>();

  /** Read-only view of the currently active toasts. */
  readonly toasts = this.toastsSignal.asReadonly();

  /**
   * Shows a toast, auto-dismissing it after a fixed delay.
   * @param message The text to display.
   * @param tone Visual/semantic tone; defaults to `'success'`.
   * @returns The new toast's id, usable with {@link dismiss} to close it early.
   */
  show(message: string, tone: ToastTone = 'success'): number {
    const id = nextId++;
    this.toastsSignal.update((toasts) => [...toasts, { id, message, tone }]);
    this.timers.set(
      id,
      setTimeout(() => this.dismiss(id), AUTO_DISMISS_MS),
    );
    return id;
  }

  /** Removes a toast, clearing its pending auto-dismiss timer if it hasn't fired yet. */
  dismiss(id: number): void {
    this.toastsSignal.update((toasts) => toasts.filter((t) => t.id !== id));
    const timer = this.timers.get(id);
    if (timer !== undefined) {
      clearTimeout(timer);
      this.timers.delete(id);
    }
  }
}
