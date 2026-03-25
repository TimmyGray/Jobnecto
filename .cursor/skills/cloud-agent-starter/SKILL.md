---
name: cloud-agent-starter
description: Minimal quick-start for Cloud agents on JobNecto. Use when starting work in this repo to run the app, execute tests, handle config/feature-flag-like toggles, and follow practical per-area workflows.
---

# JobNecto Cloud Agent Starter

Use this skill first when you are new to the repo or starting a fresh Cloud run.

## 0) Immediate setup checklist (first 2-3 minutes)

1. Work from repo root: `/workspace`.
2. Use the correct solution file (always): `backend/JobNecto.slnx`.
3. Restore/build once before editing:
   - `dotnet restore backend/JobNecto.slnx`
   - `dotnet build backend/JobNecto.slnx`
4. Know the current scope:
   - API is minimal and exposes OpenAPI in Development.
   - Root `/` returning 404 is expected.
   - No application login/auth flow is implemented yet.

## 1) API area (`backend/src/JobNecto.API`)

### Run workflow

From repo root:

```bash
cd backend/src/JobNecto.API
ASPNETCORE_ENVIRONMENT=Development DOTNET_URLS="http://localhost:5000" dotnet run
```

### Smoke-test workflow

In another terminal:

```bash
curl -i http://localhost:5000/openapi/v1.json
curl -i http://localhost:5000/
```

Expected:

- `/openapi/v1.json` returns `200` in Development.
- `/` returns `404` (current expected behavior).

### Practical notes

- There is currently no auth/login endpoint to "sign in" to; do not block on login setup.
- If a task mentions "login", clarify that auth is not wired yet and test endpoint behavior directly.
- Keep `DOTNET_URLS` explicit in Cloud to avoid port ambiguity.

## 2) Application + Domain areas (`backend/src/JobNecto.Application`, `backend/src/JobNecto.Domain`)

### Typical change workflow

1. Make code changes.
2. Run targeted tests first:
   - `dotnet test backend/JobNecto.slnx --filter FullyQualifiedName~<Name>`
3. Run full test project when stable:
   - `dotnet test backend/JobNecto.slnx`

### What to validate

- Business-rule changes should have or update unit tests in `backend/tests/JobNecto.Tests`.
- Prefer adding focused tests near changed behavior before running full suite.

## 3) Infrastructure + DB area (`backend/src/JobNecto.Infrastructure`)

If your task touches EF/Npgsql/migrations or repositories, include DB checks.

### PostgreSQL quick workflow

```bash
sudo pg_ctlcluster 16 main start
dotnet ef database update --project backend/src/JobNecto.Infrastructure --startup-project backend/src/JobNecto.API
```

Use Development connection string from:

- `backend/src/JobNecto.API/appsettings.Development.json`

Current dev default:

- `Host=localhost;Port=5432;Database=JobNecto;Username=admin;Password=admin`

### DB verification workflow

Use `psql` for quick checks:

```bash
PGPASSWORD=admin psql -h localhost -p 5432 -U admin -d JobNecto
```

Then inspect schema/data (`\dt`, simple `SELECT`).

### Related skill

For deeper DB seeding/queries/migrations details, use:

- `.cursor/skills/jobnecto-postgresql/SKILL.md`

## 4) Config, env vars, and "feature flags"

There is no dedicated feature-flag framework (for example `FeatureManagement` or LaunchDarkly) in this codebase yet.

### How to handle flag-like tasks today

1. Check whether the behavior is controlled by config/environment variables.
2. Override config in Cloud with env vars when possible:
   - `ASPNETCORE_ENVIRONMENT=Development`
   - `DOTNET_URLS="http://localhost:5000"`
   - `ConnectionStrings__Postgres="Host=...;Port=...;Database=...;Username=...;Password=..."`
3. If no flag exists, mock behavior in tests (or add a minimal config toggle in code if requested by task).

Rule of thumb: do not invent a feature-flag system unless the task explicitly asks for one.

## 5) CI-parity workflow (pre-PR confidence)

Run from repo root:

```bash
dotnet build backend/JobNecto.slnx --configuration Release --warnaserror
dotnet test backend/JobNecto.slnx --configuration Release --no-build --warnaserror
```

Use this before finalizing changes that affect compilation, warnings, or tests.

## 6) Common Cloud gotchas

- Do not use root `Jobnecto.sln`; use `backend/JobNecto.slnx`.
- Docker files are placeholders; do not spend time on dockerized runs unless task explicitly asks.
- Redis/Quartz packages exist but are not functionally wired yet.
- Infrastructure DI exists, but API startup may not wire all infrastructure paths yet; verify `Program.cs` before assuming runtime behavior.

## 7) How to update this skill when new runbook knowledge appears

When you discover a reliable new trick, add it immediately in the relevant section.

Update rules:

1. Prefer copy-paste commands that were proven in Cloud.
2. Add expected output/result for each new workflow.
3. Keep entries scoped by codebase area (API, App/Domain, Infrastructure/DB, CI).
4. If a step is flaky or environment-dependent, label it as such and include fallback steps.
5. Keep this skill minimal: move deep DB details to `jobnecto-postgresql` and link to it.

Recommended update format:

- **Context:** what changed (for example, new endpoint pattern).
- **Command(s):** exact command(s) that worked.
- **Expected result:** status code, log line, or test outcome.
- **When to use:** the trigger condition for future agents.
