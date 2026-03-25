# JobNecto — backend roadmap (adapted from Merlin “JobLens AI” plan)

The Merlin dashboard described **JobLens AI** with projects `JobLens.*`, entities like `UserProfile` and `VacancyAnalysis`, and a specific REST surface. **This repository is JobNecto** (`backend/JobNecto.slnx`, projects `JobNecto.*`). This document maps the Merlin plan to what exists today and defines **execution order from the start** (phases, not calendar estimates).

---

## 1. Product and solution naming

| Merlin (source) | This repository |
|-----------------|-----------------|
| JobLens AI | **JobNecto** |
| `JobLens.sln` / `src/JobLens.*` | `backend/JobNecto.slnx` / `backend/src/JobNecto.*` |
| `JobLens.API` | `JobNecto.API` |
| `JobLens.Application` | `JobNecto.Application` |
| `JobLens.Domain` | `JobNecto.Domain` |
| `JobLens.Infrastructure` | `JobNecto.Infrastructure` |
| `JobLens.Infrastructure.LLM` | `JobNecto.Infrastructure.LLM` |
| `JobLens.Infrastructure.JobSources` | `JobNecto.Infrastructure.JobSources` |

---

## 2. Architecture layers (concept unchanged)

Dependencies still flow **inward**: API → Application → Domain ← Infrastructure.

| Layer | Project | Today |
|-------|---------|--------|
| API | `JobNecto.API` | OpenAPI/Swashbuckle, Serilog packages present; **no REST resources**; **`Program.cs` does not call `AddInfrastructure()`**. |
| Application | `JobNecto.Application` | **MediatR** and **FluentValidation** referenced; **only repository interfaces** today — no `Commands/` / `Queries/` / `Behaviors/` folders yet. |
| Domain | `JobNecto.Domain` | Entities, enums, value objects (`VacancyFilter`, `JobSource`, pagination types). **No `VacancyAnalysis` entity**; **no domain events** wired. |
| Infrastructure | `JobNecto.Infrastructure` | EF Core + Npgsql, Redis + Quartz **packages**; `AppDbContext`, configurations, repositories; **`UnitOfWork` largely unimplemented**; **no committed migrations**. |
| LLM | `JobNecto.Infrastructure.LLM` | Stub project. |
| Job sources | `JobNecto.Infrastructure.JobSources` | Stub project (Merlin’s hh.ru / Arbeitnow clients not implemented). |

---

## 3. Domain entities — Merlin vs JobNecto

Merlin showed five conceptual models. The codebase models **profile + resume + education** explicitly instead of a single `UserProfile` with `ResumeText`.

| Merlin entity | JobNecto equivalent | Notes |
|---------------|---------------------|--------|
| **Vacancy** | `Vacancy` | Uses value object **`JobSource`** (`Name`, `Url?`) stored as **jsonb**, not separate `Source` / `SourceUrl` strings. Extra fields: `CompanyWebsite`, `WorkTimeType`, `WorkLocationType`, `JobCategories`, `Currency`, `IsChosen`, `IsHidden`, `UserId`. **No `SyncedAt`** — use `UpdatedAt` or add a field later. |
| **UserProfile** | **`User`** + **`Resume`** + **`Education`** | `User` holds identity and profile (login, email, skills, languages, etc.). **`Resume`** is the template for filtering, matching, and LLM context (salary, locations, excluded words, M:N to educations via **`ResumeEducations`**). |
| **LlmProviderConfig** | `LlmProviderConfig` (class, not `BaseEntity`) | Fields differ: we have `LlmProvider` enum, `ApiKey`, `BaseUrl`, `Model`, `Temperature?`. **No** `IsLocal`, `MaxTokens`, or separate `EndpointUrl` / `ModelName` — align naming when implementing config storage. |
| **CoverLetter** | `CoverLetter` | Extends **`BaseEntity`**: `Id`, `CreatedAt`, `UpdatedAt` + `UserId`, `VacancyId`, `Content`. **No** `ModelUsed`, `ProviderUsed`, `GeneratedAt` — add if product needs audit metadata. |
| **VacancyAnalysis** | *Not present* | Merlin’s persisted analysis (strengths, weaknesses, recommendation) **does not exist** as an entity. Options: add `VacancyAnalysis` (or embed JSON on `Vacancy`), or treat analysis as ephemeral until schema is defined. `Vacancy.MatchScore` exists for sorting/filtering. |

---

## 4. REST API — Merlin routes vs JobNecto target shape

Merlin listed authenticated JSON endpoints under `/api/...`. For JobNecto, pick a **version prefix** (e.g. `/api/v1/...`) and implement incrementally.

| Merlin | JobNecto direction |
|--------|-------------------|
| `GET /api/vacancies` (filters, pagination, match score) | Back with **`IVacancyRepository.GetFilteredAsync`** + `PagedQuery` / `VacancyFilter`; add auth when JWT exists. |
| `GET /api/vacancies/{id}` | Single vacancy by id (+ optional analysis payload if you add `VacancyAnalysis`). |
| `POST /api/vacancies/sync` | Orchestrates **JobSources** adapters + persistence; depends on ingestion design. |
| `POST /api/vacancies/{id}/analyze` | LLM pipeline; may create/update **`MatchScore`** and/or future **`VacancyAnalysis`**. |
| `POST /api/vacancies/{id}/cover-letter` | LLM + **`CoverLetter`** persistence. |
| `GET/PUT /api/profile` | Map to **`User`** (+ related **`Resume`** / **`Education`** — either nested resource or separate endpoints). |
| `PUT /api/profile/llm-config` | Persist **`LlmProviderConfig`** per user (may require new entity/table or user JSON column — **design task**). |
| `GET/POST /api/sources` | Job source registry + sync metadata (not in domain yet beyond `JobSource` on vacancy). |

---

## 5. Tech stack — Merlin vs repo

| Area | Merlin | JobNecto today |
|------|--------|----------------|
| Runtime | .NET 10 | **net10.0** |
| API docs | Swagger/OpenAPI | **Microsoft.AspNetCore.OpenApi** + **Swashbuckle** |
| ORM | EF Core 10 | **EF Core 10** + **Npgsql** |
| DB | PostgreSQL 17 | **PostgreSQL** (version is environment-specific) |
| CQRS | MediatR v12 | **MediatR 14** — referenced; **handlers not added** |
| Validation | FluentValidation | **FluentValidation 12** — referenced; **not wired in DI/pipeline** |
| Mapping | Mapster | **Not referenced** (manual mapping or add later) |
| AI | Semantic Kernel + MEAI | **Not in csproj** — add when implementing LLM router |
| Redis | StackExchange.Redis | **Package present**; **no cache service** |
| Quartz | Quartz.NET | **Packages present**; **no jobs** |
| PDF | PdfPig | **Not referenced** |
| Logging | Serilog (+ Seq in Merlin) | **Serilog** in API (console/file) |
| Observability | OpenTelemetry, Prometheus | **Not referenced** |
| Tests | xUnit, Moq, FluentAssertions, Testcontainers | **xUnit** project exists; **minimal tests** |

Docker: repo `docker/` files are **placeholders** — Merlin’s compose stack is **not** implemented.

---

## 6. Execution order from the start (phased)

Work through these in order; each phase unlocks the next.

### Phase A — Foundation (persistence works)

1. Register infrastructure: **`AddInfrastructure()`** in `Program.cs` (and connection string validation).
2. **Initial EF Core migration** + `dotnet ef database update` documented in CI/agents.
3. Complete **`UnitOfWork`**: `SaveChangesAsync`, `DisposeAsync`, repository getters, transactions.
4. Add missing **repositories** for `Resume`, `Education`, `CoverLetter` if not using generic access only.
5. Implement **`VacancyRepository.UpdateMatchScoreAsync`**.

### Phase B — HTTP core (read/write without LLM)

6. Introduce **API versioning** and first **Minimal APIs or controllers**.
7. **Users** CRUD (validation aligned with EF constraints: email, phone E.164, age).
8. **Vacancies** list (filters + cursor paging) and single-resource CRUD.
9. **Resumes** + **Educations** + **Cover letters** CRUD and relationships.

### Phase C — Security

10. **Password hashing** (no plaintext).
11. **JWT** (or chosen auth) + protected routes.
12. **Authorization** (user owns their rows).

### Phase D — Intelligence and ingestion

13. **Job source** abstraction + first adapter (Merlin: hh.ru; implement whichever API you license first).
14. **Sync** endpoint or background trigger persisting vacancies with **`JobSource`**.
15. **LLM**: add packages (Semantic Kernel / MEAI as chosen), **`ILlmRouterService`**, config from storage.
16. **Analyze** endpoint (define whether to add **`VacancyAnalysis`** entity or return transient DTOs).
17. **Cover letter** generation + persist **`CoverLetter`** (optionally extend with model/provider metadata).

### Phase E — Production hardening

18. **Quartz** scheduled sync jobs.
19. **Redis** caching where it helps (rate limiting, vacancy lists, etc.).
20. **Rate limiting**, **CORS**, **health/ready**, **global exception → problem details**.
21. **Integration tests** (Testcontainers.PostgreSql) + CI parity.
22. **Optional**: PdfPig resume upload, OpenTelemetry, architecture tests (NetArchTest).

---

## 7. Tracking

- GitHub issues **#16–#37** in this repository map to chunks of Phases A–E (small, testable).
- This file is the **narrative** mapping from the Merlin **JobLens** document to **JobNecto** naming and schema.

---

## 8. Vision (unchanged in intent)

- Aggregate **vacancies** from multiple **sources** with structured **`JobSource`**.
- Users maintain **profile**, **education**, **resume templates**, and **cover letters**.
- **Filter and paginate** vacancies; compute or refresh **`MatchScore`** for matching.
- **LLM** for analysis and cover letters, with configurable providers aligned to **`LlmProvider`** / **`LlmProviderConfig`**.
