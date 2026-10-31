import { beforeEach, describe, expect, it } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatchScoreStubComponent } from './match-score-stub';

describe('MatchScoreStubComponent', () => {
  let fixture: ComponentFixture<MatchScoreStubComponent>;
  const el = (): HTMLElement => fixture.nativeElement;
  const note = (): HTMLElement => el().querySelector('[role="note"]') as HTMLElement;

  beforeEach(() => {
    fixture = TestBed.createComponent(MatchScoreStubComponent);
    fixture.detectChanges();
  });

  it('reads "AI match · soon" in a mono pill', () => {
    const pill = el().querySelector('[data-testid="match-score-pill"]') as HTMLElement;
    expect(pill.textContent?.trim()).toBe('AI match · soon');
    expect(pill.className).toContain('font-mono');
  });

  it('is a labelled note stating the score is not yet available', () => {
    expect(note()).not.toBeNull();
    expect(note().getAttribute('aria-label')).toBe('AI match score — not yet available');
  });

  it('hides the visual pill from screen readers and carries the label as text inside the note', () => {
    const pill = el().querySelector('[data-testid="match-score-pill"]') as HTMLElement;
    expect(pill.getAttribute('aria-hidden')).toBe('true');
    expect(note().querySelector('.sr-only')?.textContent?.trim()).toBe(
      'AI match score — not yet available',
    );
  });

  it('uses a dashed, neutral container — never error styling', () => {
    expect(note().className).toContain('border-dashed');
    expect(note().className).toContain('border-border-default');
    expect(el().innerHTML).not.toMatch(/danger|error/);
  });

  it('is not interactive: not focusable, no buttons or links', () => {
    expect(el().querySelector('[tabindex]')).toBeNull();
    expect(el().querySelector('button, a, input, select, textarea')).toBeNull();
  });

  it('shows no number', () => {
    expect(el().textContent).not.toMatch(/\d/);
  });
});
