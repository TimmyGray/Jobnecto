# Story 1.3: Sign-in screen

Status: ready-for-dev

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a **returning user**,
I want **a sign-in page**,
so that **I can get back into my account with my credentials**.

> **Track:** Frontend (FE). Built **in parallel with Story 1.2** (Backend-LLM) against the pinned wire contract below. The backend endpoint **does not exist yet** — do not wait for it, and do not expect generated types for it (see Trap 5).
>
> **Design status — read this.** There is **no UX design for a sign-in screen anywhere** in the planning artifacts: `ux-design-specification.md` has no sign-in section, `docs/FRONTEND_IMPLEMENTATION_GUIDE.md` §4 has a full "Sign Up Page" write-up but no sign-in equivalent and no §7.x validation rules for it, and `frontend/design-examples/` contains `Sign-up _ default.png` and `Sign-up _ 409 conflict.png` but **no sign-in render**. The layout below is **extrapolated** from the shipped sign-up page plus the general design-system rules (tokens, button hierarchy, a11y floor). Treat visual/layout choices as reasoned defaults open to review — do not describe them as "per the UX spec."

## Acceptance Criteria

**Happy path**

1. `/sign-in` is registered as an **unguarded**, lazy-loaded route. Submitting a valid identifier + password POSTs to `POST /api/v1/users/sessions`, and on `200` the cookie session is active and the user is routed to their intended destination (or `/dashboard`). [FR4, AR13]
2. On `200`, the client hydrates the profile via `GET /api/v1/users/me` before landing, mirroring sign-up's two-call flow. *(One-call is not available: `UserProfile` is `GetCurrentUserResult`, which carries `createdAt`/`updatedAt`; the sign-in response mirrors `CreateUserResult` and lacks them — see Dev Notes.)*
3. If the `200` succeeds but the profile hydration fails, the user is **still routed to `/dashboard`** and is never shown a sign-in error. They are authenticated; the cookie is set. **Do not copy sign-up's current behavior here** — see Trap 2.

**Wrong credentials (security-critical)**

4. A `401` renders **one generic banner reading "Invalid credentials"**, with **no per-field attribution** and no hint as to whether the identifier or the password was wrong. The identifier and password fields must not be marked invalid, and the raw status code is never shown. [AR2, FR4]

**Rate limited**

5. A `429` renders a friendly, plain-language message ("You've tried several times — please wait a moment and try again"), never a raw code. [UX-DR15]
6. The message **honors `Retry-After`**, telling the user roughly how long to wait. This requires surfacing the header — see Task 1 and Trap 1.

**Validation**

7. Client validation enforces **non-empty only** on both fields (trimmed), matching the backend's non-empty-only validator. **No length, format, or regex rules** — those would leak "that isn't a valid login shape" and would also lock out returning users whose existing password predates current rules. [Story 1.2 AC7]
8. Submit is disabled until the form is **dirty and valid**; validation surfaces on **blur + submit**; the button shows an inline spinner while in flight. [UX-DR15, FE Guide §2.3]
9. A `400` maps `errors[field]` to inline field errors, tied via `aria-describedby` and announced via `aria-live`. This is the **only** status that produces per-field errors.

**Accessibility (NFR10 floor — non-negotiable)**

10. Exactly one `<h1>`; every input tied to a label via `for`/`id`; visible focus ring; fully keyboard operable with no traps; error regions carry `aria-live`; error banners use `role="alert"`. [NFR10, UX-DR17]

**No dead ends**

11. The page offers a route to sign-up for users without an account. *(Extrapolated — sign-up's own 409 copy already says "try signing in instead," implying the reciprocal link. Supports FR38.)*

## Tasks / Subtasks

- [ ] **Task 1 — Surface `Retry-After` through the error pipeline (AC: 6)** ⚠️ *Shared-code change — required, see Trap 1.*
  - [ ] Add an optional `retryAfterSeconds?: number` to the `ProblemDetails` interface in `frontend/src/shared/api/problem-details.ts`.
  - [ ] Populate it in `http.interceptor.ts`'s `toProblemDetails()` — **after** calling `normalizeProblemDetails(error.error, error.status)`, read `error.headers.get('Retry-After')` and parse it as an integer number of seconds. Story 1.2 emits it as a delta-seconds string. Guard `NaN`/null → leave `undefined`.
  - [ ] **Do not change `normalizeProblemDetails`'s signature** — it is a pure `(body, status)` function with its own passing spec. Attach the header-derived field in the interceptor layer only.
  - [ ] Extend `http.interceptor.spec.ts`: a 429 with `Retry-After: 900` yields `retryAfterSeconds === 900`; a 429 without the header yields `undefined`; a non-numeric header yields `undefined`.
- [ ] **Task 2 — Types for the not-yet-generated contract (AC: 1)**
  - [ ] Hand-add to `frontend/src/entities/user/model.ts`:
    ```ts
    /** Request body for `POST /api/v1/users/sessions` (sign-in). */
    export interface SignInCommand { identifier: string; password: string }

    /** `200 OK` body returned by `POST /api/v1/users/sessions`. */
    export type SignInResult = CreateUserResult & { accessToken: string };
    ```
  - [ ] Add a comment noting these are **hand-written pending Story 1.2**, and should be replaced with `components['schemas'][...]` aliases once the backend ships and `npm run gen:api` is re-run. The names deliberately match the backend types so the swap is mechanical.
  - [ ] ⚠️ Do **not** run `npm run gen:api` expecting sign-in types — see Trap 5.
- [ ] **Task 3 — `signIn()` on `UserService` (AC: 1)**
  - [ ] Add directly after `register()` in `frontend/src/entities/user/user.service.ts`, mirroring it exactly — a bare POST that does **not** write the profile signal:
    ```ts
    signIn(input: SignInCommand): Observable<SignInResult> {
      return this.http.post<SignInResult>('/users/sessions', input);
    }
    ```
  - [ ] Extend `user.service.spec.ts`: asserts POST to `/users/sessions`, request body, `withCredentials`, and a flushed 200.
- [ ] **Task 4 — Sign-in validators (AC: 7)**
  - [ ] New `frontend/src/features/user/sign-in/sign-in.validators.ts` following the house `ValidatorFn`-factory pattern. Both validators are **non-empty after trim** only, returning `{ required: true }`.
  - [ ] ⚠️ **Do not import or reuse sign-up's `passwordValidator()`** — it enforces min 8 / max 50. See Trap 4.
  - [ ] Spec file mirroring `sign-up.validators.spec.ts`'s bare-`FormControl` `run()` helper — cover `''`, `'   '`, and a valid value for each.
- [ ] **Task 5 — The page component (AC: 1, 2, 3, 4, 5, 8, 9)**
  - [ ] `frontend/src/pages/auth-sign-in/sign-in.page.ts` + `.html`, mirroring `pages/auth-sign-up/` structure: standalone, `ChangeDetectionStrategy.OnPush`, `imports: [ReactiveFormsModule, TextFieldComponent]`, external `templateUrl`.
  - [ ] Typed form: `interface SignInForm { identifier: FormControl<string>; password: FormControl<string> }`, both `nonNullable: true`.
  - [ ] Signals: `submitting`, `generalError`, `rateLimitMessage`. **No `conflictMessage`** — sign-in has no 409.
  - [ ] `canSubmit` getter: `form.dirty && form.valid && !submitting()`.
  - [ ] `errorFor(field)` gated on `touched || dirty`, same as sign-up.
  - [ ] Submit: `signIn()` → `switchMap(fetchCurrentUser())` → navigate. **`finalize()` to clear `submitting`.** Handle the hydration-failure case per AC 3 (Trap 2).
  - [ ] `handleError(problem)` branches — note the ordering differs from sign-up's:
    - `401` → `generalError.set('Invalid credentials')`. **Never** touch field errors.
    - `429` → `rateLimitMessage.set(...)`, incorporating `problem.retryAfterSeconds` when present.
    - `400 && problem.errors` → per-field via `setErrors({ server: ... })`, same as sign-up.
    - anything else → `generalError.set(problem.detail ?? problem.title)`.
  - [ ] Template mirrors `sign-up.page.html`: `<main>` flex-centered → `<section aria-labelledby>` card → mono eyebrow `<p>` → single `<h1 id>` (serif-italic accent word permitted) → subtitle → banner divs with `role="alert"` + `aria-live="assertive"` → `<form novalidate [formGroup] (ngSubmit)>` → two `<ui-text-field formControlName …>` → submit button with spinner. Reference **tokens only**, never hardcoded hex/px (UX-DR1).
  - [ ] `autocomplete`: `username` on identifier, `current-password` on password (**not** `new-password` — that's sign-up's value and suppresses password-manager fill on a login form).
  - [ ] Add the sign-up link for AC 11.
- [ ] **Task 6 — Route registration (AC: 1)**
  - [ ] Add to `frontend/src/app/app.routes.ts`, lazy-loaded and unguarded (no guards exist yet — Story 1.4's job):
    ```ts
    { path: 'sign-in', loadComponent: () => import('@pages/auth-sign-in/sign-in.page').then((m) => m.SignInPage) },
    ```
  - [ ] Leave the existing `''` → `sign-up` redirect and `**` fallback **unchanged** — retargeting them is Story 1.4/1.5 scope.
- [ ] **Task 7 — Tests (all ACs)** — see Testing Requirements.
- [ ] **Task 8 — Verify green**
  - [ ] `cd frontend && npx ng test --no-watch` (this is the CI command; the builder enforces the coverage gate itself).

## Dev Notes

### ⚠️ Five traps

**Trap 1 — `Retry-After` is currently unreachable from any component.**
AC 6 cannot be satisfied without Task 1. `http.interceptor.ts` catches the `HttpErrorResponse` and rethrows a normalized `ProblemDetails`:
```ts
function toProblemDetails(error: unknown): ProblemDetails {
  if (error instanceof HttpErrorResponse) {
    return normalizeProblemDetails(error.error, error.status);   // headers discarded
  }
  return normalizeProblemDetails(null, 0);
}
```
Only the **body** and **status** survive. Components subscribe and receive a `ProblemDetails`, never an `HttpErrorResponse` — so reading `headers.get('Retry-After')` in the page is impossible. The header must be lifted in the interceptor. `ProblemDetails` today has `status, title, detail?, type?, instance?, errors?, traceId?, code?` — and no `retryAfter`.

**Trap 2 — do not copy sign-up's post-success error bug.**
`sign-up.page.ts:94-106` chains `register()` → `switchMap(fetchCurrentUser())` → `.subscribe({ error: handleError })`. A failed hydration **after** a successful `201` therefore renders a sign-up error, stranding a user whose account was in fact created and whose cookie is set. This is logged as **LOW-2** in `deferred-work.md`, verified still open.

Sign-in has the identical shape, and AC 3 forbids reproducing it. Catch the hydration failure separately and still navigate — e.g. `catchError` on the inner `fetchCurrentUser()` that swallows and returns `of(null)`, so the outer `next` still fires and routes to `/dashboard` with a null profile. Only failures of the **sign-in call itself** may surface an error.

**Trap 3 — the `401` handler is the opposite of sign-up's `400` handler.**
Sign-up's `handleError` maps server errors onto individual controls via `matchControl`. **Never do that for a 401.** Per-field attribution on a credential failure tells an attacker which half was wrong, defeating the byte-identical 401 the backend deliberately returns (Story 1.2 AC 4-5 goes to real lengths for this — including a dummy password verify to equalize timing). Reproducing that guarantee on the client means: one banner, no field marking, no "user not found" phrasing.

**Trap 4 — do not reuse sign-up's password validator.**
`passwordValidator()` enforces min 8 / max 50. Applying it to sign-in would client-side-block a returning user whose existing password predates those rules — they could never sign in, with no server round-trip to reveal why. The backend's sign-in validator is non-empty-only by deliberate design; mirror exactly that.

**Trap 5 — the generated schema has no sign-in types, and regenerating won't help.**
`shared/api/generated/schema.ts` is produced by `npm run gen:api` from the **running** backend's `/openapi/v1.json`. Story 1.2 hasn't shipped, so `/users/sessions` isn't in the schema. Running `gen:api` will not add it and may fail if the backend isn't running locally. Hand-write the two types (Task 2) and swap them for generated aliases after Story 1.2 merges.

**Also — `docs/FRONTEND_IMPLEMENTATION_GUIDE.md` §8.2 is stale.** It says a `401` should "Redirect to **sign-up** or auth recovery flow." That predates the sign-in endpoint. The correct target is `/sign-in` (Story 1.4 owns the interceptor-level redirect). Do not wire 401s to `/sign-up`.

### Wire contract (pinned by Story 1.2 — code against this, not against generated types)

```
POST /api/v1/users/sessions          [AllowAnonymous]
Request:  { "identifier": "daria@example.com" | "daria_dev", "password": "..." }

200 OK    Set-Cookie: auth-token=<jwt>; HttpOnly; SameSite=Strict; Secure(non-dev)
          { id, loginName, email, phone, location, about, avatar, accessToken }
          // accessToken is "" for cookie transport — the browser client ignores it
400       RFC 7807 + errors{field: [msg]}          — missing/empty identifier or password
401       RFC 7807, detail "Invalid credentials"   — unknown identifier OR wrong password OR soft-deleted
429       RFC 7807 + Retry-After header            — 5 failures / 15 min
```
The `401` body is deliberately identical for every failure cause. There is **no `409`** on this endpoint.

### Reuse — do not reinvent

| Need | Use | Location |
|---|---|---|
| Text input with label/error/a11y baked in | `<ui-text-field formControlName="…" [error]="errorFor('…')">` | `shared/ui/form/text-field.ts` |
| `withCredentials` + base URL + RFC 7807 normalization | already automatic via `httpInterceptor` | `shared/api/http.interceptor.ts` |
| Typed error shape | `ProblemDetails` | `shared/api/problem-details.ts` |
| Profile cache + `isAuthenticated` | `UserService.fetchCurrentUser()` / `profile()` | `entities/user/user.service.ts` |
| Page layout, banner, spinner-button markup | copy the structure of `sign-up.page.html` | `pages/auth-sign-up/` |

The `ui-empty-state` / `ui-error-state` / `ui-not-found-state` / `ui-skeleton` components are for list and detail pages. Sign-up uses none of them; sign-in shouldn't either — inline banner divs are the house style for auth pages.

### Testing requirements

Runner is **Vitest** via `@angular/build:unit-test`. Run once, non-watch: `npx ng test --no-watch` (from `frontend/`). Specs import `describe/it/expect/vi` from `'vitest'`.

Mirror `sign-up.page.spec.ts` exactly: `TestBed.configureTestingModule({ imports: [SignInPage], providers: [provideHttpClient(withInterceptors([httpInterceptor])), provideHttpClientTesting(), provideRouter([])] })`; assert requests with `httpMock.expectOne(`${env.apiBaseUrl}/users/sessions`)`; fake responses with `.flush(body, { status, statusText })`; spy navigation with `vi.spyOn(router, 'navigate').mockResolvedValue(true)`; call `httpMock.verify()` at the end of every HTTP test.

Required cases:
- submit disabled until dirty **and** valid
- valid submit → 200 → `GET /users/me` → `navigate(['/dashboard'])`
- **200 then a failing `/users/me` → still navigates to `/dashboard`, and no error banner renders** (AC 3 / Trap 2 regression guard)
- **401 → single generic "Invalid credentials" banner, and neither control has errors set** (AC 4 / Trap 3 regression guard — assert both the banner text *and* the absence of field errors)
- 429 with `Retry-After` → friendly message referencing the wait; body contains no `"429"`
- 429 without the header → still friendly, no `NaN`/`undefined` leaking into the copy
- 400 with `errors` → inline field errors
- invalid form on submit → no HTTP call at all (`httpMock.expectNone`)
- a11y smoke, verbatim from sign-up's suite: exactly one `<h1>`; every `<input>` has an `id` with a matching `label[for=id]`; on error `aria-invalid="true"` plus `aria-describedby` pointing at an element with `aria-live="polite"`

**Coverage gate:** enforced by the test builder itself via `angular.json` → `coverageThresholds: { perFile: true, lines: 80, statements: 80 }`. A failing file fails `ng test`. `scripts/check_coverage.py` is **backend-only** — it is not run against the frontend. The new page, validators, and service method are **not** in `coverageExclude` and must each clear 80%.

### Project Structure Notes

- Feature-sliced layout (AR11). New files land in `pages/auth-sign-in/`, `features/user/sign-in/`; `entities/user/` and `shared/api/` are extended, not duplicated.
- Path aliases in use: `@app/* @pages/* @features/* @entities/* @shared/* @widgets/* @processes/*`.
- `/sign-in` renders **outside the app shell** (no sidebar) — the shell is scoped to authenticated pages, per Story 1.5's AC ("when any **guarded** page renders"). *This is inference, not an explicit spec statement; sign-up already renders standalone and centered, and sign-in mirrors it.*
- Guards do not exist yet. Do not add them here — Story 1.4 owns route guards, the 401→re-auth interceptor, and intended-destination return. AC 1's "intended destination" should read from whatever return-URL mechanism exists at implementation time and fall back to `/dashboard`; do **not** build the guard infrastructure in this story.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md#Story-1.3`] — story statement and acceptance criteria
- [Source: `_bmad-output/implementation-artifacts/1-2-returning-user-sign-in-endpoint.md`] — the pinned wire contract this page codes against
- [Source: `_bmad-output/planning-artifacts/architecture/demo-mvp-architecture-decisions.md#Decision-1`] — AR11-AR17 (Angular stack, HTTP/errors, routing)
- [Source: `_bmad-output/planning-artifacts/ux-design-specification.md`] — UX-DR1 tokens, UX-DR15 button/feedback, UX-DR17 a11y floor. **No sign-in screen is specified; layout is extrapolated from sign-up.**
- [Source: `frontend/src/pages/auth-sign-up/sign-up.page.ts`, `.html`, `.spec.ts`] — the pattern to mirror, and the source of Trap 2
- [Source: `frontend/src/shared/api/http.interceptor.ts`, `problem-details.ts`] — Trap 1
- [Source: `frontend/src/entities/user/model.ts`] — `UserProfile = GetCurrentUserResult`, the reason AC 2 keeps the two-call flow
- [Source: `frontend/angular.json`, `.github/workflows/ci.yml`] — frontend coverage gate and CI command
- [Source: `_bmad-output/implementation-artifacts/deferred-work.md`] — LOW-2 (verified open), LOW-3, token generator

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List
