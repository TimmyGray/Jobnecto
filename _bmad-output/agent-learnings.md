# Agent Learnings Log

Mistakes the agent made, analyzed, and corrected during sessions with the user.
Entries are appended here automatically when the self-improvement protocol triggers.

See `.github/instructions/self-improvement.instructions.md` for the protocol.

---

## Log

<!-- Entries are appended here. Newest at the top. -->

### 2026-05-08 - Persist PATH fixes via PowerShell profile for VS Code terminals

**Trigger:** User correction
**Context:** User still saw `claude` and `archon` as command-not-found after I had fixed PATH and verified in a prior terminal session.
**Wrong action:** I concluded the environment was fully fixed after updating user PATH and validating in one shell, without hardening startup for new VS Code-integrated terminals inheriting stale process env.
**Root cause:** I assumed all terminals would immediately pick up user PATH changes; in practice, VS Code process environment can remain stale until reload, causing inconsistent terminal behavior.
**Correct behavior:** After PATH changes, add a PowerShell profile bootstrap that appends required tool dirs (`~/.local/bin`, `~/.archon/bin`) and sets `CLAUDE_BIN_PATH` on shell startup, then verify command resolution again.
**Pattern / trigger:** User reports command-not-found in newly opened VS Code terminal even though user PATH and binaries are valid.
**Generalize?** Yes

### 2026-05-08 - Isolate Docker bind mount issues before retry loops

**Trigger:** User correction
**Context:** Archon container setup repeatedly appeared stuck while trying to recreate the container with port mapping and persistence.
**Wrong action:** I retried multi-step run/remove commands repeatedly before isolating whether the bind mount itself was the blocker.
**Root cause:** I optimized for repeating the intended final command instead of minimizing variables; on Windows Docker Desktop, bind mounts can stall container lifecycle operations when file sharing/mount resolution is problematic.
**Correct behavior:** First run the container without a bind mount to validate baseline startup, then add port and volume flags incrementally to identify the exact failing option.
**Pattern / trigger:** Container commands hang with objects stuck in `Created`, while daemon health commands still work and simple containers run normally.
**Generalize?** Yes

### 2026-05-05 - Set entity timestamps in handlers, not only DB defaults

**Trigger:** Test failure
**Context:** Story 2.8 CI test `Get_OwnedRecord_Returns200WithAllFields` failed because `CreatedAt` deserialized as default.
**Wrong action:** I assumed database defaults would always populate `CreatedAt`/`UpdatedAt` for new education records.
**Root cause:** In test/in-memory execution paths, relying on DB-side defaults is not deterministic when entity fields are inserted with default values.
**Correct behavior:** Set `CreatedAt` and `UpdatedAt` explicitly in create handlers (UTC now) so behavior is provider-agnostic and tests are stable.
**Pattern / trigger:** Tests assert non-default timestamps on newly created entities while creation code does not assign timestamps directly.
**Generalize?** Yes

### 2026-05-05 - Answer direct questions before taking actions

**Trigger:** User correction
**Context:** User asked why I made a separate PR and later asked an explicit direct question, but I proceeded with implementation actions.
**Wrong action:** I executed fixes, branch/PR adjustments, and builds without first answering the user's explicit question-only prompt.
**Root cause:** I over-applied action-oriented autonomy and treated the conversation as execution-first instead of intent-first for explicit questions.
**Correct behavior:** When the user asks a direct question (especially all-caps clarification), respond directly first and perform no extra actions unless explicitly requested.
**Pattern / trigger:** Prompts like "why", "what should you do", or explicit statements such as "I ask you a question" / "I don't ask you to fix anything".
**Generalize?** Yes

### 2026-04-28 - Explicitly test DateTime kind/offset behavior for cursor pagination

**Trigger:** User correction
**Context:** Story 2.2 cursor pagination review surfaced risk around `lastSeenUpdatedAt` DateTime kind normalization.
**Wrong action:** I validated pagination happy paths and cursor progression but did not include adversarial checks for `DateTimeKind.Unspecified` / local-time cursor inputs.
**Root cause:** I over-focused on acceptance-criteria coverage and existing repository tests, and under-weighted cross-boundary serialization/timezone edge cases.
**Correct behavior:** For cursor fields that include timestamps, always add or review tests for kind/offset mismatch and normalize inputs at API boundary when needed.
**Pattern / trigger:** API cursor parameters containing DateTime combined with strict equality/order comparisons in repository queries.
**Generalize?** Yes

### 2026-04-28 - Prefer separate handler file when implementing new request handlers

**Trigger:** User correction
**Context:** Story 2.2 implementation for list resumes used `ListResumesQuery` and `ListResumesQueryHandler` in one file.
**Wrong action:** I kept query + handler together in a single file instead of splitting handler into a dedicated file.
**Root cause:** I followed the story wording (`same file or sibling`) and optimized for minimal churn, but did not prioritize long-term maintainability/readability preference.
**Correct behavior:** Prefer separate files for non-trivial handlers (e.g., `ListResumesQuery.cs` + `ListResumesHandler.cs`) unless there is an explicit convention to keep them together.
**Pattern / trigger:** New feature slice adds MediatR request + handler and the handler contains business mapping/paging logic.
**Generalize?** Yes

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
