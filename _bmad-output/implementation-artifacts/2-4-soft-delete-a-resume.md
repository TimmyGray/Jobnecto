# Story 2.4: Soft-delete a résumé

Status: ready-for-dev

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

- [ ] Task 0: Test-first baseline
  - [ ] Confirm green before changing anything: `cd frontend && npx ng test --no-watch` (expect 541 passing at the merge of PR #99).

- [ ] Task 1: Entity service `delete` (AC: 2, 3)
  - [ ] `frontend/src/entities/resume/resume.service.ts` (UPDATE) — `delete(id: string): Observable<void>`: `DELETE /resumes/{encodeURIComponent(id)}`; `tap(() => this.invalidate())` on success only. XML-doc it. (`delete` is a legal method name on a class.)
  - [ ] `frontend/src/entities/resume/resume.service.spec.ts` (UPDATE) — sends `DELETE` with credentials to the encoded URL; emits on `204` and invalidates `page()`; a stale in-flight `list()` cannot resurrect the cache; `403`/`404`/`500` propagate the normalized `ProblemDetails` and leave the cache alone.

- [ ] Task 2: `ConfirmDialogComponent` (AC: 1, 4)
  - [ ] `frontend/src/shared/ui/dialog/confirm-dialog.ts` (CREATE) — `ui-confirm-dialog`, `OnPush`, template over a native `<dialog>`. Inputs: `open = input(false)`, `headline = input.required<string>()`, `message = input('')`, `confirmLabel = input('Confirm')`, `cancelLabel = input('Cancel')`, `destructive = input(false)`, `busy = input(false)`. Outputs: `confirm`, `cancel`. An `effect` calls `showModal()` / `close()` when `open` changes (guard `dialog.open` to avoid `InvalidStateError`; tolerate jsdom lacking `showModal` — see Testing Requirements). Native `cancel` event (`Esc`) and a backdrop click both emit `cancel` (not when `busy`). Default focus on the Cancel button (`autofocus`). Confirm uses `aria-disabled` while `busy` and shows "Deleting…"-style busy text via `confirmLabel` swap; it ignores clicks while busy. `aria-labelledby` → the headline, `aria-describedby` → the message. On close, return focus to the element that opened it (capture `document.activeElement` on open). Destructive variant uses `status.danger` token classes, tokens only.
  - [ ] `frontend/src/shared/ui/dialog/confirm-dialog.spec.ts` (CREATE) — opens/closes with the input; Cancel has initial focus; confirm and cancel emit exactly once; `Esc` (native `cancel` event) and backdrop click emit `cancel`; neither emits while `busy`; confirm is `aria-disabled` (not `disabled`) and relabelled while busy and does not emit; destructive adds the danger token class; labelled/described by headline/message; focus returns to the opener on close.
  - [ ] `frontend/src/shared/ui/index.ts` (UPDATE) — export it.

- [ ] Task 3: Delete on `ResumeDetailPage` (AC: 1, 2, 3, 4)
  - [ ] `frontend/src/pages/resume-detail/resume-detail.page.ts` + `.html` (UPDATE) — a secondary destructive-outline **Delete résumé** button (not the page's one primary action) shown only when `ready`; opens the dialog (`confirmingDelete` signal). `onConfirmDelete()`: if `deleting()` return; `deleting.set(true)`; `resumeService.delete(id)` with `takeUntilDestroyed`; success → close dialog, toast "Résumé deleted", `router.navigate(['/resumes'])`; error → close dialog, then `403` → forbidden state, `404` → not-found state (no error toast), other → error toast and stay. `load()` also cancels an in-flight delete and resets `deleting`/`confirmingDelete` (route id changed). Pass `[submitting]="saving() || deleting()"` to the form so Save is disabled meanwhile; `onUpdate` returns early while `deleting()`.
  - [ ] `frontend/src/pages/resume-detail/resume-detail.page.spec.ts` (UPDATE) — Delete button hidden while loading and in every non-ready state; click opens the dialog and sends **no** request; Cancel/`Esc`/backdrop send nothing and return focus to the Delete button; confirm sends `DELETE /resumes/{id}`, shows busy state, ignores a second confirm; `204` → toast, list cache invalidated, navigates to `/resumes`; `403` → forbidden state with no data leaked; `404` → not-found state, no error toast; `500` → error toast, dialog closed, form and edits intact, can retry; Save is disabled while deleting and a submit during delete sends no PATCH; route-id change mid-delete cancels it and the response cannot affect the new résumé; destroying the page cancels an in-flight delete.

- [ ] Task 4: Tracker and docs hygiene
  - [ ] `deferred-work.md` (UPDATE) — add `## Deferred from: Story 2.4` for anything surfaced and left (at minimum: delete from the list, undo, and the cover-letter coupling check for Epics 4/5).

- [ ] Task 5: Verification and test coverage (AC: all)
  - [ ] `cd frontend && npx ng test --no-watch` — all passing, per-file ≥80% coverage gate clean; `npx ng build --configuration production` clean.
  - [ ] `dotnet build backend/JobNecto.slnx --configuration Release --warnaserror` and `dotnet test backend/JobNecto.slnx` (backend unaffected — confirm and record counts).
  - [ ] Live check against the API + PostgreSQL if available: confirm the DELETE/GET/PATCH/list behavior in the Decision Record still holds. Record whether a browser smoke was possible.

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

### Debug Log References

### Completion Notes List

### File List

### Review Findings

## Change Log

- 2026-10-21: Story created (ready-for-dev). Baseline: merge of PR #99 (`d6d13ea`). Delete contract verified against a live API + PostgreSQL.
