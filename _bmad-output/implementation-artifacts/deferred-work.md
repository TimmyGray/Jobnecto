# Deferred Work

## Deferred from: code review of 1-3-retrieve-current-user-profile (2026-04-23)

- **`DateTime` vs `DateTimeOffset`** — `CreatedAt`/`UpdatedAt` in `GetCurrentUserResult` omit timezone designator in JSON. Project-wide pattern, needs a cross-cutting decision on `DateTimeOffset` adoption.
- **`UserId` in `PagedQuery` (Domain layer)** — conflates row-ownership filtering with cursor pagination in a Domain value object. Accepted pragmatic decision for now; revisit when dedicated Application-layer query objects are introduced.

## Deferred from: code review of 1-4-update-user-profile (2026-04-25)

- **Cloudinary-not-configured handling policy** — Avatar endpoints currently fail with `InvalidOperationException` and return 500; decide whether this should remain fail-fast (500), become feature-unavailable (503), or become a business validation response.

## Deferred from: code review of 2-4-update-resume (2026-04-28)

- **`ApplyUpdates` null argument guards are dead code** — `resume == null` and `command == null` checks at the top of `ResumeMappers.ApplyUpdates` are unreachable from the handler (`GetByIdAsync` throws `NotFoundException` rather than returning null). Harmless but could be cleaned up project-wide.
- **`RuleFor(x => x).Must(...)` produces empty-string `PropertyName`** — The "at least one field" validator rule uses `RuleFor(x => x)` which yields `PropertyName = ""` in `ValidationResult`. This is a common FV cross-field pattern and AC intent (structured 400 response) is met, but the error is not technically keyed to a named field.
- **Empty string `Title` accepted on update** — `UpdateResumeCommandValidator` has no `NotEmpty()` rule on `Title`; `title: ""` passes validation and is persisted. Consistent with `CreateResumeCommandValidator`. Consider adding `NotEmpty()` when `Title != null` in a future validator hardening pass.
- **Conflict response detail includes submitted identifiers** — Existing cross-endpoint behavior returns login/email/phone values in `ProblemDetails.detail`; treat as a broader API security/privacy policy decision.
- **Empty-string `phone` clears value** — Design intent remains null/empty = clear field for partial-update semantics. Revisit if spec tightens empty-vs-null distinction.
- **`loginName` min-length 3 vs AC wording** — Consistent with account-creation convention; update spec wording if minimum length is intended.
- **Phone unique-index rollout policy** — Story 1.4 migration adds unique non-null `Users.Phone` index without inline legacy-data cleanup; release rollout should require pre-deploy duplicate-phone detection/remediation before applying migration in production.

## Deferred from: code review of 2-3-get-resume-detail (2026-04-28)

- **AC 4 soft-delete integration test** — No integration test verifies `GET /api/v1/resumes/{id}` returns 404 for a soft-deleted resume; deferred to story 2.5 because no DELETE endpoint exists yet to set up the scenario.
- **Entity fetched before ownership check in handler** — `GetResumeQueryHandler` calls `GetByIdAsync` (loads full entity from DB) before the `UserId` ownership guard, so cross-user probes incur a full DB read that is then discarded. Fix requires ownership-aware `GetByIdAsync` overload or a combined ownership query on `ResumeRepository`; deferred as pre-existing repository pattern outside this story's scope.

## Deferred from: code review of 2-2-list-resumes (2026-04-28)

- **W1 — Cursor pagination end-to-end test** — No integration test seeds N resumes, passes a cursor, and asserts the next page is correct (AC 4). Current coverage lives in `ResumeRepositoryTests`; deferred as pre-existing test strategy.
- **W2 — Soft-delete exclusion on `GET /api/v1/resumes`** — AC 7 is not asserted directly on this endpoint; relies on global EF filter coverage in `ResumeRepositoryTests`. Deferred as pre-existing test strategy.
- **W3 — `DateTime` kind on cursor** — `lastSeenUpdatedAt` is bound as `DateTime` (not `DateTimeOffset`); unspecified-kind values from clients could cause cursor mismatches on UTC-stored timestamps. Cross-cutting architectural concern shared with W1 from story 1.3 deferred work.

## Deferred from: code review of 2-6-create-education-record (2026-05-01)

- **Idempotency for POST /api/v1/educations** — Implement idempotency key support (Idempotency-Key header) to deduplicate retries and concurrent submissions. Deferred: requires cross-cutting design (storage, cache/DB), acceptance of retention policy, and additional tests.

- **Concurrent FK race between validation and persist** — Race where a user may be deleted between validator check and `SaveChangesAsync`, producing FK violations. Deferred: infra/transaction isolation decision required (handle via DB constraint mapping or stronger transactional guarantees).

## Deferred from: code review of r-4-consistent-forbidden-vs-notfound-contract-matrix (2026-05-23)

- **Concurrent PATCH rename race on cover-letter-templates** — `UpdateCoverLetterTemplateCommandHandler` has no application-level pre-flight uniqueness check before `SaveChangesAsync`; concurrent renames to the same name both pass the handler and only one gets a DB-level 409. Pre-existing pattern; callers receive generic `"A unique constraint was violated."` rather than a domain-meaningful conflict message.
- **`GlobalExceptionHandler.IsUniqueConstraintViolation` string-matching fallback fragile** — If `InnerException` is not `PostgresException`, the handler uses substring matching on `"duplicate key"`/`"unique constraint"`/`"UNIQUE constraint failed"`. Safe for Postgres production but could misclassify unrelated `DbUpdateException` messages in non-Postgres test environments or future provider changes.

## Deferred from: code review of r-1-separate-soft-delete-repository-contract (2026-05-06)

- **CancellationToken unused in SoftDeleteAsync** — Both `SoftDeletableRepository<T>.SoftDeleteAsync` and `VacancyRepository.SoftDeleteAsync` accept `ct` but never use it. Pre-existing pattern: `EditableRepository.UpdateAsync` has identical behavior. Revisit when a clock/cancellation hardening pass is done across all repository methods.
- **`DateTime.UtcNow` hardcoded in SoftDeletableRepository/VacancyRepository** — No clock abstraction; same as pre-existing pattern across all handlers. Already logged from story 2-8.

## Deferred from: code review of 2-8-get-update-delete-education-records (2026-05-04)

- **EF global query filter bypass** — If `IgnoreQueryFilters()` is ever used or the filter is misconfigured, all three new handlers (`GetEducationQueryHandler`, `UpdateEducationCommandHandler`, `DeleteEducationCommandHandler`) could return soft-deleted records without an explicit `IsDeleted` guard. Deferred: pre-existing architectural assumption shared with Resume handlers.
- **`DateTime.UtcNow` hardcoded** — No clock abstraction in `UpdateEducationCommandHandler` and `DeleteEducationCommandHandler` makes time-sensitive unit tests fragile. Deferred: pre-existing pattern across all handlers in the project.
- **Non-atomic `UpdateAsync` + `SaveChangesAsync`** — A `SaveChangesAsync` failure leaves the EF change tracker in a dirty state; subsequent operations on the same scope could inadvertently persist partial changes. Deferred: pre-existing pattern across all handlers.
- **`DeleteEducationCommandValidator` has no unit tests** — Validator is exercised implicitly through the pipeline; route `:guid` constraint prevents `Guid.Empty` from reaching it. Deferred: low-risk, consistent with project convention for simple validators.

## Deferred from: code review of 3-3-get-cover-letter-template-detail (2026-05-09)

- **`CoverLetterTemplateResult` exposes `UserId` in response body** — `ToCoverLetterTemplateResult()` (Story 3.1) maps `template.UserId` into the DTO; the owner's internal ID is visible to the caller. Pre-existing design decision; revisit if admin/multi-tenant scenarios require restricting user-identity fields from API responses.

## Deferred from: code review of 3-1-create-cover-letter-template (2026-05-07)

- **Unit handler test missing `result.Id != Guid.Empty` assertion** — `CreateCoverLetterTemplateCommandHandlerTests` does not assert the returned Id is populated; integration test covers this via `NotBeEmpty` assertion in `CoverLetterTemplatesApiTests`.
- **C# clock vs DB clock for timestamps** — `CreateCoverLetterTemplateCommandHandler` sets `CreatedAt`/`UpdatedAt` via `DateTime.UtcNow` rather than letting EF use `HasDefaultValueSql`; by design for EF InMemory test compatibility (documented lesson 2026-05-05).
- **`TryInitializeSchemaAsync` not thread-safe** — No synchronization guard; concurrent fixture initialization could race if a second test class uses `CoverLetterTemplatesPostgresFactory`. Latent — currently only one fixture class.
- **`ConfigureWebHost`/`TryInitializeSchemaAsync` ordering fragility** — `_scopedConnectionString` is set by `TryInitializeSchemaAsync` which must be called before `CreateClient()`; relies on `WebApplicationFactory` lazy host construction. Safe with current test pattern.
- **Exception swallowing in `CoverLetterTemplatesPostgresFactory`** — `catch { return false; }` and `catch { }` discard exceptions silently; CI skips without logging the failure cause. Intentional skip behaviour for environments without Postgres.
- **DI bypass for repository in `UnitOfWork`** — `new CoverLetterTemplateRepository(_context)` bypasses the DI container; pre-existing pattern across all repositories in `UnitOfWork`.
- **Hard-coded `Location` URI string in controller** — `Created($"/api/v1/cover-letter-templates/{result.Id}", result)` would silently break on route changes; pre-existing pattern per `EducationsController`.
- **`GetCurrentUserId()` returns 401 vs 403 for malformed claim** — A valid but malformed JWT claim returns 401 rather than 403; matches existing pattern in `EducationsController`.

## Deferred from: code review of 1-1-project-foundation-user-registration (2026-05-27) — Demo MVP / Phase D

> Carried forward from the Story 1.1 file (2026-08-20) so these survive story archiving. Review verdict was **APPROVE-WITH-NITS** (no Critical/High); MED-1, MED-2 and NIT-1 were fixed pre-merge.
>
> ⚠️ **Each item below was re-verified against the code on 2026-08-20 before being recorded here.** The Story 1.1 file's "Code Review Follow-ups" section is **stale** — it still lists two items that were actually fixed in the story's final commit (`f9b2710`) but never struck from the list. Do not trust that section; trust this one.

**Verified still open:**

- **LOW-2 — post-success hydration failure reports a false error.** *(Confirmed at `pages/auth-sign-up/sign-up.page.ts:94-106`.)* The chain is `register()` → `switchMap(fetchCurrentUser())` → `.subscribe({ error: handleError })`. A `201` that creates the account and sets the cookie, followed by a failing `GET /users/me`, routes into `handleError` — so the user sees a sign-up **error banner** and is stranded on `/sign-up` despite being fully registered and authenticated. Fix: on hydration failure after a successful auth response, still navigate to `/dashboard` and degrade gracefully with a null profile. **The sign-in page has the identical `200` → hydrate shape — do not copy this bug into it.**
- **LOW-3 — client email regex is stricter than the server's.** *(Confirmed at `features/user/sign-up/sign-up.validators.ts:32`.)* `EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/` requires a dotted domain; FluentValidation's `.EmailAddress()` is more permissive (it accepts e.g. `user@localhost`). So the client rejects addresses the server would accept. The in-file comment claiming it "Mirrors FluentValidation's `.EmailAddress()`" is inaccurate. Either reconcile the regex or drop the mirror claim.
- **Token generator — three token surfaces are hand-synced.** *(Confirmed: `frontend/package.json` has only `gen:api`; no token generation script exists.)* `styles.scss` (CSS-var source of truth), `tokens.ts` (typed mirror), and `tailwind.config.js` are kept in sync manually. Build a real `tokens.ts → CSS vars / Tailwind` generation step so there is one true source. Drift here silently breaks UX-DR1 ("components reference tokens, never hardcoded values").

**Verified already fixed — recorded so they are not "rediscovered" from the stale story file:**

- **LOW-1 — `TextField` OnPush repaint. NOT A DEFECT.** The review flagged that `shared/ui/form/text-field.ts` is `OnPush` and binds plain (non-signal) `[value]`/`[disabled]`, so `writeValue`/`setDisabledState` would not repaint. **This was fixed in `f9b2710` (Story 1.1's final commit):** both `writeValue` (line 118) and `setDisabledState` (line 131) call `this.cdr.markForCheck()`, which correctly flags the OnPush component dirty for the next CD pass. `shared/ui/form/text-field.spec.ts` covers it and passes. `patchValue`/`reset`/`disable()` on edit forms are safe.
  - *Real residual gap (much smaller):* no **page-level** test asserts that `formGroup.patchValue(...)` on an already-rendered page updates the rendered `<input>.value` — existing page specs only assert component/service state. Worth adding with the first edit form (Story 2.3 résumé edit or 6.1 profile), not urgent.
- **NIT-2 — unused `SignUpInput` type.** Already removed; zero matches for `SignUpInput` across `frontend/src`.

## Deferred from: story-context analysis of 1-2-returning-user-sign-in-endpoint (2026-08-20)

> Surfaced while building the Story 1.2 context. Deliberately **not** folded into Story 1.2 (decision: Timmy, 2026-08-20) to keep that story scoped. Recorded here so it is not lost.

- **`JwtSettings` in base `appsettings.json` is fail-OPEN, unlike the repo's fail-closed CORS convention.** Base config ships `"SecretKey": "0123456789abcdef0123456789abcdef"` (a literal 32-char dev key, committed to the repo) and `"ExpirationMinutes": 36000` (**25 days**). `CookieAuthService` reuses `ExpirationMinutes` for the auth cookie's `Expires`, so sessions last ~25 days too.
  - **Why it matters:** `AuthenticationCollectionExtensions.EnsureValidJwtConfiguration` rejects a secret shorter than 32 chars — the committed dev key is *exactly* 32, so it passes the guard. Any environment deployed without an explicit `JwtSettings__SecretKey` override silently runs on a signing key that is public in the repo, and anyone who can read the repo can forge a valid token for any user. NFR8 assumes a deployed, HTTPS-served environment for the friends round, so this is reachable, not theoretical.
  - **There is no dev/prod layering at all here.** `appsettings.Development.json:18` sets `ExpirationMinutes` to the *same* `36000` — Development does not override base, so 25 days is simply the shipped value everywhere, not a dev convenience that leaked. (Separately, the gitignored, untracked `appsettings.Local.json` sets `360000` — 250 days — on this machine.)
  - **Contrast with the established convention:** `Cors:AllowedOrigins` is `[]` in base and populated only in `appsettings.Development.json` — deliberately fail-closed (this exact posture was restored as MED-1 during the Story 1.1 review). `JwtSettings` does the opposite.
  - **Scope note:** sign-in does not *introduce* this posture — it inherits it by calling the same `IJwtTokenService` / `CookieAuthService` path as registration and token-refresh. Story 1.2 makes it a third entry point, not a regression.
  - **Suggested fix:** remove `SecretKey` from base config so startup fails fast without an env/user-secrets override (matching the CORS posture and the existing fail-fast validation style); move the dev key to `appsettings.Development.json`; choose a deliberate base `ExpirationMinutes` (a 25-day session also makes Story 1.4's session-expiry recovery path nearly untestable in practice).
  - **Related:** no `UseForwardedHeaders()` / `X-Forwarded-For` handling exists anywhere in the backend. Behind a reverse proxy, `HttpContext.Connection.RemoteIpAddress` resolves to the proxy for every request — this silently collapses any IP-partitioned logic (including Story 1.2's sign-in attempt tracker) into a single bucket. Must be addressed before any proxied deployment.
