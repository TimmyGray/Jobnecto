# Story 2.1: Create Resume

Status: done

## Story

As a job seeker,
I want to create a resume with my skills and work preferences,
So that I can describe what kind of role I'm looking for.

## Acceptance Criteria

1. `POST /api/v1/resumes` creates a new resume for the authenticated user. All profile fields (`title`, `skills`, `workLocationType`) are **optional**; an empty resume payload is valid.
2. Returns `201 Created` with the full resume object and `Location` header set to `/api/v1/resumes/{id}`.
3. If `title` is provided it must not exceed 200 characters; otherwise it is accepted as null/empty.
4. If `skills` is provided, each individual skill must not exceed 30 characters; an empty array or null is accepted.
5. If `workLocationType` is provided it must be a valid `WorkLocationType` enum value (`OnSite`, `Remote`, `Hybrid`; case-insensitive); an invalid value returns `400 Bad Request`. If omitted the field is persisted as null.
6. Optional fields (`salary`, `currency`, `experience`, `projects`, `certifications`, `languages`, `locations`, `excludedWords`) are persisted if valid. If `currency` is provided it must match a valid `Currency` enum value (e.g. `USD`, `EUR`, `UAH`). If `experience` is provided it must match a valid `Experience` enum value (`LessThanOneYear`, `OneToThreeYears`, `ThreeToFiveYears`, `MoreThanFiveYears`).
7. User identity is extracted from `HttpContext.User` using the existing `AuthContext.GetCurrentUserId()` extension.
8. Resume is soft-deletable (inherits from `SoftDeletableEntity` and has `IsDeleted` = false by default).

## Tasks / Subtasks

- [x] Task 1: Define the Application contract for Resume creation (AC: 1, 3, 4, 5, 6)
  - [x] Create `Resumes` feature slice in `JobNecto.Application`.
  - [x] Implement `CreateResumeCommand` with all functional fields from Story 2.1.
  - [x] Implement `CreateResumeCommandValidator` using FluentValidation.
    - [x] Rule for `Title`: MaxLength(200) if provided; optional otherwise.
    - [x] Rule for `Skills`: each skill MaxLength(30) if array provided; optional otherwise.
    - [x] Rule for `WorkLocationType`: Must be a valid `WorkLocationType` enum value if provided (`Enum.TryParse`, case-insensitive).
    - [x] Rule for `Currency`: Must be a valid `Currency` enum value if provided.
    - [x] Rule for `Experience`: Must be a valid `Experience` enum value if provided.
  - [x] Create `ResumeMappers.cs` with `ToEntity()` and `ToResumeResult()` methods.

- [x] Task 2: Implement Resume creation logic (AC: 1, 7, 8)
  - [x] Implement `CreateResumeCommandHandler`.
  - [x] Extract `UserId` from the command (set by controller).
  - [x] Use `IUnitOfWork.ResumeRepository` to persist the entity.
  - [x] Ensure `IsDeleted` is handled by persistence defaults or entity initialization.

- [x] Task 3: Expose API endpoint (AC: 1, 2)
  - [x] Create `ResumesController` inheriting from `ControllerBase` with `[Authorize]` attribute.
  - [x] Implement `POST /api/v1/resumes`.
  - [x] Map authenticated `UserId` to the command before sending to MediatR.
  - [x] Return `Created` status with the correct `Location` header.

- [x] Task 4: Verification and Testing (AC: 1, 2, 3, 4, 5, 6)
  - [x] Add unit tests for `CreateResumeCommandValidator`.
  - [x] Add unit tests for `CreateResumeCommandHandler`.
  - [x] Add integration tests for `POST /api/v1/resumes` including unauthorized and validation failure cases.
  - [x] Verify `dotnet test backend/JobNecto.slnx` passes.

## Dev Notes

### Decision Log

1. **Mapping Pattern**: Sticking to the established `<Entity>Mappers.cs` pattern found in `Users/Mappers/UserMappers.cs`.
2. **Persistence**: Using the existing `IUnitOfWork.ResumeRepository` which is an `IEditableRepository<Resume>`.
3. **Soft Delete**: `Resume` entity already inherits from `SoftDeletableEntity`. Global query filters in `AppDbContext` are assumed to be in place (per `architecture.md`).
4. **Enums**: `WorkLocationType`, `Experience`, `Currency` are existing enums in `Domain/Enums`. String-to-Enum conversion will be handled in the Mapper or by ASP.NET Core model binding if possible, but Mapper is safer for validation consistency.

### References

- [Source: `_bmad-output/archive/planning-artifacts/epics/epic-2-resume-education-management.md` - Story 2.1]
- [Source: `backend/src/JobNecto.Domain/Entities/Resume.cs`]
- [Source: `backend/src/JobNecto.API/Controllers/UsersController.cs`]
- [Source: `backend/src/JobNecto.Application/Users/CreateUserCommandHandler.cs`]

## Dev Agent Record

### Agent Model Used

GitHub Copilot (Gemini 1.5 Flash)

### File List

- `_bmad-output/archive/implementation-artifacts/2-1-create-resume.md`
- `backend/src/JobNecto.Application/Resumes/CreateResumeCommand.cs`
- `backend/src/JobNecto.Application/Resumes/CreateResumeCommandHandler.cs`
- `backend/src/JobNecto.Application/Resumes/Validators/CreateResumeCommandValidator.cs`
- `backend/src/JobNecto.Application/Resumes/Mappers/ResumeMappers.cs`
- `backend/src/JobNecto.API/Controllers/ResumesController.cs`
- `backend/tests/JobNecto.Tests/Application/Resumes/CreateResumeValidatorTests.cs`
- `backend/tests/JobNecto.Tests/Application/Resumes/CreateResumeHandlerTests.cs`
- `backend/tests/JobNecto.Tests/API/ResumesApiTests.cs`

