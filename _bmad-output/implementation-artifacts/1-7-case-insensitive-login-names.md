# Story 1.7: Case-insensitive login names

Status: ready-for-dev

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As **any user of Jobnecto**,
I want **my login name to work regardless of capitalization, and to be the only account with that name**,
so that **I can sign in without remembering exact casing and nobody can register a look-alike of my name**.

> **Origin:** adversarial code review of Story 1.2 (2026-08-26). The review found a cross-account lockout DoS: because `Bob` and `bob` can be two distinct accounts today, five failed attempts against one locks out the other. Story 1.2 shipped an **interim workaround** (case-normalize the rate-limit bucket only for email-style identifiers). **This story fixes the root cause and reverts that workaround.**
>
> **This changes registration semantics.** After this story, registering `bob` while `Bob` exists returns `409 Conflict`. That is the intended behavior change, not a regression.
>
> ⚠️ **Blocked on two prerequisites — do not start until both are cleared:**
> 1. **Product sign-off** on the semantic change (see Decision Record below).
> 2. **The data audit in Task 0 passes.** Any existing case-colliding logins will make the migration fail on a live database.

## Decision Record

**Decision: login names become case-insensitive for authentication and uniqueness, while preserving the user's chosen casing for display.**

Rationale, in priority order:

1. **Impersonation surface (primary).** If `Bob` and `bob` are different people, the look-alike is a phishing and social-engineering vector. Case-sensitivity buys a larger namespace and nothing else.
2. **Internal incoherence.** `POST /api/v1/users/sessions` accepts a single `identifier` field that may be an email **or** a login. Email is already lowercased at write time; login is not. The same input string therefore behaves differently depending on which branch matches it.
3. **Usability.** Mobile keyboards auto-capitalize the first character; users do not recall the casing they chose at registration.
4. **It removes a whole defect class** rather than patching each symptom — the Story 1.2 lockout collision being the first symptom found.

Prior art: OWASP's Authentication Cheat Sheet historically stated *"Make sure your usernames/userids are case insensitive. User 'smith' and user 'Smith' should be the same user."* ⚠️ **Cite this accurately:** that line is present in [an older revision](https://github.com/OWASP/CheatSheetSeries/blob/7d94e9a29174b8fd76235ca60f47245d1f34df1e/cheatsheets/Authentication_Cheat_Sheet.md) but has since been **removed from the current cheat sheet** — it appears dropped rather than reversed. Do not present it as current OWASP guidance.

**Chosen mechanism: a non-deterministic ICU collation on the `Login` column** (not `citext`, not lowercase-on-write).

| | Lowercase on write | **Non-deterministic ICU collation (chosen)** | `citext` |
|---|---|---|---|
| Display casing | Lost | **Preserved** | Preserved |
| Enforcement point | App code — forgettable | **DB index — cannot be bypassed** | DB type |
| `LIKE` on the column | Works | **Unusable** (acceptable: nothing queries `Login` with `LIKE`) | Works |
| Guidance | — | **Recommended by PostgreSQL over `citext`** | Legacy |

## Acceptance Criteria

**Uniqueness**

1. Registering a login name that differs from an existing non-soft-deleted user's login **only by case** returns `409 Conflict` with the existing conflict body, exactly as an exact-match duplicate does today. [Source: `CreateUserCommandHandler.cs:41-44`]
2. The unique index `IX_Users_Login` enforces this **at the database level**, so the invariant holds even if an application-layer check is bypassed or a future code path forgets it.
3. A soft-deleted user's login does **not** block registration of a case-variant — the existing partial-index filter `"IsDeleted" = FALSE` continues to apply unchanged.

**Authentication**

4. A user registered as `TimmyGray` can sign in via `POST /api/v1/users/sessions` with `identifier` of `timmygray`, `TIMMYGRAY`, or `TimmyGray` — all resolve to the same account.
5. Sign-in behavior for **email** identifiers is unchanged (already case-insensitive via lowercase-on-write).
6. The anti-enumeration guarantees of Story 1.2 are preserved: unknown identifier and wrong password still produce a **byte-identical** `401`, and the unknown path still performs the equivalent-cost dummy-hash verification.

**Display**

7. The casing the user typed at registration is **preserved and returned** by every endpoint that projects `loginName` (`POST /api/v1/users`, `POST /api/v1/users/sessions`, `GET /api/v1/users/me`). Registering `TimmyGray` and reading the profile back returns `TimmyGray`, never `timmygray`.

**Reverting the Story 1.2 workaround**

8. `SignInAttemptTracker.ComposeKey` reverts to **unconditional** `Trim().ToLowerInvariant()` normalization for all identifiers — the `Contains('@')` special-case added as an interim fix is removed, along with its two tests.
9. Story 1.2's **AC11 is restored to its original wording** (`Bob` / `bob` / `BOB` share one lockout bucket) and its "Deliberate AC deviation" note in the Remediation Notes is resolved and struck, because that behavior is now correct: those identifiers denote one account.
10. Story 1.2's **Trap 2** ("login names are case-sensitive; emails are not") is marked obsolete in that story's Dev Notes, and `SignInCommandHandler`'s two-lookup casing asymmetry is simplified — both lookups may now pass the same trimmed value.

## Tasks / Subtasks

- [ ] **Task 0 — Data audit (BLOCKING; do this first)**
  - [ ] Run against every environment that has real data (dev, staging, prod if it exists):
    ```sql
    SELECT lower("Login") AS collision, count(*), array_agg("Login")
    FROM "Users"
    WHERE "IsDeleted" = FALSE
    GROUP BY lower("Login")
    HAVING count(*) > 1;
    ```
  - [ ] **If this returns any rows, STOP and escalate.** The migration in Task 2 will fail on index creation. Resolving collisions is a product/ops decision (rename the newer account, or soft-delete it) and is explicitly **out of scope** for this story — do not pick a winner unilaterally.
  - [ ] Record the result (including "zero rows") in the Dev Agent Record.
- [ ] **Task 1 — Collation + entity configuration (AC: 1, 2, 3, 7)**
  - [ ] In `AppDbContext.OnModelCreating` (or the existing model-level configuration point), declare the collation:
    ```csharp
    modelBuilder.HasCollation("case_insensitive", locale: "en-u-ks-primary", provider: "icu", deterministic: false);
    ```
  - [ ] In `backend/src/JobNecto.Infrastructure/Persistance/Config/UserConfiguration.cs:31`, apply it to the column:
    ```csharp
    builder.Property(u => u.Login).IsRequired().HasMaxLength(50).UseCollation("case_insensitive");
    ```
  - [ ] Leave `UserConfiguration.cs:33` (`HasIndex(u => u.Login).IsUnique().HasFilter(...)`) **unchanged** — the index inherits the column's collation and becomes case-insensitive automatically. That is what satisfies AC 2.
  - [ ] Do **not** change the `Email` column. It is already normalized at write time and changing it is out of scope.
- [ ] **Task 2 — Migration (AC: 1, 2)**
  - [ ] `dotnet ef migrations add MakeLoginCaseInsensitive --project backend/src/JobNecto.Infrastructure --startup-project backend/src/JobNecto.API`
  - [ ] Inspect the generated migration: it must `CREATE COLLATION`, `ALTER TABLE "Users" ALTER COLUMN "Login" TYPE character varying(50) COLLATE "case_insensitive"`, and **recreate** `IX_Users_Login`. Postgres cannot alter a column's collation while an index depends on it, so verify the drop/recreate ordering is present; hand-edit the migration if EF's generated order is wrong.
  - [ ] Verify `Down()` reverses cleanly (back to the default collation + original index).
- [ ] **Task 3 — Application layer simplification (AC: 4, 5, 6, 10)**
  - [ ] `CreateUserCommandHandler.cs:32` — keep `Trim()`, still do **not** lowercase. Casing is preserved on purpose (AC 7); the DB now handles comparison.
  - [ ] `SignInCommandHandler` — the email/login lookup casing asymmetry is no longer needed. Both lookups may pass the same trimmed identifier. **Keep the email lookup lowercased** anyway (stored values are lowercase, so this is still the correct match) — the change here is that the login lookup no longer needs case preserved for correctness.
  - [ ] ⚠️ Do **not** touch the dummy-hash / equivalent-cost path or the inline 401 body. AC 6 depends on both being byte-for-byte as Story 1.2 left them.
- [ ] **Task 4 — Revert the Story 1.2 interim workaround (AC: 8, 9, 10)**
  - [ ] `backend/src/JobNecto.Infrastructure/Services/SignInAttemptTracker.cs` — `ComposeKey` returns to unconditional normalization:
    ```csharp
    var normalizedIdentifier = identifier?.Trim().ToLowerInvariant() ?? string.Empty;
    ```
    Update the XML doc comment, which currently explains the email-vs-login split.
  - [ ] Delete `Buckets_EmailStyleIdentifiers_AreCaseNormalized` and `Buckets_LoginStyleIdentifiers_AreCaseSensitiveAndIndependent`; restore a single `Buckets_AreCaseNormalized` asserting `Bob`/`bob`/`BOB` share one bucket.
  - [ ] Edit `_bmad-output/archive/implementation-artifacts/1-2-returning-user-sign-in-endpoint.md`: restore AC11's original wording, strike the "Deliberate AC deviation" paragraph as resolved (reference this story), and mark Trap 2 obsolete.
- [ ] **Task 5 — Tests (AC: all)** — see Testing Requirements. ⚠️ **Read Trap 1 first: the default in-memory test setup cannot verify any of this.**
- [ ] **Task 6 — Verify green**
  - [ ] `dotnet test backend/JobNecto.slnx` — full suite green.
  - [ ] `dotnet test backend/JobNecto.slnx --configuration Release --warnaserror` for CI parity.
  - [ ] Per-file ≥80% coverage gate: `python scripts/check_coverage.py ./coverage/backend --threshold 80`.
  - [ ] Confirm the frontend suite is untouched by this story (no frontend changes expected).

## Dev Notes

### ⚠️ Trap 1 — the in-memory provider silently cannot test this

`JobNectoApiFactory.cs:44` and every repository test use `UseInMemoryDatabase`. **The EF Core in-memory provider does not implement PostgreSQL collations.** String comparison there falls back to .NET ordinal semantics, which is case-**sensitive**.

The consequence is the worst kind: a test asserting `bob` finds `Bob` will **fail on in-memory while the production code is correct**, and — far more dangerous — a test asserting the *uniqueness* rule can **pass in-memory for the wrong reason** and give false confidence.

**Every AC in this story that depends on collation behavior (AC 1–5, 7) must be tested against real PostgreSQL.** The repo already has this pattern — follow it rather than inventing a new one:

- `backend/tests/JobNecto.Tests/API/UsersControllerConcurrencyTests.cs:140-189` — the closest model. Per-test isolated schema (`SearchPath = "users_concurrency_" + Guid`), `Database.MigrateAsync()`, connection from the `JOBNECTO_TEST_POSTGRES` env var with a default fallback, and a `TryInitializeSchemaAsync` that returns `false` so the suite **skips rather than fails** when Postgres is unavailable.
- `backend/tests/JobNecto.Tests/API/CoverLetters/CoverLettersUniquenessApiTests.cs:180` and `.../CoverLetterTemplates/CoverLetterTemplatesUniquenessApiTests.cs:274` — same pattern applied specifically to **DB-level uniqueness constraints**, which is exactly this story's shape.

Because these tests run migrations, they also give Task 2's migration real coverage.

### ⚠️ Trap 2 — `LIKE` stops working on `Login`

Non-deterministic collations do not support pattern-matching operators. Any `LIKE` / `ILIKE` / `StartsWith` / `Contains` translated to SQL **against the `Login` column** will throw at runtime.

Verified at time of writing: **nothing in the codebase does this** — `Login` is only ever compared with `==` (`UserRepository.cs:22` and `:34`). If a future story needs username search, it will need a separate expression index or an explicit `COLLATE` clause on that query. Note it, don't pre-build it.

### ⚠️ Trap 3 — ASCII-only logins make this simpler than the general case

`CreateUserCommandValidator.cs:17` constrains logins to `^[A-Za-z0-9_]+$`. This is worth knowing because the usual internationalized-username minefield — Turkish dotless-ı, Unicode case-folding, the [RFC 8265 PRECIS](https://www.rfc-editor.org/rfc/rfc8265) `UsernameCaseMapped` profile — **does not apply here**. `ToLowerInvariant` in the rate-limit tracker is safe, and the ICU collation only ever sees ASCII.

Do not add PRECIS normalization or Unicode case-folding to this story. If the login charset is ever widened, that becomes its own story and this trap should be revisited.

### ⚠️ Trap 4 — do not lowercase `Login` on write

It is tempting to mirror the `Email` treatment and lowercase at write time. **This violates AC 7.** Usernames are shown to people; `TimmyGray` must not become `timmygray` in the profile. The whole reason for choosing a collation over lowercase-on-write is to keep display casing. Comparison is the database's job here, not the handler's.

### Current-state reference (verified 2026-08-27)

| Concern | Current state |
|---|---|
| Column | `Login` `varchar(50)`, `IsRequired`, **default (deterministic) collation** — `UserConfiguration.cs:31` |
| Index | `IX_Users_Login` unique, partial `WHERE "IsDeleted" = FALSE` — `UserConfiguration.cs:33` |
| Lookups | `u.Login == login` — case-sensitive — `UserRepository.cs:22`, `:34` |
| Registration | `LoginName.Trim()`, **not** lowercased — `CreateUserCommandHandler.cs:32` |
| Duplicate check | `GetByLoginAsync` then `ConflictException` — `CreateUserCommandHandler.cs:41-44` |
| Charset | `^[A-Za-z0-9_]+$` — `CreateUserCommandValidator.cs:17` |
| Email (contrast) | lowercased at write time — already case-insensitive |

**Net effect today:** `Bob` and `bob` both register successfully and are two separate accounts. This story ends that.

### Testing Requirements

**PostgreSQL-backed** (new fixture following `UsersControllerConcurrencyTests`; skip-if-unavailable):

1. Register `TimmyGray`; register `timmygray` → `409 Conflict`. (AC 1)
2. Register `TimmyGray`; register `TIMMYGRAY` → `409 Conflict`. (AC 1)
3. Register `TimmyGray`, soft-delete that user, register `timmygray` → `201 Created`. (AC 3)
4. Register `TimmyGray`; sign in with `timmygray`, `TIMMYGRAY`, `TimmyGray` → all `200 OK`, all returning the same `id`. (AC 4)
5. Register `TimmyGray`; `GET /api/v1/users/me` returns `loginName` exactly `TimmyGray`. (AC 7)
6. Direct-repository check: `GetByLoginAsync("timmygray")` resolves a user stored as `TimmyGray`. (AC 4)
7. Migration round-trip: `Up()` then `Down()` leaves a schema the `Up()` can be re-applied to.

**In-memory (still valid — no collation dependency):**

8. `SignInAttemptTracker`: `Bob` / `bob` / `BOB` share one lockout bucket (restored `Buckets_AreCaseNormalized`). (AC 8)
9. Existing Story 1.2 anti-enumeration tests continue to pass **unmodified** — byte-identical 401, dummy-hash equivalent-cost verify. (AC 6)
10. Email sign-in tests continue to pass unmodified. (AC 5)

### References

- [PostgreSQL — `citext` and why non-deterministic collations are preferred](https://www.postgresql.org/docs/current/citext.html)
- [Npgsql EF Core — Collations and Case Sensitivity](https://www.npgsql.org/efcore/misc/collations-and-case-sensitivity.html)
- [RFC 8265 — PRECIS usernames](https://www.rfc-editor.org/rfc/rfc8265) (context for Trap 3; not applicable here)
- [OWASP Authentication Cheat Sheet — current](https://cheatsheetseries.owasp.org/cheatsheets/Authentication_Cheat_Sheet.html) · [older revision containing the case-insensitivity line](https://github.com/OWASP/CheatSheetSeries/blob/7d94e9a29174b8fd76235ca60f47245d1f34df1e/cheatsheets/Authentication_Cheat_Sheet.md)
- Origin: `_bmad-output/archive/implementation-artifacts/1-2-returning-user-sign-in-endpoint.md` — Remediation Notes items 4 and "Deliberate AC deviation"

## Dev Agent Record

### Context Reference

### Agent Model Used

### Debug Log References

### Completion Notes List

### Change Log

- 2026-08-27: Story drafted from the Story 1.2 adversarial code-review finding (cross-account lockout DoS). Blocked pending product sign-off + the Task 0 data audit.

### File List
