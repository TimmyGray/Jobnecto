---
stepsCompleted:
  - step-01-init
  - step-02-context
  - step-03-architectural-decisions
inputDocuments:
  - e:\apps\Jobnecto\_bmad-output\planning-artifacts\prd.md
  - e:\apps\Jobnecto\_bmad-output\project-context.md
workflowType: 'architecture'
project_name: 'Jobnecto'
user_name: 'Timmy'
date: '2026-04-17'
---

# Architecture Decision Document — Jobnecto Phase B

_This document builds collaboratively through step-by-step discovery. We'll make and document architectural decisions together to guide implementation._

## Project Context Analysis

### Requirements Overview

**Functional Requirements (Phase B):**
- 6 primary domain resources: User (Profile), Resume, Education, CoverLetterTemplate, CoverLetter, Vacancy
- CRUD operations on user-owned resources (User, Resume, Education, Template, Letter)
- Read-only operations on shared resources (Vacancy browsing and filtering)
- Filtering: Complex multi-criteria vacancy filtering via POST body (skills, location, salary, work type, etc.)
- Paginated list operations with configurable page size (default 20, max 100)

**Non-Functional Requirements:**
- API response SLA: <200ms for list endpoints, <100ms for detail endpoints, <500ms for complex filters
- Soft delete strategy with EF Core global query filters (data persists in DB but excluded from queries)
- Ownership model: Each user sees only their own data; architecture prepared for Phase C JWT integration
- Validation: FluentValidation pipeline before handlers reach business logic; field-level error responses
- Test coverage requirement: ≥85% code coverage on Application handlers and validators
- Async throughout: Full async/await chain with CancellationToken support on public APIs

**Scale & Complexity:**
- Complexity level: **Medium** (multi-domain entities, complex filtering logic, relationship complexity, soft deletes)
- Primary technical domain: **REST API Backend** (job aggregation and profile management)
- Estimated architectural components required: **7-8 major architectural decisions**

### Technical Constraints & Dependencies

**Technology Stack (Locked):**
- .NET 10 (`net10.0`), nullable reference types enforced
- ASP.NET Core 10.0.3 with OpenAPI/Swashbuckle auto-documentation
- MediatR 14.0.0 for CQRS command/query pattern
- FluentValidation 12.1.1 with built-in ASP.NET Core DI extensions
- Entity Framework Core 10.0.3 with Npgsql provider for PostgreSQL
- xUnit 2.9.3 + FluentAssertions 8.9.0 + Moq 4.20.72 for testing
- Project uses `backend/JobNecto.slnx` (not root `.sln`)

**Architecture Constraints:**
- Clean Architecture enforced: API → Application → Domain ← Infrastructure
- Domain layer must be persistence-ignorant (no EF Core references)
- Application layer contains handlers, validators, repository interfaces
- Infrastructure layer implements repositories, EF configurations, migrations
- No reference inversions or architectural boundary violations allowed

**Build & Quality Gates:**
- Must pass: `dotnet build backend/JobNecto.slnx --configuration Release --warnaserror`
- Must pass: `dotnet test backend/JobNecto.slnx --configuration Release --warnaserror`
- No nullable reference type warnings without documented suppression reason
- CI-parity enforcement: Release build + warnings-as-errors

**Phase A Assumptions (Must Be Complete):**
- UnitOfWork pattern implemented with `SaveChangesAsync()` and `DisposeAsync()`
- All repository interfaces defined in Application layer
- PostgreSQL connection available — see `appsettings.local.json` under `ConnectionStrings:Default` for the local connection string
- EF Core migrations system functional
- `AddInfrastructure()` DI registration wired in `Program.cs`

**Out of Scope for Phase B:**
- Authentication & JWT (Phase C)
- Job source synchronization and OAuth integrations (Phase D)
- LLM analysis and cover letter generation endpoints (Phase D)
- Rate limiting, Redis caching, Quartz scheduled jobs (Phase E)

### Cross-Cutting Concerns

**1. Ownership & Authorization**
- Every resource must enforce user ownership before write operations
- **Resumes are user-scoped:** Each user sees only their own resumes (filtered at repository layer)
- All list queries automatically filter by current user (via query handlers or repository layer)
- Architecture must support Phase C JWT integration without refactoring core patterns
- Design pattern: ownership check delegated to handler, not API layer
- **UserId source:** Generated during user registration (profile creation); Phase B uses JWT bearer token with `UserId` stored as a claim; extracted in controllers from `HttpContext.User`

**2. Soft Delete Consistency**
- Entities marked for soft delete: Resume, Education, CoverLetter, CoverLetterTemplate, Vacancy
- Strategy: EF Core global query filters on `DbContext.OnModelCreating` + PostgreSQL cascade rules
- Soft-deleted records excluded from ALL queries unless explicitly requested
- Single source of truth: query filters prevent accidental data exposure across entire application
- **Cascade Hard-Delete Rules (PostgreSQL-level):**
  - User hard-deleted → all their Resumes cascade hard-delete
  - Resume soft-deleted → all related CoverLetters cascade soft-delete
  - Resume hard-deleted → all related CoverLetters cascade hard-delete
  - Education soft-deleted → no cascades (Resume remains; Education just filtered out)
- **Logging:** All hard-delete operations are logged for audit/compliance purposes
- **Grace Period (Future - Phase E):** TTL implemented in Phase E; soft-deleted records eligible for hard-delete after 1 month

**3. Validation & Error Handling**
- Two-layer validation strategy: FluentValidation (field-level) + handler-level (business rules)
- FluentValidation validators fire before handler execution (MediatR pipeline)
- Error response format: RFC 7808 Problem Details (application/problem+json)
- HTTP status codes: 200, 201, 204 (success); 400, 404, 409, 422, 500 (errors)
- Field-level errors: `{ "email": ["must be valid format"], "phone": ["already in use"] }`

**4. Complex Filtering Architecture**
- Vacancy filtering accepts POST body (not query params) to support multi-criteria queries
- Filter object contains: skills[], location, salaryMin/Max, workLocationType[], etc.
- AND logic between fields, OR logic within arrays (e.g., matches ANY skill)
- Pagination included in filter object (page, pageSize)
- Design advantage: No URL length limits; type-safe filtering with request models

**5. Async-First Pattern**
- All I/O operations: async/await end-to-end
- CancellationToken parameters on all public async handlers
- EF Core async methods: `ToListAsync()`, `FirstOrDefaultAsync()`, `SaveChangesAsync()`
- Database context should be disposed via `await using` in tests

**6. Database Migrations & Relationships**
- Migrations live in Infrastructure; tracked via EF Core schema history
- Entity relationships: 1:N (User→Resume, User→Education, Resume→CoverLetter), M:N (Resume↔Education via join table)
- **Cascade Rules (EF Core + PostgreSQL):**
  - User → Resume: ON DELETE CASCADE (hard-delete when user deleted)
  - Resume → CoverLetter: ON DELETE CASCADE (hard-delete when resume deleted)
  - Soft deletes use EF Core global filters; cascades handled in OnModelCreating
  - PostgreSQL foreign keys enforce referential integrity; EF tracks related entities
- ResumeEducations join table: supports M:N linkage without direct references; cascade soft-deletes to join table entries

**7. OpenAPI Documentation**
- Swashbuckle auto-generates OpenAPI spec from attributes and models
- Every endpoint must have clear request/response models for auto-documentation
- Status codes documented in swagger attributes (200, 201, 400, 404, 422, etc.)
- Models exported to `/openapi/v1.json` for frontend code generation

## Core Architectural Decisions

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
        _unitOfWork.Users.Add(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new CreateUserResponse { Id = user.Id };
    }
}
```

---

### Decision 2: Validation Pipeline

**Decision:** Two-layer validation: **FluentValidation (field-level) → Handler (business rules)**

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
    var existingUser = await _unitOfWork.Users
        .FirstOrDefaultAsync(u => u.Email == request.Email, ct);
    if (existingUser != null)
        throw new DuplicateEmailException(request.Email);
    
    // Create user...
    await _unitOfWork.SaveChangesAsync(ct);
}
```

**Error Response Format (from validation failures):**
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

**Decision:** Repository interfaces defined in Application layer; implementations in Infrastructure. Repositories encapsulate query logic.

**Application Layer (Interfaces):**

```csharp
namespace JobNecto.Application.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByLoginNameAsync(string loginName, CancellationToken cancellationToken = default);
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);
}

public interface IResumeRepository
{
    Task<Resume?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Resume>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<PagedResult<Resume>> GetPaginatedByUserIdAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task AddAsync(Resume resume, CancellationToken cancellationToken = default);
    Task UpdateAsync(Resume resume, CancellationToken cancellationToken = default);
}

// Similar for Education, CoverLetterTemplate, CoverLetter, Vacancy repositories
```

**Infrastructure Layer (Implementation Pattern):**

```csharp
namespace JobNecto.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;
    
    public UserRepository(AppDbContext context) => _context = context;
    
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }
    
    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }
    
    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        // No need for explicit add; UnitOfWork tracks changes
        await _context.Users.AddAsync(user, cancellationToken);
    }
}
```

**UnitOfWork Integration:**

```csharp
public interface IUnitOfWork : IAsyncDisposable
{
    IUserRepository Users { get; }
    IResumeRepository Resumes { get; }
    IEducationRepository Educations { get; }
    ICoverLetterTemplateRepository CoverLetterTemplates { get; }
    ICoverLetterRepository CoverLetters { get; }
    IVacancyRepository Vacancies { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

**Handler Usage (UnitOfWork hides repositories):**

```csharp
public async Task<CreateResumeResponse> Handle(CreateResumeCommand req, CancellationToken ct)
{
    // Verify ownership
    var user = await _unitOfWork.Users.GetByIdAsync(req.UserId, ct);
    if (user == null)
        throw new UserNotFoundException(req.UserId);
    
    // Create resume
    var resume = new Resume { UserId = req.UserId, Title = req.Title, /* ... */ };
    await _unitOfWork.Resumes.AddAsync(resume, ct);
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
    var user = await _unitOfWork.Users.GetByIdAsync(id, cancellationToken);
    await _unitOfWork.SaveChangesAsync(cancellationToken);
}
```

**Repository Async Pattern:**
```csharp
public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
{
    return await _context.Users
        .AsNoTracking()
        .FirstOrDefaultAsync(u => u.Id == id, cancellationToken); // Pass token
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
    [Timestamp] // Optimistic locking for Phase C concurrency safety
    public byte[] RowVersion { get; set; }
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
        // User → Resume cascade hard-delete
        modelBuilder.Entity<Resume>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade); // Hard-delete when User is deleted
        
        // Resume → CoverLetter cascade hard-delete
        modelBuilder.Entity<CoverLetter>()
            .HasOne<Resume>()
            .WithMany()
            .HasForeignKey(cl => cl.ResumeId)
            .OnDelete(DeleteBehavior.Cascade); // Hard-delete when Resume is hard-deleted
    }
}
```

**Soft Delete Handler Pattern:**
```csharp
public async Task<Unit> Handle(DeleteResumeCommand request, CancellationToken cancellationToken)
{
    var resume = await _unitOfWork.Resumes.GetByIdAsync(request.Id, cancellationToken);
    if (resume == null)
        throw new ResumeNotFoundException(request.Id);
    
    // Verify ownership
    if (resume.UserId != request.UserId)
        throw new UnauthorizedAccessException();
    
    // Soft delete: mark as deleted and timestamp for TTL grace period
    resume.IsDeleted = true;
    resume.DeletedAt = DateTime.UtcNow;
    
    // Cascade soft-delete to related CoverLetters
    var relatedLetters = await _unitOfWork.CoverLetters.GetByResumeIdAsync(request.Id, cancellationToken);
    foreach (var letter in relatedLetters.Where(l => !l.IsDeleted))
    {
        letter.IsDeleted = true;
        letter.DeletedAt = DateTime.UtcNow;
    }
    
    await _unitOfWork.SaveChangesAsync(cancellationToken);
    
    return Unit.Value;
}
```

**Hard Delete Handler Pattern (When User Permanently Deletes Account - Phase C/D):**
```csharp
public async Task<Unit> Handle(HardDeleteUserCommand request, CancellationToken cancellationToken)
{
    var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
    if (user == null)
        throw new UserNotFoundException(request.UserId);
    
    // Log hard-delete for audit
    _logger.LogInformation(
        "Hard-deleting user {UserId} ({Email}). Cascade will delete all resumes, educations, and cover letters.",
        user.Id, user.Email);
    
    // PostgreSQL cascade rules will hard-delete all related Resumes, CoverLetters automatically
    _unitOfWork.Users.Remove(user);
    await _unitOfWork.SaveChangesAsync(cancellationToken);
    
    _logger.LogInformation("User {UserId} and all related data hard-deleted successfully.", user.Id);
    
    return Unit.Value;
}
```

**Cascade Rules Summary:**
| Delete Type | Entity | Behavior | Cascades |
|-------------|--------|----------|----------|
| Soft Delete | Resume | Mark IsDeleted=true, set DeletedAt | CoverLetters soft-delete |
| Soft Delete | Education | Mark IsDeleted=true, set DeletedAt | None (orphaned join table entries) |
| Soft Delete | CoverLetter | Mark IsDeleted=true, set DeletedAt | None |
| Hard Delete | Resume | Remove from DB (DELETE) | CoverLetters hard-delete (PostgreSQL FK) |
| Hard Delete | User | Remove from DB (DELETE) | Resumes hard-delete (PostgreSQL FK); cascades to CoverLetters |

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

**Pattern: Ownership Check in Handler**
```csharp
public async Task<Unit> Handle(UpdateResumeCommand request, CancellationToken ct)
{
    var resume = await _unitOfWork.Resumes.GetByIdAsync(request.Id, ct);
    if (resume == null)
        throw new ResumeNotFoundException(request.Id);
    
    // CRITICAL: Verify ownership before mutation
    if (resume.UserId != request.UserId)
        throw new UnauthorizedAccessException($"User {request.UserId} cannot modify resume {request.Id}");
    
    // Update allowed
    resume.Title = request.Title;
    resume.Skills = request.Skills;
    await _unitOfWork.SaveChangesAsync(ct);
    
    return Unit.Value;
}
```

**Pattern: User-Scoped Queries in Handlers**
```csharp
// Query for lists always includes UserId filter
public async Task<PagedResult<ResumeDto>> Handle(ListResumesQuery request, CancellationToken ct)
{
    // Repository returns only resumes for this user
    var resumes = await _unitOfWork.Resumes.GetPaginatedByUserIdAsync(
        request.UserId,  // User must be passed from request context
        request.Page,
        request.PageSize,
        ct);
    
    return resumes;
}
```

**Pattern: How UserId Gets Into Query**

UserId is extracted from the JWT bearer token claim in both Phase B and onwards:

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
- JWT bearer token issued on login; `UserId` stored as `sub` or `userId` claim
- Every mutation handler already verifies ownership
- Every query handler already receives `UserId` as a parameter from the controller
- Phase C (if applicable) can extend claims or rotate tokens without changing handler logic

---

## Summary of Architectural Decisions

| Decision | Approach | Key Benefit |
|----------|----------|------------|
| Request Handling | MediatR Commands/Queries | Consistent request flow, testable business logic |
| Validation | FluentValidation + Handler layer | Separation of concerns, reusable validators |
| Data Access | Repository pattern with UnitOfWork | Abstraction from EF Core, testable with in-memory |
| Errors | RFC 7808 Problem Details | Standardized, frontend-friendly error format |
| Async | Full chain with CancellationToken | Graceful timeout handling, responsive API |
| Soft Delete + Cascades | EF Core global filters + PostgreSQL FK cascades + Audit logging | Data safety, referential integrity, compliance trail |
| Ownership Model | User-scoped repositories + Handler ownership checks | Only users see their own resumes; Phase C ready |
| Concurrency Control | Optimistic locking (RowVersion/Timestamp) | Prevents lost updates under concurrent load |

---

## Implementation Checklist for Phase B

**Database & Entities:**
- [ ] Add `IsDeleted` and `DeletedAt` timestamps to: Resume, Education, CoverLetterTemplate, CoverLetter, Vacancy
- [ ] Add `[Timestamp]` RowVersion to all entities for optimistic locking
- [ ] Configure global query filters for soft-delete entities
- [ ] Configure PostgreSQL cascade rules in OnModelCreating (User→Resume→CoverLetter hard-deletes)
- [ ] Create migration with all schema changes

**Handlers & Repositories:**
- [ ] Implement user-scoped repository methods (GetByUserIdAsync, ListByUserIdAsync, etc.)
- [ ] Soft-delete handlers: Set IsDeleted=true, propagate cascade soft-deletes to children
- [ ] All mutation handlers: Verify ownership before allowing changes (409 Conflict if forbidden)
- [ ] All query handlers: Filter by UserId in request (user sees only their own data)

**Testing:**
- [ ] Soft-delete audit fixtures: Verify deleted data excluded from queries but exists in DB
- [ ] Ownership violation suite: Run against all mutation handlers
- [ ] Cascade soft-delete tests: Resume soft-delete→CoverLetter soft-delete
- [ ] Cascade hard-delete tests (future): User hard-delete→Resume hard-delete→CoverLetter hard-delete
- [ ] CancellationToken timeout tests: At least one per resource

**Logging & Audit:**
- [ ] Log all hard-delete operations with timestamp, user ID, affected records
- [ ] Application logs or database audit table (depending on compliance needs)

**Phase C Readiness:**
- [ ] UserId extraction from request (Phase B) → JWT token (Phase C) is straightforward
- [ ] Handlers already receive UserId as parameter; no refactoring needed
- [ ] Ownership checks are centralized per handler; easy to audit for Phase C

These decisions are codified to ensure **consistency across all Phase B endpoints** and to make your developers' jobs straightforward: follow the patterns, and the architecture handles the rest.

