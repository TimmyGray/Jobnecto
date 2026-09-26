import { describe, expect, it, beforeEach } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { AppShellComponent } from './app-shell';
import { NAV_ITEMS } from './nav-items';

describe('AppShellComponent', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideRouter([{ path: '**', component: AppShellComponent }])],
    });
  });

  it('renders every NAV_ITEMS label as a link to its route', async () => {
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/dashboard', AppShellComponent);

    const links: HTMLAnchorElement[] = Array.from(
      harness.fixture.nativeElement.querySelectorAll('nav[aria-label="Primary"] a'),
    );
    expect(links).toHaveLength(NAV_ITEMS.length);
    for (const [i, item] of NAV_ITEMS.entries()) {
      expect(links[i].textContent).toContain(item.label);
      expect(links[i].getAttribute('href')).toBe(item.path);
    }
  });

  it('marks only the current route active near-black, others inactive', async () => {
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/resumes', AppShellComponent);

    const links: HTMLAnchorElement[] = Array.from(
      harness.fixture.nativeElement.querySelectorAll('nav[aria-label="Primary"] a'),
    );
    const resumesLink = links.find((a) => a.textContent?.includes('Resumes'));
    const dashboardLink = links.find((a) => a.textContent?.includes('Dashboard'));

    expect(resumesLink?.classList.contains('text-text-primary')).toBe(true);
    expect(dashboardLink?.classList.contains('text-text-primary')).toBe(false);
  });

  it('renders a "Skip to content" link as the very first focusable element, targeting #main-content', async () => {
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/dashboard', AppShellComponent);

    const firstFocusable: HTMLElement = harness.fixture.nativeElement.querySelector('a, button');
    expect(firstFocusable.tagName).toBe('A');
    expect(firstFocusable.textContent).toContain('Skip to content');
    expect(firstFocusable.getAttribute('href')).toBe('#main-content');
  });

  it('hides the off-canvas drawer by default and opens it via the hamburger toggle', async () => {
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/dashboard', AppShellComponent);

    expect(harness.fixture.nativeElement.querySelector('[data-testid="drawer"]')).toBeNull();

    const toggle: HTMLButtonElement = harness.fixture.nativeElement.querySelector(
      '[data-testid="drawer-toggle"]',
    );
    toggle.click();
    harness.fixture.detectChanges();

    expect(harness.fixture.nativeElement.querySelector('[data-testid="drawer"]')).not.toBeNull();
  });

  it('closes the drawer on Escape and returns focus to the hamburger toggle', async () => {
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/dashboard', AppShellComponent);

    const toggle: HTMLButtonElement = harness.fixture.nativeElement.querySelector(
      '[data-testid="drawer-toggle"]',
    );
    toggle.click();
    harness.fixture.detectChanges();

    const drawer: HTMLElement = harness.fixture.nativeElement.querySelector(
      '[data-testid="drawer"]',
    );
    drawer.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
    harness.fixture.detectChanges();

    expect(harness.fixture.nativeElement.querySelector('[data-testid="drawer"]')).toBeNull();
    expect(document.activeElement).toBe(toggle);
  });

  it('closes the drawer when a drawer nav link is followed', async () => {
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/dashboard', AppShellComponent);

    const toggle: HTMLButtonElement = harness.fixture.nativeElement.querySelector(
      '[data-testid="drawer-toggle"]',
    );
    toggle.click();
    harness.fixture.detectChanges();

    const drawerLink: HTMLAnchorElement = harness.fixture.nativeElement.querySelector(
      '[data-testid="drawer"] a',
    );
    drawerLink.click();
    harness.fixture.detectChanges();

    expect(harness.fixture.nativeElement.querySelector('[data-testid="drawer"]')).toBeNull();
  });

  it('closes the drawer on backdrop click', async () => {
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/dashboard', AppShellComponent);

    const toggle: HTMLButtonElement = harness.fixture.nativeElement.querySelector(
      '[data-testid="drawer-toggle"]',
    );
    toggle.click();
    harness.fixture.detectChanges();

    const backdrop: HTMLElement = harness.fixture.nativeElement.querySelector(
      '[data-testid="drawer-backdrop"]',
    );
    backdrop.click();
    harness.fixture.detectChanges();

    expect(harness.fixture.nativeElement.querySelector('[data-testid="drawer"]')).toBeNull();
  });
});
