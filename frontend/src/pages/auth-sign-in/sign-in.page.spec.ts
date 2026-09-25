import { describe, expect, it, beforeEach, vi } from 'vitest';
import {
  provideHttpClient,
  withInterceptors,
} from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { httpInterceptor } from '@shared/api/http.interceptor';
import { env } from '@shared/config';
import { UserService } from '@entities/user';
import { SignInPage } from './sign-in.page';

describe('SignInPage', () => {
  let fixture: ComponentFixture<SignInPage>;
  let page: SignInPage;
  let httpMock: HttpTestingController;
  let router: Router;
  let userService: UserService;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SignInPage],
      providers: [
        provideHttpClient(withInterceptors([httpInterceptor])),
        provideHttpClientTesting(),
        provideRouter([]),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(SignInPage);
    page = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
    userService = TestBed.inject(UserService);
    fixture.detectChanges();
  });

  /** Fills the typed reactive form with valid values and marks it dirty. */
  function fillValid() {
    page.form.setValue({ identifier: 'daria_dev', password: 'sup3rsecret' });
    page.form.markAsDirty();
    fixture.detectChanges();
  }

  it('disables submit until the form is dirty and valid', () => {
    expect(page.canSubmit).toBe(false); // pristine
    fillValid();
    expect(page.canSubmit).toBe(true);
  });

  it('does not call the API when the form is invalid on submit', () => {
    page.form.setValue({ identifier: '', password: '' });
    page.form.markAsDirty();
    page.onSubmit();
    httpMock.expectNone(`${env.apiBaseUrl}/users/sessions`);
    expect(page.form.controls.identifier.touched).toBe(true);
    httpMock.verify();
  });

  it('on 200 signs in, hydrates /users/me, and routes to /dashboard', async () => {
    const navSpy = vi.spyOn(router, 'navigate').mockResolvedValue(true);
    fillValid();
    page.onSubmit();

    const signInReq = httpMock.expectOne(`${env.apiBaseUrl}/users/sessions`);
    expect(signInReq.request.method).toBe('POST');
    signInReq.flush(
      { id: 'u1', loginName: 'daria_dev', accessToken: '' },
      { status: 200, statusText: 'OK' },
    );

    const meReq = httpMock.expectOne(`${env.apiBaseUrl}/users/me`);
    expect(meReq.request.method).toBe('GET');
    meReq.flush({ id: 'u1', loginName: 'daria_dev' });

    expect(navSpy).toHaveBeenCalledWith(['/dashboard']);
    expect(page.submitting()).toBe(false);
    httpMock.verify();
  });

  it('trims leading/trailing whitespace from the identifier before sending it', () => {
    vi.spyOn(router, 'navigate').mockResolvedValue(true);
    page.form.setValue({ identifier: '  daria_dev  ', password: 'sup3rsecret' });
    page.form.markAsDirty();
    page.onSubmit();

    const req = httpMock.expectOne(`${env.apiBaseUrl}/users/sessions`);
    expect(req.request.body.identifier).toBe('daria_dev');
    req.flush({ id: 'u1', loginName: 'daria_dev', accessToken: '' }, { status: 200, statusText: 'OK' });
    httpMock.expectOne(`${env.apiBaseUrl}/users/me`).flush({ id: 'u1', loginName: 'daria_dev' });
    httpMock.verify();
  });

  it('does not fire a second POST when onSubmit is called again while the first is still in flight', () => {
    vi.spyOn(router, 'navigate').mockResolvedValue(true);
    fillValid();
    page.onSubmit();
    page.onSubmit();

    const requests = httpMock.match(`${env.apiBaseUrl}/users/sessions`);
    expect(requests.length).toBe(1);
    requests[0].flush(
      { id: 'u1', loginName: 'daria_dev', accessToken: '' },
      { status: 200, statusText: 'OK' },
    );
    httpMock.expectOne(`${env.apiBaseUrl}/users/me`).flush({ id: 'u1', loginName: 'daria_dev' });
    httpMock.verify();
  });

  it('clears a stale server-set field error on resubmit so a fix to a different field is not permanently blocked', () => {
    vi.spyOn(router, 'navigate').mockResolvedValue(true);
    fillValid();
    page.onSubmit();
    httpMock.expectOne(`${env.apiBaseUrl}/users/sessions`).flush(
      { title: 'Validation failed', status: 400, errors: { identifier: ['identifier is required.'] } },
      { status: 400, statusText: 'Bad Request' },
    );
    fixture.detectChanges();
    expect(page.form.valid).toBe(false); // stuck after the 400, as expected

    // User edits only the password field; identifier is left as-is.
    page.form.controls.password.setValue('anotherPassword1');
    fixture.detectChanges();

    page.onSubmit();
    httpMock.expectOne(`${env.apiBaseUrl}/users/sessions`).flush(
      { id: 'u1', loginName: 'daria_dev', accessToken: '' },
      { status: 200, statusText: 'OK' },
    );
    httpMock.expectOne(`${env.apiBaseUrl}/users/me`).flush({ id: 'u1', loginName: 'daria_dev' });
    httpMock.verify();
  });

  it('correctly shows "required" (not a stale merged server error) when the errored field is cleared to empty after a 400', () => {
    fillValid();
    page.onSubmit();
    httpMock.expectOne(`${env.apiBaseUrl}/users/sessions`).flush(
      { title: 'Validation failed', status: 400, errors: { identifier: ['identifier already in use.'] } },
      { status: 400, statusText: 'Bad Request' },
    );
    fixture.detectChanges();
    expect(page.errorFor('identifier')).toBe('identifier already in use.');

    // Angular's setErrors() replaces the control's errors wholesale, and
    // setValue() re-runs the validator — so clearing the field to empty must
    // show the *current* required state, never a stale mix of both errors.
    page.form.controls.identifier.setValue('');
    fixture.detectChanges();

    expect(page.errorFor('identifier')).toBe('This field is required.');
    httpMock.verify();
  });

  it('clears a stale cross-user profile before hydrating, so a failed hydration never leaves a stale identity (cross-user regression guard)', () => {
    vi.spyOn(router, 'navigate').mockResolvedValue(true);
    // Seed a cached profile as if a previous session left one behind.
    page.form.setValue({ identifier: 'previous_user', password: 'sup3rsecret' });
    page.form.markAsDirty();
    page.onSubmit();
    httpMock
      .expectOne(`${env.apiBaseUrl}/users/sessions`)
      .flush({ id: 'u_old', loginName: 'previous_user', accessToken: '' }, { status: 200, statusText: 'OK' });
    httpMock.expectOne(`${env.apiBaseUrl}/users/me`).flush({ id: 'u_old', loginName: 'previous_user' });
    expect(userService.profile()?.loginName).toBe('previous_user');

    // Now a different user signs in, and hydration fails.
    page.form.setValue({ identifier: 'new_user', password: 'sup3rsecret2' });
    page.form.markAsDirty();
    page.onSubmit();
    httpMock
      .expectOne(`${env.apiBaseUrl}/users/sessions`)
      .flush({ id: 'u_new', loginName: 'new_user', accessToken: '' }, { status: 200, statusText: 'OK' });
    httpMock
      .expectOne(`${env.apiBaseUrl}/users/me`)
      .flush({ title: 'Server error', status: 500 }, { status: 500, statusText: 'Server Error' });

    expect(userService.profile()).toBeNull();
    httpMock.verify();
  });

  it('a non-401/429/400 failure (e.g. 500) on the sign-in call itself shows the server detail in the general error banner', () => {
    fillValid();
    page.onSubmit();

    httpMock.expectOne(`${env.apiBaseUrl}/users/sessions`).flush(
      { title: 'Server error', status: 500, detail: 'Something exploded.' },
      { status: 500, statusText: 'Server Error' },
    );
    fixture.detectChanges();

    expect(page.generalError()).toBe('Something exploded.');
    expect(page.submitting()).toBe(false);
    httpMock.verify();
  });

  it('200 then a failing /users/me still navigates to /dashboard, with no error banner (AC3 / Trap 2 regression guard)', () => {
    const navSpy = vi.spyOn(router, 'navigate').mockResolvedValue(true);
    fillValid();
    page.onSubmit();

    httpMock
      .expectOne(`${env.apiBaseUrl}/users/sessions`)
      .flush({ id: 'u1', loginName: 'daria_dev', accessToken: '' }, { status: 200, statusText: 'OK' });

    httpMock
      .expectOne(`${env.apiBaseUrl}/users/me`)
      .flush({ title: 'Server error', status: 500 }, { status: 500, statusText: 'Server Error' });

    fixture.detectChanges();
    expect(navSpy).toHaveBeenCalledWith(['/dashboard']);
    expect(page.generalError()).toBe('');
    expect(page.submitting()).toBe(false);
    httpMock.verify();
  });

  it('401 renders a single generic "Invalid credentials" banner, with no field errors (AC4 / Trap 3 regression guard)', () => {
    fillValid();
    page.onSubmit();

    httpMock.expectOne(`${env.apiBaseUrl}/users/sessions`).flush(
      { title: 'Unauthorized', status: 401, detail: 'Invalid credentials' },
      { status: 401, statusText: 'Unauthorized' },
    );
    fixture.detectChanges();

    expect(page.generalError()).toBe('Invalid credentials');
    expect(page.form.controls.identifier.errors).toBeNull();
    expect(page.form.controls.password.errors).toBeNull();
    httpMock.verify();
  });

  it('429 with Retry-After shows a friendly message referencing the wait, never a raw code', () => {
    fillValid();
    page.onSubmit();

    httpMock.expectOne(`${env.apiBaseUrl}/users/sessions`).flush(
      { title: 'Too Many Requests', status: 429 },
      { status: 429, statusText: 'Too Many Requests', headers: { 'Retry-After': '120' } },
    );
    fixture.detectChanges();

    expect(page.rateLimitMessage()).not.toContain('429');
    expect(page.rateLimitMessage()).toContain('2 minutes');
    httpMock.verify();
  });

  it('429 without Retry-After still shows a friendly, generic message distinct from the Retry-After copy, no NaN/undefined leaking', () => {
    fillValid();
    page.onSubmit();

    httpMock
      .expectOne(`${env.apiBaseUrl}/users/sessions`)
      .flush({ title: 'Too Many Requests', status: 429 }, { status: 429, statusText: 'Too Many Requests' });
    fixture.detectChanges();

    expect(page.rateLimitMessage()).not.toMatch(/NaN|undefined/);
    expect(page.rateLimitMessage()).not.toContain('minute');
    expect(page.rateLimitMessage()).toContain('please wait a moment');
    httpMock.verify();
  });

  it('429 with a blank Retry-After header falls back to the generic message, not "about a minute"', () => {
    fillValid();
    page.onSubmit();

    httpMock.expectOne(`${env.apiBaseUrl}/users/sessions`).flush(
      { title: 'Too Many Requests', status: 429 },
      { status: 429, statusText: 'Too Many Requests', headers: { 'Retry-After': '' } },
    );
    fixture.detectChanges();

    expect(page.rateLimitMessage()).toContain('please wait a moment');
    expect(page.rateLimitMessage()).not.toContain('minute');
    httpMock.verify();
  });

  it('shows the general banner when a 400 mixes a mapped field with an unmapped one, instead of silently dropping the unmapped message', () => {
    fillValid();
    page.onSubmit();

    httpMock.expectOne(`${env.apiBaseUrl}/users/sessions`).flush(
      {
        title: 'Validation failed',
        status: 400,
        detail: 'Some fields were invalid.',
        errors: {
          identifier: ['identifier is required.'],
          captcha: ['Captcha verification failed.'],
        },
      },
      { status: 400, statusText: 'Bad Request' },
    );
    fixture.detectChanges();

    expect(page.errorFor('identifier')).toBe('identifier is required.');
    expect(page.generalError()).toBe('Some fields were invalid.');
    httpMock.verify();
  });

  it('400 maps server validation errors to inline field errors', () => {
    fillValid();
    page.onSubmit();

    httpMock.expectOne(`${env.apiBaseUrl}/users/sessions`).flush(
      { title: 'Validation failed', status: 400, errors: { identifier: ['identifier is required.'] } },
      { status: 400, statusText: 'Bad Request' },
    );
    fixture.detectChanges();

    expect(page.errorFor('identifier')).toBe('identifier is required.');
    httpMock.verify();
  });

  describe('a11y smoke', () => {
    it('has exactly one h1', () => {
      const h1s = fixture.nativeElement.querySelectorAll('h1');
      expect(h1s.length).toBe(1);
    });

    it('ties every input to a label via for/id', () => {
      const inputs: HTMLInputElement[] = Array.from(
        fixture.nativeElement.querySelectorAll('input'),
      );
      expect(inputs.length).toBe(2);
      for (const input of inputs) {
        const id = input.getAttribute('id');
        expect(id).toBeTruthy();
        const label = fixture.nativeElement.querySelector(`label[for="${id}"]`);
        expect(label).toBeTruthy();
      }
    });

    it('marks inputs aria-invalid and ties error via aria-describedby when errored, with the plain-language message rendered', () => {
      page.form.controls.identifier.setValue('  ');
      page.form.controls.identifier.markAsTouched();
      fixture.detectChanges();

      const identifierInput = fixture.nativeElement.querySelector('input[name="identifier"]');
      expect(identifierInput.getAttribute('aria-invalid')).toBe('true');
      const describedBy = identifierInput.getAttribute('aria-describedby');
      expect(describedBy).toBeTruthy();
      const errorEl = fixture.nativeElement.querySelector(`#${describedBy}`);
      expect(errorEl?.getAttribute('aria-live')).toBe('polite');
      expect(errorEl?.textContent?.trim()).toBe('This field is required.');
    });

    it('sets house autocomplete values: username on identifier, current-password on password', () => {
      const identifierInput = fixture.nativeElement.querySelector('input[name="identifier"]');
      const passwordInput = fixture.nativeElement.querySelector('input[name="password"]');
      expect(identifierInput.getAttribute('autocomplete')).toBe('username');
      expect(passwordInput.getAttribute('autocomplete')).toBe('current-password');
    });
  });

  it('offers a route to sign-up for users without an account (AC11)', () => {
    const link: HTMLAnchorElement = fixture.nativeElement.querySelector('a[routerLink="/sign-up"]');
    expect(link).toBeTruthy();
  });
});
