import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import {
  FormControl,
  FormGroup,
  ReactiveFormsModule,
} from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { catchError, finalize, of, switchMap } from 'rxjs';
import { UserService } from '@entities/user';
import { ProblemDetails } from '@shared/api';
import { TextFieldComponent } from '@shared/ui';
import {
  identifierValidator,
  signInPasswordValidator,
} from '@features/user/sign-in/sign-in.validators';

/** Strongly-typed sign-in form shape (typed Reactive Forms — Decision 1.1). */
interface SignInForm {
  identifier: FormControl<string>;
  password: FormControl<string>;
}

/**
 * `/sign-in` page (unguarded). Renders a typed Reactive Form, submits to
 * `POST /api/v1/users/sessions`, and on `200` hydrates `GET /api/v1/users/me`
 * before routing to `/dashboard`. A `401` renders one generic banner (never
 * per-field attribution — Story 1.3 Trap 3); a `429` renders a friendly,
 * rate-limit message honoring `Retry-After`. [AC1-AC11]
 */
@Component({
  selector: 'page-sign-in',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, TextFieldComponent, RouterLink],
  templateUrl: './sign-in.page.html',
})
export class SignInPage {
  private readonly userService = inject(UserService);
  private readonly router = inject(Router);

  /** The typed reactive form. Validators are non-empty-only (Story 1.2 AC7). */
  readonly form = new FormGroup<SignInForm>({
    identifier: new FormControl('', {
      nonNullable: true,
      validators: [identifierValidator()],
    }),
    password: new FormControl('', {
      nonNullable: true,
      validators: [signInPasswordValidator()],
    }),
  });

  /** True while the POST /users/sessions request is in flight (disables submit, shows spinner). */
  readonly submitting = signal(false);
  /** Generic top-level error for non-field, non-rate-limit failures, including 401. */
  readonly generalError = signal<string>('');
  /** Friendly, plain-language rate-limit guidance for a 429. [AC5, AC6] */
  readonly rateLimitMessage = signal<string>('');

  /** Submit is enabled only when the form is dirty AND valid. [AC8] */
  get canSubmit(): boolean {
    return this.form.dirty && this.form.valid && !this.submitting();
  }

  /**
   * Returns the first plain-language error for a field, but only once the
   * control is touched/dirty (validate on blur + submit). [AC8, AC9]
   */
  errorFor(field: keyof SignInForm): string {
    const control = this.form.controls[field];
    if (!control.errors || !(control.touched || control.dirty)) {
      return '';
    }
    return this.messageForErrors(control.errors);
  }

  /** Submits the sign-in. Mirrors validation client-side first. [AC1, AC7] */
  onSubmit(): void {
    this.generalError.set('');
    this.rateLimitMessage.set('');

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    const { identifier, password } = this.form.getRawValue();

    this.userService
      .signIn({ identifier, password })
      .pipe(
        // On 200 the cookie is set; hydrate the profile before landing. A
        // hydration failure must not surface as a sign-in error — the user is
        // authenticated regardless (AC3 / Trap 2).
        switchMap(() => this.userService.fetchCurrentUser().pipe(catchError(() => of(null)))),
        finalize(() => this.submitting.set(false)),
      )
      .subscribe({
        next: () => {
          void this.router.navigate(['/dashboard']);
        },
        error: (problem: ProblemDetails) => this.handleError(problem),
      });
  }

  /** Maps a typed ProblemDetails into a banner, rate-limit message, or inline field errors. [AC4-AC6, AC9] */
  private handleError(problem: ProblemDetails): void {
    if (problem.status === 401) {
      // Never per-field: attributing a 401 to a specific control would leak
      // which half of the credential pair was wrong (Trap 3).
      this.generalError.set('Invalid credentials');
      return;
    }

    if (problem.status === 429) {
      this.rateLimitMessage.set(this.rateLimitCopy(problem.retryAfterSeconds));
      return;
    }

    if (problem.status === 400 && problem.errors) {
      for (const [field, messages] of Object.entries(problem.errors)) {
        const control = this.matchControl(field);
        if (control && messages.length > 0) {
          control.setErrors({ server: messages[0] });
          control.markAsTouched();
        }
      }
      if (!this.anyFieldHasServerError()) {
        this.generalError.set(problem.detail ?? problem.title);
      }
      return;
    }

    this.generalError.set(problem.detail ?? problem.title);
  }

  /** Builds the friendly rate-limit copy, honoring Retry-After when present. [AC6] */
  private rateLimitCopy(retryAfterSeconds?: number): string {
    if (retryAfterSeconds === undefined) {
      return "You've tried several times — please wait a moment and try again.";
    }
    const minutes = Math.ceil(retryAfterSeconds / 60);
    const wait = minutes <= 1 ? 'about a minute' : `about ${minutes} minutes`;
    return `You've tried several times — please wait ${wait} and try again.`;
  }

  /** Resolves a server error field name (case-insensitive) to a form control. */
  private matchControl(field: string): FormControl<string> | null {
    const key = field.toLowerCase();
    if (key === 'identifier') return this.form.controls.identifier;
    if (key === 'password') return this.form.controls.password;
    return null;
  }

  private anyFieldHasServerError(): boolean {
    return Object.values(this.form.controls).some((c) => c.errors?.['server']);
  }

  /** Translates a control's ValidationErrors into one plain-language message. */
  private messageForErrors(errors: Record<string, unknown>): string {
    if (errors['server']) {
      return errors['server'] as string;
    }
    if (errors['required']) {
      return 'This field is required.';
    }
    return 'Please check this field.';
  }
}
