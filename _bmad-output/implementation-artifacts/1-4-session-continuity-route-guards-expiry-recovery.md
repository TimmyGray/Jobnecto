# Story 1.4: Session continuity, route guards & expiry recovery

Status: ready-for-dev

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

- [ ] **Task 1 — `SKIP_AUTH_REDIRECT` context token + interceptor 401 handling (AC: 3)**
  - [ ] `frontend/src/shared/api/auth-redirect.ts` (CREATE) — export `export const SKIP_AUTH_REDIRECT = new HttpContextToken<boolean>(() => false);`
  - [ ] `frontend/src/shared/api/http.interceptor.ts` (UPDATE) — inject `Router`; in the `catchError`, after building the `ProblemDetails`, if `error instanceof HttpErrorResponse && error.status === 401 && !req.context.get(SKIP_AUTH_REDIRECT)`: call `userService.invalidate()` and `router.navigate(['/sign-in'], { queryParams: { returnUrl: router.url } })`, then still rethrow the normalized `problem` (callers with their own 401 UI, e.g. the sign-in page itself on a bad-credentials attempt — see Trap 1 — still receive it). Inject `UserService` too.
  - [ ] Update `frontend/src/shared/api/index.ts` (UPDATE) to export `auth-redirect.ts`.
  - [ ] Extend `http.interceptor.spec.ts`: a 401 without the skip context navigates to `/sign-in` with `returnUrl` equal to the current router URL and invalidates the profile; a 401 **with** the skip context does neither; a non-401 error never navigates.
- [ ] **Task 2 — `authGuard` (AC: 1, 2)**
  - [ ] `frontend/src/app/auth.guard.ts` (CREATE) — a `CanActivateFn`:
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
  - [ ] `frontend/src/entities/user/user.service.ts` (UPDATE) — add `restoreSession(): Observable<GetCurrentUserResult>`: calls `refreshToken()` (new method, `POST /users/token/refresh` with `context: new HttpContext().set(SKIP_AUTH_REDIRECT, true)`) then `switchMap` into `fetchCurrentUser()` (also set the same context on its request so a 401 here — an edge case — doesn't double-redirect). Add `RefreshAccessTokenResult` to `entities/user/model.ts` (hand-written per Trap 2 — mirrors the backend `RefreshAccessTokenResult` shape: `{ accessToken: string; tokenType: string; renewalPolicy: string }`; only `tokenType`/`renewalPolicy` are unused by the client but kept for shape fidelity).
  - [ ] Extend `user.service.spec.ts`: `restoreSession()` POSTs to `/users/token/refresh` with the skip-redirect context set, then GETs `/users/me`, and the profile signal ends up hydrated; a `401` on the refresh call propagates as an error and never calls `/users/me`.
- [ ] **Task 3 — Wire the guard into routes (AC: 2)**
  - [ ] `frontend/src/app/app.routes.ts` (UPDATE) — add `canActivate: [authGuard]` to the `dashboard` route (today's only protected route) and to every future protected route added by this story. Leave `sign-up`, `sign-in`, and the `''`/`**` redirects unguarded — unchanged from Story 1.3 (retargeting the default redirect off `sign-up` is out of scope; no AC asks for it).
- [ ] **Task 4 — `returnUrl` round-trip in the sign-in page (AC: 3)**
  - [ ] `frontend/src/pages/auth-sign-in/sign-in.page.ts` (UPDATE) — inject `ActivatedRoute`; on successful sign-in + hydration, navigate to `this.route.snapshot.queryParamMap.get('returnUrl') ?? '/dashboard'` instead of the hardcoded `/dashboard`. Guard against an absolute/protocol-relative `returnUrl` (open-redirect hardening): only honor a value starting with a single `/` (not `//`); otherwise fall back to `/dashboard`.
  - [ ] Extend `sign-in.page.spec.ts`: a sign-in with `?returnUrl=/resumes` in the activated route navigates to `/resumes` on success; an absent `returnUrl` still navigates to `/dashboard`; a `returnUrl` of `//evil.example.com` falls back to `/dashboard` (open-redirect regression test).
- [ ] **Task 5 — 403/404 mapping primitives (AC: 4)**
  - [ ] `frontend/src/shared/ui/state/forbidden-state.ts` (CREATE) — `ForbiddenStateComponent`, mirroring `NotFoundStateComponent`'s structure/a11y (`role="status"`, headline + guidance + a single recovery CTA button emitting `recover`), default headline `"You can't access this"` / guidance `"This may belong to someone else, or you may not have permission."` / CTA label `"Back to safety"`.
  - [ ] `frontend/src/shared/ui/index.ts` (UPDATE) — export `./state/forbidden-state`.
  - [ ] `frontend/src/shared/api/problem-details.ts` (UPDATE) — add a pure function `export function mapProblemToUxState(problem: ProblemDetails): 'forbidden' | 'not-found' | 'error'` — `403 → 'forbidden'`, `404 → 'not-found'`, anything else → `'error'`. No page wiring beyond this — future stories `switch` on its result to pick which state component to render.
  - [ ] `forbidden-state.spec.ts` (CREATE) — renders headline/guidance/CTA label overrides, emits `recover` on click, asserts `role="status"` and no `aria-live` misuse (mirror `not-found-state.spec.ts` if it exists, else `error-state.spec.ts`'s pattern).
  - [ ] Extend `problem-details.spec.ts`: `mapProblemToUxState` returns `'forbidden'` for 403, `'not-found'` for 404, `'error'` for 400/401/409/429/500/502/504.
- [ ] **Task 6 — Tests and verification (AC: all)**
  - [ ] `cd frontend && npx ng test --no-watch` (CI command; builder enforces the 80%-per-file coverage gate).
  - [ ] Coverage gate ≥80% per file on every new/touched file (none are in `coverageExclude`).

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

### Debug Log References

### Completion Notes List

### File List

## Change Log

- 2026-09-25: Story created (ready-for-dev).
