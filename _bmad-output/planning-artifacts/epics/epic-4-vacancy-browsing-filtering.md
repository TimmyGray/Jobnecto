# Epic 4: Vacancy Browsing & Filtering

Users can discover and filter job vacancies using keyword and multi-criteria search, with full pagination support.

### Story 4.1: Browse Vacancies (Paginated List)

As a **job seeker**,
I want to browse all available vacancies in a paginated list,
So that I can scan what's available without filtering.

**Acceptance Criteria:**

**Given** a valid JWT token
**When** `GET /api/v1/vacancies` is called with no params
**Then** `200 OK` with `{ total, page, pageSize, items }`, ordered by `createdAt desc`, `pageSize` defaulting to 20
**And** each item includes: `id`, `title`, `company`, `workLocationType`, `location`, `salary`, `currency`, `createdAt`

**Given** `page` and `pageSize` query params are provided
**When** the request is processed
**Then** correct slice is returned; `pageSize` capped at 100

**Given** no vacancies exist in the DB
**When** `GET /api/v1/vacancies` is called
**Then** `200 OK` with `{ total: 0, items: [] }`

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

**Given** an empty filter body `{}`
**When** `POST /api/v1/vacancies/filter` is called
**Then** same result as paginated list (all vacancies)

**Given** `salaryMin` > `salaryMax` is provided
**When** the request is processed
**Then** `400 Bad Request` with field-level error

**Given** `pageSize` exceeds 100
**When** the request is processed
**Then** it is capped at 100 (or `400 Bad Request` per implementation choice — must be consistent)

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
