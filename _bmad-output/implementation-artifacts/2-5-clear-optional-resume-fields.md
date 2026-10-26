# Story 2.5: Clear optional résumé fields & land on the list after create

Status: ready-for-dev

## Story

As a **user**,
I want **to create a résumé without filling every field, to remove a skills list, salary, currency, work-location or experience value I no longer want, and to see my new résumé in my list right after creating it**,
so that **my résumé says only what I mean it to say, and I can tell the create worked**.

<!-- Track: [BE] + [FE] — Covers: FR11, FR12, FR14 (+ FR41 unchanged) — Source: Epic 2 retrospective 2026-10-26, deferred-work.md "Deferred from: Story 2.3" + "Deferred from: Story 2.2" + "Found in: Epic 2 browser pass" (B1, B2) -->

## Decision Record

- 2026-10-26: **This is a follow-up story inside Epic 2, which reopens to `in-progress`.** The work is résumé management and closes two open items from Epic 2 (deferred from 2.3 and 2.2). User decision, recorded during the Epic 2 retrospective.
- 2026-10-26: **Clear convention: an empty value means clear, `null`/absent means unchanged.** This is the rule the API already follows. `title: ""`, `locations: []` and `languages: []` already clear, and `PATCH /users/me` clears `phone` with `""`. `ResumeMappers.ApplyUpdates` **already maps `""` to `null`** for `currency`, `workLocationType` and `experience`; only `UpdateResumeCommandValidator` rejects it, because `Enum.TryParse("")` fails. Skills follow the same rule: `skills: []` clears. The parent PRD (`prd.md` §resume rules) only requires each skill to be non-empty and at most 30 characters. It never required "at least one" skill, so the validator rule `skills must contain at least one value when provided` is stricter than the requirement and is removed.
- 2026-10-26: **Salary is cleared with an explicit `clearSalary: true` flag** (user decision; options considered: flag, merge-patch explicit-`null`, leave non-clearable). `salary` is a number, so it has no "empty" value. Merge-patch `null` would need custom presence-tracking binding, because System.Text.Json cannot tell an absent property from `null`, and it would break the null-means-unchanged rule every other field follows. The flag is additive and explicit, and the generated OpenAPI types pick it up. Sending `clearSalary: true` **and** a `salary` value is rejected with a `400` field error rather than letting one silently win.
- 2026-10-26: **A cleared field is persisted as `null` for every field, skills included** (`skills: []` stores `null`). The read model already maps `null` to `[]` (`ResumeResult.Skills = resume.Skills ?? []`) and to `""` for the enums, so API responses do not change shape. One stored representation for "cleared" keeps the Epic 4 generation grounding simple.
- 2026-10-26: **After a successful create the user lands on `/resumes`** (user decision; options considered: list, the new résumé's detail page, stay and reset). The reason for staying on the page in Story 2.1 was that `/resumes` was still a stub (see the `ResumeCreatePage` doc comment). Story 2.2 shipped the list, and Story 2.4 moved the toast host into `AppShellComponent`, so a toast now survives navigation. The list cache is already invalidated by `ResumeService.create()` (2.1 AC2), so the list fetches a fresh first page that contains the new résumé. History is **not** replaced (`replaceUrl` stays false). Going Back to a blank create form is harmless, unlike 2.4's Back to a deleted résumé.
- 2026-10-27: **The create contract bug from the browser pass (B1) and the stale enum message (B2) are folded into this story** (user decision). They touch the same validators and the same "optional résumé fields" rule. **B1:** `CreateResumeCommand` declares `string Title`, `string[] Skills` and `string WorkLocationType` as non-nullable (`= null!`). With `<Nullable>enable</Nullable>`, ASP.NET MVC adds an **implicit `[Required]`** to non-nullable reference-type properties and rejects the request during model binding, before FluentValidation runs. The PRD (`prd.md` §Create Resume) says all three are optional and "an empty payload is valid". The create form omits each of them when empty (`resume-form.ts` `buildCommand()`), so today a résumé can only be created when the title, at least one skill **and** a work-location are all filled in. Verified live on 2026-10-26: `{}` → 400 on all three; `{"title":"t"}` → 400 on `Skills` and `WorkLocationType`; `{"title":"t","skills":[],"workLocationType":"Remote"}` → 201. **Fix:** make the three properties nullable (`string?`, `string[]?`, `string?`, with no `= null!`). Don't suppress the implicit-required behaviour globally (`SuppressImplicitRequiredAttributeForNonNullableReferenceTypes`), because that would silently change every other endpoint's contract. `ResumeMappers.ToResume`, `CreateResumeCommandValidator` (its `When(!IsNullOrEmpty)` guards) and the nullable `Resume` domain fields already handle `null`. **B2:** both validators and the `CreateResumeCommand` XML doc list the work-location values as "(remote, office, hybrid)"; the enum is `OnSite`, `Remote`, `Hybrid`.
- 2026-10-26: **Out of scope:** an unsaved-changes guard, skills dedupe / NG0955, legacy half-filled language rows, the three fields with no form controls (`projects`, `certifications`, `excludedWords`), and focus management after navigation (an app-wide gap, deferred in 2.4). Clearing `currency` does **not** imply clearing `salary`, and clearing `salary` does not imply clearing `currency`. They stay independent, as they are on create.

## Acceptance Criteria

1. **Clear optional fields (API)**
   **Given** I own a résumé whose skills, salary, currency, work-location and experience are set
   **When** I `PATCH /api/v1/resumes/{id}` with any of `skills: []`, `currency: ""`, `workLocationType: ""`, `experience: ""`, `clearSalary: true`
   **Then** the response is `200`, and each named field is cleared (returned as `[]`, `""` or `null` for salary, exactly as a never-set field reads today), and fields not named are unchanged (FR14)
   **And** a body containing only `clearSalary: true` counts as an update (it is not rejected as "no updatable fields")

2. **Ambiguous salary is rejected**
   **Given** a PATCH body with `clearSalary: true` **and** a non-null `salary`
   **When** the server validates it
   **Then** it returns `400` with a `Salary` field error and the résumé is unchanged

3. **Existing contracts hold**
   **Given** any other PATCH (values, invalid enums, a skill that is empty or longer than 30 characters, a negative salary, an empty body, cross-user, not-found or soft-deleted)
   **When** it is sent
   **Then** the response is exactly as before (`200` / `400` / `403` / `404` per `authorization-contract-matrix.md` row 2). Only the "empty skills array → 400" and "`""` enum → 400" behaviours change

4. **Clear from the edit form**
   **Given** I am editing a résumé on `/resumes/:id` with those fields set
   **When** I empty any of them and save
   **Then** the PATCH carries only the changed fields using the encodings in AC1, Save is enabled for that change (no "can't be cleared" message anywhere), and on `200` the form shows the fields empty and becomes the new baseline

5. **Land on the list after create**
   **Given** I submit a valid résumé on `/resumes/new`
   **When** `201` returns
   **Then** I land on `/resumes`, a "Résumé created" success toast is announced, and the new résumé is in the freshly fetched list (FR11, FR12)
   **And** the submit button stays disabled until the navigation has settled (no double-create window)
   **And** if the navigation is rejected (it does not land), I stay on the create page, the toast still shows, and the form is reset as before

6. **Create with optional fields omitted (B1)**
   **Given** I am signed in
   **When** I `POST /api/v1/resumes` with `{}`, or with any of `title`, `skills` or `workLocationType` omitted or `null`
   **Then** the response is `201` and the omitted fields read back empty (`title: ""`, `skills: []`, `workLocationType: ""`), as the PRD specifies (FR11)
   **And** on `/resumes/new`, submitting the form with only a title (no skills, no work-location) succeeds and lands on `/resumes` (AC5)
   **And** an invalid `workLocationType`, a skill that is empty or longer than 30 characters, a title over 500 characters, or a negative salary still return `400` with a field error

7. **Accurate enum message (B2)**
   **Given** an invalid `workLocationType` on create or update
   **When** the server rejects it
   **Then** the error message lists the real values: `OnSite`, `Remote`, `Hybrid`

## Tasks / Subtasks

- [ ] Task 0: Test-first baseline
  - [ ] Confirm green before changing anything: `dotnet test backend/JobNecto.slnx` (expect 632 passing) and `cd frontend && npx ng test --no-watch` (expect 607 passing at `0426474`).

- [ ] Task 1: Command + validator (AC: 1, 2, 3)
  - [ ] `backend/src/JobNecto.Application/Resumes/UpdateResumeCommand.cs` (UPDATE): add `public bool ClearSalary { get; set; }` with an XML doc: "When true, clears the stored salary. Must not be combined with `Salary`."
  - [ ] `backend/src/JobNecto.Application/Resumes/Validators/UpdateResumeCommandValidator.cs` (UPDATE):
    - Delete the `Skills` "at least one value when provided" rule. Keep the per-item `NotEmpty` and `MaximumLength(30)` rules (they run only when `Length > 0`).
    - For `WorkLocationType`, `Currency` and `Experience`, accept `value == null || value == "" || Enum.TryParse(...)`. Decide whether whitespace-only counts as clear: `ApplyUpdates` treats `IsNullOrWhiteSpace` as clear, so accept `string.IsNullOrWhiteSpace(value)` to match it.
    - Add `RuleFor(x => x.Salary).Null().When(x => x.ClearSalary).WithMessage("salary cannot be provided together with clearSalary.")`.
    - Add `|| command.ClearSalary` to `HasAtLeastOneUpdatableField`.
- [ ] Task 1b: Make the create contract match the PRD (AC: 6, 7)
  - [ ] `backend/src/JobNecto.Application/Resumes/CreateResumeCommand.cs` (UPDATE):
    - `public string? Title { get; set; }`, `public string[]? Skills { get; set; }`, `public string? WorkLocationType { get; set; }`. Drop the `= null!` initializers.
    - Fix the `WorkLocationType` XML doc to "(OnSite, Remote, Hybrid)".
    - Leave `UserId` and the already-nullable properties alone.
  - [ ] `backend/src/JobNecto.Application/Resumes/Validators/CreateResumeCommandValidator.cs` and `UpdateResumeCommandValidator.cs` (UPDATE): change the `workLocationType` message to `... valid WorkLocationType enum value (OnSite, Remote, Hybrid).`
  - [ ] Build with `--warnaserror` to surface any nullable warning in code that reads these properties. As of 2026-10-27 only `ResumeMappers.ToResume` reads them, and it handles `null`; fix any new warning at its source, never with `!`.
- [ ] Task 2: Apply the clears (AC: 1)
  - [ ] `backend/src/JobNecto.Application/Resumes/Mappers/ResumeMappers.cs` (`ApplyUpdates`, UPDATE):
    - `if (command.ClearSalary) resume.Salary = null; else if (command.Salary.HasValue) resume.Salary = command.Salary.Value;`
    - `if (command.Skills != null) resume.Skills = command.Skills.Length == 0 ? null : command.Skills;`
    - The enum branches already map `""` to `null`; leave them alone.
    - Update the method's XML doc so it states the clear convention.
- [ ] Task 3: Backend tests (AC: 1, 2, 3)
  - [ ] `backend/tests/JobNecto.Tests/Application/Resumes/UpdateResumeCommandValidatorTests.cs` (UPDATE):
    - **Invert** `Validate_EmptySkillsArray_FailsWithSkillsError` so an empty array passes.
    - Add these cases:
      - `""` and whitespace pass for each of the three enum fields.
      - `ClearSalary` alone passes. It also satisfies "at least one field".
      - `ClearSalary` with `Salary = 0` fails on `Salary`. Use 0 to prove the rule tests presence, not truthiness.
      - Invalid enum strings still fail.
      - A skill that is empty or longer than 30 characters still fails.
  - [ ] `backend/tests/JobNecto.Tests/Application/Resumes/ResumeMappersTests.cs` (UPDATE):
    - `ClearSalary` nulls the salary.
    - `ClearSalary = false` with a null `Salary` leaves the salary untouched.
    - `Skills = []` stores `null`.
    - `""` for each enum stores `null`.
    - Fields that are not named stay unchanged.
  - [ ] `backend/tests/JobNecto.Tests/Application/Resumes/UpdateResumeCommandHandlerTests.cs` (UPDATE if needed): one clear-path test in the existing style.
  - [ ] `backend/tests/JobNecto.Tests/API/ResumesControllerTests.cs` (UPDATE):
    - **Replace** `Update_EmptySkills_Returns400WithValidationErrors` with `Update_EmptySkills_ClearsSkills_Returns200`.
    - Add `Update_ClearAllOptionalFields_Returns200AndReadsBackEmpty`. Create the résumé with all five fields set, PATCH the clears, then GET and assert `skills == []`, `salary == null`, and `currency`, `workLocationType` and `experience` all `""`.
    - Add `Update_ClearSalaryWithSalary_Returns400`.
    - Add `Update_ClearSalaryOnly_Returns200`.
    - Use a fresh `JobNectoApiFactory` per test and the explicit auth cookie, following the existing `Update_*` tests.
- [ ] Task 3b: Create-contract tests (AC: 6, 7)
  - [ ] `backend/tests/JobNecto.Tests/API/ResumesControllerTests.cs` (UPDATE):
    - `Create_EmptyPayload_Returns201WithEmptyFields`: `{}` → 201, then read back `title == ""`, `skills == []`, `workLocationType == ""`.
    - `Create_TitleOnly_Returns201`.
    - `Create_WithoutWorkLocationType_Returns201`.
    - `Create_WithoutSkills_Returns201`.
    - `Create_InvalidWorkLocationType_Returns400WithAccurateMessage`: the message contains `OnSite`.
    - Use the existing `CreateUserAndGetCookieAsync` / explicit-cookie pattern.
    - These tests **must go through HTTP**. The bug lives in MVC model binding, so a handler or validator unit test cannot reproduce it. Write them first and watch them fail.
  - [ ] `backend/tests/JobNecto.Tests/Application/Resumes/CreateResumeCommandValidatorTests.cs` (UPDATE): a command with `null` title, skills and work-location is valid.
  - [ ] `backend/tests/JobNecto.Tests/API/Resumes/ResumesApiTests.cs`: keep `Create_MissingTitle_Returns201_Because_Logic_Allows_Optional` passing. It sends `Title = ""`, not an omitted title, so it never caught B1. Leave its name and comment alone.
- [ ] Task 4: Regenerate the API types (AC: 4, 6)
  - [ ] Run the API locally (AGENTS.md → *Running the API*), then `cd frontend && npm run gen:api`. `UpdateResumeCommand` in `frontend/src/shared/api/generated/schema.ts` (generated, coverage-excluded) must gain `clearSalary?: boolean`, and `CreateResumeCommand`'s `title`, `skills` and `workLocationType` become nullable (`null | ...`).
  - [ ] Check that the regen diff touches **only** `UpdateResumeCommand` and `CreateResumeCommand`. If anything else moves, investigate it before you commit. `resume-form.ts` `buildCommand()` already sends `undefined` for empty values, so no frontend code change is needed for AC6. Confirm it type-checks after the regen.
- [ ] Task 5: Form diff sends clears (AC: 4)
  - [ ] `frontend/src/widgets/resume-form/resume-form.diff.ts` (UPDATE):
    - Skills emptied: `command.skills = []`.
    - Salary emptied: `command.clearSalary = true`, and `salary` is never sent.
    - Enum emptied: `command[field] = ''`.
    - **Delete `blockedClears`** from `UpdateDiff` and every doc comment that mentions it. Keep the `UpdateDiff` wrapper only if it still carries more than `command`; otherwise return the command and update the callers.
    - A salary of `0` is still a value, not a clear.
  - [ ] `frontend/src/widgets/resume-form/resume-form.ts` (UPDATE):
    - Delete `BLOCKED_CLEAR_MESSAGE`, `BLOCKED_CLEAR_FIELD`, the blocked branch in `errorFor`, and the `blockedClears` conditions in `canSave` and submit (around lines 90-100, 217, 248-249 and 288).
    - Update the doc comment that says Save requires "no blocked clears".
  - [ ] `frontend/src/widgets/resume-form/resume-form.diff.spec.ts` (UPDATE):
    - Rewrite the `fields the API cannot clear` block (around line 165) as `clearing optional fields`, one case per field with its encoding.
    - Add these cases:
      - A clear plus a value change in one diff.
      - Clearing an already-empty field sends nothing.
      - Salary `0` → `null` sends `clearSalary` (keep the zero-is-a-value test).
      - `clearSalary` and `salary` never appear together.
  - [ ] `frontend/src/widgets/resume-form/resume-form.spec.ts` (UPDATE): remove the blocked-clear message assertions. Emptying a field now enables Save and emits `update` with the clear encoding.
  - [ ] `frontend/src/pages/resume-detail/resume-detail.page.spec.ts` (UPDATE): add one end-to-end clear. Empty skills and salary, save, and expect the PATCH body `{ skills: [], clearSalary: true }`. Flush a `200` with those fields empty, then the form shows them empty and Save is disabled again (new baseline).
- [ ] Task 6: Navigate after create (AC: 5)
  - [ ] `frontend/src/pages/resume-create/resume-create.page.ts` (UPDATE):
    - Inject `Router`. On `next`, `navigate(['/resumes'])`, then show the toast in `.then(...)`, mirroring `ResumeDetailPage.afterDeleted()` (`frontend/src/pages/resume-detail/resume-detail.page.ts:202-214`) but **without** `replaceUrl`.
    - Keep `submitting` true until the navigation promise settles. See Trap 2.
    - If the navigation does not land (`navigated === false`), keep today's behaviour: `resetForm()`, and the toast still shows.
    - Rewrite the class doc comment. The current one cites the stub-list reason, which is now obsolete.
  - [ ] `frontend/src/pages/resume-create/resume-create.page.spec.ts` (UPDATE):
    - Replace the `on 201: … resets the form` test with: `201` navigates to `/resumes`, the toast shows after the navigation, and the list cache is invalidated.
    - Add `submitting()` stays true until the navigation settles, and a second submit in that window sends no second POST.
    - Add a rejected navigation, which falls back to toast plus reset.
    - Keep the 400, non-400 and destroy-cancel tests passing.
    - Use a router that has a `resumes` route (stub component) or `RouterTestingHarness`, as `resume-detail.page.spec.ts` does. With `provideRouter([])` the navigation would always fail.
  - [ ] `frontend/src/widgets/resume-form/resume-form.spec.ts` (UPDATE, if not already covered): a create with only a title emits a command with **no** `skills` and **no** `workLocationType` keys. This pins the frontend half of AC6.
- [ ] Task 7: Docs hygiene
  - [ ] `_bmad-output/implementation-artifacts/deferred-work.md` (UPDATE): mark these entries `[RESOLVED in Story 2.5]`, using the file's existing resolved-marker style:
    - the 2.3 "API cannot clear…" entry
    - the 2.2 "`ResumeCreatePage` still stays on `/resumes/new`" entry
    - B1 and B2 under "Found in: Epic 2 browser pass"
- [ ] Task 8: Verification and test coverage (AC: all)
  - [ ] `dotnet test backend/JobNecto.slnx`: all pass.
  - [ ] `dotnet build backend/JobNecto.slnx --configuration Release --warnaserror`: 0 warnings.
  - [ ] Backend coverage gate: `dotnet test backend/JobNecto.slnx --collect:"XPlat Code Coverage" --settings backend/coverlet.runsettings --results-directory ./coverage/backend`, then `python scripts/check_coverage.py ./coverage/backend --threshold 80`. **Run it.** Stories 2.2-2.4 skipped it, claiming "python is not on PATH", but `python` resolves to `C:\Python314\python.exe` in both Git Bash and PowerShell (verified 2026-10-26). Use `python`, not `python3`. In Git Bash, `python3` hits the Microsoft Store stub.
  - [ ] `cd frontend && npx ng test --no-watch`: all pass, and the per-file coverage gate is clean.
  - [ ] `cd frontend && npx ng build --configuration production`: clean.
  - [ ] Live check against the throwaway Postgres (same approach as 2.3 and 2.4): run AC1-AC3 through `curl`, and record the status codes in Completion Notes.
  - [ ] Browser smoke if Claude-in-Chrome is connected. If it isn't, say so explicitly in Completion Notes:
    - Clear each field on `/resumes/:id` and save.
    - Create a résumé and land on the list with the toast.
    - Create a résumé with **only a title** (AC6) and land on the list.

- [ ] Task 9: PR Review (RV) recorded before merge (AGENTS.md completion checklist item 5)
  - [ ] `jobnecto-qa` (or `bmad-code-review`) runs the adversarial review on the PR and fills `## PR Review (RV)` below. No merge before it is recorded; a self-review does not count.

## Dev Notes

### Technical Requirements

- Clean Architecture: everything backend lives in `JobNecto.Application/Resumes` (command, validator, mapper). There are **no** controller changes, because `ResumesController.UpdateAsync` binds `UpdateResumeCommand` straight from the body and only sets `ResumeId`/`UserId`. Don't add a request DTO. There are no domain changes either, because `Resume.Salary`, `Currency`, `Skills`, `WorkLocationType` and `Experience` are all nullable already. There is no migration.
- The frontend has no new components, services or routes. `ResumeService.update()` and `create()` are unchanged.
- Namespaces match folders (AGENTS.md). Give anything new or changed with non-trivial behaviour an XML doc or TSDoc.

### API / Contract Guardrail

- `PATCH /api/v1/resumes/{id}`: `200` success, `400` validation, `401` no session, `403` cross-user, `404` not-found or soft-deleted (`authorization-contract-matrix.md` row 2). These stay unchanged.
- Additive contract change: the request gains `clearSalary` (boolean, default false). Responses (`ResumeResult`) keep their shape. Cleared fields read back exactly like never-set fields: `skills: []`, `salary: null`, and `""` for the three enums.
- Rule summary the dev must not break: **absent or `null` = unchanged; empty (`""`/`[]`) = clear; `clearSalary: true` = clear salary.**

### ⚠️ Trap 1: Existing tests assert the old behaviour

`UpdateResumeCommandValidatorTests.Validate_EmptySkillsArray_FailsWithSkillsError` and `ResumesControllerTests.Update_EmptySkills_Returns400WithValidationErrors` encode the rule this story removes. Invert or replace them; don't delete them silently. Name them in the File List as `(UPDATED — inverted: empty skills now clears)`.

### ⚠️ Trap 2: `finalize` re-enables Submit before the navigation lands

`ResumeCreatePage.onCreate` uses `finalize(() => this.submitting.set(false))`. The HTTP observable completes right after `next`, so `submitting` turns false while `router.navigate` is still pending, which reopens a double-create window during the lazy route load. Story 2.4 hit the same problem with `deleting` (its review finding: "deleting was reset on the 204 while navigation was still pending"). On success, reset `submitting` only after the navigation settles. On error, reset it as today.

### ⚠️ Trap 3: `takeUntilDestroyed` vs. the post-navigation toast

The page is destroyed when the navigation lands, and the toast is shown in the navigation's `.then`, after destroy. That is fine. `ToastService` is `providedIn: 'root'` and the host lives in `AppShellComponent` (since 2.4), so the toast renders. Don't move the toast inside anything tied to the page's `DestroyRef`.

### ⚠️ Trap 4: `provideRouter([])` makes every navigation fail in the spec

`resume-create.page.spec.ts` currently uses `provideRouter([])`. Navigating to `/resumes` there resolves to `false`, which would exercise only the fallback branch. Register a `resumes` route with a stub component for the success path, and use an empty router (or a guard returning `false`) for the rejected-navigation test.

### ⚠️ Trap 5b: B1 is invisible below the HTTP layer

The implicit `[Required]` comes from MVC model validation on the controller's `[ApiController]` binding, not from FluentValidation or the handler. `CreateResumeCommandValidatorTests` and handler tests pass with `null` fields today. Only an HTTP-level test (`JobNectoApiFactory`) that **omits** the property, as opposed to sending `""`, reproduces the bug. The existing `Create_MissingTitle_…` test sends `Title = ""`, which is why it never caught B1. Don't fix B1 by sending `""` from the frontend; fix the contract.

### ⚠️ Trap 5: Salary `0` and `null`

`resumeToFormValue` maps a missing or unparsable salary to `null`. `0` is a real value. A diff from `0` to `null` is a clear (`clearSalary: true`); a diff from `null` to `0` sets `salary: 0`. The existing spec "treats a salary of 0 as a value, not as cleared" must keep passing in its new form.

### File Structure Requirements

- `backend/src/JobNecto.Application/Resumes/UpdateResumeCommand.cs` (UPDATE)
- `backend/src/JobNecto.Application/Resumes/CreateResumeCommand.cs` (UPDATE)
- `backend/src/JobNecto.Application/Resumes/Validators/CreateResumeCommandValidator.cs` (UPDATE)
- `backend/tests/JobNecto.Tests/Application/Resumes/CreateResumeCommandValidatorTests.cs` (UPDATE)
- `backend/src/JobNecto.Application/Resumes/Validators/UpdateResumeCommandValidator.cs` (UPDATE)
- `backend/src/JobNecto.Application/Resumes/Mappers/ResumeMappers.cs` (UPDATE)
- `backend/tests/JobNecto.Tests/Application/Resumes/UpdateResumeCommandValidatorTests.cs` (UPDATE)
- `backend/tests/JobNecto.Tests/Application/Resumes/ResumeMappersTests.cs` (UPDATE)
- `backend/tests/JobNecto.Tests/Application/Resumes/UpdateResumeCommandHandlerTests.cs` (UPDATE, if a clear-path test is added)
- `backend/tests/JobNecto.Tests/API/ResumesControllerTests.cs` (UPDATE)
- `frontend/src/shared/api/generated/schema.ts` (UPDATE, regenerated)
- `frontend/src/widgets/resume-form/resume-form.diff.ts` + `.spec.ts` (UPDATE)
- `frontend/src/widgets/resume-form/resume-form.ts` + `.spec.ts` (UPDATE; `.html` only if it references the blocked message)
- `frontend/src/pages/resume-detail/resume-detail.page.spec.ts` (UPDATE)
- `frontend/src/pages/resume-create/resume-create.page.ts` + `.spec.ts` (UPDATE)
- `_bmad-output/implementation-artifacts/deferred-work.md` (UPDATE)

### Testing Requirements

- Backend API tests: use a fresh `JobNectoApiFactory` per test with `HandleCookies = false` and the explicit auth cookie from `CreateUserAndGetCookieAsync`, plus the `CreateResumeAsync` / `UpdateResumeAsync` helpers already in `ResumesControllerTests.cs`.
- Frontend: Vitest via `ng test`. Use `HttpTestingController` with predicate-matching `expectOne` (not exact-URL matching when there's a query string, a trap hit in 1.6 and 2.1). Keep `afterEach` failure-safe, as in 2.2 and 2.3. Assert the PATCH **body**, not just that a request was made.
- Make tests fail when the behaviour is removed. Story 2.4's mutation pass found vacuous tests, such as "Save disabled while deleting" on a pristine form. For each new test, check that it would fail if the clear encoding or the post-navigation `submitting` hold were removed.

### Previous Story Intelligence (2.3, 2.4)

- 2.3 built `buildUpdateCommand` and the blocked-clear UI. Its "changed" means a deep diff from the loaded résumé, not `form.dirty`, and the save response becomes the new baseline (snapshot-at-send keeps edits made during the save). Don't disturb either.
- 2.3's Trap 2 told the dev the wrong reset order (rebuild language rows *before* reset). The spec caught it. Trust the specs over the prose.
- 2.4 moved the toast host into `AppShellComponent` and established the navigate-then-toast pattern with `deleting` held until navigation lands. Reuse that pattern.
- 2.4 found that a toast fired into an `aria-live` region created in the same tick may not be announced. The shell host is persistent, so this is fine now, but don't add a page-local host back.

### Git Intelligence Summary

- `497b11f` (2.4): `ResumeDetailPage` delete flow, `afterDeleted()` navigation pattern, toast host moved to the shell.
- `e09d033` (2.3): `resume-form.diff.ts`, blocked-clear handling, `ResumeDetailPage` save flow.
- `6a93657` (2.1): `ResumeCreatePage`, the stay-on-page Decision Record, OpenAPI enum-as-string fix (`Http.Json.JsonOptions`).
- `ce9f214` (PR #97): `System.Security.Cryptography.Pkcs` pinned to 9.0.21. If CI's backend build fails with NU1902 or NU1903 again, it's this kind of transitive advisory, not this story.

### Latest Stack Information

- .NET 10 / ASP.NET Core 10.0.3, MediatR 14.0.0, FluentValidation 12.1.1 (`_bmad-output/project-context.md`).
- Angular standalone + Signals, typed Reactive Forms, Vitest via `ng test`, `openapi-typescript` ^7.13.0 (`npm run gen:api`).

### References

- [Source: `_bmad-output/planning-artifacts/epics.md` - Story 2.5]
- [Source: `_bmad-output/planning-artifacts/prd.md` - Resume validation rules (skills: optional; each item non-empty, ≤30)]
- [Source: `_bmad-output/planning-artifacts/architecture/authorization-contract-matrix.md` - row 2, PATCH /api/v1/resumes/{id}]
- [Source: `_bmad-output/planning-artifacts/architecture/demo-mvp-architecture-decisions.md` - Forms: submit only changed fields (PATCH)]
- [Source: `_bmad-output/implementation-artifacts/deferred-work.md` - Deferred from: Story 2.3 (cannot clear), Story 2.2 (stays on /resumes/new)]
- [Source: `_bmad-output/archive/implementation-artifacts/2-3-view-edit-a-resume.md` - Decision Record, PATCH semantics]
- [Source: `_bmad-output/archive/implementation-artifacts/2-4-soft-delete-a-resume.md` - Completion Notes, navigate-then-toast]
- [Source: `_bmad-output/implementation-artifacts/epic-2-retro-2026-10-26.md` - Action items A1, A2; §10 Browser Pass findings B1, B2]
- [Source: `_bmad-output/planning-artifacts/prd.md` - Feature: Create Resume (title, skills, workLocationType optional; empty payload valid)]

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List

## PR Review (RV)

<!-- Filled by jobnecto-qa / bmad-code-review before merge (AGENTS.md completion checklist item 5). Not a self-review. -->

## Change Log

- 2026-10-26: Story created (ready-for-dev) from the Epic 2 retrospective. User decisions: Story 2.5 inside Epic 2; `clearSalary` flag; land on `/resumes` after create.
- 2026-10-27: Added AC6 (create with optional fields omitted, B1) and AC7 (accurate work-location message, B2) from the Epic 2 browser pass (user decision). Tasks 1b and 3b, Trap 5b, and the AC6 smoke step were added.
