# Epic 4: Vacancy Browsing & Filtering

Users can discover and filter job vacancies through a single filter endpoint, with full pagination support.

## Route Strategy

- Epic 4 uses one list route only: `POST /api/v1/vacancies/filter`.
- Browse mode is achieved by sending empty criteria (empty body `{}` or no filter fields).
- Optional sort mode is provided via `sortBy`: `createdAt` (default), `updatedAt`, or `relevance` (alias of `updatedAt`).

### Story 4.1: Browse Vacancies (Empty Filter Mode)

As a **job seeker**,
I want to browse all available vacancies using the filter endpoint with empty criteria,
So that I can scan what's available without defining filters.

**Acceptance Criteria:**

**Given** a valid JWT token
**When** `POST /api/v1/vacancies/filter` is called with empty body `{}`
**Then** `200 OK` with `{ totalCount, pageSize, hasNext, lastSeenId, lastSeenUpdatedAt, items }`, returning only vacancies owned by the authenticated user, ordered by `createdAt desc` by default, `pageSize` defaulting to 20
**And** each item includes: `id`, `title`, `company`, `workLocationType`, `location`, `salary`, `currency`, `createdAt`

**Given** `pageSize`, `lastSeenId`, `lastSeenUpdatedAt`, and optional `sortBy` are provided in the request body while filter criteria are empty
**When** the request is processed
**Then** correct cursor window is returned; `pageSize` capped at 100

**Given** `sortBy` is set to `updatedAt` or `relevance`
**When** `POST /api/v1/vacancies/filter` is called
**Then** results are ordered by `updatedAt desc`, with deterministic tie-break by `id desc`

**Given** no vacancies exist in the DB
**When** `POST /api/v1/vacancies/filter` is called with empty body `{}`
**Then** `200 OK` with `{ totalCount: 0, hasNext: false, items: [] }`

---

### Story 4.2: Filter Vacancies

As a **job seeker**,
I want to search vacancies by multiple criteria at once,
So that I can find the roles that best match my profile.

**Acceptance Criteria:**

**Given** a valid JWT token and `POST /api/v1/vacancies/filter` with body `{ skills: ["Go"], location: "Berlin", salaryMin: 80000, workLocationTypes: ["remote"] }`
**When** the request is processed
**Then** `200 OK` with vacancies matching ALL specified filters (AND logic between fields)
**And** within array fields (`skills`, `workLocationTypes`, `categories`), any match is sufficient (OR logic)

**Given** one or more filter fields are provided
**When** `POST /api/v1/vacancies/filter` is called
**Then** only the matching subset is returned while preserving the same pagination envelope and selected sort mode as Story 4.1

**Given** `salaryMin` > `salaryMax` is provided
**When** the request is processed
**Then** `400 Bad Request` with field-level error

**Given** `pageSize` exceeds 100
**When** the request is processed
**Then** it is capped at 100 (or `400 Bad Request` per implementation choice - must be consistent)

**Given** `excludeKeywords` contains terms
**When** the request is processed
**Then** vacancies whose `title` or `description` contains any of those terms are excluded

---

### Story 4.3: Get Vacancy Detail

As a **job seeker**,
I want to view all details of a specific vacancy,
So that I can decide whether to apply.

**Acceptance Criteria:**

**Given** a valid JWT token and a vacancy ID that exists
**When** `GET /api/v1/vacancies/{id}` is called
**Then** `200 OK` with all fields: `id`, `title`, `description`, `company`, `skills`, `workLocationType`, `location`, `salary`, `currency`, `matchScore`, `jobSource`, `categories`, `experienceLevel`, `createdAt`

**Given** the vacancy ID does not exist
**When** `GET /api/v1/vacancies/{id}` is called
**Then** `404 Not Found`

---
<!-- EOF -->
