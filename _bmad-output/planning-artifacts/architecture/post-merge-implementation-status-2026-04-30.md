# Post-Merge Implementation Status (2026-04-30)

- Stories `1-5-password-hashing-token-policy-hardening`, `2-1-create-resume`, `2-2-list-resumes`, `2-3-get-resume-detail`, and `2-4-update-resume` are merged and active on `master`.
- `Program.cs` now wires infrastructure (`AddInfrastructure`), JWT authentication, CORS, and global exception handling in the HTTP pipeline.
- Security baseline is implemented with `IPasswordHasher` (`Pbkdf2PasswordHasher`) and authenticated token refresh (`POST /api/v1/users/token/refresh`).
- The application now has a complete resume vertical slice: `POST /api/v1/resumes` (create), `GET /api/v1/resumes` (list), `GET /api/v1/resumes/{id}` (detail), and `PATCH /api/v1/resumes/{id}` (update) with MediatR command/query handling, FluentValidation, ownership-based 404 responses, and mapper-based DTO-to-entity conversion.
- Token transport policy is explicit: browser flows rely on HTTP-only secure cookies; bearer clients can consume body tokens from refresh responses.
- Architecture decisions around conflict handling are enforced in production code (`DbUpdateException` unique-constraint mapping to HTTP 409) and concurrency integration tests.
- DB Command Timing: Added `DbCommandTimingInterceptor` to JobNecto.Infrastructure to monitor and log slow database queries (merged in story 2-4).
