import { describe, expect, it } from 'vitest';
import { humanizeEnumMember } from './humanize';

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
