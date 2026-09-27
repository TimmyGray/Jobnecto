import { beforeEach, describe, expect, it } from 'vitest';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { UserService } from '@entities/user';
import type { UserProfile } from '@entities/user';
import { httpInterceptor } from '@shared/api/http.interceptor';
import { env } from '@shared/config';
import { DashboardPage } from './dashboard.page';

const PROFILE: UserProfile = {
  id: 'u-1',
  loginName: 'mira',
  email: 'mira@example.com',
  phone: '+31 6 1234 5678',
  location: 'Amsterdam',
  about: 'Product engineer.',
  avatar: null,
};

/** A structurally complete paged body with the given items and total. */
function paged(items: unknown[], totalCount = items.length) {
  return {
    items,
    totalCount,
    lastSeenId: null,
    lastSeenUpdatedAt: null,
    pageSize: 5,
    hasNext: false,
  };
}

describe('DashboardPage', () => {
  let httpMock: HttpTestingController;
  let userService: UserService;

  const url = (path: string) => `${env.apiBaseUrl}${path}`;
  const openRequests = (path: string) => httpMock.match((req) => req.url === url(path));

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([httpInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    httpMock = TestBed.inject(HttpTestingController);
    userService = TestBed.inject(UserService);
  });

  /** Populates the profile signal the way authGuard does before the route activates. */
  function hydrateProfile(profile: UserProfile = PROFILE): void {
    userService.fetchCurrentUser().subscribe();
    httpMock.expectOne((req) => req.url === url('/users/me')).flush(profile);
  }

  function create(): ComponentFixture<DashboardPage> {
    const fixture = TestBed.createComponent(DashboardPage);
    fixture.detectChanges();
    return fixture;
  }

  /** Text of the whole rendered page. */
  const textOf = (fixture: ComponentFixture<DashboardPage>): string =>
    fixture.nativeElement.textContent;

  it('renders exactly one h1, greeting the user by login name', () => {
    hydrateProfile();
    const fixture = create();
    openRequests('/resumes')[0].flush(paged([]));
    openRequests('/educations')[0].flush(paged([]));
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelectorAll('h1')).toHaveLength(1);
    expect(fixture.nativeElement.querySelector('h1').textContent).toContain('mira');
  });

  it('requests résumés and educations concurrently — neither waits on the other', () => {
    hydrateProfile();
    create();

    // Both are matched as open before either is flushed: they overlapped.
    const resumeRequests = openRequests('/resumes');
    const educationRequests = openRequests('/educations');

    expect(resumeRequests).toHaveLength(1);
    expect(educationRequests).toHaveLength(1);

    resumeRequests[0].flush(paged([]));
    educationRequests[0].flush(paged([]));
    httpMock.verify();
  });

  it('asks for a small first page of each list', () => {
    hydrateProfile();
    create();

    expect(openRequests('/resumes')[0].request.params.get('pageSize')).toBe('5');
    expect(openRequests('/educations')[0].request.params.get('pageSize')).toBe('5');
  });

  it('shows totalCount, not the number of items on the first page', () => {
    hydrateProfile();
    const fixture = create();
    openRequests('/resumes')[0].flush(paged([{ id: 'r-1', title: 'A' }], 12));
    openRequests('/educations')[0].flush(paged([{ id: 'e-1', title: 'B' }], 4));
    fixture.detectChanges();

    const tile = (id: string) =>
      fixture.nativeElement.querySelector(`[data-testid="${id}"]`).textContent;
    expect(tile('tile-resumes')).toContain('12');
    expect(tile('tile-educations')).toContain('4');
  });

  it('coerces a string totalCount — the generated contract allows number | string', () => {
    hydrateProfile();
    const fixture = create();
    // A cast (`as number`) would leave these as strings and still render the
    // right glyphs; only a real conversion produces a usable number.
    openRequests('/resumes')[0].flush(paged([{ id: 'r-1', title: 'A' }], '12' as unknown as number));
    openRequests('/educations')[0].flush(paged([], '4' as unknown as number));
    fixture.detectChanges();

    expect(fixture.componentInstance.resumeCount()).toBe(12);
    expect(fixture.componentInstance.educationCount()).toBe(4);
    expect(typeof fixture.componentInstance.resumeCount()).toBe('number');
    expect(typeof fixture.componentInstance.educationCount()).toBe('number');
  });

  it('treats a missing totalCount as unavailable, not NaN', () => {
    hydrateProfile();
    const fixture = create();
    // A contract-violating body (totalCount omitted) must not leak a NaN into a
    // signal typed number | null — Number(undefined) is NaN, and NaN !== null.
    const { totalCount: _resumeTotal, ...resumesBody } = paged([{ id: 'r-1' }], 1);
    const { totalCount: _eduTotal, ...educationsBody } = paged([], 0);
    openRequests('/resumes')[0].flush(resumesBody);
    openRequests('/educations')[0].flush(educationsBody);
    fixture.detectChanges();

    expect(fixture.componentInstance.resumeCount()).toBeNull();
    expect(fixture.componentInstance.educationCount()).toBeNull();
    expect(
      fixture.nativeElement.querySelector('[data-testid="tile-resumes"]').textContent,
    ).toContain('Unavailable');
    expect(
      fixture.nativeElement.querySelector('[data-testid="tile-educations"]').textContent,
    ).toContain('Unavailable');
    expect(textOf(fixture)).not.toContain('NaN');
  });

  it('degrades only the résumé region when the résumé load fails', () => {
    hydrateProfile();
    const fixture = create();
    openRequests('/resumes')[0].flush(
      { title: 'Server error', status: 500 },
      { status: 500, statusText: 'Server Error' },
    );
    openRequests('/educations')[0].flush(paged([{ id: 'e-1' }], 4));
    fixture.detectChanges();

    expect(textOf(fixture)).toContain("Couldn't load your resumes");
    // The other two regions still render real content.
    expect(
      fixture.nativeElement.querySelector('[data-testid="tile-educations"]').textContent,
    ).toContain('4');
    expect(textOf(fixture)).toContain('Finish your profile');
  });

  it('degrades only the education region when the education load fails', () => {
    hydrateProfile();
    const fixture = create();
    openRequests('/resumes')[0].flush(paged([{ id: 'r-1', title: 'Staff Engineer' }], 3));
    openRequests('/educations')[0].flush(
      { title: 'Server error', status: 500 },
      { status: 500, statusText: 'Server Error' },
    );
    fixture.detectChanges();

    expect(
      fixture.nativeElement.querySelector('[data-testid="tile-educations"]').textContent,
    ).toContain('Unavailable');
    expect(textOf(fixture)).toContain('Staff Engineer');
    expect(textOf(fixture)).not.toContain("Couldn't load your resumes");
  });

  it('still renders the checklist when both list loads fail', () => {
    hydrateProfile();
    const fixture = create();
    const fail = (path: string) =>
      openRequests(path)[0].flush(
        { title: 'Server error', status: 500 },
        { status: 500, statusText: 'Server Error' },
      );
    fail('/resumes');
    fail('/educations');
    fixture.detectChanges();

    expect(textOf(fixture)).toContain('Finish your profile');
    expect(textOf(fixture)).toContain('83%');
  });

  it('refetches only the résumés when the résumé region is retried', () => {
    hydrateProfile();
    const fixture = create();
    openRequests('/resumes')[0].flush(
      { title: 'Server error', status: 500 },
      { status: 500, statusText: 'Server Error' },
    );
    openRequests('/educations')[0].flush(paged([], 0));
    fixture.detectChanges();

    const retryButton = fixture.nativeElement.querySelector(
      '[role="alert"] button',
    ) as HTMLButtonElement;
    retryButton.click();
    fixture.detectChanges();

    const retried = openRequests('/resumes');
    expect(retried).toHaveLength(1);
    expect(openRequests('/educations')).toHaveLength(0);

    retried[0].flush(paged([{ id: 'r-1', title: 'Recovered resume' }], 1));
    fixture.detectChanges();

    expect(textOf(fixture)).toContain('Recovered resume');
    expect(textOf(fixture)).not.toContain("Couldn't load your resumes");
  });

  it('refetches only the educations when the education tile is retried', () => {
    hydrateProfile();
    const fixture = create();
    openRequests('/resumes')[0].flush(paged([{ id: 'r-1', title: 'Staff Engineer' }], 3));
    openRequests('/educations')[0].flush(
      { title: 'Server error', status: 500 },
      { status: 500, statusText: 'Server Error' },
    );
    fixture.detectChanges();

    (
      fixture.nativeElement.querySelector('[data-testid="retry-educations"]') as HTMLButtonElement
    ).click();
    fixture.detectChanges();

    const retried = openRequests('/educations');
    expect(retried).toHaveLength(1);
    expect(openRequests('/resumes')).toHaveLength(0);

    retried[0].flush(paged([], 7));
    fixture.detectChanges();

    expect(
      fixture.nativeElement.querySelector('[data-testid="tile-educations"]').textContent,
    ).toContain('7');
  });

  it('recovers the profile region when its retry succeeds', () => {
    const fixture = create();
    openRequests('/users/me')[0].flush(
      { title: 'Server error', status: 500 },
      { status: 500, statusText: 'Server Error' },
    );
    openRequests('/resumes')[0].flush(paged([], 0));
    openRequests('/educations')[0].flush(paged([], 0));
    fixture.detectChanges();

    const alert = fixture.nativeElement.querySelector('[role="alert"]');
    expect(alert.textContent).toContain("Couldn't load your profile");

    (alert.querySelector('button') as HTMLButtonElement).click();
    fixture.detectChanges();
    openRequests('/users/me')[0].flush(PROFILE);
    fixture.detectChanges();

    expect(textOf(fixture)).toContain('83%');
    expect(textOf(fixture)).not.toContain("Couldn't load your profile");
  });

  it('does not refetch the profile when the guard already hydrated it', () => {
    hydrateProfile();
    create();

    expect(openRequests('/users/me')).toHaveLength(0);

    openRequests('/resumes')[0].flush(paged([]));
    openRequests('/educations')[0].flush(paged([]));
    httpMock.verify();
  });

  it('fetches the profile on the cold path when the signal is empty', () => {
    const fixture = create();

    const profileRequests = openRequests('/users/me');
    expect(profileRequests).toHaveLength(1);

    profileRequests[0].flush(PROFILE);
    openRequests('/resumes')[0].flush(paged([]));
    openRequests('/educations')[0].flush(paged([]));
    fixture.detectChanges();

    expect(textOf(fixture)).toContain('mira');
    expect(textOf(fixture)).toContain('83%');
  });

  it('degrades only the profile region when the cold-path profile fetch fails', () => {
    const fixture = create();
    openRequests('/users/me')[0].flush(
      { title: 'Server error', status: 500 },
      { status: 500, statusText: 'Server Error' },
    );
    openRequests('/resumes')[0].flush(paged([{ id: 'r-1', title: 'Staff Engineer' }], 3));
    openRequests('/educations')[0].flush(paged([], 2));
    fixture.detectChanges();

    expect(
      fixture.nativeElement.querySelector('[data-testid="tile-profile"]').textContent,
    ).toContain('Unavailable');
    expect(textOf(fixture)).toContain('Staff Engineer');
  });

  it('shows an empty state, not an error, for a brand-new user', () => {
    hydrateProfile();
    const fixture = create();
    openRequests('/resumes')[0].flush(paged([], 0));
    openRequests('/educations')[0].flush(paged([], 0));
    fixture.detectChanges();

    expect(textOf(fixture)).toContain('No resumes yet');
    expect(
      fixture.nativeElement.querySelector('[data-testid="tile-resumes"]').textContent,
    ).toContain('0');
    expect(fixture.nativeElement.querySelector('[role="alert"]')).toBeNull();
  });

  it('renders skeletons, not a whole-page spinner, while the lists are in flight', () => {
    hydrateProfile();
    const fixture = create();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelectorAll('ui-skeleton').length).toBeGreaterThan(0);
    // The header and checklist are already usable while data loads.
    expect(textOf(fixture)).toContain('mira');
    expect(textOf(fixture)).toContain('Finish your profile');

    openRequests('/resumes')[0].flush(paged([]));
    openRequests('/educations')[0].flush(paged([]));
  });

  it('offers exactly one near-black primary action, plus a secondary', () => {
    hydrateProfile();
    const fixture = create();
    openRequests('/resumes')[0].flush(paged([{ id: 'r-1', title: 'A' }], 1));
    openRequests('/educations')[0].flush(paged([], 0));
    fixture.detectChanges();

    // Scoped to interactive elements: the progress-bar fill shares the token.
    const primaries = fixture.nativeElement.querySelectorAll(
      'a.bg-action-primary, button.bg-action-primary',
    );
    expect(primaries).toHaveLength(1);
    expect(primaries[0].getAttribute('href')).toBe('/resumes');

    const secondary = fixture.nativeElement.querySelector('a[href="/educations"]');
    expect(secondary).not.toBeNull();
    expect(secondary.className).not.toContain('bg-action-primary');
  });
});
