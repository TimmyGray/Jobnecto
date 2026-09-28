import { describe, expect, it } from 'vitest';
import { FormControl } from '@angular/forms';
import {
  enumArrayMemberValidator,
  enumMemberValidator,
  nonNegativeValidator,
  resumeTitleValidator,
} from './resume-form.validators';

describe('resumeTitleValidator', () => {
  const validator = resumeTitleValidator();

  it('allows an empty title', () => {
    expect(validator(new FormControl(''))).toBeNull();
  });

  it('allows a title up to 500 characters', () => {
    expect(validator(new FormControl('x'.repeat(500)))).toBeNull();
  });

  it('rejects a title over 500 characters', () => {
    expect(validator(new FormControl('x'.repeat(501)))).toEqual({ maxlength: true });
  });
});

describe('nonNegativeValidator', () => {
  const validator = nonNegativeValidator();

  it('allows null', () => {
    expect(validator(new FormControl(null))).toBeNull();
  });

  it('allows zero', () => {
    expect(validator(new FormControl(0))).toBeNull();
  });

  it('allows a positive number', () => {
    expect(validator(new FormControl(50000))).toBeNull();
  });

  it('rejects a negative number', () => {
    expect(validator(new FormControl(-1))).toEqual({ min: true });
  });

  it('rejects NaN (defense-in-depth — TextFieldComponent cannot produce this from a real number input, but a non-DOM caller could)', () => {
    expect(validator(new FormControl(NaN))).toEqual({ min: true });
  });

  it('rejects Infinity', () => {
    expect(validator(new FormControl(Infinity))).toEqual({ min: true });
  });
});

describe('enumMemberValidator', () => {
  const validator = enumMemberValidator(['OnSite', 'Remote', 'Hybrid']);

  it('allows an empty value', () => {
    expect(validator(new FormControl(''))).toBeNull();
  });

  it('allows an exact member', () => {
    expect(validator(new FormControl('Remote'))).toBeNull();
  });

  it('allows a member case-insensitively (mirrors Enum.TryParse(.., true, ..))', () => {
    expect(validator(new FormControl('remote'))).toBeNull();
  });

  it('rejects a value that is not a member', () => {
    expect(validator(new FormControl('FromHome'))).toEqual({ enum: true });
  });
});

describe('enumArrayMemberValidator', () => {
  const validator = enumArrayMemberValidator(['Ukraine', 'Poland', 'Germany']);

  it('allows an empty array', () => {
    expect(validator(new FormControl([]))).toBeNull();
  });

  it('allows every element that is a member, case-insensitively', () => {
    expect(validator(new FormControl(['Ukraine', 'poland']))).toBeNull();
  });

  it('rejects when any element is not a member', () => {
    expect(validator(new FormControl(['Ukraine', 'Atlantis']))).toEqual({ enum: true });
  });
});
