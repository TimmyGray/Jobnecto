# Story 2.4: Soft-delete a résumé

Status: done

<!-- Merged 2026-10-21 via PR #101 (0b50e46). Archived per AGENTS.md lifecycle. -->

## Story

As a **user**,
I want **to delete a résumé**,
so that **I can remove outdated material**.

<!-- Track: [FE] — Covers: FR15, FR41, AR15, AR16, UX-DR15 -->

## Decision Record

- 2026-10-21: **The delete action lives on the résumé detail page (`/resumes/:id`), not on the list cards.** List cards are single `<a>` links; nesting a destructive button inside a link is an accessibility anti-pattern (interactive content inside interactive content) and a mis-tap hazard on mobile. The detail page already owns the résumé, its load/forbidden/not-found states and a toast host. If product wants delete straight from the list later, that is a separate story (a card "more" menu).
- 2026-10-21: **"The row is removed immediately and cursor metadata refreshes" is satisfied by navigation + refetch, not by in-place list surgery.** On `204` the page toasts "Résumé deleted", the service invalidates the cached list page (`ResumeService.delete()` calls `invalidate()` exactly as `update()` does), and the page navigates to `/resumes`, whose first load fetches a fresh first page with a fresh cursor and `totalCount`. Nothing is optimistic: the row disappears because the list is reloaded, never because the client guessed. (There is no list-page code to change; `ResumeListPage` always fetches on entry — verified in Story 2.2.)
- 2026-10-21: **Build a reusable `ConfirmDialogComponent` in `shared/ui`** (`ui-confirm-dialog`). UX-DR15 requires a destructive confirmation "never one-click irreversible"; no dialog primitive exists yet (`shared/ui` has form, feedback, layout, pagination, state). Build it over the native `<dialog>` element (`showModal()` gives focus trapping, `Esc` to cancel and an inert background for free — consistent with the project's "thin wrappers over native elements" decision from Stories 2.1/2.2; spartan-ng stays deferred). It is generic: inputs `open`, `headline`, `message`, `confirmLabel`, `cancelLabel`, `destructive`, `busy`; outputs `confirm`, `cancel`. Epic 5 (templates, cover letters) reuses it unchanged.
- 2026-10-21: **Destructive styling uses the `status.danger` token, never a raw color,** and the destructive button is paired with text (not color alone). Default focus lands on **Cancel**, not on the destructive action. The confirm button is `aria-disabled` (not `disabled`) and shows "Deleting…" while the request is in flight, so keyboard focus is never dropped; a second activation is ignored.
- 2026-10-21: **Verified live (API + PostgreSQL), 2026-10-21:** own `DELETE` → **204**; the same id again → **404**; `GET` and `PATCH` of the deleted id → **404**; the id is gone from the list and `totalCount` drops by one; another user's id → **403** and that résumé stays intact; a non-GUID id → **404**. Matches `authorization-contract-matrix.md` row 3 (404 not-found/soft-deleted, 403 cross-user).
- 2026-10-21: **Status mapping on delete:** `204` → success path. `403` → replace the page with `ui-forbidden-state` ("explain + safe recovery", AC3). `404` → the résumé is already gone (deleted in another tab/session): treat as **success-equivalent** for the user's intent — show `ui-not-found-state` with the back CTA, no error toast. Any other failure → error toast, dialog closes, the page and the résumé stay as they were, the user can retry. Use `mapProblemToUxState()`.
- 2026-10-21: **A delete cancels any save in flight and vice-versa.** While the confirm dialog is open or a delete is in flight the form's Save is disabled; the page must not issue a PATCH and a DELETE for the same résumé concurrently. (The page already tracks `saving`; add `deleting` and gate both.)
- 2026-10-21: **Out of scope:** undo/restore, bulk delete, deleting from the list, and the unsaved-changes navigation guard (deferred in Story 2.3). Deleting a résumé does **not** touch cover letters that were generated from it (that coupling arrives with Epic 4/5 and must be checked there).

## Acceptance Criteria

1. **Destructive confirmation**
   **Given** I own a résumé
   **When** I choose delete
   **Then** a destructive confirmation dialog appears (never a one-click irreversible action): it names the action, says it cannot be undone from the app, has a clearly secondary **Cancel** (default focus, `Esc` also cancels) and a destructive **Delete résumé** button (UX-DR15)

2. **Confirmed delete**
   **Given** I confirm
   **When** the client calls `DELETE /api/v1/resumes/{id}`
   **Then** on `204` the résumé list cache is invalidated, a success toast confirms, and I land on `/resumes` where a fresh first page (new cursor and `totalCount`) no longer contains it (FR15, AR16)

3. **Not mine**
   **Given** I attempt to delete a résumé I do not own
   **When** the server responds
   **Then** it returns `403` and I see an explain + safe-recovery state, never raw status codes or the résumé's data (FR41, AR15)

4. **Cancel is safe**
   **Given** the dialog is open
   **When** I cancel (button, `Esc`, or backdrop)
   **Then** nothing is sent, nothing changes, and focus returns to the Delete button

## Tasks / Subtasks

- [x] Task 0: Test-first baseline
  - [x] Confirm green before changing anything: `cd frontend && npx ng test --no-watch` (expect 541 passing at the merge of PR #99).

- [x] Task 1: Entity service `delete` (AC: 2, 3)
  - [x] `frontend/src/entities/resume/resume.service.ts` (UPDATE) — `delete(id: string): Observable<void>`: `DELETE /resumes/{encodeURIComponent(id)}`; `tap(() => this.invalidate())` on success only. XML-doc it. (`delete` is a legal method name on a class.)
  - [x] `frontend/src/entities/resume/resume.service.spec.ts` (UPDATE) — sends `DELETE` with credentials to the encoded URL; emits on `204` and invalidates `page()`; a stale in-flight `list()` cannot resurrect the cache; `403`/`404`/`500` propagate the normalized `ProblemDetails` and leave the cache alone.

- [x] Task 2: `ConfirmDialogComponent` (AC: 1, 4)
  - [x] `frontend/src/shared/ui/dialog/confirm-dialog.ts` (CREATE) — `ui-confirm-dialog`, `OnPush`, template over a native `<dialog>`. Inputs: `open = input(false)`, `headline = input.required<string>()`, `message = input('')`, `confirmLabel = input('Confirm')`, `cancelLabel = input('Cancel')`, `destructive = input(false)`, `busy = input(false)`. Outputs: `confirm`, `cancel`. An `effect` calls `showModal()` / `close()` when `open` changes (guard `dialog.open` to avoid `InvalidStateError`; tolerate jsdom lacking `showModal` — see Testing Requirements). Native `cancel` event (`Esc`) and a backdrop click both emit `cancel` (not when `busy`). Default focus on the Cancel button (`autofocus`). Confirm uses `aria-disabled` while `busy` and shows "Deleting…"-style busy text via `confirmLabel` swap; it ignores clicks while busy. `aria-labelledby` → the headline, `aria-describedby` → the message. On close, return focus to the element that opened it (capture `document.activeElement` on open). Destructive variant uses `status.danger` token classes, tokens only.
  - [x] `frontend/src/shared/ui/dialog/confirm-dialog.spec.ts` (CREATE) — opens/closes with the input; Cancel has initial focus; confirm and cancel emit exactly once; `Esc` (native `cancel` event) and backdrop click emit `cancel`; neither emits while `busy`; confirm is `aria-disabled` (not `disabled`) and relabelled while busy and does not emit; destructive adds the danger token class; labelled/described by headline/message; focus returns to the opener on close.
  - [x] `frontend/src/shared/ui/index.ts` (UPDATE) — export it.

- [x] Task 3: Delete on `ResumeDetailPage` (AC: 1, 2, 3, 4)
  - [x] `frontend/src/pages/resume-detail/resume-detail.page.ts` + `.html` (UPDATE) — a secondary destructive-outline **Delete résumé** button (not the page's one primary action) shown only when `ready`; opens the dialog (`confirmingDelete` signal). `onConfirmDelete()`: if `deleting()` return; `deleting.set(true)`; `resumeService.delete(id)` with `takeUntilDestroyed`; success → close dialog, toast "Résumé deleted", `router.navigate(['/resumes'])`; error → close dialog, then `403` → forbidden state, `404` → not-found state (no error toast), other → error toast and stay. `load()` also cancels an in-flight delete and resets `deleting`/`confirmingDelete` (route id changed). Pass `[submitting]="saving() || deleting()"` to the form so Save is disabled meanwhile; `onUpdate` returns early while `deleting()`.
  - [x] `frontend/src/pages/resume-detail/resume-detail.page.spec.ts` (UPDATE) — Delete button hidden while loading and in every non-ready state; click opens the dialog and sends **no** request; Cancel/`Esc`/backdrop send nothing and return focus to the Delete button; confirm sends `DELETE /resumes/{id}`, shows busy state, ignores a second confirm; `204` → toast, list cache invalidated, navigates to `/resumes`; `403` → forbidden state with no data leaked; `404` → not-found state, no error toast; `500` → error toast, dialog closed, form and edits intact, can retry; Save is disabled while deleting and a submit during delete sends no PATCH; route-id change mid-delete cancels it and the response cannot affect the new résumé; destroying the page cancels an in-flight delete.

- [x] Task 4: Tracker and docs hygiene
  - [x] `deferred-work.md` (UPDATE) — add `## Deferred from: Story 2.4` for anything surfaced and left (at minimum: delete from the list, undo, and the cover-letter coupling check for Epics 4/5).

- [x] Task 5: Verification and test coverage (AC: all)
  - [x] `cd frontend && npx ng test --no-watch` — all passing, per-file ≥80% coverage gate clean; `npx ng build --configuration production` clean.
  - [x] `dotnet build backend/JobNecto.slnx --configuration Release --warnaserror` and `dotnet test backend/JobNecto.slnx` (backend unaffected — confirm and record counts).
  - [x] Live check against the API + PostgreSQL: done (API half). Browser smoke NOT possible — the Claude-in-Chrome extension was not connected. See Completion Notes.

## Dev Notes

### Technical Requirements
- Angular Signals + injectable services, no NgRx/TanStack (AR11). Every component `OnPush`; Tailwind **token** classes only (`rounded-pill`, spacing steps 1-6/8/10/12 — no `.5` steps; the danger color is the `status.danger` token).
- Feature-sliced: `shared/ui/dialog` generic (no domain knowledge); `entities/resume` owns `delete`; `pages/resume-detail` owns the call, the toast and the navigation.
- One `h1`; the dialog's headline is its own heading inside the modal. Errors announce via `aria-live`.
- Use `ProblemDetails`/`mapProblemToUxState` from `@shared/api`; never string-match status codes.

### API / Contract Guardrail
- `DELETE /api/v1/resumes/{id}` → `204` (soft delete: `IsDeleted`, filtered out of every read). `403` cross-user, `404` not-found/already-deleted/non-GUID, `401` unauthenticated (handled globally by the auth-redirect interceptor). Handler: `DeleteResumeCommandHandler` (`GetByIdAsync` → NotFound; owner check → Forbidden; `SoftDeleteAsync`).
- Idempotence: a repeated delete is a `404`, not a `204` — the client must treat a `404` after its own confirmed delete (double-click race, other tab) as "already gone".
- No backend change in this story. `authorization-contract-matrix.md` row 3 already covers the status codes.

### File Structure Requirements
- `frontend/src/entities/resume/{resume.service.ts,resume.service.spec.ts}` (UPDATE)
- `frontend/src/shared/ui/dialog/{confirm-dialog.ts,confirm-dialog.spec.ts}` (CREATE), `frontend/src/shared/ui/index.ts` (UPDATE)
- `frontend/src/pages/resume-detail/{resume-detail.page.ts,.html,.spec.ts}` (UPDATE)
- `_bmad-output/implementation-artifacts/deferred-work.md` (UPDATE)

### Testing Requirements
- **jsdom and `<dialog>`:** jsdom does not implement `HTMLDialogElement.showModal()` / `close()` or the `cancel` event reliably. In the dialog spec, stub `showModal`/`close` on `HTMLDialogElement.prototype` in a `beforeEach` (set `open` and dispatch `close` in the stub) and dispatch the `cancel` event manually; do **not** change production code to suit the test. Assert on `open` attribute state and emitted events.
- Match HTTP requests by predicate; call `httpMock.verify()` inside `try/finally` in `afterEach`; remove any stray DOM nodes you add in `afterEach` (lessons from 2.2/2.3). Don't depend on timezone. jsdom doesn't blur a focused element that becomes `disabled` — assert attributes, not that focus behavior.

### Previous Story Intelligence
- **2.3** (`ResumeDetailPage`): the load/save subscriptions, `saving`, the forbidden/not-found/error states, route-id reload that cancels the in-flight save (extend the same pattern to delete), the toast host, `afterNextRender` focus helper, the CRLF line endings in several frontend files (edit with CRLF-aware tools; don't normalise whole files).
- **2.2**: `aria-disabled` not `disabled` on buttons that must keep focus; only move focus when the focused control is removed; typed child components over untyped `ng-template`.
- **2.1**: `takeUntilDestroyed` on every view-touching subscription; DOM-level assertions beat component-state assertions.
- **Deferred items relevant here:** the unsaved-changes guard (Story 2.3) — a delete navigates away from a possibly-dirty form on purpose; no guard exists, so no prompt is needed, but note it in the dialog copy only if product wants it.

### ⚠️ Trap 1 — jsdom has no `showModal()`
Calling it unguarded throws and fails every dialog test. Stub in the spec, and guard the production call with `typeof dialog.showModal === 'function'` only if you can do so without changing browser behavior.

### ⚠️ Trap 2 — Default focus must not be the destructive button
`<dialog>.showModal()` focuses the first focusable element; put **Cancel** first in DOM order (or `autofocus` it).

### ⚠️ Trap 3 — `404` after a confirmed delete is not an error
Double-click, a second tab, or a retry after a lost response all produce `404`. Show the not-found state, not a failure toast.

### ⚠️ Trap 4 — Navigating while the dialog is open
Closing the dialog must not leave the page `inert`/scroll-locked: close it (or let the native `close` run) before navigating, and verify the body is interactive on the list page.

### ⚠️ Trap 5 — Concurrent save and delete
`saving` and `deleting` must each disable the other path; otherwise a PATCH can race a DELETE and the PATCH response (`404`) would show a misleading not-found after a *successful* delete.

### Open follow-up (product call, not part of this story)
Should users be able to delete a résumé from the list (card menu) and/or undo a delete (restore is not exposed by the API)? Both are separate stories; soft-delete means the data still exists server-side.

### References
- [Source: `_bmad-output/planning-artifacts/epics.md` — Story 2.4, lines 480-498; UX-DR15 line 179]
- [Source: `_bmad-output/planning-artifacts/prd-demo-mvp.md` — FR15, FR41]
- [Source: `_bmad-output/planning-artifacts/architecture/authorization-contract-matrix.md` — résumé row 3]
- [Source: `backend/src/JobNecto.API/Controllers/ResumesController.cs` — `DeleteAsync`; `backend/src/JobNecto.Application/Resumes/DeleteResumeCommandHandler.cs`]
- [Source: `frontend/src/pages/resume-detail/*`, `frontend/src/entities/resume/resume.service.ts`]
- [Source: `frontend/src/shared/ui/{state,feedback,pagination}/*` — component conventions]
- [Source: `_bmad-output/archive/implementation-artifacts/{2-1-create-a-resume,2-2-browse-my-resumes,2-3-view-edit-a-resume}.md`]
- [Source: `_bmad-output/implementation-artifacts/deferred-work.md`]

## Dev Agent Record

### Agent Model Used

Sonnet 5.5 (claude-sonnet-5-5)

### Debug Log References

- Baseline revision for the review diff: `a22352fc71e998514bd3b158a496ac4d2426d968` (branch `story/2-4-soft-delete-a-resume`, master after PR #100).
- Backend (unchanged): `dotnet build backend/JobNecto.slnx --configuration Release --warnaserror` → `Build succeeded. 0 Warning(s) 0 Error(s)`; `dotnet test backend/JobNecto.slnx --configuration Release --no-build` → `Passed! - Failed: 0, Passed: 632, Skipped: 0, Total: 632`.
- Frontend baseline before changes: 541 passing. Final: `cd frontend && npx ng test --no-watch` → `Test Files 40 passed (40)`, `Tests 607 passed (607)`, coverage `97.38% stmts / 95.37% branches / 98.26% lines`, no per-file threshold failures.
- `cd frontend && npx ng build --configuration production` → `Application bundle generation complete.` (no errors/warnings).
- **Live API checks** (API on `localhost:5000`, throwaway Postgres 16 with all migrations, 25 seeded résumés): own `DELETE` → 204; the same id again → 404; `GET` and `PATCH` of the deleted id → 404; the id is gone from the list and `totalCount` drops 25 → 24; another user's id → **403** and that résumé stays intact (200); non-GUID → 404. Confirms every contract claim in the Decision Record.

### Completion Notes List

- Built to the plan: `ResumeService.delete()` (id percent-encoded, invalidates the list cache on success only), a reusable `ui-confirm-dialog` over the native `<dialog>`, and a Delete action + confirmation on `ResumeDetailPage`.
- Behaviour changes found in review and kept: `deleting` stays true after the 204 until navigation lands (so Save/Delete can't hit a deleted résumé while the lazy list route loads); the toast is raised only once navigation has landed; navigation uses `replaceUrl` so Back can't return to the dead URL; if navigation doesn't happen the page shows not-found (the résumé is gone).
- **Toast host moved to the app shell.** The local `<ui-toast-host />` on the create and detail pages is removed (and none was added to the list page): one persistent host in `AppShellComponent` means the `aria-live` region already exists when a toast is raised on one page and the user lands on another — a region inserted together with its content is typically not announced. This is the promotion Story 2.1's Decision Record said to do once a second page needed a toast.
- Dialog hardening from review: handles the native `close` event (Chrome closes a dialog on a second Escape even if the first was prevented, which would desync `open`); a press that starts inside and is released on the backdrop no longer cancels; `returnFocusTo` input (Safari/Firefox-on-macOS don't focus a button on click); a removed opener doesn't break focus restoration; a polite busy announcement.
- Focus is moved to the recovery state when a save or delete answers 403/404 (the control that had focus is removed). Delete button reads `aria-disabled` while a save is in flight.
- **Not verified in a real browser:** native `<dialog>` behaviour (focus trap, Escape, backdrop, top-layer) and the visual result. The Claude-in-Chrome extension was not connected; jsdom specs use a stub that mimics `showModal`/`close` (including focus moving in and out). The API half of the live check was done.
- Backend coverage gate (`scripts/check_coverage.py`) not re-run: no backend file changed and `python` is not on this shell's PATH.

### File List

- `_bmad-output/implementation-artifacts/2-4-soft-delete-a-resume.md` (UPDATED — status, ticks, Dev Agent Record)
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (UPDATED — 2-4 status)
- `_bmad-output/implementation-artifacts/deferred-work.md` (UPDATED — Story 2.4 deferrals)
- `frontend/src/entities/resume/resume.service.ts` (UPDATED — `delete`)
- `frontend/src/entities/resume/resume.service.spec.ts` (UPDATED — delete specs)
- `frontend/src/shared/ui/dialog/confirm-dialog.ts` (CREATED)
- `frontend/src/shared/ui/dialog/confirm-dialog.spec.ts` (CREATED)
- `frontend/src/shared/ui/index.ts` (UPDATED — export)
- `frontend/src/pages/resume-detail/resume-detail.page.ts` (UPDATED — delete flow, recovery focus)
- `frontend/src/pages/resume-detail/resume-detail.page.html` (UPDATED — Delete section, dialog)
- `frontend/src/pages/resume-detail/resume-detail.page.spec.ts` (UPDATED — delete specs)
- `frontend/src/pages/resume-create/resume-create.page.{ts,html,spec.ts}` (UPDATED — local toast host removed)
- `frontend/src/pages/resume-list/resume-list.page.spec.ts` (UPDATED — none; host was added then moved to the shell)
- `frontend/src/widgets/app-shell/app-shell.ts` (UPDATED — hosts the toasts)
- `frontend/src/widgets/app-shell/app-shell.spec.ts` (UPDATED — single persistent toast host)

### Review Findings

Three parallel lenses (adversarial + edge-case, verification-gap with mutation testing on a scratch copy, acceptance); each finding was checked against the code before triage.

- [x] [Patch] **high/medium** — `deleting` was reset on the 204 while navigation was pending, re-enabling Save/Delete for a deleted résumé (a PATCH/DELETE would 404 and flip the page to not-found after "deleted" had toasted).
- [x] [Patch] **medium** — The success toast mounted into an `aria-live` region created at the same moment (likely silent) and its 4s timer started before the lazy list route loaded: persistent host in the shell; toast raised after navigation lands.
- [x] [Patch] **medium** — Focus fell to `<body>` after a 403/404 on delete (and save) because the focused control was removed; and focus-return failed where a click doesn't focus a button (Safari): recovery-region focus + `returnFocusTo`.
- [x] [Patch] **medium** — Chrome's second Escape could close the native dialog behind the host's back (desync, un-reopenable): `(close)` handled.
- [x] [Patch] **low-medium** — Back returned to the deleted résumé's URL (`replaceUrl`); a drag from inside the dialog released on the backdrop cancelled it; Delete looked enabled while a save blocked it; busy state wasn't announced.
- [x] [Patch] **medium (tests)** — Mutation survivors killed: "Save disabled while deleting" was vacuous (form was pristine), the page-level re-entry guard was masked by the dialog's, toast tone, the id deleted after a route change, focus restoration ordering (stub now moves focus like a browser), Esc/backdrop focus return, edits intact after a 500; a vacuous "does not reopen" test removed.
- [x] [Dismiss] `navigate` with `void` — now handled via `.then`. Redundant defensive resets in `handleDeleteError`/`load()` — harmless.
- [ ] [Defer] Scroll lock behind the modal; list-page focus after a successful delete (the shell has no post-navigation focus management); the `backdrop:` colour uses an arbitrary Tailwind value over the `--color-text-primary` token (no overlay token exists); two copies of the `<dialog>` jsdom stub in specs. Logged to `deferred-work.md`.

## Change Log

- 2026-10-21: Story created (ready-for-dev). Baseline: merge of PR #100.
- 2026-10-21: Implemented all tasks test-first; delete contract verified against a live API + PostgreSQL; self-reviewed with three lenses (6 patch groups, 2 dismissed, 4 deferred); toast host promoted to the app shell. Frontend 607/607, backend 632/632, AOT build clean. Browser smoke not run. Status set to `review`.
