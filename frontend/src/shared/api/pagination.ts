/**
 * Cursor-pagination contract shared by every list endpoint (AR16).
 *
 * List responses carry `{ items, totalCount, lastSeenId, lastSeenUpdatedAt,
 * pageSize, hasNext, totalPages }`; the next page echoes both cursor fields
 * back. Load-more, never page numbers. The envelope itself is generated per
 * entity (`PagedResultOfResumeResult`, …) — only the request-side cursor is
 * hand-written, because it is identical across entities.
 */

/** Both cursor fields from a previous page, identifying where the next page starts. */
export interface PageCursor {
  lastSeenId: string;
  lastSeenUpdatedAt: string;
}

/**
 * Derives the cursor for the next page from a list response.
 *
 * The generated envelope types both cursor fields as nullable, so this is the
 * single place that narrows them: a cursor is only produced when the server
 * says another page exists *and* both fields are usable. A half-populated
 * cursor yields null, which stops "load more" rather than sending a malformed
 * request.
 * @param page The `hasNext` flag and cursor fields of the page just loaded.
 * @returns The cursor to request the next page with, or null when there is none.
 */
export function nextCursor(page: {
  hasNext: boolean;
  lastSeenId: string | null;
  lastSeenUpdatedAt: string | null;
}): PageCursor | null {
  if (!page.hasNext || !page.lastSeenId || !page.lastSeenUpdatedAt) {
    return null;
  }
  return { lastSeenId: page.lastSeenId, lastSeenUpdatedAt: page.lastSeenUpdatedAt };
}

/**
 * Coerces a paged response's `totalCount` (typed `number | string`) to a real
 * number, or `null` if the body is missing or malformed. A bare `Number(...)`
 * would let a missing field become `NaN`, which fails every `=== null` guard
 * and renders as the literal text "NaN".
 * @param totalCount The raw `totalCount` field from a paged response.
 * @returns A finite number, or null when the value can't be coerced to one.
 */
export function toCount(totalCount: unknown): number | null {
  // Number(null), Number('') and Number('  ') are all 0, which would read as
  // "you have zero" next to a populated list; treat them as unknown instead.
  if (totalCount === null || (typeof totalCount === 'string' && totalCount.trim() === '')) {
    return null;
  }
  const value = Number(totalCount);
  return Number.isFinite(value) ? value : null;
}
