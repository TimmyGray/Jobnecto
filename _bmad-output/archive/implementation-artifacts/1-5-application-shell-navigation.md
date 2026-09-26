# Story 1.5: Application shell & navigation

Status: done

## Story

As a **signed-in user**,
I want **a consistent navigation shell**,
so that **I can move around the product and always know where I am**.

<!-- Track: [FE] — Covers: FR6, NFR2, NFR4, NFR9, NFR10, AR11, AR13, UX-DR14, UX-DR15, UX-DR16, UX-DR17 -->

> **Scope note.** This story delivers the **shell infrastructure and the full guarded route table** (`AR13`): every route in the FE-Guide's route list gets registered and guarded now, even though most of their real pages are still backlog (Epics 2–6). A route whose feature isn't built yet renders a minimal, generic "coming soon" stub — this is **not** the canonical `ComingSoonPlaceholder` (`UX-DR9`) system; that dashed/hatched, mono-pill, card-stub/full-panel treatment is Story 7.1's job and this stub is deliberately bare-bones so it can be swapped wholesale without ceremony. Building it now (rather than leaving unbuilt routes unregistered) is required by the PRD's explicit MVP acceptance bar: "every unbuilt feature shows a consistent, unmistakable 'coming soon' placeholder (never an error or dead end)" (`prd-demo-mvp.md` §Success Criteria item 6) — an authenticated user clicking a sidebar item must never be bounced out to `/sign-in` by the wildcard route. Story 7.2 ("no dead ends navigation sweep") still owns auditing this exhaustively once every real page exists; this story only guarantees the route table itself has no gaps.

## Decision Record

- 2026-09-26: **Guard moves to the shell's parent route, not each child** — `authGuard` (Story 1.4) is applied once on a new parent route (`AppShellComponent`, path `''`) that hosts every authenticated route as a child. This is the standard Angular pattern for a shared authenticated layout and avoids re-registering `canActivate: [authGuard]` on eleven separate route entries. `/sign-up` and `/sign-in` stay top-level, outside the shell.
- 2026-09-26: **Off-canvas mobile nav uses `@angular/cdk/overlay` + `@angular/cdk/a11y`, not spartan-ng** — `text-field.ts`'s own header comment records "no spartan-ng yet — deferred to the first story needing overlay/select" (Decision 1.4). This story is the first to need an overlay (the `xs/sm` drawer), but introducing a new UI-kit dependency for one drawer is disproportionate scope for this story. `@angular/cdk` (`^21.2.0`) is already a dependency and ships `Overlay`/`OverlayModule` and `CdkTrapFocus` directly — sufficient for a focus-trapped, `Esc`-dismissible, backdrop-closing drawer per `UX-DR16`/`UX-DR17`. The spartan-ng-vs-approved-fallback decision stays open for the first story that actually needs a rich primitive (select, combobox) — not reopened here.
- 2026-09-26: **A single reusable `ComingSoonStubPage` reads its copy from route `data`** rather than one component per unbuilt route — six routes (`/profile`, `/resumes`, `/resumes/:id`, `/educations`, `/educations/:id`, `/settings`, `/vacancies`, `/vacancies/:id`, `/cover-letters`, `/cover-letters/:id`) need near-identical placeholder content differing only by title. `route.snapshot.data['title']` (static route data, no fetch) keeps this to one component and one spec file instead of ten.

## Acceptance Criteria

1. **Sidebar navigation on desktop**
   **Given** I am authenticated
   **When** any guarded page renders at `lg`/`xl`
   **Then** a fixed left sidebar shows Dashboard, Profile, Resumes, Education, Vacancies, Cover Letters, Settings, in that order, each a working link to its route
   **And** the item matching the current route is rendered near-black (active state) while the others are not (UX-DR14)

2. **Consistent page structure**
   **Given** a guarded page renders inside the shell
   **When** it uses the shared `PageHeaderComponent`
   **Then** it shows a mono uppercase eyebrow label above one `h1` (optionally carrying a serif-italic accent word via content projection), an optional subtitle below, and at most one near-black primary action top-right (UX-DR14, UX-DR15)

3. **Responsive collapse to off-canvas drawer**
   **Given** the viewport is `xs` or `sm`
   **When** the shell renders
   **Then** the sidebar is replaced by a top bar (brand mark + hamburger toggle) and the nav becomes an off-canvas drawer, closed by default
   **And** opening the drawer traps focus inside it, closes on `Esc` or backdrop click, and returns focus to the toggle button on close (UX-DR16, UX-DR17)

4. **Skip-to-content link**
   **Given** the shell has just rendered
   **When** a keyboard user presses Tab once from the top of the page
   **Then** a visually-hidden-until-focused "Skip to content" link is the first focusable element, and activating it moves focus to the main content landmark (UX-DR17)

5. **No dead-end nav destinations**
   **Given** I am authenticated
   **When** I follow any of the seven sidebar links, or navigate directly to any route in the AR13 route table whose real page is still backlog (`/profile`, `/resumes`, `/resumes/:id`, `/educations`, `/educations/:id`, `/vacancies`, `/vacancies/:id`, `/cover-letters`, `/cover-letters/:id`, `/settings`)
   **Then** I land on a rendered page inside the shell (not a redirect to `/sign-in` and not the wildcard route), showing a "Coming soon" stub with the route's own title (FR6, AR13)

6. **Guard still covers every shell route**
   **Given** I am unauthenticated
   **When** I navigate directly to any URL in the AR13 route table other than `/sign-up` or `/sign-in`
   **Then** the guard redirects me to `/sign-in?returnUrl=<attempted-path>`, exactly as Story 1.4 established (FR6, NFR4)

## Tasks / Subtasks

- [x] **Task 1 — `PageHeaderComponent` (AC: 2)**
  - [x] `frontend/src/shared/ui/layout/page-header.ts` (CREATE) — standalone `OnPush` component. Inputs: `eyebrow` (required string), `subtitle` (optional string). Content projection slots: default slot for the `h1` content (so a page can embed a `<span class="font-serif italic ...">` accent word inline, matching `dashboard.page.ts`'s existing `text-brand-accent` pattern) and a named slot (`ng-content select="[primaryAction]"`) for the top-right action. Render exactly one `<h1>`.
  - [x] `frontend/src/shared/ui/index.ts` (UPDATE) — export `./layout/page-header`.
  - [x] `page-header.spec.ts` (CREATE) — renders eyebrow text, one `h1` with projected content, subtitle only when provided, and the `primaryAction`-slotted content only when projected. (Uses three separate host components per scenario rather than mutating a shared host's bound fields after the first `detectChanges()` — the latter tripped `NG0100` in this Angular 21 setup; separate hosts is also the more idiomatic pattern for static content projection.)
- [x] **Task 2 — Nav config + desktop sidebar (AC: 1)**
  - [x] `frontend/src/widgets/app-shell/nav-items.ts` (CREATE) — exported `readonly NAV_ITEMS` array of `{ label: string; path: string }`, in the exact order: Dashboard `/dashboard`, Profile `/profile`, Resumes `/resumes`, Education `/educations`, Vacancies `/vacancies`, Cover Letters `/cover-letters`, Settings `/settings`.
  - [x] `frontend/src/widgets/app-shell/app-shell.ts` (CREATE) — standalone `OnPush` component, selector `widget-app-shell`. Imports `RouterOutlet`, `RouterLink`, `RouterLinkActive`. Desktop (`lg:` prefix) template: fixed left `<nav aria-label="Primary">` iterating `NAV_ITEMS`, each an `<a [routerLink]>` with `routerLinkActive` applying the near-black active class. No `exact` option needed (no top-level path is a prefix of another). Main content: `<main id="main-content" tabindex="-1">` wrapping `<router-outlet>`.
  - [x] `frontend/src/widgets/app-shell/index.ts` (CREATE) — barrel exporting `app-shell.ts` and `nav-items.ts`.
  - [x] `app-shell.spec.ts` (CREATE) — renders all 7 `NAV_ITEMS` labels as links with the correct `href`s; the link whose `routerLink` matches the current mocked route carries the active class, others don't (`provideRouter([{ path: '**', component: AppShellComponent }])` + `RouterTestingHarness`).
- [x] **Task 3 — Responsive collapse: top bar + off-canvas drawer + skip link (AC: 3, 4)**
  - [x] `frontend/src/widgets/app-shell/app-shell.ts` (UPDATE, same file as Task 2) — added: (a) a "Skip to content" `<a href="#main-content">` as the very first element, `sr-only focus:not-sr-only`, with an explicit `(click)="focusMainContent()"` handler (`viewChild<ElementRef>('mainContent')`) rather than relying solely on native anchor-to-fragment focus behavior — added during review (see Review Findings) once the verification-gap lens flagged the native-only approach as unconfirmed/untestable in this test setup; (b) a `lg:hidden` top bar with a hamburger `<button data-testid="drawer-toggle">` toggling a `drawerOpen` signal via `toggleDrawer()` (click-to-open **and** click-to-close, with `aria-expanded`/`aria-label` both reflecting state — tightened during review, see Review Findings); (c) the off-canvas drawer: same `NAV_ITEMS` list inside a `cdkTrapFocus cdkTrapFocusAutoCapture` panel shown only when `drawerOpen()`, a backdrop `<div>` closing on click, `(keydown.escape)` closing too, each drawer nav link also closes on click, and `closeDrawer()` returns focus to the toggle button via a `viewChild<ElementRef>('drawerToggle')`.
  - [x] `frontend/src/widgets/app-shell/app-shell.ts` (UPDATE) — imports `A11yModule` (`CdkTrapFocus`) from `@angular/cdk/a11y`.
  - [x] `app-shell.spec.ts` (UPDATE) — skip-link is the first focusable element and points to `#main-content`; drawer absent by default; hamburger click opens it; `Escape` closes it and returns focus to the toggle; backdrop click closes it; following a drawer link closes it. 7/7 passing, 100% statements/branches/functions/lines on `app-shell.ts`.
- [x] **Task 4 — `ComingSoonStubPage` (AC: 5)**
  - [x] `frontend/src/pages/coming-soon-stub/coming-soon-stub.page.ts` (CREATE) — standalone `OnPush` component, selector `page-coming-soon-stub`, reading `title = inject(ActivatedRoute).snapshot.data['title'] as string`. Renders `<ui-page-header [eyebrow]="title">{{ title }}</ui-page-header>` then `<ui-empty-state icon="🚧" headline="Coming soon" [guidance]="title + ' is on its way — check back soon.'" />` (reusing the existing `EmptyStateComponent`, `UX-DR12`, rather than building the `UX-DR9` canonical treatment early — see Decision Record).
  - [x] `coming-soon-stub.page.spec.ts` (CREATE) — with route data `{ title: 'Resumes' }` provided via `ActivatedRoute` stub, asserts exactly one `h1`, and that the eyebrow/heading text and the empty-state guidance both contain "Resumes". 1/1 passing, 100% coverage.
- [x] **Task 5 — Wire the full AR13 route table through the shell (AC: 1, 5, 6)**
  - [x] `frontend/src/app/app.routes.ts` (UPDATE) — restructured: kept `{ path: '', pathMatch: 'full', redirectTo: 'sign-up' }`, `sign-up`, `sign-in` unchanged and top-level. Added one new parent route: `{ path: '', canActivate: [authGuard], loadComponent: () => import('@widgets/app-shell').then(m => m.AppShellComponent), children: [...] }`. Children: `dashboard` (existing `DashboardPage`, guard removed from here since the parent now carries it), plus `profile`, `resumes`, `resumes/:id`, `educations`, `educations/:id`, `vacancies`, `vacancies/:id`, `cover-letters`, `cover-letters/:id`, `settings` all pointing at `ComingSoonStubPage` with `data: { title }` matching the sidebar label (`'<Label> detail'` for `:id` routes). `{ path: '**', redirectTo: 'sign-up' }` unchanged.
  - [x] `frontend/src/app/app.routes.spec.ts` (UPDATE) — asserts: `sign-up`/`sign-in` unguarded and outside the shell; the shell parent route (`path: ''` with `children`) carries `[authGuard]`; all 11 child paths exist under it; no individual child carries its own `canActivate`; every non-dashboard child has a non-empty string `data.title`; the wildcard route still redirects to `sign-up`. 6/6 passing.
- [x] **Task 6 — Retrofit `DashboardPage` onto `PageHeaderComponent` (AC: 2)**
  - [x] `frontend/src/pages/dashboard/dashboard.page.ts` (UPDATE) — replaced the hand-rolled `<p>`/`<h1>` markup with `<ui-page-header eyebrow="Dashboard">` wrapping the existing welcome/name conditional; the subtitle paragraph is unchanged below it. No behavior change.
  - [x] `frontend/src/pages/dashboard/dashboard.page.spec.ts` (UPDATE) — existing assertions unchanged; added an assertion that exactly one `h1` renders. 4/4 passing.
- [x] **Task 7 — Verification and test coverage (AC: all)**
  - [x] `cd frontend && npx ng test --no-watch` — 174/174 passing (full suite, includes this story's 20 new/updated spec files' worth of tests).
  - [x] Coverage gate ≥80% per file on every new/touched file — confirmed, zero `ERROR:` lines for any file this story touched (see Debug Log).
  - [x] Manual keyboard walkthrough — not performed; no browser/display is available in this execution environment. Substituted with an explicit end-to-end integration test (`app.routes.integration.spec.ts`) exercising the real `routes` config, plus `app-shell.spec.ts`'s programmatic keyboard-equivalent assertions (Escape closes the drawer and returns focus to the toggle; the skip link moves focus to `#main-content` on activation; `aria-expanded`/`aria-label` track open state). Flagging this honestly rather than claiming a manual pass that didn't happen.

## Dev Notes

### Technical Requirements

- Standalone components only, `OnPush` change detection, Signals for `drawerOpen` — no NgModules, no NgRx (AR11).
- Feature-sliced layering: `AppShellComponent` and `nav-items.ts` are a **widget** (composes shared UI + routes into a layout); `PageHeaderComponent` and the reused `EmptyStateComponent` are **shared/ui** (no dependency on entities or app-level routing); `ComingSoonStubPage` is a **page** (may depend on `ActivatedRoute`). Do not let `shared/ui` import from `widgets` or `app` — same upward-dependency rule Story 1.4's Completion Notes had to correct for the interceptor (`shared > entities > features > widgets > pages > processes > app`, per `AR11`/FE Guide).
- Reuse `EmptyStateComponent` (`frontend/src/shared/ui/state/empty-state.ts`) as-is for the stub's body — it already accepts `icon`/`headline`/`guidance` inputs and a content-projected CTA slot; no new empty-state variant is needed for this story.

### API / Contract Guardrail

- No backend endpoints are touched by this story. `AR13`'s route table is a **client-side routing** decision — the guard behavior (401 → `/sign-in` with `returnUrl`) is entirely Story 1.4's existing `authGuard`/`authRedirectInterceptor`; this story only changes *which* routes that guard sits in front of.

### File Structure Requirements

- `frontend/src/shared/ui/layout/page-header.ts` (CREATE)
- `frontend/src/shared/ui/layout/page-header.spec.ts` (CREATE)
- `frontend/src/shared/ui/index.ts` (UPDATE)
- `frontend/src/widgets/app-shell/nav-items.ts` (CREATE)
- `frontend/src/widgets/app-shell/app-shell.ts` (CREATE)
- `frontend/src/widgets/app-shell/app-shell.spec.ts` (CREATE)
- `frontend/src/widgets/app-shell/index.ts` (CREATE)
- `frontend/src/pages/coming-soon-stub/coming-soon-stub.page.ts` (CREATE)
- `frontend/src/pages/coming-soon-stub/coming-soon-stub.page.spec.ts` (CREATE)
- `frontend/src/app/app.routes.ts` (UPDATE)
- `frontend/src/app/app.routes.spec.ts` (UPDATE)
- `frontend/src/pages/dashboard/dashboard.page.ts` (UPDATE)
- `frontend/src/pages/dashboard/dashboard.page.spec.ts` (UPDATE)

### Testing Requirements

- Runner is **Vitest** via `@angular/build:unit-test`: `cd frontend && npx ng test --no-watch`.
- Route-table tests: follow `app.routes.spec.ts`'s existing pattern (import `routes`, assert on the plain `Routes` array/objects — no `TestBed` needed for a static config assertion).
- `AppShellComponent` navigation tests: use Angular's router testing utilities (`provideRouter(routes)` + `RouterTestingHarness` or `Router.navigateByUrl` inside `TestBed`) to drive an actual route change and assert the resulting `routerLinkActive` class, rather than hand-asserting `routerLink` equality — this is what makes "active item near-black" an observed behavior, not just wiring.
- `cdkTrapFocus` needs `A11yModule`/`CdkTrapFocus` imported into the spec's `TestBed` the same way the component imports it; assert focus containment by checking `document.activeElement` stays within the drawer element after simulating `Tab` past its last focusable child (or, more simply, assert the drawer element has `cdkTrapFocus` bound and that Escape/backdrop-click close it — full focus-trap-cycling behavior is CDK's own tested responsibility, not this story's).
- Coverage gate: `angular.json` → `coverageThresholds.perFile`. None of this story's files are in `coverageExclude`.

### Previous Story Intelligence (1.4)

- `app.routes.ts`'s own header comment already flagged this exact handoff: *"The app shell (1.5) and full dashboard (1.6) are still out of scope"* — this story is that promised follow-up for the shell half.
- Story 1.4 built `ForbiddenStateComponent` as a primitive with no consumer yet ("no protected data-fetching page exists yet to organically trigger a 403 or 404... each future resource story wires the mapping into its own page"). This story follows the same precedent in reverse: it builds consuming *pages* (the stubs) ahead of their real feature stories, which is fine — the stub pages are throwaway by design and documented as such, unlike 1.4's warning against fabricating pages purely to exercise a code path.
- `auth.guard.ts`, `auth-redirect.interceptor.ts` are untouched by this story — moving `authGuard` from the `dashboard` route to the new shell parent route is a routing-config change only, not a guard-logic change. Do not re-derive or duplicate guard logic in `AppShellComponent` itself.
- Trap 5 from 1.4 (`authGuard`'s `isAuthenticated()` fast path) still applies unchanged — nothing in this story calls `restoreSession()` a second time; the shell route triggers the guard exactly once per navigation, same as before.

### Git Intelligence Summary

- `34a3de2`/`325084d` (PR #90, Story 1.4) — introduced `authGuard`, `auth-redirect.interceptor.ts`, `ForbiddenStateComponent`, `mapProblemToUxState`. This story builds directly on top without touching any of it.
- `6fe1eb2` — post-merge docs update after 1.4; no code relevant to this story.
- No prior commit has touched `frontend/src/widgets/` — this is a new top-level slice in the feature-sliced layout (was empty per the earlier directory listing); follow the same barrel (`index.ts`) convention as `shared/ui` and `entities/user`.

### Latest Stack Information

- Angular `^21.2.0`, standalone components, functional guards (`CanActivateFn`) — no NgModules.
- `@angular/cdk` `^21.2.0` already a dependency; this story is the first to import from it (`@angular/cdk/a11y`) — no new dependency added.
- State: Signals + injectable services only (Decision 1.2) — `drawerOpen` is a component-local `signal(false)`, not a service; it doesn't need to survive navigation or be shared across components.
- Test runner: Vitest via `@angular/build:unit-test` (not Jasmine/Karma).
- Tailwind breakpoints (`tailwind.config.js`): unprefixed = `xs` (0–479), `sm:` 480px, `md:` 768px, `lg:` 1024px, `xl:` 1440px — matches `UX-DR16` exactly; use `lg:` prefixed utilities for the desktop sidebar/main-content layout and unprefixed/`sm:` for the mobile top-bar+drawer, per the existing convention (no new breakpoint config needed).

### References

- [Source: `_bmad-output/planning-artifacts/epics.md` §Story 1.5] — story statement and acceptance criteria
- [Source: `_bmad-output/planning-artifacts/epics.md` §AR13, §UX-DR14, §UX-DR15, §UX-DR16, §UX-DR17] — routing contract and shell/page-structure/responsive/a11y decisions this story implements
- [Source: `_bmad-output/planning-artifacts/prd-demo-mvp.md` §Success Criteria item 6, §Product Scope] — the "no unbuilt feature is a dead end" MVP acceptance bar driving the stub-route decision
- [Source: `_bmad-output/planning-artifacts/ux-design-specification.md` lines 635-681] — Navigation Patterns, Responsive Strategy, Accessibility Strategy sections (sidebar/drawer/skip-link/a11y floor detail)
- [Source: `docs/FRONTEND_IMPLEMENTATION_GUIDE.md` §2.4 Routing] — full route list, and the explicit note that 1.5 is where the shell + route table land
- [Source: `_bmad-output/archive/implementation-artifacts/1-4-session-continuity-route-guards-expiry-recovery.md`] — `authGuard`/`authRedirectInterceptor` this story reuses unchanged, and its precedent for building infra ahead of consumers
- [Source: `frontend/src/app/app.routes.ts`, `frontend/src/app/auth.guard.ts`] — current route table and guard this story restructures
- [Source: `frontend/src/pages/dashboard/dashboard.page.ts`, `.spec.ts`] — existing eyebrow/h1 pattern `PageHeaderComponent` formalizes, and the file this story retrofits
- [Source: `frontend/src/shared/ui/state/empty-state.ts`, `index.ts`] — the component the stub page reuses, and the shared/ui barrel this story extends
- [Source: `frontend/src/shared/ui/form/text-field.ts` header comment] — "no spartan-ng yet" note that grounds this story's CDK-only overlay decision
- [Source: `frontend/tailwind.config.js`] — confirmed breakpoint values (`sm`/`md`/`lg`/`xl`) backing the responsive tasks
- [Source: `frontend/package.json`] — confirms `@angular/cdk` is already a dependency, no new package needed

## Dev Agent Record

### Agent Model Used

Sonnet 5 (`claude-sonnet-5`), Jobnecto Dev persona. Baseline: `a71ad1bb6d6bc9eb6ab5f1aa52c86ce1b8ea4fef`.

### Debug Log References

`cd frontend && npx ng test --no-watch` (final run, after review-driven fixes):
```
 Test Files  20 passed (20)
      Tests  174 passed (174)
```
Coverage summary: Statements 96.54%, Branches 93.48%, Functions 96.26%, Lines 97.48%. No per-file coverage-gate errors emitted (all touched/new files clear the 80% per-file threshold).

Backend (`dotnet build`/`dotnet test`) not run — `dotnet` is not installed in this environment (same limitation noted in Stories 1.3/1.4). No backend files were touched by this story.

### Completion Notes List

- Discovered mid-implementation (review-fix round): binding `[class.x]="rla.isActive"` off a `#rla="routerLinkActive"` template-reference variable requires an explicit change-detection pass to paint under `OnPush` — unlike the original single-string `routerLinkActive="classes"` form, which mutates the DOM imperatively via `Renderer2` inside the directive itself, bypassing Angular CD entirely. This only matters in the test harness (`RouterTestingHarness.navigateByUrl` didn't trigger a further CD tick before assertions); in the running app, Router navigation always triggers a full application tick regardless. Added one `harness.fixture.detectChanges()` call after navigation in the affected test — not a product-code concern.
- Ran the four self-review lenses (adversarial, edge-case hunter, verification-gap, acceptance) in parallel per `jobnecto-dev`'s `references/review.md`. See Review Findings below for the full triage.
- Fixed 4 real defects/gaps surfaced by review before moving to `review`: a CSS class conflict that made the active nav link's color depend on stylesheet generation order rather than being deterministic; an unguarded `undefined`-title cast in `ComingSoonStubPage`; a hamburger toggle whose `aria-label` never reflected open state and whose click handler could only open (never close) the drawer; and a skip-to-content link relying purely on unverified native browser fragment-focus behavior instead of explicit, testable focus management. Added a new `app.routes.integration.spec.ts` exercising the *real* `routes` config end-to-end (closing a verification-gap finding that the synthetic per-spec route tables never proved the shell's `<router-outlet>` actually activates its configured children, or that the guard still redirects on the real config).
- Two lower-severity findings were deferred (not caused by a defect this story could cheaply fix without disproportionate complexity) — see `deferred-work.md`.
- One finding (full CDK focus-trap tab-cycling verification) was dismissed as a duplicate of an already-explicit scope decision recorded in this story's own Testing Requirements section, not a new gap.

### File List

- `frontend/src/shared/ui/layout/page-header.ts` (CREATED)
- `frontend/src/shared/ui/layout/page-header.spec.ts` (CREATED)
- `frontend/src/shared/ui/index.ts` (UPDATED — exports `layout/page-header`)
- `frontend/src/widgets/app-shell/nav-items.ts` (CREATED)
- `frontend/src/widgets/app-shell/app-shell.ts` (CREATED — includes review-round fixes: non-conflicting active/inactive nav classes via `#rla="routerLinkActive"` + `[class.x]` bindings, toggling hamburger with dynamic `aria-label`/`aria-expanded`, explicit skip-link focus management)
- `frontend/src/widgets/app-shell/app-shell.spec.ts` (CREATED — includes review-round test additions)
- `frontend/src/widgets/app-shell/index.ts` (CREATED)
- `frontend/src/pages/coming-soon-stub/coming-soon-stub.page.ts` (CREATED — includes review-round title-fallback fix)
- `frontend/src/pages/coming-soon-stub/coming-soon-stub.page.spec.ts` (CREATED — includes review-round fallback test)
- `frontend/src/app/app.routes.ts` (UPDATED — restructured around the shell parent route + full AR13 route table)
- `frontend/src/app/app.routes.spec.ts` (UPDATED)
- `frontend/src/app/app.routes.integration.spec.ts` (CREATED — review-round addition, real-config end-to-end route wiring)
- `frontend/src/pages/dashboard/dashboard.page.ts` (UPDATED — retrofitted onto `PageHeaderComponent`)
- `frontend/src/pages/dashboard/dashboard.page.spec.ts` (UPDATED)
- `_bmad-output/implementation-artifacts/deferred-work.md` (UPDATED — 2 deferred findings)
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (UPDATED — 1.5 status)

### Review Findings

Four lenses (adversarial, edge-case hunter, verification gap, acceptance) run in parallel per `references/review.md`.

- [x] [Review][Patch] CSS active/inactive class conflict on nav links: the base class list included `text-text-secondary` while `routerLinkActive="text-text-primary font-semibold"` only *adds* classes, never removing the base one — when active, both `text-text-secondary` and `text-text-primary` were present simultaneously, so which color rendered depended on Tailwind's generated stylesheet order, not template intent (UX-DR14's near-black-active/secondary-inactive contrast could silently fail). `frontend/src/widgets/app-shell/app-shell.ts` (desktop + drawer nav) — fixed by switching to `#rla="routerLinkActive"` + `[class.text-text-primary]="rla.isActive"` / `[class.font-semibold]="rla.isActive"` / `[class.text-text-secondary]="!rla.isActive"`, making the two states mutually exclusive. Regression tests added asserting the full class set on both the active and an inactive link.
- [x] [Review][Patch] `ComingSoonStubPage.title` was an unguarded `as string` cast off `ActivatedRoute.snapshot.data['title']` — any future route reusing this shared stub without setting `data.title` would render the literal string `"undefined is on its way — check back soon."` with no error. `frontend/src/pages/coming-soon-stub/coming-soon-stub.page.ts` — fixed with a `?? 'Coming soon'` fallback. Regression test added for missing route data.
- [x] [Review][Patch] Drawer hamburger toggle had a static `aria-label="Open navigation"` that never changed even though `aria-expanded` did (a screen reader would announce the contradictory "Open navigation, expanded"), and its click handler only ever set `drawerOpen` to `true` — the button itself could never close the drawer (only Escape/backdrop/link-click could), despite looking like a toggle. `frontend/src/widgets/app-shell/app-shell.ts` — fixed with a dynamic `[attr.aria-label]` bound to `drawerOpen()` and a real `toggleDrawer()` handler. Regression tests added for open→label/aria-expanded and close-via-second-click.
- [x] [Review][Patch] Verification gap: the skip-to-content link's focus transfer to `#main-content` was asserted only structurally (first focusable element, correct `href`) — no test proved activating it actually moves focus, and the implementation relied entirely on native browser anchor-to-fragment focus behavior, which is not guaranteed to be simulated by the test environment. `frontend/src/widgets/app-shell/app-shell.ts` — added an explicit `(click)="focusMainContent()"` handler calling `.focus()` on the `#main-content` `ElementRef`, made the behavior deterministic across environments and testable. Regression test added asserting `document.activeElement` after a click.
- [x] [Review][Patch] Verification gap: no test exercised the *real* `routes` config end-to-end — `app-shell.spec.ts` bootstraps its own synthetic `provideRouter([{ path: '**', component: AppShellComponent }])`, and `app.routes.spec.ts` only inspects the `Routes` object's shape (paths/guards/data), so nothing proved the shell's `<router-outlet>` actually renders `DashboardPage`/`ComingSoonStubPage` as configured in production, or that the real guard still redirects an unauthenticated visitor. `frontend/src/app/app.routes.integration.spec.ts` (CREATED) — uses the real `routes` array with `HttpClientTestingController`-backed `UserService` hydration (mirroring `auth.guard.spec.ts`'s established pattern), asserting the shell + `DashboardPage` render at `/dashboard`, the shell + `ComingSoonStubPage` render at `/profile`, and an unauthenticated `/resumes` visit redirects to `/sign-in?returnUrl=%2Fresumes`.
- [x] [Review][Patch] Verification gap: the active-nav-link test only checked `text-text-primary`, never `font-semibold` — a regression dropping just the font-weight class would have passed. Folded into the CSS-conflict fix's regression tests above (now asserts the full class set on both branches).
- [Review][Defer] `authGuard` runs once per shell entry, not re-validated on every sibling in-shell navigation (moving the guard to the shared parent route is the standard Angular pattern, but means the router doesn't re-invoke `canActivate` across a child-only transition). No security exposure — the backend enforces auth on every request and `authRedirectInterceptor` still catches any real `401`. Logged in `deferred-work.md`.
- [Review][Defer] Off-canvas drawer's `drawerOpen` signal isn't reset on a viewport-breakpoint resize while open (open at `xs/sm`, resize past `lg` and back down without closing it first reopens it with no new user action). Real but low-severity edge case disproportionate to fix now (needs a `matchMedia`/resize subscription + cleanup). Logged in `deferred-work.md`.
- [Review][Dismiss] Verification-gap lens flagged the CDK focus-trap's tab-cycling behavior as untested. This is an explicit, pre-declared scope decision in this story's own Testing Requirements section ("full focus-trap-cycling behavior is CDK's own tested responsibility, not this story's"), not a new gap — no action.
- [Review][Dismiss] Acceptance lens's "Task 7 unchecked" note was accurate at the time it read the diff (the full-suite run happened but hadn't yet been recorded in the story file) — resolved by this final edit, not a code finding.

## Change Log

- 2026-09-26: Story created (ready-for-dev).
- 2026-09-26: Implemented (Tasks 1-7), self-reviewed (4 parallel lenses), fixed 6 real defects/gaps (CSS active-class conflict, missing title fallback, non-toggling/stale-label hamburger button, untested skip-link focus transfer, untested real-config route wiring, untested `font-semibold`), added one new integration spec. Two lower-severity findings deferred; one dismissed as an already-declared scope decision. 174/174 frontend tests passing, coverage gate clean. Status → review.
- 2026-09-26: PR #92 opened. CI's `llm-review` bot raised 4 findings; 2 duplicated this story's own already-deferred items (same conclusions independently reached, no new action) and 2 were low-severity with no action needed. Merged (PR #92, commit `ca33bae`). Status → done.
