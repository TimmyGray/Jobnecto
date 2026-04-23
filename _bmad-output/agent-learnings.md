# Agent Learnings Log

Mistakes the agent made, analyzed, and corrected during sessions with the user.
Entries are appended here automatically when the self-improvement protocol triggers.

See `.github/instructions/self-improvement.instructions.md` for the protocol.

---

## Log

<!-- Entries are appended here. Newest at the top. -->

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
