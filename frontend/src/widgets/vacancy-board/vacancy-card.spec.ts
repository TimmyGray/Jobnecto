import { beforeEach, describe, expect, it } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import type { VacancyListItem } from '@entities/vacancy';
import { VacancyCardComponent } from './vacancy-card';

/** Builds one vacancy, overriding whichever fields the scenario cares about. */
function vacancy(overrides: Partial<VacancyListItem> = {}): VacancyListItem {
  return {
    id: 'v-1',
    title: 'Senior Backend Engineer',
    company: 'MedStack',
    location: 'CzechRepublic',
    workLocationType: 'OnSite',
    skills: ['C#', '.NET'],
    createdAt: '2026-10-01T10:00:00Z',
    ...overrides,
  };
}

describe('VacancyCardComponent', () => {
  let fixture: ComponentFixture<VacancyCardComponent>;
  const el = (): HTMLElement => fixture.nativeElement;
  const text = (testId: string): string | undefined =>
    el().querySelector(`[data-testid="${testId}"]`)?.textContent?.trim();
  const chips = (): string[] =>
    Array.from(el().querySelectorAll('[data-testid="vacancy-skill"]')).map(
      (c) => (c as HTMLElement).textContent?.trim() ?? '',
    );
  const link = (testId: string): HTMLAnchorElement | null =>
    el().querySelector(`a[data-testid="${testId}"]`);

  function render(item: VacancyListItem): void {
    fixture.componentRef.setInput('vacancy', item);
    fixture.detectChanges();
  }

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideRouter([])] });
    fixture = TestBed.createComponent(VacancyCardComponent);
  });

  it('renders the card as an article, not a link', () => {
    render(vacancy());

    const article = el().querySelector('article');
    expect(article).not.toBeNull();
    expect(article?.closest('a')).toBeNull();
    expect(article?.hasAttribute('tabindex')).toBe(false);
    expect(article?.getAttribute('role')).toBeNull();
  });

  it('renders title, company and humanized location, skills and a humanized work-type chip', () => {
    render(vacancy());

    expect(el().querySelector('h2')?.textContent?.trim()).toBe('Senior Backend Engineer');
    expect(text('vacancy-meta')).toBe('MedStack · Czech Republic');
    expect(chips()).toEqual(['C#', '.NET']);
    expect(text('vacancy-work-type')).toBe('On Site');
  });

  it('falls back to "Untitled vacancy" for a missing or blank title', () => {
    for (const title of [null, undefined, '', '   ']) {
      render(vacancy({ title }));
      expect(el().querySelector('h2')?.textContent?.trim()).toBe('Untitled vacancy');
    }
  });

  it('skips absent meta parts without a stray separator', () => {
    render(vacancy({ company: null, location: 'Poland' }));
    expect(text('vacancy-meta')).toBe('Poland');

    render(vacancy({ company: 'Acme', location: '  ' }));
    expect(text('vacancy-meta')).toBe('Acme');
  });

  it('renders no meta line when company and location are both absent', () => {
    render(vacancy({ company: null, location: null }));

    expect(el().querySelector('[data-testid="vacancy-meta"]')).toBeNull();
  });

  it('omits the work-type chip when workLocationType is absent or blank', () => {
    render(vacancy({ workLocationType: null }));
    expect(el().querySelector('[data-testid="vacancy-work-type"]')).toBeNull();

    render(vacancy({ workLocationType: ' ' }));
    expect(el().querySelector('[data-testid="vacancy-work-type"]')).toBeNull();
  });

  it('shows at most five skills plus a +N chip with screen-reader text', () => {
    render(vacancy({ skills: ['a', 'b', 'c', 'd', 'e', 'f', 'g'] }));

    expect(chips()).toEqual(['a', 'b', 'c', 'd', 'e']);
    const overflow = el().querySelector('[data-testid="vacancy-skill-overflow"]') as HTMLElement;
    expect(overflow.querySelector('[aria-hidden="true"]')?.textContent?.trim()).toBe('+2');
    expect(overflow.querySelector('.sr-only')?.textContent?.trim()).toBe('and 2 more skills');
  });

  it('uses the singular in the screen-reader overflow text for one hidden skill', () => {
    render(vacancy({ skills: ['a', 'b', 'c', 'd', 'e', 'f'] }));

    const overflow = el().querySelector('[data-testid="vacancy-skill-overflow"]') as HTMLElement;
    expect(overflow.querySelector('.sr-only')?.textContent?.trim()).toBe('and 1 more skill');
  });

  it('shows no overflow chip for five or fewer skills', () => {
    render(vacancy({ skills: ['a', 'b', 'c', 'd', 'e'] }));

    expect(chips()).toHaveLength(5);
    expect(el().querySelector('[data-testid="vacancy-skill-overflow"]')).toBeNull();
  });

  it('drops blank skills before capping and counting', () => {
    render(vacancy({ skills: ['', '  ', 'a', 'b', 'c', 'd', 'e', 'f'] }));

    expect(chips()).toEqual(['a', 'b', 'c', 'd', 'e']);
    expect(text('vacancy-skill-overflow')).toContain('+1');
  });

  it('renders no skill list for null or empty skills', () => {
    render(vacancy({ skills: null }));
    expect(el().querySelector('[aria-label="Skills"]')).toBeNull();

    render(vacancy({ skills: [] }));
    expect(el().querySelector('[aria-label="Skills"]')).toBeNull();
  });

  it('links both actions to /vacancies/{id} with distinct accessible names', () => {
    render(vacancy({ id: 'v-42', title: 'Staff Engineer' }));

    const generate = link('vacancy-generate')!;
    const view = link('vacancy-view')!;
    expect(generate.getAttribute('href')).toBe('/vacancies/v-42');
    expect(view.getAttribute('href')).toBe('/vacancies/v-42');
    expect(generate.textContent?.trim()).toBe('Generate');
    expect(view.textContent?.trim()).toBe('View');
    expect(generate.getAttribute('aria-label')).toBe('Generate cover letter for Staff Engineer');
    expect(view.getAttribute('aria-label')).toBe('View Staff Engineer');
  });

  it('styles Generate as primary and View as secondary, both touch-sized with focus-visible rings', () => {
    render(vacancy());

    const generate = link('vacancy-generate')!;
    const view = link('vacancy-view')!;
    expect(generate.className).toContain('bg-action-primary');
    expect(view.className).toContain('border-border-strong');
    expect(view.className).not.toContain('bg-action-primary');
    for (const a of [generate, view]) {
      expect(a.className).toContain('min-h-[44px]');
      expect(a.className).toContain('focus-visible:outline');
    }
  });

  it('uses the fallback title in the accessible names', () => {
    render(vacancy({ title: '  ' }));

    expect(link('vacancy-view')?.getAttribute('aria-label')).toBe('View Untitled vacancy');
  });

  it('renders no action links for a vacancy without an id', () => {
    render(vacancy({ id: undefined }));

    expect(el().querySelector('a')).toBeNull();
    expect(el().innerHTML).not.toContain('/vacancies/undefined');
    expect(el().querySelector('article')?.getAttribute('tabindex')).toBe('-1');
  });

  it('shows the match-score stub and never a number, even when matchScore is set', () => {
    render({ ...vacancy(), matchScore: 87 } as VacancyListItem);

    const stub = el().querySelector('ui-match-score-stub');
    expect(stub).not.toBeNull();
    expect(stub?.textContent).not.toMatch(/\d/);
    expect(el().textContent).not.toContain('87');
  });

  it('lets long unbroken titles and skills wrap instead of widening the grid', () => {
    render(vacancy({ title: 'x'.repeat(200), skills: ['y'.repeat(120)] }));

    expect(el().querySelector('h2')?.className).toContain('break-words');
    expect(el().querySelector('[data-testid="vacancy-skill"]')?.className).toContain('break-all');
  });
});
