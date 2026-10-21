# Story 2.2: Browse my résumés

Status: review

## Story

As a **user**,
I want **a paginated list of my résumés**,
so that **I can find and manage them**.

<!-- Track: [FE] — Covers: FR12, FR41, NFR2, NFR5, UX-DR12, UX-DR16, AR16 -->

## Decision Record

- 2026-10-21: **Accumulate pages in the page component, not in `ResumeService`.** `ResumeService.list()` overwrites `page()` with whichever single page it last received (and `invalidate()` nulls it), so `page()` cannot back a load-more list. The page keeps its own `items` signal, appends each loaded page, and always calls `list()` on init (the service never short-circuits from cache), so revisiting `/resumes` after a create (which `invalidate()`s) always shows fresh data. Do **not** change `list()`'s signature or caching semantics — Story 1.6's dashboard depends on them.
- 2026-10-21: **Narrow the cursor at the boundary with a helper, rather than loosening `PageCursor`.** The generated `lastSeenId`/`lastSeenUpdatedAt` are `null | string`; `PageCursor` (`shared/api/pagination.ts`) is non-nullable (deferred from Story 1.6 PR review, `deferred-work.md`). Add `nextCursor(page): PageCursor | null` to `shared/api/pagination.ts`, returning `null` unless `hasNext` is true **and** both fields are non-null strings. Epic 3's vacancy board reuses it. Because this adds runtime code, remove `src/shared/api/pagination.ts` from `coverageExclude` in `frontend/angular.json` and give it a spec (it is no longer a pure-type file).
- 2026-10-21: **Share `toCount` instead of duplicating it.** `dashboard.page.ts` has a private `toCount()` that safely coerces `totalCount` (`number | string`) to `number | null`. This story needs the same coercion for the "N résumés" subtitle. Move it to `shared/api/pagination.ts` (exported), import it in `dashboard.page.ts`, and delete the local copy. Behavior must be byte-for-byte unchanged; the dashboard's existing specs are the regression guard.
- 2026-10-21: **Build `CursorPaginationComponent` in `shared/ui`** (`ui-cursor-pagination`). It is named in UX's component inventory (`ux-design-specification.md:524`, "`CursorPagination` (load-more, not page numbers)") and does not exist yet. It is generic — inputs `hasNext`, `loading`; output `loadMore` — so Epic 3 reuses it unchanged. A thin wrapper over a native `<button>`, matching `ErrorStateComponent`'s button styling (no spartan-ng, same deferral as Story 2.1).
- 2026-10-21: **Presentational `widgets/resume-list` + container `pages/resume-list`.** Same split as Story 1.6/2.1: the widget renders cards and takes plain inputs (no `HttpClient`, no `ResumeService`); the page owns fetching, accumulation, and load state. The card markup is *similar* to `widgets/recent-resumes`'s but not identical (skills, updated date, non-link fallback) — do **not** refactor `recent-resumes` to share it in this story; note the duplication in Completion Notes for a later cleanup.
- 2026-10-21: **A résumé without an `id` renders as a non-link card.** `ResumeResult.id` is optional in the generated schema. The dashboard widget links such a row to `/resumes/undefined` (deferred from Story 1.6). This story makes the list the main entry point, so the list must not: a card with no `id` renders as a plain (non-anchor) card. Do not touch `recent-resumes` here.
- 2026-10-21: **Card links target `/resumes/:id`, which is still a `ComingSoonStubPage` until Story 2.3.** That is intentional and consistent with the shell's "no dead ends" stub policy (Story 1.5); do not build any detail UI here.
- 2026-10-21: **Single primary action.** UX-DR14 allows at most one primary action per page. The page-header "Create résumé" action (→ `/resumes/new`) is shown only when the list is non-empty; when empty, the empty-state CTA is the one primary action.
- 2026-10-21: **Point the dashboard's "Create your first resume" CTA at `/resumes/new`.** It currently links to `/resumes` (a stub when it was written). With this story `/resumes` is the list, whose empty state would then ask the user to click create a second time. One-line fix plus its spec assertion (`recent-resumes.spec.ts:76`).
- 2026-10-21: **`ResumeCreatePage` is left unchanged.** Its Story 2.1 Decision Record stays on `/resumes/new` after a successful create because `/resumes` used to be a stub. That reason no longer holds, but navigating to the list on success is not in 2.1's or 2.2's epics ACs, so changing it is a product call, not a patch. Flagged in Dev Notes → *Open follow-up* for the user to decide.
- 2026-10-21: **Cite NFR2 + UX-DR16 for AC3 exactly as epics.md does; ownership (AC4) is server-scoped.** There is no client-side ownership logic to build: the cookie scopes `GET /api/v1/resumes`, and the client must simply never send a user identifier and never filter/merge anything beyond what the server returns. AC4 is satisfied by a test proving that, plus rendering exactly the returned items.

## Acceptance Criteria

1. **Cursor-paginated list with load-more**
   **Given** I have résumés
   **When** I open `/resumes`
   **Then** I see a cursor-paginated list using `{ items, lastSeenId, lastSeenUpdatedAt, hasNext }` with a load-more pattern (never page numbers); the next page sends both cursor fields (AR16, FR12)

2. **Empty state**
   **Given** I have no résumés
   **When** the list loads
   **Then** I see the "No resumes yet → Create first resume" empty state with a working CTA to `/resumes/new` (UX-DR12)

3. **Skeleton and cards**
   **Given** the list is loading, or the viewport is `xs/sm`
   **When** it renders
   **Then** a layout-preserving skeleton shows first (no layout shift) and rows render as cards, not a dense table (NFR2, UX-DR16)

4. **Ownership**
   **Given** another user's résumés exist
   **When** I list
   **Then** I only ever see my own (server-scoped; the client respects ownership and sends no user identifier) (FR41, NFR5)

## Tasks / Subtasks

- [x] Task 0: Test-first baseline
  - [x] Confirm baseline is green before changing anything: `cd frontend && npx ng test --no-watch` (expect 344 passing at `f05a5f6`).

- [x] Task 1: Cursor helper and shared `toCount` (AC: 1)
  - [x] `frontend/src/shared/api/pagination.ts` (UPDATE) — add `nextCursor(page: { hasNext: boolean; lastSeenId: string | null; lastSeenUpdatedAt: string | null }): PageCursor | null` (null unless `hasNext` and both fields are non-empty strings) and move `toCount(totalCount: unknown): number | null` here from `dashboard.page.ts` (exported, with the same XML-style doc comment).
  - [x] `frontend/src/shared/api/pagination.spec.ts` (CREATE) — `nextCursor`: `hasNext=false` → null; `hasNext=true` with either field null → null; both present → exact cursor object. `toCount`: number, numeric string, `undefined`, `'abc'`, `NaN` → expected values.
  - [x] `frontend/src/shared/api/index.ts` (UPDATE if needed) — export the new functions.
  - [x] `frontend/src/pages/dashboard/dashboard.page.ts` (UPDATE) — import `toCount` from `@shared/api`, delete the local function. No behavior change.
  - [x] `frontend/angular.json` (UPDATE) — remove `src/shared/api/pagination.ts` from `coverageExclude`.

- [x] Task 2: `CursorPaginationComponent` (AC: 1)
  - [x] `frontend/src/shared/ui/pagination/cursor-pagination.ts` (CREATE) — `ui-cursor-pagination`, `ChangeDetectionStrategy.OnPush`. Inputs `hasNext = input(false)`, `loading = input(false)`, `label = input('Load more')`; output `loadMore = output<void>()`. Renders nothing when `hasNext` is false. When `hasNext`, a native `<button type="button">` that is `aria-disabled` (not `disabled`, so it keeps keyboard focus mid-request) and shows a loading label ("Loading…") while `loading`, and does not emit while loading. Token classes only; visible focus ring like `ErrorStateComponent`'s button.
  - [x] `frontend/src/shared/ui/pagination/cursor-pagination.spec.ts` (CREATE) — hidden when `hasNext=false`; emits `loadMore` on click; disabled and non-emitting while `loading`; label changes while loading.
  - [x] `frontend/src/shared/ui/index.ts` (UPDATE) — export it.

- [x] Task 3: `ResumeListComponent` widget (AC: 2, 3, 4)
  - [x] `frontend/src/widgets/resume-list/resume-list.ts` (CREATE) — `widget-resume-list`, `OnPush`, presentational. Input: `items: ResumeResult[]` (a `totalCount` input was dropped — nothing in the widget needs it; the page shows the total). Renders a `<ul>` of cards (`data-testid="resume-card"`), single column on `xs/sm`, two columns from `md` (`grid gap-3 md:grid-cols-2`), never a table. Each card: title (`resume.title || 'Untitled resume'`) as an `h2`; meta line from `experience` · `workLocationType` humanized with `humanizeEnumMember` from `@entities/resume`, skipping absent parts so no stray separator (same rule as `recent-resumes`); up to 5 skills as chips plus a "+N" chip for the rest; "Updated {date}" from `updatedAt` via `DatePipe` (`mediumDate`) when present. A card with an `id` is an `<a [routerLink]="['/resumes', id]">`; a card without one is a plain non-anchor element (Decision Record). Visible focus ring and hover state on links, tokens only.
  - [x] `frontend/src/widgets/resume-list/resume-list.spec.ts` (CREATE) — renders N cards; title fallback; meta line with/without each part; skills cap + "+N"; `href` is `/resumes/<id>`; id-less résumé renders no anchor and no `/resumes/undefined` anywhere; no `<table>` in the DOM.
  - [x] `frontend/src/widgets/resume-list/index.ts` (CREATE) — barrel.

- [x] Task 4: `ResumeListPage` (AC: 1, 2, 3, 4)
  - [x] `frontend/src/pages/resume-list/resume-list.page.ts` + `resume-list.page.html` (CREATE) — `page-resume-list`, `OnPush`, injects `ResumeService`. State signals: `items: ResumeResult[]`, `status: 'loading' | 'ready' | 'error'`, `nextCursor: PageCursor | null`, `totalCount: number | null`, `loadingMore: boolean`, `loadMoreFailed: boolean`.
    - On init: `list()` (default page size); on success set `items`, `nextCursor = nextCursor(page)`, `totalCount = toCount(page.totalCount)`, `status='ready'`; on error `status='error'`.
    - `loadMore()`: no-op if `loadingMore` or `nextCursor` is null; else `list(DEFAULT, cursor)`, **append** the returned items, skipping any whose `id` is already present; update cursor/total; on error keep the existing items, set `loadMoreFailed`; an inline `role="alert"` message appears and the *same* pagination button is relabelled "Try again" (one element across failure and retry, so keyboard focus is never dropped). The loaded list is never discarded by a failed load-more.
    - `retry()` (initial-load error) re-runs the initial load; `status` returns to `'loading'`.
    - Both subscriptions use `takeUntilDestroyed(destroyRef)` (Story 2.1 lesson).
    - Template: `ui-page-header` (eyebrow "Résumés", h1 "Your *résumés*" with the serif accent word like 2.1's header, subtitle "N résumé(s)" when `totalCount` is known), then by `status`: `loading` → skeleton **cards** (4, in the same grid as the real cards; the subtitle line is reserved with a non-breaking space so the header does not jump); `error` → `ui-error-state` with retry; `ready` with no items → `ui-empty-state` ("No resumes yet", CTA `<a routerLink="/resumes/new">Create first resume</a>`); `ready` with items → `widget-resume-list` + `ui-cursor-pagination`. The header "Create résumé" `primaryAction` link (→ `/resumes/new`) renders only when `ready` and non-empty.
    - A polite live region announces "Showing X of Y résumés" after each successful load/load-more; when a load-more removes the Load-more button (the last page), move focus to the first newly appended card's link (or the card itself, `tabindex="-1"`, when it has no link) so keyboard focus is not lost; on a non-final page focus stays on the button. Focus is never moved if the user has since focused something else.
  - [x] `frontend/src/pages/resume-list/resume-list.page.spec.ts` (CREATE) — see Task 7.
  - [x] `frontend/src/app/app.routes.ts` (UPDATE) — replace the `resumes` route's `ComingSoonStubPage` with `loadComponent` → `ResumeListPage`, keep `data: { title: 'Resumes' }`. Leave `resumes/new` and `resumes/:id` exactly as they are.

- [x] Task 5: Dashboard CTA (AC: 2)
  - [x] `frontend/src/widgets/recent-resumes/recent-resumes.ts` (UPDATE) — the empty-state CTA `routerLink` becomes `/resumes/new`.
  - [x] `frontend/src/widgets/recent-resumes/recent-resumes.spec.ts` (UPDATE) — the CTA assertion at ~line 76 selects `a[href="/resumes/new"]`.
  - [x] (added) `frontend/src/pages/dashboard/dashboard.page.html` + `.spec.ts` (UPDATE) — the header "New resume" primary action also pointed at `/resumes` and now targets `/resumes/new`, for the same reason as the empty-state CTA.

- [x] Task 6: Tracker and docs hygiene
  - [x] Add one line to `_bmad-output/implementation-artifacts/deferred-work.md` under a new `## Deferred from: Story 2.2` heading for anything surfaced and intentionally left (at minimum the `recent-resumes`/`resume-list` card duplication and the dashboard's raw, un-humanized meta line).

- [x] Task 7: Verification and test coverage (AC: all)
  - [x] Page spec with `HttpTestingController` + `provideRouter([])` (match requests with a predicate, not an exact URL — Trap 3): initial load shows skeleton then cards; `hasNext=true` shows Load more; clicking it sends **both** `lastSeenId` and `lastSeenUpdatedAt` from the previous page and `pageSize`, appends (does not replace) items, and hides the button when `hasNext=false`; duplicate `id` across pages is not rendered twice; double-clicking Load more while in flight sends exactly one request; load-more failure keeps existing items and shows an inline retry that succeeds on retry; initial-load failure shows the error state and Retry recovers; empty response shows the empty state with a CTA whose `href` is `/resumes/new`, and no header primary action; non-empty shows the header action; **no request carries a `userId`/user-identifying param** and exactly the returned items render (AC4); `hasNext=true` with a null cursor field shows no Load more (no crash); destroying the page before the response arrives cancels the request (Story 2.1 pattern).
  - [x] `dotnet` is unaffected (frontend-only) — still run `dotnet build backend/JobNecto.slnx --configuration Release --warnaserror` and `dotnet test backend/JobNecto.slnx` once to confirm; record counts.
  - [x] `cd frontend && npx ng test --no-watch` — all passing, per-file ≥80% coverage gate satisfied including the newly un-excluded `pagination.ts`.
  - [ ] (NOT RUN — needs a live API + database and a browser; see Completion Notes) Manual smoke (record in Completion Notes): with the API running, create 21+ résumés (or lower the page size in the call temporarily and revert) and verify one load-more round-trip, the empty state on a fresh user, and the card layout at a narrow viewport.

## Dev Notes

### Technical Requirements
- Angular Signals + injectable services, no NgRx/TanStack (AR11). Every new component: `ChangeDetectionStrategy.OnPush`; Tailwind classes mapped to Career-OS tokens only — never raw hex/px (`shared/config/tokens.ts`).
- Feature-sliced boundaries are load-bearing (carried from 1.6 and 2.1): `shared/ui` generic, `entities/resume` API-bound state, `widgets/resume-list` composed presentational UI (never `HttpClient`), `pages/resume-list` owns the network calls and load state.
- Heading order (NFR10): one `h1` (the page header), card titles are `h2`. `EmptyState`/`ErrorState` render their own `h2`, which is correct at page level.
- Honor `prefers-reduced-motion` (the skeleton's shimmer is already covered by the global rule).

### API / Contract Guardrail
- `GET /api/v1/resumes` exists and is stable: `[Authorize]`, query `pageSize` (default 20, server caps at 100), `lastSeenId` (`Guid?`), `lastSeenUpdatedAt` (`DateTime?`); response is `PagedResult<ResumeResult>` = `{ items, totalCount, lastSeenId, lastSeenUpdatedAt, pageSize, hasNext, totalPages }` (`backend/src/JobNecto.API/Controllers/ResumesController.cs:65-92`; handler `ListResumesHandler.cs` caps page size at 100). Ordered `UpdatedAt DESC, Id DESC`; soft-deleted rows excluded by the global EF filter. **No backend change in this story.**
- `ResumeService.list(pageSize, cursor?)` already builds the exact query string, including both cursor fields (`resume.service.ts:41-57`).

### File Structure Requirements
- `frontend/src/shared/api/pagination.ts` (UPDATE), `frontend/src/shared/api/pagination.spec.ts` (CREATE), `frontend/src/shared/api/index.ts` (UPDATE if needed)
- `frontend/src/shared/ui/pagination/cursor-pagination.ts` (CREATE), `.spec.ts` (CREATE), `frontend/src/shared/ui/index.ts` (UPDATE)
- `frontend/src/widgets/resume-list/{resume-list.ts,resume-list.spec.ts,index.ts}` (CREATE)
- `frontend/src/pages/resume-list/{resume-list.page.ts,resume-list.page.html,resume-list.page.spec.ts}` (CREATE)
- `frontend/src/app/app.routes.ts` (UPDATE)
- `frontend/src/pages/dashboard/dashboard.page.ts` (UPDATE — import shared `toCount`)
- `frontend/src/widgets/recent-resumes/{recent-resumes.ts,recent-resumes.spec.ts}` (UPDATE)
- `frontend/angular.json` (UPDATE — `coverageExclude`)
- `_bmad-output/implementation-artifacts/deferred-work.md` (UPDATE)

### Testing Requirements
- `HttpTestingController` for the page spec. `list()` sends a query string, so `expectOne('/resumes')` will not match — use a predicate on `req.url`/`req.params` (Story 1.6 Trap 5, hit again in 2.1).
- Frontend runner is Vitest via `@angular/build:unit-test` (`npx ng test --no-watch`); per-file ≥80% line coverage is enforced (`AGENTS.md`).
- `src/**/index.ts` barrels are coverage-excluded; `pagination.ts` stops being excluded in this story.

### Previous Story Intelligence (2.1, merged 2026-09-28, PR #95)
- Established the primitives to reuse: `TextField`, `SelectField<T>`, `TagInput`, `ToastService`/`ToastHostComponent` (toast host is mounted locally per page — not needed here), `humanizeEnumMember` (`entities/resume/resume-enums.ts`).
- `ResumeService` gained a generation counter so an in-flight `list()` cannot resurrect a cache after `invalidate()`. This story must not weaken it; it also means a `list()` response arriving after `create()` is simply not cached — irrelevant here because the page never reads `page()`.
- Lesson from 2.1's review: every subscription that touches view state needs `takeUntilDestroyed`; DOM-level assertions beat component-state assertions for OnPush rendering bugs.
- The Angular `@for` + signal-read-inside-row-template gotcha from 2.1's review (a signal read inside a per-item template made `@for` treat the iterable as empty): keep per-row template expressions to plain item fields and pure methods; do not read page-level signals from inside the `@for` row.
- Deferred items this story resolves: `PageCursor` nullability (Decision Record) and the `/resumes/undefined` link for the *list* (not the dashboard widget).

### Git Intelligence Summary
- Baseline `f05a5f6` (merge of PR #95). Recent FE commits: `ee2a08d` (tag-input paste split), `6a93657` (Story 2.1), `553ee7a` (1.6 docs). Backend résumé CRUD shipped in the earlier backend phase; this is the Angular counterpart for the list endpoint.

### ⚠️ Trap 1 — `ResumeService.page()` is a single page, not an accumulated list
`list()` calls `pageSignal.set(page)` with the latest page only. Binding the UI to `page()` makes "Load more" *replace* the list. Accumulate in the page's own `items` signal.

### ⚠️ Trap 2 — Generated pagination fields are loosely typed
`totalCount`/`pageSize` are `number | string`; `lastSeenId`/`lastSeenUpdatedAt` are `null | string`. Coerce `totalCount` with `toCount`; build the cursor only through `nextCursor`. Never `as PageCursor`, and never send `'null'`/`'undefined'` as a query value (`HttpParams.set` stringifies whatever it is given).

### ⚠️ Trap 3 — Exact-URL `expectOne` misses querystring requests
`httpMock.expectOne('/resumes')` fails for `/resumes?pageSize=20`. Match by predicate.

### ⚠️ Trap 4 — Focus loss when Load more disappears
When the final page loads, `hasNext` becomes false and the button the user just activated is removed from the DOM, dropping keyboard focus to `<body>`. The focus-move requirement in Task 4 exists for this; test it at the DOM level.

### Open follow-up (needs a user decision, not part of this story)
After a successful create on `/resumes/new`, should the app navigate to `/resumes` (now a real list) instead of staying on the form? Story 2.1 chose to stay only because `/resumes` was a stub. If yes, it is a small change in `pages/resume-create` plus its spec — worth a one-line story or an addition to 2.3.

### References
- [Source: `_bmad-output/planning-artifacts/epics.md` — Story 2.2, lines 436-458; AR16 line 158; UX-DR12 line 176; UX-DR14 line 178; UX-DR16 line 180]
- [Source: `_bmad-output/planning-artifacts/prd-demo-mvp.md:332` (FR12), `:376` (FR41), `:383` (NFR2), `:389` (NFR5), `:397` (NFR10)]
- [Source: `_bmad-output/planning-artifacts/ux-design-specification.md:524` (`CursorPagination`), `:646-647` (skeletons, load-more)]
- [Source: `backend/src/JobNecto.API/Controllers/ResumesController.cs:57-92`; `backend/src/JobNecto.Application/Resumes/ListResumesHandler.cs`]
- [Source: `frontend/src/entities/resume/resume.service.ts`, `model.ts`, `resume-enums.ts`]
- [Source: `frontend/src/shared/api/pagination.ts`; `frontend/src/shared/api/generated/schema.ts:2090-2105` (`PagedResultOfResumeResult`), `:2131-2150` (`ResumeResult`)]
- [Source: `frontend/src/widgets/recent-resumes/recent-resumes.ts` — card/meta/skeleton/empty-state precedent]
- [Source: `frontend/src/shared/ui/state/{empty-state,error-state,skeleton}.ts`, `layout/page-header.ts`]
- [Source: `frontend/src/pages/dashboard/dashboard.page.ts` — `toCount`, independent-load pattern]
- [Source: `frontend/src/pages/resume-create/resume-create.page.ts` — `takeUntilDestroyed` pattern, header usage]
- [Source: `_bmad-output/archive/implementation-artifacts/2-1-create-a-resume.md`, `1-6-orientation-dashboard.md` — previous-story intelligence]
- [Source: `_bmad-output/implementation-artifacts/deferred-work.md` — `PageCursor` nullability, `/resumes/undefined`]

## Dev Agent Record

### Agent Model Used

Sonnet 5.5 (claude-sonnet-5-5)

### Debug Log References

- Baseline revision for the review diff: `a1839910db54e7fcbdde1efabf16d28355e74da7` (branch `story/2-2-browse-my-resumes`, the docs commit directly on top of `f05a5f6`).
- Backend (unchanged by this story, run once to confirm): `dotnet build backend/JobNecto.slnx --configuration Release --warnaserror` → `Build succeeded. 0 Warning(s) 0 Error(s)`. `dotnet test backend/JobNecto.slnx --configuration Release --no-build` → `Passed! - Failed: 0, Passed: 632, Skipped: 0, Total: 632`.
- Frontend baseline before changes: `npx ng test --no-watch` → 344 passing.
- Frontend after review patches: `cd frontend && npx ng test --no-watch` → `Test Files 37 passed (37)`, `Tests 422 passed (422)`, coverage `96.76% stmts / 94.39% branches / 97.63% lines`; after the PR-review (RV) patches: `Tests 429 passed (429)`, coverage `96.86% stmts / 94.69% branches / 97.71% lines`, no per-file threshold failures (including the newly un-excluded `shared/api/pagination.ts`).
- `cd frontend && npx ng build --configuration production` → `Application bundle generation complete.` with no errors or warnings (AOT compiles the new templates).

### Completion Notes List

- Added `nextCursor()` and the shared `toCount()` to `shared/api/pagination.ts`; the dashboard now imports `toCount`. `toCount` also now maps `null`/blank strings to `null` instead of `0` (found in review — a null `totalCount` would have rendered "0 résumés" beside a populated list); the dashboard inherits that strictly-safer behavior.
- New generic `ui-cursor-pagination` (button is `aria-disabled`, not `disabled`, so it keeps keyboard focus mid-request). Load-more failure reuses that one button relabelled "Try again" with a `role="alert"` message above it — an earlier version swapped in a separate error block, which dropped focus (review finding).
- Focus after a load-more goes to the first new card, or the last card if the final page added nothing new, and is skipped if the user has moved focus elsewhere while the request was in flight.
- Dashboard's header "New resume" action (not just the empty-state CTA) now targets `/resumes/new`; this was not in the written tasks, caught while updating the CTA, and is recorded as a task above.
- The `recent-resumes` / `resume-list` card duplication is intentional and logged in `deferred-work.md`, as the Decision Record requires.
- **Manual smoke NOT performed.** It needs a running API + PostgreSQL and a browser, which were not available in this session. Everything else is covered by unit/DOM-level specs and an AOT production build; the skeleton-versus-card height match and the narrow-viewport layout have therefore only been reasoned about, not eyeballed.
- Backend coverage gate (`scripts/check_coverage.py`) was not re-run — no backend file changed and `python` is not on this shell's PATH.

### File List

- `_bmad-output/implementation-artifacts/2-2-browse-my-resumes.md` (UPDATED — task ticks, Dev Agent Record)
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (UPDATED — 2-2 status)
- `_bmad-output/implementation-artifacts/deferred-work.md` (UPDATED — Story 2.2 deferrals)
- `frontend/angular.json` (UPDATED — `shared/api/pagination.ts` removed from `coverageExclude`)
- `frontend/src/shared/api/pagination.ts` (UPDATED — `nextCursor`, `toCount`)
- `frontend/src/shared/api/pagination.spec.ts` (CREATED)
- `frontend/src/shared/ui/pagination/cursor-pagination.ts` (CREATED)
- `frontend/src/shared/ui/pagination/cursor-pagination.spec.ts` (CREATED)
- `frontend/src/shared/ui/index.ts` (UPDATED — export `CursorPaginationComponent`)
- `frontend/src/widgets/resume-list/resume-list.ts` (CREATED)
- `frontend/src/widgets/resume-list/resume-card.ts` (CREATED — typed card body, extracted in PR review so the template is type-checked)
- `frontend/src/widgets/resume-list/resume-list.spec.ts` (CREATED)
- `frontend/src/widgets/resume-list/index.ts` (CREATED)
- `frontend/src/pages/resume-list/resume-list.page.ts` (CREATED)
- `frontend/src/pages/resume-list/resume-list.page.html` (CREATED)
- `frontend/src/pages/resume-list/resume-list.page.spec.ts` (CREATED)
- `frontend/src/app/app.routes.ts` (UPDATED — `resumes` route → `ResumeListPage`)
- `frontend/src/app/app.routes.spec.ts` (UPDATED — route wiring + `resumes/new` ordering tests)
- `frontend/src/pages/dashboard/dashboard.page.ts` (UPDATED — import shared `toCount`)
- `frontend/src/pages/dashboard/dashboard.page.html` (UPDATED — "New resume" → `/resumes/new`)
- `frontend/src/pages/dashboard/dashboard.page.spec.ts` (UPDATED — expected href)
- `frontend/src/widgets/recent-resumes/recent-resumes.ts` (UPDATED — empty-state CTA → `/resumes/new`)
- `frontend/src/widgets/recent-resumes/recent-resumes.spec.ts` (UPDATED — expected href)

### Review Findings

Four parallel lenses (Adversarial, Edge-case hunter, Verification gap, Acceptance) ran against the diff since the baseline; each finding was verified against the cited code before triage.

- [x] [Patch] Focus dropped when load-more failed or was retried (separate error block replaced the button) — `resume-list.page.html`: single pagination button relabelled "Try again"; `cursor-pagination.ts` uses `aria-disabled` so focus survives the loading state.
- [x] [Patch] Focus dropped when the last page contained only already-seen ids (button vanished, nothing new to focus) — `resume-list.page.ts`: focus the last card in that case.
- [x] [Patch] Focus could be stolen if the user moved focus while the request was in flight — focus now moves only if it is on `body` or inside the pagination control.
- [x] [Patch] `focusCard` located cards via the `data-testid` test hook — now a structural selector (`:scope > ul > li > *`).
- [x] [Patch] `toCount(null)` / `toCount('')` returned 0 — now `null`; tests added.
- [x] [Patch] Duplicate ids within one page (and on the first page) produced duplicate `@for` track keys; empty-string id collided under `??` — `uniqueById` helper on both paths; widget tracks `resume.id || $index`.
- [x] [Patch] Whitespace-only title rendered an empty heading/link; blank skills rendered empty chips and inflated the "+N" count — trimmed title fallback; blank skills dropped before capping.
- [x] [Patch] Long unbroken titles/skills could widen the grid — `min-w-0` / `break-words` / `break-all`.
- [x] [Patch] Rapid double Retry on first-load error could race two requests — `retry()` ignores clicks while loading.
- [x] [Patch] Header jumped when the subtitle appeared after load — subtitle line reserved with a non-breaking space while loading; skeleton cards use 4 lines.
- [x] [Patch] Test gaps — added: route wiring (`resumes` → `ResumeListPage`) and `resumes/new` ordering, announcement null-total/empty/after-load-more, id-less items on a later page, within-page dedupe, server-order preservation with non-sorted ids, `aria-busy`/`aria-disabled`/`role=alert` assertions, `Sep 28, 2026` date format, focus-unchanged on out-of-range `focusCard`, `httpMock.verify()` after each page spec.
- [x] [Dismiss] Task 6 "unmet" (acceptance lens) — false; the deferred-work entry was written after the reviewed diff was staged.
- [x] [Dismiss] Widget lacks a `totalCount` input (acceptance lens) — nothing needs it; story text amended.
- [x] [Dismiss] No unit test for skeleton-vs-card height (acceptance lens) — not measurable in jsdom; covered by the manual smoke, which was not run.
- [x] [Dismiss] First-load `401` briefly flashes the error state — the global interceptor is already redirecting to sign-in; not worth a special case.
- [x] [Dismiss] `humanizeEnumMember` does not split `Full_Time` / `HRManager` — depends on backend enum values; none today.
- [ ] [Defer] A `200` with a malformed body (`null`, `{}`, `items: null`) leaves the skeleton up forever (first load) or the button stuck on "Loading…" (load-more) — logged to `deferred-work.md`; same exposure exists on the dashboard.
- [ ] [Defer] `429` / a permanent `400` (stale cursor) on load-more is retried identically forever with no reset-to-page-1 — logged.
- [ ] [Defer] `totalCount` is overwritten per page, so rows added/removed mid-paging can read "Showing 25 of 24" — logged.
- [ ] [Defer] `updatedAt` through `DatePipe` throws on an unparsable string and would blank the list — logged.
- [ ] [Defer] `hasNext: true` with a half-populated cursor ends the list silently (no error/retry) — logged.

## PR Review (jobnecto-qa, RV) — 2026-10-21

Target: `story/2-2-browse-my-resumes` vs `origin/master` (`f05a5f6`). Lenses run: correctness, regression, architecture & quality, test coverage (edge-cases were covered by the earlier self-review lens, so not repeated). Tests: backend 632/632 (unchanged, Release build 0 warnings); frontend 429/429 after patches, per-file coverage gate clean, AOT production build clean.

Regression lens: **no findings** (every caller of `toCount`, the `/resumes` route, the `/resumes/new` links, the barrels and the `coverageExclude` change traced). Correctness lens: no high/medium. Findings and disposition:

- [x] [Patch] **medium** — `ng-template let-resume` was untyped (`any`) under `strictTemplates`, duplicating the card shell: extracted `widget-resume-card` with a typed required input and `computed` view-model (`resume-card.ts`).
- [x] [Patch] **medium** — Focus moved off the Load more button to the first new card after *every* page, forcing keyboard users to tab through ~20 links to page again: focus now moves only when the button disappears (last page), else stays put.
- [x] [Patch] **medium** — Focus dropped to `<body>` when first-load Retry destroyed its own button: focus moves to the loading region.
- [x] [Patch] **medium** — "+N" overflow chip was read as a skill: now `aria-hidden` "+N" plus sr-only "and N more skills", own test id.
- [x] [Patch] **minor** — token violations (`rounded-full`→`rounded-pill`, `py-0.5`→`py-1`); skeleton `role=status` had no text (sr-only "Loading résumés…" added, redundant height dropped); hover feedback on an `aria-disabled` button; "Showing 1 of 1 résumés" grammar; missing doc comments; nested ternary replaced; prettier applied to files this PR created.
- [x] [Patch] **medium (tests)** — AC3 layout untested (skeleton count/grid parity now asserted), `aria-disabled`-not-`disabled` untested, the focus-while-button-survives branch untested, one failing page test cascaded into dozens (`afterEach` now `try/finally` + stray-node cleanup), a timezone-dependent date assertion (now computed with the same local formatting), fixtures redundantly appended to `document.body`, `nextCursor` with omitted fields.
- [x] [Dismiss] `toCount` still accepts `[]`/`false`/negatives — input is typed `number | string` from an int32; no reachable impact.
- [x] [Dismiss] `retry()` re-entry guard is unreachable from the UI — harmless defensive code, and tested directly.
- [ ] [Defer] `aria-busy` on a button is ignored by most AT (harmless, kept). Page/widget use DOM queries (`querySelector`, `:scope > ul > li > *`) rather than Angular queries. The whole-card link has a verbose accessible name (matches the dashboard pattern). Server silently returns page 1 when the cursor row was edited/soft-deleted since load, so a load-more can add nothing new with no feedback. All logged to `deferred-work.md`.

## Change Log

- 2026-10-21: Story created (ready-for-dev). Baseline `f05a5f6`.
- 2026-10-21: PR review (RV) run with four lenses; 6 patch groups applied, 2 dismissed, 4 deferred. Frontend 429/429.
- 2026-10-21: Implemented all tasks test-first; self-reviewed with four parallel lenses; 11 findings patched, 5 deferred, 5 dismissed. Frontend 422/422, backend 632/632, AOT production build clean. Manual smoke not run. Status set to `review`.
