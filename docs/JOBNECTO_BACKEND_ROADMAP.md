# JobNecto — backend roadmap

**JobNecto** is a **.NET 10** backend for job vacancy aggregation, user profiles, matching, and LLM-assisted cover letters. Solution entry point: `backend/JobNecto.slnx`.

## Vision

- Aggregate and store **vacancies** from multiple **job sources** (`JobSource` value object: `Name`, optional `Url`; stored as **jsonb** on vacancies).
- Users maintain **profiles** (`User`), **education** (`Education`), **resume templates** (`Resume`), and **cover letters** (`CoverLetter`).
- **Filter and paginate** vacancies (`VacancyFilter`, `PagedQuery`, `PagedResult`); support **matching** via `Vacancy.MatchScore` (and optional future persisted analysis if the team adds it).
- **LLM** integration via `JobNecto.Infrastructure.LLM`, `LlmProvider` enum, and `LlmProviderConfig`.

## Implementation snapshot (2026-04-23)

- Story `1-1` (global exception handling), `1-2` (create user account), and `1-5` (password hashing and token policy hardening) are merged to `master`.
- Authentication baseline is live for user onboarding: `POST /api/v1/users` creates users and issues secure auth cookies; `POST /api/v1/users/token/refresh` renews JWTs for authenticated clients.
- Password persistence now uses PBKDF2 (`pbkdf2-sha256`) via `IPasswordHasher` and `Pbkdf2PasswordHasher`, with test coverage for malformed hash formats.
- CI and PR review automation are active on merge and PR events (`CI` + `PR review (LLM via OpenRouter)`).

## Solution layout

| Project | Path |
|---------|------|
| `JobNecto.API` | `backend/src/JobNecto.API` |
| `JobNecto.Application` | `backend/src/JobNecto.Application` |
| `JobNecto.Domain` | `backend/src/JobNecto.Domain` |
| `JobNecto.Infrastructure` | `backend/src/JobNecto.Infrastructure` |
| `JobNecto.Infrastructure.LLM` | `backend/src/JobNecto.Infrastructure.LLM` |
| `JobNecto.Infrastructure.JobSources` | `backend/src/JobNecto.Infrastructure.JobSources` |
| `JobNecto.Tests` | `backend/tests/JobNecto.Tests` |

## Architecture

Dependencies flow **inward**: API → Application → Domain ← Infrastructure.

| Layer | Project | Status |
|-------|---------|--------|
| API | `JobNecto.API` | OpenAPI/Swashbuckle, Serilog, and active user/auth endpoints (`POST /api/v1/users`, `POST /api/v1/users/token/refresh`); `Program.cs` wires infrastructure, JWT auth, CORS, and global exception handling. |
| Application | `JobNecto.Application` | MediatR handlers and FluentValidation pipeline are active for user creation; repository abstractions and password hasher contract are in use. |
| Domain | `JobNecto.Domain` | Entities, enums, value objects. No separate “vacancy analysis” aggregate today; **`Vacancy.MatchScore`** supports filtering/sorting. Domain events not wired. |
| Infrastructure | `JobNecto.Infrastructure` | EF Core + Npgsql wired through DI; repositories + `UnitOfWork` transaction API implemented; committed migrations include password hash length hardening. |
| LLM | `JobNecto.Infrastructure.LLM` | Stub. |
| Job sources | `JobNecto.Infrastructure.JobSources` | Stub (external API clients not implemented). |

## Domain model

Shared: **`BaseEntity`** — `Id`, `CreatedAt`, `UpdatedAt`.

| Entity | Role |
|--------|------|
| **`User`** | Identity and profile: login, password, email, phone, location, skills, languages, about, certificates, projects, avatar; navigations to educations, resumes, cover letters. |
| **`Resume`** | Template for filtering, matching, and LLM context: salary, currency, skills, work location type, experience, projects, certifications, languages, locations, excluded words; M:N to **`Education`** via **`ResumeEducations`**. |
| **`Education`** | Title, specialization, degree; belongs to user; linkable to resumes. |
| **`Vacancy`** | Belongs to `UserId`; title, description, company, company website, location, work time/location types, categories, skills, salary range, currency, `MatchScore`, experience level, required **`JobSource`**, `IsChosen`, `IsHidden`. |
| **`CoverLetter`** | `UserId`, `VacancyId`, `Content` (+ base audit fields). |
| **`LlmProviderConfig`** | Not an EF entity today: `LlmProvider`, `ApiKey`, `BaseUrl`, `Model`, `Temperature?` — persistence strategy TBD when LLM config is stored per user. |

## Planned HTTP API

Use a **version prefix** (e.g. `/api/v1/...`) and add auth where noted below.

| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/api/v1/vacancies` | List with `VacancyFilter` + cursor pagination (`GetFilteredAsync`). |
| GET | `/api/v1/vacancies/{id}` | Single vacancy. |
| POST | `/api/v1/vacancies` / PUT / DELETE | Vacancy CRUD (ownership rules with auth). |
| POST | `/api/v1/vacancies/sync` | Trigger ingestion from configured job sources. |
| POST | `/api/v1/vacancies/{id}/analyze` | LLM analysis; update `MatchScore` and/or return analysis DTO (persist only if schema is added). |
| POST | `/api/v1/vacancies/{id}/cover-letter` | Generate and persist `CoverLetter`. |
| GET, PUT | `/api/v1/users/me` or `/api/v1/profile` | Current user profile; align with `User` + related `Resume`/`Education` (nested or separate resources). |
| PUT | `/api/v1/users/me/llm-config` | Store LLM settings (design storage first). |
| GET, POST | `/api/v1/sources` | List and register job sources / sync metadata (beyond `JobSource` on each vacancy). |

## Current HTTP API (implemented)

| Method | Endpoint | Purpose |
|--------|----------|---------|
| POST | `/api/v1/users` | Register user, persist password hash, return `201 Created`, and issue HTTP-only auth cookie. |
| POST | `/api/v1/users/token/refresh` | Refresh JWT for authenticated clients; always renew cookie and return body token only for bearer transport clients. |

## Tech stack

| Area | In repo | Notes |
|------|---------|--------|
| Runtime | .NET 10 | `net10.0` |
| API | ASP.NET Core, OpenAPI, Swashbuckle | |
| Data | EF Core 10, Npgsql | Migrations expected under Infrastructure |
| Optional packages | Redis, Quartz | Present; not functionally wired |
| App patterns | MediatR, FluentValidation | Add DI registration and handlers when adopting CQRS |
| Logging | Serilog (API) | Console/file |
| LLM | — | Add chosen packages when implementing router |
| PDF upload | — | Optional later (e.g. PdfPig) |
| Observability | — | Optional: OpenTelemetry, Seq, etc. |
| Tests | xUnit | Expand with integration tests (e.g. Testcontainers) |

`docker/` in the repo is currently placeholder.

## Phased delivery

### Phase A — Foundation (persistence)

1. [done] Call **`AddInfrastructure()`** from `Program.cs` (validate connection string).
2. [done] **Initial EF Core migration** and documented `database update` flow.
3. [done] Complete **`UnitOfWork`**: `SaveChangesAsync`, `DisposeAsync`, repository getters, transactions.
4. [done] Repositories for **`Resume`**, **`Education`**, **`CoverLetter`** as needed.
5. [done] Implement **`VacancyRepository.UpdateMatchScoreAsync`**.

### Phase B — HTTP core

6. [done] API versioning and first endpoints (controllers under `/api/v1`).
7. [in-progress] **Users** CRUD with validation (create endpoint implemented; remaining profile endpoints pending).
8. [backlog] **Vacancies** list + CRUD.
9. [backlog] **Resumes**, **educations**, **cover letters** CRUD and relationships.

### Phase C — Security

10. [done] Password hashing.
11. [done] JWT (or chosen scheme) and protected routes.
12. [in-progress] Authorization: users mutate only their data.

### Phase D — Ingestion and LLM

13. Job source **abstraction** + first **adapter** (start with manual/static feed or first external API you adopt).
14. **Sync** endpoint or scheduled job writing vacancies with valid **`JobSource`**.
15. LLM router + config storage; **analyze** and **cover letter** endpoints.

### Phase E — Hardening

16. Quartz jobs, Redis where useful, rate limiting, CORS, health/ready, problem details.
17. Integration tests and CI parity; optional PDF pipeline, OpenTelemetry, architecture tests.

## Tracking

Work is broken into small GitHub issues **#16–#37** (foundation through hardening).
Story **1-5 password hashing and token policy hardening** merged on **2026-04-23**.
