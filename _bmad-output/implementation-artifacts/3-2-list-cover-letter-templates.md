# Story 3.2: List Cover Letter Templates

Status: done

## Story

As a job seeker,
I want to browse and search my template library,
so that I can find the right template for a job application.

## Acceptance Criteria

1. `GET /api/v1/cover-letter-templates` requires a valid JWT token. Unauthenticated requests return `401 Unauthorized`.
2. Given a valid JWT token and no query params, returns `200 OK` with envelope `{ totalCount, pageSize, hasNext, lastSeenId, lastSeenUpdatedAt, items }` containing only non-deleted templates owned by the authenticated user, ordered by `updatedAt desc`.
3. Each item in `items` includes `id`, `name`, `createdAt`, `updatedAt`, and `contentPreview` (first 200 chars of `content`). Full `content` is **NOT** returned in the list.
4. Given `?search=senior`, only templates whose `name` contains "senior" (case-insensitive) are returned.
5. Given `pageSize`, `lastSeenId`, and `lastSeenUpdatedAt` cursor params, returns the correct cursor window; `pageSize` is capped at 100.
6. Templates belonging to another user are never returned.

## Tasks / Subtasks

- [x] Task 1: Add `Search` to `PagedQuery` and `ApplyAdditionalFilters` hook to `BaseRepository` (AC: 4)
  - [x] **FIRST:** Add `public string? Search { get; init; } = null;` to `PagedQuery` in `backend/src/JobNecto.Domain/ValueObjects/Pagination.cs` — required before all downstream tasks
  - [x] Add `protected virtual IQueryable<T> ApplyAdditionalFilters(IQueryable<T> query, PagedQuery pagedQuery)` virtual method to `BaseRepository<T>` (returns query unchanged by default)
  - [x] Call `ApplyAdditionalFilters` in `BaseRepository.GetAsync` on the line **immediately after the closing brace of the `UserId` filter block and immediately before the `var totalCount = await query.CountAsync(ct);` line** (line 59 in current file). This ensures the total count reflects search-filtered results.

- [x] Task 2: Override search in `CoverLetterTemplateRepository` (AC: 4)
  - [x] Override `ApplyAdditionalFilters` in `CoverLetterTemplateRepository`: when `pagedQuery.Search` is non-null/empty, filter `t.Name.ToLower().Contains(pagedQuery.Search.ToLower())`
  - [x] Use `ToLower().Contains(...)` pattern — translates to SQL and works with InMemory provider

- [x] Task 3: Define `CoverLetterTemplateListItemResult` DTO and mapper (AC: 3)
  - [x] Add `CoverLetterTemplateListItemResult` class to `backend/src/JobNecto.Application/CoverLetterTemplates/ListCoverLetterTemplatesQuery.cs` (same file as the query — NOT in `CreateCoverLetterTemplateCommand.cs`):
    - Properties: `Id` (Guid), `Name` (string), `ContentPreview` (string), `CreatedAt` (DateTime), `UpdatedAt` (DateTime)
    - No `UserId` in list items — not needed per AC
  - [x] Add `ToCoverLetterTemplateListItemResult(this CoverLetterTemplate template)` extension to `CoverLetterTemplateMappers.cs`
    - `ContentPreview = template.Content.Length <= 200 ? template.Content : template.Content[..200]`

- [x] Task 4: Create list query and handler (AC: 2, 4, 5)
  - [x] Create `backend/src/JobNecto.Application/CoverLetterTemplates/ListCoverLetterTemplatesQuery.cs` — define BOTH `ListCoverLetterTemplatesQuery` and `CoverLetterTemplateListItemResult` in this file:
    - On query class: `[JsonIgnore] public Guid UserId { get; set; }` — `[JsonIgnore]` prevents `UserId` from being bound from the HTTP query string; it is injected by the controller
    - `public int PageSize { get; set; } = 20;`
    - `public Guid? LastSeenId { get; set; }`
    - `public DateTime? LastSeenUpdatedAt { get; set; }`
    - `public string? Search { get; set; }`
    - Implements `IRequest<PagedResult<CoverLetterTemplateListItemResult>>`
  - [x] Create `backend/src/JobNecto.Application/CoverLetterTemplates/ListCoverLetterTemplatesQueryHandler.cs`:
    - Cap pageSize: `var cappedPageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, 100);`
    - Build `PagedQuery` with `UserId`, `cappedPageSize`, cursor, and `Search = request.Search`
    - Call `_unitOfWork.CoverLetterTemplateRepository.GetAsync(pagedQuery, cancellationToken)`
    - Project items with `t.ToCoverLetterTemplateListItemResult()`
    - Return `PagedResult<CoverLetterTemplateListItemResult>` using same 6-arg constructor as `ListResumesQueryHandler`

- [x] Task 5: Expose authenticated GET endpoint (AC: 1, 2, 3, 4, 5, 6)
  - [x] Add `List` action to `backend/src/JobNecto.API/Controllers/CoverLetterTemplatesController.cs`:
    - `[HttpGet]`
    - `[ProducesResponseType(typeof(PagedResult<CoverLetterTemplateListItemResult>), StatusCodes.Status200OK)]`
    - `[ProducesResponseType(StatusCodes.Status401Unauthorized)]`
    - Params: `[FromQuery] int pageSize = 20`, `[FromQuery] Guid? lastSeenId = null`, `[FromQuery] DateTime? lastSeenUpdatedAt = null`, `[FromQuery] string? search = null`, `CancellationToken cancellationToken = default`
    - UTC-normalize `lastSeenUpdatedAt` (copy from `EducationsController.ListAsync` exactly)
    - Extract `UserId` via `HttpContext.GetCurrentUserId()` — return `Unauthorized()` on failure
    - Set `query.UserId = userId; query.Search = search;` and dispatch via `_mediator.Send`

- [x] Task 6: Add comprehensive tests (AC: 1–6)
  - [x] Add list tests to `backend/tests/JobNecto.Tests/API/CoverLetterTemplates/CoverLetterTemplatesApiTests.cs`:
    - `List_WithoutToken_Returns401`
    - `List_EmptyLibrary_Returns200WithEmptyResult` — `{ totalCount: 0, hasNext: false, items: [] }`
    - `List_ReturnsOnlyCurrentUsersTemplates` — User B sees empty list when User A created templates
    - `List_ReturnsItemsWithContentPreview` — creates template with 300-char content, verifies `contentPreview` is 200 chars (not full content)
    - `List_SearchMatchesName_ReturnsFilteredResults` — creates "Senior Developer" and "Junior Analyst", search "senior" returns 1 item
    - `List_SearchIsCaseInsensitive` — creates "Senior Developer", search "SENIOR" returns 1 item
    - `List_SearchNoMatch_ReturnsEmpty` — search "zzznomatch" returns 0 items
    - `List_SoftDeletedTemplatesNotVisible` — seed a template directly via InMemory DB, set `IsDeleted = true`, call `SaveChanges()`. The EF Core global query filter (`!IsDeleted`) applies to InMemory provider (EF Core 5+), so the list query naturally excludes it. No `IgnoreQueryFilters()` needed.
  - [x] Create `backend/tests/JobNecto.Tests/Application/CoverLetterTemplates/ListCoverLetterTemplatesQueryHandlerTests.cs`:
    - `Handle_NoTemplates_ReturnsEmptyPagedResult`
    - `Handle_WithTemplates_ReturnsProjectedListItems` — items have `ContentPreview` not full `Content`
    - `Handle_ContentOver200Chars_ContentPreviewTruncatedTo200`
    - `Handle_ContentExactly200Chars_ContentPreviewNotTruncated`
    - `Handle_PageSizeCappedAt100` — `PageSize = 200` → result.PageSize == 100
    - `Handle_PageSizeLessThan1_DefaultsTo20`
  - [x] Run targeted tests: `dotnet test backend/JobNecto.slnx --filter "FullyQualifiedName~CoverLetterTemplates"`
  - [x] Run full suite: `dotnet test backend/JobNecto.slnx`
  - [x] Run CI parity: `dotnet build backend/JobNecto.slnx --configuration Release --warnaserror` then `dotnet test backend/JobNecto.slnx --configuration Release --no-build --warnaserror`

## Dev Notes

### Entity State — What Already Exists After Story 3.1

`CoverLetterTemplate` at `backend/src/JobNecto.Domain/Entities/CoverLetterTemplate.cs`:
```csharp
public class CoverLetterTemplate : SoftDeletableEntity
{
    public Guid UserId;           // PUBLIC FIELD, not auto-property
    public required string Name;  // PUBLIC FIELD, not auto-property
    public required string Content; // PUBLIC FIELD, not auto-property
}
```
- **CRITICAL:** `UserId`, `Name`, `Content` are **fields**, not properties. LINQ queries against fields work in EF Core (both InMemory and PostgreSQL). Use `t.Name.ToLower()` directly — EF Core Npgsql translates this.
- EF Core global soft-delete query filter (`!IsDeleted`) is already applied to `CoverLetterTemplate` — soft-deleted templates are automatically excluded from all queries without additional code.

`CoverLetterTemplateRepository` at `backend/src/JobNecto.Infrastructure/Repositories/CoverLetterTemplateRepository.cs`:
- Inherits `SoftDeletableRepository<CoverLetterTemplate>` → `EditableRepository` → `BaseRepository`
- Currently empty (no overrides). This story adds `ApplyAdditionalFilters` override.

`CoverLetterTemplateRepository` is exposed in `IUnitOfWork` as `IMutableRepository<CoverLetterTemplate> CoverLetterTemplateRepository` — already wired after story 3.1.

`CoverLetterTemplateResult` at `backend/src/JobNecto.Application/CoverLetterTemplates/CreateCoverLetterTemplateCommand.cs`:
- Has full `Content` field — **do not use for list endpoint**
- Story 3.2 adds a new `CoverLetterTemplateListItemResult` class to this same file

`CoverLetterTemplateMappers` at `backend/src/JobNecto.Application/CoverLetterTemplates/Mappers/CoverLetterTemplateMappers.cs`:
- Already has `ToEntity` and `ToCoverLetterTemplateResult`. Story 3.2 adds `ToCoverLetterTemplateListItemResult`.

### BaseRepository Extension Pattern

Add to `BaseRepository<T>.GetAsync` AFTER the UserId filter block and BEFORE `CountAsync`:
```csharp
// Apply entity-specific additional filters (subclass hook)
query = ApplyAdditionalFilters(query, pagedQuery);
```

Add virtual method at end of `BaseRepository<T>`:
```csharp
/// <summary>
/// Override in a derived repository to apply additional entity-specific filters.
/// Called after UserId scoping and before CountAsync/ordering.
/// </summary>
protected virtual IQueryable<T> ApplyAdditionalFilters(IQueryable<T> query, PagedQuery pagedQuery)
{
    return query;
}
```

### CoverLetterTemplateRepository Override

```csharp
protected override IQueryable<CoverLetterTemplate> ApplyAdditionalFilters(
    IQueryable<CoverLetterTemplate> query, PagedQuery pagedQuery)
{
    if (!string.IsNullOrWhiteSpace(pagedQuery.Search))
        query = query.Where(t => t.Name.ToLower().Contains(pagedQuery.Search.ToLower()));

    return query;
}
```

**Why `ToLower().Contains()` instead of `EF.Functions.ILike`:**
- `EF.Functions.ILike` is PostgreSQL-specific — throws on InMemory provider used in unit/integration tests
- `t.Name.ToLower().Contains(search.ToLower())` works in EF Core InMemory (pure LINQ evaluation) and translates to `lower(name) LIKE '%' || lower(@search) || '%'` in PostgreSQL (case-insensitive)

### Handler Pattern

Follow `ListEducationsQueryHandler` exactly:
```csharp
public async Task<PagedResult<CoverLetterTemplateListItemResult>> Handle(
    ListCoverLetterTemplatesQuery request, CancellationToken cancellationToken)
{
    var cappedPageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, 100);

    var pagedQuery = new PagedQuery
    {
        UserId = request.UserId,
        PageSize = cappedPageSize,
        LastSeenId = request.LastSeenId,
        LastSeenUpdatedAt = request.LastSeenUpdatedAt,
        Search = request.Search,
    };

    var result = await _unitOfWork.CoverLetterTemplateRepository.GetAsync(pagedQuery, cancellationToken);

    var projectedItems = result.Items.Select(t => t.ToCoverLetterTemplateListItemResult()).ToList();

    return new PagedResult<CoverLetterTemplateListItemResult>(
        projectedItems,
        result.TotalCount,
        result.LastSeenId,
        result.LastSeenUpdatedAt,
        result.PageSize,
        result.HasNext
    );
}
```

### Controller Pattern

Follow `EducationsController.ListAsync` exactly for the GET action — especially the UTC normalization of `lastSeenUpdatedAt`:
```csharp
[HttpGet]
[ProducesResponseType(typeof(PagedResult<CoverLetterTemplateListItemResult>), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public async Task<ActionResult<PagedResult<CoverLetterTemplateListItemResult>>> ListAsync(
    [FromQuery] int pageSize = 20,
    [FromQuery] Guid? lastSeenId = null,
    [FromQuery] DateTime? lastSeenUpdatedAt = null,
    [FromQuery] string? search = null,
    CancellationToken cancellationToken = default)
{
    var userIdValue = HttpContext.GetCurrentUserId();
    if (string.IsNullOrWhiteSpace(userIdValue) || !Guid.TryParse(userIdValue, out var userId))
        return Unauthorized();

    if (lastSeenUpdatedAt.HasValue)
    {
        lastSeenUpdatedAt =
            lastSeenUpdatedAt.Value.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(lastSeenUpdatedAt.Value, DateTimeKind.Utc)
                : lastSeenUpdatedAt.Value.ToUniversalTime();
    }

    var query = new ListCoverLetterTemplatesQuery
    {
        UserId = userId,
        PageSize = pageSize,
        LastSeenId = lastSeenId,
        LastSeenUpdatedAt = lastSeenUpdatedAt,
        Search = search,
    };

    var result = await _mediator.Send(query, cancellationToken);
    return Ok(result);
}
```

**Why UTC-normalize `lastSeenUpdatedAt`:** Lesson 2026-04-28 — cursor timestamp fields require `DateTimeKind` normalization at the API boundary to prevent strict equality failures in the repository cursor comparison.

### Mapper for List Item

```csharp
/// <summary>
/// Maps a domain entity to the list-item API response DTO.
/// Returns a content preview (first 200 chars) instead of the full content.
/// </summary>
public static CoverLetterTemplateListItemResult ToCoverLetterTemplateListItemResult(
    this CoverLetterTemplate template)
{
    if (template == null)
        throw new ArgumentNullException(nameof(template));

    return new CoverLetterTemplateListItemResult
    {
        Id = template.Id,
        Name = template.Name,
        ContentPreview = template.Content.Length <= 200
            ? template.Content
            : template.Content[..200],
        CreatedAt = template.CreatedAt,
        UpdatedAt = template.UpdatedAt,
    };
}
```

### Test Patterns

Copy `CreateUserAndGetCookieHelperAsync` and `PostTemplateAsync` from existing `CoverLetterTemplatesApiTests` — they are already `internal static` methods on the class.

Add a `GetTemplatesAsync` helper method:
```csharp
private static async Task<HttpResponseMessage> GetTemplatesAsync(
    HttpClient client,
    string authCookie,
    string queryString = "")
{
    var url = "/api/v1/cover-letter-templates" +
              (string.IsNullOrEmpty(queryString) ? "" : "?" + queryString);
    var request = new HttpRequestMessage(HttpMethod.Get, url);
    request.Headers.TryAddWithoutValidation("Cookie", authCookie);
    return await client.SendAsync(request);
}
```

Inner DTO for list deserialization:
```csharp
private class PagedResultDto
{
    public int TotalCount { get; set; }
    public int PageSize { get; set; }
    public bool HasNext { get; set; }
    public Guid? LastSeenId { get; set; }
    public DateTime? LastSeenUpdatedAt { get; set; }
    public List<CoverLetterTemplateListItemDto> Items { get; set; } = [];
}

private class CoverLetterTemplateListItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string ContentPreview { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

**Test data constraints (lesson 2026-04-25):**
- `name`: any non-empty string ≤ 100 chars
- `content` for valid payloads: 50-10,000 chars
- For the `ContentPreview` truncation test: use content of exactly 300 chars; verify `ContentPreview.Length == 200`
- `LoginName` prefix ≤ 12 chars, alphanumeric + underscore only

**Handler unit tests (InMemory DB):**
```csharp
// In handler tests, use unique DB name per test: Guid.NewGuid().ToString()
var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseInMemoryDatabase(Guid.NewGuid().ToString())
    .Options;
```

**Soft-deleted template test:** DELETE endpoint (story 3.5) is not implemented yet. Use InMemory DB directly: seed a template, set `IsDeleted = true`, call `SaveChanges()`. EF Core global query filter (`!IsDeleted`) applies in InMemory (EF Core 5+) — the list query will naturally exclude the deleted entity without any `IgnoreQueryFilters()` call.

### File Structure Requirements

| File | Action |
|------|--------|
| `backend/src/JobNecto.Domain/ValueObjects/Pagination.cs` | UPDATE — add `Search` field to `PagedQuery` |
| `backend/src/JobNecto.Infrastructure/Repositories/BaseRepository.cs` | UPDATE — add `ApplyAdditionalFilters` virtual hook; call it in `GetAsync` |
| `backend/src/JobNecto.Infrastructure/Repositories/CoverLetterTemplateRepository.cs` | UPDATE — override `ApplyAdditionalFilters` for name search |
| `backend/src/JobNecto.Application/CoverLetterTemplates/CreateCoverLetterTemplateCommand.cs` | NO CHANGE — `CoverLetterTemplateListItemResult` goes in the query file, not here |
| `backend/src/JobNecto.Application/CoverLetterTemplates/Mappers/CoverLetterTemplateMappers.cs` | UPDATE — add `ToCoverLetterTemplateListItemResult` |
| `backend/src/JobNecto.Application/CoverLetterTemplates/ListCoverLetterTemplatesQuery.cs` | NEW — contains both `ListCoverLetterTemplatesQuery` and `CoverLetterTemplateListItemResult` |
| `backend/src/JobNecto.Application/CoverLetterTemplates/ListCoverLetterTemplatesQueryHandler.cs` | NEW |
| `backend/src/JobNecto.API/Controllers/CoverLetterTemplatesController.cs` | UPDATE — add `ListAsync` GET action |
| `backend/tests/JobNecto.Tests/Application/CoverLetterTemplates/ListCoverLetterTemplatesQueryHandlerTests.cs` | NEW |
| `backend/tests/JobNecto.Tests/API/CoverLetterTemplates/CoverLetterTemplatesApiTests.cs` | UPDATE — add list tests |
| `_bmad-output/implementation-artifacts/sprint-status.yaml` | UPDATE — status to ready-for-dev |

Do **not** modify Resume, Education, or any other entity handlers, repositories, or controllers.

### Namespace Conventions

- `JobNecto.Application.CoverLetterTemplates` — query and handler files
- `JobNecto.Application.CoverLetterTemplates.Mappers` — mapper file
- `JobNecto.Infrastructure.Repositories` — CoverLetterTemplateRepository override
- `JobNecto.Domain.ValueObjects` — Pagination value objects
- `JobNecto.API.Controllers` — controller file
- `JobNecto.Tests.API.CoverLetterTemplates` — API test files
- `JobNecto.Tests.Application.CoverLetterTemplates` — handler test file

### Previous Story Intelligence

- **Separate handler file** — `ListCoverLetterTemplatesQuery.cs` and `ListCoverLetterTemplatesQueryHandler.cs` are separate files (lesson 2026-04-28)
- **Set timestamps in handler** — not needed for list/read paths; only required for create/update handlers
- **Do not null-check after `GetByIdAsync`** — not applicable here (list uses `GetAsync`)
- **`[JsonIgnore]` on `UserId`** — must be set on the query's `UserId` property so it is not bound from the request body/query string
- **UTC cursor normalization** — copy from `EducationsController.ListAsync` verbatim (lesson 2026-04-28)
- **Test data validator-compliant** — `content` must be 50-10,000 chars for setup; `LoginName` prefix ≤ 12 chars, no hyphens (lesson 2026-04-25)
- **EF query filter covers soft-delete** — global filter on `CoverLetterTemplate` already excludes `IsDeleted = true` records; no additional `Where(!IsDeleted)` needed in repository override

### References

- [Epic 3 source](_bmad-output/planning-artifacts/epics/epic-3-cover-letter-template-library.md) — Story 3.2 ACs
- [Core architectural decisions](_bmad-output/planning-artifacts/architecture/core-architectural-decisions.md) — MediatR, pagination, ownership patterns
- [Story 3.1 dev notes](3-1-create-cover-letter-template.md) — entity field vs property gotcha, test patterns, cookie auth
- [Pattern: ListEducationsQueryHandler](backend/src/JobNecto.Application/Educations/ListEducationsQueryHandler.cs)
- [Pattern: EducationsController.ListAsync](backend/src/JobNecto.API/Controllers/EducationsController.cs)
- [Pattern: BaseRepository.GetAsync](backend/src/JobNecto.Infrastructure/Repositories/BaseRepository.cs)
- [Pattern: CoverLetterTemplatesApiTests](backend/tests/JobNecto.Tests/API/CoverLetterTemplates/CoverLetterTemplatesApiTests.cs)

## Dev Agent Record

### Agent Model Used

claude-haiku-4-5-20251001

### Debug Log References

- Pre-existing CS8602 warnings in `CoverLetterTemplatesUniquenessApiTests.cs` became errors under `--warnaserror`. Fixed by adding `!` null-forgiving operators to the three `_factory.TryInitializeSchemaAsync()` calls (lines 48, 75, 103). These were latent from story 3.1 and undetected because that CI run used an older compiler/configuration.

### Completion Notes List

- Task 1: Added `Search?` to `PagedQuery` and `ApplyAdditionalFilters` virtual hook to `BaseRepository<T>`. Hook is called after UserId filter, before `CountAsync` — ensures search-filtered total counts are correct.
- Task 2: Overrode `ApplyAdditionalFilters` in `CoverLetterTemplateRepository` using `t.Name.ToLower().Contains(search.ToLower())` — works in both InMemory (LINQ evaluation) and PostgreSQL (translates to `lower(name) LIKE ...`).
- Task 3: Created `CoverLetterTemplateListItemResult` (with `ContentPreview`, not full `Content`) and `ToCoverLetterTemplateListItemResult` mapper. Truncates to 200 chars using range indexer.
- Task 4: `ListCoverLetterTemplatesQuery` + `ListCoverLetterTemplatesQueryHandler` follow the `ListEducationsQueryHandler` pattern exactly. PageSize capped at 100, defaulted to 20.
- Task 5: `ListAsync` GET action on `CoverLetterTemplatesController` follows `EducationsController.ListAsync` exactly — UTC-normalizes `lastSeenUpdatedAt` at API boundary.
- Task 6: 18 new tests added (8 API integration + 8 handler unit + 2 bonus). 326/326 pass. CI parity: 0 warnings, 0 errors.

AC coverage:
- AC1: `[Authorize]` on controller + `List_WithoutToken_Returns401` ✅
- AC2: `GET /api/v1/cover-letter-templates` returns 200 + correct envelope ✅
- AC3: items have `contentPreview` (200-char max), not full `content` ✅
- AC4: `?search=senior` — case-insensitive name filter (`List_SearchMatchesName_ReturnsFilteredResults`, `List_SearchIsCaseInsensitive`) ✅
- AC5: `pageSize` capped at 100, cursor params wired through ✅
- AC6: user-scoped list — `List_ReturnsOnlyCurrentUsersTemplates` ✅

### File List

- `backend/src/JobNecto.Domain/ValueObjects/Pagination.cs` — updated: added `Search` to `PagedQuery`
- `backend/src/JobNecto.Infrastructure/Repositories/BaseRepository.cs` — updated: added `ApplyAdditionalFilters` virtual hook + call in `GetAsync`
- `backend/src/JobNecto.Infrastructure/Repositories/CoverLetterTemplateRepository.cs` — updated: overrides `ApplyAdditionalFilters` for case-insensitive name search
- `backend/src/JobNecto.Application/CoverLetterTemplates/ListCoverLetterTemplatesQuery.cs` — new: `ListCoverLetterTemplatesQuery` + `CoverLetterTemplateListItemResult`
- `backend/src/JobNecto.Application/CoverLetterTemplates/ListCoverLetterTemplatesQueryHandler.cs` — new
- `backend/src/JobNecto.Application/CoverLetterTemplates/Mappers/CoverLetterTemplateMappers.cs` — updated: added `ToCoverLetterTemplateListItemResult`
- `backend/src/JobNecto.API/Controllers/CoverLetterTemplatesController.cs` — updated: added `ListAsync` GET action
- `backend/tests/JobNecto.Tests/Application/CoverLetterTemplates/ListCoverLetterTemplatesQueryHandlerTests.cs` — new: 8 unit tests
- `backend/tests/JobNecto.Tests/API/CoverLetterTemplates/CoverLetterTemplatesApiTests.cs` — updated: 8 integration tests added
- `backend/tests/JobNecto.Tests/API/CoverLetterTemplates/CoverLetterTemplatesUniquenessApiTests.cs` — patched: fixed pre-existing CS8602 nullable warnings
- `_bmad-output/implementation-artifacts/3-2-list-cover-letter-templates.md` — this file
- `_bmad-output/implementation-artifacts/sprint-status.yaml` — updated: status to review

## Change Log

- 2026-05-08: Implemented story 3.2 — GET /api/v1/cover-letter-templates endpoint with JWT auth, cursor pagination, case-insensitive name search, contentPreview truncation (200 chars). Added `Search` to `PagedQuery`, `ApplyAdditionalFilters` extensibility hook to `BaseRepository`, and 18 new tests (326 total, 0 failures). Fixed pre-existing CS8602 nullable warnings in `CoverLetterTemplatesUniquenessApiTests.cs`.

## Story Completion Status

- Ultimate context engine analysis completed - comprehensive developer guide created.
