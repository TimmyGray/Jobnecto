import { beforeEach, describe, expect, it } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import type { VacancyListItem } from '@entities/vacancy';
import { VacancyBoardComponent } from './vacancy-board';

function vacancy(id: string | undefined, title = `Vacancy ${id}`): VacancyListItem {
  return { id, title, company: 'Acme', skills: [] };
}

describe('VacancyBoardComponent', () => {
  let fixture: ComponentFixture<VacancyBoardComponent>;
  const el = (): HTMLElement => fixture.nativeElement;
  const cards = (): HTMLElement[] => Array.from(el().querySelectorAll('article'));

  function render(items: VacancyListItem[]): void {
    fixture.componentRef.setInput('items', items);
    fixture.detectChanges();
  }

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideRouter([])] });
    fixture = TestBed.createComponent(VacancyBoardComponent);
  });

  it('renders one card per vacancy, in the order given', () => {
    render([vacancy('z', 'Zed'), vacancy('a', 'Alpha'), vacancy('m', 'Mid')]);

    expect(cards().map((c) => c.querySelector('h2')?.textContent?.trim())).toEqual([
      'Zed',
      'Alpha',
      'Mid',
    ]);
  });

  it('renders cards in a 1 / md:2 / xl:3 column grid, never a table', () => {
    render([vacancy('a')]);

    expect(el().querySelector('table')).toBeNull();
    const list = el().querySelector('ul') as HTMLElement;
    expect(list.className).toContain('grid');
    expect(list.className).toContain('md:grid-cols-2');
    expect(list.className).toContain('xl:grid-cols-3');
    expect(el().querySelector('li')?.className).toContain('min-w-0');
  });

  // Smoke test only: Angular does not surface @for track-key collisions (NG0955) in this test
  // environment, so the `|| $index` fallback is guarded by review rather than by an assertion.
  it('renders every card without throwing for duplicate or missing ids', () => {
    expect(() =>
      render([vacancy(undefined), vacancy(undefined), vacancy('d'), vacancy('d')]),
    ).not.toThrow();

    expect(cards()).toHaveLength(4);
  });

  it('focusCard() focuses the first link of the card', () => {
    render([vacancy('a'), vacancy('b')]);

    fixture.componentInstance.focusCard(1);

    expect(document.activeElement).toBe(cards()[1].querySelector('a'));
    expect((document.activeElement as HTMLElement).getAttribute('href')).toBe('/vacancies/b');
  });

  it('focusCard() focuses the article itself when the card has no links', () => {
    render([vacancy(undefined)]);

    fixture.componentInstance.focusCard(0);

    expect(document.activeElement).toBe(cards()[0]);
  });

  it('focusCard() with an out-of-range index is a no-op', () => {
    render([vacancy('a')]);
    const before = document.activeElement;

    expect(() => fixture.componentInstance.focusCard(5)).not.toThrow();
    expect(document.activeElement).toBe(before);
  });
});
