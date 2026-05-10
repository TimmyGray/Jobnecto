# Story 4.3: Get Vacancy Detail

Status: ready-for-dev

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a job seeker,
I want to view all details of a specific vacancy,
so that I can decide whether to apply.

## Acceptance Criteria

1. Given a valid JWT token and a vacancy ID that exists, when `GET /api/v1/vacancies/{id}` is called, then `200 OK` with all fields: `id`, `title`, `description`, `company`, `skills`, `workLocationType`, `location`, `salary`, `currency`, `matchScore`, `jobSource`, `categories`, `experienceLevel`, `createdAt`.
2. Given the vacancy ID does not exist (or is soft-deleted), when `GET /api/v1/vacancies/{id}` is called, then `404 Not Found`.
3. Given no valid JWT token, when `GET /api/v1/vacancies/{id}` is called, then `401 Unauthorized`.

## Tasks / Subtasks

- [ ] Task 1: Create the query and result DTOs (AC: 1)
  - [ ] Create `backend/src/JobNecto.Application/Vacancies/GetVacancyQuery.cs` with:
    - `GetVacancyQuery : IRequest<VacancyDetailResult>` — fields: `VacancyId` (Guid), `UserId` (Guid)
    - `VacancyDetailResult` — fields: `Id`, `Title`, `Description`, `Company`, `Skills`, `WorkLocationType`, `Location`, `Salary` (VacancySalaryResult), `Currency`, `MatchScore`, `JobSource` (VacancyJobSourceResult), `Categories`, `ExperienceLevel`, `CreatedAt`
    - `VacancyJobSourceResult` — fields: `Name` (string), `Url` (string?)

- [ ] Task 2: Create the query handler (AC: 1, 2)
  - [ ] Create `backend/src/JobNecto.Application/Vacancies/GetVacancyQueryHandler.cs`
  - [ ] Call `_unitOfWork.VacancyRepository.GetByIdAsync(request.VacancyId, cancellationToken)` — throws `NotFoundException` automatically if not found (soft-deleted vacancies excluded by global query filter)
  - [ ] No user ownership check — per FR26, any authenticated user can retrieve any vacancy by ID
  - [ ] Map entity to `VacancyDetailResult` using `vacancy.ToVacancyDetailResult()`

- [ ] Task 3: Add mapper (AC: 1)
  - [ ] Add `ToVacancyDetailResult()` extension method to `backend/src/JobNecto.Application/Vacancies/Mappers/VacancyMappers.cs`
  - [ ] Map all AC-specified fields: id, title, description, company, skills, workLocationType, location, salary (VacancySalaryResult), currency, matchScore, jobSource (VacancyJobSourceResult), categories, experienceLevel, createdAt

- [ ] Task 4: Add controller action (AC: 1, 2, 3)
  - [ ] Add `[HttpGet("{id:guid}")]` action to `backend/src/JobNecto.API/Controllers/VacanciesController.cs`:
    ```csharp
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(VacancyDetailResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VacancyDetailResult>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var userIdValue = HttpContext.GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userIdValue) || !Guid.TryParse(userIdValue, out var userId))
            return Unauthorized();

        var result = await _mediator.Send(
            new GetVacancyQuery { VacancyId = id, UserId = userId },
            cancellationToken);

        return Ok(result);
    }
    ```
  - [ ] XML doc: `/// <summary>Returns full detail of a vacancy by ID.</summary>` + params

- [ ] Task 5: Add handler unit tests (AC: 1, 2)
  - [ ] Create `backend/tests/JobNecto.Tests/Application/Vacancies/GetVacancyQueryHandlerTests.cs`
  - [ ] `Handle_ExistingVacancy_ReturnsMappedDetail` — verify all DTO fields populated
  - [ ] `Handle_VacancyWithNoSalary_ReturnNullSalary` — salary null when SalaryMin/Max both null
  - [ ] `Handle_PropagatesNotFoundExceptionFromRepository` — mock throws `NotFoundException`, verify it bubbles up

- [ ] Task 6: Add API integration tests (AC: 1, 2, 3)
  - [ ] `GetById_WithoutToken_Returns401`
  - [ ] `GetById_ExistingVacancy_Returns200WithAllFields` — seed one vacancy, call GET, assert each field in the response
  - [ ] `GetById_NonExistentId_Returns404`
  - [ ] File: `backend/tests/JobNecto.Tests/API/Vacancies/VacanciesApiTests.cs`

- [ ] Task 7: Verification gates
  - [ ] Targeted: `dotnet test backend/JobNecto.slnx --filter "FullyQualifiedName~Vacanc"`
  - [ ] Full suite: `dotnet test backend/JobNecto.slnx`
  - [ ] CI parity build: `dotnet build backend/JobNecto.slnx --configuration Release --warnaserror`
  - [ ] CI parity tests: `dotnet test backend/JobNecto.slnx --configuration Release --no-build --warnaserror`

## Dev Notes

### New Files to Create

| File | Purpose |
|---|---|
| `backend/src/JobNecto.Application/Vacancies/GetVacancyQuery.cs` | Query class + VacancyDetailResult + VacancyJobSourceResult DTOs |
| `backend/src/JobNecto.Application/Vacancies/GetVacancyQueryHandler.cs` | Handler |
| `backend/tests/JobNecto.Tests/Application/Vacancies/GetVacancyQueryHandlerTests.cs` | Handler unit tests |

### Files to Modify

| File | Change |
|---|---|
| `backend/src/JobNecto.Application/Vacancies/Mappers/VacancyMappers.cs` | Add `ToVacancyDetailResult()` extension |
| `backend/src/JobNecto.API/Controllers/VacanciesController.cs` | Add `[HttpGet("{id:guid}")]` action |
| `backend/tests/JobNecto.Tests/API/Vacancies/VacanciesApiTests.cs` | Add 3 API integration tests |

### Handler Pattern (follow GetCoverLetterTemplateQueryHandler exactly)

Reference: `backend/src/JobNecto.Application/CoverLetterTemplates/GetCoverLetterTemplateQueryHandler.cs`

Key differences for story 4.3:
- No ownership check — FR26 says "any authenticated user" can retrieve any vacancy by ID
- Use `_unitOfWork.VacancyRepository.GetByIdAsync(request.VacancyId, ct)` which inherits from `BaseRepository<T>`
- `BaseRepository.GetByIdAsync` throws `NotFoundException` if entity not found (null check → throw)
- Global EF soft-delete query filter on `Vacancy` (`HasQueryFilter(v => !v.IsDeleted)`) ensures soft-deleted vacancies appear as "not found"
- `ExceptionHandlingMiddleware` maps `NotFoundException` → 404 — controller action does NOT need explicit null check

```csharp
public async Task<VacancyDetailResult> Handle(GetVacancyQuery request, CancellationToken cancellationToken)
{
    var vacancy = await _unitOfWork.VacancyRepository.GetByIdAsync(request.VacancyId, cancellationToken);
    return vacancy.ToVacancyDetailResult();
}
```

### Query DTO Design

```csharp
public class GetVacancyQuery : IRequest<VacancyDetailResult>
{
    public Guid VacancyId { get; init; }
    public Guid UserId { get; init; }  // set by controller; not used for ownership filter per FR26
}

public class VacancyDetailResult
{
    public Guid Id { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Company { get; set; }
    public string[]? Skills { get; set; }
    public string? WorkLocationType { get; set; }
    public string? Location { get; set; }
    public VacancySalaryResult? Salary { get; set; }   // reuse existing DTO
    public string? Currency { get; set; }
    public double? MatchScore { get; set; }
    public VacancyJobSourceResult? JobSource { get; set; }
    public string[]? Categories { get; set; }
    public string? ExperienceLevel { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class VacancyJobSourceResult
{
    public string Name { get; set; } = string.Empty;
    public string? Url { get; set; }
}
```

**Note:** `VacancySalaryResult` already exists in `FilterVacanciesQuery.cs` — reuse it, don't define a new one. Reference: `namespace JobNecto.Application.Vacancies`.

### Mapper Implementation

Add to `VacancyMappers.cs` in `namespace JobNecto.Application.Vacancies.Mappers`:

```csharp
public static VacancyDetailResult ToVacancyDetailResult(this Vacancy vacancy)
{
    ArgumentNullException.ThrowIfNull(vacancy);

    return new VacancyDetailResult
    {
        Id = vacancy.Id,
        Title = vacancy.Title,
        Description = vacancy.Description,
        Company = vacancy.Company,
        Skills = vacancy.Skills,
        WorkLocationType = vacancy.WorkLocationType?.ToString(),
        Location = vacancy.Location?.ToString(),
        Salary = vacancy.SalaryMin.HasValue || vacancy.SalaryMax.HasValue
            ? new VacancySalaryResult { Min = vacancy.SalaryMin, Max = vacancy.SalaryMax }
            : null,
        Currency = vacancy.Currency?.ToString(),
        MatchScore = vacancy.MatchScore,
        JobSource = new VacancyJobSourceResult
        {
            Name = vacancy.JobSource.Name,
            Url = vacancy.JobSource.Url,
        },
        Categories = vacancy.JobCategories,
        ExperienceLevel = vacancy.ExperienceLevel,
        CreatedAt = vacancy.CreatedAt,
    };
}
```

### Controller Action (follow CoverLetterTemplatesController.GetAsync exactly)

Route pattern: `[HttpGet("{id:guid}")]` — the `:guid` constraint ensures non-GUID segments don't match.

`ProducesResponseType` attributes required:
- `200` with `typeof(VacancyDetailResult)`
- `401` (no type)
- `404` (no type — thrown as exception, not returned explicitly)

XML doc required on the action method — see `CoverLetterTemplatesController.GetAsync` for the exact format.

No `[FromBody]` parameter — the ID comes from the route.

### Vacancy Entity Mapping Notes

- `Vacancy.WorkLocationType` is `WorkLocationType?` (nullable enum) → `.ToString()` if not null
- `Vacancy.Location` is `Location?` (nullable enum) → `.ToString()` if not null
- `Vacancy.Currency` is `Currency?` (nullable enum) → `.ToString()` if not null
- `Vacancy.Skills` is `string[]?` — pass through directly (may be null)
- `Vacancy.JobCategories` is `string[]?` — maps to `Categories` in DTO
- `Vacancy.JobSource` is a required value object `JobSource { Name, Url }` — always present
- `JsonStringEnumConverter` is registered globally — enums serialize as strings automatically; the DTO uses `string?` so no special handling needed

### NotFoundException → 404 (middleware handles this)

`BaseRepository.GetByIdAsync` throws:
```csharp
throw new NotFoundException($"Entity with id {id} not found");
```

`ExceptionHandlingMiddleware` maps `NotFoundException` → `404 Not Found`. The controller action must NOT catch this exception — let it propagate to middleware. This matches the established pattern across all GET detail endpoints.

### Test: Seed Data for Integration Tests

For `GetById_ExistingVacancy_Returns200WithAllFields`, seed a single fully-populated vacancy from `VacancyTestData` (e.g., `VacancyTestData.SeniorDotNetRemotePoland`). Assert:
- `id` matches seeded ID
- `title` = "Senior Backend Engineer (.NET / Azure)"
- `skills` contains ".NET", "C#"
- `workLocationType` = "Remote"
- `location` = "Poland"
- `salary.min` = 95000
- `matchScore` = 0.91
- `jobSource.name` = "LinkedIn"
- `categories` contains "Engineering"
- `experienceLevel` = "Senior"
- `createdAt` != default

For `GetById_NonExistentId_Returns404`, just send `GET /api/v1/vacancies/{Guid.NewGuid()}` with a valid auth cookie.

### Auth Guard (identical to all other controller actions)

```csharp
var userIdValue = HttpContext.GetCurrentUserId();
if (string.IsNullOrWhiteSpace(userIdValue) || !Guid.TryParse(userIdValue, out var userId))
    return Unauthorized();
```

Always the first statement in the action method.

### Namespace Conventions (mandatory)

| File | Namespace |
|---|---|
| `GetVacancyQuery.cs` | `namespace JobNecto.Application.Vacancies;` |
| `GetVacancyQueryHandler.cs` | `namespace JobNecto.Application.Vacancies;` |
| `GetVacancyQueryHandlerTests.cs` | `namespace JobNecto.Tests.Application.Vacancies;` |
| `VacancyMappers.cs` additions | `namespace JobNecto.Application.Vacancies.Mappers;` (unchanged) |
| `VacanciesController.cs` additions | `namespace JobNecto.API.Controllers;` (unchanged) |

### Agent Learnings to Apply

- **ProblemDetails shape** (2026-05-10): This endpoint does not return 400 — but any future validation must use the `BadRequest(new ProblemDetails {...})` shape
- **Auth guard** (always): First statement in every controller action — confirmed with `HttpContext.GetCurrentUserId()` + `Guid.TryParse` guard
- **ProducesResponseType** (api-checker pattern): Must include 200, 401, 404 — do not omit
- **XML docs** (code documentation requirements in AGENTS.md): All new non-trivial public methods need `/// <summary>`, `<param>`, `<returns>`
- **Separate handler file** (2026-04-28): Handler in own file, not co-located with query — `GetVacancyQuery.cs` + `GetVacancyQueryHandler.cs`
- **No ownership check** (FR26): "Any user can retrieve a single vacancy" — do not add user-scope filtering to this endpoint

### VacancyTestData.SeniorDotNetRemotePoland (for tests)

```
Id: auto-generated Guid
UserId: seeded user's Id
Title: "Senior Backend Engineer (.NET / Azure)"
Description: "...clinical data platform..."
Company: "MedStack Analytics"
Location: Location.Poland   → "Poland"
WorkLocationType: WorkLocationType.Remote → "Remote"
WorkTimeType: WorkTimeType.FullTime
Skills: [".NET", "C#", "Azure", "PostgreSQL", "Docker", "REST"]
JobCategories: ["Engineering", "Backend", "Healthcare IT"]
SalaryMin: 95_000m, SalaryMax: 118_000m
Currency: Currency.EUR → "EUR"
MatchScore: 0.91
ExperienceLevel: "Senior"
JobSource: SourceLinkedIn { Name = "LinkedIn", Url = "https://www.linkedin.com/jobs" }
```

### Project Structure Notes

```
New files:
  backend/src/JobNecto.Application/Vacancies/GetVacancyQuery.cs
  backend/src/JobNecto.Application/Vacancies/GetVacancyQueryHandler.cs
  backend/tests/JobNecto.Tests/Application/Vacancies/GetVacancyQueryHandlerTests.cs

Modified files:
  backend/src/JobNecto.Application/Vacancies/Mappers/VacancyMappers.cs
  backend/src/JobNecto.API/Controllers/VacanciesController.cs
  backend/tests/JobNecto.Tests/API/Vacancies/VacanciesApiTests.cs
```

### References

- [Source: `_bmad-output/planning-artifacts/epics/epic-4-vacancy-browsing-filtering.md` - Story 4.3 ACs]
- [Source: `_bmad-output/planning-artifacts/epics/requirements-inventory.md` - FR26, FR27, NFR4, NFR9]
- [Source: `_bmad-output/agent-learnings.md` - XML docs, auth guard, ProblemDetails, separate handler file]
- [Source: `backend/src/JobNecto.Application/CoverLetterTemplates/GetCoverLetterTemplateQuery.cs`]
- [Source: `backend/src/JobNecto.Application/CoverLetterTemplates/GetCoverLetterTemplateQueryHandler.cs`]
- [Source: `backend/src/JobNecto.API/Controllers/CoverLetterTemplatesController.cs`]
- [Source: `backend/src/JobNecto.Application/Interfaces/IRepository.cs`]
- [Source: `backend/src/JobNecto.Infrastructure/Repositories/BaseRepository.cs`]
- [Source: `backend/src/JobNecto.Application/Vacancies/FilterVacanciesQuery.cs`]
- [Source: `backend/src/JobNecto.Application/Vacancies/Mappers/VacancyMappers.cs`]
- [Source: `backend/src/JobNecto.API/Controllers/VacanciesController.cs`]
- [Source: `backend/src/JobNecto.Domain/Entities/Vacancy.cs`]
- [Source: `backend/tests/JobNecto.Tests/Infrastructure/VacancyTestData.cs`]

## Story Completion Status

- Ultimate context engine analysis completed - comprehensive developer guide created.

## Dev Agent Record

### Agent Model Used

claude-haiku-4-5-20251001

### Debug Log References

- Story 4.3 follows the established GET detail pattern from CoverLetterTemplates (GetCoverLetterTemplateQueryHandler).
- `BaseRepository.GetByIdAsync` is inherited by `VacancyRepository` — no new repository method needed.
- No ownership check per FR26 ("any user can retrieve a single vacancy").
- `VacancySalaryResult` already exists in `FilterVacanciesQuery.cs` — reuse it in `VacancyDetailResult`.

### Completion Notes List

- Story context created with exact code snippets for all new files.
- Handler is minimal: one repository call + one mapper call.
- No new repository interface method needed — `GetByIdAsync` already exists in `IRepository<T>`.

### File List

- `_bmad-output/implementation-artifacts/4-3-get-vacancy-detail.md` (this file)
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (status update — see Step 6)
