import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import type { ResumeResult } from '@entities/resume';
import { RecentResumesComponent } from './recent-resumes';

/** Builds one résumé, overriding whichever fields the scenario cares about. */
function resume(overrides: Partial<ResumeResult> = {}): ResumeResult {
  return {
    id: 'r-1',
    title: 'Senior Product Engineer',
    experience: '5+ years',
    workLocationType: 'Hybrid',
    ...overrides,
  };
}

interface Inputs {
  resumes: ResumeResult[] | null;
  loading: boolean;
  failed: boolean;
}

const LOADED: Inputs = { resumes: [], loading: false, failed: false };

/** Fresh fixture with every input set before the first change detection. */
function render(overrides: Partial<Inputs> = {}): ComponentFixture<RecentResumesComponent> {
  const fixture = TestBed.createComponent(RecentResumesComponent);
  const inputs = { ...LOADED, ...overrides };
  for (const [key, value] of Object.entries(inputs)) {
    fixture.componentRef.setInput(key, value);
  }
  fixture.detectChanges();
  return fixture;
}

describe('RecentResumesComponent', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideRouter([])] });
  });

  it('renders an error state with a working Retry when the load failed', () => {
    const fixture = render({ failed: true, resumes: null });
    const retried = vi.fn();
    fixture.componentInstance.retry.subscribe(retried);

    const alert = fixture.nativeElement.querySelector('[role="alert"]');
    expect(alert).not.toBeNull();

    const retryButton = alert.querySelector('button') as HTMLButtonElement;
    retryButton.click();

    expect(retried).toHaveBeenCalledOnce();
  });

  it('prefers the error state over the loading state when both are set', () => {
    const fixture = render({ failed: true, loading: true, resumes: null });

    expect(fixture.nativeElement.querySelector('[role="alert"]')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('ui-skeleton')).toBeNull();
  });

  it('renders a skeleton while loading', () => {
    const fixture = render({ loading: true, resumes: null });

    expect(fixture.nativeElement.querySelector('ui-skeleton')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('[role="alert"]')).toBeNull();
  });

  it('renders an empty state with a link to /resumes when there are none', () => {
    const fixture = render({ resumes: [] });
    const text: string = fixture.nativeElement.textContent;

    expect(text).toContain('No resumes yet');

    const cta = fixture.nativeElement.querySelector('a[href="/resumes"]');
    expect(cta).not.toBeNull();
  });

  it('renders at most five cards even when more résumés are supplied', () => {
    const six = Array.from({ length: 6 }, (_, i) => resume({ id: `r-${i}`, title: `Resume ${i}` }));
    const fixture = render({ resumes: six });

    const cards = fixture.nativeElement.querySelectorAll('[data-testid="resume-card"]');
    expect(cards).toHaveLength(5);
    expect(fixture.nativeElement.textContent).not.toContain('Resume 5');
  });

  it('links each card to its résumé detail route', () => {
    const fixture = render({ resumes: [resume({ id: 'r-42' })] });
    const card = fixture.nativeElement.querySelector('[data-testid="resume-card"]');

    expect(card.getAttribute('href')).toBe('/resumes/r-42');
  });

  it('renders the résumé title with its experience and work-location meta', () => {
    const fixture = render({ resumes: [resume()] });
    const text: string = fixture.nativeElement.textContent;

    expect(text).toContain('Senior Product Engineer');
    expect(text).toContain('5+ years');
    expect(text).toContain('Hybrid');
  });

  it('falls back to a placeholder title when the résumé has none', () => {
    const fixture = render({ resumes: [resume({ title: null })] });

    expect(fixture.nativeElement.textContent).toContain('Untitled resume');
  });

  it('omits the meta separator when only one meta field is present', () => {
    const fixture = render({ resumes: [resume({ workLocationType: null })] });
    const meta = fixture.nativeElement.querySelector('[data-testid="resume-meta"]');

    expect(meta.textContent.trim()).toBe('5+ years');
  });

  it('renders no meta line at all when both meta fields are absent', () => {
    const fixture = render({
      resumes: [resume({ experience: null, workLocationType: null })],
    });

    expect(fixture.nativeElement.querySelector('[data-testid="resume-meta"]')).toBeNull();
  });

  it('survives résumés with no id — duplicate track keys would otherwise throw', () => {
    const fixture = render({
      resumes: [resume({ id: undefined, title: 'One' }), resume({ id: undefined, title: 'Two' })],
    });

    expect(fixture.nativeElement.querySelectorAll('[data-testid="resume-card"]')).toHaveLength(2);
    expect(fixture.nativeElement.textContent).toContain('One');
    expect(fixture.nativeElement.textContent).toContain('Two');
  });

  it('uses h2/h3 headings so the page keeps a single h1', () => {
    const fixture = render({ resumes: [resume()] });

    expect(fixture.nativeElement.querySelectorAll('h1')).toHaveLength(0);
    expect(fixture.nativeElement.querySelectorAll('h2').length).toBeGreaterThan(0);
  });
});
