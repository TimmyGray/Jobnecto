---
project_name: 'Jobnecto'
user_name: 'Timmy'
date: '2026-04-18T00:00:00Z'
sections_completed:
  - technology_stack
  - language_rules
  - framework_rules
  - testing_rules
  - quality_rules
  - workflow_rules
  - anti_patterns
status: complete
rule_count: 32
optimized_for_llm: true
existing_patterns_found: 5
---

# Project Context for AI Agents

_This file contains critical rules and patterns that AI agents must follow when implementing code in this project. Focus on unobvious details that agents might otherwise miss._

---

## Technology Stack & Versions

| Area | Choice |
|------|--------|
| Runtime | .NET **10** (`net10.0`), **nullable** + **implicit usings** on all projects |
| Solution | **`backend/JobNecto.slnx`** for every `dotnet build` / `dotnet test` / CI parity |
| API | ASP.NET Core **10.0.3** (OpenAPI package), Swashbuckle **10.1.4**, Serilog **10** |
| Application | **MediatR** **14.0.0**, **FluentValidation** **12.1.1** (+ DI extensions) |
| Data | **EF Core** **10.0.3**, **Npgsql** provider **10.0.0**; dev connection string: Npgsql **`Key=Value`** pairs, include **`Port=5432`** |
| Infra extras | **StackExchange.Redis** **2.11.3**, **Quartz** **3.15.1** — referenced; **no full implementation yet** |
| Tests | **xUnit** **2.9.3**, **FluentAssertions** **8.9.0**, **Moq** **4.20.72**, **EFCore.InMemory** **10.0.5**, coverlet |
| CI | `dotnet-version: 10.0.x`; Release **`dotnet build` / `dotnet test`** with **`--warnaserror`** |

**Layers:** API → Application → Domain; **Infrastructure** (+ **Infrastructure.LLM**, **Infrastructure.JobSources**) implement persistence and integrations. **Do not** reference Infrastructure from Domain or invert dependencies.

**Repo layout:** the .NET backend lives in **`backend/`**. **All frontend code MUST live in the repo-root `frontend/` folder** (Angular SPA workspace root: `frontend/angular.json`, `frontend/package.json`, `frontend/src/…`, feature-sliced per FE Guide §2.2). Never scatter client code under `backend/`, the repo root, or elsewhere. Preserve `frontend/design-examples/` (non-binding visual references). The Demo MVP / Phase D frontend stack is **Angular** (standalone components, Signals + services — no NgRx, HttpClient + thin per-entity cache — no TanStack, typed Reactive Forms, spartan-ng on CDK themed by tokens); the React recommendations in `docs/FRONTEND_IMPLEMENTATION_GUIDE.md` are superseded by `architecture/demo-mvp-architecture-decisions.md` Decision 1.

---

## Critical Implementation Rules

### Language-specific (C# / .NET)

- Treat **nullable reference types** as enforced: avoid suppressing warnings without a documented reason.
- Prefer **`async`/`await`** end-to-end for I/O; use `CancellationToken` parameters on public async APIs where appropriate.
- Rely on **implicit usings**; add explicit usings only when needed — tests project already includes global `Xunit`.
- Use **UTC** for persisted timestamps where the domain uses `DateTime` (match existing entities).

### Framework-specific (ASP.NET Core, EF Core, Clean Architecture)

- **OpenAPI** is served at **`GET /openapi/v1.json`**. Root URL **404** is expected until user-facing routes exist.
- **Application** handlers/validators live in **Application**; **API** maps HTTP to application entry points only.
- **EF Core**: migrations and `DbContext` stay in **Infrastructure**; Application/Domain stay persistence-ignorant except via abstractions already in the design.
- **`Program.cs`** wires **`AddInfrastructure()`** and **`AddJwtAuthentication()`** in the API host. For tests, DB wiring/connection scope can be overridden by the test host.
- When adding Redis/Quartz behavior, align with packages already referenced — avoid duplicate client/scheduler abstractions unless introducing a deliberate replacement.

### Current implementation snapshot (2026-05-25)

- Merged stories: `1-1` through `1-5`, `2-1` through `2-8`, `r-1`, `3-1` through `3-5`, `4-1` through `4-3`, `5-1` through `5-5`, `r-2` through `r-5`. Epic 5 (cover letter application management) merged 2026-05-11; Epic R (authorization & ownership hardening) merged 2026-05-25.
- Phase C (Security) is complete: authentication, password hashing, and consistent ownership enforcement are closed. Every authenticated endpoint follows the canonical cross-user contract (404 on detail reads, 403 on mutations, 404 on body-supplied foreign keys), codified in `_bmad-output/planning-artifacts/architecture/authorization-contract-matrix.md`, audited in `_bmad-output/planning-artifacts/architecture/endpoint-ownership-audit.md`, and regression-guarded by `backend/tests/JobNecto.Tests/API/Authorization/`. Phase D (Ingestion and LLM) is cleared to start.
- Active HTTP endpoints: `POST /api/v1/users`, `POST /api/v1/users/token/refresh`, `GET /api/v1/users/me`, `PATCH /api/v1/users/me`, `POST|PATCH|DELETE /api/v1/users/me/avatar`, `POST /api/v1/resumes`, `GET /api/v1/resumes`, `GET /api/v1/resumes/{id}`, `PATCH /api/v1/resumes/{id}`, `DELETE /api/v1/resumes/{id}`, `POST /api/v1/educations`, `GET /api/v1/educations`, `GET /api/v1/educations/{id}`, `PATCH /api/v1/educations/{id}`, `DELETE /api/v1/educations/{id}`, `POST /api/v1/cover-letter-templates`, `GET /api/v1/cover-letter-templates`, `GET /api/v1/cover-letter-templates/{id}`, `PATCH /api/v1/cover-letter-templates/{id}`, `DELETE /api/v1/cover-letter-templates/{id}`, `POST /api/v1/vacancies/filter`, `GET /api/v1/vacancies/{id}`, `POST /api/v1/cover-letters`, `GET /api/v1/cover-letters`, `GET /api/v1/cover-letters/{id}`, `PATCH /api/v1/cover-letters/{id}`, `DELETE /api/v1/cover-letters/{id}`.
- Password storage uses PBKDF2 (`Pbkdf2PasswordHasher`) behind `IPasswordHasher`.
- Auth transport policy: browser flows rely on HTTP-only cookie transport; bearer clients can use response-body token on refresh.
- Education records support degree types: `bachelor`, `master`, `phd`, `postdoc`, `other`.

### Artifact lifecycle and archive policy

- Keep active work artifacts only in `_bmad-output/implementation-artifacts/` and `_bmad-output/planning-artifacts/epics/`.
- Keep historical completed artifacts in archive folders:
  - `_bmad-output/archive/implementation-artifacts/`
  - `_bmad-output/archive/planning-artifacts/epics/`
- When a story is marked `done`, move its story file to `_bmad-output/archive/implementation-artifacts/`.
- When an epic is marked `done`, move its completed epic spec to `_bmad-output/archive/planning-artifacts/epics/` and move that epic's retrospective/sprint-plan artifacts to `_bmad-output/archive/implementation-artifacts/`.
- Keep `_bmad-output/implementation-artifacts/sprint-status.yaml` active (never archived).
- After moves, rewrite all references to direct archive paths; do not leave compatibility stubs.

### Testing rules

- Use **xUnit** `[Fact]` / `[Theory]`; assertions with **FluentAssertions**; mocks with **Moq** when isolating application services.
- Infrastructure/repository tests: **`UseInMemoryDatabase`** with a **unique database name per test** (e.g. `Guid.NewGuid()`), **`await using`** context where applicable.
- Test project references **API + Application + Domain + Infrastructure** (+ JobSources/LLM) as needed — follow existing **`JobNecto.Tests.csproj`** pattern.
- After substantive changes, run **`dotnet test backend/JobNecto.slnx`**; for CI parity on risky edits use **Release** + **`--warnaserror`** (see workflow rules).

### Namespace conventions

- **Namespaces must mirror the folder structure.** Every C# file's `namespace` declaration must start with the project root namespace (e.g. `JobNecto.API`, `JobNecto.Application`, `JobNecto.Domain`, `JobNecto.Infrastructure`) and append each subfolder as a segment separated by `.`.
- **Example:** `backend/src/JobNecto.API/Infrastructure/Cors/CorsServiceExtensions.cs` → `namespace JobNecto.API.Infrastructure.Cors;`
- This rule is **mandatory** — all agents must strictly follow it and never use a flat or mismatched namespace.

### Code quality and style

- No repo **`.editorconfig`** found — follow **IDE defaults** and match **existing file naming** in each folder (note: **`UserRepositorytests.cs`** exists; prefer consistent `*Tests.cs` for **new** files unless matching a local convention).
- Keep changes **scoped** to the task; avoid drive-by refactors in unrelated areas.
- Match **existing patterns** in the same layer (repositories, entities, handlers) before introducing new abstractions.

### Development workflow

- **PR reviews**: use a dedicated **Code reviewer** subagent when a PR is opened/updated or when review is requested; scope includes correctness, regression risk, quality, optimization, tests, and **running** `dotnet test backend/JobNecto.slnx` (and Release/warn-as-error when appropriate).
- **Review output**: prioritized findings with **severity**, **risk_score (1–10)**, **impact**, **evidence**, **recommended_fix**; state explicitly when there are no findings.

### Critical don’t-miss rules

- **Never** use root **`Jobnecto.sln`** for build/test — it uses Windows path style and **omits the test project**. Always **`backend/JobNecto.slnx`**.
- **PostgreSQL** dev settings: **`Host=localhost;Port=5432;Database=JobNecto;Username=admin;Password=admin`** (adjust only via config, not hardcoding in app code).
- **`docker/Dockerfile`** and **`docker/docker-compose.yml`** are **placeholders** — do not assume containerized workflows are ready.
- Infrastructure packages (**Redis**, **Quartz**) are **not fully implemented** — do not build features that silently assume they work without wiring.

---

## Usage guidelines

### For AI agents

- Read this file (and root **`AGENTS.md`**) before implementing changes.
- Follow rules here exactly; when unclear, prefer the **stricter** option and the **documented** solution entrypoint.
- Update this file when stack or conventions change.

### For humans

- Keep this file **lean** and agent-focused; prefer **`AGENTS.md`** for long-form contributor docs if content overlaps.
- Refresh when **TargetFramework**, CI, or layer boundaries change; remove rules that become universally obvious.

Last updated: 2026-05-27 (archive lifecycle policy added; active and historical artifact paths clarified)

---
