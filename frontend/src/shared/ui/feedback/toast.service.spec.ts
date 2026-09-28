import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { ToastService } from './toast.service';

describe('ToastService', () => {
  let service: ToastService;

  beforeEach(() => {
    vi.useFakeTimers();
    TestBed.configureTestingModule({});
    service = TestBed.inject(ToastService);
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('starts with no toasts', () => {
    expect(service.toasts()).toEqual([]);
  });

  it('show() appends a toast with the given message and tone', () => {
    service.show('Résumé created', 'success');

    expect(service.toasts()).toHaveLength(1);
    expect(service.toasts()[0]).toMatchObject({ message: 'Résumé created', tone: 'success' });
  });

  it('show() defaults to the success tone', () => {
    service.show('Done');
    expect(service.toasts()[0].tone).toBe('success');
  });

  it('each toast gets a distinct id', () => {
    service.show('First');
    service.show('Second');
    const [first, second] = service.toasts();
    expect(first.id).not.toBe(second.id);
  });

  it('dismiss() removes a toast by id', () => {
    const id = service.show('Removable');
    expect(service.toasts()).toHaveLength(1);

    service.dismiss(id);

    expect(service.toasts()).toEqual([]);
  });

  it('auto-dismisses a toast exactly at the documented 4-second delay, not merely "eventually"', () => {
    service.show('Fades away');
    expect(service.toasts()).toHaveLength(1);

    vi.advanceTimersByTime(3999);
    expect(service.toasts()).toHaveLength(1);

    vi.advanceTimersByTime(1);
    expect(service.toasts()).toEqual([]);
  });

  it('dismiss() clears the pending auto-dismiss timer instead of leaking it', () => {
    const id = service.show('Closed early');
    expect(vi.getTimerCount()).toBe(1);

    service.dismiss(id);

    expect(vi.getTimerCount()).toBe(0);
  });
});
