# Endpoint Ownership Audit (Story R.2)

_Audit date: 2026-05-21. Commit baseline: `d77756e` (latest at audit time). All 29 authenticated endpoints across 6 controllers reviewed._

---

## Canonical Behaviors

Established by this audit and referenced by R.4 for the formal contract matrix.

**FR27** (requirements-inventory.md, line 31): _All user-owned list endpoints must automatically filter by `userId` at the repository/handler level to enforce data isolation._

**FR28** (requirements-inventory.md, line 32): _All mutation handlers must verify resource ownership before allowing changes; return `403 Forbidden` or throw ownership exception if violated._

| Scenario | HTTP Status | Exception | Rationale |
|---|---|---|---|
| Detail GET-by-id, cross-user | `404` | `NotFoundException` | Existence non-leakage: `GlobalExceptionHandler` maps `NotFoundException` → `404` with generic "Resource not found." detail |
| Detail GET-by-id, missing id | `404` | `NotFoundException` (from `BaseRepository.GetByIdAsync`) | Indistinguishable from cross-user — intentional |
| Detail GET-by-id, soft-deleted | `404` | `NotFoundException` (via EF global filter; `_dbSet.FirstOrDefaultAsync` returns null) | Indistinguishable from missing — intentional |
| PATCH/DELETE, cross-user | `403` | `ForbiddenException` | Caller authenticated, lacks permission. Existence implied by route shape (`/{id}`). |
| PATCH/DELETE, missing id | `404` | `NotFoundException` (from `GetByIdAsync`, before ownership check) | Resource does not exist |
| PATCH/DELETE, soft-deleted | `404` | `NotFoundException` (via EF global filter, before ownership check) | Indistinguishable from missing |
| POST cover-letter, cross-user vacancy | `404` | `NotFoundException("Vacancy", ...)` | Body carries foreign id; existence non-leakage applies |
| POST cover-letter, soft-deleted vacancy | `404` | `NotFoundException` (via EF global filter on `GetByIdAsync`) | See Open Items — R.4 to confirm |
| POST cover-letter, duplicate vacancy | `409` | `ConflictException` | Unique constraint enforced |
| `/users/me*` | n/a | n/a | No cross-user vector; `UserId` always JWT-bound |

**Exception → HTTP mapping** (confirmed in `GlobalExceptionHandler.cs`):
- `NotFoundException` → `404 Not Found`, detail = "Resource not found." (generic — no entity-specific detail leaked)
- `ForbiddenException` → `403 Forbidden`, detail = "You do not have permission to access this resource."
- `ConflictException` → `409 Conflict`, detail = exception message
- `ValidationException` → `400 Bad Request`

**EF global query filters** (confirmed in `AppDbContext.cs`): All six entity types (`User`, `Resume`, `Education`, `CoverLetter`, `CoverLetterTemplate`, `Vacancy`) carry `HasQueryFilter(e => !e.IsDeleted)`. This filter is active on all non-`IgnoreQueryFilters()` queries, causing `BaseRepository.GetByIdAsync` to return `null` (and throw `NotFoundException`) for soft-deleted rows without any explicit handler-side check.

---

## Endpoint Matrix

| # | Method + Route | Controller.Action | MediatR Request | Handler | Cross-user current | Cross-user target | Soft-deleted current | Soft-deleted target | Not-found current | Not-found target | Gap? |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | POST `/api/v1/users` | `UsersController.Create` | `CreateUserCommand` | `CreateUserCommandHandler` | n/a (anonymous) | n/a | n/a | n/a | n/a | n/a | No |
| 2 | POST `/api/v1/users/token/refresh` | `UsersController.RefreshToken` | — (controller-only) | — | n/a (JWT-bound, no DB lookup) | n/a | n/a | n/a | n/a | n/a | No |
| 3 | GET `/api/v1/users/me` | `UsersController.GetCurrentUser` | `GetCurrentUserQuery` | `GetCurrentUserQueryHandler` | n/a (JWT-bound) | n/a | `404` (EF filter → NotFoundException) | `404` | `404` (NotFoundException) | `404` | No |
| 4 | PATCH `/api/v1/users/me` | `UsersController.UpdateCurrentUser` | `UpdateCurrentUserCommand` | `UpdateCurrentUserCommandHandler` | n/a (JWT-bound) | n/a | `404` (EF filter) | `404` | `404` (NotFoundException) | `404` | No |
| 5 | POST `/api/v1/users/me/avatar` | `UsersController.UploadAvatar` | `UploadCurrentUserAvatarCommand` | `UploadCurrentUserAvatarCommandHandler` | n/a (JWT-bound) | n/a | `404` (EF filter) | `404` | `404` | `404` | No |
| 6 | PUT `/api/v1/users/me/avatar` | `UsersController.UpdateAvatar` | `UploadCurrentUserAvatarCommand` | `UploadCurrentUserAvatarCommandHandler` | n/a (JWT-bound) | n/a | `404` (EF filter) | `404` | `404` | `404` | No |
| 7 | DELETE `/api/v1/users/me/avatar` | `UsersController.DeleteAvatar` | `DeleteCurrentUserAvatarCommand` | `DeleteCurrentUserAvatarCommandHandler` | n/a (JWT-bound) | n/a | `404` (EF filter) | `404` | `404` | `404` | No |
| 8 | POST `/api/v1/resumes` | `ResumesController.Create` | `CreateResumeCommand` | `CreateResumeCommandHandler` | n/a (no foreign-key body) | n/a | n/a | n/a | n/a | n/a | No |
| 9 | GET `/api/v1/resumes` | `ResumesController.ListAsync` | `ListResumesQuery` | `ListResumesQueryHandler` | scoped: `pagedQuery.UserId = request.UserId` → `BaseRepository.GetAsync` applies `EF.Property<Guid>(entity,"UserId") == userId` | scoped | excluded by EF filter | excluded | empty list | empty list | No |
| 10 | GET `/api/v1/resumes/{id}` | `ResumesController.GetAsync` | `GetResumeQuery` | `GetResumeQueryHandler` | `404` (NotFoundException — "Resume") | `404` | `404` (EF filter → null → NotFoundException) | `404` | `404` | `404` | No |
| 11 | PATCH `/api/v1/resumes/{id}` | `ResumesController.UpdateAsync` | `UpdateResumeCommand` | `UpdateResumeCommandHandler` | `403` (ForbiddenException) | `403` | `404` (EF filter before ownership check) | `404` | `404` (NotFoundException) | `404` | No |
| 12 | DELETE `/api/v1/resumes/{id}` | `ResumesController.DeleteAsync` | `DeleteResumeCommand` | `DeleteResumeCommandHandler` | `403` (ForbiddenException) | `403` | `404` (EF filter) | `404` | `404` | `404` | **OpenAPI only** — see Gaps |
| 13 | POST `/api/v1/educations` | `EducationsController.Create` | `CreateEducationCommand` | `CreateEducationCommandHandler` | n/a | n/a | n/a | n/a | n/a | n/a | No |
| 14 | GET `/api/v1/educations` | `EducationsController.ListAsync` | `ListEducationsQuery` | `ListEducationsQueryHandler` | scoped: `pagedQuery.UserId = request.UserId` → `BaseRepository.GetAsync` | scoped | excluded by EF filter | excluded | empty list | empty list | No |
| 15 | GET `/api/v1/educations/{id}` | `EducationsController.GetAsync` | `GetEducationQuery` | `GetEducationQueryHandler` | `404` (NotFoundException — "Education") | `404` | `404` (EF filter) | `404` | `404` | `404` | No |
| 16 | PATCH `/api/v1/educations/{id}` | `EducationsController.UpdateAsync` | `UpdateEducationCommand` | `UpdateEducationCommandHandler` | `403` (ForbiddenException) | `403` | `404` (EF filter) | `404` | `404` | `404` | No |
| 17 | DELETE `/api/v1/educations/{id}` | `EducationsController.DeleteAsync` | `DeleteEducationCommand` | `DeleteEducationCommandHandler` | `403` (ForbiddenException) | `403` | `404` (EF filter) | `404` | `404` | `404` | No |
| 18 | POST `/api/v1/cover-letter-templates` | `CoverLetterTemplatesController.Create` | `CreateCoverLetterTemplateCommand` | `CreateCoverLetterTemplateCommandHandler` | n/a | n/a | n/a | n/a | n/a | n/a | No |
| 19 | GET `/api/v1/cover-letter-templates` | `CoverLetterTemplatesController.ListAsync` | `ListCoverLetterTemplatesQuery` | `ListCoverLetterTemplatesQueryHandler` | scoped: `pagedQuery.UserId = request.UserId` → `BaseRepository.GetAsync` | scoped | excluded by EF filter | excluded | empty list | empty list | No |
| 20 | GET `/api/v1/cover-letter-templates/{id}` | `CoverLetterTemplatesController.GetAsync` | `GetCoverLetterTemplateQuery` | `GetCoverLetterTemplateQueryHandler` | `404` (NotFoundException — "CoverLetterTemplate") | `404` | `404` (EF filter) | `404` | `404` | `404` | No |
| 21 | PATCH `/api/v1/cover-letter-templates/{id}` | `CoverLetterTemplatesController.UpdateAsync` | `UpdateCoverLetterTemplateCommand` | `UpdateCoverLetterTemplateCommandHandler` | `403` (ForbiddenException) | `403` | `404` (EF filter) | `404` | `404` | `404` | No |
| 22 | DELETE `/api/v1/cover-letter-templates/{id}` | `CoverLetterTemplatesController.DeleteAsync` | `DeleteCoverLetterTemplateCommand` | `DeleteCoverLetterTemplateCommandHandler` | `403` (ForbiddenException) | `403` | `404` (EF filter) | `404` | `404` | `404` | No |
| 23 | POST `/api/v1/vacancies/filter` | `VacanciesController.FilterAsync` | `FilterVacanciesQuery` | `FilterVacanciesQueryHandler` | scoped: `pagedQuery.UserId = request.UserId` → `VacancyRepository.GetFilteredAsync` applies `v.UserId == userId` | scoped | excluded by EF filter | excluded | empty list | empty list | No |
| 24 | GET `/api/v1/vacancies/{id}` | `VacanciesController.GetAsync` | `GetVacancyQuery` | `GetVacancyQueryHandler` | `404` (NotFoundException — "Vacancy") | `404` | `404` (EF filter) | `404` | `404` | `404` | No |
| 25 | POST `/api/v1/cover-letters` | `CoverLettersController.CreateAsync` | `CreateCoverLetterCommand` | `CreateCoverLetterCommandHandler` | `404` for cross-user vacancy (NotFoundException — "Vacancy") | `404` | `404` for soft-deleted vacancy (EF filter on `GetByIdAsync`) | `404` | `404` | `404` | No |
| 26 | GET `/api/v1/cover-letters` | `CoverLettersController.ListAsync` | `ListCoverLettersQuery` | `ListCoverLettersQueryHandler` | scoped: `pagedQuery.UserId = request.UserId` → `CoverLetterRepository.GetPagedListAsync` applies `cl.UserId == userId` | scoped | excluded: `!cl.IsDeleted` explicit + EF filter | excluded | empty list | empty list | No |
| 27 | GET `/api/v1/cover-letters/{id}` | `CoverLettersController.GetByIdAsync` | `GetCoverLetterQuery` | `GetCoverLetterQueryHandler` | `404` (NotFoundException — "CoverLetter"; variant: `result is null \|\| result.UserId != request.UserId`) | `404` | `404` (`GetDetailByIdAsync` applies `!cl.IsDeleted` explicitly; returns null → handler throws NotFoundException) | `404` | `404` | `404` | No |
| 28 | PATCH `/api/v1/cover-letters/{id}` | `CoverLettersController.UpdateAsync` | `UpdateCoverLetterCommand` | `UpdateCoverLetterCommandHandler` | `403` (ForbiddenException) | `403` | `404` (EF filter) | `404` | `404` | `404` | No |
| 29 | DELETE `/api/v1/cover-letters/{id}` | `CoverLettersController.DeleteAsync` | `DeleteCoverLetterCommand` | `DeleteCoverLetterCommandHandler` | `403` (ForbiddenException) | `403` | `404` (EF filter) | `404` | `404` | `404` | No |

---

## Gaps and Closures

Only **one gap** was found during the audit. All handler-level ownership guards and repository-level user-scoping checks are correctly implemented. The gap is purely in OpenAPI metadata.

### Gap R.2-G1 — `DELETE /api/v1/resumes/{id}` falsely advertised `400 Bad Request`

**File:** `backend/src/JobNecto.API/Controllers/ResumesController.cs`, `DeleteAsync` action  
**Discovery:** Task 9 (OpenAPI reconciliation)  
**Severity:** Low — incorrect OpenAPI metadata only; no behavioral impact  

`ResumesController.DeleteAsync` carried `[ProducesResponseType(StatusCodes.Status400BadRequest)]`. The handler (`DeleteResumeCommandHandler`) has no path that produces a `400`:
- Missing id → `NotFoundException` → `404`
- Cross-user → `ForbiddenException` → `403`
- Owner delete → `SoftDeleteAsync` + `SaveChangesAsync` → `204`

There is no validator for `DeleteResumeCommand` and no body is accepted (id comes from route). The `400` attribute was a false advertisement, inconsistent with all other DELETE endpoints in the codebase.

**Closure:** Removed `[ProducesResponseType(StatusCodes.Status400BadRequest)]` from `ResumesController.DeleteAsync`. Attribute set now matches the canonical contract: `204, 401, 403, 404`. No test added — this is a metadata-only change with no behavioral logic.

---

## Open Items for R.4

R.4 owns the formal `403`/`404` contract matrix. The following items are surface-level ambiguities or design choices that are consistent today but have not been formally documented for external consumers.

1. **Soft-deleted resource the caller previously owned (GET/PATCH/DELETE):** The EF global filter makes a caller's own soft-deleted resource return `404`, identical to a resource that never existed. Callers cannot tell the difference between "you deleted this" and "this ID was never yours." This is currently intentional (no feature exposes restore). R.4 should ratify this as the canonical behavior so it is not confused with a bug.

2. **POST `/api/v1/cover-letters` when the referenced vacancy is soft-deleted:** The EF global filter on `VacancyRepository.GetByIdAsync` causes a `NotFoundException` → `404`, even if the vacancy was previously owned by the caller. A caller trying to create a cover letter for a vacancy they soft-deleted receives `404`. Whether this should be a specialized error (e.g., `410 Gone`) or remain `404` is an open design choice. R.4 should finalize.

3. **Cover-letter list includes soft-deleted vacancy snapshot (`IgnoreQueryFilters()` in `GetPagedListAsync`):** The `GET /api/v1/cover-letters` list uses `IgnoreQueryFilters()` on the vacancy join, so cover letters associated with soft-deleted vacancies still show vacancy title/company in the list item. This is intentional historical snapshot behavior. R.4's matrix should annotate this explicitly so it is not treated as a leak.

4. **`POST /api/v1/users/token/refresh` — no user-existence check:** The controller generates a fresh JWT from `HttpContext.GetCurrentUserId()` without a database lookup. A valid JWT for a soft-deleted user will still refresh successfully. R.4 may want to add a user-existence guard here, or explicitly document "token is valid until expiry regardless of soft-delete."

5. **Anonymous endpoints out of scope:** `POST /api/v1/users` (registration) has no `[Authorize]` and no ownership to audit. R.4's contract matrix should include a "not applicable" row for completeness of the API surface.
