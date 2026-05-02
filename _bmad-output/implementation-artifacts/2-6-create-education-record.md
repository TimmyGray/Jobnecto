# Story 2.6: Create Education Record

Status: done

## Story

As a job seeker,
I want to add an education record to my profile,
so that employers see my academic background.

## Acceptance Criteria

1. `POST /api/v1/educations` requires a valid JWT token. Unauthenticated requests return `401 Unauthorized`.
2. Given a valid JWT token and `POST /api/v1/educations` with `title`, `specialization`, and `degree` (`bachelor`/`master`/`phd`/`postdoc`/`other`), returns `201 Created` with the full education object and `Location` header `/api/v1/educations/{id}`.
3. If `title` is missing or empty, returns `400 Bad Request` with a field-level error on `title`.
4. If `degree` is not one of `bachelor`, `master`, `phd`, `postdoc`, `other`, returns `400 Bad Request` with a field-level error on `degree`.

## Tasks / Subtasks

- [x] Task 1: Define education domain and application create contract (AC: 2, 3, 4)
  - [x] Define `backend/src/JobNecto.Domain/Entities/Education.cs` with required fields: `Title`, `Specialization`, `Degree`.
  - [x] Align degree enum contract in `backend/src/JobNecto.Domain/Enums/Degree.cs` to support `Bachelor`, `Master`, `PhD`, `PostDoc`, `Other`.
  - [x] Configure `backend/src/JobNecto.Infrastructure/Persistance/Config/EducationConfiguration.cs` for entity persistence.
  - [x] Create `backend/src/JobNecto.Application/Educations/CreateEducationCommand.cs` with request/response DTOs.
  - [x] Create `backend/src/JobNecto.Application/Educations/Mappers/EducationMappers.cs` for command/entity and entity/result mapping.
  - [x] Create `backend/src/JobNecto.Application/Educations/Validators/CreateEducationCommandValidator.cs` with rules:
    - [x] `Title` required, non-empty, max length 100.
    - [x] `Specialization` required, non-empty, max length 100.
    - [x] `Degree` required and restricted to valid enum values (case-insensitive).
    - [x] `UserId` required and non-empty.

- [x] Task 2: Implement Application handler create flow (AC: 2)
  - [x] Create `backend/src/JobNecto.Application/Educations/CreateEducationCommandHandler.cs`.
  - [x] Map command to entity and set `UserId` from auth context supplied by controller.
  - [x] Persist through `_unitOfWork.EducationRepository.CreateAsync(entity, cancellationToken)` and `_unitOfWork.SaveChangesAsync(cancellationToken)`.
  - [x] Return mapped `EducationResult` from persisted entity.

- [x] Task 3: Expose authenticated HTTP endpoint (AC: 1, 2, 3, 4)
  - [x] Create `backend/src/JobNecto.API/Controllers/EducationsController.cs` with `[ApiController]`, `[Route("api/v1/educations")]`, and `[Authorize]`.
  - [x] Add `POST` action accepting `CreateEducationCommand`.
  - [x] Extract `UserId` via `HttpContext.GetCurrentUserId()` and return `Unauthorized()` if parse fails.
  - [x] Assign `command.UserId`, dispatch via MediatR, return `Created($"/api/v1/educations/{result.Id}", result)`.
  - [x] Add response contracts for `201`, `400`, and `401`.

- [x] Task 4: Add comprehensive tests and verification gates (AC: 1, 2, 3, 4)
  - [x] Add validator tests in `backend/tests/JobNecto.Tests/Application/Educations/CreateEducationCommandValidatorTests.cs`:
    - [x] valid payload passes.
    - [x] missing/empty title fails.
    - [x] missing/empty specialization fails.
    - [x] invalid degree fails.
  - [x] Add handler tests in `backend/tests/JobNecto.Tests/Application/Educations/CreateEducationCommandHandlerTests.cs`:
    - [x] valid create persists once and returns mapped result.
  - [x] Add API integration tests in `backend/tests/JobNecto.Tests/API/Educations/EducationsApiTests.cs`:
    - [x] no token returns `401`.
    - [x] valid request returns `201` with `Location` and response payload.
    - [x] missing/empty `title` returns `400` with field-level error.
    - [x] invalid `degree` returns `400` with field-level error.
  - [x] Run targeted education tests.
  - [x] Run full suite: `dotnet test backend/JobNecto.slnx`.
  - [x] Run CI parity build/test: `dotnet build backend/JobNecto.slnx --configuration Release --warnaserror` and `dotnet test backend/JobNecto.slnx --configuration Release --no-build --warnaserror`.

## Dev Notes

### Technical Requirements

- Keep Clean Architecture boundaries: API -> Application -> Domain.
- Reuse existing `IUnitOfWork` and `IEditableRepository<Education>` abstractions; do not introduce a dedicated education repository interface for this story.
- Use FluentValidation in the MediatR pipeline for field-level errors; rely on `GlobalExceptionHandler` for RFC7807 Problem Details output.
- Persist timestamps in UTC via existing base entity behavior and EF defaults.
- Keep namespace declarations aligned with folder structure.
- Degree response is normalized to lowercase (e.g. `"bachelor"`) to match accepted input casing.

### Architecture Compliance

- Follow established CQRS vertical-slice pattern used by `Resumes` features:
  - command/handler in Application
  - mapper extensions for DTO <-> entity
  - controller limited to auth context extraction and MediatR dispatch
- Do not add cross-layer dependencies or bypass MediatR pipeline.

### File Structure Requirements

- New/updated files are limited to:
  - `_bmad-output/implementation-artifacts/2-6-create-education-record.md`
  - `_bmad-output/implementation-artifacts/sprint-status.yaml`
  - `backend/src/JobNecto.Domain/Entities/Education.cs`
  - `backend/src/JobNecto.Domain/Enums/Degree.cs`
  - `backend/src/JobNecto.Infrastructure/Persistance/Config/EducationConfiguration.cs`
  - `backend/src/JobNecto.Infrastructure/Migrations/AppDbContextModelSnapshot.cs`
  - `backend/src/JobNecto.Application/Educations/CreateEducationCommand.cs`
  - `backend/src/JobNecto.Application/Educations/CreateEducationCommandHandler.cs`
  - `backend/src/JobNecto.Application/Educations/Mappers/EducationMappers.cs`
  - `backend/src/JobNecto.Application/Educations/Validators/CreateEducationCommandValidator.cs`
  - `backend/src/JobNecto.API/Controllers/EducationsController.cs`
  - `backend/tests/JobNecto.Tests/Application/Educations/CreateEducationCommandValidatorTests.cs`
  - `backend/tests/JobNecto.Tests/Application/Educations/CreateEducationCommandHandlerTests.cs`
  - `backend/tests/JobNecto.Tests/API/Educations/EducationsApiTests.cs`

### Testing Requirements

- Use `xUnit` + `FluentAssertions` for unit tests and `JobNectoApiFactory` for integration tests.
- For API auth in tests, follow existing cookie-forwarding approach (`HandleCookies = false` and explicit `Cookie` header) used by `ResumesControllerTests`.
- Ensure tests cover AC-specific negative paths (title, degree, unauthorized).
- Full regression and CI parity checks are required before story completion.

### Previous Story Intelligence (2.5)

- Reuse the resume slice pattern: controller-only auth extraction, handler-only business persistence.
- Keep response contracts explicit and aligned with ACs.
- Preserve test isolation by using a fresh `JobNectoApiFactory` per test.

### References

- [Source: `_bmad-output/planning-artifacts/epics/epic-2-resume-education-management.md` - Story 2.6]
- [Source: `_bmad-output/planning-artifacts/prd.md` - Education Resource / Create Education]
- [Source: `_bmad-output/planning-artifacts/architecture.md` - CQRS, validation pipeline, clean architecture]
- [Source: `_bmad-output/project-context.md` - test patterns, namespace rules, build/test commands]
- [Source: `backend/src/JobNecto.API/Controllers/ResumesController.cs`]
- [Source: `backend/src/JobNecto.Application/Resumes/CreateResumeCommand.cs`]

## Story Completion Status

- Ultimate context engine analysis completed - comprehensive developer guide created.

## Dev Agent Record

### Agent Model Used

GitHub Copilot (GPT-5.3-Codex) + Claude Sonnet 4.6

### Debug Log References

- `dotnet test e:/apps/Jobnecto/backend/JobNecto.slnx --filter "FullyQualifiedName~Educations"` (green, 21/21)
- `runTests(mode=run)` (green, 248/248)
- `dotnet build e:/apps/Jobnecto/backend/JobNecto.slnx --configuration Release --warnaserror` (green)
- `dotnet test e:/apps/Jobnecto/backend/JobNecto.slnx --configuration Release --no-build --warnaserror` (green, 253/253)

### Completion Notes List

- Implemented Story 2.6 create-education vertical slice with authenticated endpoint `POST /api/v1/educations`.
- Required fields only: `Title`, `Specialization`, `Degree` — optional fields (Institution, GraduationYear, Gpa) removed after PRD rework.
- Degree enum supports: `Bachelor`, `Master`, `PhD`, `PostDoc`, `Other`; `Certificate` removed per updated spec.
- Degree response normalized to lowercase to match accepted input casing.
- Degree parse failure in mapper now throws `InvalidOperationException` (fail-fast guard).
- `GetCurrentUserId()` null guard made explicit in controller.
- `EducationResult` required fields (`Title`, `Specialization`, `Degree`) are non-nullable.
- Added comprehensive unit and integration coverage for unauthorized, validation failures, and successful creation.
- Verified targeted tests, full regression suite, and Release warn-as-error CI-parity gates.

### File List

- `_bmad-output/implementation-artifacts/2-6-create-education-record.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `backend/src/JobNecto.Domain/Entities/Education.cs`
- `backend/src/JobNecto.Domain/Enums/Degree.cs`
- `backend/src/JobNecto.Infrastructure/Persistance/Config/EducationConfiguration.cs`
- `backend/src/JobNecto.Infrastructure/Migrations/AppDbContextModelSnapshot.cs`
- `backend/src/JobNecto.Application/Educations/CreateEducationCommand.cs`
- `backend/src/JobNecto.Application/Educations/CreateEducationCommandHandler.cs`
- `backend/src/JobNecto.Application/Educations/Mappers/EducationMappers.cs`
- `backend/src/JobNecto.Application/Educations/Validators/CreateEducationCommandValidator.cs`
- `backend/src/JobNecto.API/Controllers/EducationsController.cs`
- `backend/tests/JobNecto.Tests/Application/Educations/CreateEducationCommandValidatorTests.cs`
- `backend/tests/JobNecto.Tests/Application/Educations/CreateEducationCommandHandlerTests.cs`
- `backend/tests/JobNecto.Tests/API/Educations/EducationsApiTests.cs`

## Change Log

- 2026-05-01: Created Story 2.6 implementation artifact and initialized AC-mapped task checklist.
- 2026-05-01: Implemented Story 2.6 education create flow, added test coverage, and passed full verification gates.
- 2026-05-01: Reworked — removed optional fields (Institution, GraduationYear, Gpa) per updated PRD; updated degree contract to Bachelor/Master/PhD/PostDoc/Other (Certificate removed).

### Review Findings

- [x] \[Review\]\[Patch\] Fail fast on Degree parse failure in mapper — `Enum.TryParse` result is now checked; throws `InvalidOperationException` on unexpected parse failure.

- [x] \[Review\]\[Patch\] Make `EducationResult` required fields non-nullable — `Title`, `Specialization`, `Degree` are now `string` (non-nullable) in `EducationResult`.

- [x] \[Review\]\[Patch\] Explicitly handle missing or malformed `GetCurrentUserId()` in controller — Added `string.IsNullOrWhiteSpace` guard before `Guid.TryParse`.

- [x] \[Review\]\[Patch\] Normalize `Degree` string in response to lowercase — `education.Degree.ToString().ToLowerInvariant()` used in mapper.

- [x] \[Review\]\[Defer\] Validate UserId existence before persist — deferred; FK cascade produces a DB error; option B (catch FK violation) deferred to infrastructure design.

- [x] \[Review\]\[Defer\] Idempotency: Implement idempotency key support for POST /api/v1/educations — deferred, pre-existing.

- [x] \[Review\]\[Defer\] Concurrent FK race between validation and persist — deferred to infra/transaction design.

- [x] \[Review\]\[N/A\] Add DB CHECK constraint for `GraduationYear` — N/A; GraduationYear field removed per PRD rework.

- [x] \[Review\]\[N/A\] Enforce GPA precision and rounding validation — N/A; Gpa field removed per PRD rework.

- [x] \[Review\]\[N/A\] Reject whitespace-only `Institution` values — N/A; Institution field removed per PRD rework.

- [x] \[Review\]\[N/A\] Align `Degree` enum with spec (remove `PostDoc`, `Other`) — N/A; spec updated — `PostDoc` and `Other` are now valid values; `Certificate` removed.

- [x] \[Review\]\[N/A\] Convert `Education.UserId` from field to auto-property — N/A; `Resume.UserId` also uses a field; converting Education alone would be inconsistent. Deferred to a global entity convention cleanup.
