# JobNecto — backend product roadmap (adapted to this repository)

This document replaces a generic “Create Next App” / frontend-oriented draft. **JobNecto** is a **.NET 10** backend for job vacancy aggregation, user profiles, matching, and LLM-assisted cover letters. Solution entry point: `backend/JobNecto.slnx`.

## Vision

- Aggregate and store **vacancies** from multiple **job sources** (`JobSource` value object: name + optional URL; stored as **jsonb** for vacancies).
- Let users maintain **profiles** (`User`), **education** (`Education`), **resume templates** (`Resume`), and generated **cover letters** (`CoverLetter`).
- Support **filtering / pagination** (`PagedQuery`, `PagedResult`) and future **matching** (e.g. `Vacancy.MatchScore`).
- Optional **LLM** integration (`Infrastructure.LLM`, `LlmProvider` enum, `LlmProviderConfig`).

## Architecture (as implemented)

| Layer | Project | Role |
|-------|---------|------|
| API | `JobNecto.API` | ASP.NET Core host, OpenAPI in Development (`/openapi/v1.json`). **No REST resources yet.** |
| Application | `JobNecto.Application` | Repository interfaces (`IRepository`, `IEditableRepository`, `IUnitOfWork`, entity-specific repos). |
| Domain | `JobNecto.Domain` | Entities, enums, value objects. |
| Infrastructure | `JobNecto.Infrastructure` | EF Core `AppDbContext`, configurations, repositories, `AddInfrastructure()`. **No migrations in repo yet.** |
| Job sources (stub) | `JobNecto.Infrastructure.JobSources` | Placeholder for ingestion. |
| LLM (stub) | `JobNecto.Infrastructure.LLM` | Placeholder for generation. |

## Domain model (canonical names and relationships)

- **`User`** (`Users`): login, password, email, phone, location, skills, languages, educations, resumes, cover letters, profile fields (name, age, about, certificates, projects, avatar).
- **`Resume`** (`Resumes`): per-user template for filtering/matching and LLM context — salary, currency, skills, work location type, experience level, projects, certifications, languages, educations (M:N via **`ResumeEducations`**), locations, excluded words.
- **`Education`** (`Educations`): title, specialization, degree; linked to user and optionally to resumes.
- **`Vacancy`** (`Vacancies`): owned by `UserId`; title, description, company, URLs, location, work time/location types, categories, skills, salary range, currency, match score, experience level, **`JobSource`** (required), `IsChosen`, `IsHidden`.
- **`CoverLetter`** (`CoverLetters`): `UserId`, `VacancyId`, `Content`.

Shared: **`BaseEntity`** — `Id`, `CreatedAt`, `UpdatedAt`.

## Current implementation status

- **Done in code:** domain types, EF configurations, `AppDbContext`, base repository, `UserRepository`, `VacancyRepository` (including `GetFilteredAsync`), infrastructure DI extension (`AddInfrastructure`).
- **Incomplete:** `UnitOfWork` throws for `SaveChangesAsync`, `DisposeAsync`, transactions, and most repository getters; `VacancyRepository.UpdateMatchScoreAsync` is not implemented. There are no `Resume` / `Education` / `CoverLetter` repository classes yet.
- **Not wired:** `Program.cs` does not call `AddInfrastructure()`; **no EF migrations** committed; no HTTP APIs beyond OpenAPI scaffold; auth, matching, LLM, and job-source ingestion are not implemented.

## Delivery themes (for issue breakdown)

1. **Host & persistence** — Wire DI, migrations, unit of work / save behavior.
2. **HTTP API** — Versioned REST (or minimal APIs) for each aggregate with validation and OpenAPI updates.
3. **Security** — Password hashing, authentication, authorization policies.
4. **Matching & search** — Query vacancies by resume criteria; update `MatchScore` where applicable.
5. **Integrations** — LLM cover-letter generation; external job source adapters.
6. **Quality** — Integration tests, CI alignment, observability (logging/metrics) as needed.

## Note on the external Merlin roadmap link

The shared URL returned no substantive content in an automated fetch (placeholder title only). This file is the **source of truth** for backend naming and scope aligned with the repository.
