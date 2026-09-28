import { describe, expect, it } from 'vitest';
import {
  CURRENCY_OPTIONS,
  EXPERIENCE_OPTIONS,
  humanizeEnumMember,
  LANGUAGE_LEVEL_OPTIONS,
  LANGUAGE_OPTIONS,
  LOCATION_OPTIONS,
  WORK_LOCATION_TYPE_OPTIONS,
} from './resume-enums';

describe('humanizeEnumMember', () => {
  it('inserts a space before a capital that follows a lowercase letter', () => {
    expect(humanizeEnumMember('OnSite')).toBe('On Site');
    expect(humanizeEnumMember('LessThanOneYear')).toBe('Less Than One Year');
    expect(humanizeEnumMember('CzechRepublic')).toBe('Czech Republic');
    expect(humanizeEnumMember('HongKong')).toBe('Hong Kong');
  });

  it('leaves consecutive capitals (acronyms) untouched', () => {
    expect(humanizeEnumMember('USD')).toBe('USD');
  });

  it('leaves a single word untouched', () => {
    expect(humanizeEnumMember('Remote')).toBe('Remote');
  });
});

describe('resume enum option lists', () => {
  it('WORK_LOCATION_TYPE_OPTIONS covers every backend member with a humanized label', () => {
    expect(WORK_LOCATION_TYPE_OPTIONS).toEqual([
      { value: 'OnSite', label: 'On Site' },
      { value: 'Remote', label: 'Remote' },
      { value: 'Hybrid', label: 'Hybrid' },
    ]);
  });

  it('EXPERIENCE_OPTIONS has all 4 members', () => {
    expect(EXPERIENCE_OPTIONS).toHaveLength(4);
    expect(EXPERIENCE_OPTIONS).toContainEqual({
      value: 'LessThanOneYear',
      label: 'Less Than One Year',
    });
  });

  it('CURRENCY_OPTIONS has all 18 members with unhumanized (all-caps) labels', () => {
    expect(CURRENCY_OPTIONS).toHaveLength(18);
    expect(CURRENCY_OPTIONS).toContainEqual({ value: 'USD', label: 'USD' });
  });

  it('LANGUAGE_LEVEL_OPTIONS has all 4 members', () => {
    expect(LANGUAGE_LEVEL_OPTIONS).toHaveLength(4);
  });

  it('LANGUAGE_OPTIONS has all 38 members', () => {
    expect(LANGUAGE_OPTIONS).toHaveLength(38);
    expect(LANGUAGE_OPTIONS).toContainEqual({ value: 'English', label: 'English' });
    expect(LANGUAGE_OPTIONS).toContainEqual({ value: 'Urdu', label: 'Urdu' });
  });

  it('LOCATION_OPTIONS has all 122 members with humanized labels', () => {
    expect(LOCATION_OPTIONS).toHaveLength(122);
    expect(LOCATION_OPTIONS).toContainEqual({ value: 'Ukraine', label: 'Ukraine' });
    expect(LOCATION_OPTIONS).toContainEqual({ value: 'UnitedStates', label: 'United States' });
    expect(LOCATION_OPTIONS).toContainEqual({
      value: 'BosniaAndHerzegovina',
      label: 'Bosnia And Herzegovina',
    });
  });
});
