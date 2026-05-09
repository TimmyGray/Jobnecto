# Story 3.5: Delete Cover Letter Template

Status: done

## Story

As a job seeker,
I want to delete a template I no longer need,
so that my library stays tidy.

## Acceptance Criteria

1. `DELETE /api/v1/cover-letter-templates/{id}` requires a valid JWT token. Unauthenticated requests return `401 Unauthorized`.
2. Given a valid JWT token and a template ID owned by the current user, `DELETE /api/v1/cover-letter-templates/{id}` returns `204 No Content`; soft-delete applied (`IsDeleted = true`, `DeletedAt` set).
3. After soft-delete, `GET /api/v1/cover-letter-templates` does not include the deleted template.
4. After soft-delete, `GET /api/v1/cover-letter-templates/{id}` returns `404 Not Found`.
5. If the template does not exist, `DELETE /api/v1/cover-letter-templates/{id}` returns `404 Not Found`.
6. If the template belongs to a different user, `DELETE /api/v1/cover-letter-templates/{id}` returns `403 Forbidden`.
7. Cover letters that reference this template via `templateId` are NOT deleted; their `templateId` reference remains for historical context.

## Tasks / Subtasks

- [x] Task 1: Create Application delete command (AC: 2, 5, 6)
  - [x] Add `backend/src/JobNecto.Application/CoverLetterTemplates/DeleteCoverLetterTemplateCommand.cs` implementing `IRequest<Unit>` with `CoverLetterTemplateId` and `UserId` (both `[JsonIgnore]`).

- [x] Task 2: Implement handler ownership and soft-delete flow (AC: 2, 5, 6)
  - [x] Add `backend/src/JobNecto.Application/CoverLetterTemplates/DeleteCoverLetterTemplateCommandHandler.cs` implementing `IRequestHandler<DeleteCoverLetterTemplateCommand, Unit>`.
  - [x] Load template via `_unitOfWork.CoverLetterTemplateRepository.GetByIdAsync(request.CoverLetterTemplateId, ct)`.
  - [x] Enforce ownership: if `template.UserId != request.UserId`, throw `ForbiddenException`.
  - [x] Soft-delete via `_unitOfWork.CoverLetterTemplateRepository.SoftDeleteAsync(template, ct)` and `SaveChangesAsync`.

- [x] Task 3: Add FluentValidation validator (AC: 2, 5)
  - [x] Add `backend/src/JobNecto.Application/CoverLetterTemplates/Validators/DeleteCoverLetterTemplateCommandValidator.cs`.
  - [x] Validate `CoverLetterTemplateId` and `UserId` are non-empty.

- [x] Task 4: Expose HTTP endpoint (AC: 1, 2, 5, 6)
  - [x] Update `backend/src/JobNecto.API/Controllers/CoverLetterTemplatesController.cs` with `[HttpDelete("{id:guid}")]` action.
  - [x] Add `ProducesResponseType` for 204, 401, 403, 404.
  - [x] Apply strict auth guard; dispatch command via MediatR; return `NoContent()`.

- [x] Task 5: Add handler unit tests (AC: 2, 5, 6)
  - [x] Create `backend/tests/JobNecto.Tests/Application/CoverLetterTemplates/DeleteCoverLetterTemplateCommandHandlerTests.cs`.
  - [x] Test: owned delete calls `SoftDeleteAsync` once, `UpdateAsync` never, `SaveChangesAsync` once.
  - [x] Test: missing template propagates `NotFoundException`.
  - [x] Test: cross-user delete throws `ForbiddenException` without persisting.

- [x] Task 6: Add API integration tests (AC: 1–6)
  - [x] Extend `backend/tests/JobNecto.Tests/API/CoverLetterTemplates/CoverLetterTemplatesApiTests.cs`.
  - [x] Add helper: `DeleteTemplateAsync(HttpClient client, string authCookie, Guid id)`.
  - [x] Add test: `Delete_WithoutToken_Returns401`.
  - [x] Add test: `Delete_OwnedTemplate_Returns204`.
  - [x] Add test: `Delete_NonExistentId_Returns404`.
  - [x] Add test: `Delete_AnotherUsersTemplate_Returns403`.
  - [x] Add test: `Delete_OwnedTemplate_ExcludedFromListAfterDeletion`.
  - [x] Add test: `Delete_OwnedTemplate_DetailReturns404AfterDeletion`.

- [x] Task 7: Verification gates
  - [x] Targeted tests: `dotnet test backend/JobNecto.slnx --filter "FullyQualifiedName~DeleteCoverLetterTemplate"` — 3/3 passed.
  - [x] Full CoverLetterTemplate suite: `dotnet test backend/JobNecto.slnx --filter "FullyQualifiedName~CoverLetterTemplate"` — 54/54 passed.
  - [x] Full suite: `dotnet test backend/JobNecto.slnx --configuration Release --no-build --warnaserror` — 343/343 passed.
  - [x] CI parity build: `dotnet build backend/JobNecto.slnx --configuration Release --warnaserror` — 0 warnings, 0 errors.

## Dev Notes

### Story Scope and Boundaries

- Soft-delete only; no schema change or migration required (`IsDeleted`/`DeletedAt` infrastructure was added in story 3.1).
- No cascade to `CoverLetter` entities: `CoverLetter.TemplateId` FK references remain valid at the DB level because the template row is not physically removed.
- No changes to list, detail, create, or update flows.
- No changes to `IUnitOfWork`, repository interfaces, or Infrastructure layer.
- Story 3.4 (Update) was not implemented; this story is independent of it.

### Ownership and Response Code Semantics

- Cross-user DELETE returns `403 Forbidden` (not `404`) per the delete-mutation pattern from story 2.5 and the epic-2-revision rule: "Use `403` for cross-user mutations."
- Cross-user GET detail returns `404` (existence hiding). The two endpoints have different semantics deliberately.
- `ForbiddenException` is mapped to 403 by `ExceptionHandlingMiddleware`.
- `NotFoundException` (from `GetByIdAsync` contract) is mapped to 404.

### SoftDeleteAsync Ownership

- `IsDeleted = true` and `DeletedAt = DateTime.UtcNow` are set inside `SoftDeletableRepository<T>.SoftDeleteAsync`, not in the handler.
- Unit test mocks simulate this via a `Callback` on `SoftDeleteAsync`.

### Integration Test Naming

- User login name prefixes kept ≤12 chars so `prefix + 8-hex-char suffix ≤ 20` (loginName max): `"clt_del_"` (8), `"clt_da_"` (7), `"clt_db_"` (7), `"clt_dl_"` (7), `"clt_dd_"` (7).

## Story Completion Status

- Implementation complete. All 7 tasks done, 343 tests passing, CI parity clean. Status: done.

## Change Log

- 2026-05-10: Implemented story 3.5 — DELETE /api/v1/cover-letter-templates/{id}. Added command/handler/validator, controller action, 3 unit tests, 6 API integration tests.

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Completion Notes List

- Task 1: Created `DeleteCoverLetterTemplateCommand` with `[JsonIgnore]` on both properties (route + auth context, not from request body).
- Task 2: Created `DeleteCoverLetterTemplateCommandHandler` following `DeleteResumeCommandHandler` pattern exactly. `ForbiddenException` for cross-user (403); `NotFoundException` propagated from repository for missing template (404).
- Task 3: Created `DeleteCoverLetterTemplateCommandValidator` with non-empty checks on `CoverLetterTemplateId` and `UserId`.
- Task 4: Added `[HttpDelete("{id:guid}")]` action to `CoverLetterTemplatesController` with 204/401/403/404 `ProducesResponseType` attributes, strict auth guard, and `NoContent()` return.
- Task 5: Created 3 unit tests covering owned-delete success (SoftDeleteAsync once, SaveChangesAsync once, UpdateAsync never), NotFoundException propagation, and ForbiddenException on cross-user.
- Task 6: Added `DeleteTemplateAsync` helper and 6 integration tests covering all AC paths.
- Task 7: All verification gates green. Test count went from 334 to 343 (+9).

### File List

- `backend/src/JobNecto.Application/CoverLetterTemplates/DeleteCoverLetterTemplateCommand.cs` (new)
- `backend/src/JobNecto.Application/CoverLetterTemplates/DeleteCoverLetterTemplateCommandHandler.cs` (new)
- `backend/src/JobNecto.Application/CoverLetterTemplates/Validators/DeleteCoverLetterTemplateCommandValidator.cs` (new)
- `backend/src/JobNecto.API/Controllers/CoverLetterTemplatesController.cs` (modified)
- `backend/tests/JobNecto.Tests/Application/CoverLetterTemplates/DeleteCoverLetterTemplateCommandHandlerTests.cs` (new)
- `backend/tests/JobNecto.Tests/API/CoverLetterTemplates/CoverLetterTemplatesApiTests.cs` (modified)
- `_bmad-output/implementation-artifacts/3-5-delete-cover-letter-template.md` (new)
