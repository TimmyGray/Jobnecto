# Epic 5: Cover Letter Application Management

Users can write job-specific cover letters tied to a vacancy, optionally seeded from a template, with one letter permitted per vacancy per user.

## Readiness Constraints From Epic 1 Retrospective

- The "one cover letter per vacancy per user" rule must be enforced by a database constraint on active records and mapped to `409 Conflict` through the shared exception pipeline.
- Concurrent create integration coverage is required for duplicate vacancy submissions because request-level checks alone do not close the race.
- Protected endpoints in this epic follow the shared JWT session policy from Epic 1 instead of assuming bearer-only transport.

### Story 5.1: Create Cover Letter

As a **job seeker**,
I want to write a cover letter for a specific vacancy,
So that I can submit a tailored application.

**Acceptance Criteria:**

**Given** a valid JWT token and `POST /api/v1/cover-letters` with `vacancyId` (existing), `content` (50–10000 chars)
**When** the request is processed
**Then** `201 Created` with the cover letter object; `Location` header set to `/api/v1/cover-letters/{id}`

**Given** optional `templateId` is provided and belongs to the current user
**When** the request is processed
**Then** `templateId` is persisted on the cover letter; content is whatever the user provided (not auto-filled)

**Given** the user already has a (non-deleted) cover letter for the same `vacancyId`
**When** `POST /api/v1/cover-letters` is called again with the same `vacancyId`
**Then** `409 Conflict` from database-backed per-user/per-vacancy uniqueness enforcement

**Given** `vacancyId` references a vacancy that does not exist
**When** the request is processed
**Then** `404 Not Found` with detail referencing the vacancy

**Given** `templateId` is provided but does not exist or belongs to another user
**When** the request is processed
**Then** `404 Not Found` with detail referencing the template

**Given** `content` is fewer than 50 or more than 10000 characters
**When** the request is processed
**Then** `400 Bad Request` with field-level error on `content`

---

### Story 5.2: List Cover Letters

As a **job seeker**,
I want to see all my cover letters in a paginated list,
So that I can track all my job applications.

**Acceptance Criteria:**

**Given** a valid JWT token
**When** `GET /api/v1/cover-letters` is called
**Then** `200 OK` with `{ total, page, pageSize, items }` — non-deleted cover letters owned by this user, ordered by `createdAt desc`
**And** each item includes: `id`, `vacancyId`, `vacancyTitle` (from linked vacancy), `createdAt`, `updatedAt`

**Given** `page` and `pageSize` are provided
**When** the request is processed
**Then** correct slice returned; `pageSize` capped at 100

**Given** the user has no cover letters
**When** `GET /api/v1/cover-letters` is called
**Then** `200 OK` with `{ total: 0, items: [] }`

---

### Story 5.3: Get Cover Letter Detail

As a **job seeker**,
I want to view a cover letter's full content and associated vacancy,
So that I can review or edit what I've written.

**Acceptance Criteria:**

**Given** a valid JWT token and a cover letter ID owned by the current user
**When** `GET /api/v1/cover-letters/{id}` is called
**Then** `200 OK` with all fields: `id`, `content`, `vacancyId`, `templateId` (nullable), `createdAt`, `updatedAt`, plus nested `vacancy` object with key fields

**Given** the cover letter does not exist, is soft-deleted, or belongs to another user
**Then** `404 Not Found`

---

### Story 5.4: Update Cover Letter Content

As a **job seeker**,
I want to edit the content of an existing cover letter,
So that I can refine my application before submitting.

**Acceptance Criteria:**

**Given** a valid JWT token and `PUT /api/v1/cover-letters/{id}` with new `content`
**When** the request is processed
**Then** `200 OK` with updated cover letter; `updatedAt` refreshed

**Given** `vacancyId` is included in the PUT body
**When** the request is processed
**Then** it is silently ignored — `vacancyId` is immutable after creation

**Given** updated `content` violates 50–10000 char bounds
**Then** `400 Bad Request` with field-level error on `content`

**Given** the cover letter belongs to another user
**Then** `403 Forbidden`

**Given** the cover letter does not exist or is soft-deleted
**Then** `404 Not Found`

---

### Story 5.5: Delete Cover Letter

As a **job seeker**,
I want to soft-delete a cover letter I no longer need,
So that my application history stays organized without permanent loss.

**Acceptance Criteria:**

**Given** a valid JWT token and a cover letter ID owned by the current user
**When** `DELETE /api/v1/cover-letters/{id}` is called
**Then** `204 No Content`; soft-delete applied

**Given** `GET /api/v1/cover-letters` or `GET /api/v1/cover-letters/{id}` is called after deletion
**Then** the cover letter is no longer visible

**Given** the vacancy this cover letter referenced is later deleted (if applicable)
**When** `GET /api/v1/cover-letters/{id}` is called for the cover letter before its own deletion
**Then** the cover letter is still returned with `vacancyId` preserved

**Given** the cover letter belongs to another user
**When** `DELETE /api/v1/cover-letters/{id}` is called
**Then** `403 Forbidden`
