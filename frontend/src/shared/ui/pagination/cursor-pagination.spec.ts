import { beforeEach, describe, expect, it } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CursorPaginationComponent } from './cursor-pagination';

describe('CursorPaginationComponent', () => {
  let fixture: ComponentFixture<CursorPaginationComponent>;
  let emitted: number;

  const button = (): HTMLButtonElement | null => fixture.nativeElement.querySelector('button');

  beforeEach(() => {
    fixture = TestBed.createComponent(CursorPaginationComponent);
    emitted = 0;
    fixture.componentInstance.loadMore.subscribe(() => emitted++);
  });

  it('renders nothing when there is no next page', () => {
    fixture.componentRef.setInput('hasNext', false);
    fixture.detectChanges();

    expect(button()).toBeNull();
  });

  it('renders an enabled button with the default label when a next page exists', () => {
    fixture.componentRef.setInput('hasNext', true);
    fixture.detectChanges();

    expect(button()?.textContent?.trim()).toBe('Load more');
    expect(button()?.getAttribute('aria-disabled')).toBe('false');
  });

  it('uses a custom label when provided', () => {
    fixture.componentRef.setInput('hasNext', true);
    fixture.componentRef.setInput('label', 'Show more résumés');
    fixture.detectChanges();

    expect(button()?.textContent?.trim()).toBe('Show more résumés');
  });

  it('emits loadMore on click', () => {
    fixture.componentRef.setInput('hasNext', true);
    fixture.detectChanges();

    button()?.click();

    expect(emitted).toBe(1);
  });

  it('is aria-disabled and busy, relabelled, and does not emit while loading', () => {
    fixture.componentRef.setInput('hasNext', true);
    fixture.componentRef.setInput('loading', true);
    fixture.detectChanges();

    expect(button()?.getAttribute('aria-disabled')).toBe('true');
    expect(button()?.getAttribute('aria-busy')).toBe('true');
    expect(button()?.textContent?.trim()).toBe('Loading…');

    button()?.click();
    expect(emitted).toBe(0);
  });

  it('keeps keyboard focus on the button while loading starts', () => {
    fixture.componentRef.setInput('hasNext', true);
    fixture.detectChanges();
    document.body.appendChild(fixture.nativeElement);
    button()?.focus();

    fixture.componentRef.setInput('loading', true);
    fixture.detectChanges();

    expect(document.activeElement).toBe(button());
    fixture.nativeElement.remove();
  });

  it('does not emit if click() is forced through while loading', () => {
    fixture.componentRef.setInput('hasNext', true);
    fixture.componentRef.setInput('loading', true);
    fixture.detectChanges();

    fixture.componentInstance.onClick();

    expect(emitted).toBe(0);
  });
});
