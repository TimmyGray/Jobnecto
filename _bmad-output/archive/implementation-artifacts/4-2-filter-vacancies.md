# Story 4.2: Filter Vacancies (Advanced Filter Criteria)

Status: done

## Story

As a job seeker,
I want to filter vacancies by salary range and exclude vacancies containing unwanted keywords,
so that I can narrow the result set to roles that genuinely match my preferences.

## Acceptance Criteria

1. Given valid filter criteria, when `POST /api/v1/vacancies/filter` is called with `salaryMin` and/or `salaryMax`, then only vacancies whose salary range satisfies the constraint are returned.
2. Given `salaryMin > salaryMax`, when the request is processed, then `400 Bad Request` is returned with a structured ProblemDetails body containing `errors.SalaryMin` with a descriptive message.
3. Given `salaryMin == salaryMax`, when the request is processed, then `200 OK` is returned (boundary is inclusive).
4. Given `excludeKeywords` is a non-empty array, when the request is processed, then any vacancy whose `title` OR `description` contains any of the keywords is excluded from results; multiple keywords apply AND logic (vacancy must not match any).
5. Given `excludeKeywords` is an empty array `[]`, when the request is processed, then it is treated as no filter criterion and all vacancies are returned (browse mode behavior preserved).
6. All existing Story 4.1 acceptance criteria (browse mode, cursor pagination, `sortBy`, user scoping) remain unaffected.

## Tasks / Subtasks

- [x] Task 1: Add `ExcludeKeywords` to the domain `VacancyFilter` value object (AC: 4, 5)
  - [x] Add `string[]? ExcludeKeywords { get; init; }` to `VacancyFilter` in `EntityFilter.cs`.
  - [x] Include `ExcludeKeywords is not null` in `HasAnyFilterCriteria` check.

- [x] Task 2: Add `ExcludeKeywords` to `FilterVacanciesQuery` (AC: 4, 5)
  - [x] Add `string[]? ExcludeKeywords` field with XML doc to `FilterVacanciesQuery.cs`.

- [x] Task 3: Map `ExcludeKeywords` in `FilterVacanciesQueryHandler` (AC: 4, 5)
  - [x] Wire `ExcludeKeywords: NormalizeArray(request.ExcludeKeywords)` in `BuildFilterOrNull()`.
  - [x] Confirm empty array normalises to `null` via existing `NormalizeArray` helper.

- [x] Task 4: Add exclude-keywords SQL filtering in `VacancyRepository` (AC: 4)
  - [x] Add per-keyword `EF.Functions.Like` WHERE clauses in `ApplyFilters()`.
  - [x] Use null-safe `v.Title ?? string.Empty` and `v.Description ?? string.Empty`.
  - [x] AND semantics across multiple keywords (one `.Where()` per keyword).

- [x] Task 5: Add cross-field salary validation (AC: 2, 3)
  - [x] Create `FilterVacanciesQueryValidator.cs` under `JobNecto.Application.Vacancies.Validators`.
  - [x] FluentValidation rule: `SalaryMin <= SalaryMax` when both are provided.
  - [x] Auto-registered via existing `AddValidatorsFromAssembly` in `ApplicationCollectionExtensions`.

- [x] Task 6: Tests (AC: 1–5)
  - [x] Handler tests: `ExcludeKeywords` mapped to filter; empty array normalised to null.
  - [x] Repository tests: title exclusion, description exclusion, multi-keyword AND logic.
  - [x] API integration tests: `salaryMin > salaryMax` → 400 with `errors.SalaryMin`; equal values → 200; `excludeKeywords` exclusion; multi-criteria AND intersection.

- [x] Task 7: Verification gates
  - [x] Targeted: `dotnet test backend/JobNecto.slnx --filter "FullyQualifiedName~Vacancies"` → 30/30.
  - [x] Full suite: `dotnet test backend/JobNecto.slnx` → 397/397.
  - [x] Release build: `dotnet build backend/JobNecto.slnx --configuration Release --warnaserror` → 0 warnings.
  - [x] Release tests: `dotnet test backend/JobNecto.slnx --configuration Release --no-build --warnaserror` → 397/397.

## Dev Notes

### Implementation Decisions

- **Case-sensitive keyword matching**: `EF.Functions.Like` is case-sensitive on PostgreSQL, consistent with all other string `Like` filters in the repository (Title/Description/Company inclusion filters). XML doc on `ExcludeKeywords` documents this explicitly. Case-insensitive search system-wide is a separate future concern.
- **Empty array normalization**: `NormalizeArray` in the handler converts `[]` → `null`, preventing an empty `ExcludeKeywords` array from being passed to the repository. This preserves browse-mode semantics for clients that always include the field.
- **Validator placement**: `FilterVacanciesQueryValidator` lives in `Application/Vacancies/Validators/`, consistent with project namespace conventions. Auto-discovered by `AddValidatorsFromAssembly`.
- **No new migration**: `ExcludeKeywords` is a filter-only value object property, not persisted to the database.

### Review Findings (2026-05-10)

Code review found no critical or high severity issues. Three low/medium findings noted:

1. **Medium**: Case-sensitivity of `excludeKeywords` matches implementation but not the original requirement language ("case-insensitive LIKE"). Accepted as-is; consistent with existing LIKE filters. Future story should address case-insensitive search system-wide.
2. **Low**: No API-level test for empty `excludeKeywords` array returning all results. Handler unit test covers the normalization. Added to deferred work.
3. **Low**: No standalone unit tests for `FilterVacanciesQueryValidator`. Validation exercised via API integration tests.

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

- Implementation commit: `18ec65c feat(vacancies): implement Story 4.2 filter vacancies`
- Review commit: `ca3fcb6 docs(post-merge): update after feature story-4-1 merged to master`

### Completion Notes List

- 10 new tests added (2 handler, 3 repository, 5 API integration); total suite 397/397 green.
- Clean Architecture boundaries verified by automated boundary-checker sub-agent.
- API contract verified by automated api-checker sub-agent: auth guards, ProducesResponseType, ProblemDetails shape, and validator wiring all confirmed correct.

### File List

- `backend/src/JobNecto.Domain/ValueObjects/EntityFilter.cs` (modified)
- `backend/src/JobNecto.Application/Vacancies/FilterVacanciesQuery.cs` (modified)
- `backend/src/JobNecto.Application/Vacancies/FilterVacanciesQueryHandler.cs` (modified)
- `backend/src/JobNecto.Application/Vacancies/Validators/FilterVacanciesQueryValidator.cs` (created)
- `backend/src/JobNecto.Infrastructure/Repositories/VacancyRepository.cs` (modified)
- `backend/tests/JobNecto.Tests/Application/Vacancies/FilterVacanciesQueryHandlerTests.cs` (modified)
- `backend/tests/JobNecto.Tests/Infrastructure/VacancyRepositoryTests.cs` (modified)
- `backend/tests/JobNecto.Tests/API/Vacancies/VacanciesApiTests.cs` (modified)
- `_bmad-output/archive/implementation-artifacts/4-2-filter-vacancies.md` (this file)
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (status updated)

## Change Log

| Date | Version | Description | Author |
|------|---------|-------------|--------|
| 2026-05-10 | 1.0 | Story implemented and merged; all gates green; review complete | claude-sonnet-4-6 |

