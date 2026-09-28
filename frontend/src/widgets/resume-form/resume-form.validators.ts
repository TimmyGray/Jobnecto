import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

/**
 * Client-side validators for the résumé create form, mirroring
 * `CreateResumeCommandValidator` exactly (Story 2.1 AC1). Every field here is
 * optional at the backend — these only fire once a value is present.
 */

/** Title: optional, but at most 500 characters when present. */
export function resumeTitleValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = (control.value ?? '') as string;
    return value.length > 500 ? { maxlength: true } : null;
  };
}

/**
 * Salary: optional, but must be a finite number >= 0 when present.
 * `TextFieldComponent` can never actually produce `NaN`/`Infinity` from a
 * real `type="number"` input (the browser's own value-sanitization resets
 * unparseable/non-finite text to `''` before this validator ever sees it —
 * verified in `text-field.spec.ts`) — the finiteness check here is
 * defense-in-depth against a non-DOM caller setting the value directly.
 */
export function nonNegativeValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = control.value;
    if (value === null || value === undefined || value === '') {
      return null;
    }
    const numeric = Number(value);
    return !Number.isFinite(numeric) || numeric < 0 ? { min: true } : null;
  };
}

/**
 * Enum membership, case-insensitive, only when non-empty — mirrors the
 * backend's `Enum.TryParse<TEnum>(value, true, out _)` check.
 * @param members The valid member names for this enum.
 */
export function enumMemberValidator(members: readonly string[]): ValidatorFn {
  const lowerMembers = new Set(members.map((m) => m.toLowerCase()));
  return (control: AbstractControl): ValidationErrors | null => {
    const value = (control.value ?? '') as string;
    if (value.length === 0) {
      return null;
    }
    return lowerMembers.has(value.toLowerCase()) ? null : { enum: true };
  };
}

/**
 * Enum membership for every element of an array control, case-insensitive —
 * the array-valued equivalent of {@link enumMemberValidator} (for `locations`,
 * a `FormControl<Location[]>`). An empty array is valid.
 * @param members The valid member names for this enum.
 */
export function enumArrayMemberValidator(members: readonly string[]): ValidatorFn {
  const lowerMembers = new Set(members.map((m) => m.toLowerCase()));
  return (control: AbstractControl): ValidationErrors | null => {
    const values = (control.value ?? []) as string[];
    return values.every((v) => lowerMembers.has(v.toLowerCase())) ? null : { enum: true };
  };
}

/**
 * Applied to a language-proficiency row's `FormGroup`. A row must be either
 * fully empty (not yet filled in) or fully filled (both language and level)
 * — exactly one of the two is invalid, since `buildCommand()` silently drops
 * a row unless both fields are set, and doing that without surfacing an
 * error would lose data the user thought they'd saved.
 */
export function languageRowValidator(): ValidatorFn {
  return (group: AbstractControl): ValidationErrors | null => {
    const hasLanguage = !!group.get('language')?.value;
    const hasLevel = !!group.get('level')?.value;
    return hasLanguage !== hasLevel ? { incomplete: true } : null;
  };
}
