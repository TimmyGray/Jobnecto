# Story 2.1: Create a résumé

Status: review

## Story

As a **user**,
I want **to create a résumé**,
so that **I have material that generation can ground a cover letter in**.

<!-- Track: [FE] (+ one small backend OpenAPI-generation fix, see Task 1) — Covers: FR11, NFR2, UX-DR13, UX-DR15, AR11, AR16, AR17 -->

## Decision Record

- 2026-09-28: Cite **NFR2** (FluentValidation pipeline / field-level Problem Details) for AC3's "inline errors, no field-shake" rule instead of the **NFR10** epics.md literally cites — NFR10 is the pagination contract and is unrelated. This mislabel repeats elsewhere in epics.md (e.g. Story 1.4's equivalent AC), so it looks systemic; flagging here rather than silently editing epics.md.
- 2026-09-28: Build `SelectField<T>`, `TagInput`, and a `Toast` primitive as thin wrappers over native HTML form elements (a native `<select>`, including `multiple`, for `SelectField<T>`), matching `TextFieldComponent`'s actual pattern — not by installing spartan-ng now. (`TextFieldComponent`'s own doc comment says "no spartan-ng yet," but it's built on a plain native `<input>`, not `@angular/cdk` primitives — no CDK import exists in that file. Corrected here from this story's initial wording, which called it "raw Angular CDK.") The architecture ratified spartan-ng for these (Decision 1.4); that install is deferred again, to keep this story's footprint to its own files. A native `<select>` is also simpler and more accessible out of the box than a hand-built CDK listbox.
- 2026-09-28: Fold a small backend fix into this story's scope (Task 1): the generated OpenAPI schema types `WorkLocationType`, `Currency`, `Location`, `Language`, `LanguageLevel` as bare `number` with no member names, because `AddOpenApi()`'s schema generator reads `Microsoft.AspNetCore.Http.Json.JsonOptions`, not the `Microsoft.AspNetCore.Mvc.JsonOptions` that `Program.cs:17` actually configures — two distinct options types. Fixing this (rather than hardcoding option lists in the frontend) is what makes AC1's "never hardcoded" literally true for those five. `Experience` is never exposed as an actual enum-typed property anywhere in the API (always plain `string`), so no fix applies to it — verified during implementation, not assumed at planning time. This turns Story 2.1 from pure-FE into FE + one small backend config change + a schema regen.
- 2026-09-28: The six enum option arrays are hand-declared in the frontend. Five (`WorkLocationType`, `Currency`, `Location`, `Language`, `LanguageLevel`) are type-tethered to the generated schema via `satisfies <GeneratedUnionType>[]`, so a backend enum member add/remove/rename fails the FE build until updated — TypeScript types don't exist at runtime, so a literal array is unavoidable; the `satisfies` tether is the closest achievable meaning of "generated from backend enums, never hardcoded." `Experience`'s array is `satisfies string[]` only (no generated union exists to tether to). Labels are auto-derived by a generic PascalCase humanizer rather than 160 hand-authored strings (122 `Location` + 38 `Language` members).
- 2026-09-28: `Locations` (a preferred-countries list on the résumé) uses `SelectField<T>` in a new `multiple` mode, not a separate multi-select component — UX-DR13's named component inventory lists only `SelectField<T>`, no multi-select variant.
- 2026-09-28: `Projects`, `Certifications`, and `ExcludedWords` (nullable string-array fields on `CreateResumeCommand`) are omitted from this story's form. AC1 doesn't name them, FR11 doesn't require them, and the fields are nullable, so omitting them is non-breaking. Revisit if a later story's AC calls for them.
- 2026-09-28: The toast host is mounted locally inside the résumé-create page, not globally in `AppShellComponent` — keeps this story's change scoped to its own files. Promote to shell-level chrome once a second page needs a toast.
- 2026-09-28: On a successful create, the page resets the form and stays on `/resumes/new` (the toast confirms success) rather than navigating to `/resumes` — that route still resolves to `ComingSoonStubPage` until Story 2.2 ships, so auto-navigating there would dead-end a just-succeeded action into a stub. Staying put also lets the user create a second résumé immediately.
- 2026-09-28: `frontend/src/shared/api/generated/schema.ts`'s regen (Task 1) also added `SignInCommand`/`SignInResponse`/`POST /api/v1/users/sessions` — unrelated to this story's own scope, but verified as a beneficial side effect, not a regression: `entities/user/model.ts:23-32` hand-writes `SignInCommand`/`SignInResult` with an explicit standing TODO ("replace with generated `components['schemas'][...]` aliases once `gen:api` is re-run against a backend that has merged Story 1.2") — that regen is exactly what Task 1 just did. Nothing in this diff switches those call sites over (out of scope, a drive-by unrelated to résumé creation), but the TODO is now mechanically unblocked. Logged to `deferred-work.md` as a pickup for whichever story next touches `entities/user`.

## Acceptance Criteria

1. **Résumé create form uses branded, enum-driven controls**
   **Given** I am on the résumé create form
   **When** I fill title, skills, experience, work-location preference, salary, currency, preferred locations, and languages
   **Then** the form uses `ResumeForm` composed of `TextField` / `TagInput` / `SelectField<T>`, whose enum options are generated from the backend (`WorkLocationType`, `Experience`, `Currency`, `Language`, `LanguageLevel`, `Location`) — never hardcoded (UX-DR13, AR17)
   **And** client validation mirrors the backend rules (title ≤500 when present; each skill non-empty and ≤30 chars; salary ≥0; enum fields valid) before submit

2. **Successful submit creates the résumé**
   **Given** a valid form
   **When** I submit
   **Then** the client calls `POST /api/v1/resumes` and on `201` shows a success toast (`aria-live="polite"`) and invalidates the cached résumé list so it is available on next fetch (FR11)

3. **Server-side validation failure renders inline**
   **Given** validation fails server-side
   **When** `400` returns
   **Then** field errors render inline tied via `aria-describedby`, with an `aria-live` summary for anything unmapped; no field-shake (UX-DR15, NFR2)

## Tasks / Subtasks

- [x] Task 1: Fix OpenAPI enum schema generation so backend enums serialize as strings (AC: 1)
  - [x] `backend/src/JobNecto.API/Infrastructure/OpenApiCollectionExtensions.cs` (or `Program.cs`) — confirmed root cause: `AddOpenApi()`'s schema generator reads `Microsoft.AspNetCore.Http.Json.JsonOptions` (the general HTTP JSON options), not `Microsoft.AspNetCore.Mvc.JsonOptions` (what `Program.cs:17`'s `AddJsonOptions` actually configures) — two distinct options types, only one of which the document generator consults. Fix: `services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()))` (equivalent to `ConfigureHttpJsonOptions`) so `WorkLocationType`, `Currency`, `Location`, `Language`, `LanguageLevel` render as `"type": "string"` with an `"enum"` member list in `GET /openapi/v1.json`, not `"type": "integer"` with no members. **`Experience` is excluded** — verified it is never exposed as an actual `Experience`-typed property anywhere in the API (`CreateResumeCommand.Experience`, `UpdateResumeCommand.Experience`, and `ResumeResult.Experience` are all plain `string`/`string?`; only `ResumeMappers.cs` parses it internally), so it has no named OpenAPI schema today and this fix cannot create one — its generated TS type stays `string | null` regardless.
  - [x] `backend/tests/JobNecto.Tests/API/OpenApiSchemaTests.cs` (CREATE) — integration test mirroring `CorsTests.cs`'s `WebApplicationFactory<ApiAssemblyMarker>` pattern (`UseEnvironment("Development")`, hits `/openapi/v1.json`), asserting each of the **five** enum schemas (`WorkLocationType`, `Currency`, `Location`, `Language`, `LanguageLevel`) is string-typed with the expected member set (verified against the six backend enum files — `Experience` is a member source but not a schema-fix target). Red confirmed pre-fix (type mismatch, then a missing-`enum`-key failure once `type` started passing but the exporter's actual shape turned out to omit an explicit `"type"` field — test corrected to assert on `enum` member `ValueKind`, not a `type` property that the .NET exporter doesn't emit for string-converted enums). Green post-fix, all 6 facts passing. `Location`'s real member count is **122**, not the 127 estimated at planning time — corrected in the test and this story once counted directly from `Location.cs`.
  - [x] Run the backend locally (`dotnet run --project backend/src/JobNecto.API`), then from `frontend/`: `npm run gen:api` to regenerate `frontend/src/shared/api/generated/schema.ts` against the fixed document; diff to confirm the six types are now string literal unions instead of `number`. Confirmed: `Currency`, `Language`, `LanguageLevel`, `Location`, `WorkLocationType` are now string literal unions; `Experience` unchanged (`string | null`, as expected — no named schema exists for it).

- [x] Task 2: Add the `create()` call and `CreateResumeCommand` type to the résumé entity (AC: 2, 3)
  - [x] `frontend/src/entities/resume/model.ts` — add `export type CreateResumeCommand = components['schemas']['CreateResumeCommand'];`
  - [x] `frontend/src/entities/resume/resume.service.ts` — add `create(command: CreateResumeCommand): Observable<ResumeResult>`, POSTing to `'/resumes'` (relative — the interceptor prefixes the base and carries the auth cookie) and, on success, calling `this.invalidate()` so the next `list()` refetches rather than serving the stale cached page.
  - [x] `frontend/src/entities/resume/resume.service.spec.ts` — add cases: a successful `create()` invalidates `page()` (assert a following `list()` call hits the network, not the cache); a `400` propagates the raw `ProblemDetails` error untouched. Red confirmed (compile errors: no `create` member, no `CreateResumeCommand` export), then green — 10/10 passing.

- [x] Task 3: Define the résumé enum option constants (AC: 1)
  - [x] `frontend/src/entities/resume/resume-enums.ts` (CREATE) — for each of `WorkLocationType`, `Experience`, `Currency`, `Language`, `LanguageLevel`, `Location`: a literal array of every member (verified against `backend/src/JobNecto.Domain/Enums/{WorkLocationType,Experience,Currency,Language,Location}.cs`). For the five schemas Task 1 fixes (`WorkLocationType`, `Currency`, `Language`, `LanguageLevel`, `Location`), declare the array `as const satisfies <GeneratedUnionType>[]` against the Task-1-regenerated schema type, so an unmatched backend enum member fails the build. `Experience` has no generated union to tether to (Task 1 note) — declare it `as const satisfies string[]` instead, and leave a comment explaining why it's the odd one out. Include a shared `humanizeEnumMember(value: string): string` (insert a space before each capital that follows a lowercase letter, e.g. `OnSite` → `On Site`, `LessThanOneYear` → `Less Than One Year`; leaves all-caps codes like `USD` untouched) used to derive every option's display label.
  - [x] `frontend/src/entities/resume/resume-enums.spec.ts` (CREATE) — asserts each array's member count matches the current backend enum file (locks in a regression guard) and spot-checks a handful of humanized labels. Red (module not found) then green, 9/9 passing, 100% coverage. `Location`'s count corrected to 122 here too (Task 1 note).

- [x] Task 4: Build `SelectField<T>` (single + multi mode) (AC: 1)
  - [x] `frontend/src/shared/ui/form/select-field.ts` (CREATE) — generic `SelectFieldComponent<T extends string>`, `ControlValueAccessor`, `ChangeDetectionStrategy.OnPush`, inline template over a native `<select>` (`multiple` attribute for multi mode), matching `TextFieldComponent`'s label/`for`/`id`, `aria-describedby`, `aria-invalid`, `aria-live="polite"` error-region pattern. `options = input.required<SelectOption<T>[]>()`, `multiple = input<boolean>(false)`; value is `T | ''` in single mode, `T[]` in multi mode.
  - [x] `frontend/src/shared/ui/form/select-field.spec.ts` (CREATE) — red (module not found) then green, 13/13 passing, 86.25% line coverage.
  - [x] `frontend/src/shared/ui/index.ts` — export `SelectFieldComponent`.

- [x] Task 5: Build `TagInput` (AC: 1)
  - [x] `frontend/src/shared/ui/form/tag-input.ts` (CREATE) — `TagInputComponent`, `ControlValueAccessor` over `string[]`, `ChangeDetectionStrategy.OnPush`. Enter or comma adds the current text as a tag; Backspace on an empty text box removes the last tag; every tag has an explicit, keyboard-operable remove button (UX-DR13, `ux-design-specification.md:633`). Each candidate tag is checked against the mirrored rule (non-empty, ≤30 chars) before being added; a rejected tag shows inline feedback without clearing the input.
  - [x] `frontend/src/shared/ui/form/tag-input.spec.ts` (CREATE) — red (module not found), then one real failure surfaced and fixed: the test dispatched `input` + `keydown` synchronously with no `detectChanges()` between them, so Angular's property-binding cache never saw the intermediate value and skipped resetting `textbox.value` — fixed by adding the intervening `detectChanges()` call (matches real per-keystroke event timing; not a component defect). Green after, 12/12 passing, 88.29% line coverage.
  - [x] `frontend/src/shared/ui/index.ts` — export `TagInputComponent`.

- [x] Task 6: Build the toast primitive (AC: 2)
  - [x] `frontend/src/shared/ui/feedback/toast.service.ts` (CREATE) — `ToastService` (`providedIn: 'root'`), signal-backed list of `{ id, message, tone: 'success' | 'error' }`; `show(message, tone)` appends and auto-dismisses after a fixed delay; `dismiss(id)` removes early.
  - [x] `frontend/src/shared/ui/feedback/toast.ts` (CREATE) — `ToastHostComponent`: renders the service's active toasts in an `aria-live="polite"` region (`role="status"`), styled from `color.status.success` / `color.status.danger` tokens (`z.toast` for stacking), each with a manual close button.
  - [x] `frontend/src/shared/ui/feedback/toast.service.spec.ts`, `frontend/src/shared/ui/feedback/toast.spec.ts` (CREATE) — red (module not found) then green, 11/11 passing, 100% coverage.
  - [x] `frontend/src/shared/ui/index.ts` — export `ToastService`, `ToastHostComponent`.

- [x] Task 7: Build the `ResumeForm` widget (AC: 1, 3)
  - [x] `frontend/src/widgets/resume-form/resume-form.validators.ts` (CREATE) — client validators mirroring `CreateResumeCommandValidator` exactly: title `maxLength(500)` only when non-empty; salary `>= 0` when present; `workLocationType`/`experience`/`currency` membership checked against the Task 3 constant arrays (case-insensitive, matching the backend's `Enum.TryParse(..., true, ...)`). (Skills' non-empty/≤30 rule lives in `TagInputComponent` itself, at entry time — Task 5.) Own spec: red then green, 11/11 passing, 100% line coverage.
  - [x] `frontend/src/widgets/resume-form/resume-form.ts` (CREATE) — typed `FormGroup` (`title`, `skills: FormControl<string[]>`, `workLocationType`, `experience`, `currency`, `salary: FormControl<number | null>`, `locations: FormControl<Location[]>`, `languages: FormArray<FormGroup<{ language; level }>>` with add/remove-row methods). `submitting = input<boolean>(false)` disables the submit button; `create = output<CreateResumeCommand>()` emits the raw command shape on a valid submit (mirrors `SignInPage`'s `canSubmit`/`errorFor` pattern). Exposes a public `applyServerErrors(problem: ProblemDetails): void` that maps `400` field errors onto controls via `control.setErrors({ server: messages[0] })` + `markAsTouched()`, falling back to a general summary banner for any unmapped field (mirrors `SignInPage.handleError`'s `400` branch), and a public `resetForm()`. Never injects `HttpClient` — the page owns the network call (feature-sliced rule; Story 1.6 Dev Notes).
  - [x] `frontend/src/widgets/resume-form/resume-form.html` (CREATE) — composes `ui-text-field` (title, salary), `ui-tag-input` (skills), `ui-select-field` (workLocationType, experience, currency, locations in `multiple` mode), a repeatable language-proficiency row (two `ui-select-field` + add/remove buttons) bound to the `languages` `FormArray`, and an `aria-live="polite"` error-summary region above the submit button (AC3).
  - [x] `frontend/src/widgets/resume-form/index.ts` (CREATE) — barrel export.
  - [x] `frontend/src/widgets/resume-form/resume-form.spec.ts` (CREATE) — red (module not found) then green, 14/14 passing; `resume-form.ts` 89% / `resume-form.html` 98-100% line coverage once a DOM-driven add/remove-language-row test was added (the first pass drove those two actions through the component instance directly, which never rendered the template's per-row block).

- [x] Task 8: Compose the résumé-create page and route (AC: 2, 3)
  - [x] `frontend/src/pages/resume-create/resume-create.page.ts` (CREATE) — injects `ResumeService`, `ToastService`; renders `ui-page-header` + `widget-resume-form`. On `(create)`: calls `resumeService.create(command)`; on success shows a success toast and resets the form to pristine/empty (Decision Record); on a `400` `ProblemDetails`, calls `resumeForm.applyServerErrors(problem)`; on any other error, shows an error toast. Uses signal-based `viewChild.required(ResumeFormComponent)`.
  - [x] `frontend/src/pages/resume-create/resume-create.page.html` (CREATE)
  - [x] `frontend/src/pages/resume-create/resume-create.page.spec.ts` (CREATE) — red (module not found), then one real failure surfaced and fixed: the test's own `expectOne` matched `/resumes` by exact URL, which doesn't match `list()`'s querystring request (the exact Story 1.6 Trap 5 gotcha) — fixed with a predicate matcher. Green after, 5/5 passing, page itself 95.23% statement / 100% line coverage.
  - [x] `frontend/src/app/app.routes.ts` — inserted a `resumes/new` route inside the guarded shell's `children`, before the existing `resumes/:id` entry (so `:id` doesn't swallow the literal segment `new`), `loadComponent` → `ResumeCreatePage`, `data: { title: 'Create resume' }`.

- [x] Task 9: Verification and test coverage (AC: all)
  - [x] Unit tests: `select-field.spec.ts`, `tag-input.spec.ts`, `toast.spec.ts`, `toast.service.spec.ts`, `resume-enums.spec.ts`, `resume.service.spec.ts`, `resume-form.spec.ts` — cover enum options populating from the Task 3 constants; `TagInput` add/remove + the 30-char rejection path; salary negative rejection; submit disabled until dirty+valid; `400` mapping onto the right control plus the unmapped-field fallback banner (a single mapped field, multiple fields, and one unrecognized field, per AC3).
  - [x] Integration/API tests: `backend/tests/JobNecto.Tests/API/OpenApiSchemaTests.cs` (Task 1).
  - [x] Frontend tests: `resume-create.page.spec.ts` — `201` shows the toast, resets the form, and a subsequent `list()` call hits the network (not the stale cache); `400` forwards to `applyServerErrors`; a network/500 error shows an error toast.
  - [x] `dotnet test backend/JobNecto.slnx` — 632/632 passing.
  - [x] `dotnet build backend/JobNecto.slnx --configuration Release --warnaserror` — 0 warnings, 0 errors.
  - [x] Coverage gate ≥80% per file: `cd frontend && npx ng test --no-watch` — 316/316 passing, 96.17% statements / 97.1% lines overall, no per-file threshold failures. Backend: `dotnet test --collect:"XPlat Code Coverage" --settings coverlet.runsettings` + `python scripts/check_coverage.py ./coverage/backend --threshold 80` — 96.3% across 97 files, all pass.

## Dev Notes

### Technical Requirements
- Angular Signals + injectable services, no NgRx, no TanStack (AR11). Typed Reactive Forms (`FormGroup`/`FormControl<T>`/`FormArray`), validate on blur + submit, submit disabled until dirty + valid (AR11).
- Feature-sliced boundaries are load-bearing, not decorative: `shared/ui` = generic primitives (no domain knowledge), `entities/resume` = API-bound résumé state, `widgets/resume-form` = composed domain UI (owns the form, never calls `HttpClient`), `pages/resume-create` = route composition (owns the HTTP call + toast). This split was established in Story 1.6's Dev Notes and is being reused here, not invented.
- Every new component: `ChangeDetectionStrategy.OnPush`, Tailwind classes mapped to Career-OS tokens only — never raw hex/px (`shared/config/tokens.ts`).

### API / Contract Guardrail
- `POST /api/v1/resumes` already exists and is stable: `[Authorize]`, `[ProducesResponseType(ResumeResult, 201)]` / `400` / `401` (`backend/src/JobNecto.API/Controllers/ResumesController.cs:31-55`). No new endpoint, no ownership/403-vs-404 branch (creates don't have one) — `authorization-contract-matrix.md` doesn't need updating for this story.
- Wire values for `workLocationType`/`currency`/`experience` are plain strings already, both in `CreateResumeCommand` and in the generated schema (`schema.ts:1866-1879`) — no gotcha there. The gotcha is only in `Location`/`Language`/`LanguageLevel` (see Trap 1 below), fixed by Task 1.

### File Structure Requirements
- `backend/src/JobNecto.API/Infrastructure/OpenApiCollectionExtensions.cs` (UPDATE)
- `backend/tests/JobNecto.Tests/API/OpenApiSchemaTests.cs` (CREATE)
- `frontend/src/shared/api/generated/schema.ts` (regenerated, not hand-edited)
- `frontend/src/entities/resume/model.ts` (UPDATE)
- `frontend/src/entities/resume/resume.service.ts` (UPDATE)
- `frontend/src/entities/resume/resume.service.spec.ts` (UPDATE)
- `frontend/src/entities/resume/resume-enums.ts` (CREATE)
- `frontend/src/entities/resume/resume-enums.spec.ts` (CREATE)
- `frontend/src/shared/ui/form/select-field.ts` (CREATE)
- `frontend/src/shared/ui/form/select-field.spec.ts` (CREATE)
- `frontend/src/shared/ui/form/tag-input.ts` (CREATE)
- `frontend/src/shared/ui/form/tag-input.spec.ts` (CREATE)
- `frontend/src/shared/ui/feedback/toast.service.ts` (CREATE)
- `frontend/src/shared/ui/feedback/toast.ts` (CREATE)
- `frontend/src/shared/ui/feedback/toast.service.spec.ts` (CREATE)
- `frontend/src/shared/ui/feedback/toast.spec.ts` (CREATE)
- `frontend/src/shared/ui/index.ts` (UPDATE)
- `frontend/src/widgets/resume-form/resume-form.ts` (CREATE)
- `frontend/src/widgets/resume-form/resume-form.html` (CREATE)
- `frontend/src/widgets/resume-form/resume-form.validators.ts` (CREATE)
- `frontend/src/widgets/resume-form/resume-form.spec.ts` (CREATE)
- `frontend/src/widgets/resume-form/index.ts` (CREATE)
- `frontend/src/pages/resume-create/resume-create.page.ts` (CREATE)
- `frontend/src/pages/resume-create/resume-create.page.html` (CREATE)
- `frontend/src/pages/resume-create/resume-create.page.spec.ts` (CREATE)
- `frontend/src/app/app.routes.ts` (UPDATE)

### Testing Requirements
- Backend: fresh `WebApplicationFactory<ApiAssemblyMarker>` per test class, `UseEnvironment("Development")` to expose `/openapi/v1.json` (`CorsTests.cs` pattern).
- Frontend: `HttpTestingController` for service specs; `httpMock.expectOne('/resumes')` is fine for `create()` (no query params), but if a spec needs to assert the exact POST body, match by predicate rather than URL string alone (Story 1.6 Trap 5).
- Pure-type files are excluded from the coverage gate in `frontend/angular.json`'s `coverageExclude` (`entities/resume/model.ts` already is) — `resume-enums.ts` is **not** a pure-type file (it has runtime arrays + `humanizeEnumMember`) and must stay covered, not added to that list.

### Previous Story Intelligence (1.6 — the most recent FE story; Epic 2 has no prior story)
- Feature-sliced placement is mandatory, not a suggestion: a widget must not call `HttpClient` directly (carried into Task 7 above).
- `ChangeDetectionStrategy.OnPush` on every new component.
- Tokens only, never raw hex/px.
- Generated numeric-looking fields (`totalCount`, etc.) are typed `number | string` — coerce with `Number(...)` at the boundary. Not directly hit by this story's fields, but the same OpenAPI-generator looseness is the root cause of this story's Trap 1 below.
- `entities/resume/model.ts` is coverage-excluded as a pure-type file — same treatment is **not** needed for `resume-enums.ts` (see Testing Requirements).
- Deferred item (Story 1.6 PR review, 2026-09-27): a résumé with no `id` links to `/resumes/undefined` in `widgets/recent-resumes/recent-resumes.ts:58` — not this story's bug (that's the list/link widget, not create), but be aware a résumé created here has an `id` from the `201` response so it won't itself trigger that path.
- Deferred item (Story 1.6 PR review): `PageCursor` is typed non-nullable against a nullable generated contract (`shared/api/pagination.ts:11-14`) — explicitly flagged as relevant to Story 2.2 ("Browse my résumés"), not this story.

### Git Intelligence Summary
- The entire backend Epic 2 (résumé CRUD) shipped in an earlier backend-only phase: `5e6cc87 feat(resumes): merge Story 2.1 create resume`, `417450d` (2.2 list), `90fd5c1` (2.3 detail), `d1a2a76` (2.4 update), `70fb61b` (2.5 soft-delete) — all ancestors of current `master`. This new "Story 2.1 [FE track]" is the Angular counterpart layered on top of that already-shipped, already-tested backend, not a from-scratch feature.
- Most recent commit: `553ee7a docs: post-merge updates for Story 1.6` (2026-09-27, PR #94) — the FE conventions it established (feature-sliced layout, `TextFieldComponent`, `SignInPage`'s 400-handling pattern) are this story's direct precedent.

### Latest Stack Information
- Angular `^21.2.0` (standalone components, Signals), RxJS `~7.8.0`, TypeScript `~5.9.2`, Tailwind `^3.4.17`.
- Testing: **Vitest** `^4.0.8` via `@angular/build:unit-test` (not Karma) — run with `npx ng test --no-watch`.
- Forms: typed Reactive Forms only, no third-party forms library.
- Backend: `Microsoft.AspNetCore.OpenApi 10.0.0` (native OpenAPI generator, not Swashbuckle-driven for the document itself — Swashbuckle is present as a package reference but `Program.cs` wires up `AddApiOpenApi()` from `OpenApiCollectionExtensions.cs`, the native generator).

### ⚠️ Trap 1 — The generated schema's enum types are opaque numbers, not string unions
`Currency: number`, `Language: number`, `LanguageLevel: number`, `Location: number`, `WorkLocationType: number` in `schema.ts` (pre-Task-1) carry **zero** member information — not even a comment (confirmed by running the API and inspecting `/openapi/v1.json` directly: `{"type":"integer"}`, no `enum` array at all). Root cause: ASP.NET Core has two distinct `JsonOptions` types — `Microsoft.AspNetCore.Mvc.JsonOptions` (what `Program.cs:17`'s `AddControllers().AddJsonOptions(...)` configures, and what controllers actually serialize with) and `Microsoft.AspNetCore.Http.Json.JsonOptions` (what `AddOpenApi()`'s schema generator reads). The `JsonStringEnumConverter` was only ever added to the former. Do not trust the pre-fix generated type for these five schemas, and do not skip Task 1 by hand-writing the option lists against guessed member names — verify every member against the actual backend enum files (`backend/src/JobNecto.Domain/Enums/*.cs`) regardless, since Task 1's regen is the mechanism, not a substitute for reading the source of truth. `Experience` looks similar in `schema.ts` (`string | null`, no union) but for an unrelated reason — see Task 1 and Task 3's notes — and is unaffected by this fix.

### ⚠️ Trap 2 — `CreateResumeCommand.salary` is typed `null | number | string`
Same OpenAPI looseness pattern as Story 1.6's `totalCount` trap, but on a request-side field this time. It doesn't need coercion on the way out (the form control produces a real `number | null`, which satisfies the union trivially) — just don't be surprised by the union type when reading the generated schema, and don't add unnecessary `Number(...)` coercion on a value you already control.

### References
- [Source: `_bmad-output/planning-artifacts/epics.md` - Story 2.1, lines 415-434]
- [Source: `_bmad-output/planning-artifacts/prd-demo-mvp.md:331` - FR11]
- [Source: `_bmad-output/planning-artifacts/epics/requirements-inventory.md:37,45` - NFR2, NFR10]
- [Source: `_bmad-output/planning-artifacts/architecture/demo-mvp-architecture-decisions.md` - AR11, AR16, AR17, Decision 1.1, Decision 1.4]
- [Source: `_bmad-output/planning-artifacts/architecture/core-architectural-decisions.md` - CQRS/MediatR/FluentValidation pattern]
- [Source: `_bmad-output/planning-artifacts/ux-design-specification.md:378-380,522-524,621-623,633` - UX-DR13, UX-DR15, component inventory, toast/a11y rules]
- [Source: `backend/src/JobNecto.API/Controllers/ResumesController.cs:31-55`]
- [Source: `backend/src/JobNecto.Application/Resumes/CreateResumeCommand.cs`]
- [Source: `backend/src/JobNecto.Application/Resumes/Validators/CreateResumeCommandValidator.cs`]
- [Source: `backend/src/JobNecto.Domain/Enums/{WorkLocationType,Experience,Currency,Language,Location}.cs`]
- [Source: `backend/src/JobNecto.API/Infrastructure/OpenApiCollectionExtensions.cs`]
- [Source: `backend/tests/JobNecto.Tests/API/CorsTests.cs` - `WebApplicationFactory` test pattern]
- [Source: `frontend/src/entities/resume/resume.service.ts`, `model.ts`]
- [Source: `frontend/src/shared/ui/form/text-field.ts` - CVA component pattern]
- [Source: `frontend/src/pages/auth-sign-in/sign-in.page.ts` - reactive form + 400-mapping pattern]
- [Source: `frontend/src/shared/api/problem-details.ts`, `pagination.ts`]
- [Source: `frontend/src/shared/config/tokens.ts:17-51,127-133` - color/z-index tokens]
- [Source: `_bmad-output/archive/implementation-artifacts/1-6-orientation-dashboard.md` - previous-story intelligence]
- [Source: `_bmad-output/implementation-artifacts/deferred-work.md` - open items touching `entities/resume`]

## Dev Agent Record

### Agent Model Used

Sonnet 5 (claude-sonnet-5)

### Debug Log References

- Baseline revision: `553ee7a56996447f217a798beca01e2f83b41b82` (branch `story/2-1-create-a-resume`)
- Backend: `dotnet build backend/JobNecto.slnx --configuration Release --warnaserror` → `Build succeeded. 0 Warning(s) 0 Error(s)`. `dotnet test backend/JobNecto.slnx --configuration Release --no-build` → `Passed! - Failed: 0, Passed: 632, Skipped: 0, Total: 632`. Coverage: `python scripts/check_coverage.py ./backend/coverage/backend --threshold 80` → `Coverage: 1873/1945 lines = 96.3% across 97 files ... PASS`.
- Frontend: `cd frontend && npx ng test --no-watch` (full suite, post-self-review-fixes) → `Test Files 33 passed (33)`, `Tests 328 passed (328)`, coverage `96.3% stmts / 97.25% lines`, no per-file threshold failures.
- Post-PR-review-fixes (mandatory `bmad-code-review` RV round): backend unaffected (frontend-only fixes) — `dotnet build --configuration Release --warnaserror` and `dotnet test` re-confirmed clean (632/632). Frontend: `cd frontend && npx ng test --no-watch` → `Test Files 33 passed (33)`, `Tests 344 passed (344)`, coverage `96.43% stmts / 97.37% lines`, no per-file threshold failures.

### Completion Notes List

- Task 1 (backend OpenAPI enum fix) turned out to apply to only 5 of the 6 enums, not all 6 as originally scoped — `Experience` is never exposed as an actual enum-typed property anywhere in the API (always plain `string`), verified by running the API and inspecting `/openapi/v1.json` directly. Root cause pinned down precisely: `AddOpenApi()`'s schema generator reads `Microsoft.AspNetCore.Http.Json.JsonOptions`, not the `Microsoft.AspNetCore.Mvc.JsonOptions` that `Program.cs:17` configures — two distinct ASP.NET Core options types. See Change Log and the story's Decision Record for the full correction trail.
- `Location`'s real member count is 122, not the 127 estimated during planning — corrected everywhere it was cited (test, `resume-enums.ts` comment, Decision Record) once counted directly from `backend/src/JobNecto.Domain/Enums/Location.cs`.
- Self-review (4 parallel lenses: adversarial, edge-case, verification-gap, acceptance) surfaced 7 real defects, all patched before marking this story `review` — see `### Review Findings` below. The most significant: salary was silently submitted as a JSON string instead of a number (a `TextFieldComponent`/typed-`FormControl` type-safety hole that would have broken every salary submission), and server-side skill validation errors could never map inline because FluentValidation's `RuleForEach` emits indexed keys (`"Skills[0]"`) that the original `matchControl` string-equality check could never match.
- Three low-severity findings were logged to `deferred-work.md` rather than fixed: no dedupe on skills/duplicate language rows, `SelectFieldComponent.writeValue` not validating membership in `options()`, and a handful of test-coverage gaps (route-ordering test, `clearServerErrors()` resubmit test, `required`/`hint` input coverage on two components) that don't correspond to live bugs.

### File List

- `backend/src/JobNecto.API/Infrastructure/OpenApiCollectionExtensions.cs` (UPDATED — enum-as-string OpenAPI schema fix, Task 1)
- `backend/tests/JobNecto.Tests/API/OpenApiSchemaTests.cs` (CREATED)
- `frontend/src/shared/api/generated/schema.ts` (UPDATED — regenerated from the fixed OpenAPI document)
- `frontend/src/entities/resume/model.ts` (UPDATED — added `CreateResumeCommand`)
- `frontend/src/entities/resume/resume.service.ts` (UPDATED — added `create()`; added a generation counter to `list()`/`invalidate()` to close the cache-invalidation race found in review)
- `frontend/src/entities/resume/resume.service.spec.ts` (UPDATED — `create()` cases + the race-condition regression test)
- `frontend/src/entities/resume/resume-enums.ts` (CREATED)
- `frontend/src/entities/resume/resume-enums.spec.ts` (CREATED)
- `frontend/src/entities/resume/index.ts` (UPDATED — export `./resume-enums`)
- `frontend/src/shared/ui/form/text-field.ts` (UPDATED — coerces to a real `number | null` when `type="number"`, fixing the salary-as-string defect found in self-review; behavior for every other `type` is unchanged)
- `frontend/src/shared/ui/form/text-field.spec.ts` (UPDATED — number-mode coverage; PR-review round added a test locking in that a non-finite value like `"1e400"` is sanitized to `''` by the native input itself, verifying the NaN/Infinity concern doesn't reach this component)
- `frontend/src/shared/ui/form/select-field.ts` (CREATED; UPDATED in PR review — the empty placeholder `<option>` now has a visible "— Select —" label instead of rendering blank)
- `frontend/src/shared/ui/form/select-field.spec.ts` (CREATED; UPDATED in PR review)
- `frontend/src/shared/ui/form/tag-input.ts` (CREATED; UPDATED in self-review — `writeValue()` now resets stale local `draft`/`rejectionMessage` state; UPDATED in PR review — `onKeydown` no longer commits a tag when Enter is confirming IME composition)
- `frontend/src/shared/ui/form/tag-input.spec.ts` (CREATED; UPDATED in self-review and PR review)
- `frontend/src/shared/ui/feedback/toast.service.ts` (CREATED; UPDATED in PR review — `dismiss()` now clears the auto-dismiss timer instead of leaking it)
- `frontend/src/shared/ui/feedback/toast.ts` (CREATED)
- `frontend/src/shared/ui/feedback/toast.service.spec.ts` (CREATED; UPDATED in PR review — timer-leak regression test, and the auto-dismiss timing test now pins the exact 4-second boundary instead of merely "by 5 seconds")
- `frontend/src/shared/ui/feedback/toast.spec.ts` (CREATED)
- `frontend/src/shared/ui/index.ts` (UPDATED — export `SelectFieldComponent`, `TagInputComponent`, `ToastService`, `ToastHostComponent`)
- `frontend/src/widgets/resume-form/resume-form.ts` (CREATED; UPDATED in self-review — fixed `matchControl` to recognize FluentValidation's indexed `Skills[0]` key format, `languageRowValidator` wiring; UPDATED in PR review — `enumArrayMemberValidator` on `locations`, per-row `enumMemberValidator` on language/level, `matchControl` recognizes `Locations[n]`, a `languageRowServerErrors` field + explicit `ChangeDetectorRef.markForCheck()` for the nested-row error display, simplified the dead-code `locations` ternary in `buildCommand()`)
- `frontend/src/widgets/resume-form/resume-form.html` (CREATED; UPDATED in self-review — bound `[error]` on the skills `ui-tag-input`, incomplete-language-row message; UPDATED in PR review — `[error]` bindings on `locations` and each language row's language/level selects)
- `frontend/src/widgets/resume-form/resume-form.validators.ts` (CREATED; UPDATED in self-review — added `languageRowValidator`; UPDATED in PR review — added `enumArrayMemberValidator`, hardened `nonNegativeValidator` against `NaN`/`Infinity`)
- `frontend/src/widgets/resume-form/resume-form.validators.spec.ts` (CREATED; UPDATED in PR review)
- `frontend/src/widgets/resume-form/resume-form.spec.ts` (CREATED; UPDATED in self-review and PR review — skills/locations/languages error-mapping, multi-field, incomplete-language-row, and the DOM-level nested-row-error-survives-a-render-pass regression tests)
- `frontend/src/widgets/resume-form/index.ts` (CREATED)
- `frontend/src/pages/resume-create/resume-create.page.ts` (CREATED; UPDATED in review — added `takeUntilDestroyed()` so a late response can't act on a destroyed view)
- `frontend/src/pages/resume-create/resume-create.page.html` (CREATED)
- `frontend/src/pages/resume-create/resume-create.page.spec.ts` (CREATED; UPDATED in review — destroy-cancellation regression test; fixed a pre-existing URL-only `expectOne` that didn't account for `list()`'s querystring, the same Story 1.6 Trap 5 gotcha)
- `frontend/src/app/app.routes.ts` (UPDATED — `resumes/new` route)

### Review Findings

Four parallel lens subagents (adversarial, edge-case hunter, verification-gap, acceptance) reviewed the full diff since baseline. Each finding below was independently verified against the cited code before being patched.

- [x] [Review][Patch] **Salary was silently submitted as a JSON string, not a number.** `TextFieldComponent` is a `ControlValueAccessor<string>` — its `onInput` always forwarded the native `<input>`'s raw string value, regardless of `type`. `resume-form.ts` types `salary: FormControl<number | null>`, but Angular's `[formControl]` binding doesn't enforce a CVA's declared generic, so every salary entry actually flowed through as a string. Filling any non-empty salary and submitting would send `"salary":"50000"` to a `decimal?` backend property, almost certainly failing body-binding with a generic (non-field-mapped) 400 — a broken core happy path. Fixed in `text-field.ts`: `onInput` now coerces to `Number(value)` (or `null` when empty) specifically when `type() === 'number'`; every other `type` is byte-for-byte unchanged (confirmed `type="number"` had exactly one usage in the whole codebase before this story, so no other consumer could regress). Regression tests added in `text-field.spec.ts`.
- [x] [Review][Patch] **A server-side skill validation error could never render inline.** Two compounding bugs: (1) `resume-form.html`'s `ui-tag-input` never received `[error]="errorFor('skills')"` (every other mapped field did); (2) even after adding that, `matchControl`'s `known.includes(key)` check could never match, because FluentValidation's `RuleForEach` (the backend's actual Skills rule) emits one key per invalid element — `"Skills[0]"`, `"Skills[1]"`, … — never bare `"skills"`. A skills 400 was silently swallowed: treated as "mapped" (so `hasUnmappedField` stayed false and the general banner was suppressed too), with nothing shown anywhere — worse than falling back to the banner. Fixed both: `matchControl` now regex-matches `/^skills(\[\d+\])?$/i`, and the template binds `[error]="errorFor('skills')"`. Regression tests added covering the indexed-key format and DOM rendering.
- [x] [Review][Patch] **Cache-invalidation race between `list()` and `create()`.** `ResumeService.list()`'s `tap` unconditionally overwrote `pageSignal` with whatever it received, with no ordering check against a later `invalidate()`. If a `list()` request was in flight (e.g. the dashboard's recent-résumés widget) when the user created a résumé, `create()`'s `invalidate()` nulled the cache, but the earlier `list()`'s still-pending response could land afterward and silently resurrect the stale pre-create page — defeating AC2's "invalidates the cached list" guarantee. Fixed with a generation counter: `list()` and `invalidate()` both bump it, and a `list()` response only applies if its own captured generation is still current. Regression test added reproducing the exact race via two overlapping `HttpTestingController` requests.
- [x] [Review][Patch] **`TagInputComponent.writeValue()` didn't reset local UI-only state.** `draft` and `rejectionMessage` are local signals driven by user typing, not part of the bound `string[]` value — `writeValue()` never cleared them. Trigger: reject an over-length skill (draft text + error message linger by design), then submit successfully elsewhere in the form — `ResumeCreatePage.onCreate()` calls `resetForm()`, which clears the tags via `writeValue([])`, but the stray draft text and "1-30 characters" error stayed on screen right next to a success toast. Fixed: `writeValue()` now also resets `draft` and `rejectionMessage` (safe — `writeValue` is only invoked on an externally-driven value change, never during the component's own user-typing → `onChange` path).
- [x] [Review][Patch] **A half-filled language row was silently dropped, with no feedback.** `buildCommand()`'s `languages` mapping filters to rows where both `language` and `level` are set — a row with only `language` picked (level left blank) passed `canSubmit` (nothing validated row completeness) and was then silently omitted from the emitted command. The user would see a success toast having lost the language they picked, with zero indication. Fixed with a new `languageRowValidator()` (in `resume-form.validators.ts`) applied to each row's `FormGroup`: exactly one of language/level set is now invalid, blocking submit until the row is completed or removed, with an inline `aria-live` message.
- [x] [Review][Patch] **No cancellation of the in-flight create request on page destroy.** `ResumeCreatePage.onCreate()`'s subscription had no `takeUntilDestroyed()`. If the user navigated away from `/resumes/new` before the response arrived, a late response would still fire `toastService.show(...)` / `resumeForm().resetForm()` — and since the toast host is only mounted locally on this page (per this story's own Decision Record), that toast would be queued into the global `ToastService` signal with nowhere to render. Fixed by adding `takeUntilDestroyed(this.destroyRef)` to the subscription pipe; this also cancels the underlying HTTP request (confirmed in the regression test — flushing it after destroy now throws "Cannot flush a cancelled request", proof the subscription really tore down).
- [x] [Review][Dismiss] **Backend `services.Configure<Http.Json.JsonOptions>` affecting runtime serialization.** Adversarial lens raised this as a risk to verify, not a confirmed defect — checked and ruled out: `Program.cs` registers no minimal-API endpoints that read `Http.Json.JsonOptions` for real responses (only `MapControllers()`/`MapOpenApi()`), so the change is scoped to schema generation exactly as intended.
- [ ] [Review][Defer] No dedupe on skills or duplicate language rows; `SelectFieldComponent.writeValue` doesn't validate membership in `options()`; several test-coverage gaps (route-ordering test, `clearServerErrors()` resubmit test, `required`/`hint` input coverage on `SelectField`/`TagInput`, a toast success-tone class assertion, the `errors['enum']` message branch untested at component level) — none correspond to a live bug. Logged to `deferred-work.md` under "Deferred from: code review of 2-1-create-a-resume (2026-09-28)".

## PR Review (jobnecto-qa, RV) — 2026-09-28

Target: branch `story/2-1-create-a-resume` vs `origin/master` (`553ee7a`; all changes uncommitted at review time). Mandatory `bmad-code-review` workflow: Blind Hunter (diff-only, no project context), Edge Case Hunter (diff + full repo read access), and Acceptance Auditor (diff + this story file) ran as parallel subagents; each finding below was independently re-verified at the cited location before being triaged.

Tests: `dotnet test backend/JobNecto.slnx` → 632 passed, 0 failed. Release build → 0 warnings, 0 errors. `cd frontend && npx ng test --no-watch` → 344 passed, 0 failed, coverage 96.43% statements / 97.37% lines, no per-file threshold failures.

### 1. Locations/Languages server-error mapping — same bug class as the already-fixed Skills case, left unaddressed
- **severity:** high
- **risk_score:** 6
- **impact:** Flagged independently by two lenses (Acceptance Auditor, Blind Hunter) and confirmed unreachable *today* only because the backend validator has no rule for these two fields yet (verified against `CreateResumeCommandValidator.cs`) — the moment a future story adds one, a `Locations`/`Languages` `400` would silently vanish (mapped-but-invisible, worse than the generic-banner fallback) exactly like the pre-patch Skills bug this story's own self-review already found once.
- **evidence:** `frontend/src/widgets/resume-form/resume-form.html` (pre-fix): no `[error]` binding on the `locations` `ui-select-field` or the per-row language/level selects; `resume-form.ts`'s `matchControl`/`SimpleFieldKey` didn't recognize `Locations[n]` or the nested `Languages[n].Language`/`Languages[n].Level` key shapes FluentValidation's `RuleForEach` produces.
- **recommended_fix:** Applied — `enumArrayMemberValidator` (new) on `locations`; `enumMemberValidator` on each language row's `language`/`level` controls; `matchControl` now recognizes `Locations[n]`; `Languages[n].Language`/`Languages[n].Level` routed through a dedicated `languageRowServerErrors` store (see finding 2 for why not directly via `setErrors`); `[error]` bindings added in the template. Tests: `resume-form.spec.ts` (locations 400 mapping, nested language-row 400 mapping, out-of-range index falls back to the banner, client-side enum rejection for both).

### 2. A dynamically-added `FormArray` row's manually-`setErrors()`'d control silently loses that error on the very next render — and a plain-field fallback needed an explicit `markForCheck()` to actually render
- **severity:** high
- **risk_score:** 7
- **impact:** This is a real Angular-forms interaction, not a test artifact — proven by a DOM-level regression test that failed exactly as a real HTTP-error callback would trigger it. Without this fix, *any* server-side error mapped onto a language row (once finding 1's backend-side rule exists) would flash and vanish before the user ever saw it — worse than useless, since `applyServerErrors` would report success (no unmapped-field fallback triggers) while showing nothing.
- **evidence:** Stack-trace-confirmed root cause: `FormControlDirective.ngOnChanges` → `this.form.updateValueAndValidity({emitEvent:false})` re-runs the control's own validator (which sees the real, unchanged `''` value and returns `null`) on a render pass *after* the row was already showing, discarding the manually-set `{server: ...}` error. A second, independent issue: switching the workaround to a plain (non-signal) field still didn't render until `ChangeDetectorRef.markForCheck()` was called explicitly, since this component is `OnPush` and a bare field mutation doesn't notify the change detector.
- **recommended_fix:** Applied — `languageRowServerErrors` (a plain field, not `.errors`/not a `signal()` — a signal read from inside the `@for` row loop's own per-item template was separately verified to make `@for` treat the iterable as empty on the next render) with an explicit `this.cdr.markForCheck()` after every mutation. Regression test: `resume-form.spec.ts` "the mapped language-row error actually survives a render pass and shows up in the DOM" — asserts the real rendered DOM text, not just component state, specifically because the earlier (looks-right, is-wrong) version passed a component-level assertion while silently failing to render.

### 3. Toast auto-dismiss timer leaked on manual dismiss
- **severity:** low
- **risk_score:** 2
- **impact:** `setTimeout(() => this.dismiss(id), AUTO_DISMISS_MS)` was never cleared on an early `dismiss()` (e.g. the user clicking the close button) — harmless today (the stray timer's `dismiss()` call is a no-op filter on an already-removed id) but a real leaked timer per manually-dismissed toast, and a footgun for the next person who adds state to `dismiss()`.
- **evidence:** `frontend/src/shared/ui/feedback/toast.service.ts` (pre-fix) — no `clearTimeout`.
- **recommended_fix:** Applied — a `Map<id, timer>`; `dismiss()` clears and removes the entry. Test: `toast.service.spec.ts` "dismiss() clears the pending auto-dismiss timer instead of leaking it" (asserts `vi.getTimerCount()`).

### 4. `SelectField<T>`'s empty placeholder option rendered as an unlabeled blank row
- **severity:** low
- **risk_score:** 2
- **impact:** Real usability nit on three fields (work-location, experience, currency) — a user opening the dropdown before choosing saw a blank line, not a hint.
- **evidence:** `frontend/src/shared/ui/form/select-field.ts` (pre-fix): `<option value="" ...></option>` with no text content.
- **recommended_fix:** Applied — `<option value="" ...>— Select —</option>`. Test added in `select-field.spec.ts`.

### 5. `TagInput` committed an IME composition's Enter as a tag submission
- **severity:** low
- **risk_score:** 2
- **impact:** A CJK (or other IME) user pressing Enter to confirm character composition would have that in-progress text prematurely added as a skill tag, corrupting the intended input — an internationalization correctness gap the edge-case lens caught by tracing every keydown branch.
- **evidence:** `frontend/src/shared/ui/form/tag-input.ts` (pre-fix) `onKeydown` had no `event.isComposing` guard.
- **recommended_fix:** Applied — early-return on `event.isComposing`. Test added in `tag-input.spec.ts`.

### 6. Dead-code ternary in `buildCommand()`
- **severity:** low
- **risk_score:** 1
- **impact:** None functionally (`raw.locations.length > 0 ? raw.locations : []` is identical to `raw.locations` in every case) — pure clarity/maintainability, flagged as "looks intentional but does nothing, resolve one way or the other."
- **evidence:** `frontend/src/widgets/resume-form/resume-form.ts`.
- **recommended_fix:** Applied — simplified to `locations: raw.locations`.

### 7. NaN/Infinity via the salary number coercion — verified unreachable through the real UI, hardened anyway
- **severity:** low (downgraded from the lenses' medium/high — see verification)
- **risk_score:** 2
- **impact:** Two lenses (independently) raised this as a potential hole in `TextFieldComponent`'s `type="number"` coercion + `nonNegativeValidator`. Verified **false** for the actual component: HTML5 `<input type="number">` value-sanitization (confirmed matching in jsdom) resets any non-finite-parsing text to `''` before `onInput` ever runs — `Number(next)` can never observe `NaN`/`Infinity` from a real browser input. Locked in with a regression test. `nonNegativeValidator` itself still hardened as defense-in-depth against a non-DOM caller (e.g. a future direct `setValue(NaN)`).
- **evidence:** `frontend/src/shared/ui/form/text-field.spec.ts` — `numberInput.value = '1e400'` immediately reads back as `''`.
- **recommended_fix:** Applied to the validator only (`Number.isFinite` check added to `nonNegativeValidator`); no change needed to `TextFieldComponent` itself. Tests added in both spec files.

### 8. Stray unchecked `Task 7` checkbox despite every subtask being complete
- **severity:** low
- **risk_score:** 1
- **impact:** Internal contradiction in the story's own tracking (all 5 subtasks and their tests existed in the diff) — no code impact.
- **evidence:** `_bmad-output/implementation-artifacts/2-1-create-a-resume.md`.
- **recommended_fix:** Applied — checkbox corrected.

### 9. `schema.ts` regen also captured `SignInCommand`/`SignInResponse`/`POST /api/v1/users/sessions`
- **severity:** low (informational — not a defect)
- **risk_score:** 1
- **impact:** Blind Hunter flagged this as an unexplained, unreviewed contract change riding into the PR. Verified: `entities/user/model.ts:23-32` has a standing TODO to switch to the generated aliases once `gen:api` is re-run against a backend with Story 1.2 merged — this regen (needed for Task 1) is exactly that trigger. Nothing in this diff consumes the new types.
- **evidence:** `frontend/src/entities/user/model.ts:23-32`; `git diff` on `schema.ts` shows only additions, nothing removed/changed.
- **recommended_fix:** None needed for this story. Logged to `deferred-work.md` as a pickup (switch `entities/user` to the generated aliases) for whichever story next touches that entity.

### 10. Backend `services.Configure<Http.Json.JsonOptions>` affecting runtime serialization elsewhere
- **severity:** dismissed
- **risk_score:** —
- **impact:** Raised as a risk to verify, not a confirmed defect.
- **evidence:** `Program.cs` registers no minimal-API endpoints reading `Http.Json.JsonOptions` for real responses — only `MapControllers()`/`MapOpenApi()`. Scoped to schema generation exactly as intended.
- **recommended_fix:** None.

### 11. `skills` FormControl has no client-side validator (architectural asymmetry vs. the other five mapped fields)
- **severity:** dismissed
- **risk_score:** —
- **impact:** Intentional, not an oversight — the non-empty/≤30-char rule lives in `TagInputComponent.tryAddTag()` itself, at entry time, which is a stronger guarantee (rejects before the tag ever enters the array) than a `FormControl`-level validator would add on top.
- **evidence:** `frontend/src/shared/ui/form/tag-input.ts`; this story's Task 5.
- **recommended_fix:** None.

### 12. `OpenApiSchemaTests.cs` hardcodes a secret-shaped string
- **severity:** dismissed
- **risk_score:** —
- **impact:** Matches a pre-established, already-merged pattern, not something newly introduced.
- **evidence:** Identical shape to `backend/tests/JobNecto.Tests/API/CorsTests.cs`'s existing `JwtSettings:SecretKey` test setting, both explicitly named/commented as test-only dummies.
- **recommended_fix:** None.

### Deferred (low severity, no live-bug impact — see `deferred-work.md`)
No dedupe on skills or duplicate language rows; `SelectFieldComponent.writeValue` doesn't validate membership in `options()`; `TagInput` doesn't split a pasted string with embedded (non-trailing) commas into multiple tags; a whitespace-only rejected tag leaves invisible spaces in the box with only the error text as a cue; remaining test-coverage gaps (route-ordering test, `clearServerErrors()` resubmit test, `required`/`hint` input coverage on `SelectField`/`TagInput`, a toast success-tone class assertion, the `errors['enum']` message branch untested at component level).

## Change Log

- 2026-09-28: Story created (ready-for-dev). Sharpened AC1's field list (epics.md's original wording only named title/skills/experience/work-location/salary/currency explicitly, then separately cited Language/LanguageLevel/Location among the "never hardcoded" enums without naming the locations/languages fields themselves) to explicitly include preferred locations and languages, since those are the only command fields those three enums apply to.
- 2026-09-28: During Task 1 implementation, confirmed by running the API that `Experience` is never exposed as an actual enum-typed property anywhere in the API (always plain `string`), so it has no named OpenAPI schema and the Task 1 fix cannot apply to it. Corrected Task 1/3, Trap 1, and the two related Decision Record entries accordingly — narrowed the backend fix's scope from "six enums" to the five that actually have named schemas (`WorkLocationType`, `Currency`, `Location`, `Language`, `LanguageLevel`). Also pinned down the exact root cause (two distinct ASP.NET Core `JsonOptions` types) rather than the "investigate why" placeholder originally written.
- 2026-09-28: All tasks implemented and self-reviewed (four parallel lenses). Six real defects patched (salary-as-string, the skills server-error mapping gap, a `list()`/`create()` cache-invalidation race, stale `TagInput` UI state, a silently-dropped half-filled language row, and missing request cancellation on page destroy) — see `### Review Findings`. Three low-severity items deferred to `deferred-work.md`. Status set to `review`.
- 2026-09-28: Mandatory `bmad-code-review` PR review (RV) run against the full diff (Blind Hunter, Edge Case Hunter, Acceptance Auditor). Nine findings patched — most notably the Locations/Languages server-error mapping gap (the same class already fixed once for Skills, left incomplete) and, discovered while fixing it, a genuine Angular-forms interaction where a dynamically-added `FormArray` row's manually-set error silently vanished on the next render (needed an explicit `ChangeDetectorRef.markForCheck()`, proven with a DOM-level regression test after an earlier component-level-only test passed while the real UI stayed broken). Three findings verified as non-issues and dismissed. Three logged to `deferred-work.md`. See `## PR Review (jobnecto-qa, RV)`. Final verification: backend 632/632 (unaffected), frontend 344/344, coverage 96.43%/97.37%, no per-file gate failures.
