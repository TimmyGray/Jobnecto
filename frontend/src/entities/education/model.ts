import { components } from '@shared/api/generated/schema';

/**
 * Domain models for the `education` entity, re-exported from the
 * OpenAPI-generated schema so they cannot drift from the backend contract.
 *
 * Generated types mark every property optional (OpenAPI default), and numeric
 * fields widen to `number | string` — coerce at the consumer boundary rather
 * than asserting.
 */

/** A single education record as returned by the list and detail endpoints. */
export type EducationResult = components['schemas']['EducationResult'];

/** `200 OK` body returned by `GET /api/v1/educations` (cursor-paginated). */
export type PagedEducations = components['schemas']['PagedResultOfEducationResult'];
