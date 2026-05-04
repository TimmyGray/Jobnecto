# Summary of Architectural Decisions

| Decision | Approach | Key Benefit |
|----------|----------|------------|
| Request Handling | MediatR Commands/Queries | Consistent request flow, testable business logic |
| Validation | FluentValidation + Handler layer | Separation of concerns, reusable validators |
| Data Access | Repository pattern with UnitOfWork | Abstraction from EF Core, testable with in-memory |
| Errors | RFC 7808 Problem Details | Standardized, frontend-friendly error format |
| Async | Full chain with CancellationToken | Graceful timeout handling, responsive API |
| Soft Delete + Cascades | EF Core global filters + PostgreSQL FK cascades + Audit logging | Data safety, referential integrity, compliance trail |
| Ownership Model | User-scoped repositories + Handler ownership checks | Only users see their own resumes; Phase C ready |
| Concurrency Control | Optimistic locking (RowVersion/Timestamp) | Prevents lost updates under concurrent load |

---
