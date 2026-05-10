# Story 5.2: List Cover Letters

Status: ready-for-dev

## Story

As a job seeker,
I want to see all my cover letters in a paginated list,
so that I can track all my job applications.

## Acceptance Criteria

1. Given a valid JWT token, when `GET /api/v1/cover-letters` is called, then `200 OK` with `{ totalCount, pageSize, hasNext, lastSeenId, lastSeenUpdatedAt, items }` — non-deleted cover letters owned by this user, ordered by `createdAt desc`, each item includes: `id`, `vacancyId`, `vacancyTitle` (from linked vacancy), `createdAt`, `updatedAt`.
2. Given `pageSize`, `lastSeenId`, `lastSeenUpdatedAt` cursor params are provided, when the request is processed, then correct cursor window returned; `pageSize` capped at 100.
3. Given the user has no cover letters, when `GET /api/v1/cover-letters` is called, then `200 OK` with `{ totalCount: 0, hasNext: false, items: [] }`.
4. Given no JWT token, when `GET /api/v1/cover-letters` is called, then `401 Unauthorized`.

## Tasks / Subtasks

- [ ] Task 1: Introduce `ICoverLetterRepository` with specialized list method (AC: 1, 2)
  - [ ] Create `backend/src/JobNecto.Application/Interfaces/ICoverLetterRepository.cs` extending `IMutableRepository<CoverLetter>` with `GetPagedListAsync(PagedQuery pagedQuery, CancellationToken ct)` returning `PagedResult<CoverLetterListItem>`.
  - [ ] Define `CoverLetterListItem` record in the same file (or in `ListCoverLettersQuery.cs`): `Id`, `VacancyId`, `VacancyTitle` (nullable string), `CreatedAt`, `UpdatedAt`.
  - [ ] Update `IUnitOfWork.CoverLetterRepository` from `IMutableRepository<CoverLetter>` to `ICoverLetterRepository`.

- [ ] Task 2: Implement `ICoverLetterRepository` in Infrastructure (AC: 1, 2, 3)
  - [ ] Update `backend/src/JobNecto.Infrastructure/Repositories/CoverLetterRepository.cs` to implement `ICoverLetterRepository` (still extends `SoftDeletableRepository<CoverLetter>`).
  - [ ] Implement `GetPagedListAsync`: user-scoped (`UserId` filter), ordered by `CreatedAt DESC, Id DESC`, JOIN to `Vacancies` for `Title`, `pageSize` cap 100, cursor logic uses `CreatedAt` (not `UpdatedAt`), returns `PagedResult<CoverLetterListItem>` with correct `hasNext`, `totalCount`, and cursor fields.
  - [ ] Cursor field `LastSeenUpdatedAt` in `PagedQuery` carries `CreatedAt` of the last seen cover letter for this endpoint (the field name is a shared contract name — the value semantics differ here).
  - [ ] Update `backend/src/JobNecto.Infrastructure/Persistance/UnitOfWork.cs` to return `ICoverLetterRepository`.

- [ ] Task 3: Create query, handler, and DTOs (AC: 1, 2, 3)
  - [ ] Create `backend/src/JobNecto.Application/CoverLetters/ListCoverLettersQuery.cs` with `ListCoverLettersQuery` (`IRequest<PagedResult<CoverLetterListItem>>`), `UserId` (JsonIgnore), `PageSize` (default 20), `LastSeenId`, `LastSeenUpdatedAt`.
  - [ ] Create `backend/src/JobNecto.Application/CoverLetters/ListCoverLettersQueryHandler.cs`: cap `PageSize` to 100, build `PagedQuery`, call `_unitOfWork.CoverLetterRepository.GetPagedListAsync(...)`, return result.

- [ ] Task 4: Add API endpoint (AC: 4)
  - [ ] Add `[HttpGet]` action to `backend/src/JobNecto.API/Controllers/CoverLettersController.cs`.
  - [ ] Query params: `pageSize`, `lastSeenId`, `lastSeenUpdatedAt`; normalize `lastSeenUpdatedAt` to UTC (same pattern as `CoverLetterTemplatesController`).
  - [ ] `ProducesResponseType` for 200, 401.

- [ ] Task 5: Add tests (AC: 1–4)
  - [ ] Create `backend/tests/JobNecto.Tests/Application/CoverLetters/ListCoverLettersQueryHandlerTests.cs` — mock `ICoverLetterRepository`; test: empty list, populated list with correct item fields, pageSize cap.
  - [ ] In `backend/tests/JobNecto.Tests/API/CoverLetters/CoverLettersApiTests.cs` — add integration tests: no token → 401; empty result when no cover letters; correct `createdAt desc` ordering; `vacancyTitle` populated from linked vacancy; cursor pagination advances correctly; cross-user isolation (user A's letters not visible to user B).

- [ ] Task 6: Verification gates
  - [ ] Build: `dotnet build backend/JobNecto.slnx --configuration Release --warnaserror` — 0 warnings, 0 errors.
  - [ ] Tests: `dotnet test backend/JobNecto.slnx` — all pass.

## Dev Notes

### Critical: Why `ICoverLetterRepository` Is Needed

`BaseRepository<T>.GetAsync` always orders by `UpdatedAt DESC` and returns only the entity's own fields. Story 5.2 requires:
1. **Ordering by `CreatedAt DESC`** — different from standard.
2. **JOIN to `Vacancies` table** for `vacancyTitle` — not available via generic `GetAsync`.

This justifies introducing `ICoverLetterRepository` per Decision 3: "Do not introduce bespoke repository interfaces only for naming symmetry" — but this case has **real specialized query behavior**. Same rationale as `IVacancyRepository` for `GetFilteredAsync`.

### `GetPagedListAsync` Implementation Sketch

```csharp
public async Task<PagedResult<CoverLetterListItem>> GetPagedListAsync(
    PagedQuery pagedQuery, CancellationToken ct)
{
    var pageSize = Math.Max(1, Math.Min(pagedQuery.PageSize, 100));

    var query = _context.Set<CoverLetter>()
        .AsNoTracking()
        .Where(cl => cl.UserId == pagedQuery.UserId!.Value)
        .Join(_context.Set<Vacancy>().AsNoTracking(),
            cl => cl.VacancyId,
            v => v.Id,
            (cl, v) => new { CoverLetter = cl, VacancyTitle = v.Title });

    var totalCount = await query.CountAsync(ct);

    // Order by CreatedAt DESC for cover letters (not UpdatedAt)
    var orderedQuery = query.OrderByDescending(x => x.CoverLetter.CreatedAt)
                            .ThenByDescending(x => x.CoverLetter.Id);

    // Cursor logic: LastSeenUpdatedAt carries CreatedAt of last seen item
    if (pagedQuery.LastSeenId is Guid lastSeenId && pagedQuery.LastSeenUpdatedAt is DateTime cursorCreatedAt)
    {
        orderedQuery = orderedQuery.Where(x =>
            x.CoverLetter.CreatedAt < cursorCreatedAt
            || (x.CoverLetter.CreatedAt == cursorCreatedAt && x.CoverLetter.Id < lastSeenId));
    }

    var pagePlusOne = await orderedQuery.Take(pageSize + 1).ToListAsync(ct);
    var items = pagePlusOne.Take(pageSize)
        .Select(x => new CoverLetterListItem
        {
            Id = x.CoverLetter.Id,
            VacancyId = x.CoverLetter.VacancyId,
            VacancyTitle = x.VacancyTitle,
            CreatedAt = x.CoverLetter.CreatedAt,
            UpdatedAt = x.CoverLetter.UpdatedAt,
        }).ToList();

    var hasNext = pagePlusOne.Count > pageSize;
    // LastSeenUpdatedAt carries CreatedAt value for this endpoint
    return new PagedResult<CoverLetterListItem>(
        items, totalCount,
        items.Count > 0 ? items[^1].Id : null,
        items.Count > 0 ? items[^1].CreatedAt : null,  // ← createdAt, not updatedAt
        pageSize, hasNext);
}
```

**Note**: The EF global query filter for `CoverLetter` (`!IsDeleted`) applies automatically. The `Vacancy` soft-delete filter also applies, so soft-deleted vacancies won't appear in the JOIN. Per AC of Story 5.5: a cover letter whose vacancy was deleted should still return — check if the query needs `IgnoreQueryFilters` for the vacancy side if the FK cascade deletes the cover letter anyway (it does — see `CoverLetterConfiguration`: `ON DELETE CASCADE` on VacancyId FK). So this case doesn't arise in practice (cover letter is hard-deleted when vacancy is deleted).

### `IUnitOfWork` Change

In `backend/src/JobNecto.Application/Interfaces/IUnitOfWork.cs`, change:
```csharp
// From:
IMutableRepository<CoverLetter> CoverLetterRepository { get; }
// To:
ICoverLetterRepository CoverLetterRepository { get; }
```

All existing handler calls still work because `ICoverLetterRepository : IMutableRepository<CoverLetter>`.

### Cursor Semantics for This Endpoint

The standard pagination envelope uses `lastSeenUpdatedAt` as the cursor timestamp field name. For cover letters, the ordering is `createdAt desc`. Therefore, `lastSeenUpdatedAt` in requests/responses **carries the `createdAt` value** of the last seen item. Document this explicitly in code comments.

The controller must normalize `lastSeenUpdatedAt` to UTC (same pattern as `CoverLetterTemplatesController`).

### XOR Cursor Validation

Per agent-learnings: cursor requires both `lastSeenId` AND `lastSeenUpdatedAt` — if only one is provided, return 400. Add this validation in the controller (not the handler), consistent with the story 4.1 pattern:
```csharp
if (lastSeenId.HasValue != lastSeenUpdatedAt.HasValue)
    return BadRequest(new ProblemDetails { Status = 400, Title = "Validation failed",
        Detail = "lastSeenId and lastSeenUpdatedAt must both be provided or both omitted." });
```

### Files to Read Before Implementation

- `backend/src/JobNecto.Application/Interfaces/IUnitOfWork.cs`
- `backend/src/JobNecto.Application/Interfaces/IVacancyRepository.cs` (specialized interface pattern)
- `backend/src/JobNecto.Infrastructure/Repositories/BaseRepository.cs` (understand `GetAsync` to know what you're replacing)
- `backend/src/JobNecto.Infrastructure/Repositories/VacancyRepository.cs` (ordering override pattern)
- `backend/src/JobNecto.Infrastructure/Repositories/CoverLetterRepository.cs`
- `backend/src/JobNecto.Infrastructure/Persistance/UnitOfWork.cs`
- `backend/src/JobNecto.API/Controllers/CoverLetterTemplatesController.cs` (UTC normalization pattern)
- `backend/src/JobNecto.Application/CoverLetterTemplates/ListCoverLetterTemplatesQuery.cs` (query DTO pattern)
- `backend/tests/JobNecto.Tests/API/CoverLetterTemplates/CoverLetterTemplatesApiTests.cs` (test helpers)

### Project Structure Notes

- `ICoverLetterRepository` lives in `backend/src/JobNecto.Application/Interfaces/` — namespace `JobNecto.Application.Interfaces`.
- `CoverLetterListItem` DTO lives in `backend/src/JobNecto.Application/CoverLetters/ListCoverLettersQuery.cs`.
- Test namespace: `JobNecto.Tests.Application.CoverLetters`, `JobNecto.Tests.API.CoverLetters`.

### References

- [Source: `_bmad-output/planning-artifacts/epics/epic-5-cover-letter-application-management.md` — Story 5.2]
- [Source: `_bmad-output/planning-artifacts/epics/requirements-inventory.md` — FR20, NFR10]
- [Source: `_bmad-output/planning-artifacts/architecture/core-architectural-decisions.md` — Decision 3, 7]
- [Source: `_bmad-output/agent-learnings.md` — XOR cursor validation, BadRequest shape]

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Completion Notes List

- `ICoverLetterRepository` introduced because of ordering difference (`createdAt` vs `updatedAt`) and JOIN requirement.
- Cursor `lastSeenUpdatedAt` carries `createdAt` value — documented explicitly.
- XOR cursor validation required per agent-learnings.
- `IUnitOfWork` property type change is a compile-time-breaking change — update `UnitOfWork.cs` property return type too.

### File List

- `_bmad-output/implementation-artifacts/5-2-list-cover-letters.md` (this file)
