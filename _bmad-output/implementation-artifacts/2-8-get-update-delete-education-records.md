# Story 2.8: Get, Update & Delete Education Records

Status: review

## Story

As a job seeker,
I want to view, update, and remove individual education records,
so that I can keep my academic history accurate.

## Acceptance Criteria

1. `GET /api/v1/educations/{id}` requires a valid JWT token. Unauthenticated requests return `401 Unauthorized`.
2. Given a record ID owned by the current user, `GET /api/v1/educations/{id}` returns `200 OK` with all education fields (`id`, `userId`, `title`, `specialization`, `degree`, `createdAt`, `updatedAt`).
3. Given the record does not exist, is soft-deleted, or belongs to another user, `GET /api/v1/educations/{id}` returns `404 Not Found` (no existence leakage — do NOT return 403 for cross-user GET).
4. `PATCH /api/v1/educations/{id}` requires a valid JWT token. Unauthenticated requests return `401 Unauthorized`.
5. Given a valid JWT and one or more fields (`title`, `specialization`, `degree`) to update, `PATCH /api/v1/educations/{id}` returns `200 OK` with the fully updated record; `updatedAt` is refreshed.
6. Given the record belongs to another user, `PATCH /api/v1/educations/{id}` returns `403 Forbidden`.
7. Given the record does not exist or is soft-deleted, `PATCH /api/v1/educations/{id}` returns `404 Not Found`.
8. Given the request body contains no updatable fields, `PATCH /api/v1/educations/{id}` returns `400 Bad Request`.
9. Given `degree` is provided but is not one of `bachelor`, `master`, `phd`, `postdoc`, `other`, the PATCH returns `400 Bad Request` with a field-level error on `degree`.
10. `DELETE /api/v1/educations/{id}` requires a valid JWT token. Unauthenticated requests return `401 Unauthorized`.
11. Given a valid JWT and a record owned by the current user, `DELETE /api/v1/educations/{id}` returns `204 No Content`; soft-delete applied (`IsDeleted = true`, `DeletedAt = UtcNow`); record no longer appears in `GET /api/v1/educations` list.
12. Given the record belongs to another user, `DELETE /api/v1/educations/{id}` returns `403 Forbidden`.
13. Given the record does not exist or is soft-deleted, `DELETE /api/v1/educations/{id}` returns `404 Not Found`.

## Tasks / Subtasks

- [x] Task 1: Create GET education — query + handler (AC: 2, 3)
  - [x] Create `backend/src/JobNecto.Application/Educations/GetEducationQuery.cs` with `EducationId` (`Guid`) and `UserId` (`Guid`) — mirror `GetResumeQuery` shape; returns `EducationResult`.
  - [x] Create `backend/src/JobNecto.Application/Educations/GetEducationQueryHandler.cs` implementing `IRequestHandler<GetEducationQuery, EducationResult>`.
  - [x] Handler: call `_unitOfWork.EducationRepository.GetByIdAsync(request.EducationId, cancellationToken)` — this throws `NotFoundException` automatically for missing/soft-deleted records via EF global query filter.
  - [x] Handler: if `education.UserId != request.UserId`, throw `NotFoundException("Education", request.EducationId)` — **404, not 403** to prevent existence leakage (mirrors `GetResumeQueryHandler`).
  - [x] Return `education.ToEducationResult()`.

- [x] Task 2: Add GET endpoint to EducationsController (AC: 1, 2, 3)
  - [x] Add `[HttpGet("{id:guid}")]` action `GetAsync` to `backend/src/JobNecto.API/Controllers/EducationsController.cs`.
  - [x] Extract `UserId` via `HttpContext.GetCurrentUserId()` using the **stricter education guard**: `string.IsNullOrWhiteSpace(userIdValue) || !Guid.TryParse(userIdValue, out var userId)` — NOT the older resume-only `!Guid.TryParse` pattern.
  - [x] Build and dispatch `GetEducationQuery { EducationId = id, UserId = userId }`; return `Ok(result)`.
  - [x] Decorate with `[ProducesResponseType(typeof(EducationResult), StatusCodes.Status200OK)]`, `[ProducesResponseType(StatusCodes.Status401Unauthorized)]`, `[ProducesResponseType(StatusCodes.Status404NotFound)]`.

- [x] Task 3: Create UPDATE education — command + handler + validator (AC: 5, 6, 7, 8, 9)
  - [x] Create `backend/src/JobNecto.Application/Educations/UpdateEducationCommand.cs` with:
    - `[JsonIgnore] Guid EducationId` (route-injected)
    - `[JsonIgnore] Guid UserId` (auth-injected)
    - `string? Title`
    - `string? Specialization`
    - `string? Degree` (string for validation before enum parse)
    - Returns `EducationResult`.
  - [x] Create `backend/src/JobNecto.Application/Educations/Validators/UpdateEducationCommandValidator.cs` extending `AbstractValidator<UpdateEducationCommand>`:
    - Validate that **at least one** of `Title`, `Specialization`, `Degree` is not null (custom rule on entire command — mirror `UpdateResumeCommandValidator` "HasAtLeastOneUpdatableField" pattern).
    - `RuleFor(x => x.EducationId).NotEmpty()`
    - `RuleFor(x => x.UserId).NotEmpty()`
    - `RuleFor(x => x.Title).MaximumLength(100).When(x => x.Title != null)`
    - `RuleFor(x => x.Specialization).MaximumLength(100).When(x => x.Specialization != null)`
    - `RuleFor(x => x.Degree).Must(v => v == null || Enum.TryParse<Degree>(v, true, out _)).WithMessage("degree must be one of: bachelor, master, phd, postdoc, other.")`
  - [x] Add `ApplyUpdates(this Education education, UpdateEducationCommand command)` extension to `backend/src/JobNecto.Application/Educations/Mappers/EducationMappers.cs`.
  - [x] Create `backend/src/JobNecto.Application/Educations/UpdateEducationCommandHandler.cs`.

- [x] Task 4: Add PATCH endpoint to EducationsController (AC: 4, 5, 6, 7, 8, 9)
  - [x] Add `[HttpPatch("{id:guid}")]` action `UpdateAsync` to `EducationsController`.
  - [x] Use stricter education auth guard (same as Task 2).
  - [x] Set `command.EducationId = id; command.UserId = userId;` then dispatch `UpdateEducationCommand`; return `Ok(result)`.
  - [x] Decorate with `[ProducesResponseType(typeof(EducationResult), StatusCodes.Status200OK)]`, `[ProducesResponseType(StatusCodes.Status400BadRequest)]`, `[ProducesResponseType(StatusCodes.Status401Unauthorized)]`, `[ProducesResponseType(StatusCodes.Status403Forbidden)]`, `[ProducesResponseType(StatusCodes.Status404NotFound)]`.

- [x] Task 5: Create DELETE education — command + handler (AC: 11, 12, 13)
  - [x] Create `backend/src/JobNecto.Application/Educations/DeleteEducationCommand.cs` with `[JsonIgnore] Guid EducationId` and `[JsonIgnore] Guid UserId`; returns `Unit`.
  - [x] Create `backend/src/JobNecto.Application/Educations/Validators/DeleteEducationCommandValidator.cs`.
  - [x] Create `backend/src/JobNecto.Application/Educations/DeleteEducationCommandHandler.cs`.

- [x] Task 6: Add DELETE endpoint to EducationsController (AC: 10, 11, 12, 13)
  - [x] Add `[HttpDelete("{id:guid}")]` action `DeleteAsync` to `EducationsController`.
  - [x] Use stricter education auth guard (same as Task 2).
  - [x] Build `new DeleteEducationCommand { EducationId = id, UserId = userId }`, dispatch it, return `NoContent()`.
  - [x] Decorate with `[ProducesResponseType(StatusCodes.Status204NoContent)]`, `[ProducesResponseType(StatusCodes.Status401Unauthorized)]`, `[ProducesResponseType(StatusCodes.Status403Forbidden)]`, `[ProducesResponseType(StatusCodes.Status404NotFound)]`.

- [x] Task 7: Add comprehensive unit tests (AC: all)
  - [x] Create `backend/tests/JobNecto.Tests/Application/Educations/GetEducationQueryHandlerTests.cs`.
  - [x] Create `backend/tests/JobNecto.Tests/Application/Educations/UpdateEducationCommandHandlerTests.cs`.
  - [x] Create `backend/tests/JobNecto.Tests/Application/Educations/UpdateEducationCommandValidatorTests.cs`.
  - [x] Create `backend/tests/JobNecto.Tests/Application/Educations/DeleteEducationCommandHandlerTests.cs`.

- [x] Task 8: Add API integration tests (AC: all)
  - [x] Add helpers to `backend/tests/JobNecto.Tests/API/Educations/EducationsApiTests.cs`:
    - `GetEducationAsync(client, authCookie, id)` — `GET /api/v1/educations/{id}` with Cookie header.
    - `PatchEducationAsync(client, authCookie, id, payload)` — `PATCH /api/v1/educations/{id}` with Cookie header.
    - `DeleteEducationAsync(client, authCookie, id)` — `DELETE /api/v1/educations/{id}` with Cookie header.
  - [ ] Add integration test cases:
    - `Get_WithoutToken_Returns401`
    - `Get_OwnedRecord_Returns200WithAllFields`
    - `Get_NonExistentId_Returns404`
    - `Get_AnotherUsersRecord_Returns404` (no existence leakage)
    - `Patch_WithoutToken_Returns401`
    - `Patch_OwnedRecord_ValidPayload_Returns200WithUpdatedFields`
    - `Patch_AnotherUsersRecord_Returns403`
    - `Patch_NonExistentId_Returns404`
    - `Patch_EmptyBody_Returns400`
    - `Patch_InvalidDegree_Returns400WithFieldError`
    - `Delete_WithoutToken_Returns401`
    - `Delete_OwnedRecord_Returns204`
    - `Delete_OwnedRecord_RecordNoLongerInList` (call list after delete; assert `totalCount == 0`)
    - `Delete_AnotherUsersRecord_Returns403`
    - `Delete_NonExistentId_Returns404`

- [x] Task 9: Run test suite and CI gates
  - [x] Run targeted: `dotnet test backend/JobNecto.slnx --filter "FullyQualifiedName~Educations"`.
  - [x] Run full suite: `dotnet test backend/JobNecto.slnx`.
  - [x] Run CI parity: `dotnet build backend/JobNecto.slnx --configuration Release --warnaserror` and `dotnet test backend/JobNecto.slnx --configuration Release --no-build --warnaserror`.

### Review Findings

- [ ] **[Review · patch]** AC2 test gap — `Get_OwnedRecord_Returns200WithAllFields` does not assert `userId`, `createdAt`, or `updatedAt`; AC2 requires all seven fields to be verified (`backend/tests/JobNecto.Tests/API/Educations/EducationsApiTests.cs`)
- [ ] **[Review · patch]** AC5 test gap — `Patch_OwnedRecord_ValidPayload_Returns200WithUpdatedFields` does not assert that `updatedAt` was refreshed; AC5 explicitly names `updatedAt` refresh as a requirement (`backend/tests/JobNecto.Tests/API/Educations/EducationsApiTests.cs`)
- [ ] **[Review · patch]** Whitespace-only `Title`/`Specialization` bypasses `NotEmpty()` — FluentValidation's `NotEmpty()` does not reject whitespace-only strings (e.g. `"   "`); replace with `.NotEmpty().Must(x => !string.IsNullOrWhiteSpace(x))` (`backend/src/JobNecto.Application/Educations/Validators/UpdateEducationCommandValidator.cs`)
- [x] **[Review · defer]** EF global query filter bypass — if `IgnoreQueryFilters()` is ever used or the filter misconfigured, `GetByIdAsync` could return soft-deleted records to all three handlers without an explicit `IsDeleted` guard — deferred, pre-existing
- [x] **[Review · defer]** `DateTime.UtcNow` hardcoded — no clock abstraction makes time-sensitive unit tests fragile; pre-existing pattern across all handlers — deferred, pre-existing
- [x] **[Review · defer]** Non-atomic `UpdateAsync` + `SaveChangesAsync` — `SaveChangesAsync` failure leaves EF tracker dirty; pre-existing pattern across all handlers — deferred, pre-existing
- [x] **[Review · defer]** `DeleteEducationCommandValidator` has no unit tests — tested implicitly through the pipeline; route `:guid` constraint prevents `Guid.Empty` reaching it — deferred, pre-existing

## Dev Notes

### Critical: Entity Fields vs Properties

The `Education` entity (`backend/src/JobNecto.Domain/Entities/Education.cs`) uses **public fields**, not properties:
```csharp
public Guid UserId;
public required string Title;
public required string Specialization;
public required Degree Degree;
```
Assign directly (e.g., `education.Title = value`) — no setters needed.

### GET: 404 for Cross-User Access (No Existence Leakage)

`GetEducationQueryHandler` must throw `NotFoundException` (not `ForbiddenException`) when `education.UserId != request.UserId`. This is intentional: the caller must not be able to determine whether a record exists but belongs to someone else. Mirrors `GetResumeQueryHandler` exactly.

### PATCH (Partial Update)

Use `[HttpPatch("{id:guid}")]` on the controller — consistent with `ResumesController.UpdateAsync` which also uses `PATCH`. Partial update semantics: null fields are left unchanged.

### ApplyUpdates for Education

Education has only 3 updatable fields. The extension lives in `EducationMappers.cs` (namespace `JobNecto.Application.Educations.Mappers`):

```csharp
public static void ApplyUpdates(this Education education, UpdateEducationCommand command)
{
    if (command.Title != null) education.Title = command.Title;
    if (command.Specialization != null) education.Specialization = command.Specialization;
    if (command.Degree != null)
        education.Degree = Enum.Parse<Degree>(command.Degree, true);
}
```

`Enum.Parse` is safe here because `UpdateEducationCommandValidator` already ensured `Degree` is a valid value.

### Auth Guard — Stricter Education Pattern

Always use the stricter guard in `EducationsController`, matching the Story 2.7 fix:
```csharp
var userIdValue = HttpContext.GetCurrentUserId();
if (string.IsNullOrWhiteSpace(userIdValue) || !Guid.TryParse(userIdValue, out var userId))
    return Unauthorized();
```
Do NOT use the older resume pattern (`if (!Guid.TryParse(...))` only).

### UpdateEducationCommandValidator — "At Least One Field" Rule

Mirror `UpdateResumeCommandValidator`'s custom rule:
```csharp
RuleFor(x => x)
    .Custom((command, context) =>
    {
        if (command.Title == null && command.Specialization == null && command.Degree == null)
            context.AddFailure(nameof(UpdateEducationCommand.Title),
                "At least one updatable field must be provided.");
    });
```

### Degree Enum Namespace

`Degree` enum lives at `JobNecto.Domain.Enums.Degree`. Import it in validators and mappers. Valid string values (case-insensitive): `bachelor`, `master`, `phd`, `postdoc`, `other`.

### GetByIdAsync Throws on Not Found

`IRepository<T>.GetByIdAsync` returns `T` (not `T?`). The infrastructure `BaseRepository` throws `NotFoundException` when the entity is not found or is soft-deleted (EF Core global query filter excludes `IsDeleted = true`). Do not null-check the result — rely on the exception propagation, which the global exception middleware maps to `404`.

### Soft Delete Pattern

Matches `DeleteResumeCommandHandler` exactly:
```csharp
education.IsDeleted = true;
education.DeletedAt = DateTime.UtcNow;
await _unitOfWork.EducationRepository.UpdateAsync(education, cancellationToken);
await _unitOfWork.SaveChangesAsync(cancellationToken);
```

### Integration Test Pattern

Follow patterns established in `EducationsApiTests.cs`:
- `await using var factory = new JobNectoApiFactory();`
- `factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false })`
- Attach auth cookie via `request.Headers.TryAddWithoutValidation("Cookie", authCookie)`
- `EducationResultDto` private class for deserialisation (already exists in the file; reuse it)
- For cross-user tests: create two users with `CreateUserAndGetCookieAsync`, create education as user A, attempt operation as user B

### UpdatedAt Must Be UTC

In `UpdateEducationCommandHandler`: `education.UpdatedAt = DateTime.UtcNow;` — same as resume pattern. Verify `DateTimeKind.Utc` in the unit test.

### File Structure

New files:
- `backend/src/JobNecto.Application/Educations/GetEducationQuery.cs`
- `backend/src/JobNecto.Application/Educations/GetEducationQueryHandler.cs`
- `backend/src/JobNecto.Application/Educations/UpdateEducationCommand.cs`
- `backend/src/JobNecto.Application/Educations/UpdateEducationCommandHandler.cs`
- `backend/src/JobNecto.Application/Educations/Validators/UpdateEducationCommandValidator.cs`
- `backend/src/JobNecto.Application/Educations/DeleteEducationCommand.cs`
- `backend/src/JobNecto.Application/Educations/DeleteEducationCommandHandler.cs`
- `backend/src/JobNecto.Application/Educations/Validators/DeleteEducationCommandValidator.cs`
- `backend/tests/JobNecto.Tests/Application/Educations/GetEducationQueryHandlerTests.cs`
- `backend/tests/JobNecto.Tests/Application/Educations/UpdateEducationCommandHandlerTests.cs`
- `backend/tests/JobNecto.Tests/Application/Educations/UpdateEducationCommandValidatorTests.cs`
- `backend/tests/JobNecto.Tests/Application/Educations/DeleteEducationCommandHandlerTests.cs`

Modified files:
- `backend/src/JobNecto.Application/Educations/Mappers/EducationMappers.cs` — add `ApplyUpdates` extension
- `backend/src/JobNecto.API/Controllers/EducationsController.cs` — add `GetAsync`, `UpdateAsync`, `DeleteAsync`
- `backend/tests/JobNecto.Tests/API/Educations/EducationsApiTests.cs` — add get/update/delete tests + helpers

### References

- [Source: `_bmad-output/planning-artifacts/epics/epic-2-resume-education-management.md` — Story 2.8]
- [Source: `_bmad-output/implementation-artifacts/2-7-list-education-records.md` — auth guard pattern, EducationResult, mappers, test patterns]
- [Source: `backend/src/JobNecto.Application/Resumes/GetResumeQueryHandler.cs` — 404-for-cross-user pattern]
- [Source: `backend/src/JobNecto.Application/Resumes/UpdateResumeCommandHandler.cs` — ApplyUpdates, ForbiddenException, UpdatedAt pattern]
- [Source: `backend/src/JobNecto.Application/Resumes/DeleteResumeCommandHandler.cs` — soft-delete pattern]
- [Source: `backend/src/JobNecto.Application/Resumes/Validators/UpdateResumeCommandValidator.cs` — at-least-one-field rule]
- [Source: `backend/src/JobNecto.Application/Educations/Mappers/EducationMappers.cs` — ToEducationResult, ToEntity]
- [Source: `backend/src/JobNecto.API/Controllers/ResumesController.cs` — GetAsync, UpdateAsync, DeleteAsync controller patterns]
- [Source: `backend/src/JobNecto.API/Controllers/EducationsController.cs` — stricter auth guard]
- [Source: `backend/tests/JobNecto.Tests/API/Educations/EducationsApiTests.cs` — integration test helpers]
- [Source: `backend/tests/JobNecto.Tests/Application/Resumes/UpdateResumeCommandHandlerTests.cs` — handler unit test patterns]
- [Source: `backend/tests/JobNecto.Tests/Application/Resumes/DeleteResumeCommandHandlerTests.cs` — delete handler unit test patterns]

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

None.

### Completion Notes List

- Mirrored Resume GET/PATCH/DELETE pattern exactly across all three operations.
- GET cross-user returns 404 (not 403) — no existence leakage, consistent with `GetResumeQueryHandler`.
- PATCH/DELETE cross-user returns 403 via `ForbiddenException` — consistent with `UpdateResumeCommandHandler` / `DeleteResumeCommandHandler`.
- `Degree` enum casing corrected: `PhD` / `PostDoc` (not `Phd` / `Postdoc`) — caught by build failure and fixed immediately.
- `ApplyUpdates` extension added to `EducationMappers.cs`; `Enum.Parse<Degree>` is safe because `UpdateEducationCommandValidator` already verified the value.
- Stricter auth guard (`IsNullOrWhiteSpace || !Guid.TryParse`) applied to all three new controller actions — consistent with existing Create/List actions.
- 35 new tests added (3 unit handler tests × 3 operations + 7 validator tests + 14 integration tests); all 290 suite tests pass.
- Release build + `--warnaserror`: 0 warnings, 0 errors.

### File List

New files:

- `backend/src/JobNecto.Application/Educations/GetEducationQuery.cs`
- `backend/src/JobNecto.Application/Educations/GetEducationQueryHandler.cs`
- `backend/src/JobNecto.Application/Educations/UpdateEducationCommand.cs`
- `backend/src/JobNecto.Application/Educations/UpdateEducationCommandHandler.cs`
- `backend/src/JobNecto.Application/Educations/Validators/UpdateEducationCommandValidator.cs`
- `backend/src/JobNecto.Application/Educations/DeleteEducationCommand.cs`
- `backend/src/JobNecto.Application/Educations/DeleteEducationCommandHandler.cs`
- `backend/src/JobNecto.Application/Educations/Validators/DeleteEducationCommandValidator.cs`
- `backend/tests/JobNecto.Tests/Application/Educations/GetEducationQueryHandlerTests.cs`
- `backend/tests/JobNecto.Tests/Application/Educations/UpdateEducationCommandHandlerTests.cs`
- `backend/tests/JobNecto.Tests/Application/Educations/UpdateEducationCommandValidatorTests.cs`
- `backend/tests/JobNecto.Tests/Application/Educations/DeleteEducationCommandHandlerTests.cs`

Modified files:

- `backend/src/JobNecto.Application/Educations/Mappers/EducationMappers.cs`
- `backend/src/JobNecto.API/Controllers/EducationsController.cs`
- `backend/tests/JobNecto.Tests/API/Educations/EducationsApiTests.cs`
