# Story 2.2: List Resumes

Status: done

## Story

As a job seeker,
I want to see all my resumes in a paginated list,
So that I can quickly navigate to the one I need.

## Acceptance Criteria

1. `GET /api/v1/resumes` requires a valid JWT token. Unauthenticated requests return `401 Unauthorized`.
2. Returns `200 OK` with a paginated response: `{ totalCount, pageSize, hasNext, lastSeenId, lastSeenUpdatedAt, items }` — only the current user's non-deleted resumes, ordered by `updatedAt desc`, `pageSize` defaulting to 20.
3. The `pageSize` query param (optional) configures page size; capped at 100. Values < 1 are treated as default (20). Non-numeric/invalid format values (e.g., `pageSize=abc`) return `400 Bad Request` via automatic model binding validation.
4. Cursor-based pagination: optional `lastSeenId` (Guid) and `lastSeenUpdatedAt` (DateTime) query params are passed to advance the page window.
5. If the user has no resumes, returns `200 OK` with `{ totalCount: 0, items: [] }`.
6. Resumes belonging to a different user are **never** returned regardless of cursor or page size.
7. Soft-deleted resumes (flagged `IsDeleted = true`) are excluded — enforced by EF Core global query filters.

## Tasks / Subtasks

- [x] Task 1: Define the Application query contract (AC: 2, 3, 4)
  - [x] Create `ListResumesQuery.cs` in `backend/src/JobNecto.Application/Resumes/`.
  - [x] Declare `ListResumesQuery : IRequest<PagedResult<ResumeResult>>` with properties: `UserId` (Guid, `[JsonIgnore]`), `PageSize` (int, default 20), `LastSeenId` (Guid?), `LastSeenUpdatedAt` (DateTime?).
  - [x] Implement `ListResumesQueryHandler : IRequestHandler<ListResumesQuery, PagedResult<ResumeResult>>` in the same file or as a sibling file.
  - [x] In the handler: cap `pageSize` at 100 (Math.Min), build `PagedQuery { UserId = request.UserId, PageSize = capped, LastSeenId = request.LastSeenId, LastSeenUpdatedAt = request.LastSeenUpdatedAt }`.
  - [x] Call `_unitOfWork.ResumeRepository.GetAsync(pagedQuery, cancellationToken)` and project each entity to `ResumeResult` via `resume.ToResumeResult()` from `ResumeMappers`.
  - [x] Return a new `PagedResult<ResumeResult>` reconstructed with the projected items list, and the same `TotalCount`, `LastSeenId`, `LastSeenUpdatedAt`, `PageSize`, `HasNext` from the repository result.

- [x] Task 2: Expose API endpoint (AC: 1, 2, 3, 4, 5)
  - [x] Open `backend/src/JobNecto.API/Controllers/ResumesController.cs`.
  - [x] Add `GET` action `ListAsync([FromQuery] int pageSize = 20, [FromQuery] Guid? lastSeenId = null, [FromQuery] DateTime? lastSeenUpdatedAt = null, CancellationToken cancellationToken = default)`.
  - [x] Decorate with `[HttpGet]`, `[ProducesResponseType(typeof(PagedResult<ResumeResult>), 200)]`, `[ProducesResponseType(401)]`.
  - [x] Extract `UserId` from JWT context using `HttpContext.GetCurrentUserId()` (same pattern as `Create` action in same controller).
  - [x] Build and send `ListResumesQuery` via `_mediator.Send(query, cancellationToken)`.
  - [x] Return `Ok(result)`.

- [x] Task 3: Verification and Testing (AC: 1–7)
  - [x] Add unit tests for `ListResumesQueryHandler` in `backend/tests/JobNecto.Tests/Application/Resumes/ListResumesHandlerTests.cs`.
    - [x] Test: correct `PagedQuery` is forwarded to repository (UserId, PageSize capping, cursors).
    - [x] Test: handler maps returned `Resume` entities to `ResumeResult` correctly.
    - [x] Test: empty repository result returns `PagedResult` with empty items.
    - [x] Test: `pageSize` > 100 is capped at 100.
  - [x] Add integration tests in `backend/tests/JobNecto.Tests/API/ResumesControllerTests.cs`.
    - [x] Test: `GET /api/v1/resumes` without token returns `401`.
    - [x] Test: `GET /api/v1/resumes` with valid token and no resumes returns `200` with `{ totalCount: 0, items: [] }`.
    - [x] Test: `GET /api/v1/resumes` returns only the authenticated user's resumes (not another user's).
    - [x] Test: `pageSize` query param is respected (returns correct count).
    - [x] Test: non-numeric `pageSize` returns `400 Bad Request`.
    - [x] Test: `pageSize` > 100 is capped (response `pageSize` ≤ 100).
  - [x] Verify `dotnet test backend/JobNecto.slnx` passes.

## Dev Notes

### Pagination Format Decision

The project uses **cursor-based pagination** exclusively, implemented via `PagedQuery` + `PagedResult<T>` in `JobNecto.Domain.ValueObjects.Pagination`:

```csharp
// Domain/ValueObjects/Pagination.cs
public record PagedQuery
{
    public Guid? UserId { get; init; } = null;
    public Guid? LastSeenId { get; init; } = null;
    public DateTime? LastSeenUpdatedAt { get; init; } = null;
    public int PageSize { get; init; } = 20;
}

public record PagedResult<BaseEntity>(
    IReadOnlyList<BaseEntity> Items,
    int TotalCount,
    Guid? LastSeenId,
    DateTime? LastSeenUpdatedAt,
    int PageSize,
    bool HasNext
)
```

The `BaseRepository<T>.GetAsync(PagedQuery, ct)` (Infrastructure) automatically:

- Filters by `UserId` when `PagedQuery.UserId` is set (via reflection on EF model property)
- Applies cursor window using `LastSeenId` / `LastSeenUpdatedAt`
- Orders by `updatedAt desc, id desc`
- Counts total matching records (respecting global soft-delete filters)

The `ResumeRepository` in Infrastructure **overrides** `GetAsync` with user-scoped filtering to enforce that cursor positions from foreign users are ignored (see `ResumeRepositoryTests.GetAsync_WithForeignCursorAndUserId_IgnoresCursorOutsideUserScope`).

### Handler Pattern

Replicate the handler pattern from `CreateResumeCommandHandler`:

```csharp
// Application/Resumes/ListResumesQueryHandler.cs
public class ListResumesQueryHandler : IRequestHandler<ListResumesQuery, PagedResult<ResumeResult>>
{
    private readonly IUnitOfWork _unitOfWork;

    public ListResumesQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task<PagedResult<ResumeResult>> Handle(ListResumesQuery request, CancellationToken cancellationToken)
    {
        var normalizedPageSize = request.PageSize < 1 ? 20 : request.PageSize;  
        var cappedPageSize = Math.Min(normalizedPageSize, 100);  

        var pagedQuery = new PagedQuery
        {
            UserId = request.UserId,
            PageSize = cappedPageSize,
            LastSeenId = request.LastSeenId,
            LastSeenUpdatedAt = request.LastSeenUpdatedAt,
        };

        var result = await _unitOfWork.ResumeRepository.GetAsync(pagedQuery, cancellationToken);

        var projectedItems = result.Items.Select(r => r.ToResumeResult()).ToList();

        return new PagedResult<ResumeResult>(
            projectedItems,
            result.TotalCount,
            result.LastSeenId,
            result.LastSeenUpdatedAt,
            result.PageSize,
            result.HasNext
        );
    }
}
```

### Controller Pattern

Add to the **existing** `ResumesController.cs` (do NOT create a new controller):

```csharp
[HttpGet]
[ProducesResponseType(typeof(PagedResult<ResumeResult>), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public async Task<ActionResult<PagedResult<ResumeResult>>> ListAsync(
    [FromQuery] int pageSize = 20,
    [FromQuery] Guid? lastSeenId = null,
    [FromQuery] DateTime? lastSeenUpdatedAt = null,
    CancellationToken cancellationToken = default)
{
    var userIdValue = HttpContext.GetCurrentUserId();
    if (!Guid.TryParse(userIdValue, out var userId))
        return Unauthorized();

    var query = new ListResumesQuery
    {
        UserId = userId,
        PageSize = pageSize,
        LastSeenId = lastSeenId,
        LastSeenUpdatedAt = lastSeenUpdatedAt,
    };

    var result = await _mediator.Send(query, cancellationToken);
    return Ok(result);
}
```

### Integration Test Pattern

Follow `UsersControllerTests.cs` — use `JobNectoApiFactory` (WebApplicationFactory + InMemory DB). To authenticate:

1. `POST /api/v1/users` to create a user (response includes `Set-Cookie: auth-token=...`).
2. Include cookie on subsequent requests via `WebApplicationFactoryClientOptions { HandleCookies = true }` or manually set `Authorization: Bearer <token>`.
3. Seed resumes by calling `POST /api/v1/resumes` for that authenticated user.

### Unit Test Pattern

Follow `CreateResumeCommandHandlerTests.cs`:

- Mock `IUnitOfWork` with `Mock<IUnitOfWork>`.
- Mock `IEditableRepository<Resume>` (which is what `ResumeRepository` implements — the UoW exposes it as `IEditableRepository<Resume>`).
- Setup `_uowMock.Setup(x => x.ResumeRepository).Returns(_resumeRepoMock.Object)`.
- For list handler: setup `_resumeRepoMock.Setup(x => x.GetAsync(It.IsAny<PagedQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(pagedResult)`.

### Important File Paths

| Purpose | Path |
| --- | --- |
| Query + Handler | `backend/src/JobNecto.Application/Resumes/ListResumesQuery.cs` |
| Existing mapper | `backend/src/JobNecto.Application/Resumes/Mappers/ResumeMappers.cs` |
| Existing result DTO | `backend/src/JobNecto.Application/Resumes/CreateResumeCommand.cs` (`ResumeResult` class defined here) |
| Existing controller | `backend/src/JobNecto.API/Controllers/ResumesController.cs` |
| Existing auth helper | `backend/src/JobNecto.API/Infrastructure/AuthContext.cs` (extension method `GetCurrentUserId()`) |
| UoW interface | `backend/src/JobNecto.Application/Interfaces/IUnitOfWork.cs` (`ResumeRepository` as `IEditableRepository<Resume>`) |
| Pagination types | `backend/src/JobNecto.Domain/ValueObjects/Pagination.cs` |
| Resume entity | `backend/src/JobNecto.Domain/Entities/Resume.cs` |
| Resume repository | `backend/src/JobNecto.Infrastructure/Repositories/ResumeRepository.cs` |
| Unit tests (resume) | `backend/tests/JobNecto.Tests/Application/Resumes/` |
| API test factory | `backend/tests/JobNecto.Tests/API/JobNectoApiFactory.cs` |

### Namespace Rules

Per project convention, namespaces must match folder structure:

- `ListResumesQuery.cs` → `namespace JobNecto.Application.Resumes;`
- `ListResumesHandlerTests.cs` → `namespace JobNecto.Tests.Application.Resumes;` (match existing `CreateResumeCommandHandlerTests.cs`)
- Integration test file → check existing `UsersControllerTests.cs` — uses `namespace JobNecto.Tests.API;`

### Soft Delete

EF Core global query filters on `AppDbContext.OnModelCreating` exclude entities where `IsDeleted == true` automatically. **Do NOT** manually add `.Where(r => !r.IsDeleted)` — it is already applied globally.

### Dependency Injection

No new DI wiring needed. MediatR auto-discovers handlers registered via `AddMediatR(typeof(CreateResumeCommand).Assembly)` in `Program.cs` / Infrastructure DI setup.

### Project Structure Notes

- `ResumeResult` DTO lives inside `CreateResumeCommand.cs` (not a separate file) — this is the existing pattern in the Resumes feature slice. The `ListResumesQuery` handler should **reuse** this class (already imported via namespace) and NOT define a separate list-specific DTO.
- Do not add a `page` property to `PagedResult<T>` — it is a Domain value object and should not be changed for a single endpoint. If the product needs `page` in the response, expose it from the handler as a computed value in a wrapper, but this is not required by this story.

### Previous Story Learnings (from 2.1 / Epic 1)

- **Enum handling**: `WorkLocationType`, `Experience`, `Currency` are stored as enums in the domain entity and mapped to/from strings in `ResumeMappers.ToResumeResult()`. Use that mapper as-is.
- **UserId extraction**: Use `HttpContext.GetCurrentUserId()` extension (lives in `JobNecto.API.Infrastructure.AuthContext`). Parse with `Guid.TryParse`, return `Unauthorized()` on parse failure — same pattern as `Create` action.
- **IEditableRepository mock**: When unit-testing, `IUnitOfWork.ResumeRepository` is typed as `IEditableRepository<Resume>` (not a dedicated `IResumeRepository`). Mock as `Mock<IEditableRepository<Resume>>`.
- **Test isolation**: Use unique in-memory DB names per test (`Guid.NewGuid().ToString()`). Use `await using` for `AppDbContext` disposal.
- **Avoid namespace collisions**: The test files in `JobNecto.Tests` do not declare an explicit `namespace` in some cases (e.g., `ResumeRepositoryTests.cs`) — check the files you're adding near and be consistent.

### References

- [Source: `_bmad-output/archive/planning-artifacts/epics/epic-2-resume-education-management.md` — Story 2.2]
- [Source: `backend/src/JobNecto.Domain/ValueObjects/Pagination.cs`]
- [Source: `backend/src/JobNecto.Application/Interfaces/IRepository.cs`]
- [Source: `backend/src/JobNecto.Application/Interfaces/IUnitOfWork.cs`]
- [Source: `backend/src/JobNecto.Application/Resumes/CreateResumeCommand.cs`]
- [Source: `backend/src/JobNecto.Application/Resumes/CreateResumeCommandHandler.cs`]
- [Source: `backend/src/JobNecto.Application/Resumes/Mappers/ResumeMappers.cs`]
- [Source: `backend/src/JobNecto.API/Controllers/ResumesController.cs`]
- [Source: `backend/src/JobNecto.Infrastructure/Repositories/BaseRepository.cs`]
- [Source: `backend/src/JobNecto.Infrastructure/Repositories/ResumeRepository.cs`]
- [Source: `backend/tests/JobNecto.Tests/API/JobNectoApiFactory.cs`]
- [Source: `backend/tests/JobNecto.Tests/API/UsersControllerTests.cs`]
- [Source: `backend/tests/JobNecto.Tests/Application/Resumes/CreateResumeCommandHandlerTests.cs`]
- [Source: `backend/tests/JobNecto.Tests/Infrastructure/ResumeRepositoryTests.cs`]
- [Source: `_bmad-output/archive/implementation-artifacts/2-1-create-resume.md`]

## Dev Agent Record

### Agent Model Used

GitHub Copilot (Claude Sonnet 4.6)

### Debug Log References

- Full test suite: 192 tests, 0 failed (run: 2026-04-28)
- Full test suite: 193 tests, 0 failed after review patches (run: 2026-04-28)

### Review Findings

- [x] **D1 (dismissed)** — Partial cursor validation: silent half-cursor behavior accepted by product decision (2026-04-28)
- [x] **P1 (patch)** — AC 3 violation: `pageSize < 1` floors to `1` instead of default `20` — fixed: `request.PageSize < 1 ? 20 : Math.Min(request.PageSize, 100)`
- [x] **P2 (patch)** — Unit test renamed to `Handle_PageSizeBelowOne_DefaultsToTwenty`; assertion updated to `.Be(20)`
- [x] **P3 (patch)** — Added `List_WithPageSizeBelowOne_DefaultsToTwenty` integration test seeding 3 resumes and asserting `pageSize=20`
- [x] **P4 (patch)** — `List_WithPageSizeAbove100_IsCappedAt100` now seeds 3 resumes and asserts `result!.PageSize.Should().Be(100)` precisely
- [x] **W1 (defer)** — Cursor pagination not tested end-to-end (AC 4) — deferred, pre-existing test strategy; covered in `ResumeRepositoryTests`
- [x] **W2 (defer)** — Soft-delete exclusion not tested on this endpoint (AC 7) — deferred, pre-existing test strategy; global EF filter coverage in `ResumeRepositoryTests`
- [x] **W3 (defer)** — `DateTime` kind ambiguity on `lastSeenUpdatedAt` cursor — deferred, pre-existing cross-cutting architectural decision

### Completion Notes List

- Implemented `ListResumesQuery` + `ListResumesQueryHandler` in a single file (`ListResumesQuery.cs`), following the existing feature-slice pattern.
- In the handler, `pageSize` values below 1 default to 20; otherwise `pageSize` is capped at 100.
- Added `ListAsync` GET action to existing `ResumesController` — no new controller created.
- Added `using JobNecto.Domain.ValueObjects;` import to `ResumesController.cs` for `PagedResult<T>`.
- 6 unit tests covering: query forwarding, pageSize capping (above 100 and below 1), empty result, entity→DTO projection, metadata preservation.
- 5 integration tests covering: 401 without token, empty list, user isolation (other user's resumes excluded), pageSize respected, pageSize > 100 capped.
- No new DI registration needed — MediatR auto-discovers the handler via existing assembly scan.

### File List

- `_bmad-output/archive/implementation-artifacts/2-2-list-resumes.md`
- `backend/src/JobNecto.Application/Resumes/ListResumesQuery.cs`
- `backend/src/JobNecto.API/Controllers/ResumesController.cs`
- `backend/tests/JobNecto.Tests/Application/Resumes/ListResumesHandlerTests.cs`
- `backend/tests/JobNecto.Tests/API/ResumesControllerTests.cs`

