# Story 3.4: Update Cover Letter Template

Status: done

## Story

As a job seeker,
I want to update a template's name or content,
so that I can refine my reusable material over time.

## Acceptance Criteria

1. Given a valid JWT token and `PATCH /api/v1/cover-letter-templates/{id}` with new `name` and/or `content`, when the request is processed then `200 OK` is returned with the updated template and refreshed `updatedAt`.
2. Given the new `name` is already taken by another non-deleted template of this user, when the request is processed then `409 Conflict` is returned from database-backed per-user uniqueness enforcement.
3. Given the template belongs to another user, then `403 Forbidden` is returned.
4. Given updated `content` violates 50-10000 char bounds, then `400 Bad Request` is returned with field-level validation error on `content`.
5. Given the template does not exist or is soft-deleted, then `404 Not Found` is returned.
6. Given neither `name` nor `content` is provided, then `400 Bad Request` is returned.

## Tasks / Subtasks

- [x] Task 1: Implement Application update flow for cover letter templates (AC: 1, 2, 3, 4, 5, 6)
  - [x] Add `backend/src/JobNecto.Application/CoverLetterTemplates/UpdateCoverLetterTemplateCommand.cs` implementing `IRequest<CoverLetterTemplateResult>`.
  - [x] Include `[JsonIgnore]` route/auth-injected fields: `CoverLetterTemplateId` and `UserId`.
  - [x] Include optional payload fields: `string? Name` and `string? Content` (partial update semantics per story AC wording "name and/or content").
  - [x] Add `backend/src/JobNecto.Application/CoverLetterTemplates/Validators/UpdateCoverLetterTemplateCommandValidator.cs`:
    - [x] Validate `CoverLetterTemplateId` and `UserId` are not empty.
    - [x] Require at least one updatable field (`Name` or `Content`).
    - [x] When `Name` is provided: reject empty/whitespace and enforce max length 100.
    - [x] When `Content` is provided: enforce min 50 and max 10000.
  - [x] Add `ApplyUpdates(this CoverLetterTemplate template, UpdateCoverLetterTemplateCommand command)` to `backend/src/JobNecto.Application/CoverLetterTemplates/Mappers/CoverLetterTemplateMappers.cs`:
    - [x] Update only provided fields.
    - [x] Preserve `Id`, `UserId`, and `CreatedAt`.
  - [x] Add `backend/src/JobNecto.Application/CoverLetterTemplates/UpdateCoverLetterTemplateCommandHandler.cs`:
    - [x] Load entity via `_unitOfWork.CoverLetterTemplateRepository.GetByIdAsync(...)`.
    - [x] If ownership mismatches, throw `ForbiddenException("You do not have permission to update this cover letter template.")`.
    - [x] Apply updates, set `UpdatedAt = DateTime.UtcNow`, call `UpdateAsync`, then `SaveChangesAsync`.
    - [x] Return `template.ToCoverLetterTemplateResult()`.

- [x] Task 2: Add authenticated PATCH endpoint (AC: 1, 2, 3, 4, 5, 6)
  - [x] Update `backend/src/JobNecto.API/Controllers/CoverLetterTemplatesController.cs` with `[HttpPatch("{id:guid}")]` action.
  - [x] Use strict auth guard pattern:
    - [x] `var userIdValue = HttpContext.GetCurrentUserId();`
    - [x] `if (string.IsNullOrWhiteSpace(userIdValue) || !Guid.TryParse(userIdValue, out var userId)) return Unauthorized();`
  - [x] Inject route and auth fields into command: `command.CoverLetterTemplateId = id; command.UserId = userId;`.
  - [x] Dispatch via `_mediator.Send(command, cancellationToken)` and return `Ok(result)`.
  - [x] Add response metadata:
    - [x] `200 OK` (`CoverLetterTemplateResult`)
    - [x] `400 BadRequest`
    - [x] `401 Unauthorized`
    - [x] `403 Forbidden`
    - [x] `404 NotFound`
    - [x] `409 Conflict`

- [x] Task 3: Add Application unit tests (AC: 1, 2, 3, 4, 5, 6)
  - [x] Create `backend/tests/JobNecto.Tests/Application/CoverLetterTemplates/UpdateCoverLetterTemplateCommandValidatorTests.cs`:
    - [x] no updatable fields -> invalid
    - [x] valid `Name` only -> valid
    - [x] valid `Content` only -> valid
    - [x] whitespace/too-long `Name` -> invalid
    - [x] `Content` below 50 / above 10000 -> invalid
  - [x] Create `backend/tests/JobNecto.Tests/Application/CoverLetterTemplates/UpdateCoverLetterTemplateCommandHandlerTests.cs`:
    - [x] owned template update returns updated result
    - [x] `UpdatedAt` refreshed in UTC
    - [x] cross-user template update throws `ForbiddenException`
    - [x] non-existent/soft-deleted id propagates `NotFoundException`

- [x] Task 4: Add API integration tests for PATCH endpoint (AC: 1, 2, 3, 4, 5, 6)
  - [x] Update `backend/tests/JobNecto.Tests/API/CoverLetterTemplates/CoverLetterTemplatesApiTests.cs`.
  - [x] Add helper: `PatchTemplateAsync(HttpClient client, string authCookie, Guid id, object payload)`.
  - [x] Add tests:
    - [x] `Patch_WithoutToken_Returns401`
    - [x] `Patch_OwnedTemplate_NameOnly_Returns200AndPreservesContent`
    - [x] `Patch_OwnedTemplate_ContentOnly_Returns200AndPreservesName`
    - [x] `Patch_AnotherUsersTemplate_Returns403`
    - [x] `Patch_NonExistentId_Returns404`
    - [x] `Patch_InvalidContentBounds_Returns400WithFieldError`
    - [x] `Patch_EmptyBody_Returns400`

- [x] Task 5: Extend PostgreSQL uniqueness tests for update collisions (AC: 2)
  - [x] Update `backend/tests/JobNecto.Tests/API/CoverLetterTemplates/CoverLetterTemplatesUniquenessApiTests.cs`.
  - [x] Add update uniqueness tests using `CoverLetterTemplatesPostgresFactory`:
    - [x] rename to another existing non-deleted template name of same user -> `409 Conflict`
    - [x] same name used by different user does not block update -> `200 OK`
    - [x] concurrent rename collision test -> one success and at least one conflict

- [x] Task 6: Verification gates
  - [x] Run targeted tests:
    - [x] `dotnet test backend/JobNecto.slnx --filter "FullyQualifiedName~CoverLetterTemplates"`
  - [x] Run full tests:
    - [x] `dotnet test backend/JobNecto.slnx`
  - [x] Run CI parity:
    - [x] `dotnet build backend/JobNecto.slnx --configuration Release --warnaserror`
    - [x] `dotnet test backend/JobNecto.slnx --configuration Release --no-build --warnaserror`

## Dev Notes

### Scope and behavior guardrails

- This story adds update behavior only. Do not change create/list/detail semantics from stories 3.1-3.3.
- Endpoint verb is `PATCH` for story contract alignment (`FR17` and Epic 3 story text), and payload supports updating `name` and/or `content`.
- Keep ownership semantics explicit:
  - Detail read cross-user -> `404` (already implemented in 3.3).
  - Mutation cross-user -> `403` (required for 3.4).

### Existing code baseline to extend

- Controller currently has `POST`, list `GET`, and detail `GET` only:
  - `backend/src/JobNecto.API/Controllers/CoverLetterTemplatesController.cs`
- Existing create/list/detail handlers:
  - `backend/src/JobNecto.Application/CoverLetterTemplates/CreateCoverLetterTemplateCommandHandler.cs`
  - `backend/src/JobNecto.Application/CoverLetterTemplates/ListCoverLetterTemplatesQueryHandler.cs`
  - `backend/src/JobNecto.Application/CoverLetterTemplates/GetCoverLetterTemplateQueryHandler.cs`
- Existing mapper and DTO to reuse:
  - `backend/src/JobNecto.Application/CoverLetterTemplates/Mappers/CoverLetterTemplateMappers.cs`
  - `CoverLetterTemplateResult` in `backend/src/JobNecto.Application/CoverLetterTemplates/CreateCoverLetterTemplateCommand.cs`

### Data, uniqueness, and error mapping

- Database already enforces per-user template-name uniqueness on active records:
  - `backend/src/JobNecto.Infrastructure/Persistance/Config/CoverLetterTemplateConfiguration.cs`
  - unique filtered index: `(UserId, Name)` with filter `"IsDeleted" = false`.
- Rely on DB constraint for race-safe uniqueness and map unique violations to `409`:
  - `backend/src/JobNecto.API/Infrastructure/ExceptionHandling/GlobalExceptionHandler.cs`
- Do not add "pre-check only" uniqueness logic as the sole enforcement mechanism.

### Ownership and not-found handling

- `GetByIdAsync` repository contract is non-nullable and throws `NotFoundException` when missing/soft-deleted.
- Handler must perform ownership check after load and throw `ForbiddenException` for cross-user update.
- Global exception handler already maps:
  - `ForbiddenException` -> `403`
  - `NotFoundException` -> `404`
  - unique-constraint `DbUpdateException` -> `409`

### Validation specifics

- `Name` when provided: not empty/whitespace, max 100.
- `Content` when provided: min 50, max 10000.
- Empty payload (`Name` and `Content` both null) must fail with `400`.
- Keep validation style aligned with existing update validators in:
  - `backend/src/JobNecto.Application/Educations/Validators/UpdateEducationCommandValidator.cs`
  - `backend/src/JobNecto.Application/Resumes/Validators/UpdateResumeCommandValidator.cs`

### Testing guidance

- Reuse helpers in `CoverLetterTemplatesApiTests` (`CreateUserAndGetCookieHelperAsync`, `PostTemplateAsync`) and add a PATCH helper.
- Keep test setup payloads validator-compliant (especially content length 50-10000).
- For uniqueness update races, extend existing Postgres integration suite:
  - `backend/tests/JobNecto.Tests/API/CoverLetterTemplates/CoverLetterTemplatesUniquenessApiTests.cs`
- Preserve existing test architecture split:
  - Application unit tests in `backend/tests/JobNecto.Tests/Application/CoverLetterTemplates`
  - API integration tests in `backend/tests/JobNecto.Tests/API/CoverLetterTemplates`

### Project structure notes

| File | Action |
| ---- | ------ |
| `backend/src/JobNecto.Application/CoverLetterTemplates/UpdateCoverLetterTemplateCommand.cs` | NEW |
| `backend/src/JobNecto.Application/CoverLetterTemplates/UpdateCoverLetterTemplateCommandHandler.cs` | NEW |
| `backend/src/JobNecto.Application/CoverLetterTemplates/Validators/UpdateCoverLetterTemplateCommandValidator.cs` | NEW |
| `backend/src/JobNecto.Application/CoverLetterTemplates/Mappers/CoverLetterTemplateMappers.cs` | UPDATE |
| `backend/src/JobNecto.API/Controllers/CoverLetterTemplatesController.cs` | UPDATE |
| `backend/tests/JobNecto.Tests/Application/CoverLetterTemplates/UpdateCoverLetterTemplateCommandValidatorTests.cs` | NEW |
| `backend/tests/JobNecto.Tests/Application/CoverLetterTemplates/UpdateCoverLetterTemplateCommandHandlerTests.cs` | NEW |
| `backend/tests/JobNecto.Tests/API/CoverLetterTemplates/CoverLetterTemplatesApiTests.cs` | UPDATE |
| `backend/tests/JobNecto.Tests/API/CoverLetterTemplates/CoverLetterTemplatesUniquenessApiTests.cs` | UPDATE |
| `_bmad-output/implementation-artifacts/sprint-status.yaml` | UPDATE |

### Namespace requirements

- `JobNecto.Application.CoverLetterTemplates`
- `JobNecto.Application.CoverLetterTemplates.Validators`
- `JobNecto.Application.CoverLetterTemplates.Mappers`
- `JobNecto.API.Controllers`
- `JobNecto.Tests.Application.CoverLetterTemplates`
- `JobNecto.Tests.API.CoverLetterTemplates`

### Previous story intelligence

- Story 3.2 established list/search patterns and API test helpers in `CoverLetterTemplatesApiTests` - extend instead of creating duplicate API test classes.
- Story 3.3 established strict auth guard pattern and detail semantics (`404` for cross-user reads) - keep that unchanged while adding `403` for mutation.
- Recent commits follow a stable vertical-slice sequence: controller + command/query handler + validators/mappers + tests + sprint/doc updates.

### Latest technical information

- No framework/library migration is required for this story.
- Continue with current stack and constraints (`net10.0`, MediatR, FluentValidation, EF Core + Npgsql, global exception mapping).

### References

- [Source: `_bmad-output/planning-artifacts/epics/epic-3-cover-letter-template-library.md` - Story 3.4]
- [Source: `_bmad-output/planning-artifacts/epics/requirements-inventory.md` - FR17/FR28]
- [Source: `_bmad-output/planning-artifacts/architecture/index.md`]
- [Source: `_bmad-output/planning-artifacts/architecture/project-context-analysis.md`]
- [Source: `_bmad-output/planning-artifacts/architecture/core-architectural-decisions.md`]
- [Source: `_bmad-output/planning-artifacts/architecture/epic-2-architecture-revision-2026-05-05.md`]
- [Source: `backend/src/JobNecto.API/Infrastructure/ExceptionHandling/GlobalExceptionHandler.cs`]
- [Source: `_bmad-output/implementation-artifacts/3-2-list-cover-letter-templates.md`]
- [Source: `_bmad-output/implementation-artifacts/3-3-get-cover-letter-template-detail.md`]

## Story Completion Status

- Story context drafted and validated against current architecture and test patterns.
- Sprint status updated to `done`.
- Completion note: comprehensive developer guide prepared for direct `dev-story` execution.

## Dev Agent Record

### Agent Model Used

GPT-5.3-Codex

### Debug Log References

- Implemented `UpdateCoverLetterTemplateCommand`, validator, and handler with partial update semantics and ownership `403` enforcement.
- Added `ApplyUpdates` mapper extension and authenticated `PATCH /api/v1/cover-letter-templates/{id}` endpoint with full response metadata.
- Added new application tests and API integration tests for PATCH success/error paths and per-user uniqueness update collisions.
- Executed verification gates successfully:
  - `dotnet test backend/JobNecto.slnx --filter "FullyQualifiedName~CoverLetterTemplates"` (64 passed)
  - `dotnet test backend/JobNecto.slnx` (356 passed)
  - `dotnet build backend/JobNecto.slnx --configuration Release --warnaserror` (passed)
  - `dotnet test backend/JobNecto.slnx --configuration Release --no-build --warnaserror` (356 passed)

### Completion Notes List

- Completed Story 3.4 implementation for cover letter template update flow with `PATCH` endpoint and partial payload (`name` and/or `content`).
- Implemented validation for required auth/route IDs, at-least-one-field rule, name bounds/whitespace checks, and content bounds (50-10000).
- Added conflict-safe update behavior backed by database uniqueness constraints and global `409` mapping.
- Added/updated tests across application and API layers, including PostgreSQL uniqueness collision scenarios for update operations.

### File List

- backend/src/JobNecto.Application/CoverLetterTemplates/UpdateCoverLetterTemplateCommand.cs
- backend/src/JobNecto.Application/CoverLetterTemplates/Validators/UpdateCoverLetterTemplateCommandValidator.cs
- backend/src/JobNecto.Application/CoverLetterTemplates/UpdateCoverLetterTemplateCommandHandler.cs
- backend/src/JobNecto.Application/CoverLetterTemplates/Mappers/CoverLetterTemplateMappers.cs
- backend/src/JobNecto.API/Controllers/CoverLetterTemplatesController.cs
- backend/tests/JobNecto.Tests/Application/CoverLetterTemplates/UpdateCoverLetterTemplateCommandValidatorTests.cs
- backend/tests/JobNecto.Tests/Application/CoverLetterTemplates/UpdateCoverLetterTemplateCommandHandlerTests.cs
- backend/tests/JobNecto.Tests/API/CoverLetterTemplates/CoverLetterTemplatesApiTests.cs
- backend/tests/JobNecto.Tests/API/CoverLetterTemplates/CoverLetterTemplatesUniquenessApiTests.cs
- _bmad-output/implementation-artifacts/3-4-update-cover-letter-template.md
- _bmad-output/implementation-artifacts/sprint-status.yaml

## Change Log

- 2026-05-10: Implemented Story 3.4 update flow (`PATCH /api/v1/cover-letter-templates/{id}`), added validation/handler/mapper changes, expanded unit+integration+PostgreSQL uniqueness tests, and passed all verification gates.
