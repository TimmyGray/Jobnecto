# Requirements Inventory

## Functional Requirements

FR1: User can create a new account with `loginName`, `email`, `password`, and optional profile fields (`phone`, `location`, `about`, `avatar`) via `POST /api/v1/users`; returns `201 Created` with user object (excluding password).
FR2: User can retrieve their core profile fields via `GET /api/v1/users/me`; related resources (resumes, educations, cover letters) are retrieved through dedicated user-scoped endpoints; returns `200 OK`.
FR3: User can update profile fields (`email`, `loginName`, `phone`, `location`, `about`, `avatar`) via `PATCH /api/v1/users/me`; `loginName` is mutable but must remain unique system-wide; returns `200 OK` with updated object; `409 Conflict` if new `loginName` is already taken.
FR4: User can create a resume with `title`, `skills` (array, min 1), `workLocationType` (enum: remote/office/hybrid), and optional fields (`salary`, `currency`, `experience`, `projects`, `certifications`, `languages`, `locations`, `excludedWords`) via `POST /api/v1/resumes`; returns `201 Created`.
FR5: User can list their own resumes (paginated: default 20, max 100, ordered by `updatedAt desc`) via `GET /api/v1/resumes`; returns `200 OK` with `{ total, page, pageSize, items }`.
FR6: User can retrieve a single resume with all fields via `GET /api/v1/resumes/{id}`; returns `404` if not found.
FR7: User can update any resume field via `PATCH /api/v1/resumes/{id}`; returns `200 OK`; `404` if not found.
FR8: User can soft-delete a resume via `DELETE /api/v1/resumes/{id}`; returns `204 No Content`; resume disappears from list endpoints.
FR9: User can create an education record with `title`, `specialization`, `degree` (enum: bachelor/master/phd/postdoc/other) via `POST /api/v1/educations`; returns `201 Created`.
FR10: User can list their own education records (paginated: default 20, max 100, ordered by `updatedAt desc`) via `GET /api/v1/educations`; returns `200 OK` with `{ totalCount, pageSize, hasNext, lastSeenId, lastSeenUpdatedAt, items }`.
FR11: User can retrieve a single education record via `GET /api/v1/educations/{id}`; returns `404` if not found.
FR12: User can update education fields via `PATCH /api/v1/educations/{id}`; returns `200 OK`; `400` for invalid or empty payloads; `403` if the record belongs to another user; `404` if not found.
FR13: User can soft-delete an education record via `DELETE /api/v1/educations/{id}`; returns `204 No Content`; deleted records disappear from list and detail endpoints.
FR14: User can create a cover letter template with `name` (unique per user) and `content` (50-10000 chars) via `POST /api/v1/cover-letter-templates`; returns `201 Created`.
FR15: User can list their cover letter templates (paginated, searchable by `name`, ordered by `updatedAt desc`) via `GET /api/v1/cover-letter-templates`; returns `200 OK` with content preview (first 200 chars).
FR16: User can retrieve a single cover letter template with full `content` via `GET /api/v1/cover-letter-templates/{id}`; returns `404` if not found.
FR17: User can update a cover letter template's `name` or `content` via `PATCH /api/v1/cover-letter-templates/{id}`; name uniqueness re-validated if changed; returns `200 OK`.
FR18: User can soft-delete a cover letter template via `DELETE /api/v1/cover-letter-templates/{id}`; returns `204 No Content`.
FR19: User can create a job-specific cover letter with `vacancyId` (required, must exist) and `content` (50-10000 chars) via `POST /api/v1/cover-letters`; one letter per vacancy per user; returns `201 Created`.
FR20: User can list their cover letters (paginated, ordered by `createdAt desc`) via `GET /api/v1/cover-letters`; includes `vacancyTitle` from linked vacancy; returns `200 OK`.
FR21: User can retrieve a single cover letter with full `content`, `vacancyId`, and linked vacancy details via `GET /api/v1/cover-letters/{id}`; returns `404` if not found.
FR22: User can update cover letter `content` (cannot change `vacancyId` after creation) via `PATCH /api/v1/cover-letters/{id}`; returns `200 OK`.
FR23: User can soft-delete a cover letter via `DELETE /api/v1/cover-letters/{id}`; returns `204 No Content`.
FR24: Authenticated user can browse their own paginated vacancies (default 20, max 100) via `POST /api/v1/vacancies/filter` using empty criteria (`{}`); default ordering is `createdAt desc`, optional `sortBy` supports `updatedAt` and `relevance` (alias of `updatedAt`); returns `200 OK` with `{ totalCount, pageSize, hasNext, lastSeenId, lastSeenUpdatedAt, items }`.
FR25: Authenticated user can filter their own vacancies by multi-criteria (`skills`, `location`, `salaryMin`, `salaryMax`, `workLocationTypes`, `categories`, `experienceLevel`, `excludeKeywords`, `lastSeenId`, `lastSeenUpdatedAt`, `pageSize`, `sortBy`) via `POST /api/v1/vacancies/filter`; AND logic between fields, OR within arrays; returns `200 OK` with the same pagination envelope as browse mode.
FR26: Any user can retrieve a single vacancy with all fields (title, description, company, skills, workLocationType, matchScore, jobSource, etc.) via `GET /api/v1/vacancies/{id}`; returns `404` if not found.
FR27: All user-owned list endpoints must automatically filter by `userId` at the repository/handler level to enforce data isolation.
FR28: All mutation handlers must verify resource ownership before allowing changes; return `403 Forbidden` or throw ownership exception if violated.

### NonFunctional Requirements

NFR1: API response SLA: list endpoints < 200ms, detail endpoints < 100ms, `POST /api/v1/vacancies/filter` < 500ms (for typical dataset).
NFR2: All input must be validated via FluentValidation pipeline before handlers execute; field-level error messages returned in RFC 7808 Problem Details format.
NFR3: >= 85% code coverage on Application layer handlers and validators (measured by xUnit + FluentAssertions).
NFR4: Full async/await chain with `CancellationToken` propagation on all public async methods (handlers, repositories, EF calls).
NFR5: Soft-deleted records excluded from ALL queries via EF Core global query filters on `DbContext.OnModelCreating`; never accidentally exposed.
NFR6: Build must pass: `dotnet build backend/JobNecto.slnx --configuration Release --warnaserror` with zero warnings.
NFR7: All tests must pass: `dotnet test backend/JobNecto.slnx --configuration Release --warnaserror`.
NFR8: No nullable reference type warnings suppressed without documented justification.
NFR9: OpenAPI spec auto-generated for all Phase B endpoints with request/response schemas, status codes (200, 201, 204, 400, 404, 409, 422, 500), and example bodies.
NFR10: Pagination: cursor-based (`lastSeenId` + `lastSeenUpdatedAt`); `pageSize` min 1, max 100, default 20. For vacancies, `lastSeenUpdatedAt` carries cursor timestamp for the selected sort mode (`createdAt` by default, `updatedAt` for `sortBy=updatedAt|relevance`). Response shape: `{ totalCount, pageSize, hasNext, lastSeenId, lastSeenUpdatedAt, items }`. Request body max 1MB.
NFR11: Zero breaking changes to Domain model entities after Phase B is complete.
NFR12: Passwords must be stored only as one-way salted hashes; plaintext passwords are never persisted or returned, and any migration/backfill required to reach that state is part of auth readiness.
NFR13: Any business rule surfaced as `409 Conflict` must be backed by a database-level uniqueness constraint and at least one integration test that exercises concurrent create/update attempts.

### Additional Requirements (from Architecture)

- **MediatR CQRS**: All operations route through MediatR `IRequest`/`IRequestHandler`; Commands for writes, Queries for reads; no direct service calls in API layer.
- **Two-layer validation**: FluentValidation fires in MediatR pipeline (field format/length) -> handler performs DB-dependent checks (uniqueness, FK existence, ownership).
- **Repository + UnitOfWork pattern**: All `IXxxRepository` interfaces in `JobNecto.Application`; implementations in `JobNecto.Infrastructure`; `IUnitOfWork` aggregates all repos and exposes `SaveChangesAsync()`.
- **Exception handling middleware**: `ExceptionHandlingMiddleware` maps domain exceptions (`UserNotFoundException`, `DuplicateEmailException`, etc.) to RFC 7808 Problem Details responses; no raw stack traces exposed.
- **Soft-delete entities**: `Resume`, `Education`, `CoverLetterTemplate`, `CoverLetter`, `Vacancy` extend `SoftDeletableEntity` with `IsDeleted` (bool) + `DeletedAt` (DateTime?).
- **EF Core cascade rules**: User->Resume `ON DELETE CASCADE` (hard); Resume->CoverLetter `ON DELETE CASCADE` (hard); soft-delete cascades (Resume->CoverLetters) handled in handlers.
- **UserId injection pattern**: Phase B uses JWT-based authentication; browser flows issue an HTTP-only auth cookie, and any non-browser client flow must explicitly document `Authorization: Bearer` transport plus token renewal behavior. `UserId` is stored as a claim in the token and extracted via `GetCurrentUserId()` helper in controllers from `HttpContext.User`. Handlers receive `UserId` as a parameter - no handler changes needed if token structure evolves.
- **Clean Architecture boundaries**: Domain layer is persistence-ignorant (no EF references); Application layer has no EF direct calls; Infrastructure implements all persistence concerns.
- **Migrations**: All EF Core migrations in `JobNecto.Infrastructure`; `AddInfrastructure()` DI extension called in `Program.cs`.
- **OpenAPI**: All controllers use Swashbuckle attributes; spec accessible at `/openapi/v1.json`.
- **Audit logging**: All hard-delete operations logged with timestamp, `userId`, and affected record summary.
- **Secrets management**: Planning artifacts and repository docs may reference configuration keys or local config files, but must not embed live credentials or secrets.

### UX Design Requirements

_No UX design document for this API-only backend phase._

### FR Coverage Map

FR1: Epic 1 - Create user account
FR2: Epic 1 - Retrieve user profile
FR3: Epic 1 - Update profile (incl. loginName)
FR4: Epic 2 - Create resume
FR5: Epic 2 - List resumes
FR6: Epic 2 - Get resume detail
FR7: Epic 2 - Update resume
FR8: Epic 2 - Soft-delete resume
FR9: Epic 2 - Create education
FR10: Epic 2 - List educations
FR11: Epic 2 - Get education detail
FR12: Epic 2 - Update education
FR13: Epic 2 - Soft-delete education
FR14: Epic 3 - Create cover letter template
FR15: Epic 3 - List templates (paginated + search)
FR16: Epic 3 - Get template detail
FR17: Epic 3 - Update template
FR18: Epic 3 - Soft-delete template
FR19: Epic 5 - Create cover letter
FR20: Epic 5 - List cover letters
FR21: Epic 5 - Get cover letter detail
FR22: Epic 5 - Update cover letter
FR23: Epic 5 - Soft-delete cover letter
FR24: Epic 4 - List vacancies
FR25: Epic 4 - Filter vacancies
FR26: Epic 4 - Get vacancy detail
FR27: Epic 2 - User-scoped data isolation
FR28: Epic 1 - Ownership enforcement infrastructure
