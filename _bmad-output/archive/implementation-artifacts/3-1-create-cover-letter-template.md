# Story 3.1: Create Cover Letter Template

Status: done

## Story

As a job seeker,
I want to create a named cover letter template with reusable content,
so that I can quickly apply it to future job applications.

## Acceptance Criteria

1. `POST /api/v1/cover-letter-templates` requires a valid JWT token. Unauthenticated requests return `401 Unauthorized`.
2. Given a valid JWT token and `POST /api/v1/cover-letter-templates` with `name` and `content` (50-10,000 chars), returns `201 Created` with the full template object and `Location` header set to `/api/v1/cover-letter-templates/{id}`.
3. Given `content` fewer than 50 characters, returns `400 Bad Request` with a field-level error on `content`.
4. Given `content` more than 10,000 characters, returns `400 Bad Request` with a field-level error on `content`.
5. Given `name` already used by this user (across non-deleted templates), returns `409 Conflict` from database-backed per-user uniqueness enforcement.
6. Given the same `name` used by another user, returns `201 Created` (uniqueness is per-user, not global).

## Tasks / Subtasks

- [x] Task 1: Expose CoverLetterTemplateRepository in UnitOfWork (prerequisite for all handlers)
  - [x] Add `IMutableRepository<CoverLetterTemplate> CoverLetterTemplateRepository { get; }` to `backend/src/JobNecto.Application/Interfaces/IUnitOfWork.cs`
  - [x] Add backing field `IMutableRepository<CoverLetterTemplate>? _coverLetterTemplateRepository` and lazy property to `backend/src/JobNecto.Infrastructure/Persistance/UnitOfWork.cs`

- [x] Task 2: Add per-user name uniqueness DB constraint (AC: 5, 6)
  - [x] Add filtered unique index to `CoverLetterTemplateConfiguration`:
    ```csharp
    builder.HasIndex(t => new { t.UserId, t.Name })
        .IsUnique()
        .HasFilter("\"IsDeleted\" = false");
    ```
  - [x] Generate EF migration: `dotnet ef migrations add AddCoverLetterTemplateUniqueNamePerUser --project backend/src/JobNecto.Infrastructure --startup-project backend/src/JobNecto.API`

- [x] Task 3: Define application create contract (AC: 2, 3, 4)
  - [x] Create `backend/src/JobNecto.Application/CoverLetterTemplates/CreateCoverLetterTemplateCommand.cs` with command (`UserId` `[JsonIgnore]`, `Name`, `Content`) and `CoverLetterTemplateResult` DTO (`Id`, `UserId`, `Name`, `Content`, `CreatedAt`, `UpdatedAt`)
  - [x] Create `backend/src/JobNecto.Application/CoverLetterTemplates/Mappers/CoverLetterTemplateMappers.cs` with `ToEntity(this CreateCoverLetterTemplateCommand)` and `ToCoverLetterTemplateResult(this CoverLetterTemplate)` extension methods
  - [x] Create `backend/src/JobNecto.Application/CoverLetterTemplates/Validators/CreateCoverLetterTemplateCommandValidator.cs` with rules:
    - `Name`: `NotEmpty()` + `MaximumLength(100)` (rejects null, empty, whitespace)
    - `Content`: `NotEmpty()` + `MinimumLength(50)` + `MaximumLength(10000)`
    - `UserId`: `NotEmpty()`

- [x] Task 4: Implement Application handler (AC: 2, 5)
  - [x] Create `backend/src/JobNecto.Application/CoverLetterTemplates/CreateCoverLetterTemplateCommandHandler.cs`
  - [x] Map command to entity, set `CreatedAt = UpdatedAt = DateTime.UtcNow` explicitly in handler
  - [x] Persist via `_unitOfWork.CoverLetterTemplateRepository.CreateAsync(entity, ct)` then `SaveChangesAsync(ct)`
  - [x] Return mapped `CoverLetterTemplateResult`

- [x] Task 5: Expose authenticated HTTP endpoint (AC: 1, 2, 3, 4, 5)
  - [x] Create `backend/src/JobNecto.API/Controllers/CoverLetterTemplatesController.cs` with `[ApiController]`, `[Route("api/v1/cover-letter-templates")]`, `[Authorize]`
  - [x] Add `POST` action accepting `CreateCoverLetterTemplateCommand` (POST only — list/detail in later stories)
  - [x] Extract `UserId` via `HttpContext.GetCurrentUserId()` and return `Unauthorized()` if parse fails
  - [x] Return `Created($"/api/v1/cover-letter-templates/{result.Id}", result)` on success
  - [x] Add `[ProducesResponseType]` for 201, 400, 401, 409

- [x] Task 6: Add comprehensive tests and verification gates (AC: 1-6)
  - [x] Create `backend/tests/JobNecto.Tests/Application/CoverLetterTemplates/CreateCoverLetterTemplateCommandValidatorTests.cs`:
    - Valid payload passes
    - Null/empty `name` fails with error on `Name`
    - Whitespace-only `name` fails with error on `Name`
    - `content` < 50 chars fails with error on `Content`
    - `content` > 10,000 chars fails with error on `Content`
    - Empty `content` fails with error on `Content`
  - [x] Create `backend/tests/JobNecto.Tests/Application/CoverLetterTemplates/CreateCoverLetterTemplateCommandHandlerTests.cs`:
    - Valid create persists exactly once and returns mapped result with non-default timestamps
  - [x] Create `backend/tests/JobNecto.Tests/API/CoverLetterTemplates/CoverLetterTemplatesApiTests.cs`:
    - No token → `401`
    - Valid request → `201` with correct `Location` header and response payload
    - `content` < 50 chars → `400` with field-level error on `content`
    - `content` > 10,000 chars → `400` with field-level error on `content`
  - [x] Create `backend/tests/JobNecto.Tests/API/CoverLetterTemplates/CoverLetterTemplatesUniquenessApiTests.cs` (Postgres-backed factory, optional skip pattern matching UsersControllerConcurrencyTests):
    - Duplicate `name` (same user) → `409`
    - Same `name`, different users → both return `201`
    - **Concurrent duplicate `name`** (same user, parallel `Task.WhenAll`) → at least one returns `409`
  - [x] Run targeted tests: `dotnet test backend/JobNecto.slnx --filter "FullyQualifiedName~CoverLetterTemplates"`
  - [x] Run full suite: `dotnet test backend/JobNecto.slnx`
  - [x] Run CI parity: `dotnet build backend/JobNecto.slnx --configuration Release --warnaserror` then `dotnet test backend/JobNecto.slnx --configuration Release --no-build --warnaserror`

## Dev Notes

### Entity State — What Already Exists

`CoverLetterTemplate` entity at `backend/src/JobNecto.Domain/Entities/CoverLetterTemplate.cs`:
```csharp
public class CoverLetterTemplate : SoftDeletableEntity
{
    public Guid UserId;          // PUBLIC FIELD, not auto-property
    public required string Name; // PUBLIC FIELD, not auto-property
    public required string Content; // PUBLIC FIELD, not auto-property
}
```
- **CRITICAL:** `UserId`, `Name`, and `Content` are public **fields**, not properties. Set them with object initializer syntax. EF Core maps fields correctly via `CoverLetterTemplateConfiguration`.
- `Id`, `CreatedAt`, `UpdatedAt`, `IsDeleted`, `DeletedAt` are on base classes and are properties.

`CoverLetterTemplateConfiguration` at `backend/src/JobNecto.Infrastructure/Persistance/Config/CoverLetterTemplateConfiguration.cs`:
- Table `CoverLetterTemplates` configured, cascade delete from User, soft-delete fields configured.
- **Missing:** the unique filtered index on `(UserId, Name)` — this story must add it.

`CoverLetterTemplateRepository` at `backend/src/JobNecto.Infrastructure/Repositories/CoverLetterTemplateRepository.cs`:
- Inherits `SoftDeletableRepository<CoverLetterTemplate>` which implements `IMutableRepository<CoverLetterTemplate>`.
- **Not yet exposed in `IUnitOfWork`** — this story adds it.

`AppDbContext`:
- `DbSet<CoverLetterTemplate> CoverLetterTemplates` is already registered.
- Global soft-delete query filter for `CoverLetterTemplate` is already configured.

No application-layer files exist for `CoverLetterTemplates` yet. All Application + API files are new in this story.

### IUnitOfWork Update

Add to interface after the `EducationRepository` property:
```csharp
/// <summary>
/// Repository for cover letter templates with full write support (update + soft delete).
/// </summary>
IMutableRepository<CoverLetterTemplate> CoverLetterTemplateRepository { get; }
```

Add to `UnitOfWork.cs`:
```csharp
private IMutableRepository<CoverLetterTemplate>? _coverLetterTemplateRepository;

public IMutableRepository<CoverLetterTemplate> CoverLetterTemplateRepository =>
    _coverLetterTemplateRepository ??= new CoverLetterTemplateRepository(_context);
```

### Unique Index in EF Core (filtered, per-user, active records only)

Add to `CoverLetterTemplateConfiguration.Configure(...)`:
```csharp
builder.HasIndex(t => new { t.UserId, t.Name })
    .IsUnique()
    .HasFilter("\"IsDeleted\" = false");
```

The filter ensures soft-deleted templates do not block re-use of their names. The snapshot currently only has `b.HasIndex("UserId")` — the migration adds the filtered unique composite index.

### 409 Conflict — No Pre-check Required

Do **not** add a pre-check query before `CreateAsync`. The DB unique constraint is the authoritative guard:

- `GlobalExceptionHandler.IsUniqueConstraintViolation()` detects `PostgresException.SqlState == UniqueViolation` (Npgsql `PostgresErrorCodes.UniqueViolation`)
- Falls back to string matching `"duplicate key"` / `"unique constraint"` for non-Postgres providers (used in integration tests via `InMemory` + `UNIQUE constraint failed`)
- Response: `{ "title": "Conflict", "status": 409, "detail": "A unique constraint was violated." }`

This approach handles concurrent race conditions without an explicit pre-check. It satisfies NFR13 and the Epic 3 readiness constraint.

### Handler Pattern

Follow `CreateEducationCommandHandler` exactly:
```csharp
public async Task<CoverLetterTemplateResult> Handle(
    CreateCoverLetterTemplateCommand request, CancellationToken cancellationToken)
{
    var template = request.ToEntity();
    var now = DateTime.UtcNow;
    template.CreatedAt = now;
    template.UpdatedAt = now;

    await _unitOfWork.CoverLetterTemplateRepository.CreateAsync(template, cancellationToken);
    await _unitOfWork.SaveChangesAsync(cancellationToken);

    return template.ToCoverLetterTemplateResult();
}
```

**Why set timestamps in handler:** DB defaults (`Now()`) are not applied by EF InMemory provider. Tests that assert non-default timestamps fail if handler does not set them. (Lesson 2026-05-05)

### Controller Pattern

Follow `EducationsController.Create` exactly:
```csharp
[HttpPost]
[ProducesResponseType(typeof(CoverLetterTemplateResult), StatusCodes.Status201Created)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status409Conflict)]
public async Task<ActionResult<CoverLetterTemplateResult>> Create(
    CreateCoverLetterTemplateCommand command,
    CancellationToken cancellationToken)
{
    var userIdValue = HttpContext.GetCurrentUserId();
    if (string.IsNullOrWhiteSpace(userIdValue) || !Guid.TryParse(userIdValue, out var userId))
        return Unauthorized();

    command.UserId = userId;

    var result = await _mediator.Send(command, cancellationToken);
    return Created($"/api/v1/cover-letter-templates/{result.Id}", result);
}
```

`GetCurrentUserId()` is in `backend/src/JobNecto.API/Infrastructure/AuthContext.cs`. It checks `ClaimTypes.NameIdentifier`, `"sub"`, and `"userId"` claims in order.

### Validator Rules

```csharp
public class CreateCoverLetterTemplateCommandValidator
    : AbstractValidator<CreateCoverLetterTemplateCommand>
{
    public CreateCoverLetterTemplateCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Content).NotEmpty().MinimumLength(50).MaximumLength(10000);
    }
}
```

**Name validation decisions (Epic 2 validator audit lesson):**
- Null, empty string, and whitespace-only all rejected via `NotEmpty()`
- No minimum length specified in AC; `NotEmpty()` alone is sufficient
- Max length 100 follows the pattern used by `Education.Title` and `Education.Specialization`

### Test Patterns

Copy auth cookie setup from `EducationsApiTests` verbatim:
```csharp
private static async Task<string> CreateUserAndGetCookieAsync(HttpClient client, ...)
{
    var response = await client.PostAsJsonAsync("/api/v1/users", command);
    var authCookie = response.Headers.GetValues("Set-Cookie")
        .Select(x => x.Split(';', ...).FirstOrDefault(y => y.StartsWith("auth-token=", ...)))
        .First(x => !string.IsNullOrWhiteSpace(x));
    return authCookie!;
}
```

Forward cookie via:
```csharp
var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/cover-letter-templates")
{
    Content = JsonContent.Create(payload)
};
request.Headers.TryAddWithoutValidation("Cookie", authCookie);
```

**Test data constraints (lesson 2026-04-25):**
- `name`: any string ≤ 100 chars, `NotEmpty()` satisfied
- `content`: must be 50-10,000 chars for valid payloads; use exactly 49 chars for the under-limit test and exactly 10,001 chars for the over-limit test
- `LoginName` in user setup: alphanumeric + underscore only (`^[a-zA-Z0-9_]+$`), max 20 chars total (prefix + 8-char GUID suffix = 12+8 = 20 — keep prefix ≤ 12 chars)

**Concurrent 409 test pattern:**
```csharp
[Fact]
public async Task Create_ConcurrentDuplicateName_AtLeastOneReturns409()
{
    await using var factory = new JobNectoApiFactory();
    var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
    var authCookie = await CreateUserAndGetCookieAsync(client);

    var task1 = PostTemplateAsync(client, authCookie, "Same Name", ValidContent());
    var task2 = PostTemplateAsync(client, authCookie, "Same Name", ValidContent());
    var results = await Task.WhenAll(task1, task2);

    results.Should().Contain(r => r.StatusCode == HttpStatusCode.Conflict);
}
```

### File Structure Requirements

| File | Action |
|------|--------|
| `backend/src/JobNecto.Application/Interfaces/IUnitOfWork.cs` | UPDATE — add `CoverLetterTemplateRepository` |
| `backend/src/JobNecto.Infrastructure/Persistance/UnitOfWork.cs` | UPDATE — add backing field + property |
| `backend/src/JobNecto.Infrastructure/Persistance/Config/CoverLetterTemplateConfiguration.cs` | UPDATE — add unique filtered index |
| `backend/src/JobNecto.Infrastructure/Migrations/<timestamp>_AddCoverLetterTemplateUniqueNamePerUser.cs` | NEW — EF migration |
| `backend/src/JobNecto.Infrastructure/Migrations/AppDbContextModelSnapshot.cs` | UPDATE — by EF migration tooling |
| `backend/src/JobNecto.Application/CoverLetterTemplates/CreateCoverLetterTemplateCommand.cs` | NEW |
| `backend/src/JobNecto.Application/CoverLetterTemplates/CreateCoverLetterTemplateCommandHandler.cs` | NEW |
| `backend/src/JobNecto.Application/CoverLetterTemplates/Mappers/CoverLetterTemplateMappers.cs` | NEW |
| `backend/src/JobNecto.Application/CoverLetterTemplates/Validators/CreateCoverLetterTemplateCommandValidator.cs` | NEW |
| `backend/src/JobNecto.API/Controllers/CoverLetterTemplatesController.cs` | NEW |
| `backend/tests/JobNecto.Tests/Application/CoverLetterTemplates/CreateCoverLetterTemplateCommandValidatorTests.cs` | NEW |
| `backend/tests/JobNecto.Tests/Application/CoverLetterTemplates/CreateCoverLetterTemplateCommandHandlerTests.cs` | NEW |
| `backend/tests/JobNecto.Tests/API/CoverLetterTemplates/CoverLetterTemplatesApiTests.cs` | NEW |
| `_bmad-output/archive/implementation-artifacts/3-1-create-cover-letter-template.md` | THIS FILE |
| `_bmad-output/implementation-artifacts/sprint-status.yaml` | UPDATE — status to ready-for-dev |

Do **not** modify Resume, Education, or any other existing entity handlers or repositories.

### Namespace Conventions

Namespaces must match folder structure:
- `JobNecto.Application.CoverLetterTemplates`
- `JobNecto.Application.CoverLetterTemplates.Mappers`
- `JobNecto.Application.CoverLetterTemplates.Validators`
- `JobNecto.API.Controllers`
- `JobNecto.Tests.API.CoverLetterTemplates`
- `JobNecto.Tests.Application.CoverLetterTemplates`

### Previous Story Intelligence

- **Set timestamps in handler** (`CreatedAt = UpdatedAt = DateTime.UtcNow`) — EF InMemory does not execute SQL defaults; tests that assert non-default timestamps will fail otherwise (lesson 2026-05-05)
- **Separate handler file** — `CreateCoverLetterTemplateCommand.cs` and `CreateCoverLetterTemplateCommandHandler.cs` are separate files (lesson 2026-04-28)
- **Test data must be validator-compliant** — `content` must be 50-10,000 chars for setup data; `LoginName` must match `^[a-zA-Z0-9_]+$` with length ≤ 20 (lesson 2026-04-25)
- **Do not null-check after `GetByIdAsync`** — repository throws `NotFoundException` on miss (relevant to later stories 3.3, 3.4, 3.5)
- **Concurrent tests required by NFR13** — any uniqueness rule surfaced as 409 must have at least one concurrent create/update integration test

### References

- [Epic 3 source](_bmad-output/archive/planning-artifacts/epics/epic-3-cover-letter-template-library.md) — Story 3.1 ACs and readiness constraints
- [Core architectural decisions](_bmad-output/planning-artifacts/architecture/core-architectural-decisions.md) — MediatR, validation, repository patterns
- [Epic 2 architecture revision](_bmad-output/planning-artifacts/architecture/epic-2-architecture-revision-2026-05-05.md) — Epic 3 guardrails and accepted patterns
- [Pattern: EducationsController](backend/src/JobNecto.API/Controllers/EducationsController.cs)
- [Pattern: CreateEducationCommandHandler](backend/src/JobNecto.Application/Educations/CreateEducationCommandHandler.cs)
- [Pattern: EducationsApiTests](backend/tests/JobNecto.Tests/API/Educations/EducationsApiTests.cs)
- [Pattern: GlobalExceptionHandler](backend/src/JobNecto.API/Infrastructure/ExceptionHandling/GlobalExceptionHandler.cs) — 409 via DB constraint

## Dev Agent Record

### Implementation Plan

Followed the task sequence exactly as written.

- Task 1: Added `IMutableRepository<CoverLetterTemplate> CoverLetterTemplateRepository` to `IUnitOfWork` and `UnitOfWork` lazy backing field — mirrors Education pattern.
- Task 2: Added `HasIndex(t => new { t.UserId, t.Name }).IsUnique().HasFilter("\"IsDeleted\" = false")` to `CoverLetterTemplateConfiguration` and generated migration `AddCoverLetterTemplateUniqueNamePerUser`.
- Task 3: Created `CreateCoverLetterTemplateCommand` + `CoverLetterTemplateResult`, `CoverLetterTemplateMappers` (`ToEntity`, `ToCoverLetterTemplateResult`), and `CreateCoverLetterTemplateCommandValidator` — all follow Education pattern exactly.
- Task 4: Created `CreateCoverLetterTemplateCommandHandler` — sets `CreatedAt = UpdatedAt = DateTime.UtcNow` explicitly (per lesson 2026-05-05), persists via `CoverLetterTemplateRepository.CreateAsync` + `SaveChangesAsync`.
- Task 5: Created `CoverLetterTemplatesController` with `[Authorize]`, POST-only, returns `Created(...)` with Location header — mirrors `EducationsController.Create`.
- Task 6: Created 3 test files totaling 16 new tests. Uniqueness/409 tests use a Postgres-backed factory (`CoverLetterTemplatesPostgresFactory`) with graceful skip when Postgres is unavailable — same pattern as `UsersControllerConcurrencyTests`. EF Core InMemory does not enforce unique constraints, hence the split.

### Completion Notes

All 6 tasks complete. 308/308 tests pass. CI parity: 0 warnings, 0 errors.

AC coverage:
- AC1: `[Authorize]` on controller + 401 integration test ✅
- AC2: `POST /api/v1/cover-letter-templates` returns 201 + Location + full payload ✅
- AC3: content < 50 chars → 400 (validator + integration test) ✅
- AC4: content > 10,000 chars → 400 (validator + integration test) ✅
- AC5: duplicate name same user → 409 via DB unique constraint (Postgres factory test, skips without Postgres) ✅
- AC6: same name different users → 201 both (Postgres factory test, skips without Postgres) ✅

### Debug Log

No blockers encountered. EF Core InMemory uniqueness limitation resolved by introducing `CoverLetterTemplatesPostgresFactory` (matching existing project pattern from `UsersControllerConcurrencyTests`).

## File List

- `backend/src/JobNecto.Application/Interfaces/IUnitOfWork.cs` — updated: added `CoverLetterTemplateRepository`
- `backend/src/JobNecto.Infrastructure/Persistance/UnitOfWork.cs` — updated: added backing field + lazy property
- `backend/src/JobNecto.Infrastructure/Persistance/Config/CoverLetterTemplateConfiguration.cs` — updated: added filtered unique index
- `backend/src/JobNecto.Infrastructure/Migrations/<timestamp>_AddCoverLetterTemplateUniqueNamePerUser.cs` — new: EF migration
- `backend/src/JobNecto.Infrastructure/Migrations/AppDbContextModelSnapshot.cs` — updated: by EF migration tooling
- `backend/src/JobNecto.Application/CoverLetterTemplates/CreateCoverLetterTemplateCommand.cs` — new
- `backend/src/JobNecto.Application/CoverLetterTemplates/CreateCoverLetterTemplateCommandHandler.cs` — new
- `backend/src/JobNecto.Application/CoverLetterTemplates/Mappers/CoverLetterTemplateMappers.cs` — new
- `backend/src/JobNecto.Application/CoverLetterTemplates/Validators/CreateCoverLetterTemplateCommandValidator.cs` — new
- `backend/src/JobNecto.API/Controllers/CoverLetterTemplatesController.cs` — new
- `backend/tests/JobNecto.Tests/Application/CoverLetterTemplates/CreateCoverLetterTemplateCommandValidatorTests.cs` — new
- `backend/tests/JobNecto.Tests/Application/CoverLetterTemplates/CreateCoverLetterTemplateCommandHandlerTests.cs` — new
- `backend/tests/JobNecto.Tests/API/CoverLetterTemplates/CoverLetterTemplatesApiTests.cs` — new (InMemory: auth + validation tests)
- `backend/tests/JobNecto.Tests/API/CoverLetterTemplates/CoverLetterTemplatesUniquenessApiTests.cs` — new (Postgres: uniqueness + concurrency tests)
- `_bmad-output/archive/implementation-artifacts/3-1-create-cover-letter-template.md` — this file
- `_bmad-output/implementation-artifacts/sprint-status.yaml` — updated: status to review

## Change Log

- 2026-05-07: Implemented story 3.1 — POST /api/v1/cover-letter-templates endpoint with JWT auth, FluentValidation (content 50–10,000 chars, name unique per user), DB-backed 409 Conflict via filtered unique index, 16 tests added (308 total, 0 failures).

## Story Completion Status

- Ultimate context engine analysis completed - comprehensive developer guide created.

## Review Findings

- [x] [Review][Decision] Case-sensitive name uniqueness — dismissed: case-sensitive uniqueness is intentional per owner decision 2026-05-07.
- [x] [Review][Patch] Missing DB-level HasMaxLength(100) on Name — fixed: added `.HasMaxLength(100)` to `CoverLetterTemplateConfiguration.cs` + generated migration `AddCoverLetterTemplateNameMaxLength`. [`CoverLetterTemplateConfiguration.cs`]
- [x] [Review][Patch] Missing test for Name exceeding 100 characters — fixed: added `Validate_NameMoreThan100Chars_Fails`. [`CreateCoverLetterTemplateCommandValidatorTests.cs`]
- [x] [Review][Patch] Concurrent duplicate test assertion too weak — fixed: asserts exactly 1×201 and 1×409. [`CoverLetterTemplatesUniquenessApiTests.cs`]
- [x] [Review][Patch] Location header test does not validate GUID suffix — fixed: parses and validates GUID segment. [`CoverLetterTemplatesApiTests.cs:92`]
- [x] [Review][Patch] Handler test missing CreatedAt == UpdatedAt equality assertion — fixed: added `result.CreatedAt.Should().Be(result.UpdatedAt)`. [`CreateCoverLetterTemplateCommandHandlerTests.cs`]
- [x] [Review][Defer] Unit handler test missing result.Id != Guid.Empty assertion [`CreateCoverLetterTemplateCommandHandlerTests.cs`] — deferred, integration test covers ID generation via NotBeEmpty assertion
- [x] [Review][Defer] C# clock vs DB clock for timestamps [`CreateCoverLetterTemplateCommandHandler.cs:28-30`] — deferred, by design: documented EF InMemory lesson (2026-05-05); app-layer assignment required for in-memory test compatibility
- [x] [Review][Defer] TryInitializeSchemaAsync not thread-safe [`CoverLetterTemplatesUniquenessApiTests.cs:128`] — deferred, latent: only one fixture class currently uses this factory
- [x] [Review][Defer] ConfigureWebHost/TryInitializeSchemaAsync ordering fragility [`CoverLetterTemplatesUniquenessApiTests.cs:162`] — deferred, WebApplicationFactory lazy-builds host on first CreateClient(); current test pattern is safe
- [x] [Review][Defer] Exception swallowing in Postgres factory [`CoverLetterTemplatesUniquenessApiTests.cs:154-158`] — deferred, intentional skip behaviour for CI environments without Postgres
- [x] [Review][Defer] DI bypass for repository in UnitOfWork [`UnitOfWork.cs:41`] — deferred, pre-existing pattern throughout all repositories
- [x] [Review][Defer] Hard-coded Location URI string in controller [`CoverLetterTemplatesController.cs:50`] — deferred, pre-existing established pattern per EducationsController
- [x] [Review][Defer] GetCurrentUserId() returns 401 vs 403 for malformed claim [`CoverLetterTemplatesController.cs:44`] — deferred, matches existing pattern in EducationsController

## Additional Code Review (2026-05-08)

- [x] [Review][Patch] Thread-safety in `TryInitializeSchemaAsync` — fixed: added `SemaphoreSlim _initLock` with double-check pattern in `TryInitializeSchemaAsync`. [`CoverLetterTemplatesUniquenessApiTests.cs:128-145`]
- [x] [Review][Patch] Missing test for content at exactly 10,000 characters — fixed: added `Validate_ContentExactly10000Chars_Passes()` test. [`CreateCoverLetterTemplateCommandValidatorTests.cs`]
- [x] [Review][Patch] Exception swallowing in `CoverLetterTemplatesPostgresFactory` — fixed: log exception message to `_output` before returning false. [`CoverLetterTemplatesUniquenessApiTests.cs:154-160`]
- [x] [Review][Defer] Connection string ordering fragility — `_scopedConnectionString` could be uninitialized if `CreateClient` called before `TryInitializeSchemaAsync`. Current test pattern is safe. [`CoverLetterTemplatesUniquenessApiTests.cs:162-169`]

