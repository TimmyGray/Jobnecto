# Story 2.1: Create Resume

Status: not-started

## Story

As a job seeker,
I want to create a resume with my skills and work preferences,
So that I can describe what kind of role I'm looking for.

## Acceptance Criteria

1. `POST /api/v1/resumes` with `title`, at least one entry in `skills`, and `workLocationType` (remote/office/hybrid) creates a new resume for the authenticated user.
2. Returns `201 Created` with the full resume object and `Location` header set to `/api/v1/resumes/{id}`.
3. Invalid `title` (missing or empty) returns `400 Bad Request` with field-level error.
4. Empty or missing `skills` array returns `400 Bad Request` with field-level error.
5. `workLocationType` outside of allowed values (`remote`, `office`, `hybrid`) returns `400 Bad Request`.
6. Optional fields (`salary`, `currency`, `experience`, `projects`, `certifications`, `languages`, `locations`, `excludedWords`) are persisted if valid.
7. User identity is extracted from `HttpContext.User` using the existing `AuthContext.GetCurrentUserId()` extension.
8. Resume is soft-deletable (inherits from `SoftDeletableEntity` and has `IsDeleted` = false by default).

## Tasks / Subtasks

- [ ] Task 1: Define the Application contract for Resume creation (AC: 1, 3, 4, 5, 6)
  - [ ] Create `Resumes` feature slice in `JobNecto.Application`.
  - [ ] Implement `CreateResumeCommand` with all functional fields from Story 2.1.
  - [ ] Implement `CreateResumeCommandValidator` using FluentValidation.
    - [ ] Rule for `Title`: NotEmpty.
    - [ ] Rule for `Skills`: NotEmpty, MinLength(1).
    - [ ] Rule for `WorkLocationType`: Must be a valid `WorkLocationType` enum value.
    - [ ] Rule for `Currency`: Must be valid if provided.
    - [ ] Rule for `Experience`: Must be valid if provided.
  - [ ] Create `ResumeMappers.cs` with `ToEntity()` and `ToResumeResult()` methods.

- [ ] Task 2: Implement Resume creation logic (AC: 1, 7, 8)
  - [ ] Implement `CreateResumeCommandHandler`.
  - [ ] Extract `UserId` from the command (set by controller).
  - [ ] Use `IUnitOfWork.ResumeRepository` to persist the entity.
  - [ ] Ensure `IsDeleted` is handled by persistence defaults or entity initialization.

- [ ] Task 3: Expose API endpoint (AC: 1, 2)
  - [ ] Create `ResumesController` inheriting from `ControllerBase` with `[Authorize]` attribute.
  - [ ] Implement `POST /api/v1/resumes`.
  - [ ] Map authenticated `UserId` to the command before sending to MediatR.
  - [ ] Return `Created` status with the correct `Location` header.

- [ ] Task 4: Verification and Testing (AC: 1, 2, 3, 4, 5, 6)
  - [ ] Add unit tests for `CreateResumeCommandValidator`.
  - [ ] Add unit tests for `CreateResumeCommandHandler`.
  - [ ] Add integration tests for `POST /api/v1/resumes` including unauthorized and validation failure cases.
  - [ ] Verify `dotnet test backend/JobNecto.slnx` passes.

## Dev Notes

### Decision Log

1. **Mapping Pattern**: Sticking to the established `<Entity>Mappers.cs` pattern found in `Users/Mappers/UserMappers.cs`.
2. **Persistence**: Using the existing `IUnitOfWork.ResumeRepository` which is an `IEditableRepository<Resume>`.
3. **Soft Delete**: `Resume` entity already inherits from `SoftDeletableEntity`. Global query filters in `AppDbContext` are assumed to be in place (per `architecture.md`).
4. **Enums**: `WorkLocationType`, `Experience`, `Currency` are existing enums in `Domain/Enums`. String-to-Enum conversion will be handled in the Mapper or by ASP.NET Core model binding if possible, but Mapper is safer for validation consistency.

### References

- [Source: `_bmad-output/planning-artifacts/epics/epic-2-resume-education-management.md` - Story 2.1]
- [Source: `backend/src/JobNecto.Domain/Entities/Resume.cs`]
- [Source: `backend/src/JobNecto.API/Controllers/UsersController.cs`]
- [Source: `backend/src/JobNecto.Application/Users/CreateUserCommandHandler.cs`]

## Dev Agent Record

### Agent Model Used

GitHub Copilot (Gemini 1.5 Flash)

### File List

- `_bmad-output/implementation-artifacts/2-1-create-resume.md`
- `backend/src/JobNecto.Application/Resumes/CreateResumeCommand.cs`
- `backend/src/JobNecto.Application/Resumes/CreateResumeCommandHandler.cs`
- `backend/src/JobNecto.Application/Resumes/Validators/CreateResumeCommandValidator.cs`
- `backend/src/JobNecto.Application/Resumes/Mappers/ResumeMappers.cs`
- `backend/src/JobNecto.API/Controllers/ResumesController.cs`
- `backend/tests/JobNecto.Tests/Application/Resumes/CreateResumeValidatorTests.cs`
- `backend/tests/JobNecto.Tests/Application/Resumes/CreateResumeHandlerTests.cs`
- `backend/tests/JobNecto.Tests/API/ResumesApiTests.cs`
