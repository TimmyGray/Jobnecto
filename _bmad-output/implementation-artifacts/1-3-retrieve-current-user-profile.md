# Story 1.3: Retrieve Current User Profile

Status: review

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Scope Correction (2026-04-23)

- `GET /api/v1/users/me` is explicitly profile-only (core user fields).
- Resumes, educations, templates, and cover letters are served from dedicated user-scoped routes with `UserId` ownership checks.
- Planning artifacts were revised to reflect this decision (`prd.md`, `architecture.md`, epic specs, and roadmap).

## Story

As a job seeker,
I want to retrieve my current core profile data,
so that identity/profile information remains simple while resumes, educations, templates, and cover letters are managed through dedicated user-scoped routes.

## Acceptance Criteria

1. `GET /api/v1/users/me` with a valid JWT token returns `200 OK` with core profile fields only: `id`, `loginName`, `email`, `phone`, `location`, `about`, `avatar`, `createdAt`, `updatedAt`.
2. Password/hash fields are never present in the response body.
3. If the JWT references a `userId` that no longer exists in the database, `404 Not Found` is returned.
4. If no valid JWT is present in the request (no cookie, no Bearer header), `401 Unauthorized` is returned.
5. Related resources are not embedded in `/users/me`; they are retrieved via dedicated user-scoped routes (`/api/v1/resumes`, `/api/v1/educations`, `/api/v1/cover-letter-templates`, `/api/v1/cover-letters`).

## Tasks / Subtasks

- [x] Task 1: Define the query and profile-only response contract for `GET /api/v1/users/me` (AC: 1, 2)
  - [x] Create `GetCurrentUserQuery : IRequest<GetCurrentUserResult>` with `Guid UserId` in `backend/src/JobNecto.Application/Users/`.
  - [x] Keep `GetCurrentUserResult` profile-only (`id`, `loginName`, `email`, `phone`, `location`, `about`, `avatar`, `createdAt`, `updatedAt`).

- [x] Task 2: Implement handler and mapper for profile retrieval (AC: 1, 2, 3)
  - [x] Add `GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, GetCurrentUserResult>`.
  - [x] Load user by authenticated `UserId`; throw `NotFoundException` when missing.
  - [x] Map `User` to profile-only DTO in `UserMappers` and never expose password/hash fields.

- [x] Task 3: Expose authenticated endpoint in `UsersController` (AC: 1, 3, 4)
  - [x] Add `[HttpGet("me")]` + `[Authorize]` action.
  - [x] Parse authenticated user id from claims and return `401` if claim is missing/invalid.
  - [x] Return `200` profile payload and propagate `NotFoundException` to global exception handling (`404`).

- [x] Task 4: Add generic repository capability for user-scoped list queries (AC: 5)
  - [x] Extend `PagedQuery` with optional `UserId`.
  - [x] Apply `UserId` filtering in `BaseRepository.GetAsync(...)` only for entities that have a `UserId` field.
  - [x] Ensure cursor existence checks are performed inside the same filtered query scope.

- [x] Task 5: Add focused API tests (AC: 1, 2, 3, 4)
  - [x] `GET /api/v1/users/me` without auth returns `401`.
  - [x] `GET /api/v1/users/me` with valid auth returns `200` profile payload without sensitive fields.
  - [x] `GET /api/v1/users/me` with valid token for a deleted/non-existing user returns `404`.

- [x] Task 6: Add repository regression tests for user-scoped pagination behavior (AC: 5)
  - [x] Verify `GetAsync` with `PagedQuery.UserId` returns only owned records and correct total count.
  - [x] Verify cursor from another user is ignored in user-scoped queries (no cross-user cursor influence).

- [x] Task 7: Build and test validation (AC: all)
  - [x] Run targeted Story 1.3 tests.
  - [x] Run full solution tests.
  - [x] Run release build and release tests with `--warnaserror`.

### Review Findings

- [x] \[Review\]\[Decision\] `/me` response scope conflicts with prior aggregate AC2-AC4 — Resolved by product decision: keep profile-only `/users/me` and move related resources to dedicated user-scoped routes.
- [x] \[Review\]\[Defer\] Aggregate payload patch items for `resumes`, `educations`, and `coverLetters` in `/users/me` — deferred as superseded by corrected product scope.
- [x] \[Review\]\[Patch\] Add API integration coverage for authenticated missing-user path (`404`) — resolved.
- [x] \[Review\]\[Patch\] Add repository tests for `PagedQuery.UserId` filtering and cursor scope behavior — resolved.

## Dev Notes

### Previous Story Intelligence (Story 1.2)

- `UsersController` is already registered; `IMediator`, `IJwtTokenService`, and `ICookieAuthService` are already injected there.
- `HttpContext.GetCurrentUserId()` lives in `AuthContext.cs` (`JobNecto.API.Infrastructure`); it checks `ClaimTypes.NameIdentifier → sub → userId`. Use this exact method — do not reimplement.
- `CreateUserResult` already demonstrates the field mapping convention: `Login → LoginName`, `AboutMe → About`, `Location enum → string`. Apply the same in `GetCurrentUserResult`.
- Mapping extension methods live in `backend/src/JobNecto.Application/Users/Mappers/UserMappers.cs`. **Add a new overload to this file** — do not create a second mappers file.
- `CreateUserCommandHandler` pattern: injects `IUnitOfWork`, calls `_unitOfWork.UserRepository.*`, then `SaveChangesAsync`. Follow the same pattern; for reads, no `SaveChangesAsync` is needed.
- `ConflictException`, `NotFoundException`, `ForbiddenException`, `UnauthorizedException` all exist in `JobNecto.Application.Exceptions`. Use `NotFoundException` for missing user (→ global handler maps to 404).
- Story 1.2 integration tests (`UsersControllerTests.cs`) demonstrate exactly how to register with `POST /api/v1/users` and extract the cookie for subsequent authenticated requests. Reuse this pattern for `GET /api/v1/users/me`.

### Technical Requirements

- **No new DI registrations needed** in `Program.cs` or `DI.cs` unless a new repository interface is added. If you extend `IEditableRepository<T>` with a new method, make sure the implementation class (`EditableRepository<T>` or the concrete subclass) covers it.
- **Clean Architecture boundaries:** handler in Application, EF queries in Infrastructure repositories, HTTP mapping in API. Do not reference EF Core directly in `GetCurrentUserQueryHandler`.
- **Async/await all the way**: every I/O call must `await`, and every public async method must accept and forward `CancellationToken ct`.
- **Field mapping table** to keep consistent with Story 1.2 mappers:

| Domain field | JSON key |
| --- | --- |
| `User.Id` | `id` |
| `User.Login` | `loginName` |
| `User.Email` | `email` |
| `User.Phone` | `phone` |
| `User.Location` (enum) | `location` (string/null) |
| `User.AboutMe` | `about` |
| `User.Avatar` | `avatar` |
| `User.CreatedAt` | `createdAt` |
| `User.UpdatedAt` | `updatedAt` |
| `User.Password` | **NEVER include** |

### Architecture Compliance

- `GET /api/v1/users/me` must be a **query** (not a command); result type must be a read DTO, not a command result.
- Ownership enforcement: the endpoint authenticates the user via JWT, extracts `UserId` from claims, and queries only that user's data. The handler never returns another user's data.
- EF Core global query filters on `SoftDeletableEntity` automatically exclude `IsDeleted = true` rows — verified by checking `AppDbContext.OnModelCreating` for existing filter registrations before adding manual predicates.
- All list results (resumes, educations, cover letters) must be filtered by the authenticated `UserId` at the repository layer, not in the handler.
- Error contract: throw `NotFoundException` from handler; global `GlobalExceptionHandler` maps it to `404 Not Found` with RFC 7808 Problem Details — no try/catch in the controller action.

### Library / Framework Requirements

- **MediatR 14**: query implements `IRequest<GetCurrentUserResult>`; handler implements `IRequestHandler<GetCurrentUserQuery, GetCurrentUserResult>`. No new package references.
- **EF Core 10**: use `AsNoTracking()`, `FirstOrDefaultAsync()`, `ToListAsync()`, `AnyAsync()` from `Microsoft.EntityFrameworkCore`. Already referenced in Infrastructure project.
- **xUnit + FluentAssertions + Moq**: same packages as all other tests — no additions needed.
- **No AutoMapper or external mapping libraries** — follow the static extension method pattern in `UserMappers.cs`.

### Repository Design Decision

Examine `EditableRepository<T>` in `backend/src/JobNecto.Infrastructure/Repositories/EditableRepository.cs` before implementation. The generic `GetAsync(PagedQuery, ct)` likely does NOT filter by `UserId`.

**Recommended approach (preferred):**

- Add `Task<IReadOnlyList<Resume>> GetByUserIdAsync(Guid userId, CancellationToken ct)` to `ResumeRepository` (in `Infrastructure`) and expose it via an updated interface or the existing `IEditableRepository<Resume>`.
- Repeat for `EducationRepository` and `CoverLetterRepository`.
- **Do NOT** load all entities into memory and filter client-side.

**Alternative (if minimal change is preferred):**

- Query via the existing generic interface with an appropriate predicate overload. Only use this if the base repository genuinely supports it.

### File Structure Requirements

- New files to create:
  - `backend/src/JobNecto.Application/Users/GetCurrentUserQuery.cs` — query, result DTO, and nested DTOs
  - `backend/src/JobNecto.Application/Users/GetCurrentUserQueryHandler.cs` — handler
  - `backend/tests/JobNecto.Tests/Application/Users/GetCurrentUserHandlerTests.cs` — handler unit tests
- Files to modify:
  - `backend/src/JobNecto.Application/Users/Mappers/UserMappers.cs` — add `ToGetCurrentUserResult(...)` extension
  - `backend/src/JobNecto.API/Controllers/UsersController.cs` — add `GET me` action
  - `backend/src/JobNecto.Infrastructure/Repositories/ResumeRepository.cs` — add `GetByUserIdAsync` if needed
  - `backend/src/JobNecto.Infrastructure/Repositories/EducationRepository.cs` — add `GetByUserIdAsync` if needed
  - `backend/src/JobNecto.Infrastructure/Repositories/CoverLetterRepository.cs` — add `GetByUserIdAsync` if needed
  - `backend/tests/JobNecto.Tests/API/UsersControllerTests.cs` — add `GET me` integration tests
  - Corresponding `IEditableRepository<T>` or a new specific interface if method is added

### Testing Requirements

- **Handler unit tests** (`GetCurrentUserHandlerTests.cs`):
  - Use `Mock<IUnitOfWork>` + `Mock<IUserRepository>` + `Mock<IEditableRepository<Resume>>` etc. (Moq 4.20.72).
  - Set up `GetByIdAsync` to return a valid `User` and test the result shape.
  - Set up `GetByIdAsync` to return `null`; assert `NotFoundException` is thrown.
  - Set up resume/education/cover-letter repositories to return mixed `IsDeleted` data (though global filters handle this at EF level, mock the return at repository level for unit test isolation).
  - Verify cover letter `Recent` is sorted `createdAt desc` and capped at 5.
  - Verify password is absent from any returned DTO property.

- **Integration tests** (`UsersControllerTests.cs`):
  - Pattern: call `POST /api/v1/users` to register → capture the `Set-Cookie` header → replay cookie on `GET /api/v1/users/me`.
  - Assert `200 OK`, `loginName` matches registered value, `email` matches, `resumes` and `educations` are empty arrays (freshly created user), `coverLetters.total == 0`.
  - Call `GET /api/v1/users/me` without any auth → assert `401`.
  - Response JSON must NOT contain a field whose name contains "password" or "hash" (case-insensitive check).

### Project Structure Notes

- `IUnitOfWork` already exposes `ResumeRepository`, `EducationRepository`, and `CoverLetterRepository` as `IEditableRepository<T>`. If you need `GetByUserIdAsync`, add it to the `IEditableRepository<T>` interface OR create specific `IResumeRepository`, `IEducationRepository`, `ICoverLetterRepository` interfaces and update `IUnitOfWork` — whichever is consistent with the project's existing direction (check how `IUserRepository` extends `IRepository<User>` for reference).
- `AppDbContext.OnModelCreating` may already define soft-delete query filters for Resume, Education, CoverLetter. Check `backend/src/JobNecto.Infrastructure/Persistance/Config/` before adding manual `!entity.IsDeleted` predicates.
- `SoftDeletableEntity` has both `IsDeleted` (bool) and `DeletedAt` (DateTime?) fields.
- All entity fields in Domain layer use **public fields, not properties** (e.g., `public string? Title;` not `public string? Title { get; set; }`). Be aware of this when writing EF queries or manual mappings.

### Git Intelligence Summary

- Stories 1.1, 1.2, and 1.5 are all merged to `master`. The controller, auth middleware, global exception handler, cookie auth service, JWT token service, unit of work, and user repository are all production-ready.
- Story 1.2 added `UsersController`, `CreateUserCommand`, `CreateUserCommandHandler`, `UserMappers`, and all auth infrastructure. Story 1.3 extends these surfaces — no replacement of existing code.

### References

- [Source: `_bmad-output/planning-artifacts/epics/epic-1-foundation-user-profile-management.md` — Story 1.3]
- [Source: `_bmad-output/planning-artifacts/prd.md` — Feature: Retrieve Current User (GET /api/v1/users/me)]
- [Source: `_bmad-output/planning-artifacts/architecture.md` — Decision 1 (MediatR), Decision 3 (Repository), Decision 5 (Async), Decision 7 (Ownership)]
- [Source: `backend/src/JobNecto.API/Controllers/UsersController.cs`]
- [Source: `backend/src/JobNecto.API/Infrastructure/AuthContext.cs`]
- [Source: `backend/src/JobNecto.Application/Users/CreateUserCommand.cs`]
- [Source: `backend/src/JobNecto.Application/Users/CreateUserCommandHandler.cs`]
- [Source: `backend/src/JobNecto.Application/Users/Mappers/UserMappers.cs`]
- [Source: `backend/src/JobNecto.Application/Interfaces/IUnitOfWork.cs`]
- [Source: `backend/src/JobNecto.Application/Interfaces/IUserRepository.cs`]
- [Source: `backend/src/JobNecto.Application/Interfaces/IRepository.cs`]
- [Source: `backend/src/JobNecto.Application/Exceptions/NotFoundException.cs`]
- [Source: `backend/src/JobNecto.Domain/Entities/User.cs`]
- [Source: `backend/src/JobNecto.Domain/Entities/Resume.cs`]
- [Source: `backend/src/JobNecto.Domain/Entities/Education.cs`]
- [Source: `backend/src/JobNecto.Domain/Entities/CoverLetter.cs`]
- [Source: `backend/src/JobNecto.Domain/Entities/SoftDeletableEntity.cs`]
- [Source: `backend/src/JobNecto.Infrastructure/Persistance/UnitOfWork.cs`]
- [Source: `backend/src/JobNecto.Infrastructure/Repositories/UserRepository.cs`]
- [Source: `backend/tests/JobNecto.Tests/API/JobNectoApiFactory.cs`]
- [Source: `backend/tests/JobNecto.Tests/API/UsersControllerTests.cs`]
- [Source: `_bmad-output/implementation-artifacts/1-2-create-user-account.md`]

## Dev Agent Record

### Agent Model Used

GitHub Copilot (GPT-5.3-Codex)

### Debug Log References

- `dotnet build backend/JobNecto.slnx` → passed
- `dotnet test backend/JobNecto.slnx` → passed (`136` total, `136` succeeded, `0` failed)

### Completion Notes List

- Implemented `GET /api/v1/users/me` as an authenticated MediatR query endpoint in `UsersController`.
- Added current-user profile contracts in `GetCurrentUserQuery.cs` with nested DTOs for resumes, educations, and cover letter summary.
- Implemented `GetCurrentUserQueryHandler` with strict ownership filtering by authenticated `UserId` and `NotFoundException` mapping for missing users.
- Extended `UserMappers` with `ToGetCurrentUserResult(...)` while preserving password exclusion.
- Added dedicated repository interfaces (`IResumeRepository`, `IEducationRepository`, `ICoverLetterRepository`) with `GetByUserIdAsync(...)` and wired them through `IUnitOfWork` + `UnitOfWork`.
- Implemented `GetByUserIdAsync(...)` in `ResumeRepository`, `EducationRepository`, and `CoverLetterRepository` using `AsNoTracking()`.
- Added `GetCurrentUserHandlerTests` covering success, missing user, soft-delete exclusions, and recent cover letter ordering/capping.
- Extended `UsersControllerTests` with unauthorized and authorized `/users/me` integration coverage and sensitive field assertions.
- Updated `JobNectoApiFactory` to keep one in-memory DB per factory instance, then made `UsersControllerTests` instantiate a fresh factory per test to preserve test isolation.

### File List

- `_bmad-output/implementation-artifacts/1-3-retrieve-current-user-profile.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `backend/src/JobNecto.API/Controllers/UsersController.cs`
- `backend/src/JobNecto.Application/Interfaces/ICoverLetterRepository.cs`
- `backend/src/JobNecto.Application/Interfaces/IEducationRepository.cs`
- `backend/src/JobNecto.Application/Interfaces/IResumeRepository.cs`
- `backend/src/JobNecto.Application/Interfaces/IUnitOfWork.cs`
- `backend/src/JobNecto.Application/Users/GetCurrentUserQuery.cs`
- `backend/src/JobNecto.Application/Users/GetCurrentUserQueryHandler.cs`
- `backend/src/JobNecto.Application/Users/Mappers/UserMappers.cs`
- `backend/src/JobNecto.Infrastructure/Persistance/UnitOfWork.cs`
- `backend/src/JobNecto.Infrastructure/Repositories/CoverLetterRepository.cs`
- `backend/src/JobNecto.Infrastructure/Repositories/EducationRepository.cs`
- `backend/src/JobNecto.Infrastructure/Repositories/ResumeRepository.cs`
- `backend/tests/JobNecto.Tests/API/JobNectoApiFactory.cs`
- `backend/tests/JobNecto.Tests/API/UsersControllerTests.cs`
- `backend/tests/JobNecto.Tests/Application/Users/GetCurrentUserHandlerTests.cs`

## Change Log

- 2026-04-23: Implemented Story 1.3 end-to-end (`GET /api/v1/users/me`), added user-scoped repository query contracts and infrastructure implementations, and added unit/integration coverage. Validated with full `dotnet build` and `dotnet test` on `backend/JobNecto.slnx`.
