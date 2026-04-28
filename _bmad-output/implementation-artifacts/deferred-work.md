# Deferred Work

## Deferred from: code review of 1-3-retrieve-current-user-profile (2026-04-23)

- **`DateTime` vs `DateTimeOffset`** — `CreatedAt`/`UpdatedAt` in `GetCurrentUserResult` omit timezone designator in JSON. Project-wide pattern, needs a cross-cutting decision on `DateTimeOffset` adoption.
- **`UserId` in `PagedQuery` (Domain layer)** — conflates row-ownership filtering with cursor pagination in a Domain value object. Accepted pragmatic decision for now; revisit when dedicated Application-layer query objects are introduced.

## Deferred from: code review of 1-4-update-user-profile (2026-04-25)

- **Cloudinary-not-configured handling policy** — Avatar endpoints currently fail with `InvalidOperationException` and return 500; decide whether this should remain fail-fast (500), become feature-unavailable (503), or become a business validation response.
- **Conflict response detail includes submitted identifiers** — Existing cross-endpoint behavior returns login/email/phone values in `ProblemDetails.detail`; treat as a broader API security/privacy policy decision.
- **Empty-string `phone` clears value** — Design intent remains null/empty = clear field for partial-update semantics. Revisit if spec tightens empty-vs-null distinction.
- **`loginName` min-length 3 vs AC wording** — Consistent with account-creation convention; update spec wording if minimum length is intended.
- **Phone unique-index rollout policy** — Story 1.4 migration adds unique non-null `Users.Phone` index without inline legacy-data cleanup; release rollout should require pre-deploy duplicate-phone detection/remediation before applying migration in production.

## Deferred from: code review of 2-2-list-resumes (2026-04-28)

- **W1 — Cursor pagination end-to-end test** — No integration test seeds N resumes, passes a cursor, and asserts the next page is correct (AC 4). Current coverage lives in `ResumeRepositoryTests`; deferred as pre-existing test strategy.
- **W2 — Soft-delete exclusion on `GET /api/v1/resumes`** — AC 7 is not asserted directly on this endpoint; relies on global EF filter coverage in `ResumeRepositoryTests`. Deferred as pre-existing test strategy.
- **W3 — `DateTime` kind on cursor** — `lastSeenUpdatedAt` is bound as `DateTime` (not `DateTimeOffset`); unspecified-kind values from clients could cause cursor mismatches on UTC-stored timestamps. Cross-cutting architectural concern shared with W1 from story 1.3 deferred work.
