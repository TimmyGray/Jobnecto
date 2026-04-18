# Story 1.1: JWT Authentication & Global Exception Handling Infrastructure

Status: ready-for-dev

<!-- Note: Complete story prepared with all context for implementation. -->

## Story

As a **developer**,
I want JWT bearer token authentication middleware and a global exception handling middleware wired into the API,
So that all endpoints are secured by token, UserId is extracted from claims for every request, and all errors return consistent RFC 7807 Problem Details responses. This is the foundation infrastructure for all subsequent features in the platform.

## Acceptance Criteria

### JWT Authentication (AC 1-3)

1. [x] JWT bearer token authentication middleware is registered in `Program.cs` using `AddAuthentication()` with JWT bearer scheme.
2. [x] Valid JWT tokens with `sub` claim (or `userId` claim as fallback) must be required on all protected endpoints; requests without token or with invalid/expired token return `401 Unauthorized`.
3. [x] UserId is reliably extracted from JWT claims via `GetCurrentUserId()` helper method and is available to all handlers and controllers for ownership validation.

### Exception Handling (AC 4-9)

4. [x] A global exception handler is registered in the API pipeline using `IExceptionHandler` to catch all unhandled exceptions.
5. [x] Unhandled exceptions return an RFC 7807 Problem Details response (`application/problem+json`) with a `500 Internal Server Error` status.
6. [x] Stack traces must **never** be included in the response body when running in Production environment.
7. [x] Validation failures (FluentValidation) must return a `400 Bad Request` Problem Details response with field-level errors in `errors` dictionary.
8.  ] Task 1: Define Application Exceptions (AC: 8)
  - [ ] Create `Exceptions` folder in `JobNecto.Application`.
  - [ ] Implement base exception classes: `NotFoundException`, `ForbiddenException`, `UnauthorizedException`, `ConflictException`.
  - [ ] Each exception class has a descriptive `Message` property for the Problem Details `detail` field.

- [ ] Task 2: Setup JWT Authentication Middleware (AC: 1, 2, 3)
  - [ ] Add JWT bearer authentication scheme in `Program.cs` using `AddAuthentication("Bearer")`.
  - [ ] Configure JWT validation: issuer, audience, signing key from `appsettings.json` (or local config).
  - [ ] Wire `app.UseAuthentication()` and `app.UseAuthorization()` in the middleware pipeline.
  - [ ] Create `AuthContext` utility class with `GetCurrentUserId()` extension method on `HttpContext` to extract `sub` or `userId` claim.
  - [ ] Ensure invalid/missing/expired tokens return `401 Unauthorized` before reaching handlers.

- [ ] Task 3: Setup Global Exception Handling (AC: 4, 5, 6, 9)
  - [ ] Implement `GlobalExceptionHandler` class using `IExceptionHandler` interface in `JobNecto.API/Infrastructure/ExceptionHandling/`.
  - [ ] Register handler in `Program.cs` via `AddExceptionHandler<GlobalExceptionHandler>()` and `AddProblemDetails()`.
  - [ ] Exception handler maps all exception types to RFC 7807 Problem Details responses:
    - `ValidationException` → `400 Bad Request` with `errors` field (field name → string[] of messages)
    - `NotFoundException` → `404 Not Found`
    - `ForbiddenException` → `403 Forbidden`
    - `UnauthorizedException` → `401 Unauthorized`
    - `ConflictException` → `409 Conflict`
    - All others → `500 Internal Server Error`
  - [ ] Stack traces **excluded** from response body in Production; included in Development for debugging.
  - [ ] All exceptions logged to Serilog with request context (path, method, userId if available).
  - [ ] `traceId` from `HttpContext.TraceIdentifier` included in all error responses.

- [ ] Task 4: Integration & Unit Testing (AC: 4-9)
  - [ ] Create `AuthenticationTests` class in `JobNecto.Tests` to verify:
    - Valid JWT token allows request to proceed.
    - Missing token returns `401 Unauthorized` with Problem Details.
    - Expired/invalid token returns `401 Unauthorized`.
    - UserId claim extracted correctly from token.
  - [ ] Create `ExceptionHandlingTests` class using `WebApplicationFactory<Program>` to verify:
    - `ValidationException` returns `400` with structured errors.
    - `NotFoundException` returns `404` with message in `detail` field.
    - `ForbiddenException` returns `403`.
    - `ConflictException` returns `409`.
    - Unhandled exception returns `500` without stack trace in Production.
    - All responses include `traceId` field.
  - [ ] Tests use in-memory test host; no real DB required
  - [x] Map `UnauthorizedException` to `401 Unauthorized`.
- [x] Task 4: Integration Testing (AC: 9)
  - [x] Create `JobNecto.Tests.Integration` project if missing, or use existing `JobNecto.Tests`.
  - [x] Implement `ExceptionHandlingTests` using `WebApplicationFactory`.
  - [x] Verify 400 for validation, 404 for missing resources (mocked), and 500 for unhandled throws.

## Dev Notes

### Clean Architecture Constraints

- **Exception classes** reside in Application layer, domain-agnostic (no HTTP or middleware references).
- **Exception handler** (IExceptionHandler) resides in API layer only; maps application exceptions to HTTP responses.
- JWT validation happens in ASP.NET Core authentication middleware (API layer), not in handlers.
- `AuthContext.GetCurrentUserId()` is a convenience method for controllers and handlers to extract the claim; **never assume UserId is present** — JWT middleware ensures it before protected endpoints.

### ASP.NET Core 10 Specific Details

- Use `AddAuthentication("Bearer")` with `AddJwtBearer()` for JWT scheme.
- Use `IExceptionHandler` interface and `AddExceptionHandler<T>()` in DI (newer pattern than middleware delegates).
- Use `AddProblemDetails()` to enable RFC 7807 automatic serialization.
- `HttpContext.TraceIdentifier` is auto-populated by ASP.NET Core; use as-is.
- Serilog context binding: log with `LogContext.PushProperty("UserId", userId)` for correlation.

### JWT Token Assumption for This Story

**This story assumes JWT token generation endpoint is NOT YET implemented** — Story 1.2 (Create User Account) will add the endpoint and token issuing logic. For now:
- Tests can manually create valid JWT tokens using `System.IdentityModel.Tokens.Jwt` library (already in Microsoft ecosystem).
- Test helper: `GenerateTestToken(userId)` to create a valid bearer token.
- Production auth config (issuer, audience, signing key) comes from `appsettings.json` — **for local dev, mock or use a test key in `appsettings.Development.json`**.

### Project Structure Notes

**New Folders:**
- `JobNecto.Application/Exceptions`
- `JobNecto.API/Infrastructure/ExceptionHandling`

**Modified Files:**
- `JobNecto.API/Program.cs` — Add JWT auth middleware and exception handler registration
- `JobNecto.API/Controllers/*` — Add `[Authorize]` attribute to protected endpoints (if not already present)
- `JobNecto.Tests` — Add test classes for authentication and exception handling

### Key Implementation Checkpoints

1. **Task 1 complete:** Run `dotnet build backend/JobNecto.slnx` — should pass with no errors.
2. **Task 2 complete:** Start app in Development mode; verify unauthenticated request to a protected endpoint returns `401`.
3. **Task 3 complete:** Throw a test exception; verify response is RFC 7807 Problem Details with `traceId` included.
4. **Task 4 complete:** Run `dotnet test backend/JobNecto.slnx` — all new tests pass.

### References

- [RFC 7807 Problem Details](https://tools.ietf.org/html/rfc7807)
- [ASP.NET Core JWT Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/jwt-authn)
- [IExceptionHandler in .NET 8+](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/error-handling)
- Project: [Source: GitHub Issue #36 + Phase B Epic 1]

## Dev Agent Record

_To be completed after implementation. Document decisions, tests created, and any significant findings here._

### Agent Model Used
TBD

### Debug Log References
TBD

### Completion Notes List
TBD

### File List
- `backend/src/JobNecto.Application/Exceptions/NotFoundException.cs` (NEW)
- `backend/src/JobNecto.Application/Exceptions/ForbiddenException.cs` (NEW)
- `backend/src/JobNecto.Application/Exceptions/UnauthorizedException.cs` (NEW)
- `backend/src/JobNecto.Application/Exceptions/ConflictException.cs` (NEW)
- `backend/src/JobNecto.API/Infrastructure/ExceptionHandling/GlobalExceptionHandler.cs` (NEW)
- `backend/src/JobNecto.API/Infrastructure/AuthContext.cs` (NEW)
- `backend/src/JobNecto.API/Program.cs` (MODIFIED — JWT auth + exception handler registration)
- `backend/src/JobNecto.API/appsettings.Development.json` (MODIFIED or NEW — JWT test config)
- `backend/tests/JobNecto.Tests/API/AuthenticationTests.cs` (NEW)
- `backend/tests/JobNecto.Tests/API/ExceptionHandlingTests.cs` (NEW)

### Review Findings

_To be completed after code review. Document any issues found and their fixes._
