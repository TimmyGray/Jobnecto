import { components } from '@shared/api/generated/schema';

/**
 * Domain models for the `resume` entity, re-exported from the OpenAPI-generated
 * schema so they cannot drift from the backend contract.
 *
 * Generated types mark every property optional (OpenAPI default), and numeric
 * fields widen to `number | string` — coerce at the consumer boundary rather
 * than asserting.
 */

/** A single résumé projection as returned by the list and detail endpoints. */
export type ResumeResult = components['schemas']['ResumeResult'];

/** `200 OK` body returned by `GET /api/v1/resumes` (cursor-paginated). */
export type PagedResumes = components['schemas']['PagedResultOfResumeResult'];
