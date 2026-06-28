import { components } from '@shared/api/generated/schema';

/**
 * Domain models for the `user` entity, re-exported from the OpenAPI-generated
 * schema so they cannot drift from the backend contract. [AC4]
 *
 * Generated types mark every property optional (OpenAPI default). The narrowed
 * aliases below express the request/response shapes used by the sign-up flow.
 */

/** Request body for `POST /api/v1/users` (registration). */
export type CreateUserCommand = components['schemas']['CreateUserCommand'];

/** `201 Created` body returned by `POST /api/v1/users`. */
export type CreateUserResult = components['schemas']['CreateUserResult'];

/** `200 OK` body returned by `GET /api/v1/users/me`. */
export type GetCurrentUserResult = components['schemas']['GetCurrentUserResult'];

/** The hydrated, authenticated user profile held in client state. */
export type UserProfile = GetCurrentUserResult;
