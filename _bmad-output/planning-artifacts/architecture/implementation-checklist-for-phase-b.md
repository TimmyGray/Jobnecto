# Implementation Checklist for Phase B

This checklist was revised after Epic 2 completion on 2026-05-05. It distinguishes completed Phase B baseline work from hardening that remains deferred.

## Database & Entities

- [x] Add `IsDeleted` and `DeletedAt` timestamps to soft-deletable entities.
- [x] Configure global query filters for soft-delete entities.
- [x] Create and maintain EF Core migrations through Infrastructure.
- [x] Add partial unique indexes for active user login/email/phone constraints.
- [ ] Add `[Timestamp]` / RowVersion optimistic locking where a future story requires lost-update protection.
- [ ] Add Epic 3 database-backed per-user cover letter template-name uniqueness.

## Handlers & Repositories

- [x] Keep `/api/v1/users/me` profile-only; serve related user-owned resources via dedicated user-scoped routes.
- [x] Use generic `IRepository<T>` / `IEditableRepository<T>` for CRUD-like resources.
- [x] Use specialized repository interfaces only for resources with distinct queries, such as users and vacancy filtering.
- [x] Soft-delete handlers set `IsDeleted = true` and `DeletedAt = DateTime.UtcNow`.
- [x] Mutation handlers verify ownership before allowing changes.
- [x] List query handlers pass authenticated `UserId` into `PagedQuery`.
- [ ] Decide whether Epic 3 introduces ownership-aware single-record access before implementing template detail/update/delete.
- [ ] Decide whether `PagedQuery.UserId` remains acceptable long term or moves to an Application-layer query object.
- [ ] Expose cover letter template persistence through UnitOfWork as part of Epic 3 implementation.

## Testing

- [x] Resume and Education handler/API tests cover core create/list/detail/update/delete behavior.
- [x] Ownership behavior is covered for shipped Resume and Education endpoints.
- [x] Soft-delete behavior is covered for shipped Resume and Education endpoints.
- [x] Conflict handling has database unique-constraint mapping and concurrency coverage for implemented user constraints.
- [ ] Add Epic 3 concurrent create/update tests for per-user template-name uniqueness.
- [ ] Diagnose current local `dotnet test backend/JobNecto.slnx` and Release build failures recorded in the Epic 2 retrospective.
- [ ] Add cursor pagination end-to-end tests where endpoint-level coverage is still deferred.
- [ ] Add explicit soft-delete exclusion tests for endpoints where coverage currently relies on repository/global-filter tests.

## Validation & Error Policy

- [x] FluentValidation pipeline runs before handlers.
- [x] Global exception handling returns RFC 7808 Problem Details.
- [x] Database uniqueness violations map to `409 Conflict`.
- [ ] Create a validator checklist for null, empty-string, whitespace, max-length, enum, and cross-field rules.
- [ ] Decide privacy policy for conflict details that include submitted identifiers.
- [ ] Decide Cloudinary-not-configured behavior policy.

## Time, Transactions, And Reliability

- [x] Creation/update/delete handlers set timestamps explicitly where current tests require provider-independent values.
- [ ] Define project timestamp policy: `DateTime` vs `DateTimeOffset`, cursor timestamp kind handling, and UTC normalization.
- [ ] Decide whether to introduce an injectable clock for time-sensitive handlers and tests.
- [ ] Decide FK race handling between validation and persistence.
- [ ] Decide whether repository update/save failure behavior needs cleanup of tracked dirty entities after failed `SaveChangesAsync`.
- [ ] Decide idempotency support for retry-prone POST endpoints.

## Logging & Audit

- [x] Add `DbCommandTimingInterceptor` for slow database query monitoring.
- [ ] Define production audit requirements for hard-delete operations.
- [ ] Implement hard-delete audit trail when account deletion or administrative purge stories are scheduled.

## Phase C / Later Readiness

- [x] JWT claim extraction and transport policy can be extended to role-based controls without changing the handler shape.
- [x] Handlers receive authenticated user ID as input rather than reading HTTP context directly.
- [x] Ownership checks are easy to audit per handler.
- [ ] Add role/authorization policy documentation when Phase C introduces non-owner access paths.
