# Epic 2: Resume & Education Management

Users can build and maintain their professional credentials - resumes with skills and work preferences, plus education records. All data is user-scoped and isolated.

## Readiness Dependencies From Epic 1

- Do not start Epic 2 delivery until Epic 1 password hashing and any required migration/backfill scope are planned; otherwise authenticated user data will be built on a security gap we already know about.
- Protected endpoints in this epic follow the shared JWT session policy from Epic 1 instead of assuming bearer-only transport.

### Story 2.1: Create Resume

As a **job seeker**,
I want to create a resume with my skills and work preferences,
So that I can describe what kind of role I'm looking for.

**Acceptance Criteria:**

**Given** a valid JWT token and `POST /api/v1/resumes` with `title`, at least one entry in `skills`, and `workLocationType` (remote/office/hybrid)
**When** the request is processed
**Then** `201 Created` with the full resume object; `Location` header set to `/api/v1/resumes/{id}`

**Given** `title` is missing or empty
**When** the request is processed
**Then** `400 Bad Request` with field-level error on `title`

**Given** `skills` array is empty or missing
**When** the request is processed
**Then** `400 Bad Request` with field-level error on `skills`

**Given** `workLocationType` is not one of `remote`, `office`, `hybrid`
**When** the request is processed
**Then** `400 Bad Request` with field-level error on `workLocationType`

**Given** optional fields (`salary`, `currency`, `experience`, `projects`, `certifications`, `languages`, `locations`, `excludedWords`) are provided
**When** the request is processed
**Then** they are persisted and returned in the response

---

### Story 2.2: List Resumes

As a **job seeker**,
I want to see all my resumes in a paginated list,
So that I can quickly navigate to the one I need.

**Acceptance Criteria:**

**Given** a valid JWT token
**When** `GET /api/v1/resumes` is called with no query params
**Then** `200 OK` with `{ totalCount, pageSize, hasNext, lastSeenId, lastSeenUpdatedAt, items }` - only this user's non-deleted resumes, ordered by `updatedAt desc`, `pageSize` defaulting to 20

**Given** `pageSize`, `lastSeenId`, and `lastSeenUpdatedAt` cursor params are provided
**When** the request is processed
**Then** the correct cursor window is returned; `pageSize` is capped at 100

**Given** the user has no resumes
**When** `GET /api/v1/resumes` is called
**Then** `200 OK` with `{ totalCount: 0, hasNext: false, items: [] }`

**Given** another user's resume exists in the DB
**When** this user calls `GET /api/v1/resumes`
**Then** the other user's resume is NOT returned

---

### Story 2.3: Get Resume Detail

As a **job seeker**,
I want to view the full detail of a specific resume,
So that I can review all its fields before applying.

**Acceptance Criteria:**

**Given** a valid JWT token and a resume ID that belongs to the current user
**When** `GET /api/v1/resumes/{id}` is called
**Then** `200 OK` with all resume fields

**Given** the resume ID does not exist or has been soft-deleted
**When** `GET /api/v1/resumes/{id}` is called
**Then** `404 Not Found`

**Given** the resume ID belongs to a different user
**When** `GET /api/v1/resumes/{id}` is called
**Then** `404 Not Found` (no information leak about existence)

---

### Story 2.4: Update Resume

As a **job seeker**,
I want to update any field of an existing resume,
So that I can keep my skills and preferences current.

**Acceptance Criteria:**

**Given** a valid JWT token and `PUT /api/v1/resumes/{id}` with one or more fields
**When** the request is processed
**Then** `200 OK` with fully updated resume; `updatedAt` refreshed

**Given** the resume does not exist or is soft-deleted
**When** `PUT /api/v1/resumes/{id}` is called
**Then** `404 Not Found`

**Given** the resume belongs to a different user
**When** `PUT /api/v1/resumes/{id}` is called
**Then** `403 Forbidden`

**Given** `skills` is provided but empty
**When** the request is processed
**Then** `400 Bad Request` with field-level error on `skills`

---

### Story 2.5: Delete Resume

As a **job seeker**,
I want to soft-delete a resume I no longer need,
So that it disappears from my list without permanent data loss.

**Acceptance Criteria:**

**Given** a valid JWT token and a resume ID that belongs to the current user
**When** `DELETE /api/v1/resumes/{id}` is called
**Then** `204 No Content`; `IsDeleted` set to `true`, `DeletedAt` set to now

**Given** `GET /api/v1/resumes` is called after soft-deletion
**Then** the deleted resume does NOT appear

**Given** `GET /api/v1/resumes/{id}` is called after soft-deletion
**Then** `404 Not Found`

**Given** the resume does not exist or belongs to a different user
**When** `DELETE /api/v1/resumes/{id}` is called
**Then** `404 Not Found` / `403 Forbidden` respectively

---

### Story 2.6: Create Education Record

As a **job seeker**,
I want to add an education record to my profile,
So that employers see my academic background.

**Acceptance Criteria:**

**Given** a valid JWT token and `POST /api/v1/educations` with `title`, `specialization`, and `degree` (bachelor/master/phd/certificate)
**When** the request is processed
**Then** `201 Created` with the full education object; `Location` header set to `/api/v1/educations/{id}`

**Given** `title` is missing or empty
**When** the request is processed
**Then** `400 Bad Request` with field-level error on `title`

**Given** `degree` is not one of `bachelor`, `master`, `phd`, `certificate`
**When** the request is processed
**Then** `400 Bad Request` with field-level error on `degree`

**Given** optional fields (`institution`, `graduationYear`, `gpa`) are provided
**When** the request is processed
**Then** they are persisted and returned in the response

---

### Story 2.7: List Education Records

As a **job seeker**,
I want to see all my education records in order,
So that I have a complete academic timeline.

**Acceptance Criteria:**

**Given** a valid JWT token
**When** `GET /api/v1/educations` is called
**Then** `200 OK` with array of non-deleted education records owned by this user, ordered by `graduationYear desc` (nulls last)

**Given** the user has no education records
**When** `GET /api/v1/educations` is called
**Then** `200 OK` with `[]`

**Given** another user's education records exist
**When** this user calls `GET /api/v1/educations`
**Then** the other user's records are NOT returned

---

### Story 2.8: Get, Update & Delete Education Records

As a **job seeker**,
I want to view, update, and remove individual education records,
So that I can keep my academic history accurate.

**Acceptance Criteria:**

**GET /api/v1/educations/{id}**

**Given** a record ID owned by the current user
**When** `GET /api/v1/educations/{id}` is called
**Then** `200 OK` with all education fields

**Given** the record does not exist, is soft-deleted, or belongs to another user
**Then** `404 Not Found`

---

**PUT /api/v1/educations/{id}**

**Given** a valid JWT and one or more fields to update
**When** `PUT /api/v1/educations/{id}` is called
**Then** `200 OK` with updated record

**Given** the record belongs to another user
**Then** `403 Forbidden`

---

**DELETE /api/v1/educations/{id}**

**Given** a valid JWT and a record owned by the current user
**When** `DELETE /api/v1/educations/{id}` is called
**Then** `204 No Content`; soft-delete applied; record no longer visible in list

---
