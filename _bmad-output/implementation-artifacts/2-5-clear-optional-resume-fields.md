# Story 2.5: Clear optional résumé fields & land on the list after create

Status: review

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
   **And** a rejected navigation behaves like a refused one: a navigation promise that rejects (e.g. a `ChunkLoadError`) falls back exactly like `navigated === false`, with the toast, the reset, and `submitting` false

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

- [x] Task 0: Test-first baseline
  - [x] Confirm green before changing anything: `dotnet test backend/JobNecto.slnx` (expect 632 passing) and `cd frontend && npx ng test --no-watch` (expect 607 passing at `0426474`).

- [x] Task 1: Command + validator (AC: 1, 2, 3)
  - [x] `backend/src/JobNecto.Application/Resumes/UpdateResumeCommand.cs` (UPDATE): add `public bool ClearSalary { get; set; }` with an XML doc: "When true, clears the stored salary. Must not be combined with `Salary`."
  - [x] `backend/src/JobNecto.Application/Resumes/Validators/UpdateResumeCommandValidator.cs` (UPDATE):
    - Delete the `Skills` "at least one value when provided" rule. Keep the per-item `NotEmpty` and `MaximumLength(30)` rules (they run only when `Length > 0`).
    - For `WorkLocationType`, `Currency` and `Experience`, accept `value == null || value == "" || Enum.TryParse(...)`. Decide whether whitespace-only counts as clear: `ApplyUpdates` treats `IsNullOrWhiteSpace` as clear, so accept `string.IsNullOrWhiteSpace(value)` to match it.
    - Add `RuleFor(x => x.Salary).Null().When(x => x.ClearSalary).WithMessage("salary cannot be provided together with clearSalary.")`.
    - Add `|| command.ClearSalary` to `HasAtLeastOneUpdatableField`.
- [x] Task 1b: Make the create contract match the PRD (AC: 6, 7)
  - [x] `backend/src/JobNecto.Application/Resumes/CreateResumeCommand.cs` (UPDATE):
    - `public string? Title { get; set; }`, `public string[]? Skills { get; set; }`, `public string? WorkLocationType { get; set; }`. Drop the `= null!` initializers.
    - Fix the `WorkLocationType` XML doc to "(OnSite, Remote, Hybrid)".
    - Leave `UserId` and the already-nullable properties alone.
  - [x] `backend/src/JobNecto.Application/Resumes/Validators/CreateResumeCommandValidator.cs` and `UpdateResumeCommandValidator.cs` (UPDATE): change the `workLocationType` message to `... valid WorkLocationType enum value (OnSite, Remote, Hybrid).`
  - [x] Build with `--warnaserror` to surface any nullable warning in code that reads these properties. As of 2026-10-27 only `ResumeMappers.ToResume` reads them, and it handles `null`; fix any new warning at its source, never with `!`.
- [x] Task 2: Apply the clears (AC: 1)
  - [x] `backend/src/JobNecto.Application/Resumes/Mappers/ResumeMappers.cs` (`ApplyUpdates`, UPDATE):
    - `if (command.ClearSalary) resume.Salary = null; else if (command.Salary.HasValue) resume.Salary = command.Salary.Value;`
    - `if (command.Skills != null) resume.Skills = command.Skills.Length == 0 ? null : command.Skills;`
    - The enum branches already map `""` to `null`; leave them alone.
    - Update the method's XML doc so it states the clear convention.
- [x] Task 3: Backend tests (AC: 1, 2, 3)
  - [x] `backend/tests/JobNecto.Tests/Application/Resumes/UpdateResumeCommandValidatorTests.cs` (UPDATE):
    - **Invert** `Validate_EmptySkillsArray_FailsWithSkillsError` so an empty array passes.
    - Add these cases:
      - `""` and whitespace pass for each of the three enum fields.
      - `ClearSalary` alone passes. It also satisfies "at least one field".
      - `ClearSalary` with `Salary = 0` fails on `Salary`. Use 0 to prove the rule tests presence, not truthiness.
      - Invalid enum strings still fail.
      - A skill that is empty or longer than 30 characters still fails.
  - [x] `backend/tests/JobNecto.Tests/Application/Resumes/ResumeMappersTests.cs` (UPDATE):
    - `ClearSalary` nulls the salary.
    - `ClearSalary = false` with a null `Salary` leaves the salary untouched.
    - `Skills = []` stores `null`.
    - `""` for each enum stores `null`.
    - Fields that are not named stay unchanged.
  - [x] `backend/tests/JobNecto.Tests/Application/Resumes/UpdateResumeCommandHandlerTests.cs` (UPDATE if needed): one clear-path test in the existing style.
  - [x] `backend/tests/JobNecto.Tests/API/ResumesControllerTests.cs` (UPDATE):
    - **Replace** `Update_EmptySkills_Returns400WithValidationErrors` with `Update_EmptySkills_ClearsSkills_Returns200`.
    - Add `Update_ClearAllOptionalFields_Returns200AndReadsBackEmpty`. Create the résumé with all five fields set, PATCH the clears, then GET and assert `skills == []`, `salary == null`, and `currency`, `workLocationType` and `experience` all `""`.
    - Add `Update_ClearSalaryWithSalary_Returns400`.
    - Add `Update_ClearSalaryOnly_Returns200`.
    - Use a fresh `JobNectoApiFactory` per test and the explicit auth cookie, following the existing `Update_*` tests.
- [x] Task 3b: Create-contract tests (AC: 6, 7)
  - [x] `backend/tests/JobNecto.Tests/API/ResumesControllerTests.cs` (UPDATE):
    - `Create_EmptyPayload_Returns201WithEmptyFields`: `{}` → 201, then read back `title == ""`, `skills == []`, `workLocationType == ""`.
    - `Create_TitleOnly_Returns201`.
    - `Create_WithoutWorkLocationType_Returns201`.
    - `Create_WithoutSkills_Returns201`.
    - `Create_InvalidWorkLocationType_Returns400WithAccurateMessage`: the message contains `OnSite`.
    - Use the existing `CreateUserAndGetCookieAsync` / explicit-cookie pattern.
    - These tests **must go through HTTP**. The bug lives in MVC model binding, so a handler or validator unit test cannot reproduce it. Write them first and watch them fail.
  - [x] `backend/tests/JobNecto.Tests/Application/Resumes/CreateResumeCommandValidatorTests.cs` (UPDATE): a command with `null` title, skills and work-location is valid.
  - [x] `backend/tests/JobNecto.Tests/API/Resumes/ResumesApiTests.cs`: keep `Create_MissingTitle_Returns201_Because_Logic_Allows_Optional` passing. It sends `Title = ""`, not an omitted title, so it never caught B1. Leave its name and comment alone.
- [x] Task 4: Regenerate the API types (AC: 4, 6)
  - [x] Run the API locally (AGENTS.md → *Running the API*), then `cd frontend && npm run gen:api`. `UpdateResumeCommand` in `frontend/src/shared/api/generated/schema.ts` (generated, coverage-excluded) must gain `clearSalary?: boolean`, and `CreateResumeCommand`'s `title`, `skills` and `workLocationType` become nullable (`null | ...`).
  - [x] Check that the regen diff touches **only** `UpdateResumeCommand` and `CreateResumeCommand`. If anything else moves, investigate it before you commit. `resume-form.ts` `buildCommand()` already sends `undefined` for empty values, so no frontend code change is needed for AC6. Confirm it type-checks after the regen.
- [x] Task 5: Form diff sends clears (AC: 4)
  - [x] `frontend/src/widgets/resume-form/resume-form.diff.ts` (UPDATE):
    - Skills emptied: `command.skills = []`.
    - Salary emptied: `command.clearSalary = true`, and `salary` is never sent.
    - Enum emptied: `command[field] = ''`.
    - **Delete `blockedClears`** from `UpdateDiff` and every doc comment that mentions it. Keep the `UpdateDiff` wrapper only if it still carries more than `command`; otherwise return the command and update the callers.
    - A salary of `0` is still a value, not a clear.
  - [x] `frontend/src/widgets/resume-form/resume-form.ts` (UPDATE):
    - Delete `BLOCKED_CLEAR_MESSAGE`, `BLOCKED_CLEAR_FIELD`, the blocked branch in `errorFor`, and the `blockedClears` conditions in `canSave` and submit (around lines 90-100, 217, 248-249 and 288).
    - Update the doc comment that says Save requires "no blocked clears".
  - [x] `frontend/src/widgets/resume-form/resume-form.diff.spec.ts` (UPDATE):
    - Rewrite the `fields the API cannot clear` block (around line 165) as `clearing optional fields`, one case per field with its encoding.
    - Add these cases:
      - A clear plus a value change in one diff.
      - Clearing an already-empty field sends nothing.
      - Salary `0` → `null` sends `clearSalary` (keep the zero-is-a-value test).
      - `clearSalary` and `salary` never appear together.
  - [x] `frontend/src/widgets/resume-form/resume-form.spec.ts` (UPDATE): remove the blocked-clear message assertions. Emptying a field now enables Save and emits `update` with the clear encoding.
  - [x] `frontend/src/pages/resume-detail/resume-detail.page.spec.ts` (UPDATE): add one end-to-end clear. Empty skills and salary, save, and expect the PATCH body `{ skills: [], clearSalary: true }`. Flush a `200` with those fields empty, then the form shows them empty and Save is disabled again (new baseline).
- [x] Task 6: Navigate after create (AC: 5)
  - [x] `frontend/src/pages/resume-create/resume-create.page.ts` (UPDATE):
    - Inject `Router`. On `next`, `navigate(['/resumes'])`, then show the toast in `.then(...)`, mirroring `ResumeDetailPage.afterDeleted()` (`frontend/src/pages/resume-detail/resume-detail.page.ts:202-214`) but **without** `replaceUrl`.
    - Keep `submitting` true until the navigation promise settles. See Trap 2.
    - If the navigation does not land (`navigated === false`), keep today's behaviour: `resetForm()`, and the toast still shows.
    - Rewrite the class doc comment. The current one cites the stub-list reason, which is now obsolete.
  - [x] `frontend/src/pages/resume-create/resume-create.page.spec.ts` (UPDATE):
    - Replace the `on 201: … resets the form` test with: `201` navigates to `/resumes`, the toast shows after the navigation, and the list cache is invalidated.
    - Add `submitting()` stays true until the navigation settles, and a second submit in that window sends no second POST.
    - Add a rejected navigation, which falls back to toast plus reset.
    - Keep the 400, non-400 and destroy-cancel tests passing.
    - Use a router that has a `resumes` route (stub component) or `RouterTestingHarness`, as `resume-detail.page.spec.ts` does. With `provideRouter([])` the navigation would always fail.
  - [x] `frontend/src/widgets/resume-form/resume-form.spec.ts` (UPDATE, if not already covered): a create with only a title emits a command with **no** `skills` and **no** `workLocationType` keys. This pins the frontend half of AC6.
- [x] Task 6b: RV-101 fixes on ResumeDetailPage (retroactive PR #101 review, findings #2 and #3; AC5 parity)
  - [x] RV-101 #2: `frontend/src/pages/resume-detail/resume-detail.page.ts` (UPDATE): `openDelete()` guards on `!this.saving() && !this.deleting()`, so the Delete button stays inert after the 204 while navigation is pending.
  - [x] RV-101 #2: `frontend/src/pages/resume-detail/resume-detail.page.html` (UPDATE): bind the Delete button's `[attr.aria-disabled]="saving() || deleting()"`.
  - [x] RV-101 #2: in the `!navigated` branch of `afterDeleted()`, also `confirmingDelete.set(false)`.
  - [x] RV-101 #2 spec: click Delete after the 204 while navigation is pending. Expect no dialog and no second DELETE.
  - [x] RV-101 #3: `afterDeleted()` gets a rejection handler on `router.navigate(...)` that runs the same honest-state branch as `!navigated`: toast, `deleting(false)`, `confirmingDelete(false)`, not-found state, focus the recovery region.
  - [x] RV-101 #3 spec: `navigate` rejects (a spy with `mockRejectedValue`), and the page ends in that state.
  - [x] Same rejection handling on `ResumeCreatePage`'s navigate-then-toast (AC5): a rejected navigation falls back exactly like `navigated === false`, with toast, reset, and `submitting` false. Spec covers both refused and rejected.

- [x] Task 7: Docs hygiene
  - [x] `_bmad-output/implementation-artifacts/deferred-work.md` (UPDATE): mark these entries `[RESOLVED in Story 2.5]`, using the file's existing resolved-marker style:
    - the 2.3 "API cannot clear…" entry
    - the 2.2 "`ResumeCreatePage` still stays on `/resumes/new`" entry
    - B1 and B2 under "Found in: Epic 2 browser pass"
- [x] Task 8: Verification and test coverage (AC: all)
  - [x] `dotnet test backend/JobNecto.slnx`: all pass.
  - [x] `dotnet build backend/JobNecto.slnx --configuration Release --warnaserror`: 0 warnings.
  - [x] Backend coverage gate: `dotnet test backend/JobNecto.slnx --collect:"XPlat Code Coverage" --settings backend/coverlet.runsettings --results-directory ./coverage/backend`, then `python scripts/check_coverage.py ./coverage/backend --threshold 80`. **Run it.** Stories 2.2-2.4 skipped it, claiming "python is not on PATH", but `python` resolves to `C:\Python314\python.exe` in both Git Bash and PowerShell (verified 2026-10-26). Use `python`, not `python3`. In Git Bash, `python3` hits the Microsoft Store stub.
  - [x] `cd frontend && npx ng test --no-watch`: all pass, and the per-file coverage gate is clean.
  - [x] `cd frontend && npx ng build --configuration production`: clean.
  - [x] Live check against the throwaway Postgres (same approach as 2.3 and 2.4): run AC1-AC3 through `curl`, and record the status codes in Completion Notes.
  - [x] Browser smoke if Claude-in-Chrome is connected. If it isn't, say so explicitly in Completion Notes:
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

Claude Opus 5.5 (`claude-opus-5-5`), `jobnecto-dev` skill, story mode. Baseline revision: `0d1cf58` (master, merge of PR #103).

### Debug Log References

- **Baseline (Task 0, before any change):** `dotnet test backend/JobNecto.slnx` → `Passed!  - Failed: 0, Passed: 632, Skipped: 0, Total: 632`. `cd frontend && npx ng test --no-watch` → `Test Files 40 passed (40)`, `Tests 607 passed (607)`.
- **Red, B1 (Trap 5b):** the new HTTP-level create tests failed before the fix for the right reason, MVC's implicit `[Required]`: e.g. `Create_InvalidOptionalValue_Returns400WithFieldError(negativeSalary)` → `Expected {"Title", "Skills", "WorkLocationType"} to contain "Salary"`. First red run: `Failed: 25, Passed: 93, Total: 118` (resume-filtered).
- **Red, frontend:** 8 new specs failed before the page changes (e.g. `expected '/resumes/new' to be '/resumes'`, `submitting() expected false to be true`, RV-101 `aria-disabled expected 'false' to be 'true'`).
- **Mutation checks (frontend):** reverting the post-navigation `submitting` hold failed `keeps submitting() true after the 201…`; reverting the `openDelete` `deleting()` guard failed `keeps the Delete button inert after the 204…`. Originals restored and re-verified.
- **Build:** `dotnet build backend/JobNecto.slnx --configuration Release --warnaserror`
  ```
  Build succeeded.
      0 Warning(s)
      0 Error(s)
  ```
- **Backend tests:** `dotnet test backend/JobNecto.slnx --configuration Release --no-build` → `Passed!  - Failed: 0, Passed: 665, Skipped: 0, Total: 665` (632 → 665, +33).
- **Backend coverage gate:** `dotnet test backend/JobNecto.slnx --collect:"XPlat Code Coverage" --settings backend/coverlet.runsettings --results-directory ./coverage/backend` then `python scripts/check_coverage.py ./coverage/backend --threshold 80`
  ```
  Coverage: 1882/1954 lines = 96.3% across 97 files (threshold 80%)
  PASS: all files meet the threshold.
  ```
  Touched files: `ResumeMappers.cs` 113/113, `CreateResumeCommandValidator.cs` 40/40, `UpdateResumeCommandValidator.cs` 59/59 (100% each).
- **Frontend tests + per-file gate:** `cd frontend && npx ng test --no-watch` → exit 0, `Test Files 40 passed (40)`, `Tests 616 passed (616)` (607 → 616, +9 net). All files lines 98.27%. Touched: `resume-form.diff.ts` 100%, `resume-form.ts` 97.4%, `resume-form.validators.ts` 100%, `text-field.ts` 96.55%, `pages/resume-create` 100%, `pages/resume-detail` 100%.
- **Frontend production build:** `npx ng build --configuration production` → `Application bundle generation complete.`, no warnings or errors.
- **API types:** `npm run gen:api` against the throwaway API → the `schema.ts` diff touches only `CreateResumeCommand` (`title`, `skills`, `workLocationType` become `null | …`) and `UpdateResumeCommand` (`clearSalary?: boolean`). Nothing else moved.

### Completion Notes List

- **Backend (AC1-3, 6, 7):**
  - `UpdateResumeCommand.ClearSalary` added.
  - The update validator no longer has the "at least one skill" rule. The three enums accept null or blank, so whitespace counts as clear to match `ApplyUpdates`.
  - New rule: `Salary` must be null when `ClearSalary` is set. `ClearSalary` counts as an updatable field.
  - `ApplyUpdates`: `ClearSalary` wins over `Salary`, and `skills: []` is stored as `null`.
  - `CreateResumeCommand.Title`, `Skills` and `WorkLocationType` are now nullable, which removes MVC's implicit `[Required]`. Nothing is suppressed globally.
  - Both validators and the XML doc now say "(OnSite, Remote, Hybrid)".
  - No controller, domain or migration changes.
- **Frontend (AC4, 5):**
  - `buildUpdateCommand` returns the `UpdateResumeCommand` itself and sends the clears: `skills: []`, `clearSalary: true` (never together with `salary`), and `''` for the enums. `UpdateDiff`, `blockedClears` and the blocked-clear message are deleted.
  - `ResumeCreatePage` navigates to `/resumes` with no `replaceUrl`. It toasts in `.then`, and `submitting` stays true until the navigation settles; there is also a re-entry guard. If the navigation is refused or rejects, it toasts, resets the form and sets `submitting` back to false. `finalize` is gone; the error branch resets `submitting`.
- **RV-101 fixes (Task 6b), scope added by the coordinator on 2026-10-27:**
  - `ResumeDetailPage.openDelete()` now also checks `deleting()`, and the Delete button binds `aria-disabled` to `saving() || deleting()`.
  - The post-delete navigation has a rejection handler. The refused and rejected branches share `afterDeleteNavigation(false)`, which shows the toast, sets `deleting` false and `confirmingDelete` false, switches to not-found and focuses the recovery region.
  - BLOCK A (the RV table) is appended to the archived 2.4 story. BLOCK B goes into `deferred-work.md`. `epic-5` in the tracker has a pre-req comment.
- **Self-review patch, salary typos (medium):** after this story an emptied salary sends `clearSalary`. But Chrome reports unparseable number input (for example "5000e") as `value === ''`, which `TextFieldComponent` turned into `null`, so a typo would silently clear the stored salary. Now `TextFieldComponent` emits `NaN` on `validity.badInput` and leaves the typed text in place. `nonNegativeValidator` reports `{ number: true }`, and the form shows "Must be a number." with Save disabled.
- **Self-review patch, create/update consistency:** create now stores `skills: []` as `null`, the same as an update clear, per the Decision Record's "one stored representation". The create validator treats whitespace-only enum values as blank, as update does. Responses are unchanged.
- **Behaviour note for the reviewer:** `PATCH` and `POST` now accept a whitespace-only enum value (`"   "`) as blank. On PATCH it clears the field; on POST it means "not set". Before, both returned `400`. Task 1 sanctioned this for PATCH, and create follows for consistency.
- **Live API check, AC1-AC3, AC6, AC7** (throwaway `postgres:16-alpine` container on :55433, migrations applied, API copy on :5000 without `appsettings.Local.json`, random secrets kept only in a deleted scratch file):
  - **AC6:** `POST {}` → 201, reads back `title ""`, `skills []`, `workLocationType ""`. Title-only → 201. Explicit nulls → 201. No skills → 201.
  - **AC7 and AC6 validation:**
    - Invalid `workLocationType` → 400 with `WorkLocationType`: "...(OnSite, Remote, Hybrid)."
    - Empty skill → 400 with `Skills[0]`.
    - A 31-character skill → 400 with `Skills[0]`.
    - Negative salary → 400 with `Salary`.
  - **AC1:**
    - PATCH with all five clears → 200, and GET reads back `skills []`, `salary null`, `currency ""`, `workLocationType ""`, `experience ""`, with the title kept.
    - `clearSalary` only → 200, and the other fields are unchanged.
    - `skills: []` only → 200.
  - **AC2:** `clearSalary: true` with `salary: 0` → 400 with `Salary`: "salary cannot be provided together with clearSalary.", and GET still shows `salary 5000`.
  - **AC3:**
    - Values → 200. Invalid enum → 400. Empty skill → 400. A 31-character skill → 400. Negative salary → 400.
    - `{}` → 400. `{"clearSalary":false}` → 400.
    - Cross-user → 403. Not found → 404. Soft-deleted (after `DELETE` → 204) → 404. No session → 401.
  - The live check ran before the two self-review backend patches. Their behaviour is covered by unit tests and the full suite.
- **Browser smoke (Claude-in-Chrome, `ng serve` on :4200 against the throwaway API):** ran.
  - **AC6 + AC5:** on `/resumes/new`, I entered only a title and clicked Create. It landed on `/resumes`, the "Résumé created" toast showed, and the list showed the new résumé (1 résumé).
  - **AC4:** on `/resumes/:id` with all five fields set, I removed both skill tags, emptied the salary, and set work-location, experience and currency to "— Select —". No "can't be cleared" message appeared, and Save was enabled. Saving sent `PATCH` → 200 with the "Résumé saved" toast, and Save was disabled again because the saved résumé became the new baseline. GET showed `skills []`, `salary null`, and `currency`, `workLocationType`, `experience` all `""`.
  - The test account was created through the API with the project's fixture password, on the throwaway DB only.
  - Not smoke-tested in a browser: the RV-101 delete-pending race. It is timing-dependent, so it's covered by specs only.
- **Cleanup:** the API process, `ng serve`, the browser tab and the `jobnecto-dev25-pg` container were stopped or removed. Ports 5000, 4200 and 55433 were verified free, and the staged API copy and secrets file were deleted.
- **Task 9** (independent PR Review) is intentionally not done here. `## PR Review (RV)` is left empty for `jobnecto-qa`.

### Review Findings

Self-review (`jobnecto-dev` → `references/review.md`). Four lenses ran in parallel as subagents (Adversarial, Edge-case hunter, Verification gap, Acceptance), and all four returned reports. This does **not** satisfy AGENTS.md checklist item 5; the independent RV is still required.

- [x] [Review][Patch] **Unparseable salary input silently clears the stored salary** (edge-case lens, medium). The browser reports "5000e" as `''`, which became `null`, which now becomes `clearSalary: true`. Fixed: `shared/ui/form/text-field.ts` emits `NaN` on `validity.badInput`, `resume-form.validators.ts` returns `{ number: true }`, and `resume-form.ts` shows "Must be a number.". Specs: `text-field.spec.ts` "emits NaN, not null…", `resume-form.spec.ts` "treats unparseable salary input as an error, not as a clear", and the validators spec.
- [x] [Review][Patch] **Create stored `skills: []` as `[]` while an update clear stores `null`** (adversarial and edge-case lenses, low-medium; contradicts the Decision Record). Fixed in `ResumeMappers.ToEntity`. Spec: `ToEntity_EmptySkills_StoresNull`, plus `ToEntity_NullOptionalFields_StoreNull`.
- [x] [Review][Patch] **Whitespace enum values: PATCH clears, POST returned 400** (adversarial and edge-case lenses, low). `CreateResumeCommandValidator` guards now use `IsNullOrWhiteSpace`. Spec: `WhitespaceEnumValues_PassAsBlank`.
- [x] [Review][Patch] **No test pinned `ClearSalary` precedence over `Salary` in the mapper** (verification lens, low). Added `ApplyUpdates_ClearSalaryWithSalary_ClearWins`.
- [x] [Review][Patch] **A create spec left a real navigation running after it finished** (adversarial lens, test hygiene). `does not replace the history entry…` now mocks `navigate` and awaits it.
- [x] [Review][Patch] **An assertion that could never fail** (verification lens): `not.toContain("can't be cleared")` in the detail-page clear spec was removed. Save-enabled and the PATCH body still prove the behaviour.
- [ ] [Review][Defer] **`Enum.TryParse` accepts `"7"`, `"-1"` and `"Remote, Hybrid"`** (adversarial and edge-case lenses, medium). This predates the story, and AC3 keeps the invalid-enum contract unchanged. Logged in `deferred-work.md` under "code review of 2-5".
- [ ] [Review][Defer] **An abandoned create leaves the list cache stale** (adversarial lens, low). Create-side twin of RV-4. Logged in `deferred-work.md`.
- [ ] [Review][Defer] **The create form stays editable while the post-201 navigation is pending** (adversarial lens, low). Logged in `deferred-work.md`.
- [x] [Review][Dismiss] **Clears are proven only against EF InMemory** (verification lens). The live check against PostgreSQL 16 showed the cleared jsonb `skills` and the enum columns read back as `[]` and `""`.
- [x] [Review][Dismiss] **"New résumé in the freshly fetched list" is tested only at the cache level** (verification and acceptance lenses). The list page's own specs cover the refetch, and the browser smoke showed the new résumé in the list.
- [x] [Review][Dismiss] **Story checkboxes empty and deferred items marked RESOLVED before review** (adversarial lens). Checkboxes are filled here. Task 7 requires the RESOLVED markers in this branch.
- [x] [Review][Dismiss] **The archived 2.4 RV table still says "[Patch] Story 2.5"** (acceptance lens). BLOCK A was copied verbatim as instructed, and that resolution is accurate.
- [x] [Review][Dismiss] **Clearing salary keeps the currency** (edge-case lens). Out of scope per the Decision Record.

### File List

- `backend/src/JobNecto.Application/Resumes/UpdateResumeCommand.cs` (UPDATED: `ClearSalary` flag)
- `backend/src/JobNecto.Application/Resumes/CreateResumeCommand.cs` (UPDATED: `Title`/`Skills`/`WorkLocationType` nullable, B1; XML doc lists OnSite, Remote, Hybrid, B2)
- `backend/src/JobNecto.Application/Resumes/Validators/UpdateResumeCommandValidator.cs` (UPDATED: empty skills allowed; blank enums clear; `Salary` null when `ClearSalary`; `ClearSalary` counts as a field; B2 message)
- `backend/src/JobNecto.Application/Resumes/Validators/CreateResumeCommandValidator.cs` (UPDATED: B2 message; nullable-safe enum helper; whitespace treated as blank)
- `backend/src/JobNecto.Application/Resumes/Mappers/ResumeMappers.cs` (UPDATED: `ApplyUpdates` applies `ClearSalary` and stores `[]` skills as null; `ToEntity` stores `[]` skills as null; docs)
- `backend/tests/JobNecto.Tests/API/ResumesControllerTests.cs` (UPDATED: inverted `Update_EmptySkills_Returns400WithValidationErrors` → `Update_EmptySkills_ClearsSkills_Returns200`, since empty skills now clears; new clear, ambiguous-salary, B1 create and B2 message HTTP tests; helpers)
- `backend/tests/JobNecto.Tests/Application/Resumes/UpdateResumeCommandValidatorTests.cs` (UPDATED: inverted `Validate_EmptySkillsArray_FailsWithSkillsError` → `Validate_EmptySkillsArray_PassesAsClear`, since empty skills now clears; blank-enum, ClearSalary, skill-item and message cases)
- `backend/tests/JobNecto.Tests/Application/Resumes/CreateResumeCommandValidatorTests.cs` (UPDATED: null-fields, whitespace-enum and message cases)
- `backend/tests/JobNecto.Tests/Application/Resumes/ResumeMappersTests.cs` (UPDATED: clear-path, precedence and `ToEntity` null cases)
- `backend/tests/JobNecto.Tests/Application/Resumes/UpdateResumeCommandHandlerTests.cs` (UPDATED: clear-path handler test)
- `frontend/src/shared/api/generated/schema.ts` (UPDATED: regenerated with `npm run gen:api`)
- `frontend/src/widgets/resume-form/resume-form.diff.ts` (UPDATED: sends clear encodings; returns the command; `UpdateDiff`/`blockedClears` removed)
- `frontend/src/widgets/resume-form/resume-form.diff.spec.ts` (UPDATED: "clearing optional fields" block and new cases)
- `frontend/src/widgets/resume-form/resume-form.ts` (UPDATED: blocked-clear message, map and conditions removed; "Must be a number." message)
- `frontend/src/widgets/resume-form/resume-form.spec.ts` (UPDATED: blocked-clear assertions replaced by clear-encoding specs; title-only create body; unparseable-salary spec; new-baseline spec)
- `frontend/src/widgets/resume-form/resume-form.validators.ts` (UPDATED: non-finite salary reports `{ number: true }`)
- `frontend/src/widgets/resume-form/resume-form.validators.spec.ts` (UPDATED: NaN/Infinity expect `{ number: true }`)
- `frontend/src/shared/ui/form/text-field.ts` (UPDATED: emits NaN for `validity.badInput` on number inputs)
- `frontend/src/shared/ui/form/text-field.spec.ts` (UPDATED: bad-input spec)
- `frontend/src/pages/resume-create/resume-create.page.ts` (UPDATED: navigates to `/resumes` and toasts afterwards; `submitting` held until the navigation settles; refused or rejected navigation falls back)
- `frontend/src/pages/resume-create/resume-create.page.spec.ts` (UPDATED: rewritten on `RouterTestingHarness` with a `resumes` stub route)
- `frontend/src/pages/resume-detail/resume-detail.page.ts` (UPDATED: RV-101 #2 `openDelete` guard and `confirmingDelete` reset; RV-101 #3 rejection handler)
- `frontend/src/pages/resume-detail/resume-detail.page.html` (UPDATED: Delete `aria-disabled` is `saving() || deleting()`)
- `frontend/src/pages/resume-detail/resume-detail.page.spec.ts` (UPDATED: end-to-end clear spec; RV-101 #2 and #3 specs)
- `_bmad-output/implementation-artifacts/2-5-clear-optional-resume-fields.md` (UPDATED: Task 6b, AC5 rejected-navigation clause, Dev Agent Record, Status: review)
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (UPDATED: `2-5…: review`; `epic-5` pre-req comment)
- `_bmad-output/implementation-artifacts/deferred-work.md` (UPDATED: B1, B2, 2.3 "cannot clear" and 2.2 "stays on /resumes/new" marked RESOLVED; RV-101 BLOCK B; code-review-of-2-5 deferrals)
- `_bmad-output/archive/implementation-artifacts/2-4-soft-delete-a-resume.md` (UPDATED: `## PR Review (RV)` BLOCK A from the retroactive PR #101 review)

## PR Review (RV)

<!-- Filled by jobnecto-qa / bmad-code-review before merge (AGENTS.md completion checklist item 5). Not a self-review. -->

## Change Log

- 2026-10-26: Story created (ready-for-dev) from the Epic 2 retrospective. User decisions: Story 2.5 inside Epic 2; `clearSalary` flag; land on `/resumes` after create.
- 2026-10-27: Added AC6 (create with optional fields omitted, B1) and AC7 (accurate work-location message, B2) from the Epic 2 browser pass (user decision). Tasks 1b and 3b, Trap 5b, and the AC6 smoke step were added.
- 2026-10-27: added RV-101 #2/#3 fixes (retroactive PR #101 review)
- 2026-10-27: Implemented (Tasks 0-8 and 6b); self-review patches applied (unparseable salary is an error, not a clear; create stores `[]` skills as null; whitespace enums blank on create). Status: review. Task 9 (independent RV) pending.
