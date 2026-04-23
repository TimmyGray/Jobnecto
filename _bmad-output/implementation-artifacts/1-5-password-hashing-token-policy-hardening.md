# Story 1.5: Password Hashing & Token Policy Hardening

Status: review

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a platform owner,
I want password persistence and token transport behavior standardized,
so that all authenticated features in later epics build on a secure and explicit auth foundation.

## Acceptance Criteria

1. All newly persisted user passwords are stored only as one-way salted hashes and are never stored or returned in plaintext.
2. Legacy plaintext password rows (if any) are remediated through an approved migration/backfill strategy before Epic 2 delivery starts.
3. Browser authentication flows use JWT session cookies with HTTP-only and environment-appropriate secure settings, and this behavior is covered by integration tests.
4. Non-browser authentication flows are explicitly defined for `Authorization: Bearer` transport, including token renewal/refresh behavior, and documented consistently in PRD, architecture, and API contracts.
5. Any auth-related uniqueness race that triggers a database unique violation is mapped to `409 Conflict` through global exception handling with stable Problem Details shape.
6. Test coverage includes at least one concurrent-request integration scenario for auth-related uniqueness constraints.

## Tasks / Subtasks

- [x] Task 1: Introduce secure password hashing for auth flows (AC: 1)
  - [x] Add an application-level password hashing contract and infrastructure implementation using a one-way salted algorithm.
  - [x] Update user registration and credential-update flows to persist only hashed values.
  - [x] Ensure all outbound DTOs and API responses continue to exclude password-derived fields.

- [x] Task 2: Execute migration/backfill path for legacy password data (AC: 2)
  - [x] Define migration approach and document the greenfield assumption (no existing legacy users in this environment).
  - [x] Confirm runtime backfill is not required for current development stage; keep one-off migration/script as future option if legacy imports appear.
  - [x] Keep password storage hardened for all newly created users.

- [x] Task 3: Standardize token transport policy for browser and non-browser clients (AC: 3, 4)
  - [x] Keep browser session behavior on secure HTTP-only JWT cookies.
  - [x] Define and document non-browser `Authorization: Bearer` usage and token-renewal lifecycle.
  - [x] Align PRD, architecture, and OpenAPI security descriptions so transport policy is unambiguous.

- [x] Task 4: Ensure DB-level conflict mapping and race-condition reliability (AC: 5, 6)
  - [x] Verify auth-relevant unique constraints are enforced at the database layer.
  - [x] Ensure database unique-violation exceptions are mapped to `409 Conflict` through `GlobalExceptionHandler`.
  - [x] Add integration tests that reproduce concurrent duplicate submissions and assert deterministic `409` behavior.

- [x] Task 5: Run quality checks for story completion (AC: 1, 2, 3, 4, 5, 6)
  - [x] Run `dotnet build backend/JobNecto.slnx`.
  - [x] Run `dotnet test backend/JobNecto.slnx`.

## Dev Notes

### Previous Story Intelligence

- Story 1.2 implemented registration and intentionally deferred password hashing.
- Story 1.1 and Story 1.2 established JWT claim extraction and global exception handling patterns that this story must extend, not replace.
- Epic 1 retrospective identified hashing, explicit non-browser token policy, and race handling as release blockers before Epic 2.

### Technical Requirements

- Maintain Clean Architecture boundaries: API for transport, Application for orchestration/validation, Infrastructure for hashing and persistence details.
- Keep `UserId` claim extraction patterns compatible with existing `GetCurrentUserId()` behavior.
- Ensure cancellation tokens flow through all new async public methods.
- Do not introduce plaintext password logging in app logs, exceptions, telemetry, or test snapshots.

### Architecture Compliance

- Browser auth transport remains cookie-based in Phase B.
- Non-browser auth transport must use `Authorization: Bearer` with explicit renewal strategy.
- Any uniqueness business rule exposed as `409 Conflict` must be backed by DB constraints, not only pre-check logic.

### File Structure Requirements

- Update planning artifacts:
  - `_bmad-output/planning-artifacts/prd.md`
  - `_bmad-output/planning-artifacts/architecture.md`
  - `_bmad-output/planning-artifacts/epics/epic-1-foundation-user-profile-management.md`
- Expected implementation areas:
  - `backend/src/JobNecto.Application`
  - `backend/src/JobNecto.Infrastructure`
  - `backend/src/JobNecto.API`
  - `backend/tests/JobNecto.Tests`

### Testing Requirements

- Add unit tests for password hashing behavior and credential verification.
- Add integration tests for browser cookie auth behavior.
- Add integration tests for non-browser bearer flow and token-renewal policy contract.
- Add concurrent duplicate-request tests verifying DB uniqueness + `409` mapping.

### References

- [Source: `_bmad-output/implementation-artifacts/epic-1-retro-2026-04-22.md`]
- [Source: `_bmad-output/planning-artifacts/epics/epic-1-foundation-user-profile-management.md` - Story 1.5]
- [Source: `_bmad-output/planning-artifacts/prd.md`]
- [Source: `_bmad-output/planning-artifacts/architecture.md`]
- [Source: `_bmad-output/implementation-artifacts/1-2-create-user-account.md`]

## Dev Agent Record

### Agent Model Used

GitHub Copilot

### Completion Notes List

- Added `IPasswordHasher` contract, PBKDF2 implementation, and wired create-user persistence to store only hashed password values.
- For this greenfield environment (no existing users), removed runtime legacy-password backfill service to avoid unnecessary startup complexity.
- Increased `Users.Password` column length to 256 via EF Core migration `20260422212528_IncreaseUserPasswordHashLength`.
- Added explicit bearer renewal API contract via `POST /api/v1/users/token/refresh` and aligned policy docs in PRD/architecture/epic-1 plus OpenAPI security scheme descriptions.
- Added DB unique-violation mapping (`DbUpdateException`) to `409 Conflict` in `GlobalExceptionHandler` and added deterministic concurrent duplicate submission integration coverage.
- Post-review hardening pass: replaced simulated concurrency test with a PostgreSQL-backed schema-isolated path and tightened PBKDF2 parser bounds.
- Fixed mojibake artifacts in requirements/epics and updated stale password-hardening XML comment in `CreateUserCommand`.
- Validation executed: `dotnet build backend/JobNecto.slnx`; `dotnet test backend/JobNecto.slnx` (124 passed, 0 failed).

### File List

- `_bmad-output/implementation-artifacts/1-5-password-hashing-token-policy-hardening.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/planning-artifacts/prd.md`
- `_bmad-output/planning-artifacts/architecture.md`
- `_bmad-output/planning-artifacts/epics/epic-1-foundation-user-profile-management.md`
- `_bmad-output/planning-artifacts/epics/requirements-inventory.md`
- `_bmad-output/planning-artifacts/epics/epic-3-cover-letter-template-library.md`
- `_bmad-output/planning-artifacts/epics/epic-2-resume-education-management.md`
- `backend/src/JobNecto.Application/Interfaces/IPasswordHasher.cs`
- `backend/src/JobNecto.Application/Users/CreateUserCommandHandler.cs`
- `backend/src/JobNecto.Application/Users/Mappers/UserMappers.cs`
- `backend/src/JobNecto.Infrastructure/DI.cs`
- `backend/src/JobNecto.Infrastructure/Persistance/AppDbContext.cs`
- `backend/src/JobNecto.Infrastructure/Persistance/Config/UserConfiguration.cs`
- `backend/src/JobNecto.Infrastructure/Services/Pbkdf2PasswordHasher.cs`
- `backend/src/JobNecto.Infrastructure/Migrations/20260422212528_IncreaseUserPasswordHashLength.cs`
- `backend/src/JobNecto.Infrastructure/Migrations/20260422212528_IncreaseUserPasswordHashLength.Designer.cs`
- `backend/src/JobNecto.Infrastructure/Migrations/AppDbContextModelSnapshot.cs`
- `backend/src/JobNecto.API/Program.cs`
- `backend/src/JobNecto.API/Infrastructure/OpenApiCollectionExtensions.cs`
- `backend/src/JobNecto.API/Infrastructure/ExceptionHandling/GlobalExceptionHandler.cs`
- `backend/src/JobNecto.API/Controllers/UsersController.cs`
- `backend/src/JobNecto.API/Contracts/Auth/RefreshAccessTokenResult.cs`
- `backend/tests/JobNecto.Tests/Application/Users/CreateUserCommandHandlerTests.cs`
- `backend/tests/JobNecto.Tests/API/UsersControllerTests.cs`
- `backend/tests/JobNecto.Tests/API/UsersControllerConcurrencyTests.cs`
- `backend/tests/JobNecto.Tests/API/ExceptionHandlingTests.cs`
- `backend/tests/JobNecto.Tests/Infrastructure/Security/Pbkdf2PasswordHasherTests.cs`
- `backend/tests/JobNecto.Tests/Infrastructure/UserRepositorytests.cs`
- `_bmad-output/agent-learnings.md`

### Change Log

- 2026-04-23: Completed Story 1.5 implementation across Application, Infrastructure, API, tests, and planning artifacts.
- 2026-04-23: Completed post-review fixes for real DB concurrency coverage, backfill safety, parser strictness, and artifact text cleanup.
- 2026-04-23: Removed runtime legacy-password backfill service/tests after confirming greenfield environment has no existing users.

### Review Findings

- [x] [Review]\[Patch\] Concurrency integration test is provider-simulated, not a real DB uniqueness race [backend/tests/JobNecto.Tests/API/UsersControllerConcurrencyTests.cs:92]
- [x] [Review]\[Patch\] Backfill implementation removed for greenfield environment (no existing users), so runtime rehash behavior is no longer applicable.
- [x] [Review]\[Patch\] Password hash format validation is too permissive and accepts undersized weak payloads [backend/src/JobNecto.Infrastructure/Services/Pbkdf2PasswordHasher.cs:95]
- [x] [Review]\[Patch\] Story-related planning artifacts contain mojibake text in requirement ranges/symbols [\_bmad-output/planning-artifacts/epics/requirements-inventory.md:18]
- [x] [Review]\[Patch\] `CreateUserCommand` comment contradicts implemented password-hardening scope [backend/src/JobNecto.Application/Users/CreateUserCommand.cs:22]
