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

/**
 * Hand-written pending Story 1.2's schema generation: `POST /api/v1/users/sessions`
 * isn't in `generated/schema.ts` yet (see Story 1.3 Trap 5). Names deliberately
 * match the backend types so the swap to `components['schemas'][...]` aliases is
 * mechanical once `npm run gen:api` is re-run against the shipped endpoint.
 */

/** Request body for `POST /api/v1/users/sessions` (sign-in). */
export interface SignInCommand {
  identifier: string;
  password: string;
}

/** `200 OK` body returned by `POST /api/v1/users/sessions`. */
export type SignInResult = CreateUserResult & { accessToken: string };
