import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import type { UserProfile } from '@entities/user';
import { ProfileCompletenessComponent, profileCompleteness } from './profile-completeness';

/** A profile with every one of the six tracked fields filled. */
const COMPLETE: UserProfile = {
  id: 'u-1',
  loginName: 'mira',
  email: 'mira@example.com',
  phone: '+31 6 1234 5678',
  location: 'Amsterdam',
  about: 'Product engineer.',
  avatar: 'https://cdn.example.com/a.png',
};

/** Builds a fresh fixture with the inputs set before the first change detection. */
function render(
  profile: UserProfile | null,
  failed = false,
): ComponentFixture<ProfileCompletenessComponent> {
  const fixture = TestBed.createComponent(ProfileCompletenessComponent);
  fixture.componentRef.setInput('profile', profile);
  fixture.componentRef.setInput('failed', failed);
  fixture.detectChanges();
  return fixture;
}

describe('profileCompleteness()', () => {
  it('reports 100% when all six fields are filled', () => {
    const { percent, items } = profileCompleteness(COMPLETE);

    expect(percent).toBe(100);
    expect(items).toHaveLength(6);
    expect(items.every((i) => i.done)).toBe(true);
  });

  it('reports 83% with only the avatar missing — the reference-mock case', () => {
    const { percent, items } = profileCompleteness({ ...COMPLETE, avatar: null });

    expect(percent).toBe(83);
    expect(items.filter((i) => !i.done).map((i) => i.label)).toEqual(['Upload avatar']);
  });

  it('reports 0% for an empty profile', () => {
    const { percent, items } = profileCompleteness({});

    expect(percent).toBe(0);
    expect(items.every((i) => i.done)).toBe(false);
  });

  it('treats a blank or whitespace-only field as not filled', () => {
    const { percent } = profileCompleteness({ ...COMPLETE, about: '   ', phone: '' });

    expect(percent).toBe(67);
  });

  it('reports 0% for a null profile', () => {
    expect(profileCompleteness(null).percent).toBe(0);
  });
});

describe('ProfileCompletenessComponent', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideRouter([])] });
  });

  it('renders the six checklist items with the honest email label', () => {
    const text: string = render(COMPLETE).nativeElement.textContent;

    expect(text).toContain('Set login name');
    expect(text).toContain('Add email');
    expect(text).toContain('Add phone');
    expect(text).toContain('Set location');
    expect(text).toContain('Write about');
    expect(text).toContain('Upload avatar');
    expect(text).not.toContain('Verify email');
  });

  it('renders the percentage and an accessible progress bar', () => {
    const fixture = render({ ...COMPLETE, avatar: null });
    const bar = fixture.nativeElement.querySelector('[role="progressbar"]');

    expect(fixture.nativeElement.textContent).toContain('83%');
    expect(bar.getAttribute('aria-valuenow')).toBe('83');
    expect(bar.getAttribute('aria-valuemin')).toBe('0');
    expect(bar.getAttribute('aria-valuemax')).toBe('100');
    expect(bar.getAttribute('aria-label')).toBeTruthy();

    // The ARIA value alone doesn't prove the visible fill matches it. 83%
    // (not 100%) so a hardcoded-full fill would fail this, not just pass by
    // coincidence.
    const fill = bar.querySelector('div') as HTMLElement;
    expect(fill.style.width).toBe('83%');
  });

  it('conveys done-ness as text, not colour alone', () => {
    const fixture = render({ ...COMPLETE, avatar: null });
    const items = fixture.nativeElement.querySelectorAll('[data-testid="checklist-item"]');

    const done = Array.from(items as NodeListOf<HTMLElement>).filter((i) =>
      i.textContent?.includes('done') && !i.textContent?.includes('not done'),
    );
    const notDone = Array.from(items as NodeListOf<HTMLElement>).filter((i) =>
      i.textContent?.includes('not done'),
    );

    expect(done).toHaveLength(5);
    expect(notDone).toHaveLength(1);
  });

  it('links every not-done item to /profile and leaves done items as plain text', () => {
    const fixture = render({ ...COMPLETE, avatar: null, phone: null });
    const links = fixture.nativeElement.querySelectorAll('[data-testid="checklist-item"] a');

    expect(links).toHaveLength(2);
    Array.from(links as NodeListOf<HTMLAnchorElement>).forEach((link) => {
      expect(link.getAttribute('href')).toBe('/profile');
    });
  });

  it('nudges without shaming — no guilt language and no danger styling', () => {
    const fixture = render({});
    const html: string = fixture.nativeElement.innerHTML;
    const text: string = fixture.nativeElement.textContent;

    expect(html).not.toContain('status-danger');
    expect(text).not.toMatch(/missing|incomplete|you should|don't forget|warning/i);
  });

  it('renders a skeleton while the profile is unknown', () => {
    const fixture = render(null);

    expect(fixture.nativeElement.querySelector('ui-skeleton')).not.toBeNull();
    expect(fixture.nativeElement.querySelectorAll('[data-testid="checklist-item"]')).toHaveLength(0);
  });

  it('renders its own error state with a working Retry when the profile load failed', () => {
    const fixture = render(null, true);
    const retried = vi.fn();
    fixture.componentInstance.retry.subscribe(retried);

    const alert = fixture.nativeElement.querySelector('[role="alert"]');
    expect(alert).not.toBeNull();
    // Not a forever-skeleton: the failure is stated, with a way out.
    expect(fixture.nativeElement.querySelector('ui-skeleton')).toBeNull();

    (alert.querySelector('button') as HTMLButtonElement).click();
    expect(retried).toHaveBeenCalledOnce();
  });

  it('prefers the error state over the checklist when a stale profile is present', () => {
    const fixture = render(COMPLETE, true);

    expect(fixture.nativeElement.querySelector('[role="alert"]')).not.toBeNull();
    expect(fixture.nativeElement.querySelectorAll('[data-testid="checklist-item"]')).toHaveLength(0);
  });

  it('uses h2 headings so the page keeps a single h1', () => {
    const fixture = render(COMPLETE);

    expect(fixture.nativeElement.querySelectorAll('h1')).toHaveLength(0);
    expect(fixture.nativeElement.querySelectorAll('h2').length).toBeGreaterThan(0);
  });
});
