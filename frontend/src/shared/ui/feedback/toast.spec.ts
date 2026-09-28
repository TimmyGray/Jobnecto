import { describe, expect, it, beforeEach } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ToastHostComponent } from './toast';
import { ToastService } from './toast.service';

describe('ToastHostComponent', () => {
  let fixture: ComponentFixture<ToastHostComponent>;
  let service: ToastService;

  beforeEach(() => {
    fixture = TestBed.createComponent(ToastHostComponent);
    service = TestBed.inject(ToastService);
    fixture.detectChanges();
  });

  it('renders an aria-live polite region', () => {
    const region: HTMLElement = fixture.nativeElement.querySelector('[role="status"]');
    expect(region.getAttribute('aria-live')).toBe('polite');
  });

  it('renders nothing when there are no toasts', () => {
    expect(fixture.nativeElement.querySelectorAll('[data-testid="toast"]').length).toBe(0);
  });

  it('renders a toast when the service shows one', () => {
    service.show('Résumé created', 'success');
    fixture.detectChanges();

    const toasts = fixture.nativeElement.querySelectorAll('[data-testid="toast"]');
    expect(toasts.length).toBe(1);
    expect(toasts[0].textContent).toContain('Résumé created');
  });

  it('dismisses a toast when its close button is clicked', () => {
    service.show('Bye');
    fixture.detectChanges();

    const closeButton: HTMLButtonElement = fixture.nativeElement.querySelector(
      '[data-testid="toast-close"]',
    );
    closeButton.click();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelectorAll('[data-testid="toast"]').length).toBe(0);
  });

  it('applies the error tone styling class for an error toast', () => {
    service.show('Something broke', 'error');
    fixture.detectChanges();

    const toast: HTMLElement = fixture.nativeElement.querySelector('[data-testid="toast"]');
    expect(toast.className).toContain('danger');
  });
});
