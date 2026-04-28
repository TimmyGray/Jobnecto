# JobNecto — backend roadmap

**JobNecto** is a **.NET 10** backend for job vacancy aggregation, user profiles, matching, and LLM-assisted cover letters. Solution entry point: `backend/JobNecto.slnx`.

## Vision

- Aggregate and store **vacancies** from multiple **job sources** (`JobSource` value object: `Name`, optional `Url`; stored as **jsonb** on vacancies).
- Users maintain **profiles** (`User`), **education** (`Education`), **resume templates** (`Resume`), and **cover letters** (`CoverLetter`).
- **Filter and paginate** vacancies (`VacancyFilter`, `PagedQuery`, `PagedResult`); support **matching** via `Vacancy.MatchScore` (and optional future persisted analysis if the team adds it).
- **LLM** integration via `JobNecto.Infrastructure.LLM`, `LlmProvider` enum, and `LlmProviderConfig`.

## Implementation snapshot (2026-04-28)

- Stories `1-1` (global exception handling), `1-2` (create user account), `1-3` (retrieve current user profile), `1-4` (update user profile + avatar management), `1-5` (password hashing and token policy hardening), `2-1` (create resume), `2-2` (list resumes), and `2-3` (get resume detail) are merged to `master`.
- Authentication baseline is live: `POST /api/v1/users` creates users; `POST /api/v1/users/token/refresh` renews JWTs; `GET /api/v1/users/me` returns the core profile (id, loginName, email, phone, location, about, avatar, timestamps). Story 1.4 adds `PATCH /api/v1/users/me` for partial profile updates and avatar endpoints (`POST|PUT|DELETE /api/v1/users/me/avatar`). Story 2.1 adds `POST /api/v1/resumes` for authenticated resume creation, and Story 2.2 adds `GET /api/v1/resumes` with cursor pagination.
- Password persistence uses PBKDF2 (`pbkdf2-sha256`) via `IPasswordHasher` and `Pbkdf2PasswordHasher`, with test coverage for malformed hash formats.
- CI and PR review automation are active on merge and PR events (`CI` + `PR review (LLM via OpenRouter)`).
- Repository layer supports UserId-scoped filtering and cursor-based pagination (BaseRepository); ownership filtering is enforced for all user-scoped resources.
- Product direction: resumes, educations, templates, and cover letters are exposed through separate user-scoped routes with mandatory ownership checks. Resume creation now follows the optional-field contract documented in Story 2.1.

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
| API | `JobNecto.API` | OpenAPI/Swashbuckle, Serilog, and active user/auth/resume endpoints (`POST /api/v1/users`, `POST /api/v1/users/token/refresh`, `POST /api/v1/resumes`, `GET /api/v1/resumes`); `Program.cs` wires infrastructure, JWT auth, CORS, and global exception handling. |
| Application | `JobNecto.Application` | MediatR handlers and FluentValidation pipeline are active for user creation and resume creation; repository abstractions and password hasher contract are in use. |
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
| GET, PATCH | `/api/v1/users/me` | Current user core profile only (`id`, `loginName`, `email`, `phone`, `location`, `about`, `avatar`, timestamps). |
| GET, POST | `/api/v1/resumes` | User-scoped resume list/create for the authenticated user only. |
| GET, PATCH, DELETE | `/api/v1/resumes/{id}` | User-scoped resume detail/update/soft delete with ownership checks. |
| GET, POST | `/api/v1/educations` | User-scoped education list/create for the authenticated user only. |
| GET, PATCH, DELETE | `/api/v1/educations/{id}` | User-scoped education detail/update/soft delete with ownership checks. |
| GET, POST | `/api/v1/cover-letter-templates` | User-scoped template list/create for the authenticated user only. |
| GET, PATCH, DELETE | `/api/v1/cover-letter-templates/{id}` | User-scoped template detail/update/soft delete with ownership checks. |
| GET, POST | `/api/v1/cover-letters` | User-scoped cover letter list/create for the authenticated user only. |
| GET, PATCH, DELETE | `/api/v1/cover-letters/{id}` | User-scoped cover letter detail/update/soft delete with ownership checks. |
| PUT | `/api/v1/users/me/llm-config` | Store LLM settings (design storage first). |
| GET, POST | `/api/v1/sources` | List and register job sources / sync metadata (beyond `JobSource` on each vacancy). |

## Current HTTP API (implemented)

| Method | Endpoint | Purpose |
|--------|----------|---------|
| POST | `/api/v1/users` | Register user, persist password hash, return `201 Created`, and issue HTTP-only auth cookie. |
| POST | `/api/v1/users/token/refresh` | Refresh JWT for authenticated clients; always renew cookie and return body token only for bearer transport clients. |
| GET | `/api/v1/users/me` | Return core profile fields for the authenticated user (id, loginName, email, phone, location, about, avatar, timestamps). Requires valid JWT. |
| POST | `/api/v1/resumes` | Create a resume for the authenticated user and return the created resource with `Location` header. |
| GET | `/api/v1/resumes` | Return a cursor-paginated list of resumes for the authenticated user. |
| GET | `/api/v1/resumes/{id}` | Return the full detail of a specific resume owned by the authenticated user; `404` if not found or not owned. |

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
7. [done] **Users** CRUD with validation (create endpoint implemented; profile update and avatar management implemented in Story 1.4).
8. [backlog] **Vacancies** list + CRUD.
9. [in-progress] **Resumes**, **educations**, **cover letters** CRUD and relationships via user-scoped routes only (no cross-user list endpoints). Resume create/list are merged; remaining detail/update/delete work stays in backlog.

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
Stories **1-4 update user profile and avatar management**, **2-1 create resume**, and **2-2 list resumes** merged on **2026-04-25**, **2026-04-27**, and **2026-04-28** respectively.
