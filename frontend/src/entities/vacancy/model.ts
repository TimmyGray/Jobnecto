import { components } from '@shared/api/generated/schema';

/**
 * Domain models for the `vacancy` entity, re-exported from the OpenAPI-generated
 * schema so they cannot drift from the backend contract.
 *
 * Generated types mark every property optional (OpenAPI default), and numeric
 * fields widen to `number | string` — coerce at the consumer boundary rather
 * than asserting.
 */

/** A single vacancy projection as returned by the browse/filter endpoint. */
export type VacancyListItem = components['schemas']['VacancyListItemResult'];

/** `200 OK` body returned by `POST /api/v1/vacancies/filter` (cursor-paginated). */
export type PagedVacancies = components['schemas']['PagedResultOfVacancyListItemResult'];

/** Request body for `POST /api/v1/vacancies/filter`; empty criteria means browse mode. */
export type FilterVacanciesQuery = components['schemas']['FilterVacanciesQuery'];
