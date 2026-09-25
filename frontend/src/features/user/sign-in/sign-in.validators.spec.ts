import { describe, expect, it } from 'vitest';
import { FormControl } from '@angular/forms';
import { identifierValidator, signInPasswordValidator } from './sign-in.validators';

/** Helper: run a validator against a raw value and return the ValidationErrors (or null). */
function run(validatorFactory: () => (c: FormControl) => unknown, value: string) {
  const control = new FormControl(value);
  return validatorFactory()(control);
}

describe('identifierValidator (non-empty only, Story 1.3 AC7)', () => {
  it('rejects empty as required', () => {
    expect(run(identifierValidator, '')).toEqual({ required: true });
  });

  it('rejects whitespace-only as required', () => {
    expect(run(identifierValidator, '   ')).toEqual({ required: true });
  });

  it('accepts any non-empty trimmed value, no length/format rules', () => {
    expect(run(identifierValidator, 'daria_dev')).toBeNull();
    expect(run(identifierValidator, 'ab')).toBeNull();
    expect(run(identifierValidator, 'not-an-email')).toBeNull();
  });
});

describe('signInPasswordValidator (non-empty only, Story 1.3 AC7 / Trap 4)', () => {
  it('rejects empty as required', () => {
    expect(run(signInPasswordValidator, '')).toEqual({ required: true });
  });

  it('rejects whitespace-only as required', () => {
    expect(run(signInPasswordValidator, '   ')).toEqual({ required: true });
  });

  it('accepts a short value that would fail sign-up min-length rules', () => {
    expect(run(signInPasswordValidator, 'ab')).toBeNull();
  });

  it('accepts a valid value', () => {
    expect(run(signInPasswordValidator, 'sup3rsecret')).toBeNull();
  });
});
