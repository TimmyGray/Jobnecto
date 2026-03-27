# AGENTS.md

## Cursor Cloud specific instructions

### Project overview

JobNecto is a .NET 10 backend API for job vacancy aggregation and matching. Clean Architecture with layers: API, Application, Domain, Infrastructure, Infrastructure.LLM, Infrastructure.JobSources. See `.github/workflows/ci.yml` for CI commands.

### Build, test, lint

- **Solution file:** `backend/JobNecto.slnx` (used by CI and for all dotnet commands)
- **Build:** `dotnet build backend/JobNecto.slnx`
- **Test:** `dotnet test backend/JobNecto.slnx`
- **CI-equivalent lint/build:** `dotnet build backend/JobNecto.slnx --configuration Release --warnaserror`

### Running the API

- Start: `cd backend/src/JobNecto.API && ASPNETCORE_ENVIRONMENT=Development DOTNET_URLS="http://localhost:5000" dotnet run`
- The API serves OpenAPI spec at `GET /openapi/v1.json`.
- No user-facing routes exist yet; 404 on root is expected.

### PostgreSQL

- Required for the full application (EF Core + Npgsql).
- Dev config (`appsettings.Development.json`) expects: `Host=localhost;Port=5432;Database=JobNecto;Username=admin;Password=admin` (Npgsql requires `Key=Value` pairs, including `Port=5432`).
- The `Program.cs` does not yet call `AddInfrastructure()`, so the DB connection is not used at startup. When it is wired up, PostgreSQL must be running with the above credentials.
- To start PostgreSQL: `sudo pg_ctlcluster 16 main start`

### Cursor agent command: document functions

- **Command:** `.cursor/commands/document-functions.md`
- **Skill:** `.cursor/skills/document-functions/SKILL.md`
- **Purpose:** Add C# XML docs (`///` with `<summary>`, `<param>`, `<returns>`) to members that benefit from documentation; skip trivial code and thin repositories per the skill.
- **Arguments:** Optional paths, globs, or symbol names to limit scope; with no arguments, walk `backend/src` systematically file-by-file.

### Gotchas

- The root `.sln` (`Jobnecto.sln`) uses Windows-style backslash paths and does not include the test project. Always use `backend/JobNecto.slnx` for builds and tests.
- Docker files (`docker/Dockerfile`, `docker/docker-compose.yml`) are empty placeholders.
- Redis and Quartz are referenced in `Infrastructure.csproj` but have no implementation yet.

## Mandatory comprehensive PR review workflow

Run this workflow every time a PR is created or updated.

### Trigger

- PR opened, synchronized, rebased, or receives follow-up commits.
- Any request to "review PR", "review changes", or "code review".

### Required execution model

- Use a separate subagent dedicated to review, with description `Code reviewer`.
- Do not skip this review even when implementation appears small.

### Review scope (must include all)

1. **Correctness of new implementation**
   - Validate behavior against changed requirements and code intent.
2. **Regression safety**
   - Check whether existing functionality can break due to changed contracts, side effects, or shared components.
   - Verify project-level health (build/test stability for the solution).
3. **Best practices + code quality**
   - Clean Architecture boundaries, readability, naming, null/error handling, security, and maintainability.
4. **Optimization opportunities**
   - Identify unnecessary complexity, possible performance waste, and simplification opportunities.
5. **Test coverage**
   - Confirm tests cover the new behavior and important edge cases.
   - If coverage is missing, propose concrete tests.
6. **Test execution**
   - Run relevant targeted tests first, then run:
     - `dotnet test backend/JobNecto.slnx`
   - For CI-parity confidence on risky changes, also run:
     - `dotnet build backend/JobNecto.slnx --configuration Release --warnaserror`
     - `dotnet test backend/JobNecto.slnx --configuration Release --no-build --warnaserror`

### Required review output format

- Prioritized findings ordered by severity.
- For each finding include:
  - `severity`: `critical | high | medium | low`
  - `risk_score`: integer `1-10` (10 = highest risk)
  - `impact`: what can break and who is affected
  - `evidence`: file paths, symbols, commands, or test outputs
  - `recommended_fix`: concrete change suggestion
- Explicitly state when no findings are detected.
