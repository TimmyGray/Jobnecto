# Agent Learnings Log

Mistakes the agent made, analyzed, and corrected during sessions with the user.
Entries are appended here automatically when the self-improvement protocol triggers.

See `.github/instructions/self-improvement.instructions.md` for the protocol.

---

## Log

<!-- Entries are appended here. Newest at the top. -->

### 2026-04-28 - Avoid escaped quotes inside C# interpolation expressions

**Trigger:** Test failure
**Context:** Added cursor-pagination integration test in `backend/tests/JobNecto.Tests/API/ResumesControllerTests.cs`.
**Wrong action:** I inserted `ToString(\"o\")` inside a C# interpolated expression, producing invalid syntax and compile-time failures.
**Root cause:** I carried patch-string escaping into target C# code instead of writing the literal as `ToString("o")`.
**Correct behavior:** When patching source code, verify language-level string literals directly in the target file and avoid transport-layer escaping artifacts.
**Pattern / trigger:** Any patch that injects nested string literals inside interpolation or lambda expressions.
**Generalize?** Yes

### 2026-04-25 - Verify review findings against concrete code paths

**Trigger:** User correction
**Context:** Story 1.4 code review findings included overstated/duplicate issues before user requested a more accurate re-review.
**Wrong action:** I accepted subagent findings too quickly and persisted some claims (e.g., phone race returning 500, soft-delete phone lookup mismatch) without fully validating exception mapping/query-filter behavior in the actual code.
**Root cause:** Insufficient evidence-first verification on each finding before triage persistence; I optimized for speed over source-level confirmation.
**Correct behavior:** Re-check every finding against concrete implementation points (controller flow, validators, repository/query filters, global exception handler, and tests) before classifying severity or writing review artifacts.
**Pattern / trigger:** Multi-layer review outputs with overlapping findings, especially where framework behavior (EF query filters, global exception mapping) can invalidate assumptions.
**Generalize?** Yes

### 2026-04-25 - Keep generated test data validator-compliant

**Trigger:** Test failure
**Context:** Story 1.4 API tests in `UsersControllerTests` failed before reaching conflict assertions.
**Wrong action:** I generated dynamic `loginName` values containing hyphens and an update email that exceeded the max length constraint, causing `400 Bad Request` on setup/update steps.
**Root cause:** Test data helpers were not aligned with FluentValidation constraints (`loginName` regex and `email` max length).
**Correct behavior:** Normalize helper prefixes to allowed characters and generate bounded email values so setup data always satisfies validators unless the test explicitly targets validation errors.
**Pattern / trigger:** Integration tests that create prerequisite entities with dynamic identifiers before asserting downstream conflict/business behavior.
**Generalize?** Yes

### 2026-04-23 - Treat planning correction as source of truth immediately

**Trigger:** User correction
**Context:** Code review for Story 1.3 where artifacts required `/users/me` to return nested resumes/educations/cover letters.
**Wrong action:** I treated the existing story acceptance criteria as authoritative until after review findings, instead of immediately applying the user's clarified product direction.
**Root cause:** I over-weighted stale planning artifacts versus explicit user intent about endpoint boundaries and ownership model.
**Correct behavior:** When the user clarifies route boundaries and ownership scope, update epics/PRD/architecture/roadmap first, then re-triage review findings against the corrected artifacts.
**Pattern / trigger:** User states goals changed or documents misunderstood requirements.
**Generalize?** Yes

### 2026-04-23 - Over-expanded /me response and repository surface

**Trigger:** User correction
**Context:** Story 1.3 implementation for `GET /api/v1/users/me` and related repository contracts.
**Wrong action:** Added specialized repository interfaces (`IResumeRepository`, `IEducationRepository`, `ICoverLetterRepository`) and returned resumes/educations/cover letters from `/me`.
**Root cause:** I overfit to implementation convenience and prior story notes instead of preserving the existing generic repository pattern and endpoint boundary the user wanted.
**Correct behavior:** Keep `/me` limited to user profile data, move related resources to separate paged endpoints, and support user scoping through generic `GetAsync(PagedQuery, ct)`.
**Pattern / trigger:** Requests that explicitly say "use existing method", "do not create new interface", or "keep `/me` user-only".
**Generalize?** Yes

### 2026-04-23 - Secure cookie not auto-sent in HTTP test client

**Trigger:** Test failure
**Context:** Added token refresh cookie-transport coverage in `UsersControllerTests`.
**Wrong action:** Assumed the integration test client would automatically resend the auth cookie after registration.
**Root cause:** In test environment the auth cookie is marked `Secure`, so it is not automatically sent over the HTTP test channel.
**Correct behavior:** For cookie transport assertions in this environment, forward the auth cookie explicitly via the request `Cookie` header (or run the test channel over HTTPS).
**Pattern / trigger:** Integration tests that rely on secure cookies when the test host/client is using HTTP.
**Generalize?** No

### 2026-04-23 - Derived DbContext registration mismatch in tests

**Trigger:** Test failure
**Context:** Added `UsersControllerConcurrencyTests` with a custom test DbContext.
**Wrong action:** Registered a derived DbContext in a way that did not satisfy services requesting `AppDbContext` options.
**Root cause:** DI registration and constructor generic option types were inconsistent between base and derived DbContext usage.
**Correct behavior:** Register the derived context explicitly and map `AppDbContext` to that instance, with compatible DbContextOptions constructor signatures.
**Pattern / trigger:** Test environments overriding EF contexts while hosted services still resolve the base context type.
**Generalize?** No

### 2026-04-23 - Non-translatable method inside EF query

**Trigger:** Test failure
**Context:** Full test run after adding `LegacyPasswordBackfillHostedService`.
**Wrong action:** Used `passwordHasher.IsHashSupportedFormat(...)` directly inside EF query predicates.
**Root cause:** I applied application logic inside `IQueryable` where providers must translate expressions to query syntax.
**Correct behavior:** Materialize rows first, then apply non-translatable hash-format checks in memory.
**Pattern / trigger:** Any EF LINQ query that calls custom service methods.
**Generalize?** No

### 2026-04-23 - In-memory DB name changed per scope

**Trigger:** Test failure
**Context:** Added `LegacyPasswordBackfillHostedService` tests under `backend/tests/JobNecto.Tests/Infrastructure/Security`.
**Wrong action:** Registered EF InMemory with `Guid.NewGuid()` directly inside `AddDbContext`, which produced a different database name per scope.
**Root cause:** I placed randomness in the options lambda (executed on each context creation) instead of once per test service provider.
**Correct behavior:** Generate one database name per test provider and reuse it for all contexts in that provider.
**Pattern / trigger:** Any test setup that seeds data in one scope and verifies in another with EF InMemory.
**Generalize?** No
<!-- EOF -->
