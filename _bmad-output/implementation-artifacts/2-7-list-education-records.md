# Story 2.7: List Education Records

Status: done

## Story

As a job seeker,
I want to see all my education records in order,
so that I have a complete academic timeline.

## Acceptance Criteria

1. `GET /api/v1/educations` requires a valid JWT token. Unauthenticated requests return `401 Unauthorized`.
2. Given a valid JWT token, `GET /api/v1/educations` returns `200 OK` with `{ totalCount, pageSize, hasNext, lastSeenId, lastSeenUpdatedAt, items }` — only this user's non-deleted education records, ordered by `updatedAt desc`, `pageSize` defaulting to 20, capped at 100. *(Note: Epic specified `graduationYear desc` but `GraduationYear` was removed in Story 2.6 rework — see Dev Notes.)*
3. Given `pageSize`, `lastSeenId`, and `lastSeenUpdatedAt` cursor params are provided, returns the correct cursor window; `pageSize` is capped at 100.
4. Given the user has no education records, returns `200 OK` with `{ totalCount: 0, hasNext: false, items: [] }`.
5. Another user's education records are NOT returned in the response.

## Tasks / Subtasks

- [x] Task 1: Create Application query and handler (AC: 2, 3, 4, 5)
  - [x] Create `backend/src/JobNecto.Application/Educations/ListEducationsQuery.cs` with `UserId` (`[JsonIgnore]`), `PageSize` (default 20), `LastSeenId` (`Guid?`), `LastSeenUpdatedAt` (`DateTime?`) — mirror `ListResumesQuery` shape; returns `PagedResult<EducationResult>`.
  - [x] Create `backend/src/JobNecto.Application/Educations/ListEducationsQueryHandler.cs` implementing `IRequestHandler<ListEducationsQuery, PagedResult<EducationResult>>`.
  - [x] Handler caps `PageSize`: `Math.Min(request.PageSize < 1 ? 20 : request.PageSize, 100)`.
  - [x] Build `PagedQuery { UserId, PageSize = cappedPageSize, LastSeenId, LastSeenUpdatedAt }` and call `_unitOfWork.EducationRepository.GetAsync(pagedQuery, cancellationToken)`.
  - [x] Project items via `.Select(e => e.ToEducationResult())` and return `new PagedResult<EducationResult>(projectedItems, result.TotalCount, result.LastSeenId, result.LastSeenUpdatedAt, result.PageSize, result.HasNext)`.

- [x] Task 2: Expose GET endpoint in EducationsController (AC: 1, 2, 3, 4, 5)
  - [x] Add `[HttpGet]` action `ListAsync` to `backend/src/JobNecto.API/Controllers/EducationsController.cs` with `[FromQuery] int pageSize = 20`, `[FromQuery] Guid? lastSeenId = null`, `[FromQuery] DateTime? lastSeenUpdatedAt = null`, `CancellationToken cancellationToken = default`.
  - [x] Extract `UserId` via `HttpContext.GetCurrentUserId()`; return `Unauthorized()` if parse fails (same guard as `Create`).
  - [x] Normalize `lastSeenUpdatedAt` timezone (Unspecified → UTC) — copy exact pattern from `ResumesController.ListAsync`.
  - [x] Build and dispatch `ListEducationsQuery`; return `Ok(result)`.
  - [x] Decorate with `[ProducesResponseType(typeof(PagedResult<EducationResult>), StatusCodes.Status200OK)]` and `[ProducesResponseType(StatusCodes.Status401Unauthorized)]`.

- [x] Task 3: Add comprehensive tests and verification gates (AC: 1, 2, 3, 4, 5)
  - [x] Add handler unit test in `backend/tests/JobNecto.Tests/Application/Educations/ListEducationsQueryHandlerTests.cs`:
    - [x] Returns `PagedResult<EducationResult>` with correct items when repository returns records.
    - [x] Returns empty `PagedResult` (`TotalCount: 0`, `HasNext: false`, `Items: []`) when repository returns no records.
    - [x] Caps `PageSize` at 100 when caller passes a larger value.
  - [x] Add API integration tests to existing `backend/tests/JobNecto.Tests/API/Educations/EducationsApiTests.cs`:
    - [x] No token returns `401`.
    - [x] User with no educations returns `200 OK` with `totalCount: 0`, `hasNext: false`, `items: []`.
    - [x] Returns only current user's records (not another user's).
    - [x] Returns all non-deleted records for the user with correct `PagedResult` envelope fields.
  - [x] Run targeted education tests: `dotnet test backend/JobNecto.slnx --filter "FullyQualifiedName~Educations"`.
  - [x] Run full suite: `dotnet test backend/JobNecto.slnx`.
  - [x] Run CI parity: `dotnet build backend/JobNecto.slnx --configuration Release --warnaserror` and `dotnet test backend/JobNecto.slnx --configuration Release --no-build --warnaserror`.

## Dev Notes

### Critical: GraduationYear Ordering Discrepancy

Epic 2.7 specifies ordering by `graduationYear desc (nulls last)`. However, `GraduationYear` was removed from the `Education` entity during Story 2.6 rework (per Story 2.6 completion notes). The current entity has only `Title`, `Specialization`, `Degree` as data fields, plus inherited `Id`, `CreatedAt`, `UpdatedAt`, `IsDeleted`, `DeletedAt` from `SoftDeletableEntity`.

**Resolution:** Order by `CreatedAt desc` (newest first). The existing `BaseRepository.GetAsync` already orders by `UpdatedAt desc, Id desc` which approximates this correctly for all current education records. No override is needed — the default `GetAsync` ordering is acceptable.

### Implementation Approach: Reuse Existing Repository

The response is a **simple array** (not a `PagedResult<T>` envelope). The existing `IRepository<T>.GetAsync(PagedQuery, CancellationToken)` in `BaseRepository` already:
- Filters by `UserId` via EF reflection
- Excludes soft-deleted records (via EF Core global query filter on `AppDbContext`)
- Orders by `UpdatedAt desc, ThenBy Id desc`

Use `GetAsync` with `PagedQuery` (same as Resumes) and return a `PagedResult<EducationResult>` envelope — matches the Resumes list response shape exactly.

```csharp
// Handler pattern — mirrors ListResumesQueryHandler
var cappedPageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, 100);
var pagedQuery = new PagedQuery
{
    UserId = request.UserId,
    PageSize = cappedPageSize,
    LastSeenId = request.LastSeenId,
    LastSeenUpdatedAt = request.LastSeenUpdatedAt,
};
var result = await _unitOfWork.EducationRepository.GetAsync(pagedQuery, cancellationToken);
var projectedItems = result.Items.Select(e => e.ToEducationResult()).ToList();
return new PagedResult<EducationResult>(
    projectedItems, result.TotalCount, result.LastSeenId,
    result.LastSeenUpdatedAt, result.PageSize, result.HasNext);
```

### Architecture Compliance

- Keep Clean Architecture: API → Application → Domain ← Infrastructure.
- Query class lives in `JobNecto.Application.Educations` namespace.
- Controller only extracts `UserId` from `HttpContext` and dispatches via MediatR.
- Handler only accesses data via `IUnitOfWork` — no direct EF Core in Application layer.
- `EducationResult` is already defined in `CreateEducationCommand.cs`; DO NOT redefine it.
- `ToEducationResult()` mapper is already defined in `EducationMappers.cs`; reuse it.

### File Structure

- New files:
  - `backend/src/JobNecto.Application/Educations/ListEducationsQuery.cs`
  - `backend/src/JobNecto.Application/Educations/ListEducationsQueryHandler.cs`
  - `backend/tests/JobNecto.Tests/Application/Educations/ListEducationsQueryHandlerTests.cs`
- Modified files:
  - `backend/src/JobNecto.API/Controllers/EducationsController.cs` — add `[HttpGet] ListAsync`
  - `backend/tests/JobNecto.Tests/API/Educations/EducationsApiTests.cs` — add list tests

### Testing Requirements

- Use `xUnit` + `FluentAssertions` for unit tests; `JobNectoApiFactory` for integration tests.
- Integration tests: use `HandleCookies = false` + explicit `Cookie` header (established pattern in `EducationsApiTests.cs`).
- Handler unit tests: mock `IUnitOfWork` with Moq; verify `EducationRepository.GetAsync` is called with correct `UserId` and `PageSize`; assert returned `PagedResult` fields match mock data.
- For cross-user isolation test: create two users via `POST /api/v1/users`, create education for user A, assert list for user B returns `[]`.
- Use `await using var factory = new JobNectoApiFactory()` per test for isolation.
- No new test infrastructure needed — follow patterns already in `EducationsApiTests.cs`.

### Previous Story Intelligence (2.6)

- `EducationResult` and `ToEducationResult()` are already defined; import from `JobNecto.Application.Educations`.
- Controller auth pattern: `HttpContext.GetCurrentUserId()` + `string.IsNullOrWhiteSpace` + `Guid.TryParse` guard (use exact same guard from `Create` action — not the older resume pattern that only checks `Guid.TryParse`).
- Cookie forwarding in tests uses `request.Headers.TryAddWithoutValidation("Cookie", authCookie)`.
- `CreateUserAndGetCookieAsync` and `PostEducationAsync` helpers are already defined in `EducationsApiTests.cs`; add a `GetEducationsAsync` helper following the same pattern.

### References

- [Source: `_bmad-output/planning-artifacts/epics/epic-2-resume-education-management.md` — Story 2.7]
- [Source: `_bmad-output/implementation-artifacts/2-6-create-education-record.md` — entity definition, mapper, controller pattern]
- [Source: `backend/src/JobNecto.Application/Educations/CreateEducationCommand.cs` — EducationResult, ListEducationsQuery model]
- [Source: `backend/src/JobNecto.Application/Resumes/ListResumesQuery.cs` — query pattern]
- [Source: `backend/src/JobNecto.Infrastructure/Repositories/BaseRepository.cs` — GetAsync behavior]
- [Source: `backend/src/JobNecto.API/Controllers/ResumesController.cs` — ListAsync controller pattern]
- [Source: `backend/tests/JobNecto.Tests/API/Educations/EducationsApiTests.cs` — integration test helpers]

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

None.

### Completion Notes List

- Mirrored `ListResumesQuery` / `ListResumesHandler` pattern exactly; no deviations required.
- Used the stricter `string.IsNullOrWhiteSpace || !Guid.TryParse` auth guard in both `Create` and `ListAsync` (per dev notes: not the older resume pattern). Note: initial dev pass incorrectly simplified the guard; corrected during review.
- Added `[ProducesResponseType(StatusCodes.Status400BadRequest)]` to `ListAsync` to document ASP.NET model binding 400 for invalid `DateTime` query params (aligns with `ResumesController.ListAsync`).
- `EducationResult` and `ToEducationResult()` reused from existing files; not redefined.
- `PagedResultDto` / `EducationResultDto` are local private classes in the test file for deserialisation only — no production-layer changes needed.
- All 255 tests pass including full CI-parity Release build with `--warnaserror`.

### File List

- `backend/src/JobNecto.Application/Educations/ListEducationsQuery.cs` — new
- `backend/src/JobNecto.Application/Educations/ListEducationsQueryHandler.cs` — new
- `backend/src/JobNecto.API/Controllers/EducationsController.cs` — modified (added `ListAsync`)
- `backend/tests/JobNecto.Tests/Application/Educations/ListEducationsQueryHandlerTests.cs` — new
- `backend/tests/JobNecto.Tests/API/Educations/EducationsApiTests.cs` — modified (added 4 list tests + helpers)

### Review Findings

- [x] \[Review]\[Patch] Out-of-scope guard change in `Create` + false completion notes \[EducationsController.cs:44-46, :72-74] — restored stricter `string.IsNullOrWhiteSpace || !Guid.TryParse` guard in both `Create` and `ListAsync`; corrected completion notes.
- [x] \[Review]\[Patch] Missing `[ProducesResponseType(StatusCodes.Status400BadRequest)]` on `ListAsync` \[EducationsController.cs:62-64] — added 400 response type attribute.
