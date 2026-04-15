# Story 1.1: Global exception handling and problem details

Status: ready-for-dev

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an API Consumer (Frontend or Partner App),
I want all API responses for errors and invalid data to follow a consistent RFC 7807 Problem Details structure,
so that my application can gracefully handle and display errors to users without breaking due to inconsistent error shapes or leaking stack traces in production.

## Acceptance Criteria

1. [ ] A global exception handler is registered in the API pipeline to catch all unhandled exceptions.
2. [ ] Unhandled exceptions return an RFC 7807 Problem Details response (`application/problem+json`) with a `500 Internal Server Error` status.
3. [ ] Stack traces must **never** be included in the response body when the application is running in the Production environment.
4. [ ] Validation failures (FluentValidation) must return a `400 Bad Request` Problem Details response containing an `errors` dictionary of field names and their corresponding error messages.
5. [ ] The response types are consistent across all endpoints so that frontend clients can rely on `title`, `status`, and `errors`.
6. [ ] Tests should assert that invalid JSON or validation failures return the expected `application/problem+json` shape.

## Tasks

- [ ] Task 1: Setup Global Exception Handling (AC: 1, 2, 3)
  - [ ] Update `Program.cs` or add exception handling middleware to use `IExceptionHandler` / `AddProblemDetails` for ASP.NET Core 10.
  - [ ] Ensure Production environment strips stack traces from error messages.
  - [ ] Ensure exceptions are logged to Serilog.
- [ ] Task 2: Implement Validation Exception Mapping (AC: 4, 5)
  - [ ] Intercept or catch FluentValidation exceptions thrown by the Application layer.
  - [ ] Map FluentValidation `ValidationException` to `400 Bad Request` Problem Details.
  - [ ] Map validation errors to the `errors` extension field of the Problem Details object.
- [ ] Task 3: Testing (AC: 6)
  - [ ] Write integration or unit tests verifying a `500` response for unhandled exceptions (simulate a throw).
  - [ ] Write testing to verify a `400` validation error returns the proper dictionary format.

## Dev Notes

- **Clean Architecture Constraint:** The exception handler must reside in the API layer, but it will need to know about the exceptions specifically thrown by the Application layer (e.g., FluentValidation `ValidationException`).
- **ASP.NET Core 10:** Leverage the native `IExceptionHandler` interface and the built-in `AddProblemDetails()` infrastructure. There's no need to build a fully custom middleware if ASP.NET provides exactly what we need via `WebApplicationBuilder.Services.AddExceptionHandler<T>()`.
- **References:** Review `project-context.md` - ensure DI Registration doesn't accidentally interfere with `AddInfrastructure()` usage rules.

### Project Structure Notes

- Keep the new ExceptionHandlers directly inside the `JobNecto.API` project (perhaps under a new folder `JobNecto.API/Infrastructure/ExceptionHandling` or `JobNecto.API/Middlewares`).
- Ensure any tests are placed correctly in `JobNecto.Tests`.

### References

- [Source: GitHub Issue #36] - "Global exception handling and problem details"

## Dev Agent Record

### Agent Model Used



### Debug Log References

### Completion Notes List

### File List

