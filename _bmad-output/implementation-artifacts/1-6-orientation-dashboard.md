# Story 1.6: Orientation dashboard

Status: ready-for-dev

## Story

As a **first-time user**,
I want **a dashboard that orients me**,
so that **I know what to do next without instructions**.

<!-- Track: [FE] — Covers: FR37, FR40, NFR2, NFR10, AR11, AR12, AR16, UX-DR12, UX-DR14, UX-DR15, UX-DR18 -->

> **Scope note.** This is the last unblocked slice of Epic 1 (1.7 is gated on product sign-off + a data audit). It replaces the `DashboardPage` stub that Story 1.1 shipped and 1.5 re-parented into the shell. It also introduces the **first two data-fetching entity services after `UserService`** (`ResumeService`, `EducationService`) — Epic 2 (2.2 Browse my résumés) and Epic 6 (6.3 Education records) build directly on them, so their shape matters beyond this story. Keep them thin and generic; do **not** bake dashboard-specific concerns into them.

## Decision Record

- 2026-09-26: **Profile is read from the already-hydrated `UserService.profile` signal, not refetched on every dashboard load.** `authGuard` (Story 1.4) calls `restoreSession()` → `fetchCurrentUser()` before the shell route activates, so by the time `DashboardPage` constructs, the profile signal is populated on every normal entry. The dashboard therefore treats profile as *already loaded*, and only issues `fetchCurrentUser()` when `profile()` is `null` (a defensive cold path). A literal reading of the AC says "fetches profile … in parallel"; refetching a profile the guard just fetched is a wasted round-trip for zero user benefit. The **observable behavior the AC asks for is preserved**: the completeness widget still has loading and error states for the cold path, and a profile failure still degrades only that widget. See AC 2 and AC 6.
- 2026-09-26: **The "Cover Letters" stat tile renders the reference mock's `—` placeholder rather than a fourth fetch.** `frontend/design-examples/Populated.png` shows exactly this: three live counts and a dashed rule under COVER LETTERS. `GET /api/v1/cover-letters` exists, but the AC scopes the parallel fetch to profile + résumés + educations, and Epic 5 owns the cover-letter surface. A `—` with an accessible label is honest; a fourth fetch is scope creep.
- 2026-09-26: **The mock's black "TIP · 01 … Read playbook" banner is omitted.** Its CTA has no destination in the Demo MVP, and shipping a primary-styled button that goes nowhere contradicts the PRD's no-dead-ends bar (`prd-demo-mvp.md` §Success Criteria item 6). The tip is decorative copy, not an AC. If it returns later it is Story 7.1's canonical placeholder treatment, not a live button.
- 2026-09-26: **The checklist item for email is labelled "Add email", not the mock's "Verify email".** No email-verification flow exists anywhere in the backend; the check is purely "is the field present". Labelling a presence check as verification would be a false claim in the UI.
- 2026-09-26: **Profile completeness is a fixed six-field formula** over `GetCurrentUserResult`: `loginName`, `email`, `phone`, `location`, `about`, `avatar`. Percentage = `round(filled / 6 × 100)`. This reproduces the reference mock exactly (5 of 6 filled = 83%), which confirms the mock was drawn against these same six fields.

## Acceptance Criteria

1. **Orientation on arrival**
   **Given** I am authenticated and land on `/dashboard`
   **When** the page loads
   **Then** it renders, inside the 1.5 shell, a `PageHeaderComponent` greeting me by login name, a resource-count row, an onboarding/profile-completeness checklist, a "Recent resumes" panel, and quick links into key tasks (FR37, UX-DR18)
   **And** the page contains exactly one `<h1>` (NFR10)

2. **Parallel, independent loads**
   **Given** the dashboard is loading
   **When** it requests its data
   **Then** the résumé and education first pages are requested **concurrently** (neither waits on the other), and profile comes from the hydrated `UserService.profile` signal — or, only when that signal is `null`, from a concurrent `GET /api/v1/users/me` (UX-DR18)
   **And** each still-loading region shows a layout-preserving `ui-skeleton`, never a spinner over the whole page (NFR2, UX-DR12)

3. **Partial failure never breaks the page**
   **Given** exactly one of the loads fails (any non-2xx, or a network error)
   **When** the dashboard renders
   **Then** only that region shows a `ui-error-state` with a working **Retry** that refetches that one source, and every other region renders its real content normally (UX-DR18, FR40)
   **And** the same holds when two of the three fail — the third still renders

4. **Counts reflect the real totals**
   **Given** my résumé and education first pages have loaded
   **When** the count row renders
   **Then** ACTIVE RESUMES and EDUCATION show `totalCount` from each paged response (not `items.length`, which is capped by page size), and COVER LETTERS shows a non-numeric `—` placeholder with an accessible "not available yet" label (AR16)

5. **Recent résumés**
   **Given** I have résumés
   **When** the panel renders
   **Then** it lists up to **five** most-recently-updated résumés (the server already orders `UpdatedAt DESC, Id DESC`), each a working link to `/resumes/:id`, showing title plus experience and work-location when present

6. **Brand-new user is nudged, never shamed**
   **Given** I am brand new with no résumés
   **When** the dashboard renders
   **Then** the Recent-resumes panel shows a `ui-empty-state` ("No resumes yet") whose CTA links to `/resumes`, the checklist frames remaining items as helpful next steps (no guilt, no shame, no red), and the count row shows `0` — not an error (UX-DR18)

7. **Completeness checklist**
   **Given** my profile is loaded
   **When** the checklist renders
   **Then** it lists the six fields from the Decision Record, each marked done or not-done with **text plus shape, never colour alone**, and shows a percentage plus a progress bar carrying `role="progressbar"` with `aria-valuenow`/`aria-valuemin`/`aria-valuemax` (NFR10, UX-DR18)
   **And** every not-done item links to `/profile`

8. **Quick links and page actions**
   **Given** the dashboard has rendered
   **When** I look at the header and widgets
   **Then** there is **exactly one** near-black primary action ("New resume" → `/resumes`), with "Add education" (→ `/educations`) as a secondary, and every quick link resolves to a rendered page inside the shell — today most are the 1.5 `ComingSoonStubPage`, which is the intended honest destination, never a dead end (UX-DR14, UX-DR15, FR38)

9. **Accessibility floor**
   **Given** any dashboard state (loading, loaded, partial failure, empty)
   **When** a keyboard user traverses the page
   **Then** heading order is logical under the single `h1`, every link/button is reachable with a visible focus ring, error regions announce via the `ui-error-state` `role="alert"`, and no state is conveyed by colour alone (NFR10, UX-DR17)

## Tasks / Subtasks

- [ ] **Task 1 — `resume` entity slice (AC: 2, 4, 5)**
  - [ ] `frontend/src/entities/resume/model.ts` (CREATE) — re-export from the generated schema, exactly as `entities/user/model.ts` does: `export type ResumeResult = components['schemas']['ResumeResult'];` and `export type PagedResumes = components['schemas']['PagedResultOfResumeResult'];`. Do **not** hand-write these shapes — they are already generated.
  - [ ] `frontend/src/entities/resume/resume.service.ts` (CREATE) — `@Injectable({ providedIn: 'root' })`, `inject(HttpClient)`. One method: `list(pageSize = 20, cursor?: { lastSeenId: string; lastSeenUpdatedAt: string }): Observable<PagedResumes>` issuing `GET /resumes` with `HttpParams`. Keep it generic — cursor support is here for Story 2.2, the dashboard just passes `pageSize: 5`. Hold the last successful page in a private `signal<PagedResumes | null>(null)` exposed read-only, with an `invalidate()` matching `UserService`'s thin-cache convention (AR11 Decision 1.3).
  - [ ] `frontend/src/entities/resume/index.ts` (CREATE) — barrel re-exporting `./model` and `./resume.service`, mirroring `entities/user/index.ts`.
- [ ] **Task 2 — `education` entity slice (AC: 2, 4)**
  - [ ] `frontend/src/entities/education/model.ts` (CREATE) — `EducationResult`, `PagedEducations` (`PagedResultOfEducationResult`) aliased from the generated schema.
  - [ ] `frontend/src/entities/education/education.service.ts` (CREATE) — same shape as Task 1 against `GET /educations`.
  - [ ] `frontend/src/entities/education/index.ts` (CREATE) — barrel.
  - [ ] `frontend/angular.json` (UPDATE) — add `"src/entities/resume/model.ts"` and `"src/entities/education/model.ts"` to `coverageExclude`, directly beside the existing `"src/entities/user/model.ts"` entry. **This is required, not optional** — see Trap 7.
- [ ] **Task 3 — `ResourceCountsComponent` widget (AC: 4, 6, 9)**
  - [ ] `frontend/src/widgets/resource-counts/resource-counts.ts` (CREATE) — `OnPush` standalone. Inputs: `resumeCount: number | null`, `educationCount: number | null`, `resumesLoading`, `educationsLoading`, `resumesFailed`, `educationsFailed` (booleans). Four tiles: PROFILE COMPLETE (%), ACTIVE RESUMES, EDUCATION, COVER LETTERS. Mono uppercase eyebrow over a large numeral, per `frontend/design-examples/Populated.png`. A loading tile renders `ui-skeleton`; a failed tile renders a short "unavailable" text (the *panel* owns the retry, not the tile — keeps one retry per source); COVER LETTERS renders `—` with `<span class="sr-only">not available yet</span>`.
  - [ ] `frontend/src/widgets/resource-counts/index.ts` (CREATE) — barrel.
- [ ] **Task 4 — `ProfileCompletenessComponent` widget (AC: 7, 9)**
  - [ ] `frontend/src/widgets/profile-completeness/profile-completeness.ts` (CREATE) — `OnPush` standalone. Input: `profile: UserProfile | null`. A `computed()` derives the six-item checklist `{ label, done, route }[]` and the percentage per the Decision Record. Template: mono eyebrow "ONBOARDING CHECKLIST", an `h2` "Finish your profile", a progress bar with the full `role="progressbar"` ARIA set, then the list. Done items carry a filled glyph **and** `line-through` **and** an `sr-only` "done"; not-done items carry a hollow glyph **and** an `sr-only` "not done" **and** a `routerLink` to `/profile`.
  - [ ] `frontend/src/widgets/profile-completeness/index.ts` (CREATE) — barrel.
- [ ] **Task 5 — `RecentResumesComponent` widget (AC: 3, 5, 6, 9)**
  - [ ] `frontend/src/widgets/recent-resumes/recent-resumes.ts` (CREATE) — `OnPush` standalone. Inputs: `resumes: ResumeResult[] | null`, `loading: boolean`, `failed: boolean`. Output: `retry`. Renders, in priority order: `failed` → `ui-error-state` (`headline="Couldn't load your resumes"`, emitting `retry`); `loading` → `ui-skeleton`; empty → `ui-empty-state` (`headline="No resumes yet"`, guidance + a secondary CTA `routerLink="/resumes"`); otherwise up to five cards, each `routerLink="/resumes/{{ id }}"`, title with a fallback for `null | undefined`, and an `experience · workLocationType` meta line that omits absent parts cleanly (no stray `·`).
  - [ ] `frontend/src/widgets/recent-resumes/index.ts` (CREATE) — barrel.
- [ ] **Task 6 — Rewrite `DashboardPage` (AC: 1, 2, 3, 6, 8)**
  - [ ] `frontend/src/pages/dashboard/dashboard.page.ts` (UPDATE — replaces the 1.1 stub body wholesale) — move the template out to a `.html` file (the sign-in page's convention for non-trivial pages). Own three independent state triples (`data`, `loading`, `error`) as signals, one per source: profile, résumés, educations. Kick the résumé and education loads in the constructor via two **separate** `subscribe()` calls so they are concurrent and independently failable. Expose `retryResumes()` / `retryEducations()` / `retryProfile()`.
  - [ ] `frontend/src/pages/dashboard/dashboard.page.html` (CREATE) — `ui-page-header` with `eyebrow="Welcome back"`, the `h1` greeting (`Hello, <span class="font-serif italic text-brand-accent">{{ loginName }}</span>.`), subtitle, and a `primaryAction` slot holding the secondary "Add education" + the single near-black primary "New resume". Then the three widgets in the mock's layout: count row full width; checklist and Recent resumes side by side at `lg+`, stacked at `xs/sm`.
- [ ] **Task 7 — Verification and test coverage (AC: all)**
  - [ ] `frontend/src/entities/resume/resume.service.spec.ts` (CREATE) — `HttpTestingController` per `entities/user/user.service.spec.ts`: `list()` GETs `/resumes`, sends `pageSize`, sends both cursor params when given and neither when not, sets `withCredentials`, caches the page in the signal, and `invalidate()` clears it.
  - [ ] `frontend/src/entities/education/education.service.spec.ts` (CREATE) — the same scenarios against `/educations`.
  - [ ] `frontend/src/widgets/resource-counts/resource-counts.spec.ts` (CREATE) — renders `totalCount` values; `0` renders as `0` and not as an empty/error tile; loading renders a skeleton; failed renders the unavailable text; COVER LETTERS renders `—` plus its `sr-only` label.
  - [ ] `frontend/src/widgets/profile-completeness/profile-completeness.spec.ts` (CREATE) — 6-of-6 → 100%; 5-of-6 (avatar missing) → **83%** (the reference-mock case); 0-of-6 → 0% with no shaming copy; `null` profile renders the loading branch; the progressbar carries `aria-valuenow` matching the percentage; every not-done item has a `/profile` link; done-ness is exposed as text, not colour only.
  - [ ] `frontend/src/widgets/recent-resumes/recent-resumes.spec.ts` (CREATE) — error branch renders `ui-error-state` and clicking Retry emits `retry`; loading renders a skeleton; `[]` renders the empty state with a `/resumes` CTA; six résumés render **five** cards; a résumé with `title: null` renders the fallback; a résumé with no `experience`/`workLocationType` renders no stray separator.
  - [ ] `frontend/src/pages/dashboard/dashboard.page.spec.ts` (UPDATE — the existing stub-page spec must be rewritten; its `{ provide: UserService, useValue: { profile } }` stub no longer satisfies the page) — assert: exactly one `h1`; both list requests are in flight **before either is flushed** (this is the AC 2 concurrency proof — `httpMock.match()` returns two open requests); flushing résumés with a `500` while educations succeed leaves the education content rendered and only the résumé region in error (AC 3); the reverse case; both failing still renders the checklist; Retry on the résumé region issues a second `GET /resumes`; a `null` cached profile triggers `fetchCurrentUser()` and a populated one does not.
  - [ ] `cd frontend && npx ng test --no-watch` — all green, per-file coverage ≥80% on every new file.
  - [ ] `dotnet build backend/JobNecto.slnx --configuration Release --warnaserror` — untouched by this story, but run once to confirm the tree is clean before handing off.

## Dev Notes

### Technical Requirements

- **Feature-sliced placement is not optional** (AR11 / FE Guide §2.2): generic primitives in `shared/ui`, API-bound domain state in `entities/<name>`, composed domain UI in `widgets/<name>`, route-level composition in `pages/<name>`. A widget must not call `HttpClient` directly — it takes inputs and emits outputs; the page owns the fetching.
- **Signals + services only.** No NgRx, no TanStack Query, no `async` pipe over a shared subject. Follow `UserService`: a private `signal`, an `asReadonly()` view, `computed()` for derived state (AR11 Decision 1.2/1.3).
- **`ChangeDetectionStrategy.OnPush` on every new component**, matching every existing component in the codebase.
- **Tokens only** — Tailwind classes mapped to the Career-OS tokens (`bg-bg-surface`, `text-text-primary`, `text-text-secondary`, `text-brand-accent`, `border-border-default`, `bg-action-primary`, …). Never a raw hex or px colour (`shared/config/tokens.ts` header).
- **Reuse, do not rebuild:** `PageHeaderComponent`, `SkeletonComponent`, `EmptyStateComponent`, `ErrorStateComponent` all exist and are exported from `@shared/ui`. This story should add **no** new `shared/ui` primitives.

### API / Contract Guardrail

- `GET /api/v1/resumes?pageSize=&lastSeenId=&lastSeenUpdatedAt=` → `200 PagedResultOfResumeResult` | `400` | `401`. Owner-scoped by the auth cookie; there is no user-id parameter.
- `GET /api/v1/educations?pageSize=&lastSeenId=&lastSeenUpdatedAt=` → `200 PagedResultOfEducationResult` | `400` | `401`.
- `GET /api/v1/users/me` → `200 GetCurrentUserResult` | `401`.
- Paged envelope (AR16): `{ items, totalCount, lastSeenId, lastSeenUpdatedAt, pageSize, hasNext, totalPages }`. Server orders `UpdatedAt DESC, Id DESC` (`backend/src/JobNecto.Infrastructure/Repositories/BaseRepository.cs:63`) — **"most recent" needs no client-side sort.** `pageSize` defaults to 20 and is capped at 100 server-side (`ListResumesHandler.cs:29`, `ListEducationsQueryHandler.cs:33`); `pageSize: 5` is well inside that.
- URLs passed to `HttpClient` are **relative** (`/resumes`). The interceptor prefixes `env.apiBaseUrl` and sets `withCredentials` (AR12). Never build an absolute URL in a service.
- A `401` on these calls must **not** be marked `SKIP_AUTH_REDIRECT`. A 401 here is a genuine session lapse and should reach `authRedirectInterceptor` (Story 1.4) for the re-auth bounce — the opposite of `signIn()`'s deliberate opt-out.

### ⚠️ Trap 1 — `forkJoin` will break AC 3

The obvious way to "fetch in parallel" is `forkJoin([resumes$, educations$])`. **It fails the story**: `forkJoin` errors the whole stream the instant either source errors, so one failing widget takes down the other. Use two independent `subscribe()` calls (simplest, and the pattern the page's per-source signals already want). If you insist on combining, every inner stream needs its own `catchError(() => of(FAILED_MARKER))` *before* the combinator — at which point the combinator buys nothing.

### ⚠️ Trap 2 — `totalCount` is typed `number | string`

The OpenAPI generator emits `totalCount: number | string` (and the same for `pageSize`/`totalPages`) in `PagedResultOfResumeResult`. Binding it straight into a numeric input or comparing it with `===  0` will not typecheck or will misbehave. Coerce once at the page boundary (`Number(page.totalCount)`) and hand the widget a real `number`. The same generator quirk makes almost every `ResumeResult` property optional (`title?: null | string`) — the template needs fallbacks, not non-null assertions.

### ⚠️ Trap 3 — the existing `dashboard.page.spec.ts` will pass while testing nothing

It currently provides `{ provide: UserService, useValue: { profile } }` — a partial stub with no `fetchCurrentUser`. Once the page can call that method the stub silently breaks, and the tempting fix is to bolt another fake onto the object. Rewrite the spec instead, using `provideHttpClient(withInterceptors([httpInterceptor])) + provideHttpClientTesting()` like `user.service.spec.ts` does, so the concurrency and partial-failure assertions test real request behavior rather than a hand-made mock.

### ⚠️ Trap 4 — `NG0100` from mutating a shared test host

Story 1.5 hit this: binding inputs on one shared host component and changing them after the first `detectChanges()` throws `ExpressionChangedAfterItHasBeenCheckedError` in this Angular 21 setup. Use a separate host (or set the input before the first `detectChanges()`) per scenario. This will bite hardest in `profile-completeness.spec.ts`, which naturally wants to sweep several profiles through one fixture.

### ⚠️ Trap 5 — `expectOne` with query params

`httpMock.expectOne('/resumes')` will **not** match a request carrying `?pageSize=5`. Match on a predicate (`httpMock.expectOne((r) => r.url === \`${env.apiBaseUrl}/resumes\`)`) and assert `req.request.params.get('pageSize')` separately. For the AC 2 concurrency assertion use `httpMock.match(...)` twice *before* flushing either, then flush — that ordering is the proof the requests overlapped.

### ⚠️ Trap 6 — "quick links" land on stubs, and that is correct

`/resumes`, `/educations`, `/resumes/:id` all currently resolve to `ComingSoonStubPage` (Story 1.5, `app.routes.ts`). Do **not** disable the CTAs, hide them, or add a placeholder treatment of your own. 1.5 registered those routes precisely so these links resolve; Story 7.1 owns the canonical `ComingSoonPlaceholder` and 7.2 owns the final sweep. Shipping working links to honest stubs is the intended state.

### ⚠️ Trap 7 — the two new `model.ts` files will fail the coverage gate unless excluded

`frontend/angular.json` → `coverageExclude` already lists `"src/entities/user/model.ts"` by name (line 84). That file is pure type aliases over the generated schema — it has no runtime statements to cover, so the per-file ≥80% gate cannot be satisfied and it had to be named explicitly. `entities/resume/model.ts` and `entities/education/model.ts` are exactly the same kind of file and need the same treatment. The pattern `src/**/index.ts` covers the barrels automatically; `model.ts` is **not** covered by any glob. Skipping this fails CI with a coverage error that looks nothing like its cause.

### Per-file coverage gate

Every other file in this story is hand-written with real runtime behavior and is in the gate (`frontend/angular.json` → `coverageThresholds.perFile`, ≥80% lines and statements). Barrel `index.ts` files are excluded by glob; the services, widgets, and page are not. A widget with an untested `failed` or empty branch is the usual way this gate fails.

### File Structure Requirements

- `frontend/src/entities/resume/model.ts` (CREATE)
- `frontend/src/entities/resume/resume.service.ts` (CREATE)
- `frontend/src/entities/resume/resume.service.spec.ts` (CREATE)
- `frontend/src/entities/resume/index.ts` (CREATE)
- `frontend/src/entities/education/model.ts` (CREATE)
- `frontend/src/entities/education/education.service.ts` (CREATE)
- `frontend/src/entities/education/education.service.spec.ts` (CREATE)
- `frontend/src/entities/education/index.ts` (CREATE)
- `frontend/src/widgets/resource-counts/resource-counts.ts` (CREATE)
- `frontend/src/widgets/resource-counts/resource-counts.spec.ts` (CREATE)
- `frontend/src/widgets/resource-counts/index.ts` (CREATE)
- `frontend/src/widgets/profile-completeness/profile-completeness.ts` (CREATE)
- `frontend/src/widgets/profile-completeness/profile-completeness.spec.ts` (CREATE)
- `frontend/src/widgets/profile-completeness/index.ts` (CREATE)
- `frontend/src/widgets/recent-resumes/recent-resumes.ts` (CREATE)
- `frontend/src/widgets/recent-resumes/recent-resumes.spec.ts` (CREATE)
- `frontend/src/widgets/recent-resumes/index.ts` (CREATE)
- `frontend/src/pages/dashboard/dashboard.page.ts` (UPDATE — stub replaced)
- `frontend/src/pages/dashboard/dashboard.page.html` (CREATE — template extracted)
- `frontend/src/pages/dashboard/dashboard.page.spec.ts` (UPDATE — rewritten)
- `frontend/angular.json` (UPDATE — two `coverageExclude` entries, see Trap 7)

No backend file changes. No new npm dependency: everything needed (`@angular/common/http`, `@angular/router`, CDK) is already installed.

### Testing Requirements

- Vitest + `@angular/core/testing` `TestBed`, the pattern in `frontend/src/entities/user/user.service.spec.ts` and `frontend/src/pages/dashboard/dashboard.page.spec.ts`.
- Service specs: `provideHttpClient(withInterceptors([httpInterceptor]))` + `provideHttpClientTesting()`, then `httpMock.verify()` at the end of each test.
- Widget specs: pure input/output tests with no HTTP — that is the payoff of keeping fetching in the page.
- Assert accessibility contracts directly (`querySelectorAll('h1')).toHaveLength(1)`, `getAttribute('aria-valuenow')`, presence of `sr-only` text) rather than trusting them by inspection — Story 1.5's review found four verification gaps of exactly this kind.

### Previous Story Intelligence (1.5)

- The shell (`widgets/app-shell/app-shell.ts`) already provides `<main id="main-content" class="mx-auto max-w-[1040px] flex-1 px-4 py-8">`. **Do not add your own page container, max-width, or padding** — the dashboard renders inside that.
- `PageHeaderComponent` (`shared/ui/layout/page-header.ts`) takes `eyebrow` (required) and `subtitle` (optional), projects the `h1` content in the default slot, and takes a `[primaryAction]`-selected slot for top-right actions. It renders the page's only `h1`, so every heading you add must be `h2` or lower.
- 1.5's review caught a real bug worth not repeating: `routerLinkActive` only *adds* classes, so a base `text-text-secondary` and an active `text-text-primary` coexisted and the winner depended on stylesheet order. If any dashboard element has mutually exclusive visual states, bind them as mutually exclusive (`[class.x]="cond"` / `[class.y]="!cond"`), don't layer them.
- `_bmad-output/agent-learnings.md` was checked and has **no entry applicable to this story** — every logged lesson is backend (EF snapshots, xUnit APIs, C# enum/LINQ traps) from Phases B/C. The frontend equivalents live as Traps inside the Story 1.1–1.5 files; the ones that still bite are reproduced as Traps 3–6 above.
- Two items were deferred from 1.5 and this story must **not** regress them: `authGuard` is on the shell parent route and does not re-run on in-shell sibling navigation, and the drawer's open state is not reset across a breakpoint resize. Neither is this story's to fix; just don't build on an assumption that the guard re-validates per child route.

### Git Intelligence Summary

- `fcd6a5a` / `28f05c8` — Story 1.5 implementation and its self-review fixes; the shell, `PageHeaderComponent`, `ComingSoonStubPage`, and the full route table land here. Read `app-shell.ts` before styling anything.
- `325084d` / `34a3de2` — Story 1.4: `authGuard`, `authRedirectInterceptor`, `restoreSession()`. This is why the profile is already hydrated on dashboard entry.
- `f9b2710` (Story 1.1) — the original `DashboardPage` stub, `UserService`, the token layer, and the `shared/ui` state components this story consumes.

### Latest Stack Information

- Angular `^21.2.0` (standalone components, signals, `input()`/`output()`, `@if`/`@for` control flow), `@angular/cdk ^21.2.0`, TypeScript `~5.9.2`, Tailwind `^3.4.17`, Vitest `^4.0.8` (`frontend/package.json`).
- Frontend gate: `cd frontend && npx ng test --no-watch`, per-file ≥80% (`AGENTS.md` → Test coverage policy).

### References

- [Source: `_bmad-output/planning-artifacts/epics.md` - Story 1.6: Orientation dashboard]
- [Source: `_bmad-output/planning-artifacts/epics.md` - UX-DR18 (Dashboard orientation), UX-DR14/15/17, AR11, AR12, AR16]
- [Source: `_bmad-output/planning-artifacts/prd-demo-mvp.md` - FR37, FR38, FR40, NFR10]
- [Source: `_bmad-output/planning-artifacts/architecture/demo-mvp-architecture-decisions.md` - Decision 1.1-1.3 (stack, signals, thin server-state cache), Decision 1.6 (routing)]
- [Source: `_bmad-output/planning-artifacts/ux-design-specification.md` - UX Consistency Patterns (button hierarchy, feedback, empty/loading states), Responsive Strategy]
- [Source: `frontend/design-examples/Populated.png` and `Partial failure.png` - the canonical dashboard layout and its sparse variant; `Mobile _ dashboard.png` for the `xs/sm` stack]
- [Source: `frontend/src/shared/api/generated/schema.ts` - `GetCurrentUserResult`, `ResumeResult`, `EducationResult`, `PagedResultOfResumeResult`, `PagedResultOfEducationResult`]
- [Source: `backend/src/JobNecto.Infrastructure/Repositories/BaseRepository.cs:63` - `UpdatedAt DESC, Id DESC` ordering]
- [Source: `backend/src/JobNecto.Application/Resumes/ListResumesHandler.cs:29` - page-size default 20 / cap 100]
- [Source: `_bmad-output/archive/implementation-artifacts/1-5-application-shell-navigation.md` - shell contract, `PageHeaderComponent` API, review findings]
- [Source: `_bmad-output/implementation-artifacts/deferred-work.md` - Story 1.5 and 1.4 deferrals this story must not regress]
- [Source: `AGENTS.md` - coverage policy, story completion checklist, namespace/documentation conventions]

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List

## Change Log

- 2026-09-26: Story created (ready-for-dev). ACs carried from `epics.md` §Story 1.6 and sharpened in three places, each recorded above: the parallel-fetch AC now states that profile comes from the hydrated signal with a cold-path fetch (Decision Record); the count AC now specifies `totalCount` over `items.length` and pins the cover-letter tile to a placeholder; the empty-state AC now names the concrete widget and CTA target. AC 4, 5, 8 and 9 were added to make the mock's count row, five-item recency cut, single-primary rule, and a11y floor testable rather than implied.
