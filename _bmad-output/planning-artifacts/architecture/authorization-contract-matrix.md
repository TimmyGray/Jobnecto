# Authorization Contract Matrix (Story R.4)

_Authored: 2026-05-23. Baseline: commit `d77756e` and R.2 audit (2026-05-21). All 14 user-scoped detail/update/delete endpoints verified._

**Purpose:** Single source of truth for the canonical HTTP status code each user-scoped endpoint returns across the three standard failure scenarios (not-found, soft-deleted, cross-user). Consumed by: API clients (error-handling, retry logic), R.3 authorization regression suite (expected status codes), and future stories (auditing drift from canonical behavior).

---

## Canonical Behaviors

Three failure modes are codified here; their outcomes are deliberately non-distinguishable at the wire in several cases to prevent information disclosure.

**Detail GET-by-id (rows 1, 4, 7, 10, 13) — cross-user → `404 NotFound`**
Returning `403` on a cross-user GET would leak resource existence to the caller. The canonical contract is `404`, indistinguishable from "this id has never existed." Handler throws `NotFoundException("{Entity}", id)`.

**PATCH / DELETE (rows 2, 3, 5, 6, 8, 9, 11, 12) — cross-user → `403 Forbidden`**
For mutations, the caller is authenticated and the resource existence is implied by the route shape (`/{id}`). Returning `403` communicates "you are authenticated but lack permission" without leaking new information. Handler throws `ForbiddenException(...)`. Both not-found and soft-deleted produce `404` because `GetByIdAsync` (backed by the EF global query filter) throws `NotFoundException` before the ownership check is reached.

**POST `/api/v1/cover-letters` (row 14) — all three scenarios → `404 NotFound`**
The request body carries `VacancyId`, a foreign key. Returning `403` would leak the vacancy's existence to a caller who does not own it. The handler throws `NotFoundException("Vacancy", VacancyId)` for all three sub-cases (not-found, soft-deleted, cross-user), indistinguishable at the wire.

**Soft-deleted resources owned by the caller — canonical `404`**
The EF global query filter (`HasQueryFilter(e => !e.IsDeleted)`) excludes soft-deleted rows from `GetByIdAsync`. The filter runs before any handler ownership check, so the caller cannot distinguish "I deleted this" from "this id has never existed." This is an intentional design choice that extends existence non-leakage to resource lifecycle.

---

## Excluded Endpoints

| Endpoint Group | Rationale |
|---|---|
| `/api/v1/users/me*` (GET, PATCH, POST/PUT/DELETE avatar) | No `{id}` route parameter; `UserId` is always JWT-bound. No cross-user vector by construction. Covered by R.2 AC 9. |
| Anonymous endpoints (`POST /api/v1/users`, `POST /api/v1/users/token/refresh`) | No `[Authorize]`; no ownership to enforce. |
| List / filter endpoints (`GET /api/v1/resumes`, educations, cover-letter-templates, cover-letters; `POST /api/v1/vacancies/filter`) | No `{id}` route parameter or foreign-key body subject to the matrix. User-scoping is enforced at the repository layer via `pagedQuery.UserId` predicate (FR27). Covered by R.2 AC 3. |

---

## Exception Mapping

Verbatim from `backend/src/JobNecto.API/Infrastructure/ExceptionHandling/GlobalExceptionHandler.cs`:

| Exception type | HTTP status | Response body `detail` |
|---|---|---|
| `NotFoundException` | `404 Not Found` | `"Resource not found."` (generic — existence non-leakage preserved) |
| `ForbiddenException` | `403 Forbidden` | `"You do not have permission to access this resource."` |
| `ConflictException` | `409 Conflict` | exception `.Message` (entity-specific) |
| `UnauthorizedException` | `401 Unauthorized` | `"Authentication is required to access this resource."` |
| `ValidationException` (FluentValidation) | `400 Bad Request` | `"One or more validation errors occurred."` + `errors` extension |
| `DbUpdateException` (unique violation) | `409 Conflict` | `"A unique constraint was violated."` |

No mapping drift found between this table and the handler exception choices; the matrix status codes are derived solely from this mapper.

---

## Matrix

**Scenario definitions:**

- **not-found**: the `{id}` route parameter (or body `VacancyId` for row 14) refers to a `Guid` that has never existed in the database.
- **soft-deleted**: the resource existed but `IsDeleted = true`; the EF global query filter excludes it from `GetByIdAsync`. May or may not be owned by the caller — the filter runs before the ownership check, so the two sub-cases are indistinguishable at the wire.
- **cross-user**: the resource exists, is not soft-deleted, and is owned by a different user (`entity.UserId != request.UserId`).

| # | Endpoint | not-found | soft-deleted | cross-user | Exception path | Test Reference |
|---|---|---|---|---|---|---|
| 1 | `GET /api/v1/resumes/{id}` | `404` | `404` | `404` | `NotFoundException("Resume", id)` | `GetResumeHandlerTests.Handle_NotFound_PropagatesNotFoundException`; `Handle_WrongUser_ThrowsNotFoundException`; `GetResumeQueryHandlerTests.Handle_CrossUserAccess_ThrowsNotFoundException` |
| 2 | `PATCH /api/v1/resumes/{id}` | `404` | `404` | `403` | `NotFoundException` from `GetByIdAsync` (not-found, soft-deleted); `ForbiddenException` (cross-user) | `UpdateResumeCommandHandlerTests.Handle_ResumeNotFound_PropagatesNotFoundException`; `Handle_CrossUserUpdate_ThrowsForbiddenException` |
| 3 | `DELETE /api/v1/resumes/{id}` | `404` | `404` | `403` | same as PATCH | `DeleteResumeCommandHandlerTests.Handle_ResumeNotFound_PropagatesNotFoundException`; `Handle_CrossUserDelete_ThrowsForbiddenException` |
| 4 | `GET /api/v1/educations/{id}` | `404` | `404` | `404` | `NotFoundException("Education", id)` | `GetEducationQueryHandlerTests.Handle_RecordNotFound_PropagatesNotFoundException`; `Handle_CrossUserAccess_ThrowsNotFoundException` |
| 5 | `PATCH /api/v1/educations/{id}` | `404` | `404` | `403` | `NotFoundException` / `ForbiddenException` | `UpdateEducationCommandHandlerTests.Handle_RecordNotFound_PropagatesNotFoundException`; `Handle_CrossUserUpdate_ThrowsForbiddenException` |
| 6 | `DELETE /api/v1/educations/{id}` | `404` | `404` | `403` | same as PATCH | `DeleteEducationCommandHandlerTests.Handle_RecordNotFound_PropagatesNotFoundException`; `Handle_CrossUserDelete_ThrowsForbiddenException` |
| 7 | `GET /api/v1/cover-letter-templates/{id}` | `404` | `404` | `404` | `NotFoundException("CoverLetterTemplate", id)` | `GetCoverLetterTemplateQueryHandlerTests.Handle_NonExistentId_PropagatesNotFoundException`; `Handle_CrossUserAccess_ThrowsNotFoundException` |
| 8 | `PATCH /api/v1/cover-letter-templates/{id}` | `404` | `404` | `403` | `NotFoundException` / `ForbiddenException` | `UpdateCoverLetterTemplateCommandHandlerTests.Handle_NotFound_PropagatesNotFoundException`; `Handle_CrossUserUpdate_ThrowsForbiddenException` |
| 9 | `DELETE /api/v1/cover-letter-templates/{id}` | `404` | `404` | `403` | same as PATCH | `DeleteCoverLetterTemplateCommandHandlerTests.Handle_TemplateNotFound_PropagatesNotFoundException`; `Handle_CrossUserDelete_ThrowsForbiddenException` |
| 10 | `GET /api/v1/cover-letters/{id}` | `404` | `404` | `404` | `NotFoundException("CoverLetter", id)` — note: handler uses `GetDetailByIdAsync` + null-check variant; 404 contract identical | `GetCoverLetterQueryHandlerTests.Handle_NotFound_ThrowsNotFoundException`; `Handle_CrossUserAccess_ThrowsNotFoundException` |
| 11 | `PATCH /api/v1/cover-letters/{id}` | `404` | `404` | `403` | `NotFoundException` / `ForbiddenException` | `UpdateCoverLetterCommandHandlerTests.Handle_NotFound_PropagatesNotFoundException`; `Handle_CrossUserUpdate_ThrowsForbiddenException` |
| 12 | `DELETE /api/v1/cover-letters/{id}` | `404` | `404` | `403` | same as PATCH | `DeleteCoverLetterCommandHandlerTests.Handle_NotFound_PropagatesNotFoundException`; `Handle_CrossUserDelete_ThrowsForbiddenException` |
| 13 | `GET /api/v1/vacancies/{id}` | `404` | `404` | `404` | `NotFoundException("Vacancy", id)` | `GetVacancyQueryHandlerTests.Handle_VacancyNotFound_PropagatesNotFoundException`; `Handle_VacancyOwnedByDifferentUser_ThrowsNotFoundException` |
| 14 | `POST /api/v1/cover-letters` (with `VacancyId` body) | `404` | `404` | `404` | `NotFoundException("Vacancy", VacancyId)` for all three; existence non-leakage applies to the body-supplied FK | `CreateCoverLetterCommandHandlerTests.Handle_VacancyNotFound_ThrowsNotFoundException`; `Handle_VacancyOwnedByDifferentUser_ThrowsNotFoundException` |

**Notes:**
- Row 10 (`GET /api/v1/cover-letters/{id}`) uses `GetDetailByIdAsync` + `if (result is null || result.UserId != request.UserId)` pattern instead of `GetByIdAsync` + `NotFoundException` propagation. The wire contract (`404` for all three failure modes) is identical to the other detail GET-by-id rows.
- Row 14 cross-user is `404` intentionally — the existence of another user's vacancy must not be disclosed. This matches the detail GET-by-id contract extended to body-supplied foreign keys.
- `409 Conflict` (duplicate cover letter for the same vacancy) is **not** a matrix cell — it is a uniqueness contract, not a not-found / soft-deleted / cross-user scenario.
- Vacancies have no PATCH or DELETE endpoint; the matrix has only a GET row (13) and the FK-body row (14).

---

## How Soft-Deleted is Tested at the Handler Layer

At the handler unit-test layer, the **soft-deleted scenario is identical to the not-found scenario**. The reason: `GetByIdAsync` is backed by EF Core with a global query filter (`HasQueryFilter(e => !e.IsDeleted)`). In production, the filter causes the repository to return `null` for soft-deleted rows, which the `BaseRepository.GetByIdAsync` turns into a `NotFoundException`. In tests, the repository is replaced by a `Mock<I...Repository>`, and the mock is configured to return `null` (or throw `NotFoundException` directly) — the mock simulates the same exclusion the EF filter performs in production.

Therefore: one `Handle_WhenEntityNotFound_PropagatesNotFoundException` test covers both the not-found and soft-deleted cells simultaneously. HTTP-level distinction between not-found and soft-deleted (where they may diverge, e.g. for row 14 with a caller-owned soft-deleted vacancy) is covered by R.3's authorization regression suite, not by these handler unit tests.

---

## OpenAPI Notes — 409 on PATCH /api/v1/cover-letter-templates/{id}

`PATCH /api/v1/cover-letter-templates/{id}` advertises `[ProducesResponseType(StatusCodes.Status409Conflict)]` in addition to the 403/404 cells in the matrix above. This is NOT a matrix scenario (it is a uniqueness contract, not a not-found/soft-deleted/cross-user scenario), but it is a real reachable response:

- The database enforces a unique partial index `IX_CoverLetterTemplates_UserId_Name` on `(UserId, Name)` where `IsDeleted = false`.
- `UpdateCoverLetterTemplateCommandHandler` calls `SaveChangesAsync` without an application-level conflict guard; a rename collision causes `DbUpdateException` with a unique violation.
- `GlobalExceptionHandler.IsUniqueConstraintViolation` maps this to `409 Conflict` with `"A unique constraint was violated."`.
- The `409` attribute is therefore correctly advertised on this PATCH action; it should not be confused with the three-scenario matrix cells.

## Refinements over R.2

None. Every status-code cell in this matrix is consistent with the behaviors affirmed in R.2 (endpoint-ownership-audit.md "Canonical Behaviors" table). No canonical behavior was changed or narrowed by R.4.

---

## Open Items

None. All 14 matrix rows conform. Handler-level conformance was verified against every row in Task 3; no deviations were found. OpenAPI attribute conformance was verified in Task 6; one false `409` attribute was removed from `CoverLetterTemplatesController.UpdateAsync`.

Items from R.2's "Open Items for R.4" that are addressed by this matrix:

1. **Soft-deleted resource the caller previously owned (GET/PATCH/DELETE):** Ratified in this matrix as canonical `404`. The EF global filter is the authoritative boundary; callers cannot distinguish "I deleted this" from "never existed." See "How Soft-Deleted is Tested at the Handler Layer" above.
2. **POST `/api/v1/cover-letters` when the referenced vacancy is soft-deleted:** Ratified as `404` (row 14). The EF filter on `VacancyRepository.GetByIdAsync` produces `NotFoundException` indistinguishable from not-found or cross-user.

R.2 Open Items 3 (cover-letter list `IgnoreQueryFilters` on vacancy join), 4 (token/refresh no user-existence check), and 5 (anonymous endpoints) are explicitly **out of scope** for the matrix; they involve list behavior, token lifecycle, and unauthenticated flows — none of which map to the three matrix scenarios (not-found / soft-deleted / cross-user on a single-resource endpoint with an `{id}` parameter or `VacancyId` body FK).
