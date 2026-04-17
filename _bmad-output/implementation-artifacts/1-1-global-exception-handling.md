# Story 1.1: Global exception handling and problem details

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an API Consumer (Frontend or Partner App),
I want all API responses for errors and invalid data to follow a consistent RFC 7807 Problem Details structure,
so that my application can gracefully handle and display errors to users without breaking due to inconsistent error shapes or leaking stack traces in production.

## Acceptance Criteria

1. [x] A global exception handler is registered in the API pipeline using `IExceptionHandler` to catch all unhandled exceptions.
2. [x] Unhandled exceptions return an RFC 7807 Problem Details response (`application/problem+json`) with a `500 Internal Server Error` status.
3. [x] Stack traces must **never** be included in the response body when the application is running in the Production environment.
4. [x] Validation failures (FluentValidation) must return a `400 Bad Request` Problem Details response.
5. [x] Validation errors must be formatted as a dictionary of field names to arrays of error messages (`Dictionary<string, string[]>`) in the `errors` extension field.
6. [x] Custom domain exceptions in the Application layer (`NotFoundException`, `ForbiddenException`, `UnauthorizedException`) must be mapped to their corresponding HTTP status codes (404, 403, 401).
7. [x] Every error response must include a `traceId` extension field (mapped from `HttpContext.TraceIdentifier`) for log correlation.
8. [x] The response types are consistent across all endpoints so that frontend clients can rely on `title`, `status`, and `traceId`.
9. [x] Tests must assert that invalid JSON, validation failures, and domain exceptions return the expected `application/problem+json` shape.

## Tasks

- [x] Task 1: Define Application Exceptions (AC: 6)
  - [x] Create `Exceptions` folder in `JobNecto.Application`.
  - [x] Implement `NotFoundException`, `ForbiddenException`, and `UnauthorizedException` base classes.
- [x] Task 2: Setup Global Exception Handling (AC: 1, 2, 3, 7, 8)
  - [x] Implement `GlobalExceptionHandler` using `IExceptionHandler` in `JobNecto.API/Infrastructure/ExceptionHandling`.
  - [x] Register `AddExceptionHandler<GlobalExceptionHandler>()` and `AddProblemDetails()` in `Program.cs`.
  - [x] Ensure Production environment strips stack traces.
  - [x] Ensure exceptions and `traceId` are logged to Serilog.
- [x] Task 3: Implement Exception Mapping (AC: 4, 5, 6)
  - [x] Map `ValidationException` (FluentValidation) to `400 Bad Request` with structured errors.
  - [x] Map `NotFoundException` to `404 Not Found`.
  - [x] Map `ForbiddenException` to `403 Forbidden`.
  - [x] Map `UnauthorizedException` to `401 Unauthorized`.
- [x] Task 4: Integration Testing (AC: 9)
  - [x] Create `JobNecto.Tests.Integration` project if missing, or use existing `JobNecto.Tests`.
  - [x] Implement `ExceptionHandlingTests` using `WebApplicationFactory`.
  - [x] Verify 400 for validation, 404 for missing resources (mocked), and 500 for unhandled throws.

## Dev Notes

- **Clean Architecture Constraint:** The exception handler resides in the API layer, but maps exceptions from the Application layer.
- **ASP.NET Core 10:** Use `IExceptionHandler` and `AddProblemDetails()`.
- **References:** Review `project-context.md` for Serilog configuration.

### Project Structure Notes

- New folder: `JobNecto.API/Infrastructure/ExceptionHandling`
- New folder: `JobNecto.Application/Exceptions`

### References

- [Source: GitHub Issue #36] - "Global exception handling and problem details"

## Dev Agent Record

### Agent Model Used
Gemini 3.1 Pro (High)

### Debug Log References
- Extracted and tested exception mapping logic via `WebApplicationFactory`.
- Fixed `ContentType` manually in `GlobalExceptionHandler` to ensure `WriteAsJsonAsync` output maps correctly to RFC 7807 problem details specification.

### Completion Notes List
- Created `NotFoundException`, `ForbiddenException`, and `UnauthorizedException` in `JobNecto.Application.Exceptions`.
- Programmed `GlobalExceptionHandler` and registered it in `Program.cs`.
- Wrote integration tests covering 400, 401, 403, 404, and 500 error outputs matching `application/problem+json` shape.
- Validation format matches standard `{ errors: { Field: [] } }` format.
- `traceId` injected mapped from `HttpContext.TraceIdentifier`.

### File List
- `backend/src/JobNecto.Application/Exceptions/NotFoundException.cs`
- `backend/src/JobNecto.Application/Exceptions/ForbiddenException.cs`
- `backend/src/JobNecto.Application/Exceptions/UnauthorizedException.cs`
- `backend/src/JobNecto.API/Infrastructure/ExceptionHandling/GlobalExceptionHandler.cs`
- `backend/src/JobNecto.API/Program.cs`
- `backend/tests/JobNecto.Tests/API/ExceptionHandlingTests.cs`

### Review Findings

- [x] [Review][Patch] Potential NullReferenceException on problemDetails.Status if switch case fails [GlobalExceptionHandler.cs:245]
