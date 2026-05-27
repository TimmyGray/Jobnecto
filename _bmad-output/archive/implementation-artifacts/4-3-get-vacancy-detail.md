# Story 4.3: Get Vacancy Detail

Status: done

## Story

As a job seeker,
I want to view all details of a specific vacancy,
So that I can decide whether to apply.

## Acceptance Criteria

1. Given a valid JWT token and a vacancy ID that exists, when `GET /api/v1/vacancies/{id}` is called, then `200 OK` with all fields: `id`, `title`, `description`, `company`, `skills`, `workLocationType`, `location`, `salary`, `currency`, `matchScore`, `jobSource`, `categories`, `experienceLevel`, `createdAt`.
2. Given the vacancy ID does not exist, when `GET /api/v1/vacancies/{id}` is called, then `404 Not Found`.
3. Given the vacancy ID belongs to another user, when `GET /api/v1/vacancies/{id}` is called with a valid token, then `404 Not Found` (existence leakage prevention).
4. Given no JWT token, when `GET /api/v1/vacancies/{id}` is called, then `401 Unauthorized`.

## Tasks / Subtasks

- [x] Task 1: Add query, response DTOs, and handler (AC: 1, 2, 3)
  - [x] Create `backend/src/JobNecto.Application/Vacancies/GetVacancyQuery.cs` with `GetVacancyQuery`, `VacancyDetailResult`, and `VacancyJobSourceResult`.
  - [x] Create `backend/src/JobNecto.Application/Vacancies/GetVacancyQueryHandler.cs` handling ownership check and NotFoundException propagation.

- [x] Task 2: Add mapper method (AC: 1)
  - [x] Add `ToVacancyDetailResult()` extension to `backend/src/JobNecto.Application/Vacancies/Mappers/VacancyMappers.cs`.

- [x] Task 3: Add API endpoint (AC: 1, 2, 3, 4)
  - [x] Add `GetAsync` action to `backend/src/JobNecto.API/Controllers/VacanciesController.cs` with `[HttpGet("{id:guid}")]`, auth guard, and ProducesResponseType attributes.

- [x] Task 4: Add tests (AC: 1, 2, 3, 4)
  - [x] Create `backend/tests/JobNecto.Tests/Application/Vacancies/GetVacancyQueryHandlerTests.cs` (3 handler unit tests).
  - [x] Extend `backend/tests/JobNecto.Tests/API/Vacancies/VacanciesApiTests.cs` (4 API integration tests).

- [x] Task 5: Verification gates
  - [x] Build: `dotnet build backend/JobNecto.slnx --configuration Release --warnaserror` — 0 warnings, 0 errors.
  - [x] Tests: `dotnet test backend/JobNecto.slnx --configuration Release --no-build --warnaserror` — 394 passed, 0 failed.

## Dev Notes

### Files Created

- `backend/src/JobNecto.Application/Vacancies/GetVacancyQuery.cs`
- `backend/src/JobNecto.Application/Vacancies/GetVacancyQueryHandler.cs`
- `backend/tests/JobNecto.Tests/Application/Vacancies/GetVacancyQueryHandlerTests.cs`

### Files Modified

- `backend/src/JobNecto.Application/Vacancies/Mappers/VacancyMappers.cs` — added `ToVacancyDetailResult()`
- `backend/src/JobNecto.API/Controllers/VacanciesController.cs` — added `GetAsync` action
- `backend/tests/JobNecto.Tests/API/Vacancies/VacanciesApiTests.cs` — added 4 integration tests + helpers

### Ownership Policy

Cross-user vacancy reads return `404 Not Found` (not `403`) per Decision 7 of `core-architectural-decisions.md` to prevent existence leakage. This is consistent with `GetCoverLetterTemplateQueryHandler`.

### Repository Access

`GetByIdAsync` is inherited by `IVacancyRepository` from `IRepository<Vacancy>` via `ISoftDeleteRepository<Vacancy>`. No new repository methods were added.

### DTO Scope

`VacancyDetailResult` maps exactly the fields named in the AC. Entity fields outside the AC (`CompanyWebsite`, `WorkTimeType`, `IsViewed`, `IsChosen`, `IsHidden`, `UpdatedAt`) are intentionally excluded.

### Reused Types

`VacancySalaryResult` (defined in `FilterVacanciesQuery.cs`) is reused in `VacancyDetailResult`. `VacancyJobSourceResult` is new and defined in `GetVacancyQuery.cs`.

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Completion Notes

- Story synthesised from `epic-4-vacancy-browsing-filtering.md` (no prior `4-3-*.md` story file existed).
- 394 total tests pass; 7 new tests added (3 handler unit tests + 4 API integration tests).
- Build clean: 0 warnings, 0 errors under `--warnaserror`.

### File List

- `_bmad-output/archive/implementation-artifacts/4-3-get-vacancy-detail.md` (this file)

## Change Log

| Date | Summary |
|------|---------|
| 2026-05-10 | Implementation complete. Added `GetVacancyQuery`, `GetVacancyQueryHandler`, `ToVacancyDetailResult()` mapper, `GetAsync` controller action, 3 handler unit tests, and 5 API integration tests (including soft-delete regression test added during finalization per review finding F-1). 395 tests passing. |

