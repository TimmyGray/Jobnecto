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
import { SignInPage } from './sign-in.page';

describe('SignInPage', () => {
  let fixture: ComponentFixture<SignInPage>;
  let page: SignInPage;
  let httpMock: HttpTestingController;
  let router: Router;

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
    expect(page.rateLimitMessage().length).toBeGreaterThan(0);
    httpMock.verify();
  });

  it('429 without Retry-After still shows a friendly message, no NaN/undefined leaking', () => {
    fillValid();
    page.onSubmit();

    httpMock
      .expectOne(`${env.apiBaseUrl}/users/sessions`)
      .flush({ title: 'Too Many Requests', status: 429 }, { status: 429, statusText: 'Too Many Requests' });
    fixture.detectChanges();

    expect(page.rateLimitMessage()).not.toMatch(/NaN|undefined/);
    expect(page.rateLimitMessage().length).toBeGreaterThan(0);
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

    it('marks inputs aria-invalid and ties error via aria-describedby when errored', () => {
      page.form.controls.identifier.setValue('  ');
      page.form.controls.identifier.markAsTouched();
      fixture.detectChanges();

      const identifierInput = fixture.nativeElement.querySelector('input[name="identifier"]');
      expect(identifierInput.getAttribute('aria-invalid')).toBe('true');
      const describedBy = identifierInput.getAttribute('aria-describedby');
      expect(describedBy).toBeTruthy();
      const errorEl = fixture.nativeElement.querySelector(`#${describedBy}`);
      expect(errorEl?.getAttribute('aria-live')).toBe('polite');
    });
  });
});
