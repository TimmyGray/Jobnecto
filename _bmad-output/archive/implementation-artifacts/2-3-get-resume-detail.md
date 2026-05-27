# Story 2.3: Get Resume Detail

Status: done

## Story

As a job seeker,
I want to view the full detail of a specific resume,
so that I can review all its fields before applying.

## Acceptance Criteria

1. `GET /api/v1/resumes/{id}` requires a valid JWT token. Unauthenticated requests return `401 Unauthorized`.
2. If the resume ID exists, is not soft-deleted, and belongs to the current user → `200 OK` with all resume fields (full `ResumeResult` object).
3. If the resume ID does not exist → `404 Not Found`.
4. If the resume has been soft-deleted (`IsDeleted = true`) → `404 Not Found` (global query filter excludes it; same code path as not-found).
5. If the resume ID exists but belongs to a different user → `404 Not Found` (**no information leak** — do NOT return `403`).

## Tasks / Subtasks

- [x] Task 1: Define Application query contract (AC: 2, 3, 4, 5)
  - [x] Create `GetResumeQuery.cs` in `backend/src/JobNecto.Application/Resumes/`.
  - [x] Declare `GetResumeQuery : IRequest<ResumeResult>` with properties `ResumeId` (Guid) and `UserId` (Guid).
  - [x] Implement `GetResumeQueryHandler : IRequestHandler<GetResumeQuery, ResumeResult>` in the same folder (separate file or same file, matching sibling pattern).
  - [x] In the handler: call `_unitOfWork.ResumeRepository.GetByIdAsync(request.ResumeId, cancellationToken)` — this throws `NotFoundException` if entity not found or soft-deleted (global filter).
  - [x] After fetch: if `resume.UserId != request.UserId`, throw `new NotFoundException("Resume", request.ResumeId)` — same exception type to prevent info leak.
  - [x] Return `resume.ToResumeResult()` using the existing mapper in `ResumeMappers`.

- [x] Task 2: Expose API endpoint (AC: 1, 2)
  - [x] Open `backend/src/JobNecto.API/Controllers/ResumesController.cs`.
  - [x] Add `GET` action `GetAsync([FromRoute] Guid id, CancellationToken cancellationToken = default)`.
  - [x] Decorate with `[HttpGet("{id:guid}")]`, `[ProducesResponseType(typeof(ResumeResult), 200)]`, `[ProducesResponseType(401)]`, `[ProducesResponseType(404)]`.
  - [x] Extract `UserId` via `HttpContext.GetCurrentUserId()` and `Guid.TryParse` → return `Unauthorized()` if parse fails (same pattern as `Create` and `ListAsync`).
  - [x] Build and send `GetResumeQuery { ResumeId = id, UserId = userId }` via `_mediator.Send(query, cancellationToken)`.
  - [x] Return `Ok(result)`.

- [x] Task 3: Verification and Testing (AC: 1–5)
  - [x] Add unit tests in `backend/tests/JobNecto.Tests/Application/Resumes/GetResumeHandlerTests.cs`.
    - [x] Test: owned resume found → returns correct `ResumeResult` (spot-check key fields).
    - [x] Test: repository throws `NotFoundException` (not found) → handler propagates `NotFoundException`.
    - [x] Test: resume found but `UserId` differs → handler throws `NotFoundException` (not `ForbiddenException` or other).
  - [x] Add integration tests in `backend/tests/JobNecto.Tests/API/ResumesControllerTests.cs` (add to the existing class).
    - [x] Test: `GET /api/v1/resumes/{id}` without token returns `401`.
    - [x] Test: `GET /api/v1/resumes/{id}` with valid token and owned resume returns `200` with full `ResumeResult`.
    - [x] Test: `GET /api/v1/resumes/{id}` with a non-existent GUID returns `404`.
    - [x] Test: `GET /api/v1/resumes/{id}` after soft-delete of the resume returns `404` — deferred to story 2.5 (no delete endpoint exists yet; covered by non-existent ID test as proxy).
    - [x] Test: `GET /api/v1/resumes/{id}` using valid token of a different user returns `404` (not `403`).
  - [x] Run `dotnet test backend/JobNecto.slnx` and verify all tests pass.

## Dev Notes

### Key Architecture Constraints

- **No new repository methods needed.** `IRepository<T>.GetByIdAsync(Guid, CancellationToken)` already exists on `IEditableRepository<Resume>` (via `IRepository<T>` base) and is used identically by `GetCurrentUserQueryHandler` for users.
- **Soft-delete is transparent.** `BaseRepository.GetByIdAsync` uses `_dbSet.FirstOrDefaultAsync(e => e.Id == id, ct)` which respects EF Core global query filters — soft-deleted resumes are automatically excluded. No extra `IsDeleted` check needed in handler.
- **User scope is NOT enforced at the repository level** for single-entity lookups — only `GetAsync` (paged list) overrides in `ResumeRepository` enforce user-scoping. The handler **must** check ownership explicitly.
- **Return `NotFoundException` for wrong-user case** — the global exception handler maps `NotFoundException` → `404`. Never throw or return `ForbiddenException`/`403` for the wrong-user case (AC 5 — no info leak).

### Existing `BaseRepository.GetByIdAsync` Behaviour

```csharp
// Infrastructure/Repositories/BaseRepository.cs
public virtual async Task<T> GetByIdAsync(Guid id, CancellationToken ct)
{
    var entity = await _dbSet.FirstOrDefaultAsync(e => e.Id == id, ct);
    if (entity == null)
    {
        throw new NotFoundException($"Entity with id {id} not found");
    }
    return entity;
}
```

`NotFoundException` (in `JobNecto.Application.Exceptions`) is caught by `GlobalExceptionHandler` and produces a `404` ProblemDetails response.

### Handler Pattern (follow exactly)

```csharp
// Application/Resumes/GetResumeQueryHandler.cs
namespace JobNecto.Application.Resumes;

public class GetResumeQueryHandler : IRequestHandler<GetResumeQuery, ResumeResult>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetResumeQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task<ResumeResult> Handle(GetResumeQuery request, CancellationToken cancellationToken)
    {
        var resume = await _unitOfWork.ResumeRepository.GetByIdAsync(request.ResumeId, cancellationToken);

        if (resume.UserId != request.UserId)
            throw new NotFoundException("Resume", request.ResumeId);

        return resume.ToResumeResult();
    }
}
```

### Query Contract

```csharp
// Application/Resumes/GetResumeQuery.cs
namespace JobNecto.Application.Resumes;

public class GetResumeQuery : IRequest<ResumeResult>
{
    public Guid ResumeId { get; init; }
    public Guid UserId { get; init; }
}
```

### Controller Action Pattern (add to ResumesController.cs)

```csharp
[HttpGet("{id:guid}")]
[ProducesResponseType(typeof(ResumeResult), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<ActionResult<ResumeResult>> GetAsync(
    [FromRoute] Guid id,
    CancellationToken cancellationToken = default)
{
    var userIdValue = HttpContext.GetCurrentUserId();
    if (!Guid.TryParse(userIdValue, out var userId))
        return Unauthorized();

    var query = new GetResumeQuery { ResumeId = id, UserId = userId };
    var result = await _mediator.Send(query, cancellationToken);
    return Ok(result);
}
```

### Unit Test Pattern (follow ListResumesHandlerTests)

```csharp
// Tests/Application/Resumes/GetResumeHandlerTests.cs
public class GetResumeHandlerTests
{
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IEditableRepository<Resume>> _resumeRepoMock = new();
    private readonly GetResumeQueryHandler _handler;

    public GetResumeHandlerTests()
    {
        _uowMock.Setup(x => x.ResumeRepository).Returns(_resumeRepoMock.Object);
        _handler = new GetResumeQueryHandler(_uowMock.Object);
    }

    [Fact]
    public async Task Handle_OwnedResume_ReturnsResumeResult() { ... }

    [Fact]
    public async Task Handle_NotFound_PropagatesNotFoundException() { ... }

    [Fact]
    public async Task Handle_WrongUser_ThrowsNotFoundException() { ... }
}
```

### Integration Test Soft-Delete Helper

For the "soft-deleted → 404" integration test, there is currently no soft-delete endpoint (story 2.5 is backlog). Use the EF InMemory DB directly via `WebApplicationFactory` overrides **or** simply verify the controller returns 404 for a non-existent ID as a proxy — and add the full soft-delete regression test in story 2.5 instead. Document this deferred decision in the Dev Agent Record.

### File Locations

| File | Action |
| ---- | ------ |
| `backend/src/JobNecto.Application/Resumes/GetResumeQuery.cs` | Create new |
| `backend/src/JobNecto.Application/Resumes/GetResumeQueryHandler.cs` | Create new |
| `backend/src/JobNecto.API/Controllers/ResumesController.cs` | Add action to existing |
| `backend/tests/JobNecto.Tests/Application/Resumes/GetResumeHandlerTests.cs` | Create new |
| `backend/tests/JobNecto.Tests/API/ResumesControllerTests.cs` | Add tests to existing class |

### No New Infrastructure Needed

- No new repository interface methods.
- No new migration (no schema change).
- No new DI registration (handler auto-registered by MediatR assembly scan).

### References

- [Source: `_bmad-output/archive/planning-artifacts/epics/epic-2-resume-education-management.md` — Story 2.3]
- [Source: `backend/src/JobNecto.Application/Interfaces/IRepository.cs` — `GetByIdAsync`]
- [Source: `backend/src/JobNecto.Infrastructure/Repositories/BaseRepository.cs` — `GetByIdAsync` implementation]
- [Source: `backend/src/JobNecto.API/Controllers/ResumesController.cs` — auth + mediator pattern]
- [Source: `backend/src/JobNecto.Application/Resumes/Mappers/ResumeMappers.cs` — `ToResumeResult()`]
- [Source: `backend/src/JobNecto.Application/Exceptions/NotFoundException.cs`]
- [Source: `backend/src/JobNecto.API/Infrastructure/ExceptionHandling/GlobalExceptionHandler.cs`]
- [Source: `backend/tests/JobNecto.Tests/Application/Resumes/ListResumesHandlerTests.cs` — test structure]
- [Source: `backend/tests/JobNecto.Tests/API/ResumesControllerTests.cs` — integration test helpers]

## Dev Agent Record

### Agent Model Used

GitHub Copilot (Claude Sonnet 4.6)

### Debug Log References

### Completion Notes List

- Implemented `GetResumeQuery` + `GetResumeQueryHandler` — handler uses `GetByIdAsync` (which respects global soft-delete filter) then enforces ownership with `NotFoundException` (not `ForbiddenException`) to prevent info leak.
- Added `GetAsync([FromRoute] Guid id)` to `ResumesController` — follows exact same auth pattern as `Create` and `ListAsync`.
- Soft-delete 404 regression test deferred to story 2.5 (no `DELETE` endpoint yet); non-existent GUID test covers the not-found code path.
- All 203 tests pass (0 failures, 0 regressions). Test suite grew from 199 → 203.

### Review Findings

- [x] [Review][Patch] Missing null assertion before null-forgiving operator in cross-user test [`backend/tests/JobNecto.Tests/API/ResumesControllerTests.cs` — `GetById_ResumeBelongingToDifferentUser_Returns404`] — `created` is used as `created!.Id` without a prior `.Should().NotBeNull()` guard; if the create step fails, the test throws NPE instead of surfacing the actual failure.
- [x] [Review][Defer] AC 4 soft-delete integration test deferred to story 2.5 [`_bmad-output/archive/implementation-artifacts/2-3-get-resume-detail.md` — Tasks section] — deferred, pre-existing (no DELETE endpoint exists yet; documented in story)
- [x] [Review][Defer] Entity fetched from DB before ownership check in handler [`backend/src/JobNecto.Application/Resumes/GetResumeQueryHandler.cs`] — deferred, pre-existing (BaseRepository.GetByIdAsync is a generic method; ownership-aware query would require repository interface change outside this story's scope)

### File List

- `_bmad-output/archive/implementation-artifacts/2-3-get-resume-detail.md`
- `backend/src/JobNecto.Application/Resumes/GetResumeQuery.cs`
- `backend/src/JobNecto.Application/Resumes/GetResumeQueryHandler.cs`
- `backend/src/JobNecto.API/Controllers/ResumesController.cs`
- `backend/tests/JobNecto.Tests/Application/Resumes/GetResumeHandlerTests.cs`
- `backend/tests/JobNecto.Tests/API/ResumesControllerTests.cs`

