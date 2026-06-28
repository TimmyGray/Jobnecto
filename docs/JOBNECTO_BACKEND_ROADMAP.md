# JobNecto — backend roadmap

**JobNecto** is a **.NET 10** backend for job vacancy aggregation, user profiles, matching, and LLM-assisted cover letters. Solution entry point: `backend/JobNecto.slnx`.

## Vision

- Aggregate and store **vacancies** from multiple **job sources** (`JobSource` value object: `Name`, optional `Url`; stored as **jsonb** on vacancies).
- Users maintain **profiles** (`User`), **education** (`Education`), **resume templates** (`Resume`), and **cover letters** (`CoverLetter`).
- **Filter and paginate** vacancies (`VacancyFilter`, `PagedQuery`, `PagedResult`); support **matching** via `Vacancy.MatchScore` (and optional future persisted analysis if the team adds it).
- **LLM** integration via `JobNecto.Infrastructure.LLM`, `LlmProvider` enum, and `LlmProviderConfig`.

## Implementation snapshot (2026-06-28)

- Stories `1-1` through `1-5`, `2-1` through `2-8`, `r-1`, `3-1` through `3-5`, `4-1` through `4-3`, and `5-1` through `5-5` are all merged to `master`. Epic 5 (cover letter application management) was merged 2026-05-11.
- Authentication baseline is live: `POST /api/v1/users` creates users; `POST /api/v1/users/token/refresh` renews JWTs; `GET /api/v1/users/me` returns the core profile (id, loginName, email, phone, location, about, avatar, timestamps). Story 1.4 adds `PATCH /api/v1/users/me` for partial profile updates and avatar endpoints (`POST|PUT|DELETE /api/v1/users/me/avatar`). Resume create/list/detail/update/delete and education create/list/detail/update/delete endpoints are merged. Epic 3 is complete: cover letter template CRUD with per-user ownership, soft-delete semantics, and DB-backed name uniqueness. Epic 4 is complete: vacancy browse/filter and vacancy detail. Epic 5 is complete: cover letter CRUD (`POST /api/v1/cover-letters`, `GET /api/v1/cover-letters`, `GET /api/v1/cover-letters/{id}`, `PATCH /api/v1/cover-letters/{id}`, `DELETE /api/v1/cover-letters/{id}`) with DB-backed per-user/per-vacancy uniqueness (partial unique index), cursor pagination ordered by `createdAt desc`, nested vacancy fields on detail, soft-delete, and ownership enforcement (404 on reads, 403 on mutations).
- Password persistence uses PBKDF2 (`pbkdf2-sha256`) via `IPasswordHasher` and `Pbkdf2PasswordHasher`, with test coverage for malformed hash formats.
- CI and PR review automation are active on merge and PR events (`CI` + `PR review (LLM via OpenRouter)`).
- Repository layer supports UserId-scoped filtering and cursor-based pagination (BaseRepository); ownership filtering is enforced for all user-scoped resources.
- Product direction: resumes, educations, templates, and cover letters are exposed through separate user-scoped routes with mandatory ownership checks. Resume creation now follows the optional-field contract documented in Story 2.1.
- Epic R (Authorization & Ownership Enforcement Hardening) closed on 2026-05-25. R.2 produced the endpoint ownership audit (`_bmad-output/planning-artifacts/architecture/endpoint-ownership-audit.md`); R.3 added the cross-user HTTP authorization regression suite (`backend/tests/JobNecto.Tests/API/Authorization/`); R.4 published the canonical 403-vs-404 contract matrix (`_bmad-output/planning-artifacts/architecture/authorization-contract-matrix.md`). `dotnet build` and `dotnet test` against `backend/JobNecto.slnx` in Release with `--warnaserror` both clean (0 warnings; 520/520 tests passing). Phase C complete; Phase D cleared to start.
- Demo MVP frontend Story 1.1 (Angular frontend foundation + user registration) merged on 2026-06-28 via PR #80. The repository now includes a production Angular workspace in `frontend/` with typed API contract consumption (`openapi-typescript`), RFC 7807 normalization, sign-up flow integration (`POST /api/v1/users` then `GET /api/v1/users/me`), tokenized UI foundations, and baseline component/unit-test coverage.
- Phase D has started with frontend foundation delivery and remains in-progress for LLM-powered cover-letter generation and ingestion tracks.

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
| POST | `/api/v1/vacancies/filter` | **[implemented — story 4-1]** Browse (empty criteria) or filter (populated criteria) with keyset pagination, `sortBy`, user scoping. |
| GET | `/api/v1/vacancies/{id}` | Single vacancy — story 4-3 (implemented). |
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
| GET, POST | `/api/v1/cover-letters` | **[implemented — Epic 5]** User-scoped cover letter list/create for the authenticated user only. |
| GET, PATCH, DELETE | `/api/v1/cover-letters/{id}` | **[implemented — Epic 5]** User-scoped cover letter detail/update/soft delete with ownership checks. |
| PUT | `/api/v1/users/me/llm-config` | Store LLM settings (design storage first). |
| GET, POST | `/api/v1/sources` | List and register job sources / sync metadata (beyond `JobSource` on each vacancy). |

## Current HTTP API (implemented)

| Method | Endpoint | Purpose |
|--------|----------|---------|
| POST | `/api/v1/users` | Register user, persist password hash, return `201 Created`, and issue HTTP-only auth cookie. |
| POST | `/api/v1/users/token/refresh` | Refresh JWT for authenticated clients; always renew cookie and return body token only for bearer transport clients. |
| GET | `/api/v1/users/me` | Return core profile fields for the authenticated user (id, loginName, email, phone, location, about, avatar, timestamps). Requires valid JWT. |
| PATCH | `/api/v1/users/me` | Update current user profile fields (email, phone, location, about, avatar). |
| POST/PUT/DELETE | `/api/v1/users/me/avatar` | Create, update, or delete user avatar. |
| POST | `/api/v1/resumes` | Create a resume for the authenticated user and return the created resource with `Location` header. |
| GET | `/api/v1/resumes` | Return a cursor-paginated list of resumes for the authenticated user. |
| GET | `/api/v1/resumes/{id}` | Return the full detail of a specific resume owned by the authenticated user; `404` if not found or not owned. |
| PATCH | `/api/v1/resumes/{id}` | Update a resume owned by the authenticated user. |
| DELETE | `/api/v1/resumes/{id}` | Soft-delete a resume owned by the authenticated user. |
| POST | `/api/v1/educations` | Create an education record for the authenticated user and return the created resource with `Location` header. |
| GET | `/api/v1/educations` | Return a cursor-paginated list of education records for the authenticated user. |
| GET | `/api/v1/educations/{id}` | Return a single education record owned by the authenticated user; `404` if not found or not owned. |
| PATCH | `/api/v1/educations/{id}` | Update an education record owned by the authenticated user. |
| DELETE | `/api/v1/educations/{id}` | Soft-delete an education record owned by the authenticated user. |
| POST | `/api/v1/cover-letter-templates` | Create a cover letter template for the authenticated user (name unique per user, content 50–10,000 chars); returns `201 Created` with `Location` header. |
| GET | `/api/v1/cover-letter-templates` | Return a cursor-paginated template library for the authenticated user; supports case-insensitive `search` and returns `contentPreview` only. |
| GET | `/api/v1/cover-letter-templates/{id}` | Return full detail for an owned template, including `content`; return `404` for missing, soft-deleted, or cross-user records. |
| PATCH | `/api/v1/cover-letter-templates/{id}` | Update name/content on an owned template; return `403` for cross-user access, `404` for missing/soft-deleted records, and `409` for uniqueness conflicts. |
| DELETE | `/api/v1/cover-letter-templates/{id}` | Soft-delete an owned template; returns `204 No Content`. Returns `403` for cross-user attempts, `404` for non-existent IDs. |
| POST | `/api/v1/vacancies/filter` | Browse or filter user-scoped vacancies with keyset cursor pagination. Empty body = browse all. Optional `sortBy`: `createdAt` (default), `updatedAt`, `relevance`. Enum filter arrays accept string names. Partial cursor returns `400`. Unknown `sortBy` returns `400`. Salary cross-field validation: `salaryMin ≤ salaryMax` (400 + `errors.SalaryMin` on violation). `excludeKeywords` array excludes vacancies whose title or description contains any keyword (AND logic, explicit-escape LIKE). **[story 4-1 + 4-2]** |
| GET | `/api/v1/vacancies/{id}` | Return full detail for an owned vacancy (`id`, `title`, `description`, `company`, `skills`, `workLocationType`, `location`, `salary`, `currency`, `matchScore`, `jobSource`, `categories`, `experienceLevel`, `createdAt`); returns `404` for non-existent, soft-deleted, or cross-user vacancies; `401` if unauthenticated. **[story 4-3]** |
| POST | `/api/v1/cover-letters` | Create a cover letter for an owned vacancy (`vacancyId`, `content` 50–10,000 chars); returns `201 Created` with `Location` header. DB-backed uniqueness: one non-deleted cover letter per user per vacancy; duplicate returns `409`. `404` for non-existent or cross-user vacancy. **[Epic 5]** |
| GET | `/api/v1/cover-letters` | Cursor-paginated list of cover letters for the authenticated user; ordered by `createdAt desc`; includes `vacancyTitle` from linked vacancy. **[Epic 5]** |
| GET | `/api/v1/cover-letters/{id}` | Full detail for an owned cover letter including nested vacancy fields (`title`, `company`, `workLocationType`, `location`); `IgnoreQueryFilters` on vacancy side to handle soft-deleted vacancies; `404` for missing or cross-user. **[Epic 5]** |
| PATCH | `/api/v1/cover-letters/{id}` | Update `content` only (`vacancyId` is immutable); returns `200 OK` with updated cover letter; `403` for cross-user, `404` for missing. **[Epic 5]** |
| DELETE | `/api/v1/cover-letters/{id}` | Soft-delete an owned cover letter; returns `204 No Content`; `403` for cross-user, `404` for missing. **[Epic 5]** |

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
8. [done] **Vacancies** browse/filter (stories 4-1 and 4-2) and vacancy detail (story 4-3) — all merged.
9. [done] **Resumes**, **educations**, **cover letters** CRUD and relationships via user-scoped routes only (no cross-user list endpoints). Resume and education CRUD are merged; all cover letter template CRUD are merged; cover letter CRUD (Epic 5) is merged.

### Phase C — Security

10. [done] Password hashing.
11. [done] JWT (or chosen scheme) and protected routes.
12. [done] Authorization: users mutate only their data.

### Phase D — Ingestion and LLM

13. Job source **abstraction** + first **adapter** (start with manual/static feed or first external API you adopt).
14. **Sync** endpoint or scheduled job writing vacancies with valid **`JobSource`**.
15. LLM router + config storage; **analyze** and **cover letter** endpoints.

### Phase E — Hardening

16. Quartz jobs, Redis where useful, rate limiting, CORS, health/ready, problem details.
17. Integration tests and CI parity; optional PDF pipeline, OpenTelemetry, architecture tests.

## Tracking

Work is broken into small GitHub issues **#16–#37** (foundation through hardening).
Stories **1-4 update user profile and avatar management**, **2-1 create resume**, **2-2 list resumes**, **2-4 update resume**, **2-8 get/update/delete education records**, and **3-4 update cover letter template** merged on **2026-04-25**, **2026-04-27**, **2026-04-28**, **2026-04-30**, **2026-05-05**, and **2026-05-09** respectively. Epic 5 (cover letter CRUD, stories 5-1 through 5-5) merged **2026-05-11**. All Phase B stories are now complete.
