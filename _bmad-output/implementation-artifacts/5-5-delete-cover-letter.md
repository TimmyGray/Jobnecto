# Story 5.5: Delete Cover Letter

Status: done

## Story

As a job seeker,
I want to soft-delete a cover letter I no longer need,
so that my application history stays organized without permanent loss.

## Acceptance Criteria

1. Given a valid JWT token and a cover letter ID owned by the current user, when `DELETE /api/v1/cover-letters/{id}` is called, then `204 No Content`; soft-delete applied.
2. Given `GET /api/v1/cover-letters` or `GET /api/v1/cover-letters/{id}` is called after deletion, then the cover letter is no longer visible.
3. Given the vacancy this cover letter referenced is later deleted, when `GET /api/v1/cover-letters/{id}` is called for the cover letter before its own deletion, then the cover letter is still returned with `vacancyId` preserved.
4. Given the cover letter belongs to another user, when `DELETE /api/v1/cover-letters/{id}` is called, then `403 Forbidden`.
5. Given the cover letter does not exist or is soft-deleted, when `DELETE /api/v1/cover-letters/{id}` is called, then `404 Not Found`.
6. Given no JWT token, when `DELETE /api/v1/cover-letters/{id}` is called, then `401 Unauthorized`.

## Tasks / Subtasks

- [x] Task 1: Create command and handler (AC: 1, 2, 4, 5)
  - [x] Create `backend/src/JobNecto.Application/CoverLetters/DeleteCoverLetterCommand.cs` with `DeleteCoverLetterCommand` (`IRequest<Unit>`), `CoverLetterId`, `UserId` (JsonIgnore).
  - [x] Create `backend/src/JobNecto.Application/CoverLetters/DeleteCoverLetterCommandHandler.cs`: get cover letter via `GetByIdAsync`; if `coverLetter.UserId != request.UserId` → throw `ForbiddenException`; call `SoftDeleteAsync`; call `SaveChangesAsync`; return `Unit.Value`.
  - [x] No DTO needed — returns `Unit`.

- [x] Task 2: Create validator (minimal, AC: 1)
  - [x] Create `backend/src/JobNecto.Application/CoverLetters/Validators/DeleteCoverLetterCommandValidator.cs` — validate `CoverLetterId` is not empty, `UserId` is not empty.

- [x] Task 3: Add API endpoint (AC: 6)
  - [x] Add `[HttpDelete("{id:guid}")]` action to `backend/src/JobNecto.API/Controllers/CoverLettersController.cs`.
  - [x] Extract `UserId`, dispatch `DeleteCoverLetterCommand { CoverLetterId = id, UserId = userId }`, return `NoContent()`.
  - [x] `ProducesResponseType` for 204, 401, 403, 404.

- [x] Task 4: Add tests (AC: 1–6)
  - [x] Create `backend/tests/JobNecto.Tests/Application/CoverLetters/DeleteCoverLetterCommandHandlerTests.cs` — mock `IUnitOfWork`; test: valid delete → `SoftDeleteAsync` called + `SaveChangesAsync` called, other-user → ForbiddenException, not found → NotFoundException.
  - [x] In `backend/tests/JobNecto.Tests/API/CoverLetters/CoverLettersApiTests.cs` — add: no token → 401; valid DELETE → 204; deleted letter no longer appears in GET list; deleted letter returns 404 on GET detail; other-user ID → 403.

- [x] Task 5: Verification gates
  - [x] Build: `dotnet build backend/JobNecto.slnx --configuration Release --warnaserror` — 0 warnings, 0 errors.
  - [x] Tests: `dotnet test backend/JobNecto.slnx` — all pass.
  - [x] Run full CI parity: `dotnet test backend/JobNecto.slnx --configuration Release --warnaserror`.

## Dev Notes

### Soft Delete via `SoftDeleteAsync`

Per Decision 6 and story R.1: use `IMutableRepository<CoverLetter>.SoftDeleteAsync(entity, ct)`. The flag-setting (`IsDeleted = true`, `DeletedAt = DateTime.UtcNow`) is handled inside `SoftDeletableRepository<T>`. The handler only calls `SoftDeleteAsync` and then `SaveChangesAsync`.

```csharp
public async Task<Unit> Handle(DeleteCoverLetterCommand request, CancellationToken ct)
{
    var coverLetter = await _unitOfWork.CoverLetterRepository.GetByIdAsync(request.CoverLetterId, ct);

    if (coverLetter.UserId != request.UserId)
        throw new ForbiddenException("You do not have permission to delete this cover letter.");

    await _unitOfWork.CoverLetterRepository.SoftDeleteAsync(coverLetter, ct);
    await _unitOfWork.SaveChangesAsync(ct);

    return Unit.Value;
}
```

### 403 vs 404

Same semantics as Story 5.4: DELETE returns **`403 Forbidden`** for cross-user (not 404). Consistent with `DeleteResumeCommandHandler` and `DeleteCoverLetterTemplateCommandHandler`.

### AC 3: Vacancy Deletion Edge Case

AC 3 ("vacancy is later deleted → cover letter still returns with vacancyId") is **informational context** only. The implementation doesn't require special code here because:
- If the vacancy is **soft-deleted**: the cover letter still exists; `GetDetailByIdAsync` uses `IgnoreQueryFilters()` on the vacancy side (implemented in Story 5.3), so the cover letter detail is still returned.
- If the vacancy is **hard-deleted**: `CoverLetterConfiguration` has `ON DELETE CASCADE` on `VacancyId` FK — the cover letter is also hard-deleted. This means AC 3 (before its own deletion) is satisfied automatically since the cover letter would already be gone.

No special implementation needed for this AC — it is covered by the Story 5.3 `IgnoreQueryFilters()` design. Add a comment in the handler or a test note explaining this.

### EF Global Query Filter

The `CoverLetter` entity has `HasQueryFilter(cl => !cl.IsDeleted)` in `AppDbContext`. After soft-delete, `GetByIdAsync` will throw `NotFoundException` (filtered out) — so AC 2 (letter no longer visible after delete) is automatically satisfied by the global filter.

### Existing Pattern Reference

Follow `DeleteCoverLetterTemplateCommandHandler.cs` exactly for the handler structure.

### Files to Read Before Implementation

- `backend/src/JobNecto.Application/CoverLetterTemplates/DeleteCoverLetterTemplateCommandHandler.cs`
- `backend/src/JobNecto.Infrastructure/Repositories/SoftDeletableRepository.cs` (`SoftDeleteAsync` implementation)
- `backend/src/JobNecto.Application/Exceptions/ForbiddenException.cs`
- `backend/tests/JobNecto.Tests/Application/CoverLetterTemplates/DeleteCoverLetterTemplateCommandHandlerTests.cs` (test pattern)

### Project Structure Notes

- New files: `DeleteCoverLetterCommand.cs`, `DeleteCoverLetterCommandHandler.cs`, `Validators/DeleteCoverLetterCommandValidator.cs`.
- All under `backend/src/JobNecto.Application/CoverLetters/` and sub-folders.
- Namespace: `JobNecto.Application.CoverLetters`, `JobNecto.Application.CoverLetters.Validators`.

### References

- [Source: `_bmad-output/planning-artifacts/epics/epic-5-cover-letter-application-management.md` — Story 5.5]
- [Source: `_bmad-output/planning-artifacts/epics/requirements-inventory.md` — FR23, NFR5]
- [Source: `_bmad-output/planning-artifacts/architecture/core-architectural-decisions.md` — Decision 6, 7]

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Completion Notes List

- AC 3 (vacancy deleted edge case) requires no special implementation — covered by `IgnoreQueryFilters()` in Story 5.3 and cascade delete behavior.
- 403 for cross-user delete (not 404) — consistent with all mutation handlers.
- `SoftDeleteAsync` sets `IsDeleted` and `DeletedAt` internally — handler does not set them directly.

### File List

- `backend/src/JobNecto.Application/CoverLetters/DeleteCoverLetterCommand.cs` (CREATED)
- `backend/src/JobNecto.Application/CoverLetters/DeleteCoverLetterCommandHandler.cs` (CREATED)
- `backend/src/JobNecto.Application/CoverLetters/Validators/DeleteCoverLetterCommandValidator.cs` (CREATED)
- `backend/tests/JobNecto.Tests/Application/CoverLetters/DeleteCoverLetterCommandHandlerTests.cs` (CREATED)
- `backend/tests/JobNecto.Tests/Application/CoverLetters/DeleteCoverLetterCommandValidatorTests.cs` (CREATED)
- `backend/src/JobNecto.API/Controllers/CoverLettersController.cs` (UPDATED — DELETE /{id} endpoint added)
- `_bmad-output/implementation-artifacts/5-5-delete-cover-letter.md` (this file)
