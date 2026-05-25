# Story 5.3: Get Cover Letter Detail

Status: done

## Story

As a job seeker,
I want to view a cover letter's full content and associated vacancy,
so that I can review or edit what I've written.

## Acceptance Criteria

1. Given a valid JWT token and a cover letter ID owned by the current user, when `GET /api/v1/cover-letters/{id}` is called, then `200 OK` with all fields: `id`, `content`, `vacancyId`, `createdAt`, `updatedAt`, plus nested `vacancy` object with key fields (`id`, `title`, `company`, `workLocationType`, `location`).
2. Given the cover letter does not exist, is soft-deleted, or belongs to another user, then `404 Not Found`.
3. Given no JWT token, when `GET /api/v1/cover-letters/{id}` is called, then `401 Unauthorized`.

## Tasks / Subtasks

- [x] Task 1: Add specialized detail method to `ICoverLetterRepository` (AC: 1, 2)
  - [x] Add `GetDetailByIdAsync(Guid id, CancellationToken ct)` to `backend/src/JobNecto.Application/Interfaces/ICoverLetterRepository.cs` returning `CoverLetterDetailResult?`.
  - [x] Define `CoverLetterDetailResult` and nested `VacancyInCoverLetterResult` DTOs in `backend/src/JobNecto.Application/CoverLetters/GetCoverLetterQuery.cs` (or a dedicated DTOs file in the same folder).
  - [x] Implement in `CoverLetterRepository`: JOIN cover letter with vacancy; return null if not found (let the handler/query handler throw `NotFoundException`).

- [x] Task 2: Create query and handler (AC: 1, 2)
  - [x] Create `backend/src/JobNecto.Application/CoverLetters/GetCoverLetterQuery.cs` with `GetCoverLetterQuery` (`IRequest<CoverLetterDetailResult>`), `CoverLetterId`, `UserId` (JsonIgnore).
  - [x] Create `backend/src/JobNecto.Application/CoverLetters/GetCoverLetterQueryHandler.cs`: call `ICoverLetterRepository.GetDetailByIdAsync`; if null → throw `NotFoundException`; if `result.UserId != request.UserId` → throw `NotFoundException` (existence leakage prevention per Decision 7); return result.

- [x] Task 3: Add API endpoint (AC: 3)
  - [x] Add `[HttpGet("{id:guid}")]` action to `backend/src/JobNecto.API/Controllers/CoverLettersController.cs`.
  - [x] Extract `UserId`, dispatch `GetCoverLetterQuery`, return `Ok(result)`.
  - [x] `ProducesResponseType` for 200, 401, 404.

- [x] Task 4: Add tests (AC: 1–3)
  - [x] Create `backend/tests/JobNecto.Tests/Application/CoverLetters/GetCoverLetterQueryHandlerTests.cs` — mock `ICoverLetterRepository`; test: owned letter → returns detail with nested vacancy, not found → NotFoundException, other-user letter → NotFoundException.
  - [x] In `backend/tests/JobNecto.Tests/API/CoverLetters/CoverLettersApiTests.cs` — add: no token → 401; valid GET → 200 with full nested vacancy; non-existent ID → 404; other-user ID → 404.

- [x] Task 5: Verification gates
  - [x] Build: `dotnet build backend/JobNecto.slnx --configuration Release --warnaserror` — 0 warnings, 0 errors.
  - [x] Tests: `dotnet test backend/JobNecto.slnx` — all pass.

## Dev Notes

### Prerequisite: Story 5.2 Must Be Complete

This story extends `ICoverLetterRepository` introduced in Story 5.2. Ensure `ICoverLetterRepository`, `UnitOfWork.CoverLetterRepository` type change, and `CoverLetterRepository` base implementation are all in place.

### `GetDetailByIdAsync` Implementation Sketch

```csharp
public async Task<CoverLetterDetailResult?> GetDetailByIdAsync(Guid id, CancellationToken ct)
{
    var result = await _context.Set<CoverLetter>()
        .AsNoTracking()
        .Where(cl => cl.Id == id)
        .Join(_context.Set<Vacancy>().AsNoTracking().IgnoreQueryFilters(),
            cl => cl.VacancyId,
            v => v.Id,
            (cl, v) => new CoverLetterDetailResult
            {
                Id = cl.Id,
                UserId = cl.UserId,
                VacancyId = cl.VacancyId,
                Content = cl.Content,
                CreatedAt = cl.CreatedAt,
                UpdatedAt = cl.UpdatedAt,
                Vacancy = new VacancyInCoverLetterResult
                {
                    Id = v.Id,
                    Title = v.Title,
                    Company = v.Company,
                    WorkLocationType = v.WorkLocationType,
                    Location = v.Location,
                }
            })
        .FirstOrDefaultAsync(ct);

    return result;
}
```

**Important**: Use `IgnoreQueryFilters()` on the Vacancy side of the JOIN. Per Story 5.5 AC: "if the vacancy is later deleted, the cover letter is still returned with vacancyId preserved." Since `VacancyId` FK in `CoverLetterConfiguration` is `ON DELETE CASCADE`, when a vacancy is hard-deleted, its cover letters are also hard-deleted — so this edge case won't occur in practice. But if soft-delete is used for vacancies, the global query filter would hide the vacancy from the JOIN, resulting in no join result. Use `IgnoreQueryFilters()` on the vacancy query to handle this safely.

### Response DTOs

```csharp
public class CoverLetterDetailResult
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }        // used for ownership check in handler, not returned to client
    public Guid VacancyId { get; set; }
    public string Content { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public VacancyInCoverLetterResult Vacancy { get; set; } = null!;
}

public class VacancyInCoverLetterResult
{
    public Guid Id { get; set; }
    public string? Title { get; set; }
    public string? Company { get; set; }
    public WorkLocationType? WorkLocationType { get; set; }
    public Location? Location { get; set; }
}
```

The `UserId` field on `CoverLetterDetailResult` is used internally for the ownership check in the handler. Do NOT serialize it in the JSON response — add `[JsonIgnore]` or use a separate internal DTO and a public API-facing DTO.

### Ownership: 404 Not 403

Per Decision 7 and the AC: cross-user detail reads return `404 Not Found` (not `403`) to prevent existence leakage. This is consistent with `GetCoverLetterTemplateQueryHandler` and `GetVacancyQueryHandler`.

### Files to Read Before Implementation

- `backend/src/JobNecto.Application/Interfaces/ICoverLetterRepository.cs` (created in Story 5.2)
- `backend/src/JobNecto.Application/CoverLetterTemplates/GetCoverLetterTemplateQueryHandler.cs` (404 ownership pattern)
- `backend/src/JobNecto.Application/Vacancies/GetVacancyQueryHandler.cs` (nested DTO pattern)
- `backend/src/JobNecto.Domain/Entities/Vacancy.cs` (to confirm which fields to include in nested DTO)
- `backend/src/JobNecto.Infrastructure/Repositories/CoverLetterRepository.cs`

### Project Structure Notes

- `CoverLetterDetailResult` and `VacancyInCoverLetterResult` in `backend/src/JobNecto.Application/CoverLetters/GetCoverLetterQuery.cs`.
- Handler in `backend/src/JobNecto.Application/CoverLetters/GetCoverLetterQueryHandler.cs`.
- Namespace: `JobNecto.Application.CoverLetters`.

### References

- [Source: `_bmad-output/planning-artifacts/epics/epic-5-cover-letter-application-management.md` — Story 5.3]
- [Source: `_bmad-output/planning-artifacts/epics/requirements-inventory.md` — FR21]
- [Source: `_bmad-output/planning-artifacts/architecture/core-architectural-decisions.md` — Decision 3, 7]

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Completion Notes List

- Extends `ICoverLetterRepository` from Story 5.2 — that story must be complete first.
- `IgnoreQueryFilters()` on vacancy side of JOIN handles soft-deleted vacancy edge case.
- `UserId` on detail result is internal-only for ownership check — must NOT be serialized.
- `templateId` is not persisted or returned — no field on `CoverLetter` entity or in the response DTO.
- `WorkLocationType` uses `JsonStringEnumConverter` (registered globally in `Program.cs` per agent-learnings).

### File List

- `backend/src/JobNecto.Application/CoverLetters/GetCoverLetterQuery.cs` (CREATED)
- `backend/src/JobNecto.Application/CoverLetters/GetCoverLetterQueryHandler.cs` (CREATED)
- `backend/tests/JobNecto.Tests/Application/CoverLetters/GetCoverLetterQueryHandlerTests.cs` (CREATED)
- `backend/src/JobNecto.Application/Interfaces/ICoverLetterRepository.cs` (UPDATED — GetDetailByIdAsync added)
- `backend/src/JobNecto.Infrastructure/Repositories/CoverLetterRepository.cs` (UPDATED — implements GetDetailByIdAsync with vacancy JOIN + IgnoreQueryFilters)
- `backend/src/JobNecto.API/Controllers/CoverLettersController.cs` (UPDATED — GET /{id} endpoint added)
- `_bmad-output/implementation-artifacts/5-3-get-cover-letter-detail.md` (this file)
