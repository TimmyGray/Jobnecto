---
name: jobnecto-postgresql
description: Connects to the JobNecto PostgreSQL database using dev connection strings, CLI tools (psql, dotnet ef), repository patterns, and ad-hoc SQL for reads and test data. Use when querying the DB, inserting seed or test rows, running migrations, or when the user mentions Postgres, Npgsql, test data, or local database setup for this project.
---

# JobNecto PostgreSQL

## Connection strings (Development)

- **Read credentials from**: `backend/src/JobNecto.API/appsettings.Development.json` → `ConnectionStrings:Postgres` (used when `ASPNETCORE_ENVIRONMENT=Development`).
- **Fallback template**: `backend/src/JobNecto.API/appsettings.json` → same key.
- **Registration in app**: `GetConnectionString("Postgres")` → `AppDbContext` with Npgsql (`backend/src/JobNecto.Infrastructure/DI.cs`).

Npgsql uses semicolon-separated `Key=Value` pairs (e.g. `Port=5432`).

Parse the string for CLI use:

| Part        | Discovery Method |
|------------|-------------------------|
| Host       | `ConnectionStrings:Postgres` (parse `Host=`) |
| Port       | `ConnectionStrings:Postgres` (parse `Port=`) |
| Database   | `ConnectionStrings:Postgres` (parse `Database=`) |
| Username   | `ConnectionStrings:Postgres` (parse `Username=`) |
| Password   | `ConnectionStrings:Postgres` (parse `Password=`) |

> [!IMPORTANT]
> **DO NOT hardcode credentials.** Always parse them from the configuration files at runtime.

## CLI: get data from the database

**Preferred for quick inspection**: `psql` (PostgreSQL client). Install via PostgreSQL tools or package manager if missing.

1. Load host/port/db/user/password from `appsettings.Development.json` (do not hardcode secrets from production).
2. Connect (PowerShell example):

```powershell
$env:PGPASSWORD = '<Password>'
psql -h <Host> -p <Port> -U <Username> -d <Database>
```

3. Inside `psql`, discover schema:

```text
\dt
\d "Users"
```

4. Run read-only SQL as needed, e.g. `SELECT "Id", "Email", "Login" FROM "Users" LIMIT 10;`

**Notes**

- Table and column names are **PascalCase** in this model unless a configuration renames them; quote identifiers in SQL when case-sensitive (`"Users"`, `"Id"`).
- For relationship order: insert **Users** before rows that reference `UserId` (Resumes, Educations, Vacancies, CoverLetters, CoverLetterTemplates). **CoverLetters** need both `UserId` and `VacancyId`.

2. **Discovery over convention**: If you need to run SQL, always use the credentials discovered from the configuration.

3. Respect **check constraints** and **required columns** from entity configs (`backend/src/JobNecto.Infrastructure/Persistance/Config/*Configuration.cs`). Examples:
   - **Users**: valid email pattern, optional `Phone` must match `+` E.164 if set, `Age` between 1 and 99 if set.
   - **Vacancies**: `JobSource` is stored as **jsonb** (shape matches `JobSource` in domain code).
4. Use `gen_random_uuid()` for new `uuid` keys if the DB supports it, or generate GUIDs in the script.

For column-level details and table list, see [reference.md](reference.md).

## Stack (application code)

| Piece | Location |
|-------|----------|
| DbContext | `backend/src/JobNecto.Infrastructure/Persistance/AppDbContext.cs` |
| EF Core + Npgsql | `JobNecto.Infrastructure.csproj` |
| Migrations assembly | `JobNecto.Infrastructure` |

## Entities (DbSets)

`Users`, `Resumes`, `Educations`, `CoverLetters`, `CoverLetterTemplates`, `Vacancies`. Join table: `ResumeEducations` (many-to-many). Configurations: `Persistance/Config/*Configuration.cs`.

## CRUD in C# (when changing app code)

Prefer **repository + unit of work** (`IRepository<T>`, `IEditableRepository<T>`, `IUnitOfWork`, `BaseRepository<T>`). After mutations, call `SaveChangesAsync` on the unit of work when implemented.

## EF Core CLI (migrations and schema)

From repo root:

```bash
dotnet ef migrations add <Name> --project backend/src/JobNecto.Infrastructure --startup-project backend/src/JobNecto.API
dotnet ef database update --project backend/src/JobNecto.Infrastructure --startup-project backend/src/JobNecto.API
```

Optional: generate SQL from the model for review:

```bash
dotnet ef dbcontext script --project backend/src/JobNecto.Infrastructure --startup-project backend/src/JobNecto.API
```

Ensure `ConnectionStrings:Postgres` is valid for the environment you target.

## Additional resources

- [reference.md](reference.md) — tables, constraints summary, pagination types, minimal SQL examples
