---
stepsCompleted:
  - step-01-init
  - step-02-discovery
  - step-02c-executive-summary
  - step-03-success
  - step-04-user-journeys
  - step-05-feature-requirements
  - step-06-technical-details
inputDocuments:
  - e:\apps\Jobnecto\docs\JOBNECTO_BACKEND_ROADMAP.md
  - e:\apps\Jobnecto\_bmad-output\project-context.md
classification:
  projectType: api_backend
  domain: HR Tech / Recruitment
  complexity: Medium
  projectContext: brownfield
workflowType: 'prd'
---

# Product Requirements Document - Jobnecto

**Author:** Timmy
**Date:** 2026-04-16

## Executive Summary

Jobnecto is a unified job aggregation platform that solves the fragmentation problem of modern job seeking. Today's job seekers waste hours switching between multiple job boards, manually updating resumes, writing repetitive cover letters, and managing applications across platforms. This fractures focus and wastes the critical time that should go toward finding the right role.

Jobnecto consolidates this chaos. Users connect their accounts from multiple job sources (HeadHunter, LinkedIn, Indeed, etc.) to a single unified dashboard. They view all available vacancies in one place with intelligent filtering that outperforms what individual platforms offer. Using GPT-level LLMs now mature and accessible in 2026, Jobnecto generates tailored cover letters, scores job matches for fit, and manages responses across all platforms from one interface. Job seekers reclaim hours per week and get a significantly better user experience.

**Target Users:** Job seekers (professionals, career-changers, recent graduates) who actively search for roles and want efficiency, better filtering, and AI-powered application support.

**Phase B Scope:** The HTTP core and API endpoints that power the job seeker's job hunting workflow. Users manage their profiles (education, resumes, cover letters, templates), browse and filter unified vacancies, and send applications to multiple platforms.

-## Post-Merge Implementation Status (2026-04-25)

- Completed stories now merged on `master`: `1-1-global-exception-handling`, `1-2-create-user-account`, `1-4-update-user-profile`, `1-5-password-hashing-token-policy-hardening`.
- Implemented API surface includes `POST /api/v1/users` (registration), `POST /api/v1/users/token/refresh` (authenticated token renewal), and the new profile mutation and avatar management endpoints (`PATCH /api/v1/users/me`, `POST/PUT/DELETE /api/v1/users/me/avatar`).
- Password persistence uses PBKDF2 (`pbkdf2-sha256`) through `IPasswordHasher` + `Pbkdf2PasswordHasher`; tokens are renewed through HTTP-only cookie transport and bearer transport support.
- Scope adjustment: a subset of Phase C security baseline (password hashing + JWT protected refresh route) is already delivered while broader profile/resource endpoints remain in Phase B backlog.

### What Makes This Special

Three differentiators converge:

1. **Unified Aggregation** — No more context-switching. All vacancies from all sources visible in one place with a unified search and filter layer.
2. **LLM-Powered Intelligence** — Intelligent cover letter generation eliminates writer's block; AI job matching surfaced based on resume fit, not just keyword matching.
3. **Time Recovery** — Users measurably save hours per application cycle by eliminating manual resume updates, redundant typing, and platform switching.

The **enabling insight** is that LLMs in 2026 are now smart, reliable, and accessible enough to power real job matching and content generation. This wasn't viable five years ago; it is now.

### Project Classification

| Attribute | Value |
|-----------|-------|
| **Project Type** | API Backend (REST). |
| **Domain** | HR Tech / Recruitment (job aggregation and matching). |
| **Complexity** | Medium (multi-domain entities, third-party integrations, intelligent filtering). |
| **Project Context** | Brownfield; extends existing .NET 10 domain model and infrastructure. |

## Success Criteria

### User Success

A job seeker can:
- Create a profile with education, resumes, and cover letter templates
- Manage (create, read, update, soft delete) their profile data, resumes, educations, and cover letter templates
- View a paginated, filterable list of vacancies (read-only until Phase C auth)
- Browse individual vacancy details
- Store reusable cover letter templates to accelerate application workflows

**Indicator:** A job seeker performs full CRUD operations on their own resources without errors; the API responds with proper validation feedback on bad input. Cover letter templates can be created, retrieved (with pagination and filtration), updated, and soft-deleted.

### Business Success

- Phase B proves the architecture works end-to-end: MediatR + FluentValidation + Clean Architecture integration complete
- All planned endpoints are functional and documented in OpenAPI spec
- The team can confidently add Phase C (JWT auth) on top without refactoring core patterns
- Ready for limited beta testing (backend-only)

**Indicator:** Phase C can be started without reworking Phase B code; no architectural blockers discovered.

### Technical Success

- **All endpoints** follow MediatR command/query pattern consistently
- **All inputs** validated via FluentValidation before reaching handlers
- **All handlers** have corresponding unit/integration tests proving business logic
- **Clean Architecture boundaries** maintained: API maps HTTP → Application entry points; Application is persistence-agnostic
- **Database** changes committed as migrations; `AddInfrastructure()` called in `Program.cs`
- **OpenAPI spec** auto-generated and complete for all Phase B endpoints
- **Build passes** `dotnet build backend/JobNecto.slnx --configuration Release --warnaserror`
- **Tests pass** `dotnet test backend/JobNecto.slnx --configuration Release`
- **No nullable warnings** suppressed without documentation

### Measurable Outcomes

1. All Phase B endpoints implemented:
  - Users: GET/PUT (current user core profile only; related resources use dedicated endpoints)
   - Resumes: GET (list), POST (create), GET (single), PUT (update), DELETE (soft)
   - Educations: GET (list), POST (create), GET (single), PUT (update), DELETE (soft)
   - Cover Letters: GET (list), POST (create), GET (single), PUT (update), DELETE (soft)
   - Cover Letter Templates: GET (list), POST (create), GET (single), PUT (update), DELETE (soft)
   - Vacancies: GET (list), GET (single)
   - Vacancy Filtering: POST `/api/v1/vacancies/filter` (complex multi-criteria filtering via request body)
   
2. ≥ 85% code coverage on Application handlers and validators
3. Zero breaking changes to Domain model entities post-Phase B
4. OpenAPI spec includes all endpoints, request/response schemas, status codes (200, 400, 404, 500)
5. Database supports test-driven setup: migrations allow fresh DB creation and teardown per test run
6. Documentation: API versioning strategy documented in README or API docs

## Product Scope

### MVP - Minimum Viable Product (Phase B)

**Core Resources:**
- **User profiles** — Create profile, retrieve/update current user core fields only (GET `/api/v1/users/me`, PATCH `/api/v1/users/me`)
- **Resumes** — CRUD operations (GET list, POST create, GET detail, PUT update, DELETE soft)
- **Educations** — CRUD operations linked to user (GET list, POST create, GET detail, PUT update, DELETE soft)
- **Cover Letter Templates** — Reusable templates for applications (GET list, POST create, GET detail, PUT update, DELETE soft with pagination/filtration)
- **Cover Letters** — Job-specific application letters (GET list, POST create, GET detail, PUT update, DELETE soft); linked to Vacancy and Template
- **Vacancies** — Browse and filter all available job openings (GET list, GET single)
  - **Vacancy Filtering** — Complex multi-criteria filtering via POST `/api/v1/vacancies/filter` with request body (skills, location, salary range, work location type, etc.)

**Validation Requirements:**
- Email validation (valid format)
- Phone validation (E.164 format per domain model)
- Age validation per EF rules
- Proper HTTP status codes: 200 (success), 400 (validation error), 404 (not found), 500 (server error)

**Database & Architecture:**
- Migrations for all new entities and relationships
- `AddInfrastructure()` wired in `Program.cs`
- UnitOfWork pattern supporting transactional integrity

### Growth Features (Post-MVP)

- Third-party job source OAuth integration (HeadHunter, LinkedIn, Indeed)
- Vacancy synchronization from external job boards
- Job matching/scoring endpoint
- Cover letter generation endpoint
- Application response tracking

### Vision (Future - Phase C, D, E)

- JWT authentication and role-based authorization
- Rate limiting and API security hardening
- Redis caching for frequently accessed vacancies
- Quartz scheduled jobs for periodic syncing
- Full LLM integration for analysis and generation

## User Journeys

### **Journey 1: Setup Professional Profile**

**Actor:** New job seeker joining Jobnecto
**Goal:** Complete their profile so they can start job searching

1. **Create User Profile** → POST `/api/v1/users` (account creation)
2. **Retrieve Profile** → GET `/api/v1/users/me` (view current core profile fields)
3. **Add Education** → POST `/api/v1/educations` (add degree, specialization)
4. **Create Resume Template** → POST `/api/v1/resumes` (add resume with skills, salary expectations, work preferences)
5. **Update Profile** → PATCH `/api/v1/users/me` (edit personal info: location, phone, about)

**Success:** Job seeker has a complete profile and one resume template ready for matching.

---

### **Journey 2: Build Reusable Cover Letter Template Library**

**Actor:** Job seeker streamlining their application process
**Goal:** Build a library of generic cover letter templates to be customized for each application

1. **List Existing Templates** → GET `/api/v1/cover-letter-templates?page=1&pageSize=10` (see all saved templates)
2. **Create New Template** → POST `/api/v1/cover-letter-templates` (write reusable generic template)
3. **Retrieve Template Details** → GET `/api/v1/cover-letter-templates/{id}` (view specific template before using)
4. **Update Template** → PUT `/api/v1/cover-letter-templates/{id}` (refine template based on feedback or new learned approach)
5. **Delete Outdated Template** → DELETE `/api/v1/cover-letter-templates/{id}` (soft delete old/unused versions)

**Success:** Job seeker has 2-3 high-quality, reusable cover letter templates ready to be adapted per application.

---

### **Journey 3: Discover & Browse Vacancies with Intelligent Filtering**

**Actor:** Job seeker actively searching for roles
**Goal:** Find relevant job vacancies quickly using advanced filtering (better than individual job boards)

1. **Browse All Recent Vacancies** → GET `/api/v1/vacancies?page=1&pageSize=20` (see recent postings)
2. **Apply Complex Filters** → POST `/api/v1/vacancies/filter` with request body:
   ```json
   {
     "skills": ["C#", "Azure"],
     "location": "Remote",
     "salaryMin": 80000,
     "workLocationType": "remote",
     "page": 1,
     "pageSize": 20
   }
   ```
   (apply multi-criteria filtering without URL query string pollution)
3. **Paginate Results** → POST `/api/v1/vacancies/filter` with `page: 2` (load more results)
4. **View Vacancy Details** → GET `/api/v1/vacancies/{id}` (read full job description, requirements, match score)

**Success:** Job seeker finds relevant vacancies fast using intelligent filtering with a clean, semantically correct API design.

---

### **Journey 4: Create & Manage Application Cover Letters**

**Actor:** Job seeker preparing applications
**Goal:** Create job-specific cover letters based on templates and vacancy details

1. **Review Vacancy** → GET `/api/v1/vacancies/{id}` (understand job requirements)
2. **Retrieve Relevant Template** → GET `/api/v1/cover-letter-templates/{templateId}` (get starting point)
3. **Create Job-Specific Cover Letter** → POST `/api/v1/cover-letters` with body:
   ```json
   {
     "vacancyId": "vacancy-123",
     "content": "Customized content based on template..."
   }
   ```
   (create instance for this specific vacancy)
4. **Retrieve Cover Letter** → GET `/api/v1/cover-letters/{letterId}` (review before sending)
5. **Update Cover Letter** → PUT `/api/v1/cover-letters/{letterId}` (refine based on feedback)
6. **List All Cover Letters** → GET `/api/v1/cover-letters?page=1&pageSize=20` (see all applications created)
7. **Delete Cover Letter** → DELETE `/api/v1/cover-letters/{letterId}` (soft delete if decided not to apply)

**Success:** Job seeker has created multiple job-specific cover letters (Phase D will add LLM auto-generation from templates).

---

### **Journey 5: Manage Profile, Resumes, & Educations Over Time**

**Actor:** Active job seeker maintaining current profile
**Goal:** Keep resume and education records up-to-date as credentials and goals evolve

1. **List My Resumes** → GET `/api/v1/resumes?page=1` (see my resume versions)
2. **Update Resume** → PUT `/api/v1/resumes/{id}` (add new skill, update salary expectation, change work preferences)
3. **List My Educations** → GET `/api/v1/educations` (view my education history)
4. **Update Education** → PUT `/api/v1/educations/{id}` (add new degree, update specialization)
5. **Delete Education** → DELETE `/api/v1/educations/{id}` (soft delete incomplete or irrelevant education)
6. **Review & Refresh Templates** → GET, PUT `/api/v1/cover-letter-templates/{id}` (keep reusable templates fresh and relevant)

**Success:** Job seeker's profile, resumes, educations, and templates stay current and reflect their latest credentials and career goals.

---

## Feature Requirements & Acceptance Criteria

### **1. User Profile Resource**

**Purpose:** Manage job seeker identity, contact information, and professional summary.

**Endpoints:**
- `GET /api/v1/users/me` — Retrieve current user core profile fields only
- `PATCH /api/v1/users/me` — Update current user profile
- `POST /api/v1/users` — Create new user account

**Feature: Create User Profile (POST /api/v1/users)**

**Acceptance Criteria:**
- User can register with `loginName`, `email`, `password`, and optional profile fields (`phone`, `location`, `about`, `avatar`)
- Email validation: must be valid email format; must be unique across the system
- Phone validation (if provided): must be valid E.164 format (e.g., `+1234567890`)
- Password: minimum 8 characters; persisted values must be one-way salted hashes (never plaintext)
- `loginName`: must be unique, alphanumeric + underscore, 3-20 characters
- On success: return `201 Created` with user object (exclude password); set JWT token as HTTP-Only secure cookie (SameSite=Strict)
- Non-browser clients use `Authorization: Bearer` for protected APIs and renew active sessions through `POST /api/v1/users/token/refresh`; expired/invalid sessions must re-authenticate.
- On validation error: return `400 Bad Request` with field-level error messages
- On duplicate email/loginName: return `409 Conflict` with message

**Validation Rules:**
- `email`: Required, valid format, unique
- `loginName`: Required, 3-20 chars, alphanumeric + underscore, unique
- `password`: Required, minimum 8 characters; persisted values must be one-way salted hashes
- `phone`: Optional, E.164 format if provided
- `avatar`: Optional, URL or base64 data

**Edge Cases:**
- Subdomain-style emails (e.g., `user+label@example.com`) are valid
- Phone with country code required (e.g., `+1` for US)
- Duplicate email attempt after another user created account moments before

---

**Feature: Retrieve Current User (GET /api/v1/users/me)**

**Acceptance Criteria:**
- Return current user core profile fields only: `id`, `loginName`, `email`, `phone`, `location`, `about`, `avatar`, `createdAt`, `updatedAt`
- Exclude sensitive fields (password hash, tokens)
- Related resources are retrieved via dedicated user-scoped routes (`/api/v1/resumes`, `/api/v1/educations`, `/api/v1/cover-letters`)
- On success: return `200 OK` with core profile data
- If JWT references non-existing user: return `404 Not Found`
- On unauthorized (Phase C): return `401 Unauthorized`

**Response Shape (Phase B, no auth yet):**
```json
{
  "id": "uuid",
  "loginName": "john_doe",
  "email": "john@example.com",
  "phone": "+12125551234",
  "location": "New York, NY",
  "about": "Full-stack developer...",
  "avatar": "https://...",
  "createdAt": "2026-04-01T10:00:00Z",
  "updatedAt": "2026-04-16T14:30:00Z"
}
```

---

**Feature: Update User Profile (PATCH /api/v1/users/me)**

**Acceptance Criteria:**
- Update any profile field: `email`, `phone`, `location`, `about`, `avatar`
- Can change `loginName`; must remain unique system-wide; return `409 Conflict` if new value already taken
- Cannot change `id`, `createdAt` (immutable system fields)
- Email uniqueness validation if email is being changed
- Phone E.164 validation if phone is being changed
- Partial updates allowed (only send fields to change)
- Return `200 OK` with updated user object
- On validation error: return `400 Bad Request`
- On duplicate email: return `409 Conflict`
- Audit: update `updatedAt` timestamp

---

### **2. Resume Resource**

**Purpose:** Store resume templates with skills, experience expectations, and preferred work conditions.

**Endpoints:**
- `GET /api/v1/resumes` — List all resumes for current user (paginated)
- `POST /api/v1/resumes` — Create new resume
- `GET /api/v1/resumes/{id}` — Retrieve single resume detail
- `PUT /api/v1/resumes/{id}` — Update resume
- `DELETE /api/v1/resumes/{id}` — Soft delete resume

**Feature: Create Resume (POST /api/v1/resumes)**

**Acceptance Criteria:**
- Create resume with: `title`, `skills` (array), `salary` (optional), `currency` (optional), `workLocationType` (remote/office/hybrid), `experience` (text), `projects` (array), `certifications` (array), `languages` (array), `locations` (array of preferred work locations), `excludedWords` (array of keywords to avoid in LLM generation)
- All required fields must be present
- Skills: array of non-empty strings; minimum 1 skill required
- Salary: if provided, must be positive number; currency required if salary present
- WorkLocationType: must be one of: `remote`, `office`, `hybrid`
- Languages: array of language names (e.g., `["English", "Spanish"]`)
- Return `201 Created` with resume object including `id`, `createdAt`, `updatedAt`
- Record association to current user automatically

**Validation Rules:**
- `title`: Required, 1-100 characters
- `skills`: Required array, minimum 1 skill, each skill 1-50 characters
- `salary`: Optional, if present must be > 0
- `currency`: Optional, if salary present then required (ISO 4217: USD, EUR, etc.)
- `workLocationType`: Required, enum: remote | office | hybrid
- `experience`: Optional, text description
- `projects`: Optional array
- `certificates`: Optional array
- `languages`: Optional array
- `locations`: Optional array (e.g., ["New York", "San Francisco"])
- `excludedWords`: Optional array for LLM context

**Edge Cases:**
- User creates multiple resumes (different roles: "Senior Dev", "Tech Lead", "Consultant")
- Resume with no salary specified (flexible candidate)
- All locations excluded (truly remote-only candidate)

---

**Feature: List Resumes (GET /api/v1/resumes)**

**Acceptance Criteria:**
- Return paginated list of user's resumes
- Query params: `page` (1-indexed), `pageSize` (default 20, max 100)
- Return array with: `id`, `title`, `skills` (array), `salary`, `currency`, `workLocationType`, `updatedAt`
- On success: return `200 OK` with `{ total, page, pageSize, items }`
- Order by `updatedAt desc` (newest first)

---

**Feature: Get Resume Detail (GET /api/v1/resumes/{id})**

**Acceptance Criteria:**
- Return full resume with all fields
- If resume not found: return `404 Not Found`
- If resume belongs to different user: return `403 Forbidden` (Phase C with auth)
- For now (Phase B no auth): return resume if it exists

---

**Feature: Update Resume (PUT /api/v1/resumes/{id})**

**Acceptance Criteria:**
- Update any resume field
- Partial updates allowed
- Re-validate all fields same as POST
- Return `200 OK` with updated object
- Update `updatedAt` timestamp
- If `id` not found: return `404 Not Found`

---

**Feature: Delete Resume (DELETE /api/v1/resumes/{id})**

**Acceptance Criteria:**
- Soft delete: set `IsDeleted = true` or similar flag (don't physically remove from DB)
- Return `204 No Content` on success
- Resume no longer appears in list endpoints after soft delete
- If `id` not found: return `404 Not Found`

---

### **3. Education Resource**

**Purpose:** Store user's educational background (degrees, specializations, institutions).

**Endpoints:**
- `GET /api/v1/educations` — List all educations for current user
- `POST /api/v1/educations` — Create new education record
- `GET /api/v1/educations/{id}` — Retrieve single education
- `PUT /api/v1/educations/{id}` — Update education
- `DELETE /api/v1/educations/{id}` — Soft delete education

**Feature: Create Education (POST /api/v1/educations)**

**Acceptance Criteria:**
- Create with: `title` (degree name), `specialization` (field of study), `degree` (enum: bachelor, master, phd, certificate), `institution` (optional), `graduationYear` (optional), `gpa` (optional)
- Title: required, 1-100 characters
- Specialization: required, 1-100 characters
- Degree: required, must be one of: `bachelor`, `master`, `phd`, `certificate`
- Institution: optional, 1-100 characters
- GraduationYear: optional, must be valid 4-digit year (1950-2050)
- Return `201 Created` with education object including `id`, `userId`, `createdAt`, `updatedAt`
- Can be linked to resumes via `ResumeEducations` join table (Phase B: link relationship exists in domain)

---

**Feature: List Educations (GET /api/v1/educations)**

**Acceptance Criteria:**
- Return all educations for current user (no pagination needed, typically < 10 records)
- Return array with: `id`, `title`, `specialization`, `degree`, `institution`, `graduationYear`
- Order by `graduationYear desc` (most recent first)
- On success: return `200 OK`

---

**Feature: Get Education Detail, Update, Delete**

**Acceptance Criteria:** (Similar pattern to Resume)
- GET `{id}`: return full education record with all fields
- PUT `{id}`: update any field, re-validate
- DELETE `{id}`: soft delete
- 404 if not found

---

### **4. Cover Letter Template Resource**

**Purpose:** Store reusable cover letter templates that job seekers craft once and customize for each application.

**Endpoints:**
- `GET /api/v1/cover-letter-templates` — List all templates for current user (paginated, filterable)
- `POST /api/v1/cover-letter-templates` — Create new template
- `GET /api/v1/cover-letter-templates/{id}` — Retrieve single template
- `PUT /api/v1/cover-letter-templates/{id}` — Update template
- `DELETE /api/v1/cover-letter-templates/{id}` — Soft delete template

**Feature: Create Cover Letter Template (POST /api/v1/cover-letter-templates)**

**Acceptance Criteria:**
- Create with: `name` (template name), `content` (template text, may contain placeholders like {{companyName}}, {{role}})
- Name: required, 1-100 characters, unique per user (two templates can't have same name for same user)
- Content: required, minimum 50 characters, maximum 10000 characters
- Return `201 Created` with template object including `id`, `name`, `content`, `createdAt`, `updatedAt`

---

**Feature: List Cover Letter Templates (GET /api/v1/cover-letter-templates)**

**Acceptance Criteria:**
- Return paginated list of user's templates
- Query params: `page` (1-indexed), `pageSize` (default 20)
- Return array with: `id`, `name`, `contentPreview` (first 200 chars), `createdAt`, `updatedAt`
- Optional filter: `search` query param to search by name
- Order by `updatedAt desc` (newest first)
- On success: return `200 OK` with `{ total, page, pageSize, items }`

---

**Feature: Get Template Detail (GET /api/v1/cover-letter-templates/{id})**

**Acceptance Criteria:**
- Return full template with complete `content`
- If not found: return `404 Not Found`

---

**Feature: Update Template (PUT /api/v1/cover-letter-templates/{id})**

**Acceptance Criteria:**
- Update `name` or `content` or both
- Validate name uniqueness per user if name is being changed
- Return `200 OK` with updated template
- Update `updatedAt` timestamp

---

**Feature: Delete Template (DELETE /api/v1/cover-letter-templates/{id})**

**Acceptance Criteria:**
- Soft delete
- Return `204 No Content` on success
- If not found: return `404 Not Found`

---

### **5. Cover Letter Resource**

**Purpose:** Store job-specific application cover letters, typically created from a template and customized for a particular vacancy.

**Endpoints:**
- `GET /api/v1/cover-letters` — List all cover letters for current user (paginated)
- `POST /api/v1/cover-letters` — Create new cover letter for a vacancy
- `GET /api/v1/cover-letters/{id}` — Retrieve single cover letter
- `PUT /api/v1/cover-letters/{id}` — Update cover letter
- `DELETE /api/v1/cover-letters/{id}` — Soft delete cover letter

**Feature: Create Cover Letter (POST /api/v1/cover-letters)**

**Acceptance Criteria:**
- Create with: `vacancyId` (required), `content` (required), `templateId` (optional, for tracking which template was the source)
- VacancyId: must reference valid vacancy
- Content: required, minimum 50 characters, maximum 10000 characters
- TemplateId: optional, if provided must be valid template belonging to user
- One cover letter per vacancy per user (cannot create duplicate for same vacancy)
- Return `201 Created` with cover letter object including `id`, `vacancyId`, `content`, `templateId`, `createdAt`, `updatedAt`

---

**Feature: List Cover Letters (GET /api/v1/cover-letters)**

**Acceptance Criteria:**
- Return paginated list of user's cover letters
- Query params: `page` (1-indexed), `pageSize` (default 20)
- Return array with: `id`, `vacancyId`, `vacancyTitle` (from linked Vacancy), `contentPreview` (first 200 chars), `createdAt`
- Order by `createdAt desc` (newest first)
- On success: return `200 OK` with `{ total, page, pageSize, items }`

---

**Feature: Get Cover Letter Detail (GET /api/v1/cover-letters/{id})**

**Acceptance Criteria:**
- Return full cover letter with complete `content`, `vacancyId`, `templateId`
- Include vacancy details: title, company, description
- If not found: return `404 Not Found`

---

**Feature: Update Cover Letter (PUT /api/v1/cover-letters/{id})**

**Acceptance Criteria:**
- Update `content` (cannot change `vacancyId` after creation)
- Return `200 OK` with updated cover letter
- Update `updatedAt` timestamp

---

**Feature: Delete Cover Letter (DELETE /api/v1/cover-letters/{id})**

**Acceptance Criteria:**
- Soft delete
- Return `204 No Content` on success

---

### **6. Vacancy Resource**

**Purpose:** Store job vacancy listings aggregated from external sources. Phase B: read-only with filtering. Phase D: syncing from job sources.

**Endpoints:**
- `GET /api/v1/vacancies` — List recent vacancies (basic pagination)
- `POST /api/v1/vacancies/filter` — Advanced filtering with complex criteria
- `GET /api/v1/vacancies/{id}` — Retrieve single vacancy detail

**Feature: List Vacancies (GET /api/v1/vacancies)**

**Acceptance Criteria:**
- Return paginated list of all vacancies (recent first)
- Query params: `page` (1-indexed), `pageSize` (default 20, max 100)
- Return array with: `id`, `title`, `company`, `location`, `jobSource`, `salary`, `currency`, `matchScore`, `createdAt`
- Order by `createdAt desc` (newest first)
- On success: return `200 OK` with `{ total, page, pageSize, items }`

---

**Feature: Filter Vacancies (POST /api/v1/vacancies/filter)**

**Acceptance Criteria:**
- Accept POST request with filter object in body (not query params)
- Filter criteria: `skills` (array, match any), `location` (string, partial match), `salaryMin` (number), `salaryMax` (number), `workLocationTypes` (array: remote/office/hybrid), `categories` (array), `experienceLevel` (enum), `excludeKeywords` (array)
- Pagination: `page`, `pageSize` in filter body
- Return results matching ALL provided criteria (AND logic between fields; OR logic within arrays)
- Return `200 OK` with same shape as List endpoint
- Return `400 Bad Request` if filter is malformed

**Sample Request Body:**
```json
{
  "skills": ["C#", "Azure"],
  "location": "Remote",
  "salaryMin": 80000,
  "salaryMax": 150000,
  "workLocationTypes": ["remote"],
  "categories": [],
  "experienceLevel": "mid",
  "excludeKeywords": [],
  "page": 1,
  "pageSize": 20
}
```

---

**Feature: Get Vacancy Detail (GET /api/v1/vacancies/{id})**

**Acceptance Criteria:**
- Return full vacancy with all fields: `title`, `description`, `company`, `companyWebsite`, `location`, `salary`, `currency`, `categories` (array), `skills` (array), `workLocationType`, `workTimeType`, `experienceLevel`, `matchScore`, `jobSource` (object with `name` and optional `url`), `isChosen`, `isHidden`, `createdAt`, `updatedAt`
- If not found: return `404 Not Found`
- Match score: if present, display as a number 0-100 (or null if not yet calculated in Phase B)

---

## Error Handling & Status Codes

**Standard Status Codes Across All Endpoints:**

- `200 OK` — Successful GET, PUT
- `201 Created` — Successful POST (return Location header with new resource URL)
- `204 No Content` — Successful DELETE
- `400 Bad Request` — Validation error, malformed request
- `404 Not Found` — Resource not found
- `409 Conflict` — Uniqueness constraint violated (e.g., duplicate email)
- `422 Unprocessable Entity` — Semantic validation error (e.g., vacancyId doesn't exist)
- `500 Internal Server Error` — Unhandled server error

**Error Response Format:**
```json
{
  "type": "https://api.jobnecto.dev/errors/validation",
  "title": "Validation Failed",
  "status": 400,
  "detail": "One or more validation errors occurred.",
  "errors": {
    "email": ["Email is already in use"],
    "phone": ["Phone must be in E.164 format"]
  }
}
```

---

## Data Model & Relationships

**Entity Relationship Overview:**

```
User (1)
├── (1:N) Resumes
├── (1:N) Educations
├── (M:N) Education (via ResumeEducations join table)
│   └── Resume
├── (1:N) CoverLetterTemplates
├── (1:N) CoverLetters
│   ├── (0:1) CoverLetterTemplate (optional reference)
│   └── (1:1) Vacancy
└── (1:N) Vacancies (job seekers' saved/viewed vacancies)
```

**Detailed Relationships:**

| From | To | Type | Cardinality | Notes |
|------|-----|------|-------------|-------|
| User | Resume | Foreign Key | 1:N | Each resume belongs to one user. Deleting user soft-deletes all resumes. |
| User | Education | Foreign Key | 1:N | Each education record belongs to one user. |
| Resume | Education | Join Table | M:N | A resume can require many educations; an education can be linked to many resumes via `ResumeEducations` table. |
| User | CoverLetterTemplate | Foreign Key | 1:N | Templates are user-owned. |
| User | CoverLetter | Foreign Key | 1:N | Application letters are user-owned. |
| CoverLetter | Vacancy | Foreign Key | 1:1 | Each cover letter is for a specific vacancy. Cannot have > 1 letter per vacancy per user. |
| CoverLetter | CoverLetterTemplate | Foreign Key (nullable) | 0:1 | Optional tracking of which template was the source. Not required for letter creation. |
| User | Vacancy | Foreign Key | 1:N | Each vacancy is "viewed by" or "saved by" a user. Vacancy itself is owned by user for filtering/management. |

**Cascade Delete Rules:**

| When X is deleted | What happens to related records |
|------------------|-------------------------------|
| User | All resumes, educations, cover letters, templates → soft-deleted |
| Resume | References removed from any CoverLetters (templateId becomes NULL) |
| CoverLetterTemplate | References in CoverLetters become NULL (template is optional) |
| Vacancy | CoverLetters referencing it are soft-deleted (application record removed) |
| Education | ResumeEducations join records deleted; resume still exists |

---

## Ownership & Authorization Model

**Principle:** Every resource has a clear owner. Phase B uses JWT-based sessions; browser flows use HTTP-only secure cookies and non-browser clients use `Authorization: Bearer`. `UserId` is extracted from token claims and used for all ownership checks. Token renewal contract: authenticated clients call `POST /api/v1/users/token/refresh`; if refresh is rejected due to expired/invalid credentials, the client re-authenticates.

**Ownership Rules:**

| Resource | Owner | Can View | Can Modify | Can Delete |
|----------|-------|----------|-----------|-----------|
| User Profile | Self | Self only | Self only | N/A (no delete user in Phase B) |
| Resume | User | Self only | Self only | Self (soft-delete) |
| Education | User | Self only | Self only | Self (soft-delete) |
| CoverLetterTemplate | User | Self only | Self only | Self (soft-delete) |
| CoverLetter | User | Self only | Self only | Self (soft-delete) |
| Vacancy | Public | All users | Read-only in Phase B | N/A |

**Implementation Detail (for Architecture phase):**

- **Query Filtering:** All list endpoints MUST filter by `userId` automatically
  - Example: `GET /api/v1/resumes` returns only resumes where `Resume.UserId == CurrentUserId`
  - This must be enforced at the repository or query handler level, not just API level
  - In Phase B, `UserId` is extracted from authenticated JWT claims (`sub` or `userId`) regardless of whether transport is cookie or bearer

- **Validation:** Write operations MUST verify ownership before allowing mutation
  - Example: `PUT /api/v1/resumes/{id}` verifies the resume's `UserId` matches the current user

- **Authorization Layer:** Phase B uses JWT-based sessions with explicit transport policy (cookie for browser, bearer for non-browser); `UserId` claim drives all ownership checks; Phase C adds role-based authorization and extended token lifecycle controls.

---

## Soft Delete Strategy

**Definition:**
Soft deletes are logical removals—data remains in the database but is marked as deleted and excluded from queries.

**Implementation Approach:**

Use a `IsDeleted` boolean flag (or `DeletedAt` timestamp) on affected entities:
- `Resume`, `Education`, `CoverLetter`, `CoverLetterTemplate` → add `IsDeleted` flag (or `DeletedAt` timestamp)
- `Vacancy` → add `IsDeleted` flag (or `DeletedAt` timestamp)

**Query Filter Behavior:**

- **All list endpoints** automatically exclude soft-deleted records
- **Detail endpoints** (GET by ID) should return 404 for soft-deleted records unless explicitly requested
- **Filters, searches, relationships** skip soft-deleted records by default

**EF Core Implementation Pattern (for Architecture phase):**

Use EF Core global query filters to exclude soft-deleted records from ALL queries automatically:

```csharp
// In DbContext.OnModelCreating
modelBuilder.Entity<Resume>()
    .HasQueryFilter(r => !r.IsDeleted);

modelBuilder.Entity<Education>()
    .HasQueryFilter(e => !e.IsDeleted);

// ... etc for all soft-delete entities
```

This ensures soft-deleted records are never accidentally exposed.

**Restore Capability:**

Decision: **Can soft-deleted records be restored?**
- For Phase B: Not a requirement; assume permanent once soft-deleted
- For Phase C onward: Architect to support restoration (set `IsDeleted = false`), but not yet exposed in API

---

## Technical Assumptions & Dependencies

**What Phase A Must Deliver:**

Phase B development assumes Phase A is **complete**:

1. ✅ **Database Setup**
   - PostgreSQL running locally with schema migrations applied
   - `AppDbContext` fully configured and callable from Application layer

2. ✅ **Entity Framework Core**
   - All entities defined in Domain layer
   - Configurations (relationships, constraints) in Infrastructure layer
   - Initial migration created and applied
   - `DbContext` accessible in Infrastructure

3. ✅ **UnitOfWork Pattern**
   - `IUnitOfWork` interface complete in Application layer
   - Implementation in Infrastructure with `SaveChangesAsync()`, `DisposeAsync()`
   - DI wiring ready in `Program.cs` (call to `AddInfrastructure()`)

4. ✅ **Repository Interfaces**
   - `IUserRepository`, `IResumeRepository`, `IEducationRepository`, `ICoverLetterRepository`, `ICoverLetterTemplateRepository`, `IVacancyRepository` defined in Application
   - Implementations in Infrastructure (can be stubs in Phase A)

5. ✅ **Migrations**
   - At least one migration created for base entities
   - Migration system working (can run `dotnet ef database update`)

**What Phase B Assumes About Dependencies:**

- **No external job sources yet** — Vacancies are assumed to exist in the database (manually seeded or Phase D will sync them)
- **No LLM integration** — LLM endpoints NOT included in Phase B; Phase D adds `/analyze` and cover letter generation
- **Authentication** — Phase B includes JWT-based sessions; browser flows issue HTTP-only secure cookies and non-browser clients use `Authorization: Bearer`; `UserId` is stored as a claim and extracted uniformly. Phase B renewal contract is `POST /api/v1/users/token/refresh` for active sessions; expired sessions re-authenticate. Phase C adds role-based authorization and extended refresh controls.
- **No third-party OAuth** — Phase B cannot connect to HeadHunter/LinkedIn; Phase D adds OAuth
- **PostgreSQL connection string** — Assumed available; see `appsettings.local.json` under `ConnectionStrings:Default` for the local connection string
- **Secrets handling** — Planning artifacts may reference configuration keys and local config file paths, but must not embed live credentials or secrets

---

## Domain Model Constraints & Validations

**Uniqueness Constraints:**

| Entity | Field(s) | Unique Per | Notes |
|--------|----------|-----------|-------|
| User | `loginName` | System-wide | Case-insensitive |
| User | `email` | System-wide | Case-insensitive |
| Resume | `title` | Per user | Same user cannot have 2 resumes with same title |
| CoverLetterTemplate | `name` | Per user | Same user cannot have 2 templates with same name |
| CoverLetter | `(vacancyId, userId)` | Per vacancy per user | Only one cover letter allowed per vacancy per user |

All uniqueness rules above must be enforced by database-level unique constraints and mapped to `409 Conflict` at the API boundary. For race-prone create/update paths, add at least one integration test that exercises concurrent requests.

**Referential Integrity:**

- `CoverLetter.VacancyId` → must exist in `Vacancy` table (no orphaned letters)
- `CoverLetter.TemplateId` → optional; if present, must exist and belong to same user
- `ResumeEducations.ResumeId` → must exist in `Resume` table
- `ResumeEducations.EducationId` → must exist in `Education` table

**Data Validation Summary:**

See Feature Requirements & Acceptance Criteria section for detailed field-level rules (email format, E.164 phone, salary > 0, etc.).

---

## Non-Functional Requirements

**API Response Time:**
- List endpoints: < 200ms for typical 20-item page
- Detail endpoints: < 100ms
- Filter endpoint: < 500ms for complex multi-criteria filter (depends on dataset size)

**Data Availability:**
- CRUD operations should not fail due to database unavailability in Phase B (error handling in Phase E)

**Pagination Limits:**
- `pageSize` minimum: 1
- `pageSize` maximum: 100 (prevent abuse)
- Default: 20 items per page

**Request Limits:**
- Request body size: max 1MB (prevents abuse)
- Content validation per field documented in Feature Requirements

---
