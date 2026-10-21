# Story 2.3: View & edit a résumé

Status: done

<!-- Merged 2026-10-21 via PR #99 (d6d13ea). Archived per AGENTS.md lifecycle. -->

## Story

As a **user**,
I want **to open a résumé and edit it**,
so that **I can keep it current**.

<!-- Track: [FE] — Covers: FR13, FR14, FR41, AR11, AR15, UX-DR12, UX-DR15 -->

## Decision Record

- 2026-10-21: **PATCH semantics are "null/omitted = leave unchanged" — and several fields cannot be cleared.** Verified in `UpdateResumeCommandValidator.cs` and `ResumeMappers.ApplyUpdates` (`ResumeMappers.cs:82+`): `null` means "not provided"; `Skills` must contain ≥1 item when provided (`[]` → 400); `Salary` is `decimal?` with no way to send "unset"; `WorkLocationType`/`Currency`/`Experience` reject `""` (400: not a valid enum), so a set enum cannot be cleared. Only `Title` (`""` is accepted and stored), `Languages` and `Locations` (arrays — `[]` replaces and clears) can be emptied. **This story is frontend-only and does not change that.** Decision: in edit mode, when the user clears a field the API cannot clear (skills, salary, currency, workLocationType, experience) that was previously set, show an inline message ("This can't be cleared yet — choose a new value or leave it as it was") and keep Save disabled; never silently drop the change. The backend gap is logged as a candidate story (see Dev Notes → *Open follow-up*), not built here.
- 2026-10-21: **Reuse and extend `widgets/resume-form` instead of forking it.** The create form (Story 2.1) already owns the typed `FormGroup`, validators, server-error mapping and the language-row `FormArray` workaround. Add an optional `resume` input (edit mode) and a second output `update` that emits an `UpdateResumeCommand` containing **only changed fields**; create mode (`create` output, `resetForm()`) must behave exactly as before — its 14+ specs are the regression guard. Do not copy the form into a second widget.
- 2026-10-21: **"Changed" means deep-different from the loaded résumé, not just `form.dirty`.** A user can edit then revert; `dirty` stays true. Save is enabled only when the computed diff is non-empty **and** the form is valid **and** not submitting (AC2, AR11). Array fields (`skills`, `locations`, `languages`) are compared by value and, when changed, sent whole (the API replaces arrays).
- 2026-10-21: **Cross-user is 404 on read, 403 on mutate — render both distinctly.** Per `authorization-contract-matrix.md`: `GET /resumes/{id}` → `404` for not-found, soft-deleted *and* cross-user (existence not disclosed, AC3); `PATCH` → `404` not-found/soft-deleted, `403` cross-user. The detail page shows `ui-not-found-state` ("back to résumés") on a `404` load; a `403`/`404` on **save** replaces the form with `ui-forbidden-state` / `ui-not-found-state` respectively (the résumé changed or vanished underneath the user) and must never echo the other user's data. Use `mapProblemToUxState()` from `@shared/api` rather than string-matching status codes.
- 2026-10-21: **`:id` that is not a GUID resolves to 404 too.** The backend route is `{id:guid}`; a non-GUID segment never matches and the interceptor yields a normalized `404`. Treat it exactly like not-found (no client-side GUID validation needed; add a spec).
- 2026-10-21: **No router `withComponentInputBinding()`** (`app.config.ts` registers plain `provideRouter(routes)`); read the id from `ActivatedRoute` (`paramMap`) and re-load when it changes (navigating `/resumes/a` → `/resumes/b` reuses the component).
- 2026-10-21: **No client-side detail cache.** `GET` is fetched on every page entry; `ResumeService` gains `get(id)` and `update(id, command)` only. `update()` calls `invalidate()` on success (the list is now stale — AC2) and returns the `ResumeResult` that the page adopts as the new baseline for diffing, so a second save sends only what changed since the first.
- 2026-10-21: **Toast host mounted locally on the page**, as in 2.1 (`ToastService` + `ui-toast-host`). Success toast "Résumé saved"; non-400 failures toast an error and keep the user's edits.
- 2026-10-21: **Out of scope:** delete (Story 2.4), an unsaved-changes navigation guard (logged to `deferred-work.md` as a candidate), and the form's omitted `projects` / `certifications` / `excludedWords` fields (same omission as 2.1; preserve them untouched — never send them).
- 2026-10-21: **`ResumeCreatePage` is unchanged** (still stays on `/resumes/new` after create) — that remains an open product call from Story 2.2; do not fold it into this story.

## Acceptance Criteria

1. **Detail loads and is editable**
   **Given** I own a résumé
   **When** I open `/resumes/:id`
   **Then** its detail loads via `GET /api/v1/resumes/{id}` into the résumé form (title, skills, experience, work-location, salary, currency, preferred locations, languages) and is editable (FR13)

2. **Only changed fields are PATCHed**
   **Given** I change fields
   **When** I save
   **Then** the client calls `PATCH /api/v1/resumes/{id}` with **only the changed fields**, Save is disabled until the form is dirty **and** valid **and** actually differs from the loaded résumé, and on `200` the list cache is invalidated and the page adopts the returned résumé as its new baseline (FR14, AR11)

3. **Not found / soft-deleted / cross-user**
   **Given** the id does not exist, is soft-deleted, belongs to another user, or is not a GUID
   **When** I open it
   **Then** I get `404` and a not-found state with a back-to-résumés CTA — cross-user existence is not disclosed (FR41, AR15)

4. **Server-side validation failure renders inline** *(carried from 2.1's AC3 for the edit path)*
   **Given** the server rejects my edit
   **When** `400` returns
   **Then** field errors render inline via the existing `applyServerErrors` mapping with an `aria-live` summary for unmapped ones; my edits are kept (UX-DR15)

5. **Mutation denied**
   **Given** the résumé is no longer mine or no longer exists when I save
   **When** the server returns `403` / `404`
   **Then** I see the forbidden / not-found state with a safe recovery CTA, never raw status codes or another user's data (FR41, AR15)

## Tasks / Subtasks

- [x] Task 0: Test-first baseline
  - [x] Confirm green before changing anything: `cd frontend && npx ng test --no-watch` (expect 429 passing at the merge of PR #96).

- [x] Task 1: Entity service `get` / `update` (AC: 1, 2, 5)
  - [x] `frontend/src/entities/resume/model.ts` (UPDATE) — `export type UpdateResumeCommand = components['schemas']['UpdateResumeCommand'];`
  - [x] `frontend/src/entities/resume/resume.service.ts` (UPDATE) — `get(id: string): Observable<ResumeResult>` (`GET /resumes/{id}`, no caching) and `update(id: string, command: UpdateResumeCommand): Observable<ResumeResult>` (`PATCH /resumes/{id}`; `tap(() => this.invalidate())` on success only). XML-doc both.
  - [x] `frontend/src/entities/resume/resume.service.spec.ts` (UPDATE) — `get` hits the right URL with credentials; `update` sends a PATCH with exactly the given body and invalidates `page()` (a following `list()` hits the network); a `400`/`403`/`404` propagates the normalized `ProblemDetails` and does **not** invalidate.

- [x] Task 2: Diff + clearing rules (pure functions) (AC: 2)
  - [x] `frontend/src/widgets/resume-form/resume-form.diff.ts` (CREATE) — `buildUpdateCommand(original: ResumeResult, current: FormValue): { command: UpdateResumeCommand; blockedClears: string[] }`: includes a field only if it deep-differs from `original`; arrays by value; `title` cleared → `''`; `languages`/`locations` cleared → `[]`; skills/salary/currency/workLocationType/experience cleared-from-set → listed in `blockedClears` and **not** in `command`. Never emits `projects`/`certifications`/`excludedWords`. XML-doc it.
  - [x] `frontend/src/widgets/resume-form/resume-form.diff.spec.ts` (CREATE) — unchanged form → empty command; one field changed → exactly that field; edit-then-revert → empty; array reordered vs. same members (order matters for skills? treat order as significant) ; each clearable/non-clearable case above; `salary` `0` is a value, not "cleared"; enum compared case-sensitively against the loaded value after the form's own normalization.

- [x] Task 3: Edit mode on `ResumeFormComponent` (AC: 1, 2, 4)
  - [x] `frontend/src/widgets/resume-form/resume-form.ts` / `.html` (UPDATE) — `resume = input<ResumeResult | null>(null)`; when set (and when it changes, e.g. after a save) patch the form from it (`reset` with values, rebuild the `languages` rows), and store it as the diff baseline. New `update = output<UpdateResumeCommand>()`. `canSubmit` in edit mode = dirty ∧ valid ∧ not submitting ∧ non-empty diff ∧ no `blockedClears`. `onSubmit()` emits `update` with only the diff; in create mode it emits `create` exactly as today. Show the blocked-clear inline message per field. Submit button label "Save changes" in edit mode, "Create résumé" otherwise. Keep `applyServerErrors`, `clearServerErrors` and the language-row workaround untouched.
  - [x] `frontend/src/widgets/resume-form/resume-form.spec.ts` (UPDATE) — add an "edit mode" describe: populates every control (including language rows and locations) from a résumé; Save disabled when untouched, enabled after a real change, disabled again after reverting; emits only the changed fields; blocked clear shows the message and keeps Save disabled; swapping the `resume` input to the saved response resets the baseline so a second save diffs against it; a `400` still maps inline in edit mode. **All existing create-mode specs must pass unmodified.**

- [x] Task 4: `ResumeDetailPage` (AC: 1, 2, 3, 4, 5)
  - [x] `frontend/src/pages/resume-detail/resume-detail.page.ts` + `.html` (CREATE) — `page-resume-detail`, `OnPush`. Reads `id` from `ActivatedRoute.paramMap` (re-loads on change); state `status: 'loading' | 'ready' | 'not-found' | 'forbidden' | 'error'`, `resume`, `saving`. `get(id)`: success → `ready`; `404` → `not-found`; `403` → `forbidden`; other → `error` with Retry. Layout: `ui-page-header` (eyebrow "Résumés", h1 = résumé title or "Untitled résumé", back link to `/resumes`), skeleton while loading, the form in edit mode when `ready`. `(update)`: `update(id, command)`; success → toast "Résumé saved", set `resume` to the response; `400` → `form.applyServerErrors`; `403` → `forbidden`, `404` → `not-found`; other → error toast, edits kept. `ui-not-found-state` / `ui-forbidden-state` back/recover CTA → `/resumes`. Both subscriptions use `takeUntilDestroyed`. Mount `ui-toast-host`. One `h1`.
  - [x] `frontend/src/pages/resume-detail/resume-detail.page.spec.ts` (CREATE) — see Task 6.
  - [x] `frontend/src/app/app.routes.ts` (UPDATE) — `resumes/:id` → `loadComponent` → `ResumeDetailPage`, `data: { title: 'Résumé' }` (keep it a non-empty string; `app.routes.spec.ts` requires one). `resumes/new` must still precede it.
  - [x] `frontend/src/app/app.routes.spec.ts` (UPDATE) — assert `resumes/:id` loads `ResumeDetailPage`.

- [x] Task 5: Tracker and docs hygiene
  - [x] `deferred-work.md` (UPDATE) — add `## Deferred from: Story 2.3` for the unsaved-changes guard, the BE "cannot clear" gap, and anything else surfaced.

- [x] Task 6: Verification and test coverage (AC: all)
  - [x] Page spec (`HttpTestingController`, `provideRouter` with the `:id` route, match URLs by predicate): loading skeleton → form populated (AC1); `GET` once per id and again when the param changes; `404` on load (also for a non-GUID id) → not-found state with a CTA to `/resumes` (AC3); `403`/other on load → forbidden/error with Retry; save sends a PATCH containing only changed fields and, on `200`, toasts, adopts the response and a second save diffs against it (AC2); list cache invalidated (a later `list()` hits the network); `400` maps inline and keeps edits (AC4); `403`/`404` on save swap to forbidden/not-found without leaking data (AC5); other failure toasts and keeps edits; destroying the page cancels in-flight requests.
  - [x] `dotnet build backend/JobNecto.slnx --configuration Release --warnaserror` and `dotnet test backend/JobNecto.slnx` (backend unaffected — confirm and record counts).
  - [x] `cd frontend && npx ng test --no-watch` — all passing, per-file ≥80% coverage gate clean; `npx ng build --configuration production` clean.
  - [x] Manual smoke against a live API + PostgreSQL: **API-level half done, browser half NOT run** (the Claude-in-Chrome extension was not connected). See Completion Notes.

## Dev Notes

### Technical Requirements
- Angular Signals + injectable services, typed Reactive Forms, no NgRx/TanStack (AR11). Every component `OnPush`; Tailwind **token** classes only (`rounded-pill`, spacing steps 1-6/8/10/12 — no `.5` steps, no `rounded-full`).
- Feature-sliced: `widgets/resume-form` never injects `HttpClient`; `pages/resume-detail` owns the calls and the toast; `entities/resume` owns `get`/`update`.
- One `h1`; heading order; state components render their own `h2`. Errors announce via `aria-live`.
- Prettier-format files you create (the repo isn't prettier-clean overall — don't reformat untouched files).

### API / Contract Guardrail
- `GET /api/v1/resumes/{id}` → `ResumeResult`; `404` for not-found, soft-deleted and cross-user (`ResumesController.cs` `GetAsync`).
- `PATCH /api/v1/resumes/{id}` (`UpdateResumeCommand`) → `200 ResumeResult`; `400` validation (`errors` map; `RuleForEach` keys like `Skills[0]` — already handled by `matchControl`), `403` cross-user, `404` not-found/soft-deleted. At least one updatable field must be present or the server returns `400` ("At least one updatable field must be provided.") — another reason Save must be disabled on an empty diff.
- Request-side `salary` is typed `null | number | string`; the form already produces a real `number | null` (Story 2.1 fixed the string-coercion bug in `TextField` `type="number"`). Do not re-coerce.
- No backend change in this story. `authorization-contract-matrix.md` rows 1–2 already cover the status codes.

### File Structure Requirements
- `frontend/src/entities/resume/{model.ts,resume.service.ts,resume.service.spec.ts}` (UPDATE)
- `frontend/src/widgets/resume-form/{resume-form.ts,resume-form.html,resume-form.spec.ts}` (UPDATE), `resume-form.diff.ts` + `.spec.ts` (CREATE)
- `frontend/src/pages/resume-detail/{resume-detail.page.ts,.html,.spec.ts}` (CREATE)
- `frontend/src/app/{app.routes.ts,app.routes.spec.ts}` (UPDATE)
- `_bmad-output/implementation-artifacts/deferred-work.md` (UPDATE)

### Testing Requirements
- `list()`/`get()`/`update()` URLs: match with a predicate on `req.url` (Story 1.6 Trap 5). Call `httpMock.verify()` in `afterEach` inside `try/finally` so one failing test can't cascade (lesson from 2.2).
- Tests that move focus must clean up stray nodes in `afterEach`; don't depend on timezone for date text.
- jsdom does not blur a focused element that becomes `disabled`; assert attributes, not focus behavior, for that.

### Previous Story Intelligence
- **2.1**: the form widget, validators, `applyServerErrors`, the `languageRowServerErrors` field + `markForCheck()` workaround (a signal read inside the language-row `@for` breaks reconciliation; a manual `setErrors()` on a freshly pushed `FormArray` control is wiped on the next render), `ToastService`/`ui-toast-host`, `takeUntilDestroyed` on every view-touching subscription, `ResumeService` generation counter (don't weaken it).
- **2.2**: `ui-cursor-pagination`, `nextCursor`/`toCount`, the list page whose cards now link to `/resumes/:id` — this story makes those links real. Also: use `aria-disabled` not `disabled` on buttons that must keep focus; extract typed child components instead of untyped `ng-template` contexts; moving focus only when the focused control is removed.
- **Deferred items relevant here** (`deferred-work.md`): no dedupe on skills/duplicate language rows (a user can now edit into a state the server may accept — don't add client dedupe unless an AC says so); `SelectFieldComponent.writeValue` doesn't validate membership in `options()` (a stale/unknown enum value from the server will render as blank — test that loading a résumé with an unrecognized enum string doesn't throw, and decide in code review whether to surface it).

### ⚠️ Trap 1 — `form.dirty` is not "changed"
Edit-then-revert leaves `dirty === true`. Gate Save and build the PATCH from the diff against the loaded baseline.

### ⚠️ Trap 2 — Patching the form from a résumé marks it dirty or re-fires validators wrongly
Use `form.reset(values)` (pristine, untouched) and rebuild the `languages` `FormArray` rows before resetting; do not `setValue` control-by-control on a live form, which leaves it dirty and Save enabled.

### ⚠️ Trap 3 — The server can't clear most optional fields
See Decision Record. Sending `''` for an enum or `[]` for skills is a `400`; sending `null` is silently "unchanged". Do not "clear" by sending nulls and calling it done.

### ⚠️ Trap 4 — 404 vs 403 differ by verb
Read: cross-user → `404`. Write: cross-user → `403`. A single catch-all mapping will show the wrong state in one of them.

### ⚠️ Trap 5 — Component reuse across ids
`/resumes/a` → `/resumes/b` keeps the same page instance. Subscribe to `paramMap`, cancel the previous in-flight `get`, and reset `resume`/`status` on each change.

### ⚠️ Trap 6 — The post-save response is the new baseline
If the page keeps diffing against the *original* load, a second save re-sends already-saved fields; and if it doesn't push the response back into the form (`resume` input), the form stays "dirty" and Save stays enabled after success.

### Open follow-up (product/backend call, not part of this story)
The API cannot unset salary, currency, work-location, experience or skills once set (Decision Record). If users need to clear them, that's a small backend story (accept explicit null/`""` as "clear", relax the skills rule) plus removing the blocked-clear message here. Worth deciding before the demo.

### References
- [Source: `_bmad-output/planning-artifacts/epics.md` — Story 2.3, lines 460-478]
- [Source: `_bmad-output/planning-artifacts/prd-demo-mvp.md` — FR13, FR14, FR41]
- [Source: `_bmad-output/planning-artifacts/architecture/authorization-contract-matrix.md` — résumé rows 1-2 (404 read / 403 mutate)]
- [Source: `backend/src/JobNecto.API/Controllers/ResumesController.cs` — `GetAsync`, `UpdateAsync`]
- [Source: `backend/src/JobNecto.Application/Resumes/{UpdateResumeCommand.cs,UpdateResumeCommandHandler.cs,Validators/UpdateResumeCommandValidator.cs,Mappers/ResumeMappers.cs}`]
- [Source: `frontend/src/widgets/resume-form/*`, `frontend/src/pages/resume-create/*` — form, validators, server-error mapping, toast pattern]
- [Source: `frontend/src/entities/resume/{resume.service.ts,model.ts,resume-enums.ts}`]
- [Source: `frontend/src/shared/api/problem-details.ts` — `mapProblemToUxState`; `frontend/src/shared/ui/state/{not-found-state,forbidden-state,error-state,skeleton}.ts`]
- [Source: `_bmad-output/archive/implementation-artifacts/{2-1-create-a-resume,2-2-browse-my-resumes}.md`]
- [Source: `_bmad-output/implementation-artifacts/deferred-work.md`]

## Dev Agent Record

### Agent Model Used

Sonnet 5.5 (claude-sonnet-5-5)

### Debug Log References

- Baseline revision for the review diff: `a7c9dd1c0a807bb5119480beea49bc51eb3e7a06` (branch `story/2-3-view-edit-a-resume`, master after PR #98).
- Backend (unchanged by this story): `dotnet build backend/JobNecto.slnx --configuration Release --warnaserror` → `Build succeeded. 0 Warning(s) 0 Error(s)`; `dotnet test backend/JobNecto.slnx --configuration Release --no-build` → `Passed! - Failed: 0, Passed: 632, Skipped: 0, Total: 632`.
- Frontend baseline before changes: 429 passing. After review patches: `cd frontend && npx ng test --no-watch` → `Test Files 39 passed (39)`, `Tests 541 passed (541)`, coverage `97.23% stmts / 95.32% branches / 98.11% lines`, no per-file threshold failures.
- `cd frontend && npx ng build --configuration production` → `Application bundle generation complete.` (no errors/warnings).
- **Live API checks** (API on `localhost:5000` against a throwaway Postgres 16 container with all migrations applied, three seeded users, 25 résumés): `GET /resumes/{id}` own → 200, other user's → **404**, non-GUID → **404**; `PATCH` other user's → **403**; `PATCH {}` → 400; `PATCH {"skills":[]}` → 400; `PATCH {"currency":""}` → 400; `PATCH {"title":""}` → 200; `PATCH {"locations":[]}` → 200; `PATCH {"title":"T2","salary":null}` → 200 with salary unchanged. This confirms every claim in the Decision Record.

### Completion Notes List

- Built exactly the plan: `ResumeService.get/update` (id percent-encoded), pure `resume-form.diff.ts` (`resumeToFormValue`, `buildUpdateCommand` + `blockedClears`), edit mode on the shared `ResumeFormComponent` (optional `resume` input, `update` output; create mode untouched and its specs unmodified), `ResumeDetailPage` at `/resumes/:id`.
- Deviation from Task 3's text: `populate()` resets the form **first** and rebuilds the language rows afterwards. `form.reset()` also resets rows already in the `FormArray`, so the story's "rebuild before reset" order blanked them (caught by the first spec run).
- Design additions from review: the form snapshots its value when an update is emitted; if the user keeps editing while the request is in flight, the response becomes the new baseline **without** repopulating (the diff reads the baseline from the `resume` input), so those edits survive and the next Save sends only them. A different résumé id always repopulates.
- A save issued for the previous id is cancelled when the route id changes (and `saving` is reset), so a late response can never be applied to the résumé now showing.
- Server-stored values the form rejects (e.g. an unknown enum string) now show their validation error on load instead of silently disabling Save.
- Adding or removing a language row now marks the form dirty.
- **Live smoke: API half done, browser half NOT run.** The Claude-in-Chrome extension was not connected in this session, so I could not drive the UI (visual layout, real DOM input events, the narrow-viewport view). The Angular dev server and API were running against the throwaway database for it; everything the UI does is covered by DOM-level specs and the AOT build.
- Backend coverage gate (`scripts/check_coverage.py`) not re-run: no backend file changed and `python` is not on this shell's PATH.

### File List

- `_bmad-output/implementation-artifacts/2-3-view-edit-a-resume.md` (UPDATED — status, ticks, Dev Agent Record)
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (UPDATED — 2-3 status)
- `_bmad-output/implementation-artifacts/deferred-work.md` (UPDATED — Story 2.3 deferrals; disproved 2.2 item struck)
- `_bmad-output/archive/implementation-artifacts/2-2-browse-my-resumes.md` (UPDATED — corrected a review claim disproved against the live API)
- `frontend/src/entities/resume/model.ts` (UPDATED — `UpdateResumeCommand`)
- `frontend/src/entities/resume/resume.service.ts` (UPDATED — `get`, `update`)
- `frontend/src/entities/resume/resume.service.spec.ts` (UPDATED — get/update/id-encoding specs)
- `frontend/src/widgets/resume-form/resume-form.diff.ts` (CREATED)
- `frontend/src/widgets/resume-form/resume-form.diff.spec.ts` (CREATED)
- `frontend/src/widgets/resume-form/resume-form.ts` (UPDATED — edit mode, adopt/populate, canSubmit, blocked-clear messaging)
- `frontend/src/widgets/resume-form/resume-form.html` (UPDATED — submit label)
- `frontend/src/widgets/resume-form/resume-form.spec.ts` (UPDATED — edit-mode describe)
- `frontend/src/pages/resume-detail/resume-detail.page.ts` (CREATED)
- `frontend/src/pages/resume-detail/resume-detail.page.html` (CREATED)
- `frontend/src/pages/resume-detail/resume-detail.page.spec.ts` (CREATED)
- `frontend/src/app/app.routes.ts` (UPDATED — `resumes/:id` → `ResumeDetailPage`)
- `frontend/src/app/app.routes.spec.ts` (UPDATED — route wiring + title)

### Review Findings

Four parallel lenses (adversarial, edge-case hunter, verification gap, acceptance) ran against the diff; each finding was checked against the code before triage.

- [x] [Patch] **high/medium** — A save in flight when the route id changed was not cancelled or id-guarded (its 200/400/403/404 landed on the new résumé; `saving` stayed true): `load()` now unsubscribes the save and resets `saving`.
- [x] [Patch] **medium** — A save response repopulated the form and wiped edits typed while it was in flight: snapshot-at-send + adopt-without-repopulate for the same id.
- [x] [Patch] **medium** — Route id interpolated unencoded into the request URL (`..%2Fusers%2Fme` reached `/api/v1/users/me`): `encodeURIComponent` in `get`/`update`.
- [x] [Patch] **medium** — A stored value the form rejects disabled Save with no visible reason: errors surface on load.
- [x] [Patch] **low-medium** — Edit-mode Save did not require `dirty` (AC2 wording); adding/removing a language row never dirtied the form (would have made removal un-saveable): both fixed.
- [x] [Patch] **low** — `salary: ''` from the server became 0; first-load Retry dropped focus to `<body>` (now focuses the loading region); page spec `afterEach` not failure-safe.
- [x] [Patch] **medium (tests)** — untested: the blocked-clear guard in `onSubmit`, the message's scoping/rendering, banner clearing on repopulate, invalid-edit Save state, the toast fallback, `dirty` requirement, body-less 404, route title, save-vs-id-change, edit-during-save.
- [x] [Dismiss] Case-variant enum strings validate but do not display — the API serializes enums by canonical name.
- [x] [Dismiss] Null/partial PATCH response body, and `resume` going back to null — the API returns a full `ResumeResult`, and the page destroys the widget before that can happen.
- [x] [Dismiss] Trap 2 wording (rebuild rows before reset) — the implemented order is the correct one; see Completion Notes.
- [x] [Dismiss] Redundant defensive resets (`saving.set(false)` in `load()`, `resume.set(null)` in `handleSaveError`) — harmless.
- [ ] [Defer] Duplicate skills render with a duplicate track key; legacy server language rows with only one half are dropped from a languages PATCH; a save cancelled by navigating away skips the list-cache invalidation; 403/404 on save does not move focus to the new state; the diff is recomputed several times per change-detection pass. All logged to `deferred-work.md`.

## Change Log

- 2026-10-21: Story created (ready-for-dev). Baseline: merge of PR #96 (`bb8e78f`).
- 2026-10-21: Implemented all tasks test-first; verified the PATCH/GET contract against a live API and database; self-reviewed with four parallel lenses (8 patch groups, 4 dismissed, 5 deferred). Frontend 541/541, backend 632/632, AOT build clean. Browser smoke not run. Status set to `review`.
