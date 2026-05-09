# Core Architectural Decisions

### Decision 1: MediatR Request/Handler Structure

**Decision:** All CRUD operations and queries routed through MediatR commands and queries.

**Pattern:**

```
Request (Command/Query)
  ↓
MediatR Pipeline (Validation behavior)
  ↓
Handler (Business Logic)
  ↓
Repository (Data Access)
  ↓
DbContext (EF Core)
```

**Implementation Rules:**

- **Commands** for write operations: `CreateUserCommand`, `UpdateResumeCommand`, `DeleteEducationCommand`, etc.
- **Queries** for read operations: `GetUserQuery`, `ListResumesQuery`, `GetVacancyFilterQuery`, etc.
- Commands return: `Unit` (void) for updates, or the created entity (e.g., `CreatedUserDto`)
- Queries return: Single entity or paged list `PagedResult<T>`
- All handlers are synchronous logic; async I/O happens via repository and DbContext (which are awaited by handler)

**Request Class Naming:**

```csharp
// Command: verb + noun + "Command"
public class CreateUserCommand : IRequest<CreateUserResponse> { }
public class UpdateResumeCommand : IRequest<Unit> { }
public class DeleteEducationCommand : IRequest<Unit> { }

// Query: verb + noun + "Query"
public class GetUserByIdQuery : IRequest<UserDto> { }
public class ListResumesQuery : IRequest<PagedResult<ResumeDto>> { }
public class FilterVacanciesQuery : IRequest<PagedResult<VacancyDto>> { }
```

**Handler Injection Pattern:**

```csharp
public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, CreateUserResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateUserCommand> _validator;
    
    public CreateUserCommandHandler(IUnitOfWork unitOfWork, IValidator<CreateUserCommand> validator)
    {
        _unitOfWork = unitOfWork;
        _validator = validator;
    }
    
    public async Task<CreateUserResponse> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        // Handler receives already-validated request from pipeline
        // Handler focuses on business logic, not validation
        var user = new User { /* ... */ };
        await _unitOfWork.UserRepository.CreateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new CreateUserResponse { Id = user.Id };
    }
}
```

---

### Decision 2: Validation Pipeline

**Decision:** Two-layer validation: **FluentValidation (field-level) -> Handler (business rules)**

**Epic 2 implementation note:** Validator behavior is correct enough for shipped Resume/Education endpoints, but deferred work identified repeated edge cases. For new stories, validators must explicitly decide null, empty-string, and whitespace semantics; cross-field rules should use stable client-facing error keys when the API contract requires structured field errors.

**Layer 1: FluentValidation (pre-handler)**

- Fires before handler executes (MediatR behavior in pipeline)
- Validates field format, constraints, business rules that don't require DB access
- Example: email format, phone E.164 format, string length, enum values

```csharp
public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.LoginName)
            .NotEmpty()
            .Length(3, 20)
            .Matches(@"^[a-zA-Z0-9_]+$");
            
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();
            
        RuleFor(x => x.Phone)
            .Optional()
            .Matches(@"^\+\d{10,15}$", "E.164 format");
    }
}
```

**Layer 2: Handler-level validation (business rules)**

- DB-dependent validations inside handler
- Example: uniqueness checks (email already exists), ownership verification, cascade validations

```csharp
public async Task<CreateUserResponse> Handle(CreateUserCommand request, CancellationToken ct)
{
    // Check uniqueness (requires DB query)
    var emailExists = await _unitOfWork.UserRepository.ExistsByEmailAsync(request.Email, ct);
    if (emailExists)
        throw new DuplicateEmailException(request.Email);
    
    // Create user...
    await _unitOfWork.SaveChangesAsync(ct);
}
```

**Error Response Format (from validation failures):**

**Uniqueness rule:** Any uniqueness rule exposed to clients as `409 Conflict` must be backed by a database unique constraint. Handler pre-checks and validators may improve error messages, but they are not sufficient for race-prone create/update paths.

```json
{
  "type": "https://api.jobnecto.dev/errors/validation",
  "title": "Validation Failed",
  "status": 400,
  "detail": "One or more validation errors occurred.",
  "errors": {
    "email": ["Email must be valid format"],
    "phone": ["Phone must be E.164 format (e.g., +1234567890)"]
  }
}
```

---

### Decision 3: Repository Pattern

**Decision:** Repository interfaces are defined in the Application layer; implementations live in Infrastructure. The current Epic 2 baseline uses generic repository abstractions for common CRUD/list behavior, with specialized repository interfaces only where a resource needs distinct query behavior.

**Epic 2 implementation note:** Earlier architecture text described one bespoke repository interface per aggregate. The implemented code now uses `IRepository<T>` and `IEditableRepository<T>` for Resume, Education, and similar editable resources. `IUserRepository` and `IVacancyRepository` remain specialized because they need user lookup and vacancy filtering behavior.

**Current Application Layer Interfaces:**

```csharp
public interface IRepository<T>
    where T : BaseEntity
{
    Task<T> GetByIdAsync(Guid id, CancellationToken ct);
    Task<PagedResult<T>> GetAsync(PagedQuery pagedQuery, CancellationToken ct);
    Task<T> CreateAsync(T entity, CancellationToken ct);
    Task<Guid> DeleteAsync(Guid id, CancellationToken ct);
    Task<bool> IsExistsAsync(Guid id, CancellationToken ct);
}

public interface IEditableRepository<T> : IRepository<T>
    where T : BaseEntity
{
    Task<T> UpdateAsync(T entity, CancellationToken ct);
}

// Introduced by GitHub issue #65 (story R.1):
// Soft delete is expressed as a first-class contract independent of update semantics.
public interface ISoftDeleteRepository<T> : IRepository<T>
    where T : SoftDeletableEntity
{
    Task SoftDeleteAsync(T entity, CancellationToken ct);
}

// Role-based composition for entities that support all write operations (update + soft delete).
// Named by role ("mutable"), not by capability enumeration, so future write-side additions
// do not require renaming this interface.
// IEditableRepository<T> alone does NOT imply soft delete capability.
public interface IMutableRepository<T> : IEditableRepository<T>, ISoftDeleteRepository<T>
    where T : SoftDeletableEntity
{
}
```

**Interface hierarchy:**

```text
IRepository<T>
├── IEditableRepository<T>             (+ UpdateAsync)   [T : BaseEntity]
└── ISoftDeleteRepository<T>           (+ SoftDeleteAsync) [T : SoftDeletableEntity]
    └── IMutableRepository<T>  (composes both)  [T : SoftDeletableEntity]
```

**Infrastructure class hierarchy:**

```text
BaseRepository<T>                           (implements IRepository<T>)
├── EditableRepository<T>                   (implements IEditableRepository<T>)
│   └── SoftDeletableRepository<T>          (implements IMutableRepository<T>)
│       ├── ResumeRepository
│       ├── EducationRepository
│       ├── CoverLetterRepository
│       ├── CoverLetterTemplateRepository
│       └── UserRepository                  (also implements IUserRepository)
└── VacancyRepository                       (implements IVacancyRepository + SoftDeleteAsync directly)
```

**All six soft-deletable entity repositories and their final interface types:**

| Repository | Interface in UnitOfWork / Specialized |
| --- | --- |
| `ResumeRepository` | `IMutableRepository<Resume>` |
| `EducationRepository` | `IMutableRepository<Education>` |
| `CoverLetterRepository` | `IMutableRepository<CoverLetter>` |
| `CoverLetterTemplateRepository` | `IMutableRepository<CoverLetterTemplate>` |
| `UserRepository` | `IUserRepository` (extends `IEditableRepository<User>` + `ISoftDeleteRepository<User>`) |
| `VacancyRepository` | `IVacancyRepository` (extends `IRepository<Vacancy>` + `ISoftDeleteRepository<Vacancy>`) |

**`GetByIdAsync` contract:** Returns `Task<T>` (non-nullable). The repository implementation throws `NotFoundException` when no entity with the given `id` exists. Callers must not null-check the result — the throw guarantee is part of the contract. This is why handler examples do not include `if (entity == null)` guards after `GetByIdAsync` calls.

**Current UnitOfWork exposure after story 3.3:**

```csharp
public interface IUnitOfWork : IAsyncDisposable
{
    IUserRepository UserRepository { get; }          // IUserRepository extends ISoftDeleteRepository<User>
    IVacancyRepository VacancyRepository { get; }    // IVacancyRepository extends ISoftDeleteRepository<Vacancy>
    IMutableRepository<CoverLetter> CoverLetterRepository { get; }
    IMutableRepository<CoverLetterTemplate> CoverLetterTemplateRepository { get; }
    IMutableRepository<Resume> ResumeRepository { get; }
    IMutableRepository<Education> EducationRepository { get; }

    Task<int> SaveChangesAsync(CancellationToken ct);
    Task BeginTransactionAsync(CancellationToken ct);
    Task CommitTransactionAsync(CancellationToken ct);
    Task RollbackTransactionAsync(CancellationToken ct);
}
```

**Current Pagination/User-Scoping Pattern:**

`PagedQuery.UserId` is the current mechanism for user-scoped list queries. `BaseRepository<T>.GetAsync` detects a `UserId` property and applies the filter when present. This is accepted for the current codebase, but it is also listed in deferred work because it mixes ownership filtering into a Domain value object.

For Epic 3 and later, prefer the generic repository path unless the resource needs a real specialized query. Do not introduce bespoke repository interfaces only for naming symmetry.

**Handler Usage (UnitOfWork hides repositories):**

```csharp
public async Task<CreateResumeResponse> Handle(CreateResumeCommand req, CancellationToken ct)
{
    // Verify ownership
    var userExists = await _unitOfWork.UserRepository.IsExistsAsync(req.UserId, ct);
    if (!userExists)
        throw new NotFoundException($"User with id {req.UserId} not found");
    
    // Create resume
    var resume = new Resume { UserId = req.UserId, Title = req.Title, /* ... */ };
    await _unitOfWork.ResumeRepository.CreateAsync(resume, ct);
    await _unitOfWork.SaveChangesAsync(ct);
    
    return new CreateResumeResponse { Id = resume.Id };
}
```

---

### Decision 4: Error Handling & Response Format

**Decision:** RFC 7808 Problem Details format for all error responses.

**HTTP Status Codes:**

| Code | Scenario | Example |
|------|----------|---------|
| `200 OK` | Successful GET, PUT | Return resource |
| `201 Created` | Successful POST | Return created resource + Location header |
| `204 No Content` | Successful DELETE | Return empty body |
| `400 Bad Request` | Validation error | Field-level errors (format, length, etc.) |
| `404 Not Found` | Resource not found | Resume ID doesn't exist |
| `409 Conflict` | Uniqueness violation | Email already in use |
| `422 Unprocessable Entity` | Semantic error | VacancyId doesn't exist (FK broken) |
| `500 Internal Server Error` | Unhandled server error | Unexpected exception |

**Error Response Structure:**

```json
{
  "type": "https://api.jobnecto.dev/errors/validation",
  "title": "Validation Failed",
  "status": 400,
  "detail": "One or more validation errors occurred.",
  "traceId": "0HN4GTQ3F6P72:00000001",
  "errors": {
    "email": ["Email is already in use"],
    "phone": ["Phone must be in E.164 format"]
  }
}
```

**Custom Exception Mapping:**

```csharp
// In API exception middleware or ActionFilter

public class ExceptionHandlingMiddleware
{
    public async Task InvokeAsync(HttpContext context, ILogger<ExceptionHandlingMiddleware> logger)
    {
        try { await _next(context); }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            await HandleExceptionAsync(context, ex);
        }
    }
    
    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/problem+json";
        
        var response = exception switch
        {
            ValidationException ve => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation Failed",
                Type = "https://api.jobnecto.dev/errors/validation",
                Detail = "One or more validation errors occurred.",
                Extensions = new Dictionary<string, object?> { { "errors", ve.Errors } }
            },
            UserNotFoundException ue => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Not Found",
                Detail = ue.Message
            },
            DuplicateEmailException de => new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Conflict",
                Detail = de.Message
            },
            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Internal Server Error"
            }
        };
        
        context.Response.StatusCode = response.Status ?? StatusCodes.Status500InternalServerError;
        return context.Response.WriteAsJsonAsync(response);
    }
}
```

---

### Decision 5: Async Patterns & CancellationTokens

**Decision:** Full async/await chain with CancellationToken propagation.

**Handler Signature:**

```csharp
public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken)
{
    // Pass cancellationToken to all async operations
    var user = await _unitOfWork.UserRepository.GetByIdAsync(id, cancellationToken);
    await _unitOfWork.SaveChangesAsync(cancellationToken);
}
```

**Repository Async Pattern:**

```csharp
public async Task<User> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
{
    return await _context.Users
        .AsNoTracking()
        .FirstOrDefaultAsync(u => u.Id == id, cancellationToken)
        ?? throw new NotFoundException($"User with id {id} not found");
}
```

**API Endpoint Signature:**

```csharp
[HttpPost("users")]
public async Task<ActionResult<CreateUserResponse>> Create(
    [FromBody] CreateUserCommand command,
    IMediator mediator,
    CancellationToken cancellationToken) // Automatically populated by ASP.NET Core
{
    var result = await mediator.Send(command, cancellationToken);
    return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
}
```

**Test Pattern (using CancellationToken):**

```csharp
[Fact]
public async Task Handle_ValidRequest_CreatesUser()
{
    // Use CancellationToken.None for tests
    var command = new CreateUserCommand { /* ... */ };
    var handler = new CreateUserCommandHandler(_unitOfWork, _validator);
    
    var result = await handler.Handle(command, CancellationToken.None);
    
    Assert.NotNull(result);
}
```

---

### Decision 6: Soft Delete Implementation

**Decision:** EF Core global query filters to exclude soft-deleted records from all queries; PostgreSQL cascade rules enforce referential integrity; logging on hard-deletes for audit.

**Epic 2 implementation note:** Resume and Education soft deletes are implemented by setting `IsDeleted = true` and `DeletedAt = DateTime.UtcNow`, then relying on EF Core global query filters for exclusion. RowVersion/optimistic locking is not part of the current entity model and should be treated as future hardening, not as completed Phase B infrastructure.

**Entity Pattern (Domain Layer):**

```csharp
public abstract class SoftDeletableEntity : BaseEntity
{
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; } // Tracks soft-delete time for TTL grace period (Phase E)
}

public class Resume : SoftDeletableEntity
{
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    // other properties...
}
```

**DbContext Configuration (Infrastructure Layer):**

```csharp
public class AppDbContext : DbContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Global query filters for soft-delete entities
        modelBuilder.Entity<Resume>()
            .HasQueryFilter(r => !r.IsDeleted);
            
        modelBuilder.Entity<Education>()
            .HasQueryFilter(e => !e.IsDeleted);
            
        modelBuilder.Entity<CoverLetterTemplate>()
            .HasQueryFilter(c => !c.IsDeleted);
            
        modelBuilder.Entity<CoverLetter>()
            .HasQueryFilter(c => !c.IsDeleted);
            
        modelBuilder.Entity<Vacancy>()
            .HasQueryFilter(v => !v.IsDeleted);
        
        // PostgreSQL cascade rules for hard-deletes
        // User -> Resume cascade hard-delete
        modelBuilder.Entity<Resume>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade); // Hard-delete when User is deleted

        // User -> Education cascade hard-delete
        modelBuilder.Entity<Education>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade); // Hard-delete when User is deleted
        
        // User -> CoverLetter cascade hard-delete
        modelBuilder.Entity<CoverLetter>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(cl => cl.UserId)
            .OnDelete(DeleteBehavior.Cascade); // Hard-delete when User is deleted
    }
}
```

**Soft Delete Handler Pattern (after story R.1):**

Soft delete is performed via `SoftDeleteAsync` on `IMutableRepository<T>`. The flag-setting logic (`IsDeleted = true`, `DeletedAt = DateTime.UtcNow`) lives in `SoftDeletableRepository<T>`, not in the handler.

```csharp
public async Task<Unit> Handle(DeleteResumeCommand request, CancellationToken cancellationToken)
{
    var resume = await _unitOfWork.ResumeRepository.GetByIdAsync(request.ResumeId, cancellationToken);

    if (resume.UserId != request.UserId)
        throw new ForbiddenException("You do not have permission to delete this resume.");

    await _unitOfWork.ResumeRepository.SoftDeleteAsync(resume, cancellationToken);
    await _unitOfWork.SaveChangesAsync(cancellationToken);

    return Unit.Value;
}
```

**Hard Delete Handler Pattern (When User Permanently Deletes Account - Phase C/D):**

```csharp
public async Task<Unit> Handle(HardDeleteUserCommand request, CancellationToken cancellationToken)
{
    var user = await _unitOfWork.UserRepository.GetByIdAsync(request.UserId, cancellationToken);
    
    // Log hard-delete for audit
    _logger.LogInformation(
        "Hard-deleting user {UserId} ({Email}). Cascade will delete all resumes, educations, and cover letters.",
        user.Id, user.Email);
    
    // Future account-deletion stories must add the explicit hard-delete repository path.
    await _unitOfWork.UserRepository.DeleteAsync(user.Id, cancellationToken);
    await _unitOfWork.SaveChangesAsync(cancellationToken);
    
    _logger.LogInformation("User {UserId} and all related data hard-deleted successfully.", user.Id);
    
    return Unit.Value;
}
```

**Cascade Rules Summary:**

| Delete Type | Entity | Behavior | Cascades |
|-------------|--------|----------|----------|
| Soft Delete | Resume | Mark IsDeleted=true, set DeletedAt | None in current Epic 2 implementation |
| Soft Delete | Education | Mark IsDeleted=true, set DeletedAt | None |
| Soft Delete | CoverLetter | Mark IsDeleted=true, set DeletedAt | None |
| Hard Delete | User | Remove from DB (DELETE) | Resumes, Educations, CoverLetters, and CoverLetterTemplates hard-delete through PostgreSQL FK cascades |
| Hard Delete | Vacancy | Remove from DB (DELETE) | CoverLetters hard-delete through PostgreSQL FK cascade |

**Logging Strategy:**

- **Soft deletes** (user-initiated): Minimal logging (data recoverable within 1-month grace)
- **Hard deletes** (system/admin): Full audit log with timestamp, user ID, affected records count
- **Log location:** Application logs + database audit trail (if compliance required)

**Key Benefits:**

- Soft-deleted records automatically excluded from ALL queries (no accidental exposure)
- Single source of truth for deletion logic via query filters
- PostgreSQL foreign key cascades enforce data integrity (no orphaned records after hard-delete)
- Audit logging provides compliance trail for hard-deletes
- Grace period (TTL) deferred to Phase E; DeletedAt timestamp ready for future implementation

---

### Decision 7: Ownership & Authorization Checks

**Decision:** Handlers verify ownership before mutations; repository layer enforces user-scoped queries.

**Epic 2 implementation note:** List queries are user-scoped through `PagedQuery.UserId` and `BaseRepository<T>.GetAsync`. Single-record handlers currently call `GetByIdAsync` first and then verify ownership in the handler. This preserves current contracts, but ownership-aware single-record access is a recommended Epic 3 decision before template detail/update/delete patterns multiply.

**Response semantics after Epic 2:**

- Cross-user detail reads should return `404 Not Found` when the API should not reveal existence.
- Cross-user update/delete operations should return `403 Forbidden` when the story acceptance criteria require explicit forbidden mutation behavior.
- User-owned list endpoints must always include the authenticated user ID from controller auth context.

**Pattern: Ownership Check in Handler**

```csharp
public async Task<Unit> Handle(UpdateResumeCommand request, CancellationToken ct)
{
    var resume = await _unitOfWork.ResumeRepository.GetByIdAsync(request.Id, ct);
    
    // CRITICAL: Verify ownership before mutation
    if (resume.UserId != request.UserId)
        throw new ForbiddenException($"User {request.UserId} cannot modify resume {request.Id}");
    
    // Update allowed
    resume.Title = request.Title;
    resume.Skills = request.Skills;
    await _unitOfWork.ResumeRepository.UpdateAsync(resume, ct);
    await _unitOfWork.SaveChangesAsync(ct);
    
    return Unit.Value;
}
```

**Pattern: User-Scoped Queries in Handlers**

```csharp
// Query for lists always includes UserId filter
public async Task<PagedResult<ResumeDto>> Handle(ListResumesQuery request, CancellationToken ct)
{
    var pagedQuery = new PagedQuery
    {
        UserId = request.UserId,
        LastSeenId = request.LastSeenId,
        LastSeenUpdatedAt = request.LastSeenUpdatedAt,
        PageSize = request.PageSize
    };

    // BaseRepository applies UserId filtering for entities that expose UserId.
    var resumes = await _unitOfWork.ResumeRepository.GetAsync(pagedQuery, ct);
    
    return resumes;
}
```

**Pattern: How UserId Gets Into Query**

UserId is extracted from authenticated JWT claims in both Phase B and onwards:

```csharp
public async Task<ActionResult<PagedResult<ResumeDto>>> ListResumes(
    [FromQuery] int page = 1,
    IMediator mediator,
    CancellationToken cancellationToken)
{
    var query = new ListResumesQuery 
    { 
        UserId = GetCurrentUserId(),
        Page = page,
        PageSize = 20
    };
    
    var result = await mediator.Send(query, cancellationToken);
    return Ok(result);
}

private Guid GetCurrentUserId()
{
    // Extract UserId from JWT claim (set during login/token issue)
    var claim = HttpContext.User.FindFirst("sub") ?? HttpContext.User.FindFirst("userId");
    return claim != null && Guid.TryParse(claim.Value, out var id) ? id : Guid.Empty;
}
```

**JWT in Phase B:**

- Browser clients receive JWT sessions via HTTP-only secure cookie; non-browser clients use `Authorization: Bearer`
- `UserId` is stored as `sub` or `userId` claim and extracted uniformly in controllers/handlers
- Every mutation handler already verifies ownership
- Every query handler already receives `UserId` as a parameter from the controller
- Token renewal contract is `POST /api/v1/users/token/refresh` for currently authenticated sessions; browser clients receive renewed HTTP-only cookie and non-browser clients receive bearer token payload
- If refresh is rejected due to expired/invalid credentials, client must re-authenticate
- OpenAPI exposes both `CookieAuth` and `BearerAuth` security schemes with this lifecycle guidance
- Phase C (if applicable) can extend claims and role-based controls without changing handler logic

---
