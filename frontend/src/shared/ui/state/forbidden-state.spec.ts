import { describe, expect, it, beforeEach } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ForbiddenStateComponent } from './forbidden-state';

describe('ForbiddenStateComponent', () => {
  let fixture: ComponentFixture<ForbiddenStateComponent>;

  beforeEach(() => {
    fixture = TestBed.createComponent(ForbiddenStateComponent);
    fixture.detectChanges();
  });

  it('renders the default headline, guidance, and CTA label', () => {
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain("You can't access this");
    expect(el.textContent).toContain('This may belong to someone else, or you may not have permission.');
    expect(el.querySelector('button')?.textContent?.trim()).toBe('Back to safety');
  });

  it('renders overridden headline/guidance/CTA label', () => {
    fixture.componentRef.setInput('headline', 'Not your résumé');
    fixture.componentRef.setInput('guidance', 'This one belongs to someone else.');
    fixture.componentRef.setInput('recoverLabel', 'Back to résumés');
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Not your résumé');
    expect(el.textContent).toContain('This one belongs to someone else.');
    expect(el.querySelector('button')?.textContent?.trim()).toBe('Back to résumés');
  });

  it('emits recover when the CTA is clicked', () => {
    let emitted = false;
    fixture.componentInstance.recover.subscribe(() => (emitted = true));
    (fixture.nativeElement as HTMLElement).querySelector('button')?.click();
    expect(emitted).toBe(true);
  });

  it('uses role="status", never aria-live/alert (this is a safe recovery state, not an active error)', () => {
    const container = (fixture.nativeElement as HTMLElement).querySelector('[role]');
    expect(container?.getAttribute('role')).toBe('status');
    expect(container?.hasAttribute('aria-live')).toBe(false);
  });
});
