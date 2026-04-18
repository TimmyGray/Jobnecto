# Epic 3: Cover Letter Template Library

Users can build a reusable library of cover letter templates (up to 10,000 characters) they can search, manage, and apply across job applications.

### Story 3.1: Create Cover Letter Template

As a **job seeker**,
I want to create a named cover letter template with reusable content,
So that I can quickly apply it to future job applications.

**Acceptance Criteria:**

**Given** a valid JWT token and `POST /api/v1/cover-letter-templates` with `name` and `content` (50–10000 chars)
**When** the request is processed
**Then** `201 Created` with the full template object; `Location` header set to `/api/v1/cover-letter-templates/{id}`

**Given** `content` is fewer than 50 characters
**When** the request is processed
**Then** `400 Bad Request` with field-level error on `content`

**Given** `content` is more than 10000 characters
**When** the request is processed
**Then** `400 Bad Request` with field-level error on `content`

**Given** `name` already exists for this user (across non-deleted templates)
**When** the request is processed
**Then** `409 Conflict`

**Given** the same `name` was used by another user
**When** the request is processed
**Then** `201 Created` — uniqueness is per-user, not global

---

### Story 3.2: List Cover Letter Templates

As a **job seeker**,
I want to browse and search my template library,
So that I can find the right template for a job application.

**Acceptance Criteria:**

**Given** a valid JWT token
**When** `GET /api/v1/cover-letter-templates` is called with no query params
**Then** `200 OK` with `{ total, page, pageSize, items }` — non-deleted templates owned by this user, ordered by `updatedAt desc`
**And** each item includes `id`, `name`, `createdAt`, `updatedAt`, and `contentPreview` (first 200 chars of `content`)

**Given** `search` query param is provided (e.g., `?search=senior`)
**When** the request is processed
**Then** only templates whose `name` contains the search term (case-insensitive) are returned

**Given** `pageSize` and `page` params are provided
**When** the request is processed
**Then** the correct slice is returned; `pageSize` capped at 100

---

### Story 3.3: Get Cover Letter Template Detail

As a **job seeker**,
I want to view the full content of a specific template,
So that I can read or copy it when composing a cover letter.

**Acceptance Criteria:**

**Given** a valid JWT token and a template ID owned by the current user
**When** `GET /api/v1/cover-letter-templates/{id}` is called
**Then** `200 OK` with all fields including full `content`

**Given** the template does not exist, is soft-deleted, or belongs to another user
**Then** `404 Not Found`

---

### Story 3.4: Update Cover Letter Template

As a **job seeker**,
I want to update a template's name or content,
So that I can refine my reusable material over time.

**Acceptance Criteria:**

**Given** a valid JWT token and `PUT /api/v1/cover-letter-templates/{id}` with new `name` and/or `content`
**When** the request is processed
**Then** `200 OK` with updated template; `updatedAt` refreshed

**Given** the new `name` is already taken by another non-deleted template of this user
**When** the request is processed
**Then** `409 Conflict`

**Given** the template belongs to another user
**Then** `403 Forbidden`

**Given** updated `content` violates 50–10000 char bounds
**Then** `400 Bad Request` with field-level error on `content`

---

### Story 3.5: Delete Cover Letter Template

As a **job seeker**,
I want to delete a template I no longer need,
So that my library stays tidy.

**Acceptance Criteria:**

**Given** a valid JWT token and a template ID owned by the current user
**When** `DELETE /api/v1/cover-letter-templates/{id}` is called
**Then** `204 No Content`; soft-delete applied

**Given** `GET /api/v1/cover-letter-templates` or `GET /api/v1/cover-letter-templates/{id}` is called after deletion
**Then** the template is no longer visible

**Given** a cover letter that referenced this template already exists
**When** the template is deleted
**Then** the cover letter is NOT deleted; `templateId` reference remains for historical context

---
