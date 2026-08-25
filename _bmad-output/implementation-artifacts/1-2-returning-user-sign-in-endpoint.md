# Story 1.2: Returning-user sign-in endpoint

Status: ready-for-dev

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a **returning user whose session has expired**,
I want **a credential sign-in endpoint**,
so that **I can establish a new session without re-registering**.

> **Track:** Backend-LLM (BE). **Contract-first** — Story 1.3 (sign-in screen, Frontend track) builds in parallel against the contract pinned below. **Do not change the wire contract without re-coordinating**; the FE story codes directly against it.
>
> **This is 100% net-new code.** There is no `AuthController`, no `SignInCommand`, no login endpoint, and no rate-limiting infrastructure anywhere in the repo. Nothing here is a rename or an extension of existing scaffolding. [Source: verified against `backend/src` 2026-08-20]

## Acceptance Criteria

**Endpoint shape**

1. `POST /api/v1/users/sessions` exists on `UsersController`, is `[AllowAnonymous]`, accepts `{ identifier, password }`, and is wired as `SignInCommand` + `SignInCommandValidator` + `SignInCommandHandler`. **No domain change, no EF migration, no new auth scheme.** [AR1] [Source: demo-mvp-architecture-decisions.md#Decision-2.2]

**Happy path**

2. Given a valid `identifier` (email **or** login name) and correct password for a non-soft-deleted user, the endpoint returns `200 OK`, sets the HTTP-only auth cookie via the **same** `IJwtTokenService.GenerateTokenAsync` → `ICookieAuthService.SetAuthCookie` sequence registration uses, and returns the user projection `{ id, loginName, email, phone, location, about, avatar }`. [AR1]
3. `accessToken` is populated in the response body **only** when the caller used `Authorization: Bearer` transport; it is an empty string for cookie transport — mirroring the existing token-refresh policy. [AR1]

**Anti-enumeration (security-critical)**

4. An unknown identifier **and** a wrong password produce a **byte-identical** `401` response (same status, title, and detail `"Invalid credentials"`). The response must never reveal whether the account exists. [AR2]
5. The unknown-identifier path performs an **equivalent-cost password verification against a dummy hash** so response timing does not disclose account existence. [AR2]
6. A soft-deleted user cannot authenticate and is indistinguishable from an unknown identifier (`401`, same body). [AR1]

**Validation**

7. Missing, empty, or whitespace-only `identifier` or `password` returns `400` (RFC 7807) with the `errors[field]` dictionary the existing `ValidationBehavior` → `GlobalExceptionHandler` pipeline produces. The validator enforces **non-empty only** — no length, format, or regex rules (those would leak "that isn't a valid login shape"). [AR2]

**Rate limiting**

8. After **5 failed attempts within 15 minutes for the same (normalized identifier + client IP)**, further attempts return `429` with a `Retry-After` header, before any credential verification runs. [AR2]
9. **Only failures count**, and a **successful sign-in resets** that key's window. [AR2]
10. Thresholds are config-driven: `RateLimit:SignIn:MaxAttempts` (5) and `RateLimit:SignIn:WindowMinutes` (15). [AR2]
11. The tracker key uses a **case-normalized** identifier, so `Bob` / `bob` / `BOB` share one bucket and the limit cannot be bypassed by varying case.

**OpenAPI**

12. The action carries full `[ProducesResponseType]` attributes for **200, 400, 401, and 429** — matching the R.2 review bar that every declared status is one the action can actually return, and no declared status is unreachable. [AR10]

## Tasks / Subtasks

- [ ] **Task 1 — Application layer: command, result, validator (AC: 1, 7)**
  - [ ] `backend/src/JobNecto.Application/Users/SignInCommand.cs` — `namespace JobNecto.Application.Users;` — `SignInCommand : IRequest<SignInResult>` with `Identifier` and `Password` (plain `string`, `= null!`, matching `CreateUserCommand` style). `SignInResult` in the same file with the **7 fields of `CreateUserResult`**: `Id (Guid), LoginName, Email, Phone?, Location?, About?, Avatar?`. **Do not** mirror `GetCurrentUserResult` — it carries `CreatedAt`/`UpdatedAt` which this contract excludes.
  - [ ] `backend/src/JobNecto.Application/Users/Validators/SignInCommandValidator.cs` — `namespace JobNecto.Application.Users.Validators;` — `AbstractValidator<SignInCommand>` with only `RuleFor(x => x.Identifier).NotEmpty()` and `RuleFor(x => x.Password).NotEmpty()`.
  - [ ] **No DI registration needed** — `AddApplication()` calls `AddMediatR(cfg => cfg.RegisterServicesFromAssembly(...))` and `AddValidatorsFromAssembly(...)` over this assembly. Both are picked up automatically. *(Verified: `ApplicationCollectionExtensions.cs:22-28`.)*
- [ ] **Task 2 — Credential-failure signal (AC: 4)**
  - [ ] `backend/src/JobNecto.Application/Exceptions/InvalidCredentialsException.cs` — follow the existing exception style in that folder (parameterless default message + custom-message ctor). This is the handler's failure signal only; it is caught in the controller and never reaches `GlobalExceptionHandler`.
  - [ ] ⚠️ **Do NOT add a case for it to `GlobalExceptionHandler`, and do NOT modify the existing `UnauthorizedException` case.** The 401 body is written directly by the controller (Task 5) using the established `new ProblemDetails { ... }` pattern. See Trap 1 in Dev Notes.
- [ ] **Task 3 — Sign-in handler (AC: 2, 4, 5, 6)**
  - [ ] `backend/src/JobNecto.Application/Users/SignInCommandHandler.cs` — inject `IUnitOfWork` and `IPasswordHasher` (mirror `CreateUserCommandHandler`'s ctor; inject `IUnitOfWork`, **not** `IUserRepository` directly).
  - [ ] Resolve: try `GetByEmailAsync(identifier.Trim().ToLowerInvariant())`; if null, try `GetByLoginAsync(identifier.Trim())`. ⚠️ **See Trap 2 — the casing differs between the two lookups and getting it wrong breaks sign-in for mixed-case logins.**
  - [ ] On user found: `_passwordHasher.VerifyHashedPassword(user.Password, request.Password)`; on false → `throw new InvalidCredentialsException()`.
  - [ ] On user **not** found: still call `VerifyHashedPassword` against a **constant dummy PBKDF2 hash**, discard the result, then throw the same exception. ⚠️ **See Trap 3.**
  - [ ] No explicit `IsDeleted` check — the EF global query filter on `User` already excludes soft-deleted rows from both repository lookups. *(Verified: `AppDbContext.ConfigureSoftDeleteFilters`.)*
- [ ] **Task 4 — Sign-in attempt tracker (AC: 8, 9, 10, 11)**
  - [ ] `ISignInAttemptTracker` in `backend/src/JobNecto.Application/Interfaces/` — `bool IsLockedOut(string identifier, string ip, out TimeSpan retryAfter)`, `void RecordFailure(string identifier, string ip)`, `void Reset(string identifier, string ip)`.
  - [ ] Implementation in `backend/src/JobNecto.Infrastructure/Services/` backed by `IMemoryCache`, reading `RateLimit:SignIn:MaxAttempts` / `:WindowMinutes` from `IConfiguration` with the 5 / 15 defaults. Normalize the identifier (`Trim().ToLowerInvariant()`) before composing the cache key (AC 11).
  - [ ] Register the tracker in `AddInfrastructure(...)`, and add `builder.Services.AddMemoryCache();` to `Program.cs` — **`AddMemoryCache` is not currently called anywhere**, so `IMemoryCache` is not resolvable until you add it.
  - [ ] Add the `RateLimit:SignIn` section to base `appsettings.json` (see Dev Notes for the exact block).
  - [ ] ⚠️ **Do NOT use `AddRateLimiter` / `UseRateLimiter` for this.** See Trap 4 — the built-in middleware structurally cannot express this rule.
- [ ] **Task 5 — Controller action (AC: 1, 2, 3, 8, 12)**
  - [ ] Add a `SignIn` action to `UsersController` alongside `Create`. The ctor already injects `IMediator`, `IJwtTokenService`, `ICookieAuthService` — add `ISignInAttemptTracker`.
  - [ ] Order of operations: resolve client IP → `IsLockedOut` check → **return 429 + `Retry-After` before sending the command** (never verify credentials while locked out) → `_mediator.Send` → on `InvalidCredentialsException`, `RecordFailure` then return the 401 directly (below) → on success, `Reset`, generate token, set cookie, return 200.
  - [ ] Write the 401 body inline, matching the established controller-level pattern (`UsersController.cs:229-234`, `CoverLettersController.cs:82-87`, `VacanciesController.cs:77-91` all use this shape for 400s):
    ```csharp
    return Unauthorized(new ProblemDetails
    {
        Status = StatusCodes.Status401Unauthorized,
        Title = "Unauthorized",
        Detail = "Invalid credentials"
    });
    ```
    Because this bypasses `GlobalExceptionHandler`, no `traceId` extension is attached — which is what makes the two failure responses **byte-identical** (AC 4). Emit the 429 body the same way.
  - [ ] Reuse the existing private `UsesBearerTransport(Request)` helper already in `UsersController` for the `accessToken` field (AC 3) — do not write a second one.
  - [ ] `backend/src/JobNecto.API/Contracts/Auth/SignInResponse.cs` — the 7 user fields + `AccessToken`. This mirrors where `RefreshAccessTokenResult` already lives, and keeps the transport-only `accessToken` concern out of the Application DTO.
  - [ ] `[ProducesResponseType]` for 200 / 400 / 401 / 429 (AC 12).
- [ ] **Task 6 — Tests (all ACs)** — see Testing Requirements in Dev Notes for the full required matrix.
- [ ] **Task 7 — Verify green**
  - [ ] `dotnet test backend/JobNecto.slnx --configuration Release --warnaserror`
  - [ ] Coverage gate: every new file ≥80% line coverage (see Dev Notes).

## Dev Notes

### ⚠️ Four traps — each will silently produce a wrong implementation

**Trap 1 — `UnauthorizedException` cannot produce the required 401 body.**
`GlobalExceptionHandler.cs:77-81` binds `unauthorizedException` and then **discards `.Message`**, hardcoding:
```csharp
case UnauthorizedException unauthorizedException:
    problemDetails.Status = StatusCodes.Status401Unauthorized;
    problemDetails.Title = "Unauthorized";
    problemDetails.Detail = "Authentication is required to access this resource.";
    break;
```
So `throw new UnauthorizedException("Invalid credentials")` emits *"Authentication is required to access this resource."* and AC 4 fails.

**The fix is to not route this through `GlobalExceptionHandler` at all.** The controller already has to catch the credential failure in order to call `RecordFailure` on the attempt tracker, so it should write the 401 itself using the house `new ProblemDetails { ... }` pattern (Task 5). That touches no shared file, matches four existing call sites, and yields byte-identical failure responses for free.

Two things not to do:
- Do **not** "fix" the existing `UnauthorizedException` case to use `.Message` — `API/ExceptionHandlingTests.cs` asserts the current wording and every other endpoint depends on it.
- Do **not** add a new case to `GlobalExceptionHandler` for this. It would work, but it edits a shared file for no gain and reintroduces the auto-attached `traceId`, which weakens the byte-identical guarantee in AC 4.

**Trap 2 — login names are case-sensitive; emails are not.**
`CreateUserCommandHandler` stores `Email = Email.Trim().ToLowerInvariant()` but `LoginName = LoginName.Trim()` (**not** lowercased). Both repository methods are plain equality matches (`u.Email == email` / `u.Login == login`). Therefore:
- Email lookup → pass `identifier.Trim().ToLowerInvariant()`
- Login lookup → pass `identifier.Trim()` **unmodified in case**

Normalizing once at the top of the handler — the obvious refactor — breaks sign-in for every user whose login has an uppercase character.

**Trap 3 — the not-found short-circuit is a timing oracle.**
`Pbkdf2PasswordHasher` runs **100,000 PBKDF2-SHA256 iterations** and uses `CryptographicOperations.FixedTimeEquals` for the final compare — so the *comparison* is constant-time, but only when it runs. The naive shape:
```csharp
var user = await ...; if (user is null) throw ...;   // returns in ~microseconds
if (!hasher.VerifyHashedPassword(...)) throw ...;     // burns ~100k iterations
```
makes account existence trivially measurable by response time. Verify against a **constant dummy hash** on the not-found path before throwing. No such pattern exists in the repo yet — you are introducing it.

**Trap 4 — ASP.NET Core's rate limiter cannot express this rule.** *(Decision made 2026-08-20: use an application-level tracker.)*
Two independent structural blockers:
1. The partition-key callback receives `HttpContext` **before model binding**, so `identifier` (a JSON body field) is not available without `EnableBuffering()` + a manual body read + rewind inside a synchronous partition factory.
2. Built-in limiters decrement a permit on **acquisition**, not on outcome. There is no "count only failures" and no "reset on success" semantic.

Note this also means Decision 2.3's claim that sign-in reuses *"the same limiter family as generation"* is **incorrect**. Generation (Decision 3: 10/user/hour, user id from claims, counts every request) maps cleanly onto the middleware; sign-in does not. Build the tracker as specified in Task 4.

### Reuse — do not reinvent

| Need | Use this | Location |
|---|---|---|
| Verify a password | `IPasswordHasher.VerifyHashedPassword(hashed, provided) → bool` | `Application/Interfaces/IPasswordHasher.cs:21` |
| Find user by email / login | `IUserRepository.GetByEmailAsync` / `GetByLoginAsync` → `Task<User?>` | `Application/Interfaces/IUserRepository.cs:17,23` |
| Issue a JWT | `IJwtTokenService.GenerateTokenAsync(userId)` | `Application/Interfaces/IJwtTokenService.cs` |
| Set the auth cookie | `ICookieAuthService.SetAuthCookie(Response, token)` | `API/Infrastructure/CookieAuthService.cs` |
| Detect bearer vs cookie transport | private `UsesBearerTransport(Request)` | already in `UsersController` |
| Exclude soft-deleted users | EF global query filter on `User` — automatic | `Infrastructure/Persistance/AppDbContext.cs` |
| Repository access from a handler | `IUnitOfWork.UserRepository` | mirror `CreateUserCommandHandler` |

This story is the **first production caller** of `VerifyHashedPassword`, and the first caller of `GetByEmailAsync`/`GetByLoginAsync` outside registration's uniqueness checks.

### Wire contract (pinned — Story 1.3 codes against this)

```
POST /api/v1/users/sessions          [AllowAnonymous]
Request:  { "identifier": "daria@example.com" | "daria_dev", "password": "..." }

200 OK    Set-Cookie: auth-token=<jwt>; HttpOnly; SameSite=Strict; Secure(non-dev)
          { id, loginName, email, phone, location, about, avatar, accessToken }
          // accessToken populated ONLY for Authorization: Bearer callers; "" for cookie transport

400       RFC 7807 + errors{field: [msg]}   — missing/empty identifier or password
401       RFC 7807, detail "Invalid credentials"  — unknown identifier OR wrong password OR soft-deleted
429       RFC 7807 + Retry-After header    — 5 failures / 15 min / (normalized identifier + IP)
```

Config block for base `appsettings.json` (flat PascalCase, matching the existing `JwtSettings` / `Cors` convention):
```json
"RateLimit": {
  "SignIn": {
    "MaxAttempts": 5,
    "WindowMinutes": 15
  }
}
```

### Testing requirements

Run: `dotnet test backend/JobNecto.slnx --configuration Release --warnaserror` (**never** the root `Jobnecto.sln` — it uses Windows path style and omits the test project).

**Handler unit tests** — `backend/tests/JobNecto.Tests/Application/Users/SignInCommandHandlerTests.cs`. Mirror `CreateUserCommandHandlerTests`: constructor-built `Mock<IUnitOfWork>` + `Mock<IUserRepository>` + `Mock<IPasswordHasher>`, wired via `_uowMock.Setup(x => x.UserRepository).Returns(_userRepoMock.Object)`. Required cases:
- resolves by email (lowercased) → success
- resolves by login when email lookup misses → success
- **mixed-case login resolves** (regression guard for Trap 2)
- unknown identifier → `InvalidCredentialsException`
- wrong password → `InvalidCredentialsException`
- **`VerifyHashedPassword` is invoked even when the user is not found** — `Times.Once` (regression guard for Trap 3)

**Validator unit tests** — mirror `CreateUserCommandValidatorTests`: direct `_validator.Validate(cmd)`, assert `IsValid` and `Errors.Should().Contain(e => e.PropertyName == "...")`. `[Theory]`/`[InlineData]` over null / `""` / `"   "` for both fields.

**Tracker unit tests** — threshold boundary (5th fails, 6th → locked), window expiry, reset-on-success, and **case-normalized key** (`Bob` and `bob` share a bucket).

**Integration tests** — `backend/tests/JobNecto.Tests/API/SessionsApiTests.cs`. Use `await using var factory = new JobNectoApiFactory();` (fresh guid-named InMemory DB per test — no shared fixture, no cleanup) and `factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false })` whenever asserting on `Set-Cookie`. Register a throwaway user first, exactly as `UsersControllerTests` / `CoverLetterTemplatesApiTests` do. Required cases:
- valid credentials → 200, cookie present with `httponly` + `samesite=strict`, body carries the 7 user fields
- bearer transport → `accessToken` non-empty; cookie transport → `accessToken` empty
- **unknown identifier and wrong password produce identical response bodies** — assert the raw response strings are equal, no normalization needed (the controller-written ProblemDetails carries no `traceId`). This is the AC 4 regression guard and the single most important test in the story
- soft-deleted user → 401, same body
- empty identifier / empty password → 400 with `errors`
- 5 failures then a 6th → 429 with a `Retry-After` header
- 4 failures → success → failures again: counter was reset (AC 9)

Do **not** reach for the Postgres factory (`CoverLetterTemplatesPostgresFactory`) — it exists only for real unique-constraint races, which sign-in does not have.

**Coverage gate (CI-enforced, will fail the build):** `scripts/check_coverage.py --threshold 80` enforces **≥80% line coverage per file *and* overall**. `coverlet.runsettings` sets `SkipAutoProps=true`, so DTO/record auto-properties are free — but the handler, validator, tracker, and controller action all need real coverage. **Do not** reach for `[ExcludeFromCodeCoverage]`: the only sanctioned use in this repo is `CloudinaryAvatarStorageService.UploadUserAvatarAsync`, justified by a live external HTTP dependency. Nothing in this story has one.

### Project Structure Notes

- **Namespaces mirror folders**, rooted at the project namespace — mandatory. `backend/src/JobNecto.Application/Users/Validators/SignInCommandValidator.cs` → `namespace JobNecto.Application.Users.Validators;`
- Layering: API → Application → Domain; Infrastructure implements. The handler stays persistence-ignorant (repository via `IUnitOfWork`); the **client IP is a transport concern and must not enter `SignInCommand`** — the pinned command shape is `{ Identifier, Password }` only, which is why the rate-limit check lives in the controller.
- `Program.cs` slot-ins: `builder.Services.AddMemoryCache();` beside the other cross-cutting registrations (near lines 17-21). No `UseRateLimiter()` middleware is being added, so pipeline order is unchanged.
- Keep changes scoped — `GlobalExceptionHandler` should not be touched by this story at all.

### Known-adjacent issues (do not fix here, do not make worse)

- `ConflictException` passes `.Message` straight into `ProblemDetails.detail`, leaking submitted identifiers on registration — an open policy question in `deferred-work.md`. **Sign-in must not follow that precedent**; its 401 detail is a fixed generic string.
- Base `appsettings.json` ships a committed JWT signing key and a 25-day token/cookie lifetime — logged in `deferred-work.md` (2026-08-20), deliberately out of scope here.
- No `UseForwardedHeaders()` exists. Behind a reverse proxy, `Connection.RemoteIpAddress` is the proxy for every request, collapsing this story's IP partition into one bucket. Fine for local/dev; logged in `deferred-work.md` as a pre-deployment blocker.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md#Story-1.2`] — story statement and base acceptance criteria
- [Source: `_bmad-output/planning-artifacts/architecture/demo-mvp-architecture-decisions.md#Decision-2`] — AR1/AR2, pinned endpoint contract, resolution order, rate-limit thresholds
- [Source: `_bmad-output/planning-artifacts/prd-demo-mvp.md`] — FR4, FR6, NFR4
- [Source: `backend/src/JobNecto.API/Controllers/UsersController.cs`] — registration + token-refresh actions, `UsesBearerTransport`
- [Source: `backend/src/JobNecto.API/Infrastructure/ExceptionHandling/GlobalExceptionHandler.cs:77-81`] — the discarded-`.Message` 401 mapping (Trap 1)
- [Source: `backend/src/JobNecto.API/Controllers/UsersController.cs:229-234`, `CoverLettersController.cs:82-87`, `VacanciesController.cs:77-91`] — established controller-level `new ProblemDetails { ... }` pattern reused for the 401/429 bodies
- [Source: `backend/src/JobNecto.Application/Users/CreateUserCommandHandler.cs`] — normalization asymmetry (Trap 2), handler/ctor pattern
- [Source: `backend/src/JobNecto.Infrastructure/Services/Pbkdf2PasswordHasher.cs`] — 100k iterations, `FixedTimeEquals` (Trap 3)
- [Source: `backend/src/JobNecto.Application/ApplicationCollectionExtensions.cs:22-28`] — MediatR + validator auto-registration
- [Source: `backend/tests/JobNecto.Tests/API/JobNectoApiFactory.cs`] — integration test factory pattern
- [Source: `scripts/check_coverage.py`, `backend/coverlet.runsettings`] — coverage gate
- [Source: `_bmad-output/implementation-artifacts/deferred-work.md`] — adjacent known issues

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List
