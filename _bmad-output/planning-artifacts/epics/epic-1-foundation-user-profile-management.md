# Epic 1: Foundation & User Profile Management

Users can register an account and fully manage their professional profile. Establishes JWT authentication, ownership enforcement infrastructure, and global error handling used by all subsequent epics.

## Readiness Updates After Stories 1.1-1.2

- Password storage is not production-ready until registration and login flows persist one-way hashes and a migration/backfill plan exists for any plaintext legacy records.
- Shared auth policy: browser registration/login flows issue JWTs via HTTP-only cookies; any non-browser client support in later epics must explicitly document `Authorization: Bearer` transport and token renewal behavior.
- `loginName` and `email` uniqueness must be enforced at the database layer and mapped to `409 Conflict` through global exception handling, with concurrent create/update integration tests covering race conditions.

### Story 1.1: JWT Authentication & Global Exception Handling Infrastructure

As a **developer**,
I want JWT bearer token authentication middleware and a global exception handling middleware wired into the API,
So that all endpoints are secured by token, UserId is extracted from claims for every request, and all errors return consistent RFC 7808 Problem Details responses.

**Acceptance Criteria:**

**Given** the app starts
**When** JWT authentication and `ExceptionHandlingMiddleware` are registered in `Program.cs`
**Then** protected endpoints require a valid authenticated JWT session using the standardized transport for the client type

**Given** a valid JWT containing a `sub` or `userId` claim
**When** any controller calls `GetCurrentUserId()`
**Then** the correct `Guid` is returned from the claim

**Given** a request arrives with no valid authenticated JWT session or an invalid/expired token
**When** a protected endpoint is hit
**Then** `401 Unauthorized` is returned with a Problem Details body

**Given** a `ValidationException` is thrown anywhere in the pipeline
**When** it reaches the middleware
**Then** `400 Bad Request` is returned with `application/problem+json` and an `errors` map of field -> [messages]

**Given** a `NotFoundException` is thrown
**When** it reaches the middleware
**Then** `404 Not Found` with descriptive `detail`

**Given** a `ConflictException` is thrown (duplicate email/loginName)
**When** it reaches the middleware
**Then** `409 Conflict` with descriptive `detail`

**Given** an unhandled exception is thrown
**When** it reaches the middleware
**Then** `500 Internal Server Error` is returned; no stack trace in response body; error is logged

---

### Story 1.2: Create User Account

As a **job seeker**,
I want to register a new account with my login name, email, and password,
So that I can receive a JWT token and start managing my profile.

**Acceptance Criteria:**

**Given** a `POST /api/v1/users` request with valid `loginName`, `email`, `password`
**When** the request is processed
**Then** a new user is created, `201 Created` returned with user object (password excluded)
**And** a signed JWT token is set in an HTTP-Only secure cookie (SameSite=Strict, Secure flag set in Production)
**And** `Location` header set to `/api/v1/users/me`

**Given** `loginName` is shorter than 3 or longer than 20 characters, or contains chars other than alphanumeric/underscore
**When** the request is processed
**Then** `400 Bad Request` with field-level error on `loginName`

**Given** `email` is not a valid email format
**When** the request is processed
**Then** `400 Bad Request` with field-level error on `email`

**Given** `password` is fewer than 8 characters
**When** the request is processed
**Then** `400 Bad Request` with field-level error on `password`

**Given** the `email` or `loginName` already exists in the system
**When** the request is processed
**Then** `409 Conflict` with descriptive message indicating which field is taken

**Given** optional `phone` is provided but not in E.164 format
**When** the request is processed
**Then** `400 Bad Request` with field-level error on `phone`

---

### Story 1.3: Retrieve Current User Profile

As a **job seeker**,
I want to retrieve my current core profile data,
So that identity/profile information stays simple while resumes, educations, and cover letters are managed through their own user-scoped endpoints.

**Acceptance Criteria:**

**Given** a valid JWT token
**When** `GET /api/v1/users/me` is called
**Then** `200 OK` with full user object: `id`, `loginName`, `email`, `phone`, `location`, `about`, `avatar`, `createdAt`, `updatedAt`
**And** password/hash fields are never present in the response
**And** related resources are fetched via dedicated user-scoped routes (`GET /api/v1/resumes`, `GET /api/v1/educations`, `GET /api/v1/cover-letters`)

**Given** the JWT token references a user ID that no longer exists
**When** `GET /api/v1/users/me` is called
**Then** `404 Not Found`

**Given** no JWT token is provided
**When** `GET /api/v1/users/me` is called
**Then** `401 Unauthorized`

---

### Story 1.4: Update User Profile

As a **job seeker**,
I want to update my profile fields including my login name,
So that I can keep my professional identity and contact info current.

**Acceptance Criteria:**

**Given** a valid JWT token and a `PUT /api/v1/users/me` request with one or more of: `loginName`, `email`, `phone`, `location`, `about`, `avatar`
**When** the request is processed
**Then** `200 OK` with updated user object; `updatedAt` timestamp refreshed

**Given** `loginName` is being changed to a value already taken by another user
**When** the request is processed
**Then** `409 Conflict`

**Given** `email` is being changed to a value already in use
**When** the request is processed
**Then** `409 Conflict`

**Given** `phone` is provided in a non-E.164 format
**When** the request is processed
**Then** `400 Bad Request` with field-level error on `phone`

**Given** only a subset of fields is provided
**When** the request is processed
**Then** only those fields are updated; unmentioned fields remain unchanged

**Given** `id` or `createdAt` is included in the request body
**When** the request is processed
**Then** they are silently ignored

---

### Story 1.5: Password Hashing & Token Policy Hardening

As a **platform owner**,
I want password persistence and token transport behavior standardized,
So that all authenticated features in later epics build on a secure and explicit auth foundation.

**Acceptance Criteria:**

**Given** a user registers or updates credentials through the supported auth flow
**When** user credentials are persisted
**Then** passwords are stored only as one-way salted hashes and are never stored or returned in plaintext

**Given** legacy user rows may contain non-hashed password values from early implementation
**When** the password-hardening migration plan is executed
**Then** legacy values are remediated through an approved migration/backfill path before Epic 2 delivery begins

**Given** browser-based clients authenticate in Phase B
**When** registration/login succeeds
**Then** JWT session tokens are delivered via HTTP-only secure cookies with explicit SameSite and Secure behavior per environment

**Given** non-browser clients authenticate in Phase B
**When** they call protected endpoints
**Then** `Authorization: Bearer` token transport and renewal behavior are explicit: active sessions renew via `POST /api/v1/users/token/refresh`, and expired/invalid sessions re-authenticate

**Given** a race condition causes a DB-level uniqueness violation during auth-related create/update operations
**When** the exception reaches global handling
**Then** the API returns `409 Conflict` with a stable problem-details contract and integration coverage proves the concurrent path

---
