# Story R.1: Separate Soft Delete Repository Contract from Editable Repositories

Status: done

GitHub Issue: #65

## Story

As a developer working on the Jobnecto backend,
I want a dedicated `ISoftDeleteRepository<T>` interface applied to every repository whose entity extends `SoftDeletableEntity`,
so that soft delete capability is expressed as a first-class contract independent of editable (update) semantics across the entire codebase.

## Acceptance Criteria

1. A `ISoftDeleteRepository<T>` interface exists in Application/Interfaces, constrained to `T : SoftDeletableEntity`, extending `IRepository<T>`, with a single `SoftDeleteAsync(T entity, CancellationToken ct)` method.
2. A `IMutableRepository<T>` interface exists in Application/Interfaces as an explicit composition of `IEditableRepository<T>` and `ISoftDeleteRepository<T>`.
3. `IEditableRepository<T>` is unchanged — it does not imply soft delete capability.
4. All six soft-deletable entity repositories expose `ISoftDeleteRepository<T>` (directly or via composition) through their interface contracts:
   - `IMutableRepository<Resume>` for `ResumeRepository`
   - `IMutableRepository<Education>` for `EducationRepository`
   - `IMutableRepository<CoverLetter>` for `CoverLetterRepository`
   - `IMutableRepository<CoverLetterTemplate>` for `CoverLetterTemplateRepository`
   - `IUserRepository` extends `ISoftDeleteRepository<User>` (specialized repo, explicit extension)
   - `IVacancyRepository` extends `ISoftDeleteRepository<Vacancy>` (specialized repo, explicit extension)
5. `IUnitOfWork.ResumeRepository`, `EducationRepository`, and `CoverLetterRepository` expose `IMutableRepository<T>` instead of `IEditableRepository<T>`.
6. `DeleteResumeCommandHandler` and `DeleteEducationCommandHandler` call `SoftDeleteAsync` instead of manually setting `IsDeleted`/`DeletedAt` and calling `UpdateAsync`.
7. A `SoftDeletableRepository<T>` abstract class in Infrastructure encapsulates the `IsDeleted = true; DeletedAt = DateTime.UtcNow` flag-setting logic and calls `_dbSet.Update(entity)`.
8. All repositories whose entities are soft-deletable derive from `SoftDeletableRepository<T>` or implement `SoftDeleteAsync` directly: `ResumeRepository`, `EducationRepository`, `CoverLetterRepository`, `CoverLetterTemplateRepository`, `UserRepository`. `VacancyRepository` implements `SoftDeleteAsync` directly (it does not extend `EditableRepository`).
9. Existing resume and education delete/update flows produce identical HTTP responses — zero behavior change at the API level.
10. Unit tests for `DeleteResumeCommandHandler` and `DeleteEducationCommandHandler` are updated to mock `IMutableRepository<T>` and verify `SoftDeleteAsync` is called.
11. New unit/infrastructure tests verify `SoftDeletableRepository<T>` sets `IsDeleted = true` and `DeletedAt` (UTC, non-null) and that the soft-deleted entity is subsequently excluded from queries via the EF global query filter.
12. `dotnet build backend/JobNecto.slnx` passes.
13. `dotnet test backend/JobNecto.slnx` passes.

## Tasks / Subtasks

- [x] Task 1: Create `ISoftDeleteRepository<T>` (AC: 1)
  - [x] Create `backend/src/JobNecto.Application/Interfaces/ISoftDeleteRepository.cs`
  - [x] Namespace: `JobNecto.Application.Interfaces`
  - [x] Constraint: `where T : SoftDeletableEntity`; extends `IRepository<T>`
  - [x] Declare `Task SoftDeleteAsync(T entity, CancellationToken ct);`
  - [x] Add XML doc on interface and method

- [x] Task 2: Create `IMutableRepository<T>` (AC: 2, 3)
  - [x] Create `backend/src/JobNecto.Application/Interfaces/IMutableRepository.cs`
  - [x] Namespace: `JobNecto.Application.Interfaces`
  - [x] Constraint: `where T : SoftDeletableEntity`; extends `IEditableRepository<T>` and `ISoftDeleteRepository<T>`
  - [x] Add XML doc: role-based composition for entities that support all write operations (update + soft delete); `IEditableRepository<T>` alone does NOT imply soft delete

- [x] Task 3: Extend `IUserRepository` with `ISoftDeleteRepository<User>` (AC: 4)
  - [x] Modify `backend/src/JobNecto.Application/Interfaces/IUserRepository.cs`
  - [x] Add `ISoftDeleteRepository<User>` to the interface inheritance list
  - [x] Drop the now-redundant `IRepository<User>` from the list (already covered by `IEditableRepository<User>` and `ISoftDeleteRepository<User>`)
  - [x] Result: `public interface IUserRepository : IEditableRepository<User>, ISoftDeleteRepository<User>`
  - [x] Update XML doc

- [x] Task 4: Extend `IVacancyRepository` with `ISoftDeleteRepository<Vacancy>` (AC: 4)
  - [x] Modify `backend/src/JobNecto.Application/Interfaces/IVacancyRepository.cs`
  - [x] Add `ISoftDeleteRepository<Vacancy>` to the interface inheritance list
  - [x] Result: `public interface IVacancyRepository : IRepository<Vacancy>, ISoftDeleteRepository<Vacancy>`
  - [x] Add XML doc

- [x] Task 5: Update `IUnitOfWork` property types for CoverLetter, Resume, Education (AC: 5)
  - [x] Modify `backend/src/JobNecto.Application/Interfaces/IUnitOfWork.cs`
  - [x] `IEditableRepository<CoverLetter> CoverLetterRepository` → `IMutableRepository<CoverLetter> CoverLetterRepository`
  - [x] `IEditableRepository<Resume> ResumeRepository` → `IMutableRepository<Resume> ResumeRepository`
  - [x] `IEditableRepository<Education> EducationRepository` → `IMutableRepository<Education> EducationRepository`
  - [x] `UserRepository` and `VacancyRepository` property types stay as `IUserRepository` and `IVacancyRepository` (those interfaces now extend ISoftDeleteRepository)
  - [x] Update XML docs on changed properties

- [x] Task 6: Create `SoftDeletableRepository<T>` in Infrastructure (AC: 7)
  - [x] Create `backend/src/JobNecto.Infrastructure/Repositories/SoftDeletableRepository.cs`
  - [x] Namespace: `JobNecto.Infrastructure.Repositories`
  - [x] Class: `public abstract class SoftDeletableRepository<T> : EditableRepository<T>, IMutableRepository<T> where T : SoftDeletableEntity`
  - [x] Implement `SoftDeleteAsync`: `entity.IsDeleted = true; entity.DeletedAt = DateTime.UtcNow; _dbSet.Update(entity); return Task.CompletedTask;`
  - [x] Add XML doc on class and method

- [x] Task 7: Switch `ResumeRepository`, `EducationRepository`, `CoverLetterRepository`, `CoverLetterTemplateRepository`, `UserRepository` to derive from `SoftDeletableRepository<T>` (AC: 8)
  - [x] `ResumeRepository : SoftDeletableRepository<Resume>` (was `EditableRepository<Resume>`)
  - [x] `EducationRepository : SoftDeletableRepository<Education>` (was `EditableRepository<Education>`)
  - [x] `CoverLetterRepository : SoftDeletableRepository<CoverLetter>` (was `EditableRepository<CoverLetter>`)
  - [x] `CoverLetterTemplateRepository : SoftDeletableRepository<CoverLetterTemplate>` (was `EditableRepository<CoverLetterTemplate>`)
  - [x] `UserRepository : SoftDeletableRepository<User>, IUserRepository` (was `EditableRepository<User>, IUserRepository`)
  - [x] No other changes in these files — constructors and existing methods remain identical

- [x] Task 8: Implement `SoftDeleteAsync` directly in `VacancyRepository` (AC: 8)
  - [x] Modify `backend/src/JobNecto.Infrastructure/Repositories/VacancyRepository.cs`
  - [x] Keep `VacancyRepository : BaseRepository<Vacancy>, IVacancyRepository` — do NOT change the base class to `EditableRepository` or `SoftDeletableRepository`
  - [x] Add explicit implementation of `ISoftDeleteRepository<Vacancy>.SoftDeleteAsync`
  - [x] Add XML doc on the method

- [x] Task 9: Update `UnitOfWork` field and property types for CoverLetter (AC: 5)
  - [x] Modify `backend/src/JobNecto.Infrastructure/Persistance/UnitOfWork.cs`
  - [x] Change `IEditableRepository<CoverLetter>? _coverLetterRepository` → `IMutableRepository<CoverLetter>?`
  - [x] Change `IEditableRepository<Resume>? _resumeRepository` → `IMutableRepository<Resume>?`
  - [x] Change `IEditableRepository<Education>? _educationRepository` → `IMutableRepository<Education>?`
  - [x] Update the three lazy-init properties to return `IMutableRepository<T>`

- [x] Task 10: Refactor `DeleteResumeCommandHandler` to use `SoftDeleteAsync` (AC: 6, 9)
  - [x] Modify `backend/src/JobNecto.Application/Resumes/DeleteResumeCommandHandler.cs`
  - [x] Remove manual flag setting; add `SoftDeleteAsync` call
  - [x] All other logic unchanged

- [x] Task 11: Refactor `DeleteEducationCommandHandler` to use `SoftDeleteAsync` (AC: 6, 9)
  - [x] Modify `backend/src/JobNecto.Application/Educations/DeleteEducationCommandHandler.cs`
  - [x] Remove manual flag setting; add `SoftDeleteAsync` call
  - [x] All other logic unchanged

- [x] Task 12: Update delete handler unit tests (AC: 10)
  - [x] `backend/tests/JobNecto.Tests/Application/Resumes/DeleteResumeCommandHandlerTests.cs` — `Mock<IMutableRepository<Resume>>`, verify `SoftDeleteAsync` Times.Once and `UpdateAsync` Times.Never
  - [x] `backend/tests/JobNecto.Tests/Application/Educations/DeleteEducationCommandHandlerTests.cs` — same pattern
  - [x] Updated all other Resume/Education handler tests: `Mock<IMutableRepository<T>>` (6 additional files)

- [x] Task 13: Add infrastructure tests for `SoftDeletableRepository<T>` (AC: 11)
  - [x] Create `backend/tests/JobNecto.Tests/Infrastructure/SoftDeletableRepositoryTests.cs`
  - [x] `SoftDeleteAsync_SetsIsDeletedAndDeletedAtUtc` passes
  - [x] `SoftDeleteAsync_AfterSaveChanges_EntityExcludedFromQuery` passes

- [x] Task 14: Run build and tests (AC: 12, 13)
  - [x] `dotnet build backend/JobNecto.slnx` — 0 warnings, 0 errors
  - [x] `dotnet test backend/JobNecto.slnx` — 292/292 passed
  - [x] `dotnet build backend/JobNecto.slnx --configuration Release --warnaserror` — succeeded

- [x] Task 15: Update architecture documentation (Decision 3 and 6)
  - [x] Update `_bmad-output/planning-artifacts/architecture/core-architectural-decisions.md`
  - [x] Decision 3: all six repositories listed with final interface types; full class hierarchy
  - [x] Decision 6: soft delete handler pattern shows `SoftDeleteAsync` usage

## Dev Notes

### Core Problem

Every entity in the domain that is soft-deletable extends `SoftDeletableEntity`. However, the soft delete operation — setting `IsDeleted = true` and `DeletedAt = DateTime.UtcNow` — was previously done inside handlers by calling `UpdateAsync` from `IEditableRepository<T>`. This couples delete semantics to update semantics. The fix: move soft delete into a dedicated interface and abstract class, and apply it uniformly across every repository whose entity is soft-deletable.

### All Soft-Deletable Entities and Their Repositories

| Entity | Current Repository Interface in UoW | After Refactor |
| --- | --- | --- |
| `Resume` | `IEditableRepository<Resume>` | `IMutableRepository<Resume>` |
| `Education` | `IEditableRepository<Education>` | `IMutableRepository<Education>` |
| `CoverLetter` | `IEditableRepository<CoverLetter>` | `IMutableRepository<CoverLetter>` |
| `CoverLetterTemplate` | Not in UoW yet (Epic 3) | Base class changes now; Epic 3 adds to UoW |
| `User` | `IUserRepository` | `IUserRepository` (now extends `ISoftDeleteRepository`) |
| `Vacancy` | `IVacancyRepository` | `IVacancyRepository` (now extends `ISoftDeleteRepository`) |

### Interface Hierarchy After Refactor

```text
IRepository<T>                                      [T : BaseEntity]
├── IEditableRepository<T>  (+ UpdateAsync)         [T : BaseEntity]
└── ISoftDeleteRepository<T>  (+ SoftDeleteAsync)   [T : SoftDeletableEntity]
    └── IMutableRepository<T>                       [T : SoftDeletableEntity]
            ← role-based composition: IEditableRepository<T> + ISoftDeleteRepository<T>
```

Specialized interfaces after refactor:

```text
IUserRepository    : IEditableRepository<User>,    ISoftDeleteRepository<User>
IVacancyRepository : IRepository<Vacancy>,         ISoftDeleteRepository<Vacancy>
```

### Infrastructure Class Hierarchy After Refactor

```text
BaseRepository<T>                           (implements IRepository<T>)
└── EditableRepository<T>                   (implements IEditableRepository<T>)
    └── SoftDeletableRepository<T>          (implements ISoftDeleteRepository<T>)
        ├── ResumeRepository
        ├── EducationRepository
        ├── CoverLetterRepository
        ├── CoverLetterTemplateRepository
        └── UserRepository                  (also implements IUserRepository)

BaseRepository<Vacancy>                     (implements IRepository<Vacancy>)
└── VacancyRepository                       (implements IVacancyRepository + SoftDeleteAsync directly)
```

### All Files to Touch

| File | Change Type |
| --- | --- |
| `backend/src/JobNecto.Application/Interfaces/ISoftDeleteRepository.cs` | **CREATE** |
| `backend/src/JobNecto.Application/Interfaces/IMutableRepository.cs` | **CREATE** |
| `backend/src/JobNecto.Infrastructure/Repositories/SoftDeletableRepository.cs` | **CREATE** |
| `backend/tests/JobNecto.Tests/Infrastructure/SoftDeletableRepositoryTests.cs` | **CREATE** |
| `backend/src/JobNecto.Application/Interfaces/IUnitOfWork.cs` | **UPDATE** — Resume, Education, CoverLetter property types |
| `backend/src/JobNecto.Application/Interfaces/IUserRepository.cs` | **UPDATE** — add `ISoftDeleteRepository` |
| `backend/src/JobNecto.Application/Interfaces/IVacancyRepository.cs` | **UPDATE** — add `ISoftDeleteRepository` |
| `backend/src/JobNecto.Infrastructure/Persistance/UnitOfWork.cs` | **UPDATE** — field and property types for Resume, Education, CoverLetter |
| `backend/src/JobNecto.Infrastructure/Repositories/ResumeRepository.cs` | **UPDATE** — base class |
| `backend/src/JobNecto.Infrastructure/Repositories/EducationRepository.cs` | **UPDATE** — base class |
| `backend/src/JobNecto.Infrastructure/Repositories/CoverLetterRepository.cs` | **UPDATE** — base class |
| `backend/src/JobNecto.Infrastructure/Repositories/CoverLetterTemplateRepository.cs` | **UPDATE** — base class |
| `backend/src/JobNecto.Infrastructure/Repositories/UserRepository.cs` | **UPDATE** — base class |
| `backend/src/JobNecto.Infrastructure/Repositories/VacancyRepository.cs` | **UPDATE** — add SoftDeleteAsync |
| `backend/src/JobNecto.Application/Resumes/DeleteResumeCommandHandler.cs` | **UPDATE** — use SoftDeleteAsync |
| `backend/src/JobNecto.Application/Educations/DeleteEducationCommandHandler.cs` | **UPDATE** — use SoftDeleteAsync |
| `backend/tests/JobNecto.Tests/Application/Resumes/DeleteResumeCommandHandlerTests.cs` | **UPDATE** — mock type + verify calls |
| `backend/tests/JobNecto.Tests/Application/Educations/DeleteEducationCommandHandlerTests.cs` | **UPDATE** — mock type + verify calls |
| `_bmad-output/planning-artifacts/architecture/core-architectural-decisions.md` | **UPDATE** — Decisions 3 and 6 |

### Key Implementation Details

**`SoftDeletableRepository<T>` implementation:**

```csharp
public abstract class SoftDeletableRepository<T> : EditableRepository<T>, ISoftDeleteRepository<T>
    where T : SoftDeletableEntity
{
    protected SoftDeletableRepository(AppDbContext context) : base(context) { }

    public Task SoftDeleteAsync(T entity, CancellationToken ct)
    {
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        _dbSet.Update(entity);
        return Task.CompletedTask;
    }
}
```

`Task.CompletedTask` is correct — `_dbSet.Update` is synchronous; no `async/await` needed.

**`VacancyRepository` — implement directly, do NOT change base class:**
Vacancy does not need generic `UpdateAsync` (it has `UpdateMatchScoreAsync`). Changing its base to `SoftDeletableRepository<T>` would silently add an unintended `UpdateAsync` to the class and widen its capabilities without purpose. Implement `SoftDeleteAsync` inline instead.

**`IUserRepository` — drop redundant `IRepository<User>`:**
`IUserRepository` currently declares `IRepository<User>, IEditableRepository<User>` — since `IEditableRepository<User>` already extends `IRepository<User>`, the declaration is redundant. Remove the explicit `IRepository<User>` while adding `ISoftDeleteRepository<User>`:

```csharp
public interface IUserRepository : IEditableRepository<User>, ISoftDeleteRepository<User> { ... }
```

**`SoftDeletableEntity` uses fields, not properties:**
`IsDeleted` and `DeletedAt` are declared as **public fields** in `SoftDeletableEntity.cs` — assign them directly without `.` property setter syntax confusion. This is an existing pattern in the codebase.

**Test mock for `SoftDeleteAsync` must simulate entity mutation:**
Moq won't run the real implementation. Configure with a Callback to ensure handler assertions on entity state stay valid:

```csharp
_repoMock
    .Setup(x => x.SoftDeleteAsync(It.IsAny<Resume>(), It.IsAny<CancellationToken>()))
    .Callback<Resume, CancellationToken>((entity, _) =>
    {
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
    })
    .Returns(Task.CompletedTask);
```

**`CoverLetterTemplate` is not in `IUnitOfWork` yet — don't add it:**

Only change its base class from `EditableRepository<CoverLetterTemplate>` to `SoftDeletableRepository<CoverLetterTemplate>`. Epic 3 will add it to UnitOfWork as `IMutableRepository<CoverLetterTemplate>`.

**InMemory test: generate database name once per test, not per scope:**

```csharp
var dbName = Guid.NewGuid().ToString();
var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseInMemoryDatabase(dbName)
    .Options;
```

Reuse `options` across `await using` context instances in the same test. [Source: agent-learnings.md — "In-memory DB name changed per scope"]

### Zero Behavioral Change Guarantee

This is a pure refactoring. The following must be identical before and after:

- `DELETE /api/v1/resumes/{id}` → `204 No Content`, `403`, `404` semantics unchanged
- `DELETE /api/v1/educations/{id}` → same
- EF global query filter exclusion of soft-deleted records — unchanged, since `_dbSet.Update(entity)` in `SoftDeleteAsync` produces the same EF change-tracking behavior as the prior `UpdateAsync` call

### Agent Learnings to Apply

- Set entity timestamps explicitly in UTC in the layer that owns the logic — here `SoftDeletableRepository.SoftDeleteAsync`, not the handler. [Source: agent-learnings.md — "Set entity timestamps in handlers, not only DB defaults"]
- Prefer separate handler files; this story modifies existing files only. [Source: agent-learnings.md — "Prefer separate handler file"]
- Keep generated test data validator-compliant. Infrastructure tests skip validators; seed any valid shape. [Source: agent-learnings.md — "Keep generated test data validator-compliant"]
- Generate one database name per test provider, not inside the options lambda. [Source: agent-learnings.md — "In-memory DB name changed per scope"]

### Namespace Convention (Mandatory)

| File | Namespace |
| --- | --- |
| `ISoftDeleteRepository.cs` | `JobNecto.Application.Interfaces` |
| `IMutableRepository.cs` | `JobNecto.Application.Interfaces` |

| `SoftDeletableRepository.cs` | `JobNecto.Infrastructure.Repositories` |
| `SoftDeletableRepositoryTests.cs` | `JobNecto.Tests.Infrastructure` |

### References

- [Source: `backend/src/JobNecto.Domain/Entities/SoftDeletableEntity.cs`] — `IsDeleted` and `DeletedAt` are **fields**
- [Source: `backend/src/JobNecto.Application/Interfaces/IRepository.cs`] — base interface
- [Source: `backend/src/JobNecto.Application/Interfaces/IEditableRepository.cs`] — unchanged
- [Source: `backend/src/JobNecto.Application/Interfaces/IUnitOfWork.cs`] — property types to update
- [Source: `backend/src/JobNecto.Application/Interfaces/IUserRepository.cs`] — add `ISoftDeleteRepository`
- [Source: `backend/src/JobNecto.Application/Interfaces/IVacancyRepository.cs`] — add `ISoftDeleteRepository`
- [Source: `backend/src/JobNecto.Infrastructure/Repositories/EditableRepository.cs`] — parent of new SoftDeletableRepository
- [Source: `backend/src/JobNecto.Infrastructure/Repositories/BaseRepository.cs`] — root implementation
- [Source: `backend/src/JobNecto.Infrastructure/Repositories/VacancyRepository.cs`] — inline SoftDeleteAsync
- [Source: `backend/src/JobNecto.Infrastructure/Persistance/UnitOfWork.cs`] — field/property changes
- [Source: `_bmad-output/planning-artifacts/architecture/core-architectural-decisions.md` — Decision 3] — repository baseline
- [Source: `_bmad-output/planning-artifacts/architecture/core-architectural-decisions.md` — Decision 6] — soft delete pattern

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

Build error: `SoftDeletableRepository<T>` initially declared `ISoftDeleteRepository<T>` — needed `IMutableRepository<T>` so concrete repositories satisfy `IMutableRepository<T>` assignment in UnitOfWork. Fixed by changing class declaration.
Side effect: 8 existing handler test files used `Mock<IEditableRepository<T>>` for Resume/Education repos — updated all to `Mock<IMutableRepository<T>>`.

### Completion Notes List

All 13 ACs satisfied. Pure refactoring — zero behavioral change at HTTP level.

- Created `ISoftDeleteRepository<T>` and `IMutableRepository<T>` in Application/Interfaces.
- Created `SoftDeletableRepository<T>` abstract class in Infrastructure; 5 repositories reparented to it.
- `VacancyRepository` implements `SoftDeleteAsync` inline (no base-class change).
- `IUserRepository` and `IVacancyRepository` extended with `ISoftDeleteRepository<T>`.
- `IUnitOfWork` and `UnitOfWork` fields/properties updated to `IMutableRepository<T>` for Resume, Education, CoverLetter.
- Both delete handlers now call `SoftDeleteAsync`; 10 test files updated.
- 2 new infra tests added; 292/292 passed, Release + warnaserror clean.

### File List

backend/src/JobNecto.Application/Interfaces/ISoftDeleteRepository.cs (CREATED)
backend/src/JobNecto.Application/Interfaces/IMutableRepository.cs (CREATED)
backend/src/JobNecto.Infrastructure/Repositories/SoftDeletableRepository.cs (CREATED)
backend/tests/JobNecto.Tests/Infrastructure/SoftDeletableRepositoryTests.cs (CREATED)
backend/src/JobNecto.Application/Interfaces/IUnitOfWork.cs (UPDATED)
backend/src/JobNecto.Application/Interfaces/IUserRepository.cs (UPDATED)
backend/src/JobNecto.Application/Interfaces/IVacancyRepository.cs (UPDATED)
backend/src/JobNecto.Infrastructure/Persistance/UnitOfWork.cs (UPDATED)
backend/src/JobNecto.Infrastructure/Repositories/ResumeRepository.cs (UPDATED)
backend/src/JobNecto.Infrastructure/Repositories/EducationRepository.cs (UPDATED)
backend/src/JobNecto.Infrastructure/Repositories/CoverLetterRepository.cs (UPDATED)
backend/src/JobNecto.Infrastructure/Repositories/CoverLetterTemplateRepository.cs (UPDATED)
backend/src/JobNecto.Infrastructure/Repositories/UserRepository.cs (UPDATED)
backend/src/JobNecto.Infrastructure/Repositories/VacancyRepository.cs (UPDATED)
backend/src/JobNecto.Application/Resumes/DeleteResumeCommandHandler.cs (UPDATED)
backend/src/JobNecto.Application/Educations/DeleteEducationCommandHandler.cs (UPDATED)
backend/tests/JobNecto.Tests/Application/Resumes/DeleteResumeCommandHandlerTests.cs (UPDATED)
backend/tests/JobNecto.Tests/Application/Educations/DeleteEducationCommandHandlerTests.cs (UPDATED)
backend/tests/JobNecto.Tests/Application/Resumes/CreateResumeCommandHandlerTests.cs (UPDATED)
backend/tests/JobNecto.Tests/Application/Resumes/UpdateResumeCommandHandlerTests.cs (UPDATED)
backend/tests/JobNecto.Tests/Application/Resumes/ListResumesHandlerTests.cs (UPDATED)
backend/tests/JobNecto.Tests/Application/Resumes/GetResumeHandlerTests.cs (UPDATED)
backend/tests/JobNecto.Tests/Application/Educations/CreateEducationCommandHandlerTests.cs (UPDATED)
backend/tests/JobNecto.Tests/Application/Educations/UpdateEducationCommandHandlerTests.cs (UPDATED)
backend/tests/JobNecto.Tests/Application/Educations/ListEducationsQueryHandlerTests.cs (UPDATED)
backend/tests/JobNecto.Tests/Application/Educations/GetEducationQueryHandlerTests.cs (UPDATED)
_bmad-output/planning-artifacts/architecture/core-architectural-decisions.md (UPDATED)

## Change Log

- 2026-05-05: Implemented story R.1 — introduced `ISoftDeleteRepository<T>`, `IMutableRepository<T>`, and `SoftDeletableRepository<T>`; reparented 5 repositories; refactored delete handlers to use `SoftDeleteAsync`; updated 10 test files; 292/292 tests passing.
- 2026-05-06: Code review completed. 0 decision-needed, 0 patch, 2 deferred, 10 dismissed.

### Review Findings

- [x] \[Review\]\[Defer\] CancellationToken unused in SoftDeleteAsync \[SoftDeletableRepository.cs, VacancyRepository.cs\] — deferred, pre-existing pattern (EditableRepository.UpdateAsync has identical ct-ignoring behavior)
- [x] \[Review\]\[Defer\] DateTime.UtcNow hardcoded in SoftDeletableRepository/VacancyRepository — deferred, pre-existing pattern already logged from story 2-8
