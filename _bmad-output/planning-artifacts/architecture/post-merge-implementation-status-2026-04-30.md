# Post-Merge Implementation Status (2026-05-10)

- Stories `1-5-password-hashing-token-policy-hardening`, `2-1-create-resume`, `2-2-list-resumes`, `2-3-get-resume-detail`, `2-4-update-resume`, `2-5-delete-resume`, `2-6-create-education-record`, `2-7-list-education-records`, `2-8-get-update-delete-education-records`, `3-1-create-cover-letter-template`, `3-2-list-cover-letter-templates`, `3-3-get-cover-letter-template-detail`, `3-4-update-cover-letter-template`, and `3-5-delete-cover-letter-template` are merged and active on `master`.
- `Program.cs` now wires infrastructure (`AddInfrastructure`), JWT authentication, CORS, and global exception handling in the HTTP pipeline.
- Security baseline is implemented with `IPasswordHasher` (`Pbkdf2PasswordHasher`) and authenticated token refresh (`POST /api/v1/users/token/refresh`).
- The application now has complete resume and education vertical slices: `POST`, `GET` list, `GET {id}`, `PATCH {id}`, and `DELETE {id}` endpoints for `/api/v1/resumes` and `/api/v1/educations`, with MediatR command/query handling, FluentValidation, ownership checks, soft-delete behavior, and mapper-based DTO-to-entity conversion.
- Cover letter template vertical slices now include full CRUD: `POST`, `GET` list, `GET {id}`, `PATCH {id}`, and `DELETE {id}` on `/api/v1/cover-letter-templates`, with ownership checks and soft-delete semantics.
- Token transport policy is explicit: browser flows rely on HTTP-only secure cookies; bearer clients can consume body tokens from refresh responses.
- Architecture decisions around conflict handling are enforced in production code (`DbUpdateException` unique-constraint mapping to HTTP 409) and concurrency integration tests.
- DB Command Timing: Added `DbCommandTimingInterceptor` to JobNecto.Infrastructure to monitor and log slow database queries (merged in story 2-4).
- Epic 2 retrospective is complete and identifies the architecture as stable, with follow-up decisions needed for ownership-aware single-record reads and timestamp/clock policy. Epic 3 now enforces database-backed template-name uniqueness with concurrent collision coverage in tests and includes delete coverage for template lifecycle completion.
- Current verification caveat: the Epic 2 retrospective recorded local `dotnet test backend/JobNecto.slnx` and Release build attempts failing without useful diagnostics. Treat implementation logs as positive evidence, but diagnose local solution-runner behavior before relying on a fresh green gate.

