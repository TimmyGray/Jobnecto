# Epic 2 Architecture Revision (2026-05-05)

## Review Sources

- `_bmad-output/archive/implementation-artifacts/epic-2-retro-2026-05-05.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- Current architecture shards in `_bmad-output/planning-artifacts/architecture/`
- Current backend code under `backend/src`

## Current Implemented Baseline

Epic 2 is complete at the architecture-pattern level. Resume and Education now use the same vertical-slice structure:

- API controllers extract the authenticated user ID and construct MediatR commands/queries.
- Application handlers own business flow, ownership checks, soft-delete marking, and response mapping.
- FluentValidation handles request-shape validation before handlers run.
- Infrastructure repositories use EF Core through generic repository abstractions and the UnitOfWork.
- EF Core global query filters exclude soft-deleted resources.
- User-owned list endpoints use cursor pagination with `PagedQuery.UserId`, `LastSeenId`, and `LastSeenUpdatedAt`.

This confirms the Clean Architecture direction held through Epic 2. Domain entities remain persistence-ignorant, Application owns contracts and handlers, Infrastructure owns EF behavior, and API remains thin.

## Corrections Applied To Architecture Docs

The previous architecture text mixed intended future patterns with actual implemented patterns. This revision updates the docs to make these boundaries explicit:

- Repository baseline is generic `IRepository<T>` / `IEditableRepository<T>` plus selected specialized interfaces where needed, not one bespoke repository interface per aggregate.
- `PagedQuery.UserId` is the current pragmatic user-scoping mechanism for list endpoints, even though it remains a deferred design concern because it places ownership filtering in a Domain value object.
- Single-record reads currently fetch by ID and then perform handler-level ownership checks. Ownership-aware `GetByIdForUserAsync` is not implemented yet.
- RowVersion/optimistic locking is a future hardening direction, not a completed Epic 2 baseline.
- `DateTime.UtcNow` and `DateTime` cursor timestamps remain the active pattern. A `DateTimeOffset` and clock-abstraction decision is still pending.
- Cover letter template UnitOfWork exposure and per-user template-name uniqueness are Epic 3 work, not completed Phase B infrastructure.

## Accepted Patterns After Epic 2

- Keep `/api/v1/users/me` profile-only. Related resources belong on dedicated routes.
- Use `404` for cross-user detail reads where existence should not leak.
- Use `403` for cross-user mutations where stories require explicit forbidden mutation behavior.
- Continue relying on EF global query filters for soft-delete exclusion.
- Continue using generic repositories for CRUD-like user-owned resources unless a resource has query behavior that genuinely needs a specialized repository.
- Continue mapping database uniqueness violations to `409 Conflict` for constraints that form part of the product contract.

## Decisions Needed Before Epic 3

### Template Name Uniqueness

Epic 3 must enforce per-user cover letter template-name uniqueness with a database constraint. A validator or pre-check alone is not enough because concurrent requests can bypass it.

Required direction:

- Add a unique filtered index over `(UserId, Name)` or the actual template-name column used by the implementation.
- Filter the index to active records if soft-deleted templates should not reserve names.
- Add concurrent integration tests for create/update collisions.
- Map unique-constraint violations to `409 Conflict`.

### Ownership-Aware Single-Record Access

Epic 2 detail handlers load the record by ID before checking ownership. This is behaviorally correct but creates unnecessary cross-user reads and spreads ownership logic across handlers.

Recommended Epic 3 decision:

- Introduce a narrowly scoped ownership-aware read helper or repository method when implementing template detail/update/delete.
- Preserve current response semantics: cross-user detail read returns `404`; cross-user mutation returns `403` when required by acceptance criteria.
- Do not refactor all existing Resume/Education handlers unless the change is intentionally scheduled.

### Timestamp And Clock Policy

Deferred work now repeats around `DateTime`, `DateTimeOffset`, cursor timestamp kind, and direct `DateTime.UtcNow`.

Recommended project-level decision:

- Store persisted timestamps consistently in UTC.
- Normalize incoming cursor timestamps at the API/Application boundary before repository comparison.
- Introduce an injectable clock before adding more time-sensitive handlers if tests begin depending on exact timestamps.
- Treat `DateTimeOffset` migration as a separate cross-cutting change rather than a story-local edit.

### Validator Checklist

Epic 2 surfaced repeated validator edge cases: empty strings, whitespace, max length, enum bounds, and cross-field rules with weak property names.

Recommended Epic 3 rule:

- Every create/update story should explicitly decide null, empty, and whitespace semantics for each string field.
- Cross-field validation errors should use a stable client-facing key, not an empty property name, when the API contract needs structured field errors.
- Business uniqueness that is externally observable as conflict must be backed by a database constraint.

## Production-Hardening Backlog

These items should not be treated as blockers for starting Epic 3 unless the story directly depends on them, but they must be resolved before production readiness:

- Cloudinary-not-configured behavior policy.
- Conflict-detail privacy policy for submitted identifiers.
- `DateTime` vs `DateTimeOffset` migration policy.
- Idempotency support for retry-prone POST endpoints.
- FK race handling between validation and persistence.
- Behavior after `UpdateAsync` plus `SaveChangesAsync` failure leaves a tracked entity modified in the current scope.
- Local verification blocker where retrospective build/test commands failed without useful diagnostics.

## Architecture Verdict

Epic 2 did not invalidate the Clean Architecture direction. The main documentation risk was drift: several shards described future or idealized patterns as if they were already implemented. The revised guidance is to keep the generic repository and vertical-slice baseline, make Epic 3 uniqueness database-backed from the start, and avoid copying the known timestamp and validator ambiguities into new resources without an explicit decision.

