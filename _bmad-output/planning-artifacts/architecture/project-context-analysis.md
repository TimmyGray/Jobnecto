# Project Context Analysis

### Requirements Overview

**Functional Requirements (Phase B):**

- 6 primary domain resources: User (Profile), Resume, Education, CoverLetterTemplate, CoverLetter, Vacancy
- Epic 2 delivered complete Resume and Education CRUD endpoints. Epic 3 delivered CoverLetterTemplate create/list/detail endpoints; CoverLetterTemplate update/delete plus CoverLetter and Vacancy endpoint work remains scheduled for later epics.
- `GET /api/v1/users/me` returns only core user profile fields; related resources are fetched via dedicated resource routes
- CRUD operations on user-owned resources (User, Resume, Education, Template, Letter)
- Read-only operations on shared resources (Vacancy browsing and filtering)
- Filtering: Complex multi-criteria vacancy filtering via POST body (skills, location, salary, work type, etc.)
- Paginated list operations with cursor-based pagination (`lastSeenId` + `lastSeenUpdatedAt`); `pageSize` default 20, max 100

**Non-Functional Requirements:**

- API response SLA: <200ms for list endpoints, <100ms for detail endpoints, <500ms for complex filters
- Soft delete strategy with EF Core global query filters (data persists in DB but excluded from queries)
- Ownership model: Each user sees only their own data with JWT-based session claims already in use; architecture remains extensible for Phase C role-based controls
- Validation: FluentValidation pipeline before handlers reach business logic; field-level error responses
- Test coverage requirement: at least 85% code coverage on Application handlers and validators
- Async throughout: Full async/await chain with CancellationToken support on public APIs

**Scale & Complexity:**

- Complexity level: **Medium** (multi-domain entities, complex filtering logic, relationship complexity, soft deletes)
- Primary technical domain: **REST API Backend** (job aggregation and profile management)
- Estimated architectural components required: **7-8 major architectural decisions**

### Technical Constraints & Dependencies

**Technology Stack (Locked):**

- .NET 10 (`net10.0`), nullable reference types enforced
- ASP.NET Core 10.0.3 with OpenAPI/Swashbuckle auto-documentation
- MediatR 14.0.0 for CQRS command/query pattern
- FluentValidation 12.1.1 with built-in ASP.NET Core DI extensions
- Entity Framework Core 10.0.3 with Npgsql provider for PostgreSQL
- xUnit 2.9.3 + FluentAssertions 8.9.0 + Moq 4.20.72 for testing
- Project uses `backend/JobNecto.slnx` (not root `.sln`)

**Architecture Constraints:**

- Clean Architecture enforced: API -> Application -> Domain <- Infrastructure
- Domain layer must be persistence-ignorant (no EF Core references)
- Application layer contains handlers, validators, repository interfaces
- Infrastructure layer implements repositories, EF configurations, migrations
- No reference inversions or architectural boundary violations allowed

**Build & Quality Gates:**

- Must pass: `dotnet build backend/JobNecto.slnx --configuration Release --warnaserror`
- Must pass: `dotnet test backend/JobNecto.slnx --configuration Release --warnaserror`
- No nullable reference type warnings without documented suppression reason
- CI-parity enforcement: Release build + warnings-as-errors

**Phase A Assumptions (Must Be Complete):**

- UnitOfWork pattern implemented with `SaveChangesAsync()` and `DisposeAsync()`
- All repository interfaces defined in Application layer
- PostgreSQL connection available - see `appsettings.local.json` under `ConnectionStrings:Default` for the local connection string
- EF Core migrations system functional
- `AddInfrastructure()` DI registration wired in `Program.cs`

**Out of Scope for Phase B:**

- Role-based authorization rules and advanced refresh-token orchestration (Phase C)
- Job source synchronization and OAuth integrations (Phase D)
- LLM analysis and cover letter generation endpoints (Phase D)
- Rate limiting, Redis caching, Quartz scheduled jobs (Phase E)

### Cross-Cutting Concerns

**1. Ownership & Authorization**

- Every resource must enforce user ownership before write operations
- `/api/v1/users/me` is a profile-only endpoint and must not embed resumes, educations, or cover letters
- **Resumes are user-scoped:** Each user sees only their own resumes (filtered at repository layer)
- All user-owned list queries automatically filter by current user (via query handlers or repository layer)
- User-owned collection endpoints are explicit resource routes (`/resumes`, `/educations`, `/cover-letter-templates`, `/cover-letters`), never cross-user aggregates
- Architecture standardizes JWT session transport (HTTP-only secure cookie for browser clients; `Authorization: Bearer` for non-browser clients) and must support Phase C role-based extension without refactoring core patterns
- Design pattern: ownership check delegated to handler, not API layer
- **UserId source:** Generated during user registration (profile creation); Phase B uses JWT-based sessions with `UserId` stored as a claim and extracted in controllers from `HttpContext.User`
- **Epic 2 baseline:** List endpoints are user-scoped through `PagedQuery.UserId`; detail/update/delete handlers fetch by ID and then enforce ownership in handler code. Ownership-aware single-record repository access is a candidate Epic 3 refinement.

**2. Soft Delete Consistency**

- Entities marked for soft delete: Resume, Education, CoverLetter, CoverLetterTemplate, Vacancy
- Strategy: EF Core global query filters on `DbContext.OnModelCreating` + PostgreSQL cascade rules
- Soft-deleted records excluded from ALL queries unless explicitly requested
- Single source of truth: query filters prevent accidental data exposure across entire application
- **Cascade Hard-Delete Rules (PostgreSQL-level):**
  - User hard-deleted -> Resumes, Educations, CoverLetters, and CoverLetterTemplates cascade hard-delete
  - Vacancy hard-deleted -> related CoverLetters cascade hard-delete
  - Resume and Education soft-delete operations do not currently cascade to other resources
- **Logging:** Hard-delete audit requirements remain a production-readiness decision; implement full audit logging when hard-delete/account-deletion stories are scheduled
- **Grace Period (Future - Phase E):** TTL implemented in Phase E; soft-deleted records eligible for hard-delete after 1 month

**3. Validation & Error Handling**

- Two-layer validation strategy: FluentValidation (field-level) + handler-level (business rules)
- FluentValidation validators fire before handler execution (MediatR pipeline)
- Error response format: RFC 7808 Problem Details (application/problem+json)
- HTTP status codes: 200, 201, 204 (success); 400, 404, 409, 422, 500 (errors)
- Field-level errors: `{ "email": ["must be valid format"], "phone": ["already in use"] }`
- Any business rule exposed as `409 Conflict` must be enforced by a database-level unique constraint, with DB unique-violation mapped through global exception handling and at least one concurrent-request integration test on race-prone endpoints.
- Epic 3 cover letter template-name uniqueness must follow this rule with a per-user database unique constraint; validator-only enforcement is not acceptable.
- Validator policy still needs a checklist for null, empty-string, whitespace, max-length, enum, and cross-field error-key semantics.

**4. Complex Filtering Architecture**

- Vacancy filtering accepts POST body (not query params) to support multi-criteria queries
- Filter object contains: skills[], location, salaryMin/Max, workLocationType[], etc.
- AND logic between fields, OR logic within arrays (e.g., matches ANY skill)
- Pagination included in filter object (`pageSize`, `lastSeenId`, `lastSeenUpdatedAt` cursor)
- Design advantage: No URL length limits; type-safe filtering with request models

**5. Async-First Pattern**

- All I/O operations: async/await end-to-end
- CancellationToken parameters on all public async handlers
- EF Core async methods: `ToListAsync()`, `FirstOrDefaultAsync()`, `SaveChangesAsync()`
- Database context should be disposed via `await using` in tests

**6. Timestamp Policy**

- Current implementation uses `DateTime` and direct `DateTime.UtcNow` in handlers.
- Persisted timestamps are treated as UTC by convention.
- Cursor pagination uses `lastSeenUpdatedAt`; timestamp kind/offset normalization is deferred and should be decided before this pattern spreads further.
- `DateTimeOffset` adoption and injectable clock support are project-level decisions, not story-local changes.

**7. Database Migrations & Relationships**

- Migrations live in Infrastructure; tracked via EF Core schema history
- Entity relationships: 1:N (User -> Resume, User -> Education, User -> CoverLetter, User -> CoverLetterTemplate, Vacancy -> CoverLetter), M:N (Resume <-> Education via join table)
- **Cascade Rules (EF Core + PostgreSQL):**
  - User -> Resume, Education, CoverLetter, CoverLetterTemplate: ON DELETE CASCADE (hard-delete when user deleted)
  - Vacancy -> CoverLetter: ON DELETE CASCADE (hard-delete when vacancy deleted)
  - Soft deletes use EF Core global filters; cascades handled in OnModelCreating
  - PostgreSQL foreign keys enforce referential integrity; EF tracks related entities
- ResumeEducations join table: supports M:N linkage without direct references; cascade soft-deletes to join table entries

**8. OpenAPI Documentation**

- Swashbuckle auto-generates OpenAPI spec from attributes and models
- Every endpoint must have clear request/response models for auto-documentation
- Status codes documented in swagger attributes (200, 201, 400, 404, 422, etc.)
- Models exported to `/openapi/v1.json` for frontend code generation

