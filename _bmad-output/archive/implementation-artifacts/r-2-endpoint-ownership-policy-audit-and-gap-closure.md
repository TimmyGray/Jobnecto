# Story R.2: Endpoint Ownership Policy Audit and Gap Closure

Status: done

GitHub Issue: TBD

## Story

As a **platform maintainer**,
I want all authenticated mutation and sensitive read endpoints to apply consistent, testable ownership checks,
so that no cross-user access path remains in production and every endpoint's contract is explicit about its `403`/`404` behavior for cross-user, missing, and soft-deleted resources.

## Acceptance Criteria

1. A written **endpoint ownership audit** exists for every active authenticated HTTP endpoint that performs a mutation (`POST`, `PATCH`, `DELETE`) or returns a single resource by ID. The audit lives in `_bmad-output/planning-artifacts/architecture/endpoint-ownership-audit.md` and records, per endpoint: HTTP verb + route, controller + action method, MediatR request type, handler type, repository call used, **current** cross-user behavior (status code + exception type), **target** cross-user behavior, and "gap" / "no gap" classification with rationale.
2. The audit covers, at minimum, every endpoint listed in `_bmad-output/project-context.md` "Active HTTP endpoints" that requires `[Authorize]` (i.e. excludes `POST /api/v1/users` and `POST /api/v1/users/token/refresh` registration/refresh edge cases as documented). Specifically: `GET|PATCH /api/v1/users/me`, `POST|PUT|DELETE /api/v1/users/me/avatar`, all `*/resumes/*`, all `*/educations/*`, all `*/cover-letter-templates/*`, all `*/cover-letters/*`, `POST /api/v1/vacancies/filter`, and `GET /api/v1/vacancies/{id}`.
3. For every list endpoint (resumes, educations, cover letter templates, cover letters, vacancies filter), the audit confirms that `userId` is propagated from JWT to the query and enforced at the repository level (`BaseRepository<T>.GetAsync` `UserId` predicate or repository-specific filter) — **FR27** parity is verified or a gap is logged.
4. For every detail/update/delete endpoint operating on a user-owned resource, the audit confirms an explicit ownership guard exists in the handler (`entity.UserId != request.UserId`) — **FR28** parity is verified or a gap is logged.
5. For each gap discovered (handler missing an ownership guard, missing test, undocumented or unintended status-code variance), the audit row marks the gap and references the task that closes it in Tasks/Subtasks below.
6. Cross-user GET-by-id behavior for `Resume`, `Education`, `CoverLetter`, `CoverLetterTemplate`, and `Vacancy` remains `404 NotFound` (not `403`) — this is the existing pattern (existence non-leakage); the audit reaffirms it as the canonical behavior so R.4 can codify it.
7. Cross-user PATCH/DELETE behavior for `Resume`, `Education`, `CoverLetter`, `CoverLetterTemplate` remains `403 Forbidden` (the existing pattern); the audit reaffirms it as canonical so R.4 can codify it. Any handler that does **not** use `ForbiddenException` for cross-user mutation is flagged as a gap and corrected.
8. `POST /api/v1/cover-letters` (which references a `VacancyId` provided in the body) explicitly verifies the referenced vacancy is owned by the caller and returns `404 NotFound` for a cross-user vacancy — audited and confirmed.
9. `/api/v1/users/me*` endpoints are confirmed to have **no cross-user vector** because the resource ID is sourced from the JWT and never accepted from the request body or route — audited and documented (no code change expected).
10. For every gap-closure code change (if any), a focused handler unit test is added or updated under `backend/tests/JobNecto.Tests/Application/` asserting the contract-correct exception type (`NotFoundException` → `404`, `ForbiddenException` → `403`) using the existing FluentAssertions + Moq + `Mock<IMutableRepository<T>>` or `Mock<I{Entity}Repository>` patterns from R.1.
11. OpenAPI `[ProducesResponseType]` attributes on every audited endpoint match the canonical behavior from AC 6/7/8 (e.g. cross-user GET detail must advertise `404`, not `403`; cross-user PATCH/DELETE must advertise both `403` and `404`). Any mismatch is corrected in the controller.
12. The audit document includes a short "Open Items for R.4" section listing endpoint behaviors whose canonical mapping is ambiguous (e.g. soft-deleted vs. cross-user) so R.4 can finalize the matrix without re-discovery.
13. `dotnet build backend/JobNecto.slnx --configuration Release --warnaserror` passes.
14. `dotnet test backend/JobNecto.slnx --configuration Release --warnaserror` passes.

## Tasks / Subtasks

- [x] Task 1: Enumerate the audit endpoint set (AC: 1, 2)
  - [x] Cross-reference `_bmad-output/project-context.md` "Active HTTP endpoints" with the 6 controllers under `backend/src/JobNecto.API/Controllers/`:
    - `UsersController.cs`
    - `ResumesController.cs`
    - `EducationsController.cs`
    - `CoverLetterTemplatesController.cs`
    - `CoverLettersController.cs`
    - `VacanciesController.cs`
  - [x] Produce the master endpoint list (verb + route + controller action + MediatR request + handler) in `_bmad-output/planning-artifacts/architecture/endpoint-ownership-audit.md`
  - [x] Group endpoints by category: (a) list/filter, (b) detail GET-by-id, (c) PATCH update, (d) DELETE soft-delete, (e) POST create, (f) `/users/me*` self-scoped

- [x] Task 2: Audit list/filter endpoints for FR27 user-scoping (AC: 3)
  - [x] `GET /api/v1/resumes` → `ListResumesQuery` → `ListResumesHandler` → `_unitOfWork.ResumeRepository.GetAsync(pagedQuery, ct)` with `pagedQuery.UserId` set
  - [x] `GET /api/v1/educations` → `ListEducationsQuery` → `ListEducationsQueryHandler`
  - [x] `GET /api/v1/cover-letter-templates` → `ListCoverLetterTemplatesQuery` → `ListCoverLetterTemplatesQueryHandler`
  - [x] `GET /api/v1/cover-letters` → `ListCoverLettersQuery` → `ListCoverLettersQueryHandler`
  - [x] `POST /api/v1/vacancies/filter` → `FilterVacanciesQuery` → `FilterVacanciesQueryHandler` → `_unitOfWork.VacancyRepository.GetFilteredAsync(pagedQuery, filter, ct)` with `pagedQuery.UserId` set
  - [x] For each: read the handler and confirm `pagedQuery.UserId = request.UserId` (or repository-specific equivalent) is set before query execution
  - [x] Confirm `BaseRepository<T>.GetAsync` at `backend/src/JobNecto.Infrastructure/Repositories/BaseRepository.cs` applies the `EF.Property<Guid>(entity, "UserId") == userId` predicate when `pagedQuery.UserId` is non-null
  - [x] For `VacancyRepository.GetFilteredAsync` (specialized), confirm it applies the equivalent `UserId` predicate (read `backend/src/JobNecto.Infrastructure/Repositories/VacancyRepository.cs`)
  - [x] Record current vs. target behavior per endpoint in the audit doc; mark "no gap" if scoping is present
  - [x] If any list endpoint omits the `UserId` predicate, log a gap and reference the closure task

- [x] Task 3: Audit detail GET-by-id endpoints for cross-user → 404 contract (AC: 4, 6)
  - [x] `GET /api/v1/resumes/{id}` → `GetResumeQuery` → `GetResumeQueryHandler` — confirm `if (resume.UserId != request.UserId) throw new NotFoundException("Resume", request.ResumeId);`
  - [x] `GET /api/v1/educations/{id}` → `GetEducationQuery` → `GetEducationQueryHandler` — same pattern
  - [x] `GET /api/v1/cover-letter-templates/{id}` → `GetCoverLetterTemplateQuery` → `GetCoverLetterTemplateQueryHandler`
  - [x] `GET /api/v1/cover-letters/{id}` → `GetCoverLetterQuery` → `GetCoverLetterQueryHandler` — note: this handler uses `GetDetailByIdAsync` and `result is null || result.UserId != request.UserId` for the same 404 mapping; record this variant
  - [x] `GET /api/v1/vacancies/{id}` → `GetVacancyQuery` → `GetVacancyQueryHandler`
  - [x] Confirm `GlobalExceptionHandler.TryHandleAsync` (`backend/src/JobNecto.API/Infrastructure/ExceptionHandling/GlobalExceptionHandler.cs`) maps `NotFoundException` → `404 NotFound` with the generic "Resource not found." detail (existence non-leakage)
  - [x] Confirm EF global query filter excludes soft-deleted rows so soft-deleted resource → `BaseRepository<T>.GetByIdAsync` throws `NotFoundException` → `404` (same as cross-user; this is intentional)
  - [x] Record current vs. target behavior per endpoint; mark "no gap" where 404 is correctly enforced

- [x] Task 4: Audit PATCH update endpoints for cross-user → 403 contract (AC: 4, 7)
  - [x] `PATCH /api/v1/resumes/{id}` → `UpdateResumeCommand` → `UpdateResumeCommandHandler` — confirm `if (resume.UserId != request.UserId) throw new ForbiddenException("You do not have permission to update this resume.");`
  - [x] `PATCH /api/v1/educations/{id}` → `UpdateEducationCommand` → `UpdateEducationCommandHandler`
  - [x] `PATCH /api/v1/cover-letter-templates/{id}` → `UpdateCoverLetterTemplateCommand` → `UpdateCoverLetterTemplateCommandHandler`
  - [x] `PATCH /api/v1/cover-letters/{id}` → `UpdateCoverLetterCommand` → `UpdateCoverLetterCommandHandler`
  - [x] Confirm `GlobalExceptionHandler` maps `ForbiddenException` → `403 Forbidden`
  - [x] Confirm missing-id path (`GetByIdAsync` throws `NotFoundException` before the ownership check) → `404` for unknown id (documented in audit)
  - [x] Record current vs. target behavior; if any handler uses a different exception type or skips the guard, log a gap and reference closure task

- [x] Task 5: Audit DELETE soft-delete endpoints for cross-user → 403 contract (AC: 4, 7)
  - [x] `DELETE /api/v1/resumes/{id}` → `DeleteResumeCommand` → `DeleteResumeCommandHandler` — confirm `ForbiddenException` for cross-user, `SoftDeleteAsync` called for owner
  - [x] `DELETE /api/v1/educations/{id}` → `DeleteEducationCommand` → `DeleteEducationCommandHandler`
  - [x] `DELETE /api/v1/cover-letter-templates/{id}` → `DeleteCoverLetterTemplateCommand` → `DeleteCoverLetterTemplateCommandHandler`
  - [x] `DELETE /api/v1/cover-letters/{id}` → `DeleteCoverLetterCommand` → `DeleteCoverLetterCommandHandler`
  - [x] Confirm soft-delete path uses `_unitOfWork.{Entity}Repository.SoftDeleteAsync(entity, ct)` (R.1 contract)
  - [x] Record current vs. target behavior; log any gap

- [x] Task 6: Audit POST create endpoints for body-supplied foreign-key ownership (AC: 8)
  - [x] `POST /api/v1/cover-letters` → `CreateCoverLetterCommand` → `CreateCoverLetterCommandHandler` — body carries `VacancyId`; confirm `if (vacancy.UserId != request.UserId) throw new NotFoundException("Vacancy", request.VacancyId);` is present and that the vacancy is **resolved from the user's scope** before creation
  - [x] `POST /api/v1/resumes`, `POST /api/v1/educations`, `POST /api/v1/cover-letter-templates`: confirm body does **not** accept a foreign `UserId` — `UserId` is always assigned from JWT in the controller (`command.UserId = userId;`); no cross-user vector
  - [x] Search for any other POST endpoint whose body accepts a resource id pointing at a user-owned entity; if found, audit it
  - [x] Record current vs. target behavior; log any gap

- [x] Task 7: Audit `/api/v1/users/me*` endpoints for self-scoped contract (AC: 9)
  - [x] `GET /api/v1/users/me` → `GetCurrentUserQuery` → `GetCurrentUserQueryHandler` — `UserId` sourced from JWT (`HttpContext.GetCurrentUserId()`), no body/route id, no cross-user vector
  - [x] `PATCH /api/v1/users/me` → `UpdateCurrentUserCommand` — `UserId` sourced from JWT, the handler at `backend/src/JobNecto.Application/Users/UpdateCurrentUserCommandHandler.cs` operates on the JWT-bound user only
  - [x] `POST /api/v1/users/me/avatar`, `PUT /api/v1/users/me/avatar`, `DELETE /api/v1/users/me/avatar` — `UserId` sourced from JWT, no cross-user vector
  - [x] Document the rationale ("ID is JWT-bound; body cannot override UserId") in the audit
  - [x] No code change expected — flag explicitly if a body parameter named `UserId` slips into any `UpdateCurrentUserCommand`/`*AvatarCommand` shape

- [x] Task 8: Close any gaps discovered in Tasks 2-7 (AC: 5, 10)
  - [x] For each gap row in the audit, implement the minimal handler/repository change to enforce ownership consistent with R.2's documented canonical behavior (404 for detail GET, 403 for PATCH/DELETE, 404 for body-supplied foreign-key references)
  - [x] Add or update a focused unit test under `backend/tests/JobNecto.Tests/Application/{Feature}/` that arranges a cross-user scenario, acts on the handler, and asserts the contract-correct exception:
    - GET cross-user → `await act.Should().ThrowAsync<NotFoundException>();`
    - PATCH cross-user → `await act.Should().ThrowAsync<ForbiddenException>();`
    - DELETE cross-user → `await act.Should().ThrowAsync<ForbiddenException>();`
    - POST cover-letter cross-user vacancy → `await act.Should().ThrowAsync<NotFoundException>();`
  - [x] Mocks use `Mock<IMutableRepository<T>>` (Resume, Education, CoverLetter, CoverLetterTemplate), `Mock<IVacancyRepository>` (Vacancy), `Mock<IUserRepository>` (User) consistent with R.1 patterns
  - [x] If no gaps are discovered, document "no behavioral changes; audit only" in the Dev Agent Record

- [x] Task 9: Reconcile OpenAPI `[ProducesResponseType]` attributes with canonical behavior (AC: 11)
  - [x] For each audited endpoint, open the controller action and verify `[ProducesResponseType]` lists every status code the action can return:
    - Detail GET-by-id: `200`, `401`, `404` (must include `404`; **must not** advertise `403` for the cross-user case)
    - List/filter: `200`, `400` (if applicable), `401`
    - PATCH update: `200`, `400`, `401`, `403`, `404`
    - DELETE: `204`, `401`, `403`, `404`
    - POST create (with foreign-key body): `201`, `400`, `401`, `404`, `409`
    - POST `/users` (anonymous registration): `201`, `400`, `409` — already correct
  - [x] Where the controller currently lists `403` on a detail GET-by-id (cross-user) action, remove it (false advertisement; the contract is `404`); where a PATCH/DELETE action omits `403` or `404`, add it
  - [x] Spot-check the runtime contract by running OpenAPI export: `dotnet run --project backend/src/JobNecto.API/JobNecto.API.csproj` against the dev profile is **not** required — Swashbuckle generates from the attributes
  - [x] If any controller attribute change is required, update the corresponding action and re-verify with `dotnet build backend/JobNecto.slnx`

- [x] Task 10: Author the audit document (AC: 1, 2, 5, 6, 7, 8, 9, 12)
  - [x] Create `_bmad-output/planning-artifacts/architecture/endpoint-ownership-audit.md`
  - [x] Sections:
    - `# Endpoint Ownership Audit (Story R.2)`
    - `## Canonical Behaviors` — restate the 404-on-cross-user-read / 403-on-cross-user-mutation rule and cite FR27/FR28
    - `## Endpoint Matrix` — a table with columns: `Method+Route`, `Controller.Action`, `MediatR Request`, `Handler`, `Cross-user current`, `Cross-user target`, `Soft-deleted current`, `Soft-deleted target`, `Not-found current`, `Not-found target`, `Gap?`
    - `## Gaps and Closures` — narrative list of each gap discovered, the task that closed it, and the test added
    - `## Open Items for R.4` — endpoint behaviors whose canonical mapping is ambiguous (e.g. soft-deleted resource that the caller used to own; cover-letter POST when vacancy is soft-deleted) — surface them for R.4
  - [x] Link the audit doc from `_bmad-output/planning-artifacts/architecture/index.md`

- [x] Task 11: Run build and tests (AC: 13, 14)
  - [x] `dotnet build backend/JobNecto.slnx --configuration Release --warnaserror` — must succeed with 0 warnings
  - [x] `dotnet test backend/JobNecto.slnx --configuration Release --warnaserror` — must succeed
  - [x] Record the test count in the Dev Agent Record / Completion Notes

- [x] Task 12: Update sprint status (AC: housekeeping)
  - [x] Already flipped to `ready-for-dev` at story-draft time; on dev start, the dev-story skill will move it to `in-progress`
  - [x] On dev completion, move to `review`

## Dev Notes

### Core Problem

The codebase grew through Epics 1-5 with a per-feature ownership pattern that — while individually correct — has never been audited as a whole. Phase C cannot close until we have a single source of truth that enumerates every authenticated endpoint, names the canonical cross-user contract, and confirms (or fixes) handler conformance. This story is the audit; R.3 will add the cross-user integration suite that protects against regressions; R.4 will publish the formal `403`/`404` matrix; R.5 is the gate that closes Phase C.

### Inventory of Endpoints in Scope

| # | Method | Route | Controller.Action | MediatR Request | Handler |
| --- | --- | --- | --- | --- | --- |
| 1 | POST | `/api/v1/users` | `UsersController.Create` | `CreateUserCommand` | `CreateUserCommandHandler` (anonymous, out of scope) |
| 2 | POST | `/api/v1/users/token/refresh` | `UsersController.RefreshToken` | — (no MediatR) | — (controller-only, JWT-bound, no cross-user vector) |
| 3 | GET | `/api/v1/users/me` | `UsersController.GetCurrentUser` | `GetCurrentUserQuery` | `GetCurrentUserQueryHandler` |
| 4 | PATCH | `/api/v1/users/me` | `UsersController.UpdateCurrentUser` | `UpdateCurrentUserCommand` | `UpdateCurrentUserCommandHandler` |
| 5 | POST | `/api/v1/users/me/avatar` | `UsersController.UploadAvatar` | `UploadCurrentUserAvatarCommand` | `UploadCurrentUserAvatarCommandHandler` |
| 6 | PUT | `/api/v1/users/me/avatar` | `UsersController.UpdateAvatar` | `UploadCurrentUserAvatarCommand` | `UploadCurrentUserAvatarCommandHandler` |
| 7 | DELETE | `/api/v1/users/me/avatar` | `UsersController.DeleteAvatar` | `DeleteCurrentUserAvatarCommand` | `DeleteCurrentUserAvatarCommandHandler` |
| 8 | POST | `/api/v1/resumes` | `ResumesController.Create` | `CreateResumeCommand` | `CreateResumeCommandHandler` |
| 9 | GET | `/api/v1/resumes` | `ResumesController.ListAsync` | `ListResumesQuery` | `ListResumesHandler` |
| 10 | GET | `/api/v1/resumes/{id}` | `ResumesController.GetAsync` | `GetResumeQuery` | `GetResumeQueryHandler` |
| 11 | PATCH | `/api/v1/resumes/{id}` | `ResumesController.UpdateAsync` | `UpdateResumeCommand` | `UpdateResumeCommandHandler` |
| 12 | DELETE | `/api/v1/resumes/{id}` | `ResumesController.DeleteAsync` | `DeleteResumeCommand` | `DeleteResumeCommandHandler` |
| 13 | POST | `/api/v1/educations` | `EducationsController.Create` | `CreateEducationCommand` | `CreateEducationCommandHandler` |
| 14 | GET | `/api/v1/educations` | `EducationsController.ListAsync` | `ListEducationsQuery` | `ListEducationsQueryHandler` |
| 15 | GET | `/api/v1/educations/{id}` | `EducationsController.GetAsync` | `GetEducationQuery` | `GetEducationQueryHandler` |
| 16 | PATCH | `/api/v1/educations/{id}` | `EducationsController.UpdateAsync` | `UpdateEducationCommand` | `UpdateEducationCommandHandler` |
| 17 | DELETE | `/api/v1/educations/{id}` | `EducationsController.DeleteAsync` | `DeleteEducationCommand` | `DeleteEducationCommandHandler` |
| 18 | POST | `/api/v1/cover-letter-templates` | `CoverLetterTemplatesController.Create` | `CreateCoverLetterTemplateCommand` | `CreateCoverLetterTemplateCommandHandler` |
| 19 | GET | `/api/v1/cover-letter-templates` | `CoverLetterTemplatesController.ListAsync` | `ListCoverLetterTemplatesQuery` | `ListCoverLetterTemplatesQueryHandler` |
| 20 | GET | `/api/v1/cover-letter-templates/{id}` | `CoverLetterTemplatesController.GetAsync` | `GetCoverLetterTemplateQuery` | `GetCoverLetterTemplateQueryHandler` |
| 21 | PATCH | `/api/v1/cover-letter-templates/{id}` | `CoverLetterTemplatesController.UpdateAsync` | `UpdateCoverLetterTemplateCommand` | `UpdateCoverLetterTemplateCommandHandler` |
| 22 | DELETE | `/api/v1/cover-letter-templates/{id}` | `CoverLetterTemplatesController.DeleteAsync` | `DeleteCoverLetterTemplateCommand` | `DeleteCoverLetterTemplateCommandHandler` |
| 23 | POST | `/api/v1/vacancies/filter` | `VacanciesController.FilterAsync` | `FilterVacanciesQuery` | `FilterVacanciesQueryHandler` |
| 24 | GET | `/api/v1/vacancies/{id}` | `VacanciesController.GetAsync` | `GetVacancyQuery` | `GetVacancyQueryHandler` |
| 25 | POST | `/api/v1/cover-letters` | `CoverLettersController.CreateAsync` | `CreateCoverLetterCommand` | `CreateCoverLetterCommandHandler` |
| 26 | GET | `/api/v1/cover-letters` | `CoverLettersController.ListAsync` | `ListCoverLettersQuery` | `ListCoverLettersQueryHandler` |
| 27 | GET | `/api/v1/cover-letters/{id}` | `CoverLettersController.GetByIdAsync` | `GetCoverLetterQuery` | `GetCoverLetterQueryHandler` |
| 28 | PATCH | `/api/v1/cover-letters/{id}` | `CoverLettersController.UpdateAsync` | `UpdateCoverLetterCommand` | `UpdateCoverLetterCommandHandler` |
| 29 | DELETE | `/api/v1/cover-letters/{id}` | `CoverLettersController.DeleteAsync` | `DeleteCoverLetterCommand` | `DeleteCoverLetterCommandHandler` |

### Current Ownership Pattern (Audit Baseline)

Established by reading the handlers as of story draft (commit `d77756e`):

- **Detail GET-by-id** (rows 10, 15, 20, 24, 27): handler calls `_unitOfWork.{Entity}Repository.GetByIdAsync(id, ct)`, then `if (entity.UserId != request.UserId) throw new NotFoundException("{Entity}", id);`. Mapping in `GlobalExceptionHandler` → `404 NotFound`. Soft-deleted rows are excluded by the EF global query filter inside `GetByIdAsync`, so `GetByIdAsync` throws `NotFoundException` for soft-deleted records too — the call site never reaches the ownership check, and the wire result is also `404`.
- **PATCH update** (rows 11, 16, 21, 28): `GetByIdAsync` first (→ `NotFoundException` if missing → `404`), then `if (entity.UserId != request.UserId) throw new ForbiddenException(...)` → `403`.
- **DELETE soft-delete** (rows 12, 17, 22, 29): same pattern as PATCH; then `SoftDeleteAsync` + `SaveChangesAsync`. R.1 already separated `SoftDeleteAsync` onto `ISoftDeleteRepository<T>`.
- **POST create (own scope)** (rows 8, 13, 18): controller sets `command.UserId = userId` from JWT; body cannot carry a foreign `UserId`. No cross-user vector.
- **POST create (foreign-key body)** (row 25): body carries `VacancyId`. Handler calls `_unitOfWork.VacancyRepository.GetByIdAsync(VacancyId, ct)`, then `if (vacancy.UserId != request.UserId) throw new NotFoundException("Vacancy", VacancyId);` → `404`.
- **List endpoints** (rows 9, 14, 19, 23, 26): controller sets `query.UserId = userId` from JWT; handler passes it to `_unitOfWork.{Entity}Repository.GetAsync(pagedQuery, ct)` (or `GetFilteredAsync` for vacancies). `BaseRepository<T>.GetAsync` applies `EF.Property<Guid>(entity, "UserId") == userId` when `pagedQuery.UserId` is non-null.
- **`/users/me*` endpoints** (rows 3-7): `UserId` is sourced from JWT in the controller (`HttpContext.GetCurrentUserId()`); none of the request DTOs accept a `UserId` from the body or route. No cross-user vector exists by construction.

### Canonical Behaviors To Affirm (Cite in Audit Doc)

| Scenario | Target Status | Exception | Why |
| --- | --- | --- | --- |
| Detail GET-by-id, cross-user | `404` | `NotFoundException` | Existence non-leakage; matches `GlobalExceptionHandler` generic "Resource not found." detail |
| Detail GET-by-id, missing id | `404` | `NotFoundException` (from `GetByIdAsync`) | Indistinguishable from cross-user (intentional) |
| Detail GET-by-id, soft-deleted | `404` | `NotFoundException` (from `GetByIdAsync` via EF filter) | Indistinguishable from missing (intentional) |
| PATCH/DELETE, cross-user | `403` | `ForbiddenException` | Caller authenticated but lacks permission; existence is implied by route shape (`/{id}`) |
| PATCH/DELETE, missing id | `404` | `NotFoundException` (from `GetByIdAsync`, before ownership check) | Resource truly does not exist |
| PATCH/DELETE, soft-deleted | `404` | `NotFoundException` (from `GetByIdAsync` via EF filter) | Indistinguishable from missing |
| POST cover-letter, cross-user vacancy | `404` | `NotFoundException("Vacancy")` | Body carries the foreign id; existence non-leakage applies |
| POST cover-letter, duplicate vacancy | `409` | `ConflictException` | Unique constraint already enforced |
| `/users/me*` | n/a | n/a | No cross-user vector; JWT-bound |

### Probable Gaps (to confirm during audit)

Based on a draft-time read of the codebase, no behavioral gaps are visible — the existing pattern is already consistent. The audit is expected to confirm this with documentation. However, **OpenAPI attribute mismatches are likely** and must be checked carefully in Task 9 — for example, several `[ProducesResponseType(StatusCodes.Status403Forbidden)]` attributes may appear on actions whose canonical cross-user result is `404` (or vice-versa). Specifically:

- `ResumesController.GetAsync` — currently advertises only `200`, `401`, `404` (correct; no `403`).
- `EducationsController.GetAsync` — currently advertises only `200`, `401`, `404` (correct).
- `CoverLetterTemplatesController.GetAsync` — currently advertises only `200`, `401`, `404` (correct).
- `CoverLettersController.GetByIdAsync` — currently advertises only `200`, `401`, `404` (correct).
- `VacanciesController.GetAsync` — currently advertises only `200`, `401`, `404` (correct).
- All PATCH/DELETE actions — currently advertise `403` and `404` (correct).

If the audit confirms all attributes already match the canonical behavior, Task 9 will be a no-op verification.

### Files to Read (Read-Only Audit)

| File | Why |
| --- | --- |
| `backend/src/JobNecto.API/Controllers/UsersController.cs` | Rows 1-7; JWT-bound endpoints |
| `backend/src/JobNecto.API/Controllers/ResumesController.cs` | Rows 8-12 |
| `backend/src/JobNecto.API/Controllers/EducationsController.cs` | Rows 13-17 |
| `backend/src/JobNecto.API/Controllers/CoverLetterTemplatesController.cs` | Rows 18-22 |
| `backend/src/JobNecto.API/Controllers/VacanciesController.cs` | Rows 23-24 |
| `backend/src/JobNecto.API/Controllers/CoverLettersController.cs` | Rows 25-29 |
| `backend/src/JobNecto.Application/Users/*Handler.cs` | Confirm JWT-bound semantics |
| `backend/src/JobNecto.Application/Resumes/*Handler.cs` | Confirm 404/403 pattern |
| `backend/src/JobNecto.Application/Educations/*Handler.cs` | Confirm 404/403 pattern |
| `backend/src/JobNecto.Application/CoverLetterTemplates/*Handler.cs` | Confirm 404/403 pattern |
| `backend/src/JobNecto.Application/CoverLetters/*Handler.cs` | Confirm 404/403 pattern + cross-user vacancy `404` on POST |
| `backend/src/JobNecto.Application/Vacancies/*Handler.cs` | Confirm 404 pattern + list UserId scoping |
| `backend/src/JobNecto.Infrastructure/Repositories/BaseRepository.cs` | Confirm `GetAsync` applies `UserId` predicate; `GetByIdAsync` throws `NotFoundException` for missing rows |
| `backend/src/JobNecto.Infrastructure/Repositories/VacancyRepository.cs` | Confirm `GetFilteredAsync` applies `UserId` predicate |
| `backend/src/JobNecto.API/Infrastructure/ExceptionHandling/GlobalExceptionHandler.cs` | Confirm exception → status code mapping |
| `_bmad-output/planning-artifacts/epics/requirements-inventory.md` | FR27/FR28 verbatim text for audit citation |

### Files to Create

| File | Reason |
| --- | --- |
| `_bmad-output/planning-artifacts/architecture/endpoint-ownership-audit.md` | The audit deliverable (AC 1, 5, 12) |

### Files to Modify (Only If Audit Surfaces a Gap)

| File | Likely Reason |
| --- | --- |
| Any controller action with mismatched `[ProducesResponseType]` attributes | OpenAPI alignment (AC 11) |
| Any handler missing an ownership guard or using the wrong exception type | Behavioral closure (AC 5, 7, 10) |
| `_bmad-output/planning-artifacts/architecture/index.md` | Link the new audit doc |

### Test Pattern (For Any Gap-Closure Test)

Mirror the R.1 / Epic 2 handler test conventions:

```csharp
// backend/tests/JobNecto.Tests/Application/{Feature}/{Handler}Tests.cs
[Fact]
public async Task Handle_WhenResourceBelongsToAnotherUser_Throws{Exception}()
{
    // Arrange
    var resourceId = Guid.NewGuid();
    var callerId = Guid.NewGuid();
    var ownerId = Guid.NewGuid();
    var entity = new {Entity} { Id = resourceId, UserId = ownerId, /* ... */ };

    var repoMock = new Mock<I{Mutable|Vacancy|User}Repository>();
    repoMock.Setup(r => r.GetByIdAsync(resourceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

    var uowMock = new Mock<IUnitOfWork>();
    uowMock.SetupGet(u => u.{Entity}Repository).Returns(repoMock.Object);

    var handler = new {Handler}(uowMock.Object);
    var request = new {Request} { /* ResourceId */ = resourceId, UserId = callerId };

    // Act
    Func<Task> act = () => handler.Handle(request, CancellationToken.None);

    // Assert
    await act.Should().ThrowAsync<{NotFoundException|ForbiddenException}>();
}
```

For the cover-letter POST cross-user-vacancy case: set up `Mock<IVacancyRepository>.GetByIdAsync` to return a vacancy owned by `ownerId`, then assert `NotFoundException` with message `"Vacancy with id {VacancyId} not found"` (or similar — match the `NotFoundException` constructor used by the handler).

### Namespace Convention (Mandatory)

If any new tests are added, namespaces must mirror folder structure:

| File | Namespace |
| --- | --- |
| `backend/tests/JobNecto.Tests/Application/Resumes/*.cs` | `JobNecto.Tests.Application.Resumes` |
| `backend/tests/JobNecto.Tests/Application/Educations/*.cs` | `JobNecto.Tests.Application.Educations` |
| `backend/tests/JobNecto.Tests/Application/CoverLetters/*.cs` | `JobNecto.Tests.Application.CoverLetters` |
| `backend/tests/JobNecto.Tests/Application/CoverLetterTemplates/*.cs` | `JobNecto.Tests.Application.CoverLetterTemplates` |
| `backend/tests/JobNecto.Tests/Application/Vacancies/*.cs` | `JobNecto.Tests.Application.Vacancies` |

### Constraints and Scope Discipline

- **Audit first, fix second.** Do not modify any handler before the audit row for that endpoint has been recorded.
- **Zero behavioral drift on no-gap endpoints.** If audit confirms an endpoint already matches the canonical contract, do not touch it.
- **No new contracts.** R.4 owns the formal contract matrix; R.2 only affirms or fixes existing ones to be internally consistent.
- **No integration tests in this story.** Cross-user integration suite is R.3.
- **Use `backend/JobNecto.slnx`** for every build/test invocation — never the root `Jobnecto.sln`. (See `_bmad-output/project-context.md`, "Critical don't-miss rules".)
- **Stay scoped.** Do not refactor unrelated code; avoid drive-by changes.

### Agent Learnings to Apply

- Keep generated test data validator-compliant; infrastructure tests skip validators. (`agent-learnings.md` — "Keep generated test data validator-compliant")
- Set persisted timestamps in UTC at the layer that owns the mutation (already enforced by R.1's `SoftDeletableRepository<T>` for soft delete; PATCH handlers continue to set `UpdatedAt = DateTime.UtcNow`).
- Prefer separate handler files; this story does not introduce new handlers but may modify existing ones — modify in place. (`agent-learnings.md` — "Prefer separate handler file")
- EF snapshot parity matters when entity shape changes; this story does not change entity shape, so no migration/snapshot work is expected. (Recent learning from commit `d77756e`.)

### References

- [Source: `_bmad-output/archive/planning-artifacts/epics/epic-r-authorization-ownership-hardening.md` — Scope Context + Story R.2 AC block] — story origin
- [Source: `_bmad-output/planning-artifacts/epics/requirements-inventory.md` — FR27, FR28] — requirements text being affirmed
- [Source: `_bmad-output/project-context.md` — "Active HTTP endpoints"] — canonical endpoint inventory
- [Source: `backend/src/JobNecto.API/Infrastructure/ExceptionHandling/GlobalExceptionHandler.cs`] — exception → status mapping
- [Source: `backend/src/JobNecto.Infrastructure/Repositories/BaseRepository.cs` — `GetByIdAsync`, `GetAsync`] — repository-level scoping baseline
- [Source: `_bmad-output/archive/implementation-artifacts/r-1-separate-soft-delete-repository-contract.md`] — R.1 patterns to mirror in any gap-closure tests
- [Source: `AGENTS.md`] — build/test commands, namespace rules, secret rules

## Open Questions

1. **Should `/users/me*` endpoints be added to the audit endpoint matrix even though they have no cross-user vector?** Assumption: yes (AC 9 explicitly requires documenting the rationale — "no cross-user vector"). If the human prefers a leaner audit that skips JWT-bound endpoints, narrow Task 7 to a single sentence in the audit doc.
2. **Soft-deleted resource owned by the caller: 404 or some other contract?** Today, the EF global query filter excludes soft-deleted rows from `GetByIdAsync`, so the caller cannot tell their own soft-deleted resource apart from a never-existed one. The audit will flag this as an Open Item for R.4 (per AC 12); R.2 does not change behavior here.
3. **`POST /api/v1/users/token/refresh` is not MediatR-backed** — it operates entirely in the controller. It's listed in scope but has no handler-level guard to audit; the audit row will note "controller-only, JWT-bound" and move on. Confirming this is the right treatment.
4. **Anonymous endpoints (`POST /api/v1/users`)**: out of scope for ownership audit (no JWT, no resource being owned), but the audit doc lists them with a "not applicable" row so the inventory is complete. Confirming this is acceptable.

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6 (Sonnet 4.6, 1M context) — 2026-05-21

### Debug Log References

No blocking issues encountered. All 29 endpoints audited in a single pass; one OpenAPI metadata gap found and closed.

### Completion Notes List

- **Audit outcome:** No behavioral gaps found. All handlers implement canonical ownership contracts (404 for cross-user reads, 403 for cross-user mutations, 404 for body-supplied foreign-key ownership failure).
- **EF global query filter confirmed** on all 6 entity types in `AppDbContext.cs` — soft-deleted rows are indistinguishable from missing for callers.
- **One OpenAPI gap closed (Gap R.2-G1):** `ResumesController.DeleteAsync` falsely advertised `[ProducesResponseType(400)]`. Removed. No behavioral change.
- **`CoverLetterRepository.GetPagedListAsync` variant noted:** uses `IgnoreQueryFilters()` on the vacancy JOIN to preserve historical snapshot for cover-letter list items; this is intentional and listed as Open Item 3 for R.4.
- **`GetCoverLetterQueryHandler` variant noted:** uses `GetDetailByIdAsync` + null-check pattern (`result is null || result.UserId != request.UserId`) instead of `GetByIdAsync` + NotFoundException; 404 contract is identical.
- **Build:** 0 warnings, 0 errors (Release, --warnaserror)
- **Tests:** 477/477 passed at R.2 implementation time (no regressions). Note (2026-05-25): the current unified working-tree count is 520/520 after sibling stories R.3/R.4 added their tests; the 477 figure reflects the tree state when R.2 was implemented.
- **No new tests added** — AC 10 states tests required only for gap-closure code changes; the single gap was OpenAPI metadata only.

### File List

- `backend/src/JobNecto.API/Controllers/ResumesController.cs` — removed false `[ProducesResponseType(400)]` from `DeleteAsync`
- `_bmad-output/planning-artifacts/architecture/endpoint-ownership-audit.md` — created (audit deliverable)
- `_bmad-output/planning-artifacts/architecture/index.md` — added link to audit doc
- `_bmad-output/archive/implementation-artifacts/r-2-endpoint-ownership-policy-audit-and-gap-closure.md` — this story file

## Change Log

- 2026-05-21: Story drafted by Amelia (bmad-create-story). Status set to `ready-for-dev`. 14 ACs, 12 tasks. No code changes expected unless audit surfaces a gap; OpenAPI attribute reconciliation is most likely scope. Sprint status `r-2-endpoint-ownership-policy-audit-and-gap-closure` flipped from `backlog` to `ready-for-dev`.
- 2026-05-21: Story implemented by Amelia (claude-sonnet-4-6). Audit complete — no behavioral gaps. One OpenAPI metadata gap closed (`ResumesController.DeleteAsync` 400 removed). Audit doc created. Build+tests pass (477/477). Status → `review`.
- 2026-05-25: Passed independent code review (no blocking issues); approved and merged. Status → done.

