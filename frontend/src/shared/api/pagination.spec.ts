import { describe, expect, it } from 'vitest';
import { nextCursor, toCount } from './pagination';

describe('nextCursor', () => {
  const ID = '3f2b8c1e-0000-4000-8000-000000000001';
  const UPDATED = '2026-09-28T10:00:00Z';

  it('returns null when there is no next page, even if cursor fields are present', () => {
    expect(nextCursor({ hasNext: false, lastSeenId: ID, lastSeenUpdatedAt: UPDATED })).toBeNull();
  });

  it('returns null when hasNext is true but lastSeenId is missing', () => {
    expect(nextCursor({ hasNext: true, lastSeenId: null, lastSeenUpdatedAt: UPDATED })).toBeNull();
  });

  it('returns null when hasNext is true but lastSeenUpdatedAt is missing', () => {
    expect(nextCursor({ hasNext: true, lastSeenId: ID, lastSeenUpdatedAt: null })).toBeNull();
  });

  it('returns null for empty-string cursor fields', () => {
    expect(nextCursor({ hasNext: true, lastSeenId: '', lastSeenUpdatedAt: UPDATED })).toBeNull();
    expect(nextCursor({ hasNext: true, lastSeenId: ID, lastSeenUpdatedAt: '' })).toBeNull();
  });

  it('returns both cursor fields when the next page exists', () => {
    expect(nextCursor({ hasNext: true, lastSeenId: ID, lastSeenUpdatedAt: UPDATED })).toEqual({
      lastSeenId: ID,
      lastSeenUpdatedAt: UPDATED,
    });
  });
});

describe('toCount', () => {
  it('passes a finite number through', () => {
    expect(toCount(12)).toBe(12);
    expect(toCount(0)).toBe(0);
  });

  it('coerces a numeric string', () => {
    expect(toCount('12')).toBe(12);
  });

  it('returns null for a missing, malformed, or non-finite value', () => {
    expect(toCount(undefined)).toBeNull();
    expect(toCount('abc')).toBeNull();
    expect(toCount(Number.NaN)).toBeNull();
    expect(toCount(Number.POSITIVE_INFINITY)).toBeNull();
  });

  it('treats null and blank strings as unknown, not zero', () => {
    expect(toCount(null)).toBeNull();
    expect(toCount('')).toBeNull();
    expect(toCount('   ')).toBeNull();
  });
});
