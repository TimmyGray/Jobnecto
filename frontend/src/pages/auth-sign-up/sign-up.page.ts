import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import {
  FormControl,
  FormGroup,
  ReactiveFormsModule,
} from '@angular/forms';
import { Router } from '@angular/router';
import { finalize, switchMap } from 'rxjs';
import { UserService } from '@entities/user';
import { ProblemDetails } from '@shared/api';
import { TextFieldComponent } from '@shared/ui';
import {
  emailValidator,
  loginNameValidator,
  passwordValidator,
} from '@features/user/sign-up/sign-up.validators';

/** Strongly-typed sign-up form shape (typed Reactive Forms — Decision 1.1). */
interface SignUpForm {
  loginName: FormControl<string>;
  email: FormControl<string>;
  password: FormControl<string>;
}

/**
 * `/sign-up` page (unguarded). Renders a typed Reactive Form mirroring the
 * backend validator, submits to `POST /api/v1/users`, and on `201` routes to
 * `/dashboard` and hydrates `GET /api/v1/users/me`. Maps `400` → inline field
 * errors and `409` → plain-language conflict guidance. [AC6–AC10]
 */
@Component({
  selector: 'page-sign-up',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, TextFieldComponent],
  templateUrl: './sign-up.page.html',
})
export class SignUpPage {
  private readonly userService = inject(UserService);
  private readonly router = inject(Router);

  /** The typed reactive form. Validators mirror CreateUserCommandValidator. */
  readonly form = new FormGroup<SignUpForm>({
    loginName: new FormControl('', {
      nonNullable: true,
      validators: [loginNameValidator()],
    }),
    email: new FormControl('', {
      nonNullable: true,
      validators: [emailValidator()],
    }),
    password: new FormControl('', {
      nonNullable: true,
      validators: [passwordValidator()],
    }),
  });

  /** True while the POST /users request is in flight (disables submit, shows spinner). */
  readonly submitting = signal(false);
  /** Plain-language conflict guidance for a 409 (email/login already used). [AC9] */
  readonly conflictMessage = signal<string>('');
  /** Generic top-level error for non-field, non-conflict failures. */
  readonly generalError = signal<string>('');

  /** Submit is enabled only when the form is dirty AND valid. [AC6] */
  get canSubmit(): boolean {
    return this.form.dirty && this.form.valid && !this.submitting();
  }

  /**
   * Returns the first plain-language error for a field, but only once the
   * control is touched/dirty (validate on blur + submit). [AC6, AC8]
   */
  errorFor(field: keyof SignUpForm): string {
    const control = this.form.controls[field];
    if (!control.errors || !(control.touched || control.dirty)) {
      return '';
    }
    return this.messageForErrors(field, control.errors);
  }

  /** Submits the registration. Mirrors validation client-side first. [AC6, AC7] */
  onSubmit(): void {
    this.conflictMessage.set('');
    this.generalError.set('');

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    const { loginName, email, password } = this.form.getRawValue();

    this.userService
      .register({ loginName, email, password })
      .pipe(
        // On 201 the cookie is set; hydrate the profile before landing.
        switchMap(() => this.userService.fetchCurrentUser()),
        finalize(() => this.submitting.set(false)),
      )
      .subscribe({
        next: () => {
          void this.router.navigate(['/dashboard']);
        },
        error: (problem: ProblemDetails) => this.handleError(problem),
      });
  }

  /** Maps a typed ProblemDetails into inline field errors / conflict / generic copy. [AC8, AC9] */
  private handleError(problem: ProblemDetails): void {
    if (problem.status === 409) {
      this.conflictMessage.set(
        'That email or login is already registered — try signing in instead.',
      );
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
      // If the 400 had no field we recognize, show a generic banner.
      if (!this.anyFieldHasServerError()) {
        this.generalError.set(problem.detail ?? problem.title);
      }
      return;
    }

    this.generalError.set(problem.detail ?? problem.title);
  }

  /** Resolves a server error field name (case-insensitive) to a form control. */
  private matchControl(field: string): FormControl<string> | null {
    const key = field.toLowerCase();
    if (key === 'loginname') return this.form.controls.loginName;
    if (key === 'email') return this.form.controls.email;
    if (key === 'password') return this.form.controls.password;
    return null;
  }

  private anyFieldHasServerError(): boolean {
    return Object.values(this.form.controls).some((c) => c.errors?.['server']);
  }

  /** Translates a control's ValidationErrors into one plain-language message. */
  private messageForErrors(field: keyof SignUpForm, errors: Record<string, unknown>): string {
    if (errors['server']) {
      return errors['server'] as string;
    }
    if (errors['required']) {
      return `${this.fieldLabel(field)} is required.`;
    }
    switch (field) {
      case 'loginName':
        if (errors['length']) {
          return 'Login name must be between 3 and 50 characters.';
        }
        if (errors['pattern']) {
          return 'Login name may only contain letters, numbers, or underscore.';
        }
        break;
      case 'email':
        if (errors['email']) {
          return 'Enter a valid email address.';
        }
        if (errors['maxlength']) {
          return 'Email must be at most 50 characters.';
        }
        break;
      case 'password':
        if (errors['minlength']) {
          return 'Password must be at least 8 characters.';
        }
        if (errors['maxlength']) {
          return 'Password must be at most 50 characters.';
        }
        break;
    }
    return 'Please check this field.';
  }

  private fieldLabel(field: keyof SignUpForm): string {
    switch (field) {
      case 'loginName':
        return 'Login name';
      case 'email':
        return 'Email';
      case 'password':
        return 'Password';
    }
  }
}
