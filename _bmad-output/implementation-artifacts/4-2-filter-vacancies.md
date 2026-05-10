# Story 4.2: Filter Vacancies

Status: ready-for-dev

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a job seeker,
I want to search vacancies by multiple criteria at once,
so that I can find the roles that best match my profile.

## Acceptance Criteria

1. Given a valid JWT token and `POST /api/v1/vacancies/filter` with body `{ skills: ["Go"], location: "Berlin", salaryMin: 80000, workLocationTypes: ["remote"] }`, when the request is processed, then `200 OK` with vacancies matching ALL specified filters (AND logic between fields), and within array fields (`skills`, `workLocationTypes`, `categories`), any match is sufficient (OR logic).
2. Given one or more filter fields are provided, when `POST /api/v1/vacancies/filter` is called, then only the matching subset is returned while preserving the same pagination envelope and selected sort mode as Story 4.1.
3. Given `salaryMin` > `salaryMax` is provided, when the request is processed, then `400 Bad Request` with field-level error.
4. Given `pageSize` exceeds 100, when the request is processed, then it is capped at 100 (consistent with Story 4.1 — silent cap, not a 400).
5. Given `excludeKeywords` contains terms, when the request is processed, then vacancies whose `title` or `description` contains any of those terms are excluded.

## Tasks / Subtasks

- [ ] Task 1: Add `ExcludeKeywords` to domain filter value object (AC: 5)
  - [ ] Add `string[]? ExcludeKeywords { get; init; }` to `backend/src/JobNecto.Domain/ValueObjects/EntityFilter.cs`

- [ ] Task 2: Add `ExcludeKeywords` to application layer (AC: 5)
  - [ ] Add `public string[]? ExcludeKeywords { get; set; }` to `FilterVacanciesQuery` in `backend/src/JobNecto.Application/Vacancies/FilterVacanciesQuery.cs`
  - [ ] In `FilterVacanciesQueryHandler.BuildFilterOrNull()`, set `ExcludeKeywords = NormalizeArray(request.ExcludeKeywords)` on the filter object
  - [ ] In `FilterVacanciesQueryHandler.HasAnyFilterCriteria()`, add `|| filter.ExcludeKeywords is not null` check

- [ ] Task 3: Implement `ExcludeKeywords` exclusion in repository (AC: 5)
  - [ ] In `VacancyRepository.ApplyFilters()`, add after the existing filter blocks:
    ```csharp
    if (filter.ExcludeKeywords != null && filter.ExcludeKeywords.Length > 0)
    {
        foreach (var keyword in filter.ExcludeKeywords)
        {
            var term = $"%{keyword.Trim()}%";
            query = query.Where(v =>
                (v.Title == null || !EF.Functions.Like(v.Title, term)) &&
                (v.Description == null || !EF.Functions.Like(v.Description, term))
            );
        }
    }
    ```
  - [ ] File: `backend/src/JobNecto.Infrastructure/Repositories/VacancyRepository.cs`

- [ ] Task 4: Add `salaryMin > salaryMax` validation to controller (AC: 3)
  - [ ] In `VacanciesController.FilterAsync()`, after `query ??= new FilterVacanciesQuery()` and `query.UserId = userId`, add:
    ```csharp
    if (query.SalaryMin.HasValue && query.SalaryMax.HasValue && query.SalaryMin > query.SalaryMax)
        return BadRequest(new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation failed",
            Detail = "salaryMin must not exceed salaryMax.",
        });
    ```
  - [ ] File: `backend/src/JobNecto.API/Controllers/VacanciesController.cs`

- [ ] Task 5: Add handler unit tests (AC: 5)
  - [ ] `Handle_ExcludeKeywords_IncludesKeywordsInFilter` — verify `ExcludeKeywords` flows from query into captured `VacancyFilter`
  - [ ] `Handle_ExcludeKeywordsEmptyArray_TreatsAsNoCriteria` — empty array → filter remains null (browse mode)
  - [ ] File: `backend/tests/JobNecto.Tests/Application/Vacancies/FilterVacanciesQueryHandlerTests.cs`

- [ ] Task 6: Add repository tests (AC: 1, 5)
  - [ ] `GetFilteredAsync_with_excludeKeywords_excludes_vacancies_matching_title` — LIKE on Title (InMemory-compatible)
  - [ ] `GetFilteredAsync_with_excludeKeywords_excludes_vacancies_matching_description` — LIKE on Description
  - [ ] `GetFilteredAsync_with_multiple_criteria_returns_AND_intersection` — e.g., Company filter + salaryMin filter together
  - [ ] File: `backend/tests/JobNecto.Tests/Infrastructure/VacancyRepositoryTests.cs`

- [ ] Task 7: Add API integration tests (AC: 1, 3, 5)
  - [ ] `Filter_WithSalaryMinGreaterThanSalaryMax_Returns400WithProblemDetails`
  - [ ] `Filter_WithMultipleCriteria_ReturnsMatchingSubset` — combined `company` + `workLocationType` (no Skills/JobCategories — InMemory array Contains fails)
  - [ ] `Filter_WithExcludeKeywords_ExcludesMatchingVacancies` — seed full set, use `excludeKeywords: ["clinical"]` (matches SeniorDotNetRemotePoland description)
  - [ ] File: `backend/tests/JobNecto.Tests/API/Vacancies/VacanciesApiTests.cs`

- [ ] Task 8: Verification gates
  - [ ] Targeted: `dotnet test backend/JobNecto.slnx --filter "FullyQualifiedName~Vacanc"`
  - [ ] Full suite: `dotnet test backend/JobNecto.slnx`
  - [ ] CI parity build: `dotnet build backend/JobNecto.slnx --configuration Release --warnaserror`
  - [ ] CI parity tests: `dotnet test backend/JobNecto.slnx --configuration Release --no-build --warnaserror`

## Dev Notes

### CRITICAL: What's Already Built — Do NOT Reinvent

Story 4.1 delivered the complete filter infrastructure. Story 4.2 is a **targeted extension** of 5 files only. Most ACs are already satisfied:

| AC | Status | Action |
|---|---|---|
| AC 1 — AND logic between fields, OR within arrays | **Already implemented** in `VacancyRepository.ApplyFilters()` | No change |
| AC 2 — Same pagination envelope + sort mode | **Already implemented** in handler + repository | No change |
| AC 3 — `salaryMin > salaryMax` → 400 | **Missing** — add to controller | Add validation |
| AC 4 — `pageSize` capped at 100 | **Already implemented** in handler | No change |
| AC 5 — `excludeKeywords` exclusion | **Missing** — `ExcludeKeywords` field not in any layer | Add field + logic |

Do **not** touch pagination, sort mode, cursor logic, response envelope, or existing filter logic.

### Files to Modify (No New Files)

| File | Change |
|---|---|
| `backend/src/JobNecto.Domain/ValueObjects/EntityFilter.cs` | Add `string[]? ExcludeKeywords { get; init; }` |
| `backend/src/JobNecto.Application/Vacancies/FilterVacanciesQuery.cs` | Add `string[]? ExcludeKeywords { get; set; }` |
| `backend/src/JobNecto.Application/Vacancies/FilterVacanciesQueryHandler.cs` | `BuildFilterOrNull` + `HasAnyFilterCriteria` |
| `backend/src/JobNecto.Infrastructure/Repositories/VacancyRepository.cs` | New block in `ApplyFilters()` |
| `backend/src/JobNecto.API/Controllers/VacanciesController.cs` | Salary cross-field validation |
| `backend/tests/JobNecto.Tests/API/Vacancies/VacanciesApiTests.cs` | 3 new tests |
| `backend/tests/JobNecto.Tests/Application/Vacancies/FilterVacanciesQueryHandlerTests.cs` | 2 new tests |
| `backend/tests/JobNecto.Tests/Infrastructure/VacancyRepositoryTests.cs` | 3 new tests |

### ExcludeKeywords Implementation Detail

Chaining `.Where()` per keyword composes AND across keywords in EF Core SQL. Each `.Where()` clause means: "for this keyword, title must NOT match AND description must NOT match." The combined effect:
- A vacancy is excluded if ANY keyword appears in its title OR description.
- Null guard prevents SQL NULL propagation from unintentionally excluding vacancies with null title/description.

**InMemory compatibility:** `EF.Functions.Like` with `%term%` is supported in EF InMemory (falls back to `string.Contains`). The existing `Company` LIKE filter in `VacancyRepositoryTests` proves InMemory works for LIKE patterns. `ExcludeKeywords` uses the same pattern — repository tests CAN use InMemory.

**Skills/JobCategories:** Do NOT add API or repository integration tests for Skills or JobCategories filters using InMemory. The `VacancyTestData.cs` header explicitly documents that array `Any()` predicates fail in InMemory. Test these at handler level with Moq only.

### SalaryMin > SalaryMax Validation — Exact Placement

Insert after `query.UserId = userId` and before the cursor XOR check. The full controller action ordering must be:

1. Auth guard → `Unauthorized()` if invalid
2. `query ??= new FilterVacanciesQuery()`
3. `query.UserId = userId`
4. **[NEW]** `salaryMin > salaryMax` → `BadRequest(ProblemDetails)`
5. Cursor XOR validation → `BadRequest(ProblemDetails)`
6. SortBy allowlist check → `BadRequest(ProblemDetails)`
7. UTC normalization of `LastSeenUpdatedAt`
8. `_mediator.Send(query, cancellationToken)`
9. `Ok(result)`

### ProblemDetails Shape (mandatory — agent-learnings 2026-05-10)

Every 400 in this project uses:
```csharp
BadRequest(new ProblemDetails
{
    Status = StatusCodes.Status400BadRequest,
    Title = "Validation failed",
    Detail = "...",
})
```
Do NOT return `BadRequest("plain string")`. Check the existing cursor XOR and sortBy blocks in `VacanciesController` as the canonical reference.

### JSON Serialization (confirmed)

`Program.cs` registers `JsonStringEnumConverter` globally:
```csharp
.AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
```
Clients send enums as strings (e.g., `"remote"` for `WorkLocationType.Remote`). This is working since Story 4.1. No change needed.

### Test Data Reference for Story 4.2

`VacancyTestData.AllDistinctUpdatedAt()` seeds 7 vacancies. Use these for filter tests:

| Fixture | Location | WorkLocationType | Company | Title/Description keywords | SalaryMin/Max |
|---|---|---|---|---|---|
| `SeniorDotNetRemotePoland` | Poland | Remote | MedStack Analytics | "clinical" in description | 95k–118k EUR |
| `JuniorReactHybridBerlin` | Germany | Hybrid | CargoFlow GmbH | "accessibility" | 48k–56k EUR |
| `ContractDevOpsRemoteUs` | UnitedStates | Remote | Northwind Cloud | "Terraform" | 120k–155k USD |
| `DataAnalystInternshipWarsaw` | Poland | OnSite | RetailPulse SA | "BigQuery" | 4.5k–5.8k PLN |
| `PartTimeContentMarketerRemote` | Netherlands | Remote | SignalForge | "LinkedIn" | 32k–42k GBP |
| `LegacyFullStackHidden` | CzechRepublic | Hybrid | OldMill Software | "monolith" | 60k–75k EUR |
| `BareMinimumScrapedListing` | Spain | Remote | UnknownCo | "details on apply" | null |

Suggested scenarios:
- `excludeKeywords: ["clinical"]` → excludes `SeniorDotNetRemotePoland` only
- `company: "MedStack" + workLocationType: ["Remote"]` → `SeniorDotNetRemotePoland` only
- `salaryMin: 500000, salaryMax: 100` → 400 (salaryMin > salaryMax)

### Auth Guard Pattern (already in controller — do not duplicate)

```csharp
var userIdValue = HttpContext.GetCurrentUserId();
if (string.IsNullOrWhiteSpace(userIdValue) || !Guid.TryParse(userIdValue, out var userId))
    return Unauthorized();
```

### Agent Learnings to Apply

- **ProblemDetails for 400s** (2026-05-10): Follow existing controller pattern — Status 400, Title "Validation failed", Detail with field name
- **Allowlist validation** (2026-05-10): `salaryMin > salaryMax` is a cross-field constraint; validate at controller boundary (same layer as cursor XOR and sortBy)
- **Validator-compliant test data** (2026-04-25): `LoginName` must not contain hyphens — already handled in `NewUserCommand()` in `VacanciesApiTests.cs`
- **Separate handler file** (2026-04-28): `FilterVacanciesQuery.cs` and `FilterVacanciesQueryHandler.cs` are already split — maintain this
- **Partial cursor XOR** (2026-05-10): Already validated in 4.1 — do NOT add duplicate validation

### Project Structure Notes

No new directories or files. All changes within existing files. Namespaces are already correct from Story 4.1 — do not change them.

```
Modified files:
  backend/src/JobNecto.Domain/ValueObjects/EntityFilter.cs
  backend/src/JobNecto.Application/Vacancies/FilterVacanciesQuery.cs
  backend/src/JobNecto.Application/Vacancies/FilterVacanciesQueryHandler.cs
  backend/src/JobNecto.Infrastructure/Repositories/VacancyRepository.cs
  backend/src/JobNecto.API/Controllers/VacanciesController.cs
  backend/tests/JobNecto.Tests/API/Vacancies/VacanciesApiTests.cs
  backend/tests/JobNecto.Tests/Application/Vacancies/FilterVacanciesQueryHandlerTests.cs
  backend/tests/JobNecto.Tests/Infrastructure/VacancyRepositoryTests.cs
```

### References

- [Source: `_bmad-output/planning-artifacts/epics/epic-4-vacancy-browsing-filtering.md` - Story 4.2 ACs]
- [Source: `_bmad-output/planning-artifacts/epics/requirements-inventory.md` - FR25, NFR2, NFR10]
- [Source: `_bmad-output/agent-learnings.md` - ProblemDetails, allowlist, test data, separate handler file]
- [Source: `backend/src/JobNecto.Domain/ValueObjects/EntityFilter.cs`]
- [Source: `backend/src/JobNecto.Application/Vacancies/FilterVacanciesQuery.cs`]
- [Source: `backend/src/JobNecto.Application/Vacancies/FilterVacanciesQueryHandler.cs`]
- [Source: `backend/src/JobNecto.Infrastructure/Repositories/VacancyRepository.cs`]
- [Source: `backend/src/JobNecto.API/Controllers/VacanciesController.cs`]
- [Source: `backend/tests/JobNecto.Tests/API/Vacancies/VacanciesApiTests.cs`]
- [Source: `backend/tests/JobNecto.Tests/Application/Vacancies/FilterVacanciesQueryHandlerTests.cs`]
- [Source: `backend/tests/JobNecto.Tests/Infrastructure/VacancyRepositoryTests.cs`]
- [Source: `backend/tests/JobNecto.Tests/Infrastructure/VacancyTestData.cs`]

## Story Completion Status

- Ultimate context engine analysis completed - comprehensive developer guide created.

## Dev Agent Record

### Agent Model Used

claude-haiku-4-5-20251001

### Debug Log References

- Story 4.1 delivered the full filter infrastructure; 4.2 is a targeted extension of 5 production files.
- `ExcludeKeywords` is the only structural addition (new field across Domain/Application/Infrastructure layers).
- `salaryMin > salaryMax` is a controller-level cross-field validation consistent with existing ProblemDetails pattern.
- InMemory compatibility confirmed: LIKE-based filters work (proven by existing Company filter repository test).

### Completion Notes List

- Story context created with exact code snippets and InMemory compatibility analysis.
- ACs 1, 2, 4 confirmed fully implemented by Story 4.1 — dev scope is minimal.
- Skills/JobCategories test warning documented clearly to prevent InMemory test failures.

### File List

- `_bmad-output/implementation-artifacts/4-2-filter-vacancies.md` (this file)
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (status update — see Step 6)
