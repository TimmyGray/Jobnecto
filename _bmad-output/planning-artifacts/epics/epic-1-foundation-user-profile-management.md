# Epic 1: Foundation & User Profile Management

Users can register an account and fully manage their professional profile. Establishes JWT authentication, ownership enforcement infrastructure, and global error handling used by all subsequent epics.

### Story 1.1: JWT Authentication & Global Exception Handling Infrastructure

As a **developer**,
I want JWT bearer token authentication middleware and a global exception handling middleware wired into the API,
So that all endpoints are secured by token, UserId is extracted from claims for every request, and all errors return consistent RFC 7808 Problem Details responses.

**Acceptance Criteria:**

**Given** the app starts
**When** `JwtBearerAuthentication` and `ExceptionHandlingMiddleware` are registered in `Program.cs`
**Then** all subsequent requests require a valid JWT bearer token to access protected endpoints

**Given** a valid JWT token containing a `sub` or `userId` claim
**When** any controller calls `GetCurrentUserId()`
**Then** the correct `Guid` is returned from the claim

**Given** a request arrives with no token or an invalid/expired token
**When** a protected endpoint is hit
**Then** `401 Unauthorized` is returned with a Problem Details body

**Given** a `ValidationException` is thrown anywhere in the pipeline
**When** it reaches the middleware
**Then** `400 Bad Request` is returned with `application/problem+json` and an `errors` map of field → [messages]

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
**Then** a new user is created, `201 Created` returned with user object (password excluded) and a signed JWT token
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
I want to retrieve my full profile including my resumes, educations, and recent cover letters,
So that I can see my complete professional record in one call.

**Acceptance Criteria:**

**Given** a valid JWT token
**When** `GET /api/v1/users/me` is called
**Then** `200 OK` with full user object: `id`, `loginName`, `email`, `phone`, `location`, `about`, `avatar`, `createdAt`, `updatedAt`
**And** `resumes` array with `id`, `title`, `updatedAt` for each non-deleted resume
**And** `educations` array with `id`, `title`, `specialization`, `degree` for each non-deleted education
**And** `coverLetters` object with `total` count and `recent` array (last 5, sorted by `createdAt desc`)
**And** password/hash fields are never present in the response

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
