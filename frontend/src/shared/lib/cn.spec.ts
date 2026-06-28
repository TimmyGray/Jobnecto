import { describe, expect, it } from 'vitest';
import { cn } from './cn';

describe('cn', () => {
  it('joins truthy class names with single spaces', () => {
    expect(cn('a', 'b', 'c')).toBe('a b c');
  });

  it('filters out falsy values', () => {
    expect(cn('a', false, null, undefined, '', 'b')).toBe('a b');
  });

  it('returns an empty string when every value is falsy', () => {
    expect(cn(false, null, undefined, '')).toBe('');
  });

  it('returns an empty string when called with no arguments', () => {
    expect(cn()).toBe('');
  });
});
