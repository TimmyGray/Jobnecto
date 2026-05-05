# Summary of Architectural Decisions

| Decision | Current Approach After Epic 2 | Key Benefit | Status |
|----------|-------------------------------|-------------|--------|
| Request Handling | MediatR commands/queries per vertical slice | Consistent request flow, testable business logic | Active |
| Validation | FluentValidation for request shape plus handler/business checks | Separation of concerns, reusable validators | Active; needs checklist hardening |
| Data Access | Generic `IRepository<T>` / `IEditableRepository<T>` with UnitOfWork; specialized repositories only for distinct query behavior | Keeps CRUD resources simple while allowing custom queries where justified | Active |
| Errors | RFC 7808 Problem Details through global exception handling | Standardized, frontend-friendly error format | Active |
| Conflict Handling | Product-level uniqueness must be database-backed and mapped to `409 Conflict` | Race-safe API contract under concurrent requests | Active; must apply to Epic 3 templates |
| Async | Full async chain with CancellationToken propagation | Graceful timeout handling, responsive API | Active |
| Soft Delete | EF Core global query filters plus `IsDeleted` / `DeletedAt` markers | Deleted data is excluded consistently without handler-level filters everywhere | Active |
| Ownership Model | User-scoped list queries through `PagedQuery.UserId`; handler ownership checks for detail and mutations | Users only access their own resources; route/API layer stays thin | Active with deferred design concern |
| Single-Record Ownership Reads | Current handlers fetch by ID, then check ownership | Behavior is correct; implementation is easy to audit | Accepted for Epic 2; revisit before Epic 3 detail/update/delete |
| Timestamp Policy | `DateTime` persisted in UTC by convention; direct `DateTime.UtcNow` in handlers; cursor uses `LastSeenUpdatedAt` | Simple and consistent with current code | Deferred hardening: DateTimeOffset/clock/cursor normalization |
| Concurrency Control | Database unique constraints for uniqueness races; no RowVersion baseline yet | Prevents known uniqueness races | Partial; RowVersion remains future hardening |
