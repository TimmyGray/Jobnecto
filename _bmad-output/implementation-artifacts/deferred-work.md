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

## Deferred from: code review of 2-8-get-update-delete-education-records (2026-05-04)

- **EF global query filter bypass** — If `IgnoreQueryFilters()` is ever used or the filter is misconfigured, all three new handlers (`GetEducationQueryHandler`, `UpdateEducationCommandHandler`, `DeleteEducationCommandHandler`) could return soft-deleted records without an explicit `IsDeleted` guard. Deferred: pre-existing architectural assumption shared with Resume handlers.
- **`DateTime.UtcNow` hardcoded** — No clock abstraction in `UpdateEducationCommandHandler` and `DeleteEducationCommandHandler` makes time-sensitive unit tests fragile. Deferred: pre-existing pattern across all handlers in the project.
- **Non-atomic `UpdateAsync` + `SaveChangesAsync`** — A `SaveChangesAsync` failure leaves the EF change tracker in a dirty state; subsequent operations on the same scope could inadvertently persist partial changes. Deferred: pre-existing pattern across all handlers.
- **`DeleteEducationCommandValidator` has no unit tests** — Validator is exercised implicitly through the pipeline; route `:guid` constraint prevents `Guid.Empty` from reaching it. Deferred: low-risk, consistent with project convention for simple validators.
