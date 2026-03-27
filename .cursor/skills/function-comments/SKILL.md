---
name: function-comments
description: Add XML documentation comments to functions that benefit from them. Use when asked to document functions, add comments, or improve code documentation across the project.
---

# Function Comments

Add C# XML documentation comments (`/// <summary>`, `/// <param>`, `/// <returns>`) to functions and methods that benefit from documentation. Skip trivial or self-explanatory code.

## When to use

- User runs the `function-comments` command (with or without arguments).
- User asks to "add comments", "document functions", or "improve documentation".

## What to comment

### MUST comment (non-trivial logic that benefits developers and AI agents)

- **Interface method signatures** in the Application layer — these define contracts consumed by multiple implementations and callers.
- **Extension methods for DI registration** (`AddInfrastructure`, `AddApplication`, etc.) — describe what services are registered and any required configuration.
- **Methods with complex business logic** — filtering, scoring, matching, transformation pipelines, multi-step workflows.
- **Methods with non-obvious side effects** — methods that throw on missing entities, mutate external state, or have retry/fallback behavior.
- **Public API endpoints** — controllers, minimal API route handlers, middleware.
- **Generic/abstract base class methods** with non-trivial behavior — pagination strategies, cursor-based queries, soft-delete implementations.
- **Domain value objects and records** with business meaning — class-level `<summary>` when the type represents a domain concept that is not obvious from the name alone.

### SHOULD NOT comment (self-explanatory or boilerplate)

- Simple CRUD repository methods whose behavior is obvious from name and signature (e.g. `GetByIdAsync(Guid id)`, `CreateAsync(T entity)`, `DeleteAsync(Guid id)`).
- EF Core `IEntityTypeConfiguration.Configure` methods — the fluent API calls are self-documenting.
- Trivial property accessors, constructors that only assign fields, and empty/`NotImplementedException` stubs.
- Enum definitions, simple record/class declarations with only auto-properties or public fields.
- Private methods that are only called from one place and whose intent is clear from context.

### Grey area (use judgement)

- Private helper methods with moderate complexity — comment if the logic is not immediately clear.
- Overridden virtual methods — comment only if the override changes the contract or adds behavior beyond the base.
- Test methods — prefer descriptive test names over XML doc comments.

## Comment format

Use standard C# XML documentation:

```csharp
/// <summary>
/// Brief description of what the method does and why.
/// </summary>
/// <param name="paramName">What this parameter represents and any constraints.</param>
/// <returns>What is returned, including edge cases (null, empty, exceptions).</returns>
```

Rules:

1. `<summary>` is always required.
2. `<param>` tags for every parameter unless the method has zero parameters.
3. `<returns>` for methods that return a value; omit for `void`. For `Task` / `Task<T>` / `ValueTask` / `ValueTask<T>`, describe what completing the task means and what the result represents when there is one.
4. `<exception cref="...">` only when the method explicitly throws and callers need to know.
5. Keep descriptions concise — one to three sentences. Do not restate the method signature.
6. Focus on **intent**, **constraints**, and **edge-case behavior** rather than implementation details.
7. Write in third-person present tense ("Retrieves…", "Applies…", "Registers…").

## Execution workflow

### With arguments (targeted)

When the user provides file paths or function names:

1. Read each specified file.
2. Identify functions/methods matching the criteria above.
3. Add XML doc comments to qualifying functions.
4. Skip functions in the "SHOULD NOT comment" category.
5. Build to verify no compilation issues: `dotnet build backend/JobNecto.slnx`.

### Without arguments (full project scan)

When no arguments are provided, scan the entire `backend/src/` tree:

1. Process one project at a time in this order:
   - `JobNecto.Application` (interfaces and contracts first — highest value)
   - `JobNecto.Domain` (value objects, records, entity summaries)
   - `JobNecto.Infrastructure` (DI registration, non-trivial repository methods)
   - `JobNecto.Infrastructure.LLM` (if `.cs` files exist)
   - `JobNecto.Infrastructure.JobSources` (if `.cs` files exist)
   - `JobNecto.API` (endpoints, middleware, program configuration)
2. Within each project, process one file at a time.
3. For each file:
   - Read the file.
   - Identify every public/protected method and class.
   - Apply the "MUST comment" / "SHOULD NOT comment" criteria.
   - Add XML doc comments only where they add value.
   - Preserve existing `/// <summary>` comments — do not overwrite them unless they are obviously wrong or incomplete.
4. After all files in a project are processed, run: `dotnet build backend/JobNecto.slnx`.
5. Fix any issues before moving to the next project.
6. After all projects: run full build and test to confirm no regressions.

```bash
dotnet build backend/JobNecto.slnx
dotnet test backend/JobNecto.slnx
```

## Examples

### Good: Interface method with contract documentation

```csharp
/// <summary>
/// Retrieves a paginated list of vacancies filtered by the specified criteria.
/// Supports cursor-based pagination using <paramref name="pagedQuery"/>.
/// </summary>
/// <param name="pagedQuery">Pagination cursor with page size and last-seen identifiers.</param>
/// <param name="filter">Optional filter criteria; when null, no filtering is applied.</param>
/// <param name="ct">Cancellation token.</param>
/// <returns>A paged result containing matching vacancies and pagination metadata.</returns>
Task<PagedResult<Vacancy>> GetFilteredAsync(PagedQuery pagedQuery, VacancyFilter? filter = null, CancellationToken ct = default);
```

### Good: DI registration extension method

```csharp
/// <summary>
/// Registers infrastructure services including the EF Core database context
/// (PostgreSQL via Npgsql) and the Unit of Work pattern.
/// Reads the "Postgres" connection string from <paramref name="configuration"/>.
/// </summary>
/// <param name="services">The service collection to add registrations to.</param>
/// <param name="configuration">Application configuration containing connection strings.</param>
/// <returns>The service collection for chaining.</returns>
public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
```

### Good: Class-level summary for a domain concept

```csharp
/// <summary>
/// Captures a user's professional profile snapshot used as the primary input
/// for vacancy matching algorithms and LLM-driven cover letter generation.
/// </summary>
public sealed class Resume : BaseEntity
```

### Skip: Simple CRUD — no comment needed

```csharp
// No XML doc needed — GetByIdAsync is universally understood.
public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken ct)
{
    return await _dbSet.FindAsync([id], ct);
}
```

### Skip: EF configuration — no comment needed

```csharp
// No XML doc needed — fluent API calls are self-documenting.
public void Configure(EntityTypeBuilder<Vacancy> builder) { ... }
```
