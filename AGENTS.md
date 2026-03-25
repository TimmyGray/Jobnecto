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

### Gotchas

- The root `.sln` (`Jobnecto.sln`) uses Windows-style backslash paths and does not include the test project. Always use `backend/JobNecto.slnx` for builds and tests.
- Docker files (`docker/Dockerfile`, `docker/docker-compose.yml`) are empty placeholders.
- Redis and Quartz are referenced in `Infrastructure.csproj` but have no implementation yet.
