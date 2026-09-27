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
