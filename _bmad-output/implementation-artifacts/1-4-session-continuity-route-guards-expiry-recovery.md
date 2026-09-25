# Story 1.4: Session continuity, route guards & expiry recovery

Status: review

## Story

As a **signed-in user**,
I want **my session to persist and to be gracefully recovered when it lapses**,
so that **I am never stranded or silently logged out mid-task**.

<!-- Track: [FE] — Covers: FR3, FR5, FR6, FR40, FR41, NFR4, NFR5, NFR10, AR12, AR13, AR15 -->

> **Scope note.** This story delivers the **infrastructure**: the guard, the interceptor's 401 handling, the `returnUrl` round-trip, and the 403/404 → UX-state mapping primitives (a `ForbiddenStateComponent` alongside the existing `NotFoundStateComponent`/`ErrorStateComponent`, plus a pure mapping function). No protected data-fetching page exists yet to *organically* trigger a 403 or 404 in the running app — `/dashboard` (Story 1.6) and every résumé/vacancy/cover-letter page are still backlog (Epics 2-5). AC 4 is therefore satisfied at the infrastructure/unit level here (the mapping function and both state components are fully tested in isolation); each future resource story wires the mapping into its own page and is responsible for its own 403/404 integration coverage, exactly as `authorization-contract-matrix.md` already requires per-endpoint. Do not attempt to fabricate a page here just to exercise 403/404 end-to-end — that would be scope creep into Epic 2/3.

## Decision Record

- 2026-09-25: **Cold-load session restore uses `token/refresh`, not a re-hydration-only check** — `UserService.profile` is an in-memory signal; it is always `null` immediately after a page reload even though the HTTP-only cookie may still be valid. Since `POST /api/v1/users/token/refresh` is `[Authorize]`-gated (succeeds only with a currently-valid cookie, fails `401` otherwise — confirmed in `UsersController.RefreshToken`), it doubles as the correct primitive for both "is this reload's cookie still good" *and* FR3's session renewal, in one call. The guard calls it first on a cold check, then hydrates via `fetchCurrentUser()` on success. This is the natural reading of AR13 ("all routes guarded") plus FR3 ("renewed without re-entering credentials") together — no separate polling/timer scheme is introduced (boring technology; a timer-based silent-refresh loop is not asked for by any AC and would need its own testing/teardown story).
- 2026-09-25: **`returnUrl` query param, read by the sign-in page** — the guard redirects to `/sign-in?returnUrl=<attempted-path>`; the sign-in page (Story 1.3) currently always navigates to `/dashboard` on success. This story updates it to read `returnUrl` from `ActivatedRoute.snapshot.queryParamMap`, falling back to `/dashboard` when absent — satisfying "returns me there after I re-authenticate" (AC 3) without inventing a second mechanism.
- 2026-09-25: **`HttpContextToken` used to stop the interceptor's 401-redirect from firing during the guard's own bootstrap calls** — the guard's `token/refresh` → `fetchCurrentUser()` calls are expected to 401 for a genuinely-anonymous visitor; that is not a "session lapsed mid-task" event and must not trigger the interceptor's redirect (which would race the guard's own `UrlTree` redirect and could clobber the intended `returnUrl`). A shared `SKIP_AUTH_REDIRECT` `HttpContextToken` is set on those two calls only; the interceptor checks it before redirecting.

## Acceptance Criteria

1. **Silent session renewal**
   **Given** I have an active session
   **When** the client needs to renew it
   **Then** it uses `POST /api/v1/users/token/refresh` without requiring me to re-enter credentials (FR3)

2. **Route guard blocks unauthenticated access**
   **Given** I navigate to any route except `/sign-up` and `/sign-in`
   **When** I am unauthenticated
   **Then** a route guard redirects me to `/sign-in` (FR6, NFR4, AR13)

3. **Mid-task expiry recovery**
   **Given** my cookie session expires while I am working
   **When** any request returns `401`
   **Then** the interceptor routes me to `/sign-in`, preserves my intended destination, and returns me there after I re-authenticate (FR5, AR12, AR15)

4. **403/404 mapping primitives**
   **Given** a request returns `403` / `404` (cross-user or not-found per the contract matrix)
   **When** the global handler maps it
   **Then** the correct UX state is produced (403 → explain+recovery CTA; 404 → not-found+back) and neither state ever renders cross-user data (FR41, AR15)

## Tasks / Subtasks

- [x] **Task 1 — `SKIP_AUTH_REDIRECT` context token + 401 handling (AC: 3)** ⚠️ *Deviates from the plan below — see Completion Notes.*
  - [x] `frontend/src/shared/api/auth-redirect.ts` (CREATE) — `SKIP_AUTH_REDIRECT` context token.
  - [x] Implemented as a **new, separately-registered interceptor** (`frontend/src/app/auth-redirect.interceptor.ts`) instead of editing `http.interceptor.ts` in place — see Completion Notes for why.
  - [x] Update `frontend/src/shared/api/index.ts` (UPDATE) to export `auth-redirect.ts`.
  - [x] `auth-redirect.interceptor.spec.ts`: a 401 without the skip context navigates to `/sign-in` with `returnUrl` equal to the current router URL and invalidates the profile; a 401 with the skip context does neither; a non-401 error never navigates; a raw `HttpErrorResponse` (no `httpInterceptor` ahead of it) still triggers the redirect; a 401 while already on `/sign-in` does not re-navigate (review fix).
- [x] **Task 2 — `authGuard` (AC: 1, 2)**
  - [x] `frontend/src/app/auth.guard.ts` (CREATE) — a `CanActivateFn`:
    ```ts
    export const authGuard: CanActivateFn = (_route, state) => {
      const userService = inject(UserService);
      const router = inject(Router);
      if (userService.isAuthenticated()) {
        return true;
      }
      return userService.restoreSession().pipe(
        map(() => true),
        catchError(() =>
          of(router.createUrlTree(['/sign-in'], { queryParams: { returnUrl: state.url } })),
        ),
      );
    };
    ```
  - [x] `frontend/src/entities/user/user.service.ts` (UPDATE) — added `restoreSession()` and `refreshToken()`. `fetchCurrentUser()` gained an optional `skipAuthRedirect` parameter (review fix — see Completion Notes) rather than always setting the skip context. `RefreshAccessTokenResult` sourced from the **already-generated** OpenAPI schema (Trap 2 didn't apply — see Completion Notes) instead of hand-written.
  - [x] `user.service.spec.ts`: `restoreSession()` POSTs to `/users/token/refresh` with the skip-redirect context set, then GETs `/users/me`, and the profile signal ends up hydrated; a `401` on the refresh call propagates as an error and never calls `/users/me`; `signIn()` sets the skip context; `fetchCurrentUser()` defaults to NOT skipping the redirect and `fetchCurrentUser(true)` does (review regression tests).
- [x] **Task 3 — Wire the guard into routes (AC: 2)**
  - [x] `frontend/src/app/app.routes.ts` (UPDATE) — `canActivate: [authGuard]` on the `dashboard` route. `sign-up`/`sign-in`/`''`/`**` unchanged.
  - [x] `app.routes.spec.ts` (CREATE, review fix — verification-gap lens found the wiring itself untested) — asserts `dashboard` carries `authGuard` and `sign-up`/`sign-in` don't.
- [x] **Task 4 — `returnUrl` round-trip in the sign-in page (AC: 3)**
  - [x] `frontend/src/pages/auth-sign-in/sign-in.page.ts` (UPDATE) — `intendedDestination()` reads `returnUrl`, falls back to `/dashboard`. Guard tightened during review to also reject backslash variants (`/\host`), not just `//`.
  - [x] `sign-in.page.spec.ts`: `?returnUrl=/resumes` navigates there on success; absent `returnUrl` falls back to `/dashboard`; `//evil.example.com`, `/\evil.example.com`, and `http://evil.example.com` all fall back to `/dashboard` (open-redirect regression tests, the backslash case added during review).
- [x] **Task 5 — 403/404 mapping primitives (AC: 4)**
  - [x] `frontend/src/shared/ui/state/forbidden-state.ts` (CREATE) — `ForbiddenStateComponent`, mirrors `NotFoundStateComponent`.
  - [x] `frontend/src/shared/ui/index.ts` (UPDATE) — exports `./state/forbidden-state`.
  - [x] `frontend/src/shared/api/problem-details.ts` (UPDATE) — `mapProblemToUxState`.
  - [x] `forbidden-state.spec.ts` (CREATE).
  - [x] `problem-details.spec.ts` — `mapProblemToUxState` cases.
- [x] **Task 6 — Tests and verification (AC: all)**
  - [x] `cd frontend && npx ng test --no-watch` — 149/149 passing, coverage gate clean (see Debug Log).
  - [x] Coverage gate ≥80% per file on every new/touched file — confirmed (see Debug Log).

## Dev Notes

### ⚠️ Traps

**Trap 1 — the interceptor's new 401-redirect must not fight the sign-in page's own 401 handling.**
The sign-in page (Story 1.3, Trap 3) renders a single "Invalid credentials" banner on a `401` from `POST /users/sessions` — that 401 means *wrong password*, not *session lapsed*, and must never trigger a navigation away from `/sign-in`. Two independent guards against this: (a) the interceptor's redirect target is `/sign-in` itself, so even an un-flagged 401 from the sign-in call would just re-navigate to the same route the user is already on with a `returnUrl` of `/sign-in` — harmless but noisy; (b) do it properly anyway by giving `UserService.signIn()`'s underlying POST the `SKIP_AUTH_REDIRECT` context too (it is a public, unauthenticated endpoint — a 401 from it is a credential failure, never a session-lapse). Add this to Task 1/2's wiring: `signIn()` in `user.service.ts` also sets the context. Extend `user.service.spec.ts` accordingly.

**Trap 2 — do not run `npm run gen:api` expecting `RefreshAccessTokenResult`.**
Same situation as Story 1.3's Trap 5: the type exists on the backend (`JobNecto.API.Contracts.Auth.RefreshAccessTokenResult`) but whether it has been through an OpenAPI-regeneration cycle on this branch is not guaranteed at story-authoring time. Hand-write it in `entities/user/model.ts` next to `SignInResult`, with the same "hand-written, swap for the generated alias later" comment convention Story 1.3 established. Verify the shape against `backend/src/JobNecto.API/Contracts/Auth/RefreshAccessTokenResult.cs` directly, not from memory.

**Trap 3 — `router.url` inside the interceptor is the URL at the moment the failing request was made, not necessarily the route the guard is trying to enter.**
For AC 3 (mid-task 401), this is exactly right — you want to return the user to wherever they *were*. Do not try to reuse this same code path for AC 2's guard redirect (cold/unauthenticated entry) — the guard has its own, more accurate `state.url` (the route being entered) and must build its own `UrlTree` rather than delegating to the interceptor. Keep the two redirect paths (guard → `UrlTree` return value; interceptor → imperative `router.navigate`) separate; do not try to unify them into one shared helper that guesses which URL to use.

**Trap 4 — an absolute or protocol-relative `returnUrl` is an open-redirect vector.**
`returnUrl` round-trips through a URL query param the user's browser controls. Only ever `router.navigate([returnUrl])`-style navigate a value matching `^/(?!/)` (single leading slash, not two — `//evil.example` is protocol-relative and browsers will follow it as an external navigation in some contexts). Validate before use in both places `returnUrl` is consumed (the sign-in page) — it is never consumed a second time by the guard itself (the guard only *sets* it).

**Trap 5 — `authGuard`'s `restoreSession()` must not be called when a profile is already hydrated.**
Calling it unconditionally on every navigation would fire a `token/refresh` + `/users/me` round-trip on every in-app route change, defeating the point of caching the profile in a signal (Decision 1.3, `demo-mvp-architecture-decisions.md`). The `isAuthenticated()` fast-path in Task 2 is required, not an optimization to skip.

### API / Contract Guardrail

- `POST /api/v1/users/token/refresh` — `[Authorize]`, no body. `200` renews the cookie (browser transport) and returns `RefreshAccessTokenResult`; `401` if the current session is not valid (expired/missing/invalid cookie). No `403`/`404` — it is not resource-scoped.
- `403`/`404` mapping is generic infra (Task 5); the concrete endpoints that produce them (résumés, vacancies, cover letters, templates, educations) are out of this story's scope — see `authorization-contract-matrix.md` for the full per-endpoint matrix each of those stories must satisfy individually.

### File Structure Requirements

- `frontend/src/shared/api/auth-redirect.ts` (CREATE)
- `frontend/src/shared/api/http.interceptor.ts` (UPDATE)
- `frontend/src/shared/api/http.interceptor.spec.ts` (UPDATE)
- `frontend/src/shared/api/index.ts` (UPDATE)
- `frontend/src/shared/api/problem-details.ts` (UPDATE)
- `frontend/src/shared/api/problem-details.spec.ts` (UPDATE)
- `frontend/src/shared/ui/state/forbidden-state.ts` (CREATE)
- `frontend/src/shared/ui/state/forbidden-state.spec.ts` (CREATE)
- `frontend/src/shared/ui/index.ts` (UPDATE)
- `frontend/src/app/auth.guard.ts` (CREATE)
- `frontend/src/app/auth.guard.spec.ts` (CREATE)
- `frontend/src/app/app.routes.ts` (UPDATE)
- `frontend/src/entities/user/model.ts` (UPDATE — hand-added `RefreshAccessTokenResult`)
- `frontend/src/entities/user/user.service.ts` (UPDATE — `refreshToken()`, `restoreSession()`; `signIn()` gets the skip-redirect context)
- `frontend/src/entities/user/user.service.spec.ts` (UPDATE)
- `frontend/src/pages/auth-sign-in/sign-in.page.ts` (UPDATE — `returnUrl` navigation)
- `frontend/src/pages/auth-sign-in/sign-in.page.spec.ts` (UPDATE)

### Testing Requirements

- Runner is **Vitest** via `@angular/build:unit-test`: `cd frontend && npx ng test --no-watch`.
- Guard tests: use `TestBed.runInInjectionContext(() => authGuard(route, state))` to invoke a `CanActivateFn` outside a real navigation (standard Angular testing pattern for functional guards); assert the return value is `true` or a `UrlTree` with the expected `queryParams`.
- Interceptor tests: mirror the existing `http.interceptor.spec.ts` pattern (`provideHttpClient(withInterceptors([httpInterceptor]))`, `provideHttpClientTesting()`, `HttpTestingController`); spy on `Router.navigate`.
- Coverage gate: `angular.json` → `coverageThresholds.perFile` (lines/statements ≥80%). None of this story's files are in `coverageExclude`.

### Previous Story Intelligence (1.3)

- Story 1.3 explicitly deferred all of this: "Guards do not exist yet. Do not add them here — Story 1.4 owns route guards, the 401→re-auth interceptor, and intended-destination return" and "AC 1's 'intended destination' should read from whatever return-URL mechanism exists at implementation time" — this story **is** that mechanism; Task 4 is the promised follow-up.
- Story 1.3's review found and fixed a **stale cross-user profile after a failed hydration** bug by calling `userService.invalidate()` before hydrating. The same discipline applies here: `restoreSession()`'s `fetchCurrentUser()` call should not leave a stale profile in place if it fails after a successful refresh — `catchError` in the guard already treats any failure in the `restoreSession()` chain as "not authenticated," so no stale-profile risk exists (the signal is simply never set), but do not add an early `invalidate()` before the refresh call — that would erase a perfectly valid in-memory profile on the fast path (which never runs `restoreSession()` at all, see Trap 5).
- Story 1.3's CI `LLM PR review` bot repeatedly re-raised concerns after the human review passed — re-verify any repeated bot finding against the actual code (`AbstractControl.setErrors()` semantics, etc.) rather than patching reflexively; two of Story 1.3's bot findings were correctly dismissed after concrete verification, not applied blind.

### Git Intelligence Summary

- `e06b8ee`/`96e73ad` — Story 1.3 merged (PR #88/#89), including the post-merge docs update. `4959266`/`b3861fa` show the shape of Story 1.2's CI-review fix cycle (small, targeted patches per finding, each with a regression test) — follow the same granularity here.
- `872aa50`/`11d7f70` are CI-tooling fixes (diff-mode for the review bot), not app code — irrelevant to this story's implementation but confirm the review bot is active on this repo's PRs; expect at least one round of its findings on the new interceptor/guard logic given how security-sensitive session handling is.

### Latest Stack Information

- Angular `^21.2.0`, standalone components, functional guards/interceptors (`CanActivateFn`, `HttpInterceptorFn`) — no NgModules, no class-based guards.
- State: Signals + injectable services only (Decision 1.2) — no NgRx.
- Test runner: Vitest via `@angular/build:unit-test` (not Jasmine/Karma).

### References

- [Source: `_bmad-output/planning-artifacts/epics.md` §Story 1.4] — story statement and acceptance criteria
- [Source: `_bmad-output/planning-artifacts/architecture/demo-mvp-architecture-decisions.md` §Decision 1.6, §Decision 2] — routing/guard scope, `token/refresh` semantics, "Guard infrastructure... remain Story 1.4 scope, as planned"
- [Source: `_bmad-output/planning-artifacts/architecture/authorization-contract-matrix.md`] — the 403-vs-404 contract each future resource story must honor; this story only builds the generic mapping primitive
- [Source: `_bmad-output/planning-artifacts/ux-design-specification.md` lines 476-484, 582-584] — Journey 4 recovery model (401/403/404/409/400 UX mapping), `EmptyState`/`ErrorState`/`NotFoundState` component family (this story adds the missing `ForbiddenState` member)
- [Source: `backend/src/JobNecto.API/Controllers/UsersController.cs` `RefreshToken`, `backend/src/JobNecto.API/Contracts/Auth/RefreshAccessTokenResult.cs`] — the exact `token/refresh` contract (`[Authorize]`, 200/401, response shape)
- [Source: `_bmad-output/archive/implementation-artifacts/1-3-sign-in-screen.md`] — the sign-in page this story updates (`returnUrl`), and the traps/conventions it established
- [Source: `frontend/src/shared/api/http.interceptor.ts`, `problem-details.ts`] — the interceptor this story extends
- [Source: `frontend/src/shared/ui/state/not-found-state.ts`, `error-state.ts`] — the component pattern `ForbiddenStateComponent` mirrors
- [Source: `frontend/src/entities/user/user.service.ts`, `frontend/src/app/app.routes.ts`] — files this story extends directly

## Dev Agent Record

### Agent Model Used

Sonnet 5 (`claude-sonnet-5`), Jobnecto Dev persona. Baseline: `2e79adf6b3c3762e7697cd40cfce0ef6aeed83d9`.

### Debug Log References

`cd frontend && npx ng test --no-watch` (final run, after review-driven fixes):
```
 Test Files  16 passed (16)
      Tests  149 passed (149)
```
Coverage summary: Statements 96.03%, Branches 92.82%, Functions 95.78%, Lines 97.13%. No per-file coverage-gate errors emitted (all touched/new files clear the 80% per-file threshold).

Backend Verify commands (`dotnet build`/`dotnet test`) could not be run — `dotnet` is not installed in this environment (same limitation noted in Story 1.3). No backend files were touched by this story, which limits the risk; this is noted rather than claimed as passing.

### Completion Notes List

- **Deviation from Task 1's plan (deliberate, not a defect):** the story's task text said to add the 401-redirect logic directly inside `frontend/src/shared/api/http.interceptor.ts`. Implementing it there would have made `shared/api` (the lowest feature-sliced layer per AR11: `app > processes > pages > widgets > features > entities > shared`) depend on `UserService`, an entity — an upward dependency the codebase's own layering forbids. Instead, added a second, separately-registered interceptor at the `app` layer (`frontend/src/app/auth-redirect.interceptor.ts`), registered ahead of `httpInterceptor` in `app.config.ts` so it observes the already-normalized `ProblemDetails`. `http.interceptor.ts` itself is untouched. Functionally equivalent to the plan; architecturally cleaner. The Acceptance-lens reviewer flagged this as "architecturally divergent from the story's stated file plan" and recommended dev/architect sign-off rather than treating it as a defect — recording that here per its recommendation.
- **Trap 2 didn't apply:** the story predicted `RefreshAccessTokenResult` would need hand-writing (mirroring Story 1.3's Trap 5 for `SignInCommand`/`SignInResult`). Checked `frontend/src/shared/api/generated/schema.ts` directly before writing any code — `RefreshAccessTokenResult` was already present (Story 1.2 apparently went through an OpenAPI-regeneration cycle after merge). Used the generated `components['schemas']['RefreshAccessTokenResult']` alias instead of hand-writing it. `SignInCommand`/`SignInResult` remain hand-written — confirmed `/users/sessions` is still absent from the generated schema.
- Ran the four self-review lenses (adversarial, edge-case, verification-gap, acceptance) in parallel per `jobnecto-dev`'s `references/review.md`. See Review Findings below for the full triage.
- Fixed 5 real defects surfaced by review before moving to `review`: `fetchCurrentUser()` unconditionally suppressing the 401 redirect for every future caller (not just the two flows that need it), a returnUrl-clobbering race when the interceptor fires while already on `/sign-in` (found independently by both the adversarial and edge-case lenses), a missing test for the actual route-guard wiring, an untested raw-`HttpErrorResponse` code path in the new interceptor, and an incomplete open-redirect guard (backslash variant).
- Three lower-severity/architectural findings were deferred (not caused by a defect this story could cheaply fix) — see `deferred-work.md`.

### File List

- `frontend/src/shared/api/auth-redirect.ts` (CREATED — `SKIP_AUTH_REDIRECT` context token)
- `frontend/src/app/auth-redirect.interceptor.ts` (CREATED — 401 session-lapse redirect; see Completion Notes for the layering deviation from the story's plan)
- `frontend/src/app/auth-redirect.interceptor.spec.ts` (CREATED)
- `frontend/src/app/auth.guard.ts` (CREATED)
- `frontend/src/app/auth.guard.spec.ts` (CREATED)
- `frontend/src/app/app.routes.spec.ts` (CREATED — review fix, route-wiring regression test)
- `frontend/src/app/app.routes.ts` (UPDATED — `canActivate: [authGuard]` on `dashboard`)
- `frontend/src/app/app.config.ts` (UPDATED — registers `authRedirectInterceptor` ahead of `httpInterceptor`)
- `frontend/src/entities/user/model.ts` (UPDATED — added `RefreshAccessTokenResult` generated-schema alias)
- `frontend/src/entities/user/user.service.ts` (UPDATED — `refreshToken()`, `restoreSession()`; `fetchCurrentUser(skipAuthRedirect = false)`; `signIn()` sets the skip context)
- `frontend/src/entities/user/user.service.spec.ts` (UPDATED)
- `frontend/src/pages/auth-sign-in/sign-in.page.ts` (UPDATED — `returnUrl` navigation via `intendedDestination()`)
- `frontend/src/pages/auth-sign-in/sign-in.page.spec.ts` (UPDATED)
- `frontend/src/pages/auth-sign-up/sign-up.page.ts` (UPDATED — `fetchCurrentUser(true)` to preserve its existing no-redirect-on-hydration-failure behavior now that `authRedirectInterceptor` exists)
- `frontend/src/shared/ui/state/forbidden-state.ts` (CREATED)
- `frontend/src/shared/ui/state/forbidden-state.spec.ts` (CREATED)
- `frontend/src/shared/ui/index.ts` (UPDATED — exports `forbidden-state`)
- `frontend/src/shared/api/problem-details.ts` (UPDATED — `mapProblemToUxState`)
- `frontend/src/shared/api/problem-details.spec.ts` (UPDATED)
- `frontend/src/shared/api/index.ts` (UPDATED — exports `auth-redirect`)
- `_bmad-output/implementation-artifacts/deferred-work.md` (UPDATED — 3 deferred findings)
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (UPDATED — 1.4 status)

### Review Findings

Four lenses (adversarial, edge-case hunter, verification gap, acceptance) run in parallel per `references/review.md`.

- [x] [Review][Patch] `fetchCurrentUser()` unconditionally set `SKIP_AUTH_REDIRECT`, silently suppressing the 401 redirect for *any* future caller (not just sign-in/sign-up hydration and `restoreSession()`) — e.g. a future "refresh my profile" action mid-session would swallow a genuine session lapse with no redirect and no visible recovery. `frontend/src/entities/user/user.service.ts` — fixed by making it an explicit `skipAuthRedirect = false` parameter; `restoreSession()`, the sign-in page's hydration call, and the sign-up page's hydration call now pass `true` explicitly, everything else defaults to participating in the redirect. Regression tests added in `user.service.spec.ts`.
- [x] [Review][Patch] Concurrent/repeat-401 race could clobber the real `returnUrl` — found independently by both the adversarial and edge-case lenses: a 401 while already on `/sign-in` (or a second 401 arriving just after the first redirect lands) recomputes `returnUrl` from `router.url`, which by then is `/sign-in` itself, producing `returnUrl=/sign-in` and stranding a freshly-authenticated user back on the sign-in page. `frontend/src/app/auth-redirect.interceptor.ts` — fixed by skipping the redirect entirely when `router.url` already starts with `/sign-in`. Regression test added.
- [x] [Review][Patch] Verification gap: the route-guard *wiring* itself (`canActivate: [authGuard]` on the `dashboard` route in `app.routes.ts`) had no test — a regression dropping it would pass every other test in the suite. `frontend/src/app/app.routes.spec.ts` (CREATED).
- [x] [Review][Patch] Verification gap: `auth-redirect.interceptor.ts`'s `error instanceof HttpErrorResponse` branch was dead in every existing test (all specs register it ahead of `httpInterceptor`, so it only ever receives an already-normalized `ProblemDetails`). Added a test registering the interceptor alone against a raw `HttpErrorResponse`.
- [x] [Review][Patch] Edge-case lens: the open-redirect guard (`/^\/(?!\/)/`) only rejected a literal `//`; a backslash variant (`/\evil.example.com`), which some browsers normalize to a protocol-relative URL, passed the regex. `frontend/src/pages/auth-sign-in/sign-in.page.ts` `intendedDestination()` — tightened to `/^\/[^/\\]/`. Regression tests added for the backslash variant and a bare absolute URL.
- [Review][Defer] `authGuard`'s fast path trusts the in-memory `isAuthenticated()` signal without revalidating against the server — a UX flash (briefly rendering a guarded route before a subsequent 401 bounces the user back), never a security hole since the backend enforces auth independently. Logged in `deferred-work.md`.
- [Review][Defer] Duplicate concurrent `restoreSession()` calls are not deduplicated if two guarded resources activated at once on a cold load. Latent, not reachable today — the app has a single router-outlet and no route activates two guarded resources simultaneously. Logged in `deferred-work.md`.
- [Review][Defer] `authGuard`/`authRedirectInterceptor`'s in-flight observables aren't explicitly torn down on a superseded navigation beyond what Angular's router does automatically; no `takeUntil(destroyed)` convention exists anywhere in this codebase yet. Pre-existing gap, not introduced by this story. Logged in `deferred-work.md`.
- [Review][Dismiss] Acceptance lens noted the story's Tasks/Subtasks checkboxes were still unchecked in the `.md` at the time it read the diff — not a code finding; checkboxes are ticked as the final step of this dev session, after all lenses had already read the file.

## Change Log

- 2026-09-25: Story created (ready-for-dev).
- 2026-09-25: Implemented (Tasks 1-6), self-reviewed (4 parallel lenses), fixed 5 real defects (one deliberate architectural deviation from the task plan, documented and left as-is; three lower-severity findings deferred). 149/149 frontend tests passing, coverage gate clean. Status → review.
