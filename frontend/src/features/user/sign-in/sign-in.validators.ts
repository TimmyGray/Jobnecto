import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

/**
 * Client-side validators for the sign-in form. Deliberately **non-empty only**
 * — mirroring the backend sign-in validator exactly (Story 1.2 AC7). No
 * length/format/regex rules: those would leak which half of the credential
 * pair is shaped wrong, and would lock out returning users whose existing
 * password predates current sign-up rules. Do not reuse sign-up's validators
 * here (Story 1.3 Trap 4). [AC7]
 */

/** Required after trim. Used for the identifier field (email or login name). */
export function identifierValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = ((control.value ?? '') as string).trim();
    return value.length === 0 ? { required: true } : null;
  };
}

/** Required after trim. Used for the password field — no min/max length. */
export function signInPasswordValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = ((control.value ?? '') as string).trim();
    return value.length === 0 ? { required: true } : null;
  };
}
