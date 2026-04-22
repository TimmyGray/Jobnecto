# Story 1.1: JWT Authentication & Global Exception Handling Infrastructure

Status: ✅ COMPLETE (100% — Exception Handling DONE, JWT Authentication DONE)

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

### Task-by-Task Status

- [x] Task 1: Define Application Exceptions (AC: 8)
  - [x] Create `Exceptions` folder in `JobNecto.Application`.
  - [x] Implement base exception classes: `NotFoundException`, `ForbiddenException`, `UnauthorizedException`, `ConflictException`.
  - [x] Each exception class has a descriptive `Message` property for the Problem Details `detail` field.

- [ ] Task 2: Setup JWT Authentication Middleware (AC: 1, 2, 3)
  - [x] Add JWT bearer authentication scheme in `Program.cs` using `AddAuthentication("Bearer")`.
  - [x] Configure JWT validation: issuer, audience, signing key from `appsettings.json` (or local config).
  - [x] Wire `app.UseAuthentication()` and `app.UseAuthorization()` in the middleware pipeline.
  - [x] Create `AuthContext` utility class with `GetCurrentUserId()` extension method on `HttpContext` to extract `sub` or `userId` claim.
  - [x] Ensure invalid/missing/expired tokens return `401 Unauthorized` before reaching handlers.

- [x] Task 3: Setup Global Exception Handling (AC: 4, 5, 6, 7)
  - [x] Implement `GlobalExceptionHandler` class using `IExceptionHandler` interface in `JobNecto.API/Infrastructure/ExceptionHandling/`.
  - [x] Register handler in `Program.cs` via `AddExceptionHandler<GlobalExceptionHandler>()` and `AddProblemDetails()`.
  - [x] Exception handler maps all exception types to RFC 7807 Problem Details responses:
    - [x] `ValidationException` → `400 Bad Request` with `errors` field (field name → string[] of messages)
    - [x] `NotFoundException` → `404 Not Found`
    - [x] `ForbiddenException` → `403 Forbidden`
    - [x] `UnauthorizedException` → `401 Unauthorized`
    - [x] `ConflictException` → `409 Conflict`
    - [x] All others → `500 Internal Server Error`
  - [x] Stack traces **excluded** from response body in Production; included in Development for debugging.
  - [x] All exceptions logged to Serilog with request context (path, method, userId if available).
  - [x] `traceId` from `HttpContext.TraceIdentifier` included in all error responses.

- [x] Task 4: Integration & Unit Testing (AC: 4-7)
  - [x] Create `AuthenticationTests` class in `JobNecto.Tests` to verify:
    - [x] Valid JWT token allows request to proceed.
    - [x] Missing token returns `401 Unauthorized` with Problem Details.
    - [x] Expired/invalid token returns `401 Unauthorized`.
    - [x] UserId claim extracted correctly from token.
  - [x] Create `ExceptionHandlingTests` class using `WebApplicationFactory<Program>` to verify:
    - [x] `ValidationException` returns `400` with structured errors.
    - [x] `NotFoundException` returns `404` with message in `detail` field.
    - [x] `ForbiddenException` returns `403`.
    - [x] `ConflictException` returns `409`.
    - [x] Unhandled exception returns `500` without stack trace in Production.
    - [x] All responses include `traceId` field.
  - [x] Tests use in-memory test host; no real DB required.

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

### Completed Work (As of 2026-04-18)

**Tasks Completed:**
- Task 1: Application Exception Classes (DONE)
  - All four custom exception classes created with descriptive Message properties
  - Classes: NotFoundException, ForbiddenException, UnauthorizedException, ConflictException
  - All include XML documentation comments

- Task 2: JWT Authentication Middleware (DONE)
  - AddAuthentication("Bearer") with JWT bearer scheme registered in Program.cs
  - JWT configuration (issuer, audience, signing key) loaded from appsettings hierarchy
  - Token validation parameters: SymmetricSecurityKey, issuer/audience/lifetime validation enabled, ClockSkew=0
  - app.UseAuthentication() and app.UseAuthorization() properly sequenced in middleware pipeline (routing → auth → authz)
  - AuthContext utility class created with GetCurrentUserId() extension method on HttpContext
  - Fallback claim extraction: ClaimTypes.NameIdentifier → "sub" → "userId"
  - Invalid/missing/expired tokens return 401 Unauthorized before reaching handlers
  - JWT secret keys configured in all appsettings files (64-character minimum for HMAC-SHA256)

- Task 3: Global Exception Handling (DONE)
  - GlobalExceptionHandler implemented and registered in Program.cs
  - Middleware chain: AddExceptionHandler<GlobalExceptionHandler>() + AddProblemDetails()
  - Exception → HTTP status code mapping implemented and tested
  - Stack trace exclusion in Production environment verified
  - traceId included in all error responses via Activity.Current?.Id
  - Serilog integration for error logging

- Task 4 (Partial): Integration & Unit Testing (DONE)
  - ExceptionHandlingTests class created using WebApplicationFactory
  - AuthenticationTests class created with comprehensive JWT validation scenarios
  - TestEndpointsStartupFilter and AuthenticationTestEndpointsStartupFilter provide test endpoints
  - AuthenticationTestFactory with in-memory configuration override for test-specific JWT settings
  - All 89 tests passing: 6 exception handling tests + 8 authentication tests + 75 existing tests
  - Tests verify valid/invalid/expired/wrong-key/wrong-issuer/wrong-audience token handling
  - Tests verify UserId claim extraction from tokens
  - Token validation middleware properly integrated with authorization attribute enforcement

**Tasks Remaining:**
- Story 1.1 COMPLETE — All acceptance criteria satisfied

### Key Implementation Details

- **Exception Handler Location:** `backend/src/JobNecto.API/Infrastructure/ExceptionHandling/GlobalExceptionHandler.cs`
- **JWT Configuration:** All three appsettings files updated with JwtSettings section
- **AuthContext Location:** `backend/src/JobNecto.API/Infrastructure/AuthContext.cs`
- **Test Factory:** Uses `WebApplicationFactory<ApiAssemblyMarker>` with custom `AuthenticationTestFactory`
- **Test Configuration Override:** Clears configuration sources and adds in-memory test values only
- **Test Secret Key:** "Thisisatestsecretkeyforjwttokentesting1234567890abcdefghijklmn" (64 characters)
- **Test Issuer/Audience:** "JobNecto" for both
- **Problem Details Format:** RFC 7807 compliant with `application/problem+json` content type
- **Error Mapping:** Validation → 400 with errors dict; NotFound → 404; Forbidden → 403; Unauthorized → 401; Conflict → 409; Other → 500

### Build & Test Status

- **Build:** `dotnet build backend/JobNecto.slnx` — ✅ PASSING (all 6 projects compiled in 1.7s)
- **Tests:** `dotnet test backend/JobNecto.slnx` — ✅ PASSING (89/89 tests pass in 1.4s)
  - ExceptionHandlingTests: 6 tests passing
  - AuthenticationTests: 8 tests passing (ValidJwtToken_Should_Succeed NOW FIXED)
  - Existing tests: 75 tests passing
- **Current Branch:** `feature/phase-b-core-api-resources`

### Story Completion Status

**Story 1.1: JWT Authentication & Global Exception Handling Infrastructure**
- Status: ✅ COMPLETE (100%)
- All acceptance criteria satisfied
- All implementation tasks completed
- All tests passing

### Agent Model Used
GitHub Copilot (using Claude Haiku 4.5)

### File List

**Implemented (DONE):**
- `backend/src/JobNecto.Application/Exceptions/NotFoundException.cs` ✅
- `backend/src/JobNecto.Application/Exceptions/ForbiddenException.cs` ✅
- `backend/src/JobNecto.Application/Exceptions/UnauthorizedException.cs` ✅
- `backend/src/JobNecto.Application/Exceptions/ConflictException.cs` ✅
- `backend/src/JobNecto.API/Infrastructure/ExceptionHandling/GlobalExceptionHandler.cs` ✅
- `backend/src/JobNecto.API/Infrastructure/AuthContext.cs` ✅ (NEW)
- `backend/src/JobNecto.API/appsettings.json` ✅ (MODIFIED — added JwtSettings with placeholder)
- `backend/src/JobNecto.API/appsettings.Development.json` ✅ (MODIFIED — added JwtSettings with dev secret)
- `backend/src/JobNecto.API/appsettings.Local.json` ✅ (MODIFIED — added JwtSettings with local secret)
- `backend/src/JobNecto.API/JobNecto.API.csproj` ✅ (MODIFIED — added JWT Bearer NuGet package)
- `backend/src/JobNecto.API/Program.cs` ✅ (MODIFIED — JWT auth registration + exception handler + proper middleware ordering)
- `backend/tests/JobNecto.Tests/API/ExceptionHandlingTests.cs` ✅
- `backend/tests/JobNecto.Tests/API/AuthenticationTests.cs` ✅ (NEW)

### Review Findings

_To be completed after code review. Document any issues found and their fixes._
