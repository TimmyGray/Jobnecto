# Story 5.1: Create Cover Letter

Status: done

## Story

As a job seeker,
I want to write a cover letter for a specific vacancy,
so that I can submit a tailored application.

## Acceptance Criteria

1. Given a valid JWT token and `POST /api/v1/cover-letters` with `vacancyId` (existing, owned by user) and `content` (50–10000 chars), when the request is processed, then `201 Created` with the cover letter object and `Location` header set to `/api/v1/cover-letters/{id}`.
2. Given the user already has a non-deleted cover letter for the same `vacancyId`, when `POST /api/v1/cover-letters` is called again with the same `vacancyId`, then `409 Conflict` from database-backed per-user/per-vacancy uniqueness enforcement.
3. Given `vacancyId` references a vacancy that does not exist or belongs to another user, when the request is processed, then `404 Not Found` with detail referencing the vacancy.
4. Given `content` is fewer than 50 or more than 10000 characters, when the request is processed, then `400 Bad Request` with field-level error on `content`.
5. Given no JWT token, when `POST /api/v1/cover-letters` is called, then `401 Unauthorized`.

## Tasks / Subtasks

- [x] Task 1: Add partial unique index to EF configuration and migrate (AC: 2)
  - [x] In `backend/src/JobNecto.Infrastructure/Persistance/Config/CoverLetterConfiguration.cs`, add partial unique index: `(UserId, VacancyId)` WHERE `IsDeleted = false`.
  - [x] Add EF migration: `dotnet ef migrations add AddCoverLetterUniqueVacancyPerUser --project backend/src/JobNecto.Infrastructure --startup-project backend/src/JobNecto.API`.

- [x] Task 2: Create command, DTO, and handler (AC: 1, 2, 3)
  - [x] Create `backend/src/JobNecto.Application/CoverLetters/CreateCoverLetterCommand.cs` with `CreateCoverLetterCommand` (implementing `IRequest<CreateCoverLetterResult>`) and `CreateCoverLetterResult` DTO in the same file.
  - [x] Create `backend/src/JobNecto.Application/CoverLetters/CreateCoverLetterCommandHandler.cs` — verify vacancy exists + belongs to user; create entity; catch `DbUpdateException` → throw `ConflictException`.
  - [x] Create `backend/src/JobNecto.Application/CoverLetters/Mappers/CoverLetterMappers.cs` with `ToEntity(CreateCoverLetterCommand)` and `ToCreateResult(CoverLetter)` extensions.
  - [x] Set `CreatedAt` and `UpdatedAt` to `DateTime.UtcNow` in the handler (per agent-learnings: do not rely on DB defaults in-memory).

- [x] Task 3: Create FluentValidation validator (AC: 4)
  - [x] Create `backend/src/JobNecto.Application/CoverLetters/Validators/CreateCoverLetterCommandValidator.cs`.
  - [x] Rules: `VacancyId` not empty; `Content` length 50–10000, not empty/whitespace.

- [x] Task 4: Add API controller and endpoint (AC: 1, 5)
  - [x] Create `backend/src/JobNecto.API/Controllers/CoverLettersController.cs` with `[ApiController]`, `[Route("api/v1/cover-letters")]`, `[Authorize]`.
  - [x] Add `[HttpPost]` action: extract `UserId` via `HttpContext.GetCurrentUserId()`; dispatch `CreateCoverLetterCommand`; return `Created($"/api/v1/cover-letters/{result.Id}", result)`.
  - [x] `ProducesResponseType` for 201, 400, 401, 404, 409.
  - [x] Use `BadRequest(new ProblemDetails { Status = 400, Title = "Validation failed", Detail = "..." })` pattern (per agent-learnings — never `BadRequest("plain string")`).

- [x] Task 5: Add tests (AC: 1–5)
  - [x] Create `backend/tests/JobNecto.Tests/Application/CoverLetters/CreateCoverLetterCommandHandlerTests.cs` — mock `IUnitOfWork`; test: valid create succeeds, missing vacancy → NotFoundException, other-user vacancy → NotFoundException, duplicate (DbUpdateException with unique violation) → ConflictException.
  - [x] Create `backend/tests/JobNecto.Tests/Application/CoverLetters/CreateCoverLetterCommandValidatorTests.cs` — test content boundary (49, 50, 10000, 10001 chars), empty/whitespace content, empty vacancyId.
  - [x] Create `backend/tests/JobNecto.Tests/API/CoverLetters/CoverLettersApiTests.cs` — integration tests: no token → 401; valid create → 201 with Location header; content too short → 400; duplicate vacancyId (same user) → 409; invalid vacancyId → 404.

- [x] Task 6: Verification gates
  - [x] Build: `dotnet build backend/JobNecto.slnx --configuration Release --warnaserror` — 0 warnings, 0 errors.
  - [x] Tests: `dotnet test backend/JobNecto.slnx` — all pass.

## Dev Notes

### EF Configuration Change

In `CoverLetterConfiguration.Configure`, add the partial unique index:

```csharp
// One non-deleted cover letter per user per vacancy
builder.HasIndex(cl => new { cl.UserId, cl.VacancyId })
    .IsUnique()
    .HasFilter("\"IsDeleted\" = false");
```

PostgreSQL-specific filter syntax. The migration will emit `WHERE ("IsDeleted" = false)`. No changes to the `CoverLetter` domain entity are needed.

### Critical: 409 Must Come from DB Constraint

Per NFR13 and the Epic 5 readiness constraint: the uniqueness rule must be backed by a database constraint. Throw `ConflictException` by catching `DbUpdateException` (not by pre-checking existence), exactly as cover letter template name uniqueness works.

```csharp
try
{
    await _unitOfWork.CoverLetterRepository.CreateAsync(coverLetter, cancellationToken);
    await _unitOfWork.SaveChangesAsync(cancellationToken);
}
catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("unique") == true
                                   || ex.InnerException?.Message.Contains("duplicate") == true)
{
    throw new ConflictException("A cover letter for this vacancy already exists.");
}
```

### Vacancy Ownership Check

Per AC 3: vacancy must exist **and belong to the current user**. `GetByIdAsync` throws `NotFoundException` if absent. Check ownership after — return `NotFoundException` (not `ForbiddenException`) to prevent existence leakage, consistent with Decision 7.

```csharp
var vacancy = await _unitOfWork.VacancyRepository.GetByIdAsync(request.VacancyId, cancellationToken);
if (vacancy.UserId != request.UserId)
    throw new NotFoundException("Vacancy", request.VacancyId);
```

### Response DTO

`CreateCoverLetterResult` fields: `Id`, `VacancyId`, `Content`, `CreatedAt`, `UpdatedAt`.

### Controller Pattern

Follow `CoverLetterTemplatesController` exactly for auth extraction and command population. `UserId` is injected from JWT: `command.UserId = userId;` — mark it `[JsonIgnore]` on the command.

### Test: 409 Uniqueness

The 409 integration test must seed a first cover letter for the user+vacancy, then POST again with the same `vacancyId`. EF InMemory does not enforce unique constraints at the DB level — follow the `CoverLetterTemplatesUniquenessApiTests.cs` test setup pattern (uses a real Npgsql test DB) for the 409 case.

### Files to Read Before Implementation

- `backend/src/JobNecto.Domain/Entities/CoverLetter.cs` (current entity — no changes needed)
- `backend/src/JobNecto.Infrastructure/Persistance/Config/CoverLetterConfiguration.cs`
- `backend/src/JobNecto.Infrastructure/Persistance/Config/CoverLetterTemplateConfiguration.cs` (unique index pattern reference)
- `backend/src/JobNecto.Application/CoverLetterTemplates/CreateCoverLetterTemplateCommandHandler.cs` (DbUpdateException catch pattern)
- `backend/src/JobNecto.Application/Interfaces/IUnitOfWork.cs`
- `backend/tests/JobNecto.Tests/API/CoverLetterTemplates/CoverLetterTemplatesUniquenessApiTests.cs` (409 test pattern)

### Project Structure Notes

- New files under `backend/src/JobNecto.Application/CoverLetters/`, `CoverLetters/Validators/`, `CoverLetters/Mappers/`.
- Controller at `backend/src/JobNecto.API/Controllers/CoverLettersController.cs`.
- Tests at `backend/tests/JobNecto.Tests/Application/CoverLetters/` and `API/CoverLetters/`.
- Namespaces: `JobNecto.Application.CoverLetters`, `JobNecto.Application.CoverLetters.Validators`, `JobNecto.Application.CoverLetters.Mappers`, `JobNecto.API.Controllers`, `JobNecto.Tests.Application.CoverLetters`, `JobNecto.Tests.API.CoverLetters`.

### References

- [Source: `_bmad-output/archive/planning-artifacts/epics/epic-5-cover-letter-application-management.md` — Story 5.1]
- [Source: `_bmad-output/planning-artifacts/epics/requirements-inventory.md` — FR19, NFR2, NFR13]
- [Source: `_bmad-output/planning-artifacts/architecture/core-architectural-decisions.md` — Decision 2, 3, 4, 6, 7]
- [Source: `_bmad-output/agent-learnings.md` — timestamp in handlers, BadRequest shape, DB-backed uniqueness]

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Completion Notes List

- `templateId` dropped per product decision: content is injected by the user; no reference persisted.
- `CoverLetter` entity unchanged — only EF configuration (partial unique index) and migration needed.
- Vacancy ownership returns 404 (not 403) per Decision 7.
- 409 must be DB-backed — catch `DbUpdateException`.

### File List

- `backend/src/JobNecto.Application/CoverLetters/CreateCoverLetterCommand.cs` (CREATED)
- `backend/src/JobNecto.Application/CoverLetters/CreateCoverLetterCommandHandler.cs` (CREATED)
- `backend/src/JobNecto.Application/CoverLetters/Mappers/CoverLetterMappers.cs` (CREATED)
- `backend/src/JobNecto.Application/CoverLetters/Validators/CreateCoverLetterCommandValidator.cs` (CREATED)
- `backend/src/JobNecto.API/Controllers/CoverLettersController.cs` (CREATED)
- `backend/tests/JobNecto.Tests/Application/CoverLetters/CreateCoverLetterCommandHandlerTests.cs` (CREATED)
- `backend/tests/JobNecto.Tests/Application/CoverLetters/CreateCoverLetterCommandValidatorTests.cs` (CREATED)
- `backend/tests/JobNecto.Tests/API/CoverLetters/CoverLettersApiTests.cs` (CREATED)
- `backend/tests/JobNecto.Tests/API/CoverLetters/CoverLettersUniquenessApiTests.cs` (CREATED)
- `backend/src/JobNecto.Infrastructure/Persistance/Config/CoverLetterConfiguration.cs` (UPDATED — partial unique index)
- `backend/src/JobNecto.Infrastructure/Migrations/20260510192135_AddCoverLetterUniqueVacancyPerUser.cs` (CREATED)
- `backend/src/JobNecto.Infrastructure/Migrations/20260510192135_AddCoverLetterUniqueVacancyPerUser.Designer.cs` (CREATED)
- `backend/src/JobNecto.Infrastructure/Migrations/AppDbContextModelSnapshot.cs` (UPDATED)
- `_bmad-output/archive/implementation-artifacts/5-1-create-cover-letter.md` (this file)

