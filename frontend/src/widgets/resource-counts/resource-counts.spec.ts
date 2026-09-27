import { describe, expect, it, vi } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ResourceCountsComponent } from './resource-counts';

/** Inputs for one render. Every field has a sensible loaded default. */
interface Inputs {
  completenessPercent: number | null;
  profileLoading: boolean;
  profileFailed: boolean;
  resumeCount: number | null;
  resumesLoading: boolean;
  resumesFailed: boolean;
  educationCount: number | null;
  educationsLoading: boolean;
  educationsFailed: boolean;
}

const LOADED: Inputs = {
  completenessPercent: 83,
  profileLoading: false,
  profileFailed: false,
  resumeCount: 3,
  resumesLoading: false,
  resumesFailed: false,
  educationCount: 2,
  educationsLoading: false,
  educationsFailed: false,
};

/**
 * Builds a fresh fixture with every input set BEFORE the first change
 * detection — mutating a shared fixture's inputs after the first
 * `detectChanges()` trips NG0100 in this Angular setup.
 */
function render(overrides: Partial<Inputs> = {}): ComponentFixture<ResourceCountsComponent> {
  const fixture = TestBed.createComponent(ResourceCountsComponent);
  const inputs = { ...LOADED, ...overrides };
  for (const [key, value] of Object.entries(inputs)) {
    fixture.componentRef.setInput(key, value);
  }
  fixture.detectChanges();
  return fixture;
}

/** Reads the rendered value of one tile by its test id. */
function tileValue(fixture: ComponentFixture<ResourceCountsComponent>, testId: string): string {
  const tile = fixture.nativeElement.querySelector(`[data-testid="${testId}"]`) as HTMLElement;
  return tile.textContent?.trim() ?? '';
}

describe('ResourceCountsComponent', () => {
  it('renders the loaded counts and the completeness percentage', () => {
    const fixture = render();

    expect(tileValue(fixture, 'tile-profile')).toContain('83%');
    expect(tileValue(fixture, 'tile-resumes')).toContain('3');
    expect(tileValue(fixture, 'tile-educations')).toContain('2');
  });

  it('labels every tile with a mono uppercase eyebrow', () => {
    const fixture = render();
    const text: string = fixture.nativeElement.textContent;

    expect(text).toContain('Profile complete');
    expect(text).toContain('Active resumes');
    expect(text).toContain('Education');
    expect(text).toContain('Cover letters');

    // Text content alone doesn't prove "mono uppercase" — check the styling too.
    const eyebrows = fixture.nativeElement.querySelectorAll('dt');
    expect(eyebrows.length).toBeGreaterThan(0);
    eyebrows.forEach((dt: HTMLElement) => {
      expect(dt.className).toContain('font-mono');
      expect(dt.className).toContain('uppercase');
    });
  });

  it('renders a zero count as 0, not as an empty or error tile', () => {
    const fixture = render({ resumeCount: 0, educationCount: 0, completenessPercent: 0 });

    expect(tileValue(fixture, 'tile-resumes')).toContain('0');
    expect(tileValue(fixture, 'tile-educations')).toContain('0');
    expect(tileValue(fixture, 'tile-profile')).toContain('0%');
    expect(fixture.nativeElement.textContent).not.toContain('Unavailable');
  });

  it('renders a skeleton in place of a still-loading value', () => {
    const fixture = render({ resumesLoading: true, resumeCount: null });

    const tile = fixture.nativeElement.querySelector('[data-testid="tile-resumes"]');
    expect(tile.querySelector('ui-skeleton')).not.toBeNull();
    expect(tileValue(fixture, 'tile-resumes')).not.toContain('3');
  });

  it('renders each source independently — a failed resume load leaves the education count intact', () => {
    const fixture = render({ resumesFailed: true, resumeCount: null });

    expect(tileValue(fixture, 'tile-resumes')).toContain('Unavailable');
    expect(tileValue(fixture, 'tile-educations')).toContain('2');
    expect(tileValue(fixture, 'tile-profile')).toContain('83%');
  });

  it('renders the profile tile as unavailable when the profile load failed', () => {
    const fixture = render({ profileFailed: true, completenessPercent: null });

    expect(tileValue(fixture, 'tile-profile')).toContain('Unavailable');
    expect(tileValue(fixture, 'tile-resumes')).toContain('3');
  });

  it('offers a Retry on the failed education tile — its only surface', () => {
    const fixture = render({ educationsFailed: true, educationCount: null });
    const retried = vi.fn();
    fixture.componentInstance.retryEducations.subscribe(retried);

    const button = fixture.nativeElement.querySelector(
      '[data-testid="retry-educations"]',
    ) as HTMLButtonElement;
    button.click();

    expect(retried).toHaveBeenCalledOnce();
  });

  it('offers no Retry on the résumé or profile tiles — their panels below own it', () => {
    const fixture = render({
      resumesFailed: true,
      resumeCount: null,
      profileFailed: true,
      completenessPercent: null,
    });

    expect(fixture.nativeElement.querySelector('[data-testid="tile-resumes"] button')).toBeNull();
    expect(fixture.nativeElement.querySelector('[data-testid="tile-profile"] button')).toBeNull();
  });

  it('renders the cover-letters tile as a placeholder with an accessible label', () => {
    const fixture = render();
    const tile = fixture.nativeElement.querySelector('[data-testid="tile-cover-letters"]');

    expect(tile.textContent).toContain('—');
    expect(tile.querySelector('.sr-only')?.textContent).toContain('not available yet');
  });

  it('marks the decorative em dash as hidden from assistive technology', () => {
    const fixture = render();
    const tile = fixture.nativeElement.querySelector('[data-testid="tile-cover-letters"]');
    const dash = tile.querySelector('[aria-hidden="true"]');

    expect(dash?.textContent).toContain('—');
  });
});
