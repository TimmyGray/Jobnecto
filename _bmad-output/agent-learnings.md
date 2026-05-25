# Agent Learnings Log

Mistakes the agent made, analyzed, and corrected during sessions with the user.
Entries are appended here automatically when the self-improvement protocol triggers.

See `.github/instructions/self-improvement.instructions.md` for the protocol.

---

## Log

<!-- Entries are appended here. Newest at the top. -->

### 2026-05-23 - Verify file existence before using Add File patches

**Trigger:** Test failure
**Context:** Implementing follow-up test fixes for R.1/R.3 in `backend/tests/JobNecto.Tests/Infrastructure/VacancyRepositoryTests.cs`.
**Wrong action:** I used an Add File patch for `VacancyRepositoryTests.cs` without confirming that the file already existed, which produced duplicate class/usings in the same file and broke compilation.
**Root cause:** I optimized for speed and skipped a quick existence check for the target file before choosing patch action type.
**Correct behavior:** Before creating any file with Add File, check whether a file with that path already exists; if it exists, use Update File and integrate changes into the existing structure.
**Pattern / trigger:** Any task that introduces "new tests" in a folder with existing dense test suites where similarly named files may already exist.
**Generalize?** Yes

### 2026-05-21 - Run a fast compile check immediately after creating new test files

**Trigger:** Test failure
**Context:** Story R.3 authorization integration suite implementation under `backend/tests/JobNecto.Tests/API/Authorization/`.
**Wrong action:** I created multiple new test files and ran full test discovery first, which failed due to two missing using directives (`JsonContent`, `CreateScope` extension import).
**Root cause:** I optimized for feature completeness before doing a quick compile/import sanity pass on new files.
**Correct behavior:** After bulk new-file creation, run a fast compile check immediately and fix missing imports before expensive test runs.
**Pattern / trigger:** Any change that adds multiple new C# test files in one burst.
**Generalize?** Yes

### 2026-05-11 - Sync EF snapshot whenever model config changes in migration-backed tests

**Trigger:** Test failure
**Context:** CI fixes for PostgreSQL-backed uniqueness/concurrency tests after enabling Postgres service in `.github/workflows/ci.yml`.
**Wrong action:** I fixed the EF runtime model (`VacancyConfiguration`) but initially did not synchronize the EF migration snapshot, which caused `PendingModelChangesWarning` during `Database.MigrateAsync()` in CI tests.
**Root cause:** I treated the issue as runtime mapping-only and overlooked that this repo's integration tests enforce migration snapshot parity when initializing PostgreSQL schemas.
**Correct behavior:** In this codebase, any EF model configuration change that affects mapped columns must keep migration metadata aligned (snapshot and/or migration) before considering CI fixed.
**Pattern / trigger:** Changes under `backend/src/JobNecto.Infrastructure/Persistance/Config/*.cs` combined with tests that call `Database.MigrateAsync()`.
**Generalize?** Yes

### 2026-05-10 - Verify xUnit runtime-skip APIs before coding dependency-gated tests

**Trigger:** Test failure
**Context:** Applying Epic 5 review fixes in `backend/tests/JobNecto.Tests/API/CoverLetters/CoverLettersUniquenessApiTests.cs`.
**Wrong action:** I implemented runtime skipping with `Xunit.Sdk.SkipException`, which is not available in the xUnit 2.9 package set used by this repo, causing compile failure.
**Root cause:** I assumed a skip exception API existed without checking the concrete xUnit version and exported types in this project.
**Correct behavior:** Before adding runtime skip logic, confirm the exact xUnit runtime capabilities in the repository and choose a compatible approach (or CI-gated fail-fast fallback) that compiles in the current package set.
**Pattern / trigger:** Any dependency-gated integration test where runtime skipping is introduced after a review recommendation.
**Generalize?** Yes

### 2026-05-10 - Enforce soft-delete predicates explicitly in specialized joined queries

**Trigger:** Test failure
**Context:** Story 5.5 post-delete API tests showed `GET /cover-letters/{id}` and list visibility regressions after soft delete.
**Wrong action:** I relied only on global query filters in specialized cover letter queries that also used JOIN/group-join with `IgnoreQueryFilters()` on related vacancy data.
**Root cause:** I assumed global filters would remain unambiguous in every composed query shape; in this path, deleted records still surfaced in tests.
**Correct behavior:** In specialized repository queries (especially with joins and query-filter overrides), add explicit base predicates for active records (`!IsDeleted`) on the primary entity to guarantee visibility rules.
**Pattern / trigger:** Any custom query that combines soft-deletable entities with `IgnoreQueryFilters()` on related entities.
**Generalize?** Yes

### 2026-05-10 - Verify enum members before writing new tests and DTO fixtures

**Trigger:** Test failure
**Context:** Story 5.3 unit/API tests for cover letter detail used `Location.ISTANBUL`.
**Wrong action:** I referenced an enum member that does not exist in `JobNecto.Domain.Enums.Location`, which caused compile failure.
**Root cause:** I assumed location granularity from endpoint expectations instead of checking the actual enum contract in the codebase.
**Correct behavior:** Before using enum literals in new tests or seeded fixtures, open the enum declaration and use only existing members; align expected JSON values with actual serializer output.
**Pattern / trigger:** Any new test or mapper that introduces enum constants from memory rather than from the source enum file.
**Generalize?** Yes

### 2026-05-10 - Do not reassign filtered IQueryable into implicitly ordered query vars

**Trigger:** Test failure
**Context:** Story 5.2 `CoverLetterRepository.GetPagedListAsync` cursor filter implementation.
**Wrong action:** I assigned an ordered LINQ query to `var query` (inferred as `IOrderedQueryable`) and then reassigned `query = query.Where(...)`, which returns `IQueryable` and caused compile error CS0266.
**Root cause:** I relied on implicit type inference for a mutable query pipeline and overlooked that adding `Where` changes the static interface from ordered to non-ordered.
**Correct behavior:** When a query variable is conditionally reassigned with filters, declare it as `IQueryable<T>` up front (or re-apply ordering after filtering) instead of relying on `var` from `OrderBy`.
**Pattern / trigger:** Any repository method that builds an ordered query then conditionally applies `Where` before paging.
**Generalize?** Yes

### 2026-05-10 - Keep EF Core types out of Application handlers

**Trigger:** Test failure
**Context:** Story 5.1 `CreateCoverLetterCommandHandler` initially caught `DbUpdateException` directly from `Microsoft.EntityFrameworkCore` in the Application layer.
**Wrong action:** I introduced an EF Core type dependency into Application to catch database uniqueness violations.
**Root cause:** I followed the story note too literally and missed the architecture boundary: Application must remain persistence-provider agnostic.
**Correct behavior:** In Application handlers, catch provider-agnostic exceptions and map conflicts without referencing EF packages; keep EF-specific details in Infrastructure/API exception mapping.
**Pattern / trigger:** Any new Application handler that tries to catch `DbUpdateException` or import `Microsoft.EntityFrameworkCore`.
**Generalize?** Yes

### 2026-05-10 - Return structured ProblemDetails for all 400 responses, never plain strings

**Trigger:** PR review comment (LLM reviewer, second pass)
**Context:** Story 4.1 `VacanciesController` returned `BadRequest("plain string")` for cursor validation. Existing `UsersController` already uses `BadRequest(new ProblemDetails { Status, Title, Detail })`.
**Wrong action:** I introduced a plain-string `BadRequest` without checking the project-wide error-shape convention first.
**Root cause:** I focused on the validation logic itself and did not cross-check the response format against existing controllers before writing the return statement.
**Correct behavior:** Before writing any `BadRequest` (or other error return) in a controller, check at least one existing controller for the established error-shape. In this project: `BadRequest(new ProblemDetails { Status = 400, Title = "Validation failed", Detail = "..." })`.
**Pattern / trigger:** Any new controller action that returns a 4xx response — check the project's existing error shape first.
**Generalize?** Yes

### 2026-05-10 - Validate all free-form string inputs that map to a fixed set of values

**Trigger:** PR review comment (LLM reviewer, second pass)
**Context:** Story 4.1 `SortBy` is a free-form string in `FilterVacanciesQuery`. The repository's `NormalizeSortBy` silently coerces unknown values to `createdAt`. Clients sending typos get 200 OK with the wrong ordering and no feedback.
**Wrong action:** I treated the silent-fallback in `NormalizeSortBy` as a design choice and did not add allowlist validation in the controller.
**Root cause:** I conflated "the code works" with "the contract is correct". The repository-level normalization is a robustness measure, not a substitute for input validation at the API boundary.
**Correct behavior:** Any string field that maps to a fixed set of values (sort modes, filter types, etc.) must be validated at the controller/handler boundary with an explicit allowlist and a 400 response for unknown values.
**Pattern / trigger:** Request DTO has a `string? SortBy` (or equivalent) field; check for allowlist validation before accepting.
**Generalize?** Yes

### 2026-05-10 - After every fix cycle, immediately update agent-learnings.md

**Trigger:** User correction (repeated, twice in same session)
**Context:** After both fix rounds in story 4.1, I failed to update agent-learnings until explicitly asked.
**Wrong action:** I treated the fix as complete once code and tests passed, skipping the mandatory self-improvement log update.
**Root cause:** I do not treat agent-learnings as part of the definition of done for a fix cycle. I optimize for the visible output (green tests, pushed commit) and treat documentation as optional.
**Correct behavior:** At the end of every session where I made a mistake that was caught by a reviewer or user, append an entry to `_bmad-output/agent-learnings.md` and commit it before closing the task — without being asked.
**Pattern / trigger:** Any session where a code review, PR comment, or user correction caused me to change code. Update learnings as the final step.
**Generalize?** Yes

### 2026-05-10 - Check JSON serialization config when reviewing request DTO field types

**Trigger:** PR review comment (Copilot reviewer)
**Context:** Story 4.1 `FilterVacanciesQuery` used typed enum arrays (`Location[]`, `WorkTimeType[]`, `WorkLocationType[]`, `Currency[]`) in the request body. Without `JsonStringEnumConverter` registered globally, clients must send integer values instead of string names — an unusable API contract.
**Wrong action:** I read the DTO fields, saw typed enum arrays, and accepted them at face value without checking whether the serializer was configured to handle string enum names.
**Root cause:** I did not trace the full client-to-handler path: HTTP body → JSON deserialization → DTO field type → serializer config. I reviewed the domain logic but skipped the deserialization layer.
**Correct behavior:** When reviewing or writing request DTOs with enum-typed fields, always check `Program.cs` / `AddControllers` options for `JsonStringEnumConverter`. If it is absent, either add it globally or use `string` fields with manual `Enum.TryParse` (consistent with the rest of the codebase).
**Pattern / trigger:** Request DTO contains `SomeEnum[]` or `SomeEnum?` fields in an ASP.NET Core project; check serializer config before accepting the design.
**Generalize?** Yes

### 2026-05-10 - Validate partial-cursor states, not just missing-cursor states

**Trigger:** PR review comment (Copilot reviewer and LLM reviewer)
**Context:** Story 4.1 cursor pagination required both `lastSeenId` and `lastSeenUpdatedAt`. Sending only one field silently skipped the cursor entirely and re-returned the first page — a potential infinite loop for clients.
**Wrong action:** I reviewed the cursor logic and correctly identified the sort-mode-change risk (finding #1), but I only considered "cursor present" vs "cursor absent". I never considered the degenerate case where exactly one cursor field is supplied.
**Root cause:** I stayed within the happy path and the one-field-missing-but-other-present case did not surface as a distinct state to test.
**Correct behavior:** For any pagination contract that requires a pair of cursor fields, explicitly validate the XOR state (exactly one provided) and return 400. Add tests for both asymmetric directions.
**Pattern / trigger:** Cursor pagination with two correlated fields (`lastSeenId` + `lastSeenTimestamp`); always add XOR validation and tests.
**Generalize?** Yes

### 2026-05-10 - Honor single-route API strategy across all planning artifacts

**Trigger:** User correction
**Context:** Epic 4 planning/story artifacts for vacancy browsing and filtering routes.
**Wrong action:** I created planning/story context with a separate simple browse route (`GET /api/v1/vacancies`) instead of using the single filter route strategy.
**Root cause:** I over-followed the previous epic wording and did not validate that the latest user route decision should replace the split-route structure before generating artifacts.
**Correct behavior:** When the user clarifies endpoint strategy, update all connected source-of-truth artifacts in one pass (epic story, generated story file, requirements mapping, and sprint plan) to the same contract.
**Pattern / trigger:** User explicitly states one endpoint should serve both browse and filtered behavior (empty criteria = browse mode).
**Generalize?** Yes

### 2026-05-10 - Align endpoint method with latest user contract

**Trigger:** User correction
**Context:** Story 3.4 planning artifacts for cover letter template update endpoint method.
**Wrong action:** I generated/kept `PUT /api/v1/cover-letter-templates/{id}` in story source artifacts.
**Root cause:** I over-relied on previously documented wording and did not treat the user's latest method preference as the immediate source of truth.
**Correct behavior:** When the user corrects an endpoint method, update all related source-of-truth artifacts in the same pass (epic story, requirements inventory, and generated implementation story file) to keep contracts consistent.
**Pattern / trigger:** User explicitly changes API method semantics (e.g., "change PUT to PATCH") after artifacts were generated.
**Generalize?** No

### 2026-05-09 - Run mandatory post-merge docs workflow immediately

**Trigger:** User correction
**Context:** PR #71 was merged to `master`, but I initially stopped after merge confirmation and did not execute the required documentation updates listed in `AGENTS.md`.
**Wrong action:** I treated "merge pr" as complete once the merge succeeded and skipped the mandatory post-merge documentation synchronization workflow.
**Root cause:** I optimized for the explicit merge action and failed to apply the repo-level lifecycle rule that merge completion is not the end state when post-merge workflow steps are mandatory.
**Correct behavior:** After any successful merge to `master`, immediately run the post-merge workflow: review and update roadmap, PRD, architecture shards (if needed), sprint status, README, and project context, then commit and push docs updates.
**Pattern / trigger:** User asks to merge a PR in a repository with explicit "Post-Merge / Post-Push Documentation Updates" instructions in `AGENTS.md`.
**Generalize?** No

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
