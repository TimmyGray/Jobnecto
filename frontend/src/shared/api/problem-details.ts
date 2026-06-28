/**
 * Typed RFC 7807 Problem Details model and normalization.
 *
 * The backend returns `application/problem+json` on every non-2xx response.
 * 400 (validation) carries an `errors` map (field -> messages). 409 is a
 * conflict (email/login already used). `traceId` arrives in the extensions.
 * [AC3, AC8, AC9; FE Guide §6.2]
 */

/** Field-level validation errors: field name -> list of human-readable messages. */
export type ProblemErrors = Record<string, string[]>;

/** Normalized, typed Problem Details surfaced to the rest of the app. */
export interface ProblemDetails {
  /** HTTP status code (e.g. 400, 409, 500). 0 when the request never reached the server. */
  status: number;
  /** Short, human-readable summary of the problem type. */
  title: string;
  /** Human-readable explanation specific to this occurrence. */
  detail?: string;
  /** URI reference identifying the problem type. */
  type?: string;
  /** URI reference identifying the specific occurrence. */
  instance?: string;
  /** Field-level validation errors (present on 400). */
  errors?: ProblemErrors;
  /** Correlation id pulled from the RFC 7807 extensions, if present. */
  traceId?: string;
  /** Optional machine-readable error code from extensions (e.g. generation_timeout). */
  code?: string;
}

/** Generic fallback title when the body carries none. */
const DEFAULT_TITLE = 'Something went wrong';

/**
 * Normalizes an arbitrary error body + HTTP status into a typed {@link ProblemDetails}.
 *
 * Accepts a parsed RFC 7807 body (or any object/string/null), tolerates missing
 * fields, and lifts `traceId`/`code` from either the top level or the
 * `extensions` bag. Never throws.
 *
 * @param body The response body (already parsed by HttpClient where possible).
 * @param status The HTTP status code from the response.
 * @returns A fully-populated, typed ProblemDetails.
 */
export function normalizeProblemDetails(body: unknown, status: number): ProblemDetails {
  const safeStatus = typeof status === 'number' && !Number.isNaN(status) ? status : 0;

  // Non-object bodies (string, null, undefined) -> generic problem.
  if (body === null || body === undefined || typeof body !== 'object') {
    return {
      status: safeStatus,
      title: typeof body === 'string' && body.trim().length > 0 ? body : DEFAULT_TITLE,
    };
  }

  const record = body as Record<string, unknown>;
  const extensions =
    typeof record['extensions'] === 'object' && record['extensions'] !== null
      ? (record['extensions'] as Record<string, unknown>)
      : {};

  const errors = normalizeErrors(record['errors']);

  return {
    status: typeof record['status'] === 'number' ? (record['status'] as number) : safeStatus,
    title: typeof record['title'] === 'string' && record['title'] ? (record['title'] as string) : DEFAULT_TITLE,
    detail: typeof record['detail'] === 'string' ? (record['detail'] as string) : undefined,
    type: typeof record['type'] === 'string' ? (record['type'] as string) : undefined,
    instance: typeof record['instance'] === 'string' ? (record['instance'] as string) : undefined,
    errors,
    traceId: pickString(record['traceId']) ?? pickString(extensions['traceId']),
    code: pickString(record['code']) ?? pickString(extensions['code']),
  };
}

/** Coerces an unknown value into a string when it is a non-empty string, else undefined. */
function pickString(value: unknown): string | undefined {
  return typeof value === 'string' && value.length > 0 ? value : undefined;
}

/**
 * Normalizes the RFC 7807 `errors` map into a `Record<string, string[]>`.
 * Tolerates single-string values (coerced to a one-element array) and drops
 * non-string entries. Returns undefined when there are no usable errors.
 */
function normalizeErrors(raw: unknown): ProblemErrors | undefined {
  if (raw === null || raw === undefined || typeof raw !== 'object') {
    return undefined;
  }

  const result: ProblemErrors = {};
  for (const [field, value] of Object.entries(raw as Record<string, unknown>)) {
    if (Array.isArray(value)) {
      const messages = value.filter((m): m is string => typeof m === 'string');
      if (messages.length > 0) {
        result[field] = messages;
      }
    } else if (typeof value === 'string' && value.length > 0) {
      result[field] = [value];
    }
  }

  return Object.keys(result).length > 0 ? result : undefined;
}
