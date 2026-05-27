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
import { SignUpPage } from './sign-up.page';

describe('SignUpPage', () => {
  let fixture: ComponentFixture<SignUpPage>;
  let page: SignUpPage;
  let httpMock: HttpTestingController;
  let router: Router;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SignUpPage],
      providers: [
        provideHttpClient(withInterceptors([httpInterceptor])),
        provideHttpClientTesting(),
        provideRouter([]),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(SignUpPage);
    page = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
    fixture.detectChanges();
  });

  /** Fills the typed reactive form with valid values and marks it dirty. */
  function fillValid() {
    page.form.setValue({
      loginName: 'daria_k',
      email: 'daria@example.com',
      password: 'sup3rsecret',
    });
    page.form.markAsDirty();
    fixture.detectChanges();
  }

  it('disables submit until the form is dirty and valid', () => {
    expect(page.canSubmit).toBe(false); // pristine
    fillValid();
    expect(page.canSubmit).toBe(true);
  });

  it('on 201 registers, hydrates /users/me, and routes to /dashboard', async () => {
    const navSpy = vi.spyOn(router, 'navigate').mockResolvedValue(true);
    fillValid();
    page.onSubmit();

    const registerReq = httpMock.expectOne(`${env.apiBaseUrl}/users`);
    expect(registerReq.request.method).toBe('POST');
    registerReq.flush(
      { id: 'u1', loginName: 'daria_k', email: 'daria@example.com' },
      { status: 201, statusText: 'Created' },
    );

    const meReq = httpMock.expectOne(`${env.apiBaseUrl}/users/me`);
    expect(meReq.request.method).toBe('GET');
    meReq.flush({ id: 'u1', loginName: 'daria_k', email: 'daria@example.com' });

    expect(navSpy).toHaveBeenCalledWith(['/dashboard']);
    expect(page.submitting()).toBe(false);
    httpMock.verify();
  });

  it('on 400 maps server validation errors to inline field errors', () => {
    fillValid();
    page.onSubmit();

    httpMock.expectOne(`${env.apiBaseUrl}/users`).flush(
      {
        title: 'Validation failed',
        status: 400,
        errors: { email: ['email must be a valid email address.'] },
      },
      { status: 400, statusText: 'Bad Request' },
    );

    fixture.detectChanges();
    expect(page.errorFor('email')).toBe('email must be a valid email address.');
    expect(page.conflictMessage()).toBe('');
    httpMock.verify();
  });

  it('on 409 shows plain-language conflict guidance, never a raw code', () => {
    fillValid();
    page.onSubmit();

    httpMock.expectOne(`${env.apiBaseUrl}/users`).flush(
      { title: 'Conflict', status: 409, detail: 'Email already registered.' },
      { status: 409, statusText: 'Conflict' },
    );

    fixture.detectChanges();
    expect(page.conflictMessage()).toContain('already registered');
    expect(page.conflictMessage()).not.toContain('409');
    httpMock.verify();
  });

  it('does not call the API when the form is invalid on submit', () => {
    page.form.setValue({ loginName: 'ab', email: 'bad', password: '123' });
    page.form.markAsDirty();
    page.onSubmit();
    httpMock.expectNone(`${env.apiBaseUrl}/users`);
    expect(page.form.controls.loginName.touched).toBe(true);
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
      expect(inputs.length).toBe(3);
      for (const input of inputs) {
        const id = input.getAttribute('id');
        expect(id).toBeTruthy();
        const label = fixture.nativeElement.querySelector(`label[for="${id}"]`);
        expect(label).toBeTruthy();
      }
    });

    it('marks inputs aria-invalid and ties error via aria-describedby when errored', () => {
      page.form.controls.email.setValue('bad');
      page.form.controls.email.markAsTouched();
      fixture.detectChanges();

      const emailInput = fixture.nativeElement.querySelector('input[type="email"]');
      expect(emailInput.getAttribute('aria-invalid')).toBe('true');
      const describedBy = emailInput.getAttribute('aria-describedby');
      expect(describedBy).toBeTruthy();
      const errorEl = fixture.nativeElement.querySelector(`#${describedBy}`);
      expect(errorEl?.getAttribute('aria-live')).toBe('polite');
    });
  });
});
