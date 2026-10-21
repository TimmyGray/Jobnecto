import { beforeEach, describe, expect, it } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import type { ResumeResult } from '@entities/resume';
import { ResumeListComponent } from './resume-list';

/** Builds one résumé, overriding whichever fields the scenario cares about. */
function resume(overrides: Partial<ResumeResult> = {}): ResumeResult {
  return {
    id: 'r-1',
    title: 'Senior Product Engineer',
    experience: 'ThreeToFiveYears',
    workLocationType: 'OnSite',
    skills: ['TypeScript', 'Angular'],
    updatedAt: '2026-09-28T12:00:00Z',
    ...overrides,
  };
}

describe('ResumeListComponent', () => {
  let fixture: ComponentFixture<ResumeListComponent>;

  const cards = (): HTMLElement[] =>
    Array.from(fixture.nativeElement.querySelectorAll('[data-testid="resume-card"]'));

  function render(items: ResumeResult[]): void {
    fixture.componentRef.setInput('items', items);
    fixture.detectChanges();
  }

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideRouter([])] });
    fixture = TestBed.createComponent(ResumeListComponent);
  });

  it('renders one card per résumé', () => {
    render([resume({ id: 'r-1' }), resume({ id: 'r-2' }), resume({ id: 'r-3' })]);

    expect(cards()).toHaveLength(3);
  });

  it('renders cards in a one-column, md-two-column grid, never a table', () => {
    render([resume()]);

    expect(fixture.nativeElement.querySelector('table')).toBeNull();
    const list = fixture.nativeElement.querySelector('ul');
    expect(list.className).toContain('grid');
    expect(list.className).toContain('md:grid-cols-2');
    expect(list.className).not.toContain('grid-cols-3');
  });

  it('links each card to its detail route', () => {
    render([resume({ id: 'r-42' })]);

    expect(cards()[0].getAttribute('href')).toBe('/resumes/r-42');
  });

  it('renders the title as an h2, falling back for a missing or blank title', () => {
    render([
      resume({ id: 'a', title: 'Staff Engineer' }),
      resume({ id: 'b', title: null }),
      resume({ id: 'c', title: '' }),
      resume({ id: 'd', title: '   ' }),
    ]);

    const titles = cards().map((c) => c.querySelector('h2')?.textContent?.trim());
    expect(titles).toEqual([
      'Staff Engineer',
      'Untitled resume',
      'Untitled resume',
      'Untitled resume',
    ]);
  });

  it('humanizes enum values in the meta line', () => {
    render([resume()]);

    const meta = fixture.nativeElement.querySelector('[data-testid="resume-meta"]');
    expect(meta.textContent.trim()).toBe('Three To Five Years · On Site');
  });

  it('omits absent meta parts without leaving a stray separator', () => {
    render([
      resume({ id: 'a', experience: null, workLocationType: 'Remote' }),
      resume({ id: 'b', experience: 'Senior', workLocationType: '  ' }),
    ]);

    const metas = Array.from(
      fixture.nativeElement.querySelectorAll('[data-testid="resume-meta"]'),
    ).map((m) => (m as HTMLElement).textContent?.trim());
    expect(metas).toEqual(['Remote', 'Senior']);
  });

  it('renders no meta line when there is nothing to show', () => {
    render([resume({ experience: null, workLocationType: null })]);

    expect(fixture.nativeElement.querySelector('[data-testid="resume-meta"]')).toBeNull();
  });

  it('shows at most five skills plus a +N chip for the rest', () => {
    render([resume({ skills: ['a', 'b', 'c', 'd', 'e', 'f', 'g'] })]);

    const chips = Array.from(
      fixture.nativeElement.querySelectorAll('[data-testid="resume-skill"]'),
    ).map((c) => (c as HTMLElement).textContent?.trim());
    expect(chips).toEqual(['a', 'b', 'c', 'd', 'e']);

    const overflow = fixture.nativeElement.querySelector('[data-testid="resume-skill-overflow"]');
    expect(overflow.textContent).toContain('+2');
    expect(overflow.querySelector('.sr-only').textContent.trim()).toBe('and 2 more skills');
    expect(overflow.querySelector('[aria-hidden="true"]').textContent.trim()).toBe('+2');
  });

  it('shows all skills with no overflow chip when there are five or fewer', () => {
    render([resume({ skills: ['a', 'b', 'c', 'd', 'e'] })]);

    const chips = fixture.nativeElement.querySelectorAll('[data-testid="resume-skill"]');
    expect(chips).toHaveLength(5);
    expect(fixture.nativeElement.querySelector('[data-testid="resume-skill-overflow"]')).toBeNull();
  });

  it('drops blank skills before capping and counting', () => {
    render([resume({ skills: ['', '  ', 'a', 'b', 'c', 'd', 'e', 'f'] })]);

    const chips = Array.from(
      fixture.nativeElement.querySelectorAll('[data-testid="resume-skill"]'),
    ).map((c) => (c as HTMLElement).textContent?.trim());
    expect(chips).toEqual(['a', 'b', 'c', 'd', 'e']);
    expect(
      fixture.nativeElement.querySelector('[data-testid="resume-skill-overflow"]').textContent,
    ).toContain('+1');
  });

  it('lets long unbroken titles and skills wrap instead of widening the grid', () => {
    render([resume({ title: 'x'.repeat(200), skills: ['y'.repeat(120)] })]);

    expect(fixture.nativeElement.querySelector('li').className).toContain('min-w-0');
    expect(fixture.nativeElement.querySelector('h2').className).toContain('break-words');
    expect(fixture.nativeElement.querySelector('[data-testid="resume-skill"]').className).toContain(
      'break-all',
    );
  });

  it('preserves the order it is given (no client-side sorting)', () => {
    render([
      resume({ id: 'z', title: 'Zed' }),
      resume({ id: 'a', title: 'Alpha' }),
      resume({ id: 'm', title: 'Mid' }),
    ]);

    expect(cards().map((c) => c.querySelector('h2')?.textContent?.trim())).toEqual([
      'Zed',
      'Alpha',
      'Mid',
    ]);
  });

  it('renders no skill chips for null or empty skills', () => {
    render([resume({ id: 'a', skills: null }), resume({ id: 'b', skills: [] })]);

    expect(fixture.nativeElement.querySelectorAll('[data-testid="resume-skill"]')).toHaveLength(0);
  });

  it('shows the updated date when present and omits it when absent', () => {
    render([resume({ id: 'a' }), resume({ id: 'b', updatedAt: undefined })]);

    const updated = fixture.nativeElement.querySelectorAll('[data-testid="resume-updated"]');
    expect(updated).toHaveLength(1);
    expect(updated[0].textContent).toContain('Updated');
    // Same local-time formatting the DatePipe applies, so the test holds in any timezone.
    const expected = new Date('2026-09-28T12:00:00Z').toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    });
    expect(updated[0].textContent).toContain(expected);
  });

  it('renders a résumé without an id as a non-link card and never links to /resumes/undefined', () => {
    render([resume({ id: undefined, title: 'No id' })]);

    const [card] = cards();
    expect(card).toBeDefined();
    expect(card.tagName).not.toBe('A');
    expect(card.getAttribute('href')).toBeNull();
    expect(card.querySelector('a')).toBeNull();
    expect(fixture.nativeElement.innerHTML).not.toContain('/resumes/undefined');
    expect(card.textContent).toContain('No id');
  });

  it('focusCard() focuses a linked card', () => {
    render([resume({ id: 'a' }), resume({ id: 'b' })]);

    fixture.componentInstance.focusCard(1);

    expect(document.activeElement).toBe(cards()[1]);
  });

  it('focusCard() focuses a non-link card too (tabindex -1)', () => {
    render([resume({ id: undefined })]);

    fixture.componentInstance.focusCard(0);

    expect(document.activeElement).toBe(cards()[0]);
  });

  it('focusCard() with an out-of-range index is a no-op', () => {
    render([resume()]);

    const before = document.activeElement;

    expect(() => fixture.componentInstance.focusCard(5)).not.toThrow();
    expect(document.activeElement).toBe(before);
  });

  it('does not throw when two id-less résumés are rendered together', () => {
    expect(() => render([resume({ id: undefined }), resume({ id: undefined })])).not.toThrow();

    expect(cards()).toHaveLength(2);
  });
});
