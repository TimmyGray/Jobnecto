# Story 1.5: Application shell & navigation

Status: ready-for-dev

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

- [ ] **Task 1 — `PageHeaderComponent` (AC: 2)**
  - [ ] `frontend/src/shared/ui/layout/page-header.ts` (CREATE) — standalone `OnPush` component. Inputs: `eyebrow` (required string), `subtitle` (optional string). Content projection slots: default slot for the `h1` content (so a page can embed a `<span class="font-serif italic ...">` accent word inline, matching `dashboard.page.ts`'s existing `text-brand-accent` pattern) and a named slot (`ng-content select="[primaryAction]"`) for the top-right action. Render exactly one `<h1>`.
  - [ ] `frontend/src/shared/ui/index.ts` (UPDATE) — export `./layout/page-header`.
  - [ ] `page-header.spec.ts` (CREATE) — renders eyebrow text, one `h1` with projected content, subtitle only when provided, and the `primaryAction`-slotted content only when projected.
- [ ] **Task 2 — Nav config + desktop sidebar (AC: 1)**
  - [ ] `frontend/src/widgets/app-shell/nav-items.ts` (CREATE) — exported `readonly NAV_ITEMS` array of `{ label: string; path: string }`, in the exact order: Dashboard `/dashboard`, Profile `/profile`, Resumes `/resumes`, Education `/educations`, Vacancies `/vacancies`, Cover Letters `/cover-letters`, Settings `/settings`.
  - [ ] `frontend/src/widgets/app-shell/app-shell.ts` (CREATE) — standalone `OnPush` component, selector `widget-app-shell`. Imports `RouterOutlet`, `RouterLink`, `RouterLinkActive`. Desktop (`lg:` prefix) template: fixed left `<nav aria-label="Primary">` iterating `NAV_ITEMS`, each an `<a [routerLink]>` with `routerLinkActive` applying the near-black active class (`text-text-primary font-semibold` vs. `text-text-secondary` inactive) and `[routerLinkActiveOptions]="{ exact: item.path === '/dashboard' }"` is unnecessary here since none of the top-level paths are prefixes of each other except none nest — no `exact` needed. Main content: `<main id="main-content" tabindex="-1">` wrapping `<router-outlet>`.
  - [ ] `frontend/src/widgets/app-shell/index.ts` (CREATE) — barrel exporting `app-shell.ts` and `nav-items.ts`.
  - [ ] `app-shell.spec.ts` (CREATE) — renders all 7 `NAV_ITEMS` labels as links with the correct `href`s; the link whose `routerLink` matches the current mocked route carries the active class, others don't (drive via `provideRouter` + `RouterTestingHarness` navigating to e.g. `/resumes` and asserting the "Resumes" link is active, "Dashboard" is not).
- [ ] **Task 3 — Responsive collapse: top bar + off-canvas drawer + skip link (AC: 3, 4)**
  - [ ] `frontend/src/widgets/app-shell/app-shell.ts` (UPDATE, same file as Task 2) — add: (a) a "Skip to content" `<a href="#main-content">` as the very first element in the template, visually hidden until `:focus` (Tailwind `sr-only focus:not-sr-only` utility pattern) that, on activation, focuses the `#main-content` element (native anchor-to-id focus behavior is enough given `tabindex="-1"` on `<main>`); (b) a `lg:hidden` top bar with a hamburger `<button>` toggling a `drawerOpen` signal; (c) the off-canvas drawer: same `NAV_ITEMS` list rendered inside a `cdkTrapFocus` `[cdkTrapFocusAutoCapture]="drawerOpen()"` panel shown only when `drawerOpen()`, with a backdrop `<div>` whose click sets `drawerOpen.set(false)`, a `(keydown.escape)` handler on the drawer doing the same, and closing returns focus to the hamburger button (call `.focus()` on the stored `ElementRef` of the toggle button in the same handler that sets `drawerOpen` false).
  - [ ] `frontend/src/widgets/app-shell/app-shell.ts` (UPDATE) — import `A11yModule` (`CdkTrapFocus`) from `@angular/cdk/a11y`.
  - [ ] `app-shell.spec.ts` (UPDATE) — skip-link is the first anchor in the DOM and points to `#main-content`; drawer is `hidden`/absent by default; clicking the hamburger sets it visible; pressing `Escape` while the drawer is open closes it and returns focus to the hamburger button; clicking the backdrop closes it.
- [ ] **Task 4 — `ComingSoonStubPage` (AC: 5)**
  - [ ] `frontend/src/pages/coming-soon-stub/coming-soon-stub.page.ts` (CREATE) — standalone `OnPush` component, selector `page-coming-soon-stub`, reading `title = inject(ActivatedRoute).snapshot.data['title'] as string`. Renders `<ui-page-header [eyebrow]="title">{{ title }}</ui-page-header>` then `<ui-empty-state icon="🚧" headline="Coming soon" [guidance]="title + ' is on its way — check back soon.'" />` (reusing the existing `EmptyStateComponent`, `UX-DR12`, rather than building the `UX-DR9` canonical treatment early — see Decision Record).
  - [ ] `coming-soon-stub.page.spec.ts` (CREATE) — with route data `{ title: 'Resumes' }` provided via `ActivatedRoute` stub, asserts the eyebrow/heading text and the empty-state guidance both contain "Resumes".
- [ ] **Task 5 — Wire the full AR13 route table through the shell (AC: 1, 5, 6)**
  - [ ] `frontend/src/app/app.routes.ts` (UPDATE) — restructure: keep `{ path: '', pathMatch: 'full', redirectTo: 'sign-up' }`, `sign-up`, `sign-in` unchanged and top-level. Add one new parent route: `{ path: '', canActivate: [authGuard], loadComponent: () => import('@widgets/app-shell').then(m => m.AppShellComponent), children: [...] }`. Children: `dashboard` (existing `DashboardPage` import, guard removed from here since the parent now carries it), then for each of `profile`, `resumes`, `resumes/:id`, `educations`, `educations/:id`, `vacancies`, `vacancies/:id`, `cover-letters`, `cover-letters/:id`, `settings` — `{ path, loadComponent: () => import('@pages/coming-soon-stub/coming-soon-stub.page').then(m => m.ComingSoonStubPage), data: { title: '<Human Title>' } }` with the title matching the sidebar label (or, for `:id` detail routes, `'<Label> detail'`, e.g. `'Resumes detail'`). Keep the final `{ path: '**', redirectTo: 'sign-up' }` unchanged — it now only ever catches a genuinely unknown URL.
  - [ ] `frontend/src/app/app.routes.spec.ts` (UPDATE) — replace the existing "dashboard carries authGuard, sign-up/sign-in don't" assertions with: the shell parent route (`path: ''` with children) carries `authGuard`; every one of the 11 child paths (`dashboard`, `profile`, `resumes`, `resumes/:id`, `educations`, `educations/:id`, `vacancies`, `vacancies/:id`, `cover-letters`, `cover-letters/:id`, `settings`) exists under it; `sign-up` and `sign-in` remain outside it and carry no guard.
- [ ] **Task 6 — Retrofit `DashboardPage` onto `PageHeaderComponent` (AC: 2)**
  - [ ] `frontend/src/pages/dashboard/dashboard.page.ts` (UPDATE) — replace the hand-rolled `<p class="... eyebrow ...">`/`<h1>` markup with `<ui-page-header eyebrow="Dashboard">Welcome@if (profile(); as user) {, <span class="font-serif italic text-brand-accent">{{ user.loginName }}</span>}</ui-page-header>`, keeping the existing conditional subtitle paragraph below it. No behavior change — this proves `PageHeaderComponent` against a real consumer instead of only its own spec.
  - [ ] `frontend/src/pages/dashboard/dashboard.page.spec.ts` (UPDATE) — existing assertions on rendered text stay valid; add one assertion that exactly one `h1` renders.
- [ ] **Task 7 — Verification and test coverage (AC: all)**
  - [ ] `cd frontend && npx ng test --no-watch`
  - [ ] Coverage gate ≥80% per file on every new/touched file (`angular.json` → `coverageThresholds.perFile`)
  - [ ] Manual keyboard walkthrough (per `ux-design-specification.md` Testing Strategy): Tab from page load reaches the skip link first; Tab through the sidebar in visual order; open/close the mobile drawer with only the keyboard.

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

### Debug Log References

### Completion Notes List

### File List

## Change Log

- 2026-09-26: Story created (ready-for-dev).
