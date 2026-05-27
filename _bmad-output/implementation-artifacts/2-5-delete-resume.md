# Story 2.5: Delete Resume

Status: review

## Story

As a job seeker,
I want to soft-delete a resume I no longer need,
so that it disappears from my list without permanent data loss.

## Acceptance Criteria

1. `DELETE /api/v1/resumes/{id}` requires a valid JWT token. Unauthenticated requests return `401 Unauthorized`.
2. Given a valid JWT token and a resume ID that belongs to the current user, returns `204 No Content`; `IsDeleted` is set to `true` and `DeletedAt` is set to now.
3. After soft-delete, `GET /api/v1/resumes` does not include the deleted resume.
4. After soft-delete, `GET /api/v1/resumes/{id}` returns `404 Not Found`.
5. If the resume does not exist, `DELETE /api/v1/resumes/{id}` returns `404 Not Found`.
6. If the resume belongs to a different user, `DELETE /api/v1/resumes/{id}` returns `403 Forbidden`.

## Tasks / Subtasks

- [x] Task 1: Define Application delete contract and validation (AC: 2, 5, 6)
  - [x] Create `DeleteResumeCommand.cs` in `backend/src/JobNecto.Application/Resumes/` with `ResumeId` and `UserId` set by route/auth context.
  - [x] Create `Validators/DeleteResumeCommandValidator.cs` with `ResumeId` and `UserId` non-empty checks.

- [x] Task 2: Implement handler ownership and soft-delete flow (AC: 2, 5, 6)
  - [x] Create `DeleteResumeCommandHandler.cs` in `backend/src/JobNecto.Application/Resumes/`.
  - [x] Load resume via `_unitOfWork.ResumeRepository.GetByIdAsync(request.ResumeId, cancellationToken)`.
  - [x] Enforce ownership: if `resume.UserId != request.UserId`, throw `ForbiddenException`.
  - [x] Set `resume.IsDeleted = true` and `resume.DeletedAt = DateTime.UtcNow`.
  - [x] Persist via `_unitOfWork.ResumeRepository.UpdateAsync(resume, cancellationToken)` and `_unitOfWork.SaveChangesAsync(cancellationToken)`.

- [x] Task 3: Expose HTTP endpoint (AC: 1, 2, 5, 6)
  - [x] Update `backend/src/JobNecto.API/Controllers/ResumesController.cs` with `[HttpDelete("{id:guid}")]` action.
  - [x] Add response contracts for `204`, `401`, `403`, and `404`.
  - [x] Extract `UserId` via `HttpContext.GetCurrentUserId()` and return `Unauthorized()` on parse failure.
  - [x] Dispatch command via MediatR and return `NoContent()`.

- [x] Task 4: Add handler unit tests (AC: 2, 5, 6)
  - [x] Create `backend/tests/JobNecto.Tests/Application/Resumes/DeleteResumeCommandHandlerTests.cs`.
  - [x] Verify owned delete sets soft-delete fields and persists once.
  - [x] Verify missing resume propagates `NotFoundException`.
  - [x] Verify cross-user delete throws `ForbiddenException` and does not persist.

- [x] Task 5: Extend API integration tests (AC: 1-6)
  - [x] Extend `backend/tests/JobNecto.Tests/API/ResumesControllerTests.cs` with:
    - [x] delete without token -> `401`
    - [x] owned delete -> `204`
    - [x] non-existent id -> `404`
    - [x] cross-user delete -> `403`
    - [x] list excludes deleted resume after delete
    - [x] detail returns `404` after delete

- [x] Task 6: Verification gates
  - [x] Run targeted tests for delete flow.
  - [x] Run full suite: `dotnet test backend/JobNecto.slnx`.
  - [x] Run CI parity build/test with Release + warn-as-error.

## Dev Notes

### Technical Requirements

- Keep Clean Architecture boundaries: API -> Application -> Domain.
- Reuse existing `IUnitOfWork` + `IEditableRepository<Resume>` abstractions.
- Do not add migration/schema changes for this story.
- Rely on global soft-delete query filter in `AppDbContext` for list/detail exclusion behavior.
- `GlobalExceptionHandler` already maps:
  - `NotFoundException` -> `404`
  - `ForbiddenException` -> `403`

### References

- `_bmad-output/archive/planning-artifacts/epics/epic-2-resume-education-management.md`
- `_bmad-output/planning-artifacts/epics/requirements-inventory.md`
- `backend/src/JobNecto.API/Controllers/ResumesController.cs`
- `backend/src/JobNecto.Application/Interfaces/IUnitOfWork.cs`
- `backend/src/JobNecto.Infrastructure/Persistance/AppDbContext.cs`

## Dev Agent Record

### Agent Model Used

GitHub Copilot (GPT-5.3-Codex)

### Debug Log References

- `dotnet test backend/JobNecto.slnx --filter "FullyQualifiedName~DeleteResume"` (green, 3/3)
- `runTests(mode=run)` (green, 223/223)
- `dotnet build backend/JobNecto.slnx --configuration Release --warnaserror` (green)
- `dotnet test backend/JobNecto.slnx --configuration Release --no-build --warnaserror *> $null; Write-Host "EXIT:$LASTEXITCODE"` (EXIT:0)

### Completion Notes List

- Added `DeleteResumeCommand` and `DeleteResumeCommandValidator` to define a validated delete contract.
- Added `DeleteResumeCommandHandler` with ownership enforcement (`ForbiddenException`) and soft-delete field mutation.
- Added `DELETE /api/v1/resumes/{id}` endpoint in `ResumesController` with `204/401/403/404` contract.
- Added `DeleteResumeCommandHandlerTests` to cover success, not found, and cross-user authorization behavior.
- Extended `ResumesControllerTests` with Story 2.5 integration coverage, including post-delete list/detail behavior.
- Verified targeted, full-suite, and Release warn-as-error CI-parity gates.

### File List

- `_bmad-output/implementation-artifacts/2-5-delete-resume.md`
- `backend/src/JobNecto.Application/Resumes/DeleteResumeCommand.cs`
- `backend/src/JobNecto.Application/Resumes/DeleteResumeCommandHandler.cs`
- `backend/src/JobNecto.Application/Resumes/Validators/DeleteResumeCommandValidator.cs`
- `backend/src/JobNecto.API/Controllers/ResumesController.cs`
- `backend/tests/JobNecto.Tests/Application/Resumes/DeleteResumeCommandHandlerTests.cs`
- `backend/tests/JobNecto.Tests/API/ResumesControllerTests.cs`

## Change Log

- 2026-05-01: Created Story 2.5 implementation artifact and task checklist.
- 2026-05-01: Implemented Story 2.5 endpoint/application flow, added unit/integration tests, and completed verification gates.

