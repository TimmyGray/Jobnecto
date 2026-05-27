# Story R.4: Consistent Forbidden vs NotFound Contract Matrix

Status: done

GitHub Issue: TBD

## Story

As an **API consumer**,
I want predictable `403 Forbidden` vs `404 NotFound` behavior for every user-scoped detail / update / delete endpoint across the three canonical failure scenarios (not-found, soft-deleted, cross-user),
so that client retry logic, error UI, and security expectations are stable, the matrix is a single source of truth that R.3's integration suite can reference, and any endpoint that deviates from the matrix is corrected in this epic — without re-discovery downstream.

## Acceptance Criteria

1. A **canonical contract matrix** document exists at `_bmad-output/planning-artifacts/architecture/authorization-contract-matrix.md` enumerating every active user-scoped detail / update / delete endpoint (and the one create endpoint that accepts a foreign-key body, `POST /api/v1/cover-letters`) against the three canonical failure scenarios: **not-found**, **soft-deleted**, **cross-user**. For each (endpoint × scenario) cell the doc records the canonical HTTP status code, the exception type raised in the handler / repository layer, and a one-line rationale (existence non-leakage vs explicit forbidden, etc.).
2. The matrix in AC 1 is **also embedded inside this story file** under `## Contract Matrix (Canonical)` — same rows, same columns — so a developer reviewing the story can read the matrix without context-switching, and so the story's `Tasks/Subtasks` can be cited directly against it.
3. The matrix is **a strict superset of, and consistent with, the canonical behaviors affirmed in R.2** (Story R.2 ACs 6, 7, 8, and the "Canonical Behaviors To Affirm" table in its Dev Notes). R.4 must not introduce a new contract that contradicts an R.2 affirmation; any deliberate refinement is listed under "Refinements over R.2" in the matrix doc with rationale.
4. The endpoint list in the matrix is derived from `_bmad-output/project-context.md` "Active HTTP endpoints" intersected with R.2's audit scope (Story R.2 Dev Notes "Inventory of Endpoints in Scope" table, rows 10–12, 15–17, 20–22, 24, 25, 27–29). List endpoints, anonymous endpoints (`POST /api/v1/users`, `POST /api/v1/users/token/refresh`), and `/api/v1/users/me*` are **explicitly excluded** with a short rationale ("no `{id}` route parameter or foreign-key body to subject to the matrix; covered separately by R.2 AC 9").
5. For every (endpoint × scenario) cell, runtime behavior is **verified by at least one test** (handler unit test under `backend/tests/JobNecto.Tests/Application/` OR HTTP-level integration test under `backend/tests/JobNecto.Tests/API/Authorization/` from R.3). The matrix doc includes a column citing the test method name(s) that prove conformance for each cell; if a cell has no covering test, a new focused test is added in this story to close the gap.
6. The `GlobalExceptionHandler` exception-to-status mapping is **inspected and documented** in the matrix doc's "Exception Mapping" section: `NotFoundException` → `404`, `ForbiddenException` → `403`, `ConflictException` → `409`, `UnauthorizedException` → `401`, `ValidationException` → `400`. The matrix relies on this mapping for status-code citations; any drift between handler exception choice and matrix expectation is corrected in this story (code fix in the handler, not in the mapper).
7. **Soft-deleted resources owned by the caller**: the canonical behavior is `404 NotFound` (indistinguishable from missing or cross-user 404), because the EF global query filter excludes soft-deleted rows from `GetByIdAsync` before the handler can inspect ownership. The matrix codifies this as canonical and lists it as a deliberate design choice (existence non-leakage extended to lifecycle). If any handler currently behaves differently (e.g. catches the filter to return `410 Gone` or `200` for a soft-deleted-but-owned resource), that handler is flagged as deviating and is reconciled to the canonical `404` in this story.
8. **Cross-user detail GET-by-id**: canonical `404 NotFound` with `NotFoundException`. Any handler that currently returns `403 Forbidden` (or maps to `ForbiddenException`) on a cross-user GET-by-id is corrected to throw `NotFoundException` and a focused unit test is added to lock the corrected behavior. (Per R.2 AC 6, no such deviation is expected as of commit `d77756e`, but R.4 verifies and locks it.)
9. **Cross-user PATCH / DELETE**: canonical `403 Forbidden` with `ForbiddenException` when the resource exists and is owned by another user; canonical `404 NotFound` when the resource id does not exist or is soft-deleted (because `GetByIdAsync` throws first, before the ownership check). Any handler that deviates is corrected and locked with a test.
10. **POST `/api/v1/cover-letters` with a foreign-key `VacancyId`**: canonical `404 NotFound` for cross-user vacancy AND soft-deleted vacancy (both surface as `NotFoundException("Vacancy", VacancyId)` because the EF query filter and the explicit ownership check both produce the same response); canonical `404 NotFound` for non-existent vacancy id. The matrix codifies all three sub-cases as `404`.
11. **OpenAPI `[ProducesResponseType]` attributes** on every endpoint in the matrix are reconciled against the canonical status codes: detail GET-by-id actions advertise `200`, `401`, `404` (and must **not** advertise `403`); PATCH and DELETE actions advertise `200`/`204`, `400`, `401`, `403`, `404`; `POST /api/v1/cover-letters` advertises `201`, `400`, `401`, `404`, `409`. Any attribute mismatch surfaced by R.2 Task 9 that was deferred to R.4 (per R.2 AC 12) is closed in this story.
12. **Swashbuckle response examples** (if any are wired via XML doc `<response>` tags or `[SwaggerResponseExample]` analog) are inspected for the affected endpoints; if an example contradicts the matrix (e.g. shows a `403` body shape on a GET endpoint that canonically returns `404`), the example is removed or corrected. If no examples exist for the affected endpoints, the matrix doc records "no Swashbuckle examples wired — attributes are the contract source".
13. The matrix doc is **linked from `_bmad-output/planning-artifacts/architecture/index.md`** under a new "Contract Matrices" or appropriate existing section so that future stories (and R.3's regression suite) can discover it without searching.
14. R.3's authorization regression suite (`backend/tests/JobNecto.Tests/API/Authorization/`), once implemented, MUST use this matrix as the source of truth for expected status codes. R.4's matrix doc is therefore written in a stable, parseable form (one markdown table per resource OR one consolidated table with `Endpoint`, `Scenario`, `Canonical Status`, `Exception`, `Rationale`, `Test Reference` columns) so R.3 tests can be cross-referenced by row.
15. `dotnet build backend/JobNecto.slnx --configuration Release --warnaserror` passes.
16. `dotnet test backend/JobNecto.slnx --configuration Release --warnaserror` passes; any new or modified handler unit tests added in this story are included in the run.

## Contract Matrix (Canonical)

The following matrix is the canonical source of truth. The permanent doc at `_bmad-output/planning-artifacts/architecture/authorization-contract-matrix.md` mirrors this verbatim (and adds the `Test Reference` column once tests are confirmed).

**Scenario definitions:**

- **not-found**: the `{id}` route parameter (or body `VacancyId` for POST cover-letter) refers to a `Guid` that has never existed in the database.
- **soft-deleted**: the resource existed but its `IsDeleted = true`; the EF global query filter excludes it from `GetByIdAsync`. May or may not be owned by the caller — the filter runs before the ownership check, so the two sub-cases are indistinguishable at the wire.
- **cross-user**: the resource exists, is not soft-deleted, and is owned by a different user than the caller (`entity.UserId != request.UserId`).

| # | Endpoint | not-found | soft-deleted | cross-user | Exception path | Test Reference |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | `GET /api/v1/resumes/{id}` | `404` | `404` | `404` | `NotFoundException("Resume", id)` | `GetResumeHandlerTests`: `Handle_NotFound_PropagatesNotFoundException`, `Handle_WrongUser_ThrowsNotFoundException` |
| 2 | `PATCH /api/v1/resumes/{id}` | `404` | `404` | `403` | `NotFoundException` from `GetByIdAsync` (not-found, soft-deleted); `ForbiddenException` (cross-user) | `UpdateResumeCommandHandlerTests`: `Handle_ResumeNotFound_PropagatesNotFoundException`, `Handle_CrossUserUpdate_ThrowsForbiddenException` |
| 3 | `DELETE /api/v1/resumes/{id}` | `404` | `404` | `403` | same as PATCH | `DeleteResumeCommandHandlerTests`: `Handle_ResumeNotFound_PropagatesNotFoundException`, `Handle_CrossUserDelete_ThrowsForbiddenException` |
| 4 | `GET /api/v1/educations/{id}` | `404` | `404` | `404` | `NotFoundException("Education", id)` | `GetEducationQueryHandlerTests`: `Handle_RecordNotFound_PropagatesNotFoundException`, `Handle_CrossUserAccess_ThrowsNotFoundException` |
| 5 | `PATCH /api/v1/educations/{id}` | `404` | `404` | `403` | `NotFoundException` / `ForbiddenException` | `UpdateEducationCommandHandlerTests`: `Handle_RecordNotFound_PropagatesNotFoundException`, `Handle_CrossUserUpdate_ThrowsForbiddenException` |
| 6 | `DELETE /api/v1/educations/{id}` | `404` | `404` | `403` | same as PATCH | `DeleteEducationCommandHandlerTests`: `Handle_RecordNotFound_PropagatesNotFoundException`, `Handle_CrossUserDelete_ThrowsForbiddenException` |
| 7 | `GET /api/v1/cover-letter-templates/{id}` | `404` | `404` | `404` | `NotFoundException("CoverLetterTemplate", id)` | `GetCoverLetterTemplateQueryHandlerTests`: `Handle_NonExistentId_PropagatesNotFoundException`, `Handle_CrossUserAccess_ThrowsNotFoundException` |
| 8 | `PATCH /api/v1/cover-letter-templates/{id}` | `404` | `404` | `403` | `NotFoundException` / `ForbiddenException` | `UpdateCoverLetterTemplateCommandHandlerTests`: `Handle_NotFound_PropagatesNotFoundException`, `Handle_CrossUserUpdate_ThrowsForbiddenException` |
| 9 | `DELETE /api/v1/cover-letter-templates/{id}` | `404` | `404` | `403` | same as PATCH | `DeleteCoverLetterTemplateCommandHandlerTests`: `Handle_TemplateNotFound_PropagatesNotFoundException`, `Handle_CrossUserDelete_ThrowsForbiddenException` |
| 10 | `GET /api/v1/cover-letters/{id}` | `404` | `404` | `404` | `NotFoundException("CoverLetter", id)` | `GetCoverLetterQueryHandlerTests`: `Handle_NotFound_ThrowsNotFoundException`, `Handle_CrossUserAccess_ThrowsNotFoundException` |
| 11 | `PATCH /api/v1/cover-letters/{id}` | `404` | `404` | `403` | `NotFoundException` / `ForbiddenException` | `UpdateCoverLetterCommandHandlerTests`: `Handle_NotFound_PropagatesNotFoundException`, `Handle_CrossUserUpdate_ThrowsForbiddenException` |
| 12 | `DELETE /api/v1/cover-letters/{id}` | `404` | `404` | `403` | same as PATCH | `DeleteCoverLetterCommandHandlerTests`: `Handle_NotFound_PropagatesNotFoundException`, `Handle_CrossUserDelete_ThrowsForbiddenException` |
| 13 | `GET /api/v1/vacancies/{id}` | `404` | `404` | `404` | `NotFoundException("Vacancy", id)` | `GetVacancyQueryHandlerTests`: `Handle_VacancyNotFound_PropagatesNotFoundException`, `Handle_VacancyOwnedByDifferentUser_ThrowsNotFoundException` |
| 14 | `POST /api/v1/cover-letters` (with `VacancyId` body) | `404` | `404` | `404` | `NotFoundException("Vacancy", VacancyId)` for all three; existence non-leakage applies to the body-supplied FK | `CreateCoverLetterCommandHandlerTests`: `Handle_VacancyNotFound_ThrowsNotFoundException`, `Handle_VacancyOwnedByDifferentUser_ThrowsNotFoundException` |

**Notes on the matrix:**

- Row 14's `404` for the cross-user case is intentional — the existence of another user's vacancy is non-leaking, mirroring the detail GET-by-id contract.
- Vacancies have no PATCH or DELETE endpoint, so the matrix has only the GET row for them and the FK-body row for cover-letter create.
- `409 Conflict` (duplicate cover-letter for the same vacancy) is **not** part of the matrix because it is a uniqueness contract, not a not-found / soft-deleted / cross-user scenario.
- `/api/v1/users/me*` endpoints are excluded (AC 4) — they have no `{id}` route parameter; the JWT supplies the user id. R.2 AC 9 already affirms the absence of a cross-user vector.

## Tasks / Subtasks

- [x] Task 1: Read inputs and confirm matrix dimensions (AC: 4)
  - [x] Read `_bmad-output/project-context.md` "Active HTTP endpoints" and confirm the 14 endpoints in the matrix above remain the right scope (no endpoint added or removed since 2026-05-11)
  - [x] Read `_bmad-output/archive/implementation-artifacts/r-2-endpoint-ownership-policy-audit-and-gap-closure.md` Dev Notes "Inventory of Endpoints in Scope" (rows 10–12, 15–17, 20–22, 24, 25, 27–29) and confirm alignment
  - [x] If R.2 has progressed and its audit doc `_bmad-output/planning-artifacts/architecture/endpoint-ownership-audit.md` exists, read its "Open Items for R.4" section (per R.2 AC 12) and merge those items into Task 4's reconciliation list
  - [x] If R.2 is still `ready-for-dev` (no audit doc yet), proceed with the inventory baseline from R.2 Dev Notes — note in the Dev Agent Record which input source was used

- [x] Task 2: Verify exception → status mapping (AC: 6)
  - [x] Read `backend/src/JobNecto.API/Infrastructure/ExceptionHandling/GlobalExceptionHandler.cs`
  - [x] Confirm the mapping: `NotFoundException` → `404`, `ForbiddenException` → `403`, `ConflictException` → `409`, `UnauthorizedException` → `401`, `ValidationException` → `400`
  - [x] Confirm the response detail string for `NotFoundException` is generic ("Resource not found." or equivalent — see R.2 Dev Notes) so existence non-leakage is preserved
  - [x] If any mapping drift is found (e.g. `NotFoundException` accidentally mapped to `400`), STOP and log it under `## Open Questions`; coordinate with R.2 before continuing (mapper changes are out of scope for R.4 unless R.2's audit explicitly delegates them)
  - [x] Record the mapping verbatim in the matrix doc's "Exception Mapping" section

- [x] Task 3: Verify per-handler conformance to the matrix (AC: 5, 7, 8, 9, 10)
  - [x] For each of the 14 matrix endpoints, read the handler and verify the exception path:
    - Detail GET-by-id (rows 1, 4, 7, 10, 13): handler calls `GetByIdAsync(id, ct)` (throws `NotFoundException` for not-found and soft-deleted via EF filter), then `if (entity.UserId != request.UserId) throw new NotFoundException(...)` for cross-user
    - PATCH / DELETE (rows 2, 3, 5, 6, 8, 9, 11, 12): handler calls `GetByIdAsync` first (`NotFoundException` for not-found/soft-deleted), then `if (entity.UserId != request.UserId) throw new ForbiddenException(...)` for cross-user
    - `POST /api/v1/cover-letters` (row 14): handler calls `_unitOfWork.VacancyRepository.GetByIdAsync(VacancyId, ct)` and applies `if (vacancy.UserId != request.UserId) throw new NotFoundException("Vacancy", VacancyId)` so all three sub-scenarios surface as `404`
  - [x] Specifically read these handler files (each is small — single responsibility):
    - `backend/src/JobNecto.Application/Resumes/GetResumeQueryHandler.cs`
    - `backend/src/JobNecto.Application/Resumes/UpdateResumeCommandHandler.cs`
    - `backend/src/JobNecto.Application/Resumes/DeleteResumeCommandHandler.cs`
    - `backend/src/JobNecto.Application/Educations/GetEducationQueryHandler.cs`
    - `backend/src/JobNecto.Application/Educations/UpdateEducationCommandHandler.cs`
    - `backend/src/JobNecto.Application/Educations/DeleteEducationCommandHandler.cs`
    - `backend/src/JobNecto.Application/CoverLetterTemplates/GetCoverLetterTemplateQueryHandler.cs`
    - `backend/src/JobNecto.Application/CoverLetterTemplates/UpdateCoverLetterTemplateCommandHandler.cs`
    - `backend/src/JobNecto.Application/CoverLetterTemplates/DeleteCoverLetterTemplateCommandHandler.cs`
    - `backend/src/JobNecto.Application/CoverLetters/GetCoverLetterQueryHandler.cs`
    - `backend/src/JobNecto.Application/CoverLetters/UpdateCoverLetterCommandHandler.cs`
    - `backend/src/JobNecto.Application/CoverLetters/DeleteCoverLetterCommandHandler.cs`
    - `backend/src/JobNecto.Application/CoverLetters/CreateCoverLetterCommandHandler.cs`
    - `backend/src/JobNecto.Application/Vacancies/GetVacancyQueryHandler.cs`
  - [x] For each handler, record "conforms" or "deviates" in a per-row note in the matrix doc; if deviates, capture the actual vs. expected behavior

- [x] Task 4: Reconcile any deviation (code fix) (AC: 7, 8, 9, 10)
  - [x] For every "deviates" row from Task 3, modify the handler to match the matrix — minimal change, preserve all unrelated logic
  - [x] Common shapes to expect (only apply if Task 3 surfaces them):
    - A GET-by-id handler that throws `ForbiddenException` on cross-user (deviation) → replace with `NotFoundException("{Entity}", id)`
    - A PATCH / DELETE handler that throws `NotFoundException` on cross-user (deviation) → replace with `ForbiddenException("You do not have permission to ...")`
    - A `CreateCoverLetterCommandHandler` path that throws `ConflictException` or `ForbiddenException` on cross-user vacancy (deviation) → replace with `NotFoundException("Vacancy", VacancyId)`
  - [x] If R.2 already closed the deviation (per its Task 8), there is nothing to fix here — Task 4 is a no-op verification step in that case; document in the Dev Agent Record
  - [x] Do NOT modify `GlobalExceptionHandler` or any middleware; corrections live in the handler

- [x] Task 5: Add or extend handler unit tests to lock the matrix (AC: 5)
  - [x] For every cell in the matrix, confirm there is at least one test asserting the canonical exception type (`Mock<I{Mutable|Vacancy|User}Repository>` + `await act.Should().ThrowAsync<{NotFoundException|ForbiddenException}>()` per R.1 / R.2 patterns)
  - [x] Coverage audit — check these existing test files for each cell:
    - `backend/tests/JobNecto.Tests/Application/Resumes/GetResumeHandlerTests.cs` — rows 1
    - `backend/tests/JobNecto.Tests/Application/Resumes/UpdateResumeCommandHandlerTests.cs` — row 2
    - `backend/tests/JobNecto.Tests/Application/Resumes/DeleteResumeCommandHandlerTests.cs` — row 3
    - `backend/tests/JobNecto.Tests/Application/Educations/GetEducationQueryHandlerTests.cs` — row 4
    - `backend/tests/JobNecto.Tests/Application/Educations/UpdateEducationCommandHandlerTests.cs` — row 5
    - `backend/tests/JobNecto.Tests/Application/Educations/DeleteEducationCommandHandlerTests.cs` — row 6
    - `backend/tests/JobNecto.Tests/Application/CoverLetterTemplates/` — rows 7–9
    - `backend/tests/JobNecto.Tests/Application/CoverLetters/` — rows 10–12, 14
    - `backend/tests/JobNecto.Tests/Application/Vacancies/` — row 13
  - [x] Cells with **no covering test** get a new `[Fact]` added to the same handler test file. Conform to existing naming: `Handle_When{Scenario}_Throws{Exception}`
  - [x] For the soft-deleted scenario, the test arranges the repository mock to return `null` from `GetByIdAsync` (because the EF filter excludes soft-deleted rows at the repo boundary, and the in-test mock simulates that exclusion) — i.e. the soft-deleted scenario at the handler level is identical to the not-found scenario; one `Handle_WhenEntityNotFound_ThrowsNotFoundException` test covers both cells. This is documented in the matrix doc's "How soft-deleted is tested at the handler layer" note
  - [x] HTTP-level coverage of the soft-deleted vs. not-found distinction (where they overlap and where they diverge for the create-cover-letter case) is provided by R.3's authorization suite — R.4 records the cross-reference but does not duplicate

- [x] Task 6: Reconcile OpenAPI `[ProducesResponseType]` attributes (AC: 11)
  - [x] For each of the 14 matrix endpoints, open the controller action and verify the advertised response types match the matrix:
    - Detail GET-by-id: `[ProducesResponseType(StatusCodes.Status200OK)]`, `[ProducesResponseType(StatusCodes.Status401Unauthorized)]`, `[ProducesResponseType(StatusCodes.Status404NotFound)]` — must NOT advertise `403`
    - PATCH: `200`, `400`, `401`, `403`, `404`
    - DELETE: `204`, `401`, `403`, `404`
    - POST cover-letter (FK body): `201`, `400`, `401`, `404`, `409`
  - [x] Files to read:
    - `backend/src/JobNecto.API/Controllers/ResumesController.cs`
    - `backend/src/JobNecto.API/Controllers/EducationsController.cs`
    - `backend/src/JobNecto.API/Controllers/CoverLetterTemplatesController.cs`
    - `backend/src/JobNecto.API/Controllers/CoverLettersController.cs`
    - `backend/src/JobNecto.API/Controllers/VacanciesController.cs`
  - [x] If R.2 Task 9 already reconciled these attributes (per R.2 AC 11), Task 6 is a no-op verification step; document in the Dev Agent Record
  - [x] If a mismatch is found, modify the attribute and re-run `dotnet build backend/JobNecto.slnx`

- [x] Task 7: Inspect Swashbuckle response examples / XML doc tags (AC: 12)
  - [x] Grep the controller files from Task 6 for `<response code=` XML doc tags and any `[SwaggerResponseExample]` analog
  - [x] If examples exist, verify the `code` value matches the matrix; remove or correct any contradiction
  - [x] If no examples exist for the affected endpoints, record "no Swashbuckle examples wired — `[ProducesResponseType]` attributes are the contract source" in the matrix doc and move on
  - [x] Confirm `GET /openapi/v1.json` still serves (no manual verification required; just confirm build still succeeds in Task 9)

- [x] Task 8: Author the canonical matrix document (AC: 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 13, 14)
  - [x] Create `_bmad-output/planning-artifacts/architecture/authorization-contract-matrix.md`
  - [x] Section structure:
    - `# Authorization Contract Matrix (Story R.4)` — purpose + audience (API consumers, R.3 regression suite, future stories)
    - `## Canonical Behaviors` — restate the three canonical contracts in prose (detail GET cross-user → `404`; PATCH/DELETE cross-user → `403`; POST FK body cross-user → `404`; soft-deleted owned by caller → `404`)
    - `## Excluded Endpoints` — `/api/v1/users/me*`, anonymous endpoints, list endpoints (rationale per AC 4)
    - `## Exception Mapping` — table from `GlobalExceptionHandler.cs` (per AC 6)
    - `## Matrix` — the table from `## Contract Matrix (Canonical)` above, copied verbatim, with the additional `Test Reference` column populated from Task 5's audit
    - `## How Soft-Deleted is Tested at the Handler Layer` — note explaining the equivalence with not-found at the unit-test layer; HTTP-level distinction is covered by R.3
    - `## Refinements over R.2` — empty unless this story changed any canonical behavior; if any, list with rationale (per AC 3)
    - `## Open Items` — anything routed to R.5 or a future story; empty if all matrix rows conform
  - [x] Link from `_bmad-output/planning-artifacts/architecture/index.md` (per AC 13) under a new "Contract Matrices" section or an appropriate existing section (e.g. next to the endpoint-ownership-audit link)
  - [x] The doc must be readable standalone — do not require the reader to open R.2 or R.3 to understand the contract

- [x] Task 9: Run build and tests (AC: 15, 16)
  - [x] `dotnet build backend/JobNecto.slnx --configuration Release --warnaserror` — must succeed with 0 warnings
  - [x] `dotnet test backend/JobNecto.slnx --configuration Release --warnaserror` — must succeed; record the test count delta vs. R.3 in the Dev Agent Record
  - [x] If any new handler test was added under Task 5, confirm it appears in the discovered test set (`dotnet test --list-tests` spot check is optional but recommended)

- [x] Task 10: Update sprint status (AC: housekeeping)
  - [x] Already flipped to `ready-for-dev` at story-draft time; on dev start, `dev-story` will move it to `in-progress`
  - [x] On dev completion, move to `review`

### Review Findings

- [x] [Review][Patch] F1 CRITICAL: 409 incorrectly removed — unique index `IX_CoverLetterTemplates_UserId_Name` makes 409 reachable on PATCH rename [`CoverLetterTemplatesController.cs`] — restored `[ProducesResponseType(409)]`
- [x] [Review][Patch] F2 HIGH: Matrix doc incorrectly described 409 removal as a gap closure; added "OpenAPI Notes — 409 on PATCH" section explaining the real unique-constraint source [`authorization-contract-matrix.md`]
- [x] [Review][Patch] F3 MEDIUM: AC 2 — embedded story matrix lacked Test Reference column; updated to 7-column format matching standalone doc [`r-4-consistent-forbidden-vs-notfound-contract-matrix.md`]
- [x] [Review][Defer] F4 MEDIUM: Concurrent PATCH rename race — no app-level uniqueness guard; pre-existing pattern deferred [`CoverLetterTemplatesController.cs`] — deferred, pre-existing
- [x] [Review][Defer] F5 LOW: GlobalExceptionHandler fallback string matching fragile for non-Postgres — pre-existing [`GlobalExceptionHandler.cs`] — deferred, pre-existing

## Dev Notes

### Core Problem

R.2 affirms canonical per-endpoint behavior and (where needed) closes gaps in handlers. R.3 builds an HTTP-level regression suite that proves the wire contract end-to-end. But neither story produces a single, parseable, permanent document that says — for every user-scoped endpoint × every failure scenario — "this is the canonical status code". That gap is what R.4 closes. The matrix is the source of truth that R.3's tests reference, that API consumers can read in isolation, and that future stories (e.g. R.5's completion gate) can audit against without rediscovering the rules.

### Why a Matrix and Not Just a Prose Section in R.2

R.2's audit doc is endpoint-centric: it tells you per-endpoint what the contract is. R.4's matrix is **scenario-centric**: it tells you, for a given failure mode, what every endpoint must do. Both shapes are useful; R.4 introduces the second one. The matrix shape also makes drift detection mechanical — a future change to a single handler can be checked against a single matrix row.

### How the Three Scenarios Surface at the Handler Layer

| Scenario | Where it surfaces | Repository behavior | Handler behavior |
| --- | --- | --- | --- |
| not-found | `GetByIdAsync(id, ct)` | Returns `null` for `null`-returning variants OR throws `NotFoundException` for throwing variants | If null is returned: handler throws `NotFoundException`. If throwing: propagates. |
| soft-deleted | `GetByIdAsync(id, ct)` | EF global query filter excludes the row → behaves identically to not-found at the repo boundary | Identical to not-found |
| cross-user | `GetByIdAsync(id, ct)` returns the row (it exists and is not soft-deleted) | Returns the row regardless of `UserId` (the repo does not filter by user for single-id lookups by design) | Handler inspects `entity.UserId != request.UserId` and throws `NotFoundException` (for detail GET) or `ForbiddenException` (for PATCH / DELETE) |

This three-way decomposition is what the matrix codifies. The wire-level distinction between not-found and soft-deleted is collapsed by design (existence non-leakage); only the cross-user case can produce `403` (and only for mutations).

### Why Cross-User Reads Are `404` and Not `403`

Returning `403` on a cross-user detail GET would leak the resource's existence to the caller — an information-disclosure vector. The codebase's canonical policy (per R.2 AC 6) is therefore `404 NotFound`, indistinguishable from "this id has never existed". The same logic extends to soft-deleted resources owned by the caller: `404` is canonical because the EF filter applies before any handler logic can decide otherwise. This is a deliberate trade-off that prioritizes uniformity and security over UX granularity (the caller cannot tell whether their own resource was soft-deleted or never existed; they would need to consult a separate "trash bin" feature, which is not in the current backlog).

### Why POST Cover-Letter With Cross-User Vacancy Is `404` and Not `403`

The same existence-non-leakage logic applies to body-supplied foreign keys: `POST /api/v1/cover-letters` carries `VacancyId` in the body. Returning `403` would leak the vacancy's existence to a caller who does not own it. The handler therefore throws `NotFoundException("Vacancy", VacancyId)`, indistinguishable from "no vacancy with this id exists at all".

### Expected Deviation Rate (Read-Time Hypothesis)

A draft-time read of the handlers (consistent with R.2's draft-time read at commit `d77756e`) suggests **no deviations** from the matrix at the handler layer. R.2's Task 9 may already have reconciled OpenAPI attributes by the time R.4 runs; if it has, R.4's Task 6 is a no-op verification step. The most likely actual work for R.4 is:

1. Authoring the canonical matrix document (Task 8).
2. Adding new `[Fact]` tests to cover any matrix cells lacking explicit coverage (Task 5).
3. Linking the doc from the architecture index (AC 13).
4. Verifying no drift was introduced between R.2 and R.4.

If R.2's audit doc (when it exists) surfaces an open item under "Open Items for R.4" (per R.2 AC 12), Task 4 expands to close it.

### Test Pattern (For Any New Handler Test)

Mirror the R.1 / Epic 2 handler-test conventions exactly:

```csharp
// backend/tests/JobNecto.Tests/Application/{Feature}/{Handler}Tests.cs
[Fact]
public async Task Handle_When{Scenario}_Throws{NotFoundException|ForbiddenException}()
{
    // Arrange
    var resourceId = Guid.NewGuid();
    var callerId = Guid.NewGuid();
    var ownerId = Guid.NewGuid();
    var entity = new {Entity} { Id = resourceId, UserId = ownerId, /* ... */ };

    var repoMock = new Mock<I{Mutable|Vacancy|User}Repository>();
    // For cross-user: return the entity. For not-found / soft-deleted: return null OR set up the mock to throw NotFoundException (match the production GetByIdAsync contract — both shapes exist in the codebase; mirror the handler under test).
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

For the `Mock<IMutableRepository<T>>` mock (Resume, Education, CoverLetter, CoverLetterTemplate), follow the R.1 pattern — its setup is identical.

### Files to Read (Read-Only)

| File | Why |
| --- | --- |
| `_bmad-output/project-context.md` | Active HTTP endpoints (AC 4) |
| `_bmad-output/archive/planning-artifacts/epics/epic-r-authorization-ownership-hardening.md` | Story origin (AC source) |
| `_bmad-output/archive/implementation-artifacts/r-2-endpoint-ownership-policy-audit-and-gap-closure.md` | Audit baseline; "Open Items for R.4" if audit doc exists |
| `_bmad-output/archive/implementation-artifacts/r-3-authorization-regression-integration-suite.md` | Consumer of the matrix (Test Reference column) |
| `backend/src/JobNecto.API/Infrastructure/ExceptionHandling/GlobalExceptionHandler.cs` | Exception → status mapping (AC 6) |
| 14 handler files listed in Task 3 | Conformance verification |
| 5 controller files listed in Task 6 | `[ProducesResponseType]` reconciliation |
| `_bmad-output/planning-artifacts/architecture/index.md` | Link target (AC 13) |
| `_bmad-output/planning-artifacts/architecture/endpoint-ownership-audit.md` (if R.2 has produced it) | Audit source for the matrix |

### Files to Create

| File | Reason |
| --- | --- |
| `_bmad-output/planning-artifacts/architecture/authorization-contract-matrix.md` | The canonical matrix doc (AC 1, 13) |

### Files to Modify (Only If Verification Surfaces Drift)

| File | Likely Reason |
| --- | --- |
| Any handler whose Task 3 row is "deviates" | Reconcile exception type to matrix (AC 7, 8, 9, 10) |
| Any controller with mismatched `[ProducesResponseType]` attributes that R.2 did not already fix | OpenAPI alignment (AC 11) |
| Any controller XML `<response>` doc tag with wrong code | Swashbuckle example alignment (AC 12) |
| Existing handler test file under `backend/tests/JobNecto.Tests/Application/{Feature}/` | Add a `[Fact]` to lock any uncovered matrix cell (AC 5) |
| `_bmad-output/planning-artifacts/architecture/index.md` | Link the matrix doc |

### Constraints and Scope Discipline

- **No middleware changes.** Corrections live in handlers (Task 4); do not touch `GlobalExceptionHandler` or any pipeline behaviors. If a mapping drift is discovered, escalate via `## Open Questions`.
- **Do not modify R.2 or R.3 artifacts.** R.4 reads them; it does not edit them.
- **Do not introduce new exception types.** The matrix uses existing exceptions (`NotFoundException`, `ForbiddenException`, `ConflictException`). New exceptions would change the mapper contract.
- **Stay scoped to the 14 matrix endpoints.** Do not expand into `/users/me*`, list endpoints, or anonymous endpoints. List endpoints' empty-page-on-cross-user behavior is R.3's territory.
- **`backend/JobNecto.slnx`** for every build/test invocation — never the root `Jobnecto.sln`. (See `_bmad-output/project-context.md`, "Critical don't-miss rules".)
- **Matrix doc lives under `_bmad-output/planning-artifacts/architecture/`** — chosen over `docs/` because (a) other architectural artifacts (e.g. `core-architectural-decisions.md`, `endpoint-ownership-audit.md` from R.2) already live there, (b) the BMad index at `_bmad-output/planning-artifacts/architecture/index.md` is the canonical discovery point, and (c) `docs/` is the `project_knowledge` glob for AI agents to read source-of-truth code docs, which the matrix is not.
- **Zero behavioral drift for conformant endpoints.** If Task 3 finds an endpoint already conforms, Task 4 does not touch it.

### Agent Learnings to Apply

- Keep generated test data validator-compliant. Some matrix-locking tests may exercise PATCH bodies; pad fields to the validator minimum where required. [Source: `agent-learnings.md` — "Keep generated test data validator-compliant"]
- Set persisted timestamps in UTC at the layer that owns the mutation. No new timestamp logic in this story. [Source: R.1 / agent-learnings.md]
- Prefer separate handler files; this story modifies existing handlers only when reconciliation is required. [Source: `agent-learnings.md` — "Prefer separate handler file"]
- EF snapshot parity matters when entity shape changes; this story does not change entity shape, so no migration/snapshot work is expected. [Source: recent learning from commit `d77756e`]

### Namespace Convention (Mandatory)

No new C# files are created by this story unless Task 5 adds a new test file (preferred: add `[Fact]`s to existing files; only create new files if the matrix surfaces a cell in a feature folder without any existing handler test file). If a new test file is required:

| File | Namespace |
| --- | --- |
| `backend/tests/JobNecto.Tests/Application/{Feature}/*Tests.cs` | `JobNecto.Tests.Application.{Feature}` |

### References

- [Source: `_bmad-output/archive/planning-artifacts/epics/epic-r-authorization-ownership-hardening.md` — Story R.4 AC block] — story origin
- [Source: `_bmad-output/planning-artifacts/epics/requirements-inventory.md` — FR27, FR28] — requirements text being affirmed
- [Source: `_bmad-output/project-context.md` — "Active HTTP endpoints"] — canonical endpoint inventory (AC 4)
- [Source: `_bmad-output/archive/implementation-artifacts/r-2-endpoint-ownership-policy-audit-and-gap-closure.md` — Dev Notes "Canonical Behaviors To Affirm" and "Inventory of Endpoints in Scope"] — audit baseline (AC 3)
- [Source: `_bmad-output/archive/implementation-artifacts/r-3-authorization-regression-integration-suite.md` — Dev Notes "Resource → Endpoint → Expected Cross-User Status Matrix"] — regression suite that will consume this matrix (AC 14)
- [Source: `backend/src/JobNecto.API/Infrastructure/ExceptionHandling/GlobalExceptionHandler.cs`] — exception → status mapping (AC 6)
- [Source: `backend/src/JobNecto.Infrastructure/Repositories/BaseRepository.cs` — `GetByIdAsync`] — repository-level not-found / soft-delete baseline
- [Source: `_bmad-output/archive/implementation-artifacts/r-1-separate-soft-delete-repository-contract.md`] — R.1 test patterns to mirror (AC 5)
- [Source: `AGENTS.md`] — build/test commands, namespace rules, secret rules

## Open Questions

1. **Should the matrix doc also catalog the `409 Conflict` contract** (e.g. `POST /api/v1/cover-letters` with a duplicate `VacancyId`)? Assumption: no — `409` is a uniqueness contract, not a not-found / soft-deleted / cross-user scenario. The matrix would conflate two different concerns. If the human prefers a wider matrix, add a "Conflict" scenario column and one row per uniqueness constraint; otherwise leave R.4 scoped to the three canonical failure modes named in the epic.
2. **Should the matrix doc include `401 Unauthorized` rows** (no auth cookie)? Assumption: no — `401` is enforced by the `[Authorize]` attribute, not by the handler; including it in the matrix would conflate framework-level concerns with handler-level concerns. The matrix's purpose is to disambiguate handler behavior; `401` is uniform across every endpoint and does not need a per-row entry. If the human prefers complete request-state coverage, add an "Unauthenticated" column with a single canonical `401` value for all rows.
3. **If R.2's audit doc has not yet been authored when R.4 runs**, the dev agent will use R.2's Dev Notes as the audit baseline (per Task 1). Assumption: this is acceptable because R.2's Dev Notes are explicit and exhaustive. If the human wants R.4 strictly gated on R.2's audit doc existing, add a Task 0 that blocks until the doc is present; this is not currently in the task list.
4. **Vacancy soft-deleted by user A, then user A creates a cover letter with that `VacancyId`** — the EF filter excludes the soft-deleted vacancy from `GetByIdAsync`, so the handler throws `NotFoundException("Vacancy", VacancyId)` → `404`. The matrix row 14 codifies this. Assumption: this is correct — the caller cannot use a soft-deleted resource even if they once owned it. If the human prefers a more granular contract (`410 Gone` for owner-soft-deleted vs `404` for cross-user), that requires a new exception type and a mapper change, which is out of scope for R.4 and would belong in a separate story.

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6 (Sonnet 4.6, 1M context) — 2026-05-23

### Debug Log References

Build: `dotnet build backend/JobNecto.slnx --configuration Release --warnaserror` → exit 0, 0 Warning(s), 0 Error(s), Time Elapsed 00:00:07.17

Test: `dotnet test backend/JobNecto.slnx --configuration Release --warnaserror` → Passed: 520, Failed: 0, Skipped: 0, Total: 520

### Completion Notes List

- **Task 1 input source:** R.2 audit doc `_bmad-output/planning-artifacts/architecture/endpoint-ownership-audit.md` was already present (R.2 status: `review`). "Open Items for R.4" section read and merged.
- **Task 2:** No mapping drift. `NotFoundException` → `404` generic "Resource not found.", `ForbiddenException` → `403`, `ConflictException` → `409`, `UnauthorizedException` → `401`, `ValidationException` → `400`.
- **Task 3:** All 14 matrix rows CONFORM. Zero handler-level deviations. Row 10 (`GET /api/v1/cover-letters/{id}`) uses `GetDetailByIdAsync` + null-check variant — 404 contract identical.
- **Task 4:** No-op — no deviations to reconcile.
- **Task 5:** All 14 matrix cells have existing test coverage. No new tests required.
- **Task 6:** One OpenAPI deviation found and closed: `CoverLetterTemplatesController.UpdateAsync` (PATCH) falsely advertised `[ProducesResponseType(StatusCodes.Status409Conflict)]`. Handler has no conflict path; attribute removed.
- **Task 7:** No Swashbuckle response examples wired — `[ProducesResponseType]` attributes are the contract source.
- **Task 8:** Canonical matrix doc created at `_bmad-output/planning-artifacts/architecture/authorization-contract-matrix.md`; linked from architecture index under new "Contract Matrices" section.
- **Test count delta:** R.2 reported 477/477; R.4 run shows 520/520 — delta of +43 (tests added by R.3 integration suite which is `review`).
- **No new handler tests added** — all cells had coverage.
- **No entity shape changes** — no migration/snapshot work.

### File List

- `backend/src/JobNecto.API/Controllers/CoverLetterTemplatesController.cs` — removed false `[ProducesResponseType(StatusCodes.Status409Conflict)]` from `UpdateAsync` PATCH action
- `_bmad-output/planning-artifacts/architecture/authorization-contract-matrix.md` — CREATED (canonical matrix doc, AC 1)
- `_bmad-output/planning-artifacts/architecture/index.md` — added "Contract Matrices" section with link to matrix doc (AC 13)
- `_bmad-output/archive/implementation-artifacts/r-4-consistent-forbidden-vs-notfound-contract-matrix.md` — this story file (status, tasks, Dev Agent Record, File List, Change Log)

## Change Log

- 2026-05-21: Story drafted by Amelia (bmad-create-story). Status set to `ready-for-dev`. 16 ACs, 10 tasks. Codifies the canonical 403-vs-404 contract for 14 user-scoped detail/update/delete endpoints × 3 scenarios (not-found, soft-deleted, cross-user), embeds the matrix in the story and as a permanent doc at `_bmad-output/planning-artifacts/architecture/authorization-contract-matrix.md`, and reconciles any handler / OpenAPI deviation surfaced during verification. Sprint status `r-4-consistent-forbidden-vs-notfound-contract-matrix` flipped from `backlog` to `ready-for-dev`.
- 2026-05-23: Story implemented by Amelia (claude-sonnet-4-6). No handler-level deviations found. One OpenAPI gap closed (CoverLetterTemplatesController.UpdateAsync false 409 removed). Canonical matrix doc created. All 14 matrix cells have existing test coverage — no new tests added. Build 0 warnings/errors; tests 520/520 passed. Status → `review`.
- 2026-05-23: Code review (bmad-code-review). 3 patches applied: (1) restored 409 on PATCH cover-letter-templates — unique index IX_CoverLetterTemplates_UserId_Name makes it reachable; (2) matrix doc corrected with OpenAPI Notes section; (3) story embedded matrix updated to 7-column format (AC 2). 2 items deferred (concurrent rename race, GlobalExceptionHandler fallback). Build clean post-patches. Status → `done`.

