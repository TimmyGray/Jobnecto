# Story 1.2: Create User Account

Status: ready-for-dev

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a job seeker,
I want to register a new account with my login name, email, and password,
so that I can receive a JWT token and start managing my profile.

## Acceptance Criteria

1. `POST /api/v1/users` with valid `loginName`, `email`, and `password` creates a new user, returns `201 Created`, returns the created user object without password fields, sets a signed JWT token in an HTTP-Only secure cookie (SameSite=Strict, Secure flag in Production), and sets `Location` header to `/api/v1/users/me`.
2. Invalid `loginName` values shorter than 3 characters, longer than 20 characters, or containing characters outside alphanumeric and underscore return `400 Bad Request` with a field-level validation error.
3. Invalid `email` values return `400 Bad Request` with a field-level validation error.
4. `password` values shorter than 8 characters return `400 Bad Request` with a field-level validation error.
5. Duplicate `email` or `loginName` values return `409 Conflict` with a descriptive message indicating which field is already taken.
6. Optional `phone` values that are not valid E.164 strings return `400 Bad Request` with a field-level validation error.

## Tasks / Subtasks

- [x] Task 1: Define the request, response, command, and validation contract for user registration (AC: 1, 2, 3, 4, 6)
  - [x] Create a `Users` feature slice in `JobNecto.Application` for the create-account command, handler, validator, and DTOs.
  - [x] Model the public API contract with `loginName`, `email`, `password`, optional `phone`, optional `location`, optional `about`, and optional `avatar`.
  - [x] Map public contract names to the existing domain model fields: `loginName -> User.Login`, `about -> User.AboutMe`, and `location -> User.Location`.
  - [x] Add FluentValidation rules for login name, email, password, and optional phone; keep validation messages usable in Problem Details `errors` output.
  - [x] Treat `location` as the existing domain `Location` enum serialized as a string unless product requirements are explicitly changed first.
  - [x] Create mapping extension methods in `Users/Mappers/UserMappers.cs`: `CreateUserCommand.ToEntity()` and `User.ToCreateUserResult()` for DTO<->Entity conversion. This pattern should be replicated for all entities (Resume, Education, CoverLetter, etc.) as `<Entity>Mappers.cs` files.

 - [x] Task 2: Extend persistence and token issuance support without bypassing the existing architecture (AC: 1, 5)
 - [x] Introduce a user-specific repository abstraction or extend the existing repository/unit-of-work contract so handlers can query by `email` and `loginName` without using `AppDbContext` directly.
 - [x] Add database-level uniqueness protection for `Users.Email` and `Users.Login` via EF Core configuration and a migration.
 - [x] Implement a JWT token service that uses the existing `JwtSettings` configuration and emits the created user ID as a GUID string claim compatible with `AuthContext.GetCurrentUserId()`.
 - [x] Include at least `ClaimTypes.NameIdentifier` and `sub`; include `userId` as well if needed to keep compatibility with the current auth test pattern.
 - [x] Keep password hashing out of scope for this story; use the current domain model as-is and leave password-hardening work to the later phase that introduces hashing.

- [x] Task 3: Implement the application handler and API endpoint for anonymous registration (AC: 1, 5)
  - [x] Register controllers, MediatR, validators, and the JWT token service in the API composition root.
  - [x] Create `UsersController` with `POST /api/v1/users` and mark the registration endpoint as anonymous so account creation is not blocked by JWT auth.
  - [x] Implement the handler to validate uniqueness, create the `User` entity, save changes, generate the JWT token, and return the response DTO without password fields.
  - [x] Set the `Location` header to `/api/v1/users/me` on success.
  - [x] Preserve the existing global exception handling flow so validation failures still surface as `400` Problem Details and duplicate values surface as `409 Conflict`.

- [ ] Task 4: Add focused tests for validation, handler behavior, and end-to-end HTTP behavior (AC: 1, 2, 3, 4, 5, 6)
  - [ ] Add validator tests for login name, email, password, and phone boundaries.
  - [ ] Add handler tests for successful user creation, duplicate email rejection, duplicate login rejection, and JWT generation.
  - [ ] Add API integration tests for `POST /api/v1/users` success, validation failures, duplicate conflicts, `Location` header, and password omission from the response body.
  - [ ] Verify that a token returned by registration is structurally valid for the configured issuer/audience and contains claims compatible with `AuthContext.GetCurrentUserId()`.
  - [ ] Use isolated in-memory databases or equivalent test-host overrides so tests do not depend on a real PostgreSQL instance.

- [ ] Task 5: Run project validation before moving the story to development review (AC: 1, 2, 3, 4, 5, 6)
  - [ ] Run `dotnet build backend/JobNecto.slnx`.
  - [ ] Run `dotnet test backend/JobNecto.slnx`.

## Dev Notes

### Previous Story Intelligence

- Story 1.1 already wired JWT bearer authentication, `AuthContext.GetCurrentUserId()`, and global RFC 7807 exception handling in the API layer.
- `Program.cs` already calls `AddInfrastructure(builder.Configuration)`, `AddJwtAuthentication(builder.Configuration)`, `AddExceptionHandler<GlobalExceptionHandler>()`, and `AddProblemDetails()`.
- Authentication tests already verify issuer, audience, signing key, expiry, and claim compatibility. Reuse those conventions instead of inventing a new token shape.
- The previous story completed with passing solution build and test runs, so Story 1.2 should extend those surfaces rather than replacing them.

### Technical Requirements

- Keep the implementation inside Clean Architecture boundaries: API maps HTTP to application requests, Application owns command/validation logic, Infrastructure owns EF Core and JWT token implementation, Domain remains persistence-ignorant.
- Do not inject `AppDbContext` into the handler. The current `IUnitOfWork.UserRepository` is typed as `IRepository<User>` and does not expose lookup methods, so this story needs an explicit repository contract change or a dedicated user repository abstraction.
- `AuthContext.GetCurrentUserId()` currently checks `ClaimTypes.NameIdentifier`, then `sub`, then `userId`, and returns `string?`. Emit the persisted `User.Id` as a GUID string claim so future `/me` endpoints can use the token without rework.
- The current domain model uses `User.Login`, `User.Password`, `User.AboutMe`, and `User.Location` (`Location` enum). Do not rename domain fields just to match the public API contract.
- `UserConfiguration` already enforces DB check constraints for email and E.164 phone. Story 1.2 still needs application-level validation and uniqueness enforcement for friendly error responses.
- Password hashing is intentionally deferred. Do not add hashing libraries or auth flows outside the current acceptance criteria.

### Architecture Compliance

- Follow the CQRS direction from the architecture artifact: registration should be a MediatR command, not controller-inline business logic.
- Keep `POST /api/v1/users` anonymous even though JWT auth middleware is registered globally; only protected endpoints should require tokens.
- Keep error handling delegated to the existing global exception handler by throwing application exceptions for conflicts and letting FluentValidation surface field errors.
- Return `201 Created` with the created resource payload and `Location: /api/v1/users/me`; do not return password fields.
- **JWT Token Handling:** Set the JWT token as an HTTP-Only secure cookie (not in response body) with `SameSite=Strict` and `Secure` flag enabled in Production. This prevents XSS token theft and simplifies client-side handling (browser automatically includes cookie on subsequent requests). The response body should NOT contain a token field.
- Preserve OpenAPI generation by using explicit request/response models and normal ASP.NET Core endpoint metadata.

### Library / Framework Requirements

- Use the packages already in the solution: ASP.NET Core 10, MediatR 14, FluentValidation 12, EF Core 10, and the existing JWT bearer stack.
- Reuse the existing `JwtSettings` section already present in API appsettings files.
- Use `CancellationToken` on all new async public methods and pass it through repository and persistence calls.

### Mapping Pattern

To maintain consistency across entities (User, Resume, Education, CoverLetter, etc.), follow this mapping pattern:

1. **File location:** `<Entity>Mappers.cs` in the feature folder (e.g., `Users/Mappers/UserMappers.cs`, `Resumes/Mappers/ResumeMappers.cs`)
2. **Extension methods:** Add `ToEntity()` and `To<Entity>Result()` methods as static extension methods
3. **Field mapping:** Handle name conversions (e.g., `LoginName` -> `Login`, `About` -> `AboutMe`) and type conversions (e.g., string enum values -> Location enum)
4. **No external dependencies:** Use only System and core libraries; no AutoMapper or external mapping libraries
5. **Null handling:** Validate inputs and handle null/empty values gracefully (e.g., empty Location string -> null Location enum)

Example from UserMappers.cs:
```csharp
public static User ToEntity(this CreateUserCommand command) { ... }
public static CreateUserResult ToCreateUserResult(this User user) { ... }
```

### File Structure Requirements

- API composition and HTTP entrypoint changes:
  - `backend/src/JobNecto.API/Program.cs`
  - `backend/src/JobNecto.API/Controllers/UsersController.cs`
- Application slice for Story 1.2:
  - `backend/src/JobNecto.Application/...` under a `Users` feature slice for command, handler, validator, and DTOs
  - Prefer colocating create-user types instead of scattering them across unrelated folders
- Infrastructure changes:
  - `backend/src/JobNecto.Infrastructure/DI.cs`
  - `backend/src/JobNecto.Infrastructure/Repositories/UserRepository.cs`
  - `backend/src/JobNecto.Infrastructure/Persistance/Config/UserConfiguration.cs`
  - `backend/src/JobNecto.Infrastructure/Migrations/*`
  - a new JWT token service implementation under Infrastructure
- Test additions should stay under `backend/tests/JobNecto.Tests` with separate API and Application coverage.

### Testing Requirements

- Follow existing xUnit + FluentAssertions + WebApplicationFactory patterns from `AuthenticationTests` and `ExceptionHandlingTests`.
- Use unique in-memory database names per test when exercising EF-backed code.
- Cover both optimistic application behavior and the race-condition edge case from the PRD where a duplicate user may appear between validation and save; DB uniqueness should still guard correctness.
- Assert response payload shape, `Location` header, conflict status, and absence of password fields.
- Keep tests focused on Story 1.2 only; do not refactor unrelated authentication or exception tests.

### Project Structure Notes

- There are currently no API controllers in the repo and `Program.cs` does not yet register controllers or map controller endpoints. Story 1.2 is the first story that should introduce that surface.
- The current `User` entity stores `Location` as an enum-backed string in the database, while planning docs describe a more human-readable profile field. For this story, stay aligned with the existing enum model rather than inventing a new freeform location representation.
- The generic repository contract throws generic exceptions in some base methods; for new duplicate-user behavior, rely on explicit conflict handling rather than generic repository exceptions.

### Git Intelligence Summary

- Recent commits show Story 1.1 completed by extracting JWT auth configuration and updating story/project context artifacts.
- Story 1.2 should build on the established auth and exception infrastructure instead of modifying their behavior.

### References

- [Source: `_bmad-output/planning-artifacts/epics/epic-1-foundation-user-profile-management.md` - Story 1.2]
- [Source: `_bmad-output/planning-artifacts/architecture.md` - Project Context Analysis, Decision 1, Decision 2, Decision 4, Decision 5, Decision 7]
- [Source: `_bmad-output/planning-artifacts/prd.md` - Feature: Create User Profile (POST /api/v1/users)]
- [Source: `backend/src/JobNecto.API/Program.cs`]
- [Source: `backend/src/JobNecto.API/Infrastructure/AuthContext.cs`]
- [Source: `backend/src/JobNecto.Infrastructure/DI.cs`]
- [Source: `backend/src/JobNecto.Infrastructure/Persistance/Config/UserConfiguration.cs`]
- [Source: `backend/src/JobNecto.Infrastructure/Repositories/UserRepository.cs`]
- [Source: `backend/src/JobNecto.Infrastructure/Persistance/UnitOfWork.cs`]
- [Source: `backend/src/JobNecto.Domain/Entities/User.cs`]
- [Source: `backend/src/JobNecto.Domain/Enums/Location.cs`]
- [Source: `backend/tests/JobNecto.Tests/API/AuthenticationTests.cs`]
- [Source: `_bmad-output/implementation-artifacts/1-1-global-exception-handling.md`]

## Dev Agent Record

### Agent Model Used

GitHub Copilot (GPT-5.4)

### Debug Log References

- Recent commit context reviewed via `git log -5 --oneline`

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Story status set to `ready-for-dev`.
- Sprint tracking updated so Story 1.2 is the next prepared item.
 - Task 1 implemented: added Application command/result, FluentValidation validator, and validator unit tests; local test run passed.
- Task 3 implemented: added MediatR handler for user creation with uniqueness validation, UsersController with anonymous POST endpoint, JWT token generation and HTTP-Only cookie setting, Location header; full test suite passed (100/100).

### File List

- `_bmad-output/implementation-artifacts/1-2-create-user-account.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

- `backend/src/JobNecto.Application/Users/CreateUserCommandHandler.cs` — MediatR handler for user creation with uniqueness checks
- `backend/src/JobNecto.API/Controllers/UsersController.cs` — API controller with anonymous POST endpoint, JWT cookie setting
- `backend/src/JobNecto.API/Program.cs` — Updated to register controllers, MediatR, validators
- `backend/src/JobNecto.Application/Interfaces/IJwtTokenService.cs` — Updated to async GenerateTokenAsync(string userId)
- `backend/src/JobNecto.Infrastructure/Services/JwtTokenService.cs` — Updated to async implementation

