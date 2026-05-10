# Story 4.1: Browse Vacancies (Empty Filter Mode)

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a job seeker,
I want to browse all available vacancies using the filter route with empty criteria,
so that I can scan what is available without defining filters.

## Acceptance Criteria

1. Given a valid JWT token, when `POST /api/v1/vacancies/filter` is called with empty body `{}`, then `200 OK` with `{ totalCount, pageSize, hasNext, lastSeenId, lastSeenUpdatedAt, items }`, returning only vacancies owned by the authenticated user, ordered by `createdAt desc` by default, `pageSize` defaulting to 20, and each item includes `id`, `title`, `company`, `workLocationType`, `location`, `salary`, `currency`, `createdAt`.
2. Given `pageSize`, `lastSeenId`, `lastSeenUpdatedAt`, and optional `sortBy` are provided in the request body while filter criteria are empty, when the request is processed, then the correct cursor window is returned and `pageSize` is capped at 100.
3. Given `sortBy` is `updatedAt` or `relevance`, when the request is processed, then ordering is `updatedAt desc` with deterministic tie-break by `id desc`.
4. Given no vacancies exist in DB, when `POST /api/v1/vacancies/filter` is called with empty body `{}`, then `200 OK` with `{ totalCount: 0, hasNext: false, items: [] }`.

## Tasks / Subtasks

- [ ] Task 1: Add unified vacancy filter query contract and browse response DTOs (AC: 1, 2, 3)
  - [ ] Create `backend/src/JobNecto.Application/Vacancies/FilterVacanciesQuery.cs` implementing `IRequest<PagedResult<VacancyListItemResult>>`.
  - [ ] Include pagination fields: `PageSize` (default 20), `LastSeenId`, `LastSeenUpdatedAt`.
  - [ ] Include optional `SortBy` field (`createdAt` default, `updatedAt`, `relevance`).
  - [ ] Include optional filter criteria fields (all nullable/optional) so empty body maps to browse mode.
  - [ ] Add list DTO `VacancyListItemResult` containing at minimum: `Id`, `Title`, `Company`, `WorkLocationType`, `Location`, salary representation, `Currency`, `CreatedAt`.

- [ ] Task 2: Add mapper and handler for empty-filter browse behavior (AC: 1, 2, 3)
  - [ ] Create `backend/src/JobNecto.Application/Vacancies/Mappers/VacancyMappers.cs` for `Vacancy` -> `VacancyListItemResult`.
  - [ ] Create `backend/src/JobNecto.Application/Vacancies/FilterVacanciesQueryHandler.cs`.
  - [ ] In handler, cap page size with `request.PageSize < 1 ? 20 : Math.Min(request.PageSize, 100)`.
  - [ ] Build `PagedQuery` and a nullable `VacancyFilter`; when criteria are empty, pass `null` to repository.
  - [ ] Call `_unitOfWork.VacancyRepository.GetFilteredAsync(pagedQuery, filter, cancellationToken)`.
  - [ ] Map to `PagedResult<VacancyListItemResult>` and preserve metadata (`TotalCount`, `HasNext`, `LastSeenId`, `LastSeenUpdatedAt`, `PageSize`).

- [ ] Task 3: Add the single vacancy list/filter API route (AC: 1, 2, 3)
  - [ ] Create `backend/src/JobNecto.API/Controllers/VacanciesController.cs` with `[ApiController]`, `[Route("api/v1/vacancies")]`, `[Authorize]`.
  - [ ] Add `[HttpPost("filter")]` action accepting `FilterVacanciesQuery` body.
  - [ ] Normalize `LastSeenUpdatedAt` using the same UTC normalization pattern used in existing list endpoints.
  - [ ] Dispatch `FilterVacanciesQuery` through MediatR and return `Ok(result)`.
  - [ ] Add response metadata attributes for `200`, `400`, and `401`.

- [ ] Task 4: Align vacancy repository ordering with Story 4.1 browse contract (AC: 1, 2, 3)
  - [ ] Support selectable sort mode in `backend/src/JobNecto.Infrastructure/Repositories/VacancyRepository.cs`: `createdAt desc` (default), `updatedAt desc` for `sortBy=updatedAt|relevance`, deterministic tiebreak by `Id desc`.
  - [ ] Update cursor comparison logic to use timestamp that matches selected sorting mode.
  - [ ] Ensure query remains scoped to authenticated user (`pagedQuery.UserId`).
  - [ ] Keep `GetFilteredAsync` signature and filter logic reusable for Story 4.2 advanced criteria.

- [ ] Task 5: Add tests for browse mode via filter route (AC: 1, 2, 3)
  - [ ] Create `backend/tests/JobNecto.Tests/Application/Vacancies/FilterVacanciesQueryHandlerTests.cs`.
  - [ ] Add handler tests for default page size, page-size cap, cursor forwarding, and metadata preservation when criteria are empty.
  - [ ] Create `backend/tests/JobNecto.Tests/API/Vacancies/VacanciesApiTests.cs`.
  - [ ] Add API tests:
    - [ ] `POST /api/v1/vacancies/filter` without token -> `401`.
    - [ ] empty body `{}` with empty DB -> `200` with empty `items`, `totalCount = 0`, `hasNext = false`.
    - [ ] empty body `{}` with seeded vacancies -> `200` with expected item shape and `createdAt desc` ordering for current user only.
    - [ ] `sortBy = updatedAt` -> `200` with `updatedAt desc` ordering.
    - [ ] `sortBy = relevance` -> `200` with `updatedAt desc` ordering (alias behavior).
    - [ ] `pageSize` cap behavior when body sets value > 100.
    - [ ] cursor paging with empty criteria returns next deterministic slice.
  - [ ] Update `backend/tests/JobNecto.Tests/Infrastructure/VacancyRepositoryTests.cs` to cover both default `createdAt desc` ordering and `sortBy=updatedAt` mode.

- [ ] Task 6: Verification gates
  - [ ] Run targeted tests: `dotnet test backend/JobNecto.slnx --filter "FullyQualifiedName~Vacanc"`.
  - [ ] Run full test suite: `dotnet test backend/JobNecto.slnx`.
  - [ ] Run CI parity build: `dotnet build backend/JobNecto.slnx --configuration Release --warnaserror`.
  - [ ] Run CI parity tests: `dotnet test backend/JobNecto.slnx --configuration Release --no-build --warnaserror`.

## Dev Notes

### Vacancy Baseline (Already Implemented)

- `IVacancyRepository` already exists with `GetFilteredAsync(PagedQuery, VacancyFilter?, CancellationToken)` and `UpdateMatchScoreAsync(...)`.
- `VacancyRepository.GetFilteredAsync` already supports pagination + optional filter application.
- `VacancyRepository` currently orders by `UpdatedAt desc`, then `Id desc`, then `MatchScore` tiebreak. Story 4.1 requires `createdAt desc` ordering for browse mode.
- Global soft-delete filter for `Vacancy` is active in `AppDbContext` (`HasQueryFilter(v => !v.IsDeleted)`).
- No API/application vacancy browse/filter endpoint exists yet (no `VacanciesController`, no vacancy query handler/DTO in Application layer).

### Architectural Guardrails

- Keep Clean Architecture boundaries:
  - API layer: HTTP contract, auth, parameter normalization, MediatR dispatch.
  - Application layer: query/handler and DTO mapping.
  - Infrastructure layer: repository query behavior.
- Use MediatR query pattern consistent with `ListResumesQuery` / `ListEducationsQuery` / `ListCoverLetterTemplatesQuery`.
- Preserve existing `PagedResult<T>` response contract used across list endpoints.
- Follow async + cancellation token propagation across controller -> mediator -> handler -> repository.

### Critical Behavior Decisions for This Story

- Story AC and FR24 require default ordering by `createdAt desc`, with optional `sortBy=updatedAt|relevance` for updated-time relevance view.
- Cursor field name remains `lastSeenUpdatedAt` for compatibility, but the timestamp it carries depends on selected sort mode.
- Keep cursor contract field names unchanged (`lastSeenUpdatedAt`) for API consistency across project, but ensure paging logic remains deterministic after ordering change.
- Do not add a separate `GET /api/v1/vacancies` list route. Browse mode must use `POST /api/v1/vacancies/filter` with empty criteria.
- Story 4.2 extends the same route by supplying non-empty filter criteria.

### Existing Files To Read Before Implementation

- `backend/src/JobNecto.Infrastructure/Repositories/VacancyRepository.cs`
- `backend/src/JobNecto.Application/Interfaces/IVacancyRepository.cs`
- `backend/src/JobNecto.Domain/Entities/Vacancy.cs`
- `backend/src/JobNecto.Domain/ValueObjects/Pagination.cs`
- `backend/src/JobNecto.Infrastructure/Persistance/AppDbContext.cs`
- `backend/src/JobNecto.API/Controllers/ResumesController.cs`
- `backend/src/JobNecto.API/Controllers/EducationsController.cs`
- `backend/src/JobNecto.Application/Resumes/ListResumesQuery.cs`
- `backend/src/JobNecto.Application/Resumes/ListResumesHandler.cs`
- `backend/src/JobNecto.Application/CoverLetterTemplates/ListCoverLetterTemplatesQueryHandler.cs`
- `backend/tests/JobNecto.Tests/Infrastructure/VacancyRepositoryTests.cs`
- `backend/tests/JobNecto.Tests/Infrastructure/VacancyTestData.cs`

### Testing Notes

- Existing `VacancyRepositoryTests` already exercise pagination/filter behavior and should be extended/updated instead of duplicated.
- `VacancyTestData` provides seeded realistic fixtures and should be reused for both repository and API integration tests.
- Keep response metadata assertions aligned with existing list endpoint tests in resume/education/template test suites.

### Project Structure Notes

- New Application files should live under `backend/src/JobNecto.Application/Vacancies/` and `.../Vacancies/Mappers/`.
- New API controller should live under `backend/src/JobNecto.API/Controllers/`.
- New tests should follow established split:
  - Application tests: `backend/tests/JobNecto.Tests/Application/Vacancies/`
  - API tests: `backend/tests/JobNecto.Tests/API/Vacancies/`
  - Infrastructure tests: update existing `backend/tests/JobNecto.Tests/Infrastructure/VacancyRepositoryTests.cs`
- Namespace must match folder structure exactly.

### Git Intelligence Summary

Recent merged work confirms this workflow sequence:

1. Implement story slice in API/Application/Infrastructure.
2. Add targeted tests + full suite verification.
3. Sync implementation artifacts and sprint status docs.

Most recent commits:

- `ce7a3c4` docs(epic-3): finalize retrospective and close epic tracking
- `c0fa357` docs(post-merge): sync story 3.5 workflow docs
- `c51a8e3` story 3.5 implementation merge

### Latest Technical Information

- Current project stack is pinned around .NET 10 + EF Core 10 + MediatR 14 + FluentValidation 12.
- No dependency upgrade is required for Story 4.1.
- Use existing project patterns and avoid adding new infrastructure libraries for this story.

### References

- [Source: `_bmad-output/planning-artifacts/epics/epic-4-vacancy-browsing-filtering.md` - Story 4.1]
- [Source: `_bmad-output/planning-artifacts/epics/requirements-inventory.md` - FR24, NFR10]
- [Source: `_bmad-output/planning-artifacts/architecture/index.md`]
- [Source: `_bmad-output/planning-artifacts/architecture/core-architectural-decisions.md`]
- [Source: `_bmad-output/planning-artifacts/architecture/project-context-analysis.md`]
- [Source: `backend/src/JobNecto.Application/Interfaces/IVacancyRepository.cs`]
- [Source: `backend/src/JobNecto.Infrastructure/Repositories/VacancyRepository.cs`]
- [Source: `backend/src/JobNecto.Infrastructure/Persistance/AppDbContext.cs`]
- [Source: `backend/tests/JobNecto.Tests/Infrastructure/VacancyRepositoryTests.cs`]
- [Source: `backend/tests/JobNecto.Tests/Infrastructure/VacancyTestData.cs`]
- [Source: `backend/src/JobNecto.API/Controllers/ResumesController.cs`]
- [Source: `backend/src/JobNecto.API/Controllers/EducationsController.cs`]

## Story Completion Status

- Ultimate context engine analysis completed - comprehensive developer guide created.

## Dev Agent Record

### Agent Model Used

GPT-5.3-Codex

### Debug Log References

- Story key selected from sprint status first backlog item: `4-1-browse-vacancies-paginated-list`.
- Epic transition required: `epic-4` backlog -> in-progress.

### Completion Notes List

- Story context prepared with implementation guardrails, file-level targets, and explicit testing gates.
- Repository ordering mismatch (`updatedAt` vs required `createdAt`) identified and converted into a concrete implementation task.

### File List

- `_bmad-output/implementation-artifacts/4-1-browse-vacancies-paginated-list.md` (this file)
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (status update required by create-story workflow)
