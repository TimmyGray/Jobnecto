# Implementation Checklist for Phase B

**Database & Entities:**

- [ ] Add `IsDeleted` and `DeletedAt` timestamps to: Resume, Education, CoverLetterTemplate, CoverLetter, Vacancy
- [ ] Add `[Timestamp]` RowVersion to all entities for optimistic locking
- [ ] Configure global query filters for soft-delete entities
- [ ] Configure PostgreSQL cascade rules in OnModelCreating (User→Resume→CoverLetter hard-deletes)
- [ ] Create migration with all schema changes

**Handlers & Repositories:**

- [ ] Implement user-scoped repository methods (GetByUserIdAsync, ListByUserIdAsync, etc.)
- [ ] Keep `/api/v1/users/me` profile-only; serve related user-owned resources via dedicated user-scoped routes
- [ ] Soft-delete handlers: Set IsDeleted=true, propagate cascade soft-deletes to children
- [ ] All mutation handlers: Verify ownership before allowing changes (`403 Forbidden` when ownership is violated)
- [ ] All query handlers: Filter by UserId in request (user sees only their own data)

**Testing:**

- [ ] Soft-delete audit fixtures: Verify deleted data excluded from queries but exists in DB
- [ ] Ownership violation suite: Run against all mutation handlers
- [ ] Cascade soft-delete tests: Resume soft-delete→CoverLetter soft-delete
- [ ] Cascade hard-delete tests (future): User hard-delete→Resume hard-delete→CoverLetter hard-delete
- [ ] CancellationToken timeout tests: At least one per resource

**Logging & Audit:**

- [ ] Log all hard-delete operations with timestamp, user ID, affected records
- [ ] Application logs or database audit table (depending on compliance needs)

**Phase C Readiness:**

- [ ] JWT claim extraction and transport policy in Phase B can be extended to role-based controls in Phase C without handler refactoring
- [ ] Handlers already receive UserId as parameter; no refactoring needed
- [ ] Ownership checks are centralized per handler; easy to audit for Phase C

These decisions are codified to ensure **consistency across all Phase B endpoints** and to make your developers' jobs straightforward: follow the patterns, and the architecture handles the rest.
