# Story 5.4: Update Cover Letter Content

Status: ready-for-dev

## Story

As a job seeker,
I want to edit the content of an existing cover letter,
so that I can refine my application before submitting.

## Acceptance Criteria

1. Given a valid JWT token and `PATCH /api/v1/cover-letters/{id}` with new `content`, when the request is processed, then `200 OK` with updated cover letter; `updatedAt` refreshed.
2. Given `vacancyId` is included in the PATCH body, when the request is processed, then it is silently ignored — `vacancyId` is immutable after creation.
3. Given updated `content` violates 50–10000 char bounds, then `400 Bad Request` with field-level error on `content`.
4. Given the cover letter belongs to another user, then `403 Forbidden`.
5. Given the cover letter does not exist or is soft-deleted, then `404 Not Found`.
6. Given no JWT token, when `PATCH /api/v1/cover-letters/{id}` is called, then `401 Unauthorized`.

## Tasks / Subtasks

- [ ] Task 1: Create command, DTO, and handler (AC: 1, 2, 4, 5)
  - [ ] Create `backend/src/JobNecto.Application/CoverLetters/UpdateCoverLetterCommand.cs` with `UpdateCoverLetterCommand` (`IRequest<CoverLetterUpdateResult>`) and `CoverLetterUpdateResult` DTO. Fields on command: `CoverLetterId` (set from route, JsonIgnore on route), `UserId` (JsonIgnore), `Content`.
  - [ ] **Do NOT include `VacancyId`** in the command — omitting it entirely enforces immutability without needing to silently ignore it at the model-binding layer.
  - [ ] Create `backend/src/JobNecto.Application/CoverLetters/UpdateCoverLetterCommandHandler.cs`: get cover letter via `GetByIdAsync`; if `coverLetter.UserId != request.UserId` → throw `ForbiddenException`; update `Content`; call `UpdateAsync`; call `SaveChangesAsync`; return updated DTO.
  - [ ] Set `UpdatedAt = DateTime.UtcNow` explicitly in the handler before calling `UpdateAsync` (per agent-learnings: do not rely on DB defaults).
  - [ ] Add `ToEntity` / `ToUpdateResult` mapper extensions in `backend/src/JobNecto.Application/CoverLetters/Mappers/CoverLetterMappers.cs`.

- [ ] Task 2: Create validator (AC: 3)
  - [ ] Create `backend/src/JobNecto.Application/CoverLetters/Validators/UpdateCoverLetterCommandValidator.cs`.
  - [ ] Rule: `Content` length 50–10000, `NotEmpty()`, not whitespace-only.

- [ ] Task 3: Add API endpoint (AC: 6)
  - [ ] Add `[HttpPatch("{id:guid}")]` action to `backend/src/JobNecto.API/Controllers/CoverLettersController.cs`.
  - [ ] Bind `id` from route → `command.CoverLetterId = id`; inject `UserId` from JWT → `command.UserId = userId`.
  - [ ] `ProducesResponseType` for 200, 400, 401, 403, 404.

- [ ] Task 4: Add tests (AC: 1–6)
  - [ ] Create `backend/tests/JobNecto.Tests/Application/CoverLetters/UpdateCoverLetterCommandHandlerTests.cs` — mock `IUnitOfWork`; test: valid update → success with refreshed updatedAt, other-user → ForbiddenException, not found → NotFoundException.
  - [ ] Create `backend/tests/JobNecto.Tests/Application/CoverLetters/UpdateCoverLetterCommandValidatorTests.cs` — test content boundaries (49, 50, 10000, 10001 chars), empty/whitespace content.
  - [ ] In `backend/tests/JobNecto.Tests/API/CoverLetters/CoverLettersApiTests.cs` — add: no token → 401; valid PATCH → 200 with updated content and refreshed `updatedAt`; content too short → 400; other-user ID → 403; non-existent ID → 404.

- [ ] Task 5: Verification gates
  - [ ] Build: `dotnet build backend/JobNecto.slnx --configuration Release --warnaserror` — 0 warnings, 0 errors.
  - [ ] Tests: `dotnet test backend/JobNecto.slnx` — all pass.

## Dev Notes

### VacancyId Immutability

AC 2 says `vacancyId` in the PATCH body is "silently ignored." The cleanest way to enforce this is to **not include `VacancyId` in `UpdateCoverLetterCommand`** at all. If `vacancyId` appears in the JSON body, the model binder simply ignores unknown properties (default ASP.NET Core behavior). No explicit handling needed.

Do NOT add a `VacancyId` field and then blank it — that invites accidental mutation bugs.

### 403 vs 404 Ownership Semantics

Per AC 4 and Decision 7: cover letter update returns **`403 Forbidden`** (not `404`) for cross-user access. This differs from read endpoints (which return `404` to prevent existence leakage). Update/delete mutations explicitly acknowledge existence but deny modification — consistent with `UpdateResumeCommandHandler` and `UpdateEducationCommandHandler`.

Pattern:

```csharp
var coverLetter = await _unitOfWork.CoverLetterRepository.GetByIdAsync(request.CoverLetterId, ct);
// GetByIdAsync throws NotFoundException if not found (AC 5 covered automatically)

if (coverLetter.UserId != request.UserId)
    throw new ForbiddenException("You do not have permission to update this cover letter.");

coverLetter.Content = request.Content;
coverLetter.UpdatedAt = DateTime.UtcNow;

await _unitOfWork.CoverLetterRepository.UpdateAsync(coverLetter, ct);
await _unitOfWork.SaveChangesAsync(ct);
```

### `CoverLetterUpdateResult` DTO

Return the full cover letter state (matches the AC "200 OK with updated cover letter"):

```csharp
public class CoverLetterUpdateResult
{
    public Guid Id { get; set; }
    public Guid VacancyId { get; set; }
    public string Content { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

### `UpdateAsync` in Repository

`IMutableRepository<CoverLetter>.UpdateAsync` is inherited from `EditableRepository<CoverLetter>`. Read the implementation to confirm it tracks the entity and calls `SaveChanges` or relies on the UoW — do not call it if the entity is already tracked by EF (depends on implementation; check `EditableRepository.cs`).

### Files to Read Before Implementation

- `backend/src/JobNecto.Application/CoverLetterTemplates/UpdateCoverLetterTemplateCommandHandler.cs` (403 pattern)
- `backend/src/JobNecto.Infrastructure/Repositories/EditableRepository.cs` (`UpdateAsync` implementation)
- `backend/src/JobNecto.Application/CoverLetters/Mappers/CoverLetterMappers.cs` (created in Story 5.1 — add new mapper here)
- `backend/src/JobNecto.Application/Exceptions/ForbiddenException.cs`
- `backend/tests/JobNecto.Tests/Application/CoverLetterTemplates/UpdateCoverLetterTemplateCommandHandlerTests.cs` (test pattern)

### Project Structure Notes

- New files: `UpdateCoverLetterCommand.cs`, `UpdateCoverLetterCommandHandler.cs`, `Validators/UpdateCoverLetterCommandValidator.cs`.
- All under `backend/src/JobNecto.Application/CoverLetters/` and sub-folders.
- Namespace: `JobNecto.Application.CoverLetters`, `JobNecto.Application.CoverLetters.Validators`.

### References

- [Source: `_bmad-output/planning-artifacts/epics/epic-5-cover-letter-application-management.md` — Story 5.4]
- [Source: `_bmad-output/planning-artifacts/epics/requirements-inventory.md` — FR22, NFR2]
- [Source: `_bmad-output/planning-artifacts/architecture/core-architectural-decisions.md` — Decision 2, 6, 7]
- [Source: `_bmad-output/agent-learnings.md` — timestamp in handlers, 403 vs 404 semantics]

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Completion Notes List

- VacancyId immutability enforced by omitting the field from the command (not by ignoring it).
- 403 Forbidden for cross-user update (not 404) — consistent with Update/Delete handlers for Resume/Education.
- `UpdatedAt` must be set explicitly in handler (per agent-learnings: DB defaults not reliable in-memory tests).

### File List

- `_bmad-output/implementation-artifacts/5-4-update-cover-letter-content.md` (this file)
