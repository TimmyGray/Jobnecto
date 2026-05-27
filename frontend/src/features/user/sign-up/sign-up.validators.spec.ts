import { describe, expect, it } from 'vitest';
import { FormControl } from '@angular/forms';
import {
  emailValidator,
  loginNameValidator,
  passwordValidator,
} from './sign-up.validators';

/** Helper: run a validator against a raw value and return the ValidationErrors (or null). */
function run(validatorFactory: () => (c: FormControl) => unknown, value: string) {
  const control = new FormControl(value);
  return validatorFactory()(control);
}

describe('loginNameValidator (mirrors CreateUserCommandValidator)', () => {
  it('rejects empty as required', () => {
    expect(run(loginNameValidator, '')).toEqual({ required: true });
  });

  it('rejects length 2 (below min boundary)', () => {
    expect(run(loginNameValidator, 'ab')).toEqual({
      length: { min: 3, max: 50 },
    });
  });

  it('accepts length 3 (min boundary)', () => {
    expect(run(loginNameValidator, 'abc')).toBeNull();
  });

  it('accepts length 50 (max boundary)', () => {
    expect(run(loginNameValidator, 'a'.repeat(50))).toBeNull();
  });

  it('rejects length 51 (above max boundary)', () => {
    expect(run(loginNameValidator, 'a'.repeat(51))).toEqual({
      length: { min: 3, max: 50 },
    });
  });

  it('rejects disallowed characters via pattern', () => {
    expect(run(loginNameValidator, 'bad name')).toEqual({ pattern: true });
    expect(run(loginNameValidator, 'bad-name')).toEqual({ pattern: true });
    expect(run(loginNameValidator, 'bad@name')).toEqual({ pattern: true });
  });

  it('accepts letters, digits and underscore', () => {
    expect(run(loginNameValidator, 'Good_Name_123')).toBeNull();
  });
});

describe('emailValidator (mirrors CreateUserCommandValidator)', () => {
  it('rejects empty as required', () => {
    expect(run(emailValidator, '')).toEqual({ required: true });
  });

  it('rejects an invalid email shape', () => {
    expect(run(emailValidator, 'not-an-email')).toEqual({ email: true });
    expect(run(emailValidator, 'missing@domain')).toEqual({ email: true });
  });

  it('accepts a valid email', () => {
    expect(run(emailValidator, 'daria@example.com')).toBeNull();
  });

  it('accepts an email exactly 50 chars long', () => {
    // local(37) + "@ex.com"(13... build precisely to 50)
    const email = 'a'.repeat(40) + '@example.com'; // 40 + 12 = 52 -> too long; build 50 below
    void email;
    const exact50 = 'a'.repeat(38) + '@example.com'; // 38 + 12 = 50
    expect(exact50.length).toBe(50);
    expect(run(emailValidator, exact50)).toBeNull();
  });

  it('rejects an email longer than 50 chars (max boundary)', () => {
    const over50 = 'a'.repeat(39) + '@example.com'; // 39 + 12 = 51
    expect(over50.length).toBe(51);
    expect(run(emailValidator, over50)).toEqual({ maxlength: { max: 50 } });
  });
});

describe('passwordValidator (mirrors CreateUserCommandValidator)', () => {
  it('rejects empty as required', () => {
    expect(run(passwordValidator, '')).toEqual({ required: true });
  });

  it('rejects length 7 (below min boundary)', () => {
    expect(run(passwordValidator, 'a'.repeat(7))).toEqual({
      minlength: { min: 8 },
    });
  });

  it('accepts length 8 (min boundary)', () => {
    expect(run(passwordValidator, 'a'.repeat(8))).toBeNull();
  });

  it('accepts length 50 (max boundary)', () => {
    expect(run(passwordValidator, 'a'.repeat(50))).toBeNull();
  });

  it('rejects length 51 (above max boundary)', () => {
    expect(run(passwordValidator, 'a'.repeat(51))).toEqual({
      maxlength: { max: 50 },
    });
  });
});
