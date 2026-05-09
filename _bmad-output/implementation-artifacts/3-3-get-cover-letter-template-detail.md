# Story 3.3: Get Cover Letter Template Detail

Status: done

## Story

As a job seeker,
I want to view the full content of a specific template,
so that I can read or copy it when composing a cover letter.

## Acceptance Criteria

1. `GET /api/v1/cover-letter-templates/{id}` requires a valid JWT token. Unauthenticated requests return `401 Unauthorized`.
2. Given a valid JWT token and a template ID owned by the current user, `GET /api/v1/cover-letter-templates/{id}` returns `200 OK` with all fields including full `content`.
3. Given the template does not exist, is soft-deleted, or belongs to another user, `GET /api/v1/cover-letter-templates/{id}` returns `404 Not Found`.

## Tasks / Subtasks

- [x] Task 1: Create Application query and handler for template detail (AC: 2, 3)
  - [x] Add `backend/src/JobNecto.Application/CoverLetterTemplates/GetCoverLetterTemplateQuery.cs` implementing `IRequest<CoverLetterTemplateResult>` with `CoverLetterTemplateId` and `UserId`.
  - [x] Add `backend/src/JobNecto.Application/CoverLetterTemplates/GetCoverLetterTemplateQueryHandler.cs` implementing `IRequestHandler<GetCoverLetterTemplateQuery, CoverLetterTemplateResult>`.
  - [x] In handler, call `_unitOfWork.CoverLetterTemplateRepository.GetByIdAsync(request.CoverLetterTemplateId, cancellationToken)`.
  - [x] If `template.UserId != request.UserId`, throw `NotFoundException("CoverLetterTemplate", request.CoverLetterTemplateId)` to avoid existence leakage.
  - [x] Return `template.ToCoverLetterTemplateResult()` (reuse existing mapper; no new DTO).

- [x] Task 2: Add authenticated detail endpoint to `CoverLetterTemplatesController` (AC: 1, 2, 3)
  - [x] Update `backend/src/JobNecto.API/Controllers/CoverLetterTemplatesController.cs` with `[HttpGet("{id:guid}")]` action.
  - [x] Add `[ProducesResponseType(typeof(CoverLetterTemplateResult), StatusCodes.Status200OK)]`, `[ProducesResponseType(StatusCodes.Status401Unauthorized)]`, and `[ProducesResponseType(StatusCodes.Status404NotFound)]`.
  - [x] Use strict auth guard: `string.IsNullOrWhiteSpace(userIdValue) || !Guid.TryParse(userIdValue, out var userId)`.
  - [x] Send `new GetCoverLetterTemplateQuery { CoverLetterTemplateId = id, UserId = userId }` via `_mediator.Send(...)` and return `Ok(result)`.

- [x] Task 3: Add unit tests for query handler (AC: 2, 3)
  - [x] Create `backend/tests/JobNecto.Tests/Application/CoverLetterTemplates/GetCoverLetterTemplateQueryHandlerTests.cs`.
  - [x] Test owned template returns full `CoverLetterTemplateResult`.
  - [x] Test repository `NotFoundException` is propagated for non-existent ID.
  - [x] Test cross-user access throws `NotFoundException` (404 semantics for detail read).

- [x] Task 4: Add API integration tests for detail endpoint (AC: 1, 2, 3)
  - [x] Update `backend/tests/JobNecto.Tests/API/CoverLetterTemplates/CoverLetterTemplatesApiTests.cs`.
  - [x] Add helper: `GetTemplateByIdAsync(HttpClient client, string authCookie, Guid id)`.
  - [x] Add test: `GetById_WithoutToken_Returns401`.
  - [x] Add test: `GetById_OwnedTemplate_Returns200WithFullContent`.
  - [x] Add test: `GetById_NonExistentId_Returns404`.
  - [x] Add test: `GetById_AnotherUsersTemplate_Returns404`.
  - [x] Add test: `GetById_SoftDeletedTemplate_Returns404` by soft-deleting directly through `AppDbContext` with `IgnoreQueryFilters()`.

- [x] Task 5: Verification gates (AC: 1, 2, 3)
  - [x] Run targeted tests: `dotnet test backend/JobNecto.slnx --filter "FullyQualifiedName~CoverLetterTemplates"`.
  - [x] Run full suite: `dotnet test backend/JobNecto.slnx`.
  - [x] Run CI parity: `dotnet build backend/JobNecto.slnx --configuration Release --warnaserror`.
  - [x] Run CI parity tests: `dotnet test backend/JobNecto.slnx --configuration Release --no-build --warnaserror`.

## Dev Notes

### Story Scope and Boundaries

- This story is read-only detail retrieval for cover letter templates.
- No schema change and no new migration are required.
- Do not modify pagination/search infrastructure introduced in story 3.2 (`PagedQuery.Search`, `BaseRepository.ApplyAdditionalFilters`).

### Existing Cover Letter Template Baseline

- `CoverLetterTemplatesController` currently exposes `POST` and list `GET`; this story adds detail `GET {id}`.
- `CoverLetterTemplateResult` already exists in `backend/src/JobNecto.Application/CoverLetterTemplates/CreateCoverLetterTemplateCommand.cs` and includes full `content`.
- Mapper `ToCoverLetterTemplateResult()` already exists in `backend/src/JobNecto.Application/CoverLetterTemplates/Mappers/CoverLetterTemplateMappers.cs`.

### Ownership and NotFound Semantics

- Preserve Epic 2/3 rule: cross-user detail reads return `404`, not `403`.
- Use the same pattern as `GetResumeQueryHandler` and `GetEducationQueryHandler`:
  - fetch by ID,
  - verify ownership,
  - throw `NotFoundException` when ownership mismatches.
- Do not leak whether another user's template exists.

### Repository and Soft-Delete Behavior

- `GetByIdAsync` contract is non-nullable and throws `NotFoundException` when record is missing.
- EF Core global query filters already exclude soft-deleted `CoverLetterTemplate` entities; soft-deleted detail reads naturally return `404` through the same code path.
- Do not add null checks after `GetByIdAsync`; rely on exception contract.

### Entity and Mapping Gotchas

- `CoverLetterTemplate` uses public fields for domain data (`UserId`, `Name`, `Content`), not auto-properties.
- Mapping for detail response is already centralized in `ToCoverLetterTemplateResult()`; avoid duplicate mapping logic.

### API Controller Guardrail

- Match strict auth guard used in `EducationsController` and current `CoverLetterTemplatesController` actions:

```csharp
var userIdValue = HttpContext.GetCurrentUserId();
if (string.IsNullOrWhiteSpace(userIdValue) || !Guid.TryParse(userIdValue, out var userId))
    return Unauthorized();
```

### Testing Guidance

- Reuse existing helpers in `CoverLetterTemplatesApiTests` for user creation and template creation.
- Use validator-compliant setup data (`name` non-empty <= 100, `content` between 50 and 10000 chars).
- For soft-delete detail test, set `IsDeleted = true` and `DeletedAt = DateTime.UtcNow` directly via test `AppDbContext` scope.

### Project Structure Notes

- Keep namespaces aligned with folders:
  - `JobNecto.Application.CoverLetterTemplates`
  - `JobNecto.API.Controllers`
  - `JobNecto.Tests.Application.CoverLetterTemplates`
  - `JobNecto.Tests.API.CoverLetterTemplates`
- Follow existing vertical-slice layout under `CoverLetterTemplates`.
- Avoid touching unrelated modules (Resumes/Educations/Vacancies).

### Previous Story Intelligence

- Story 3.1 established `CoverLetterTemplateResult` and template create flow; reuse existing DTO/mapper instead of creating parallel detail DTOs.
- Story 3.2 established list/search flow and API test infrastructure; extend existing `CoverLetterTemplatesApiTests` class rather than creating a new API test class.
- Existing learnings emphasize strict auth parsing, validator-compliant test data, and non-null `GetByIdAsync` exception semantics.

### Git Intelligence Summary

- Recent implementation commit `37d8a94` (story 3.2) followed this file pattern:
  - Controller update,
  - Application query/handler,
  - Mapper extension,
  - API + application tests,
  - Story artifact + sprint status update.
- For story 3.3, repository and pagination layers should remain unchanged; add only detail-read slice files and tests.

### Latest Technical Information

- No additional library upgrade or API migration is required for this story.
- Continue with current stack and contracts already used in the codebase (`net10.0`, MediatR, FluentValidation, EF Core global filters).

### References

- [Source: `_bmad-output/planning-artifacts/epics/epic-3-cover-letter-template-library.md` - Story 3.3 acceptance criteria]
- [Source: `_bmad-output/planning-artifacts/architecture/epic-2-architecture-revision-2026-05-05.md` - ownership and response semantics]
- [Source: `_bmad-output/planning-artifacts/architecture/core-architectural-decisions.md` - `GetByIdAsync` contract, soft-delete filters, ownership patterns]
- [Source: `_bmad-output/planning-artifacts/architecture/project-context-analysis.md` - cross-cutting constraints]
- [Source: `_bmad-output/implementation-artifacts/3-1-create-cover-letter-template.md` - template entity/mapping/test guardrails]
- [Source: `_bmad-output/implementation-artifacts/3-2-list-cover-letter-templates.md` - cover-letter test and controller extension patterns]
- [Source: `backend/src/JobNecto.API/Controllers/CoverLetterTemplatesController.cs`]
- [Source: `backend/src/JobNecto.Application/CoverLetterTemplates/Mappers/CoverLetterTemplateMappers.cs`]
- [Source: `backend/src/JobNecto.Application/Resumes/GetResumeQueryHandler.cs`]
- [Source: `backend/src/JobNecto.Application/Educations/GetEducationQueryHandler.cs`]
- [Source: `backend/tests/JobNecto.Tests/API/CoverLetterTemplates/CoverLetterTemplatesApiTests.cs`]

### Review Findings

- [x] \[Review/Defer\] `CoverLetterTemplateResult` exposes `UserId` in the response body `backend/src/JobNecto.Application/CoverLetterTemplates/Mappers/CoverLetterTemplateMappers.cs` — deferred, pre-existing design from Story 3.1

## Story Completion Status

- Implementation complete. All 5 tasks done, 334 tests passing, CI parity clean. Code review passed: 0 decision-needed, 0 patch, 1 deferred, 10 dismissed. Status: done.

## Change Log

- 2026-05-09: Implemented story 3.3 — GET /api/v1/cover-letter-templates/{id}. Added query/handler, controller action, 3 unit tests, 5 API integration tests.

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

### Completion Notes List

- Task 1: Created `GetCoverLetterTemplateQuery` and `GetCoverLetterTemplateQueryHandler` following the exact pattern of `GetResumeQueryHandler`. Ownership check throws `NotFoundException` (not `ForbiddenException`) to prevent existence leakage. Reuses `ToCoverLetterTemplateResult()` mapper from story 3.1.
- Task 2: Added `[HttpGet("{id:guid}")]` action to `CoverLetterTemplatesController` with strict auth guard, `ProducesResponseType` for 200/401/404, and MediatR dispatch.
- Task 3: Created 3 unit tests covering owned-template success, non-existent ID propagation, and cross-user 404 semantics.
- Task 4: Added `GetTemplateByIdAsync` helper and 5 integration tests covering 401 (no token), 200 (owned, full content returned), 404 (non-existent), 404 (another user), and 404 (soft-deleted via direct DbContext).
- Task 5: All 334 tests pass in Debug and Release configurations. Zero warnings.

### File List

- backend/src/JobNecto.Application/CoverLetterTemplates/GetCoverLetterTemplateQuery.cs (new)
- backend/src/JobNecto.Application/CoverLetterTemplates/GetCoverLetterTemplateQueryHandler.cs (new)
- backend/src/JobNecto.API/Controllers/CoverLetterTemplatesController.cs (modified)
- backend/tests/JobNecto.Tests/Application/CoverLetterTemplates/GetCoverLetterTemplateQueryHandlerTests.cs (new)
- backend/tests/JobNecto.Tests/API/CoverLetterTemplates/CoverLetterTemplatesApiTests.cs (modified)
