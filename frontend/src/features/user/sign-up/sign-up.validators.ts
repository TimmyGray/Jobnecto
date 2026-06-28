import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

/**
 * Client-side validators that MIRROR the backend `CreateUserCommandValidator`
 * EXACTLY, so the form blocks submissions the server would reject and the two
 * cannot drift. [AC6; Source: CreateUserCommandValidator.cs]
 *
 * Backend rules mirrored:
 *  - loginName: required, length 3–50, regex `^[A-Za-z0-9_]+$`
 *  - email:     required, valid email, max length 50
 *  - password:  required, min length 8, max length 50
 *
 * Each validator returns a single, deterministic error key so templates can map
 * to the same plain-language messaging the backend uses.
 */

/** Allowed loginName characters (letters, digits, underscore). */
export const LOGIN_NAME_PATTERN = /^[A-Za-z0-9_]+$/;

export const LOGIN_NAME_MIN = 3;
export const LOGIN_NAME_MAX = 50;
export const EMAIL_MAX = 50;
export const PASSWORD_MIN = 8;
export const PASSWORD_MAX = 50;

/**
 * Pragmatic email shape check. Mirrors FluentValidation's `.EmailAddress()`
 * intent (a single `@`, non-empty local part, a dotted domain) without being
 * stricter than the server, so the client never blocks an address the API
 * would accept.
 */
export const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

/** Required, length 3–50, and matching the allowed-character pattern. */
export function loginNameValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = (control.value ?? '') as string;
    if (value.length === 0) {
      return { required: true };
    }
    if (value.length < LOGIN_NAME_MIN || value.length > LOGIN_NAME_MAX) {
      return { length: { min: LOGIN_NAME_MIN, max: LOGIN_NAME_MAX } };
    }
    if (!LOGIN_NAME_PATTERN.test(value)) {
      return { pattern: true };
    }
    return null;
  };
}

/** Required, valid email shape, max length 50. */
export function emailValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = (control.value ?? '') as string;
    if (value.length === 0) {
      return { required: true };
    }
    if (!EMAIL_PATTERN.test(value)) {
      return { email: true };
    }
    if (value.length > EMAIL_MAX) {
      return { maxlength: { max: EMAIL_MAX } };
    }
    return null;
  };
}

/** Required, length 8–50. */
export function passwordValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = (control.value ?? '') as string;
    if (value.length === 0) {
      return { required: true };
    }
    if (value.length < PASSWORD_MIN) {
      return { minlength: { min: PASSWORD_MIN } };
    }
    if (value.length > PASSWORD_MAX) {
      return { maxlength: { max: PASSWORD_MAX } };
    }
    return null;
  };
}
