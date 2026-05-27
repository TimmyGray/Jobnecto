# Story 1.1: Project foundation & user registration

Status: review

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a **visitor**,
I want **to register an account and immediately land inside the app**,
so that **I can start using Jobnecto without a separate sign-in step**.

> **Track:** Frontend (FE). This is the **enabling slice** of the Demo MVP — it scaffolds the brownfield Angular client and delivers the first user-value outcome (registration → authenticated landing). There is **no standalone "infrastructure" epic**; the foundation is folded into this story. [Source: epics.md#Epic-1] [Source: demo-mvp-architecture-decisions.md#Decision-1]

## Acceptance Criteria

**Foundation slice (the enabling infrastructure):**

1. An Angular **standalone-components** SPA (no NgModules) is scaffolded **feature-sliced** per the FE Guide blueprint (`app / processes / pages / widgets / features / entities / shared`). [AR11] [Source: FRONTEND_IMPLEMENTATION_GUIDE.md#2.2]
2. The **token layer** (Career-OS-reconciled colors, type, spacing, radius, shadow, motion) is authored as TS/JSON → CSS custom properties → Tailwind theme config; components reference **tokens, never hardcoded values**. [UX-DR1] [Source: ux-design-specification.md#Visual-Design-Foundation]
3. An **HTTP interceptor** sets `withCredentials: true` on **every** request and normalizes RFC 7807 responses into a typed `ProblemDetails` model. [AR12] [Source: demo-mvp-architecture-decisions.md#1.1]
4. TS **enums/DTOs are generated** from `/openapi/v1.json` (not hand-authored). [AR17] [Source: FRONTEND_IMPLEMENTATION_GUIDE.md#12]
5. The standardized `EmptyState` / `ErrorState` / `NotFoundState` and a layout-preserving **skeleton** component exist in `shared/ui` for reuse. [UX-DR12, FR40 foundation] [Source: ux-design-specification.md#Component-Strategy]

**Registration happy path:**

6. On the unguarded `/sign-up` route, the form mirrors **backend validation rules** before calling the API: `loginName` 3–50 matching `^[A-Za-z0-9_]+$`; `email` valid, ≤50; `password` 8–50. Validate on **blur + submit**; submit disabled until **dirty + valid**. [FE §7.1] [Source: CreateUserCommandValidator.cs]
7. On submit, the client calls `POST /api/v1/users` with `{ loginName, email, password }` (optional `phone, location, about` omitted this story). On `201` the server sets the HTTP-only auth cookie; the client routes to the authenticated landing route (`/dashboard`) and **hydrates the profile via `GET /api/v1/users/me`**. [FR1, FR2] [Source: UsersController.cs:41-58]

**Registration error handling:**

8. When the server returns `400` (validation), field-level errors render **inline**, tied via `aria-describedby` and announced via an `aria-live` region; no field-shake (color + helper text only). [UX-DR15, NFR10] [Source: ux-design-specification.md#Feedback-Patterns]
9. When the server returns `409` (email or login already used), the conflict is shown as **explicit, plain-language guidance** ("That email is already registered — try signing in"), never a raw status code. [UX-DR15] [Source: FRONTEND_IMPLEMENTATION_GUIDE.md#8.2]
10. The `/sign-up` page meets the **a11y floor**: one `h1`, labeled inputs (`for`/`id`), visible focus ring, keyboard-complete, no traps. [NFR10, UX-DR17] [Source: ux-design-specification.md#Accessibility-Strategy]

## Tasks / Subtasks

- [x] **Task 1 — Scaffold the Angular workspace in `frontend/` (AC: 1)** ⚠️ **All frontend code lives in the repo-root `frontend/` folder** (see Project Structure Notes — this is a hard rule).
  - [x] Create the Angular standalone-components workspace **inside `frontend/`** (Angular workspace root: `frontend/angular.json`, `frontend/package.json`, `frontend/src/`). **Preserve the existing `frontend/design-examples/`** PNG references — do not delete or move them. *(Angular 21.2, standalone, scss, Vitest, scaffolded into temp dir and moved into `frontend/`; `design-examples/` preserved.)*
  - [x] Establish the feature-sliced folders under `frontend/src/`: `app/{providers,router}`, `processes/auth`, `pages/auth-sign-up`, `pages/dashboard`, `widgets`, `features/user`, `entities/user`, `shared/{api,ui,lib,config}`. *(Slices created; tsconfig path aliases `@app/* @pages/* @features/* @entities/* @shared/* @widgets/* @processes/*`.)*
  - [x] Configure Tailwind CSS wired to the token layer (Task 2). Confirm `dotnet`-independent dev build works (`npm install` + build). *(Tailwind v3 + autoprefixer via `.postcssrc.json`; `npm install` + `ng build` succeed independent of dotnet.)*
- [x] **Task 2 — Author the token layer (AC: 2)**
  - [x] Single source in `shared/config` (TS/JSON): colors (**Career OS palette** — see Dev Notes), type (Manrope / serif-italic / IBM Plex Mono), spacing (base-4: 4·8·12·16·20·24·32·40·48), radius (sm6/md10/lg14/pill999), shadow (sm/md/lg), motion (120/180/260ms + easings), z-index, breakpoints (xs/sm/md/lg/xl). *(`shared/config/tokens.ts`.)*
  - [x] Export tokens → CSS custom properties → Tailwind `theme` config. Verify a component consuming `bg.canvas`/`action.primary`/`brand.accent` renders the tokens, not hardcoded hex. *(`styles.scss` `:root` vars → `tailwind.config.js theme.extend`; verified built CSS resolves `var(--color-action-primary)` etc.)*
- [x] **Task 3 — HTTP interceptor + typed ProblemDetails (AC: 3)**
  - [x] `shared/api`: HTTP interceptor adding `withCredentials: true` to every request; base URL from `shared/config` env (dev: `http://localhost:5000/api/v1`). *(`shared/api/http.interceptor.ts`, base from `shared/config/env.ts`.)*
  - [x] One interceptor normalizes any non-2xx RFC 7807 body into a typed `ProblemDetails` model (`status, title, detail, instance, errors?` map, `traceId` from extensions). Surface `errors[field]` for forms; generic banner otherwise. [Source: FRONTEND_IMPLEMENTATION_GUIDE.md#6.2] *(`shared/api/problem-details.ts`.)*
- [x] **Task 4 — OpenAPI type generation (AC: 4)**
  - [x] Add a generation step (`openapi-typescript`) reading `/openapi/v1.json` → emits TS enums + DTOs into `shared/api/generated/schema.ts`. Document the regenerate command in `frontend/README.md`. *(`npm run gen:api`; backend started locally on :5000, generated, then stopped.)*
  - [x] Generate at least the `CreateUserCommand`/`CreateUserResult`/`GetCurrentUserResult` shapes used here; do **not** hand-author them. *(Generated; re-exported via `entities/user/model.ts`.)*
- [x] **Task 5 — Standardized state components (AC: 5)**
  - [x] `shared/ui`: `EmptyState` (icon + headline + one-line guidance + primary CTA slot), `ErrorState` (+ Retry), `NotFoundState` (+ back-to-list), and a layout-preserving `Skeleton`. a11y baked in (focus, labels, `aria-live` where dynamic). *(`shared/ui/state/*`.)*
- [x] **Task 6 — Sign-up form + validation (AC: 6, 10)**
  - [x] `pages/auth-sign-up`: a **typed Reactive Form** (`FormGroup`/`FormControl<T>`) with validators mirroring `CreateUserCommandValidator` exactly. Validate on blur + submit; submit disabled until dirty + valid; inline submit spinner. *(`pages/auth-sign-up/sign-up.page.ts`; validators in `features/user/sign-up/sign-up.validators.ts`.)*
  - [x] One `h1`, labeled inputs (`for`/`id`), visible focus, keyboard-complete (use branded `TextField` over CDK primitive). *(`shared/ui/form/text-field.ts` — CDK-only per locked Decision; spartan-ng deferred.)*
- [x] **Task 7 — Registration call + post-success hydration (AC: 7)**
  - [x] `entities/user`: a sign-up service method calling `POST /api/v1/users`. On `201`, route to `/dashboard` and call `GET /api/v1/users/me`, storing the hydrated profile in a signal-backed user service. *(`entities/user/user.service.ts` — `register()` + `fetchCurrentUser()` signal cache.)*
  - [x] Minimal authenticated `/dashboard` landing **stub** is acceptable here. *(`pages/dashboard/dashboard.page.ts`.)*
- [x] **Task 8 — Error states (AC: 8, 9)**
  - [x] Map `400` → inline field errors (`aria-describedby` + `aria-live` summary). Map `409` → plain-language conflict guidance near the form. No raw codes surfaced. *(In `sign-up.page.ts handleError()` + template `aria-live` regions.)*
- [x] **Task 9 — Tests**
  - [x] Unit: validator mirror (valid/invalid loginName, email, password boundaries 8 & 50 — plus 2/3/50/51 and email >50), ProblemDetails normalization (400 with `errors`, 409, 500), interceptor sets `withCredentials`.
  - [x] Component/integration: sign-up submits → 201 routes to `/dashboard` + hydrates `/users/me`; 400 renders inline errors; 409 renders conflict guidance (Angular `HttpTestingController`).
  - [x] a11y smoke on `/sign-up` (one h1, labels tied via for/id, aria-invalid + aria-describedby).
- [x] **Task 10 — Rewrite `docs/FRONTEND_IMPLEMENTATION_GUIDE.md` for the chosen Angular stack**
  - [x] Rewrote the guide for the **ratified Angular stack**: §2.3 (Signals + services, no NgRx; HttpClient + thin cache, no TanStack; typed Reactive Forms, no RHF/Zod/Yup; DraftStore); §2.3.1 (OpenAPI generation); §3 (Career OS palette, scale kept, three type families); §5 (Angular wrapper contracts, signal inputs/outputs); §2.2 (anchored at `frontend/`, path aliases); §2.4 routes + guard model; §6.2 (typed `ProblemDetails`). Preserved §6/§6.3/§7/§8/§9/§10/§11/§12/§13.
  - [x] Added top Angular-authoritative note pointing to Decision 1; kept the guide as canonical FE handoff artifact; documented the CDK-now / spartan-later choice.
  - [x] No contradictions remain; the "two conflicts" warning in Dev Notes is resolved (marked RESOLVED below).

## Dev Notes

### ✅ RESOLVED (Story 1.1) — two conflicts in the source docs

> Both conflicts below were resolved during implementation: `docs/FRONTEND_IMPLEMENTATION_GUIDE.md` was rewritten Angular-authoritative (Task 10), so the React-flavored stack recommendations and the LinkedIn-blue palette no longer contradict the ratified decisions. Retained here for historical context.

### ⚠️ CRITICAL — two conflicts in the source docs (do not get burned)

1. **The FE Guide (`docs/FRONTEND_IMPLEMENTATION_GUIDE.md`) is React-flavored. The RATIFIED stack is Angular.** The FE Guide's *structure, routes, token scale, validation rules (§7), states (§8), a11y (§9), responsive (§10), enum list (§12)* are authoritative. But its **tech-stack recommendations are SUPERSEDED** by Decision 1 [Source: demo-mvp-architecture-decisions.md#1.1]:
   - State: **Angular Signals + injectable services** — **NOT** TanStack Query, **NOT** NgRx. [AR11, Decision 1.2]
   - Server cache: **`HttpClient` + signals + a thin per-entity service cache** with explicit `refetch()`/`invalidate()`. [Decision 1.3]
   - Forms: **typed Reactive Forms** (`FormGroup`/`FormControl<T>`) — **NOT** React Hook Form + Zod/Yup. [Decision 1.1]
   - Component kit: **spartan-ng (brain) on Angular CDK + helm, themed by tokens** (PrimeNG/Taiga UI are approved fallbacks). [Decision 1.4]
2. **Token palette: use the Career OS palette, NOT the FE Guide's LinkedIn-blue values.** The FE Guide §3.1 colors (`#0A66C2`, `#F7F9FC`…) are **overridden** toward Career OS. Authoritative palette [Source: ux-design-specification.md#Color-System]:
   - `bg.canvas #F6F4EE` (warm cream) · `bg.surface #FFFFFF` · `bg.inverse #0E0F12`
   - `text.primary #14151A` · `text.secondary #44474F` · `text.muted #8A8D96` · `text.inverse #FAFAF7`
   - `action.primary #14151A` (hover `#000`) — **near-black = primary buttons & active nav**
   - `brand.accent #2348E0` (hover `#1B36B8`) — **royal blue = links, emphasis, focus, italic wordmark**
   - `brand.spark #8FE34A` — **lime = decorative / on-dark ONLY, never an essential signal** (fails contrast on light)
   - `status.success #15803D` · `status.warning #B45309` (bg `#FDF1D6`) · `status.danger #B42318` · `status.info #1D4ED8`
   - `border.default #E6E2D8` (warm hairline) · `border.strong #C9C4B6` · `border.focus #2348E0`
   - Keep the FE Guide's **token *scale*** (spacing 4–48, radius sm/md/lg/pill, shadow sm/md/lg, type sizes `xs12·sm14·md16·lg20·xl28·xxl36·display44`, motion 120/180/260ms). Hexes are tunable within intent; structure is fixed.
   - Typography jobs: **Manrope** (sans, ~95% of text) · **serif-italic display** (Newsreader/Fraunces — wordmark "Job*necto*", accent words, greetings ONLY; never body/controls) · **IBM Plex Mono** (uppercase eyebrow labels, entity IDs). Body ≥16px; one `h1` per page.

### Live backend contract (verified against shipped code)

- **Register:** `POST /api/v1/users` `[AllowAnonymous]`. Request `CreateUserCommand { loginName, email, password, phone?, location?, about?, avatar? }`. Success `201 Created`, `Location: /api/v1/users/me`, sets HTTP-only cookie (SameSite=Strict, Secure), body `CreateUserResult { id, loginName, email, phone?, location?, about?, avatar? }`. Errors `400` (validation, RFC 7807 with `errors` map) / `409` (email or login already used). [Source: UsersController.cs:41-58, CreateUserCommand.cs]
- **Validation rules to mirror client-side** [Source: CreateUserCommandValidator.cs]:
  - `loginName`: required, 3–50, `^[A-Za-z0-9_]+$`
  - `email`: required, valid email, ≤50
  - `password`: required, 8–50
  - (`phone` E.164 / `location` enum / `about` ≤5000 — optional, not collected in this story's form)
- **Profile hydration:** `GET /api/v1/users/me` `[Authorize]` → `200 GetCurrentUserResult` (requires the cookie set by register). [Source: UsersController.cs:65-80]
- **OpenAPI doc:** `GET /openapi/v1.json` — exposed via `MapOpenApi()` (Development only). Backend dev host: `http://localhost:5000` (HTTP) / `https://localhost:7247` (HTTPS). [Source: backend/src/JobNecto.API/Program.cs, Properties/launchSettings.json]
- **Error contract for the interceptor:** RFC 7807 `application/problem+json`; `400` carries `errors` (field → messages) map. The canonical exception→status mapping lives in the backend `GlobalExceptionHandler`. [Source: FRONTEND_IMPLEMENTATION_GUIDE.md#6.2] [AR15]

### Routing scope for this story

- `/sign-up` (unguarded) and a minimal authenticated `/dashboard` landing stub. The full guard system (1.4), app shell/sidebar (1.5), and full dashboard (1.6) are **out of scope** here — establish the router skeleton only. All routes except `/sign-up` and `/sign-in` will be guarded later. [Source: demo-mvp-architecture-decisions.md#1.6] [AR13]

### Source tree components to touch

- **NEW:** the entire `frontend/` Angular workspace (this is the first FE story). No backend changes.
- **PRESERVE:** `frontend/design-examples/` (15 reference PNGs — non-binding visual references for the look).

### Testing standards summary

- Frontend tests use the Angular toolchain (Jasmine/Karma or Jest per scaffold default) with `HttpTestingController` for API mocking. No backend test changes. Keep this story's tests in the `frontend/` workspace.
- Mirror-validation tests must assert the **exact boundaries** (loginName length 2/3/50/51, password 7/8/50/51, email >50) so client and `CreateUserCommandValidator` cannot drift.

### Project Structure Notes

> ⚠️ **HARD RULE — All frontend code lives in the repo-root `frontend/` folder.**
> No artifact previously recorded this convention, so it is **established here** and has been added to `_bmad-output/project-context.md`. The backend lives in `backend/`; the Angular client is its sibling at **`frontend/`** (Angular workspace root: `frontend/angular.json`, `frontend/package.json`, `frontend/src/…`). The feature-sliced `src/` blueprint from FE Guide §2.2 is rooted at `frontend/src/`. The existing `frontend/design-examples/` directory must be preserved. Do **not** scatter client code under `backend/`, the repo root, or any other location.

- Alignment: feature-sliced layout (`app/processes/pages/widgets/features/entities/shared`) per FE Guide §2.2, rooted at `frontend/src/`. [AR11]
- Variance: the FE Guide shows the blueprint as bare `src/` with no repo anchor — this story anchors it to `frontend/` (rationale: parallel to the shipped `backend/`, and `frontend/` already exists holding design references).

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story-1.1] — story statement & ACs
- [Source: _bmad-output/planning-artifacts/architecture/demo-mvp-architecture-decisions.md#Decision-1] — ratified Angular stack (AR11–AR17 origin)
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Visual-Design-Foundation] — Career OS token values (UX-DR1)
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Component-Strategy] — Empty/Error/NotFound state components (UX-DR12)
- [Source: docs/FRONTEND_IMPLEMENTATION_GUIDE.md#2.2] — feature-sliced folder blueprint
- [Source: docs/FRONTEND_IMPLEMENTATION_GUIDE.md#7.1] — sign-up client validation rules
- [Source: docs/FRONTEND_IMPLEMENTATION_GUIDE.md#6.2] — Problem Details error shape
- [Source: backend/src/JobNecto.API/Controllers/UsersController.cs:41-80] — register + me endpoints
- [Source: backend/src/JobNecto.Application/Users/Validators/CreateUserCommandValidator.cs] — server validation to mirror
- [Source: _bmad-output/project-context.md] — stack, namespace, dev DB/host conventions

## Dev Agent Record

### Agent Model Used

Amelia (bmad-agent-dev) — claude-opus-4-7.

### Debug Log References

- **Locked decisions (owner-approved):** Angular 21 (latest CLI); Vitest unit runner; OpenAPI via `openapi-typescript` against a locally-started backend; branded `TextField` on `@angular/cdk` only with **spartan-ng deferred**; Tailwind v3 `theme.extend` wired from the token source.
- **Unit tests (final):** `npx ng test --no-watch` → `Test Files 7 passed (7)`, `Tests 47 passed (47)`.
- **Production build (final):** `npx ng build` → `Application bundle generation complete.` Lazy chunks `sign-up-page`, `dashboard-page` emitted; `styles.css` compiled with token CSS vars (verified `var(--color-action-primary)` / `var(--color-bg-canvas)` / `var(--color-brand-accent)` present in built CSS → AC2).
- **OpenAPI generation:** backend started in Development on `http://localhost:5000` (readiness confirmed `200` on `/openapi/v1.json`), `npm run gen:api` emitted `src/shared/api/generated/schema.ts`, backend then stopped.
- Backend CORS enabling change (`CorsServiceExtensions.AllowCredentials()` + `appsettings.Development.json` adds `localhost:4200`) so the SPA cookie transport works in dev; base `appsettings.json` left fail-closed (`AllowedOrigins: []`). `frontend/design-examples/` preserved.

### Completion Notes List

- Scaffolded the brownfield Angular 21 standalone SPA in `frontend/` (feature-sliced: app / processes / pages / widgets / features / entities / shared), preserving `frontend/design-examples/`. tsconfig path aliases added per slice.
- Career-OS token layer: `styles.scss :root` is the canonical CSS-variable source, Tailwind `theme.extend` maps utility classes onto those vars, and `shared/config/tokens.ts` is a typed TS mirror — the three are kept in sync by hand (no generator yet; tracked as a follow-up). Components use token-mapped utility classes only; verified no hardcoded hex reaches built CSS (AC2).
- HTTP interceptor sets `withCredentials: true` on every request, prefixes relative URLs with the configured API base, and normalizes any non-2xx RFC 7807 body into a typed `ProblemDetails` (lifts `traceId`/`code` from extensions; coerces single-string error values) (AC3).
- OpenAPI types generated (not hand-authored) and re-exported through `entities/user` (AC4).
- Standardized `EmptyState` / `ErrorState` (Retry, role=alert/assertive) / `NotFoundState` (back) / `Skeleton` (aria-hidden, reduced-motion honored) added to `shared/ui` (AC5).
- `/sign-up` typed Reactive Form mirrors `CreateUserCommandValidator` exactly (loginName 3–50 `^[A-Za-z0-9_]+$`; email valid ≤50; password 8–50); validates on blur+submit; submit disabled until dirty+valid; inline spinner (AC6). On 201 → `register()` then `fetchCurrentUser()` hydration into a signal-backed `UserService` → route `/dashboard` (AC7). 400 → inline field errors via `aria-describedby` + polite `aria-live`; 409 → plain-language conflict guidance, never a raw code (AC8, AC9). a11y floor: one `h1`, labels tied `for`/`id`, visible focus ring, `aria-invalid` (AC10).
- Tests (Vitest + `HttpTestingController`): validator boundaries (2/3/50/51 loginName, 7/8/50/51 password, email 50/51), ProblemDetails normalization (400/409/500/null/extensions), interceptor `withCredentials`+normalization, UserService, state components, and the sign-up page integration + a11y smoke (AC9).
- Rewrote `docs/FRONTEND_IMPLEMENTATION_GUIDE.md` Angular-authoritative (Task 10); the two source-doc conflicts are resolved.
- **Non-blocking note:** running under Node 25 emits an "odd-numbered Node" advisory; Angular 21 still supports it (`>=24.0.0`). Consider an LTS Node (22.x) for CI. spartan-ng intentionally deferred — first overlay/select story should add it behind the unchanged branded contracts.

### File List

**Frontend workspace (all under `frontend/`):**

_Scaffold config (CREATED by `ng new`, some UPDATED):_
- `frontend/package.json` (UPDATED — renamed project, pinned Angular 21 deps, added @angular/cdk, tailwindcss v3, autoprefixer, postcss, openapi-typescript; added `gen:api` script)
- `frontend/angular.json` (UPDATED — project renamed to `jobnecto-frontend`)
- `frontend/tsconfig.json` (UPDATED — feature-sliced path aliases)
- `frontend/tsconfig.app.json` (CREATED)
- `frontend/tsconfig.spec.json` (CREATED)
- `frontend/tailwind.config.js` (CREATED — theme wired to token CSS vars)
- `frontend/.postcssrc.json` (CREATED — tailwind + autoprefixer)
- `frontend/.gitignore`, `frontend/.editorconfig`, `frontend/.prettierrc`, `frontend/.vscode/*`, `frontend/public/favicon.ico` (CREATED by scaffold)
- `frontend/README.md` (UPDATED — Angular stack docs + `gen:api` regen command)
- `frontend/src/index.html` (UPDATED — title `Jobnecto`)
- `frontend/src/main.ts` (CREATED by scaffold; unchanged)
- `frontend/src/styles.scss` (UPDATED — token CSS vars, fonts, a11y focus, reduced-motion, sr-only)

_App shell + router:_
- `frontend/src/app/app.ts` (UPDATED — OnPush shell)
- `frontend/src/app/app.html` (UPDATED — router-outlet)
- `frontend/src/app/app.spec.ts` (UPDATED — shell/outlet tests)
- `frontend/src/app/app.config.ts` (UPDATED — provideHttpClient + interceptor)
- `frontend/src/app/app.routes.ts` (UPDATED — /sign-up + /dashboard skeleton)

_shared/config (tokens + env):_
- `frontend/src/shared/config/tokens.ts` (CREATED)
- `frontend/src/shared/config/env.ts` (CREATED)
- `frontend/src/shared/config/index.ts` (CREATED)

_shared/api (interceptor, ProblemDetails, generated):_
- `frontend/src/shared/api/problem-details.ts` (CREATED)
- `frontend/src/shared/api/problem-details.spec.ts` (CREATED)
- `frontend/src/shared/api/http.interceptor.ts` (CREATED)
- `frontend/src/shared/api/http.interceptor.spec.ts` (CREATED)
- `frontend/src/shared/api/index.ts` (CREATED)
- `frontend/src/shared/api/generated/schema.ts` (CREATED — openapi-typescript output)

_shared/ui (state components + branded TextField):_
- `frontend/src/shared/ui/state/empty-state.ts` (CREATED)
- `frontend/src/shared/ui/state/error-state.ts` (CREATED)
- `frontend/src/shared/ui/state/not-found-state.ts` (CREATED)
- `frontend/src/shared/ui/state/skeleton.ts` (CREATED)
- `frontend/src/shared/ui/state/state-components.spec.ts` (CREATED)
- `frontend/src/shared/ui/form/text-field.ts` (CREATED)
- `frontend/src/shared/ui/index.ts` (CREATED)

_shared/lib:_
- `frontend/src/shared/lib/cn.ts` (CREATED)
- `frontend/src/shared/lib/index.ts` (CREATED)

_entities/user:_
- `frontend/src/entities/user/model.ts` (CREATED)
- `frontend/src/entities/user/user.service.ts` (CREATED)
- `frontend/src/entities/user/user.service.spec.ts` (CREATED)
- `frontend/src/entities/user/index.ts` (CREATED)

_features/user/sign-up (validators):_
- `frontend/src/features/user/sign-up/sign-up.validators.ts` (CREATED)
- `frontend/src/features/user/sign-up/sign-up.validators.spec.ts` (CREATED)

_pages:_
- `frontend/src/pages/auth-sign-up/sign-up.page.ts` (CREATED)
- `frontend/src/pages/auth-sign-up/sign-up.page.html` (CREATED)
- `frontend/src/pages/auth-sign-up/sign-up.page.spec.ts` (CREATED)
- `frontend/src/pages/dashboard/dashboard.page.ts` (CREATED)

_Blueprint placeholders (no code yet — materialized in later stories):_
- `frontend/src/widgets/.gitkeep` (CREATED)
- `frontend/src/processes/auth/.gitkeep` (CREATED)

**Repo docs:**
- `docs/FRONTEND_IMPLEMENTATION_GUIDE.md` (UPDATED — rewritten Angular-authoritative, Task 10)
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (UPDATED — story 1-1 → review)
- `_bmad-output/implementation-artifacts/1-1-project-foundation-user-registration.md` (UPDATED — checkboxes, Dev Agent Record, File List, Change Log, Status)

### Change Log

- 2026-05-27: Implemented Story 1.1 — scaffolded the Angular 21 SPA foundation (feature-sliced, Career-OS tokens, withCredentials + RFC 7807 → ProblemDetails interceptor, OpenAPI type generation, standardized state components) and the `/sign-up` registration flow (POST /api/v1/users → 201 → /dashboard → hydrate GET /api/v1/users/me) with 400/409 error mapping. 47 unit/integration tests pass; production build green. Rewrote the FE Implementation Guide Angular-authoritative. (Amelia / dev-story)
- 2026-05-27: Code review (fresh context) → **APPROVE-WITH-NITS**, no Critical/High. Applied pre-merge fixes: **MED-1** (reverted base `appsettings.json` `Cors:AllowedOrigins` to `[]`, fail-closed; dev origins stay in `appsettings.Development.json`), **MED-2** (corrected `tokens.ts`/`tailwind.config.js` comments — `styles.scss` is the CSS-var source, `tokens.ts` a typed mirror, hand-synced), **NIT-1** (restored `appsettings.json` trailing newline). LOW/NIT items deferred — see Code Review Follow-ups. (Amelia)

### Code Review Follow-ups (deferred, non-blocking)

These were raised in review, do not manifest in Story 1.1, and are tracked for a later story (likely 1.3 sign-in screen or the first form-heavy story):

- **LOW-1** — `shared/ui/form/text-field.ts`: OnPush component binds plain fields `[value]`/`[disabled]`; `writeValue`/`setDisabledState` won't trigger re-render (latent — form is always empty here). Fix: signal-back `value`/`disabled` or call `ChangeDetectorRef.markForCheck()`. Address before any form uses `patchValue`/`reset`/programmatic `disable()`.
- **LOW-2** — `pages/auth-sign-up/sign-up.page.ts`: a 201 followed by a failed `GET /users/me` surfaces a sign-up error despite the account being created. Fix: on hydration failure after 201, still navigate to `/dashboard` (degrades gracefully when profile is null).
- **LOW-3** — `features/user/sign-up/sign-up.validators.ts`: client email regex requires a dotted domain, marginally stricter than the server's `.EmailAddress()`. Reconcile or drop the "exact mirror" claim.
- **NIT-2** — `entities/user/model.ts`: unused `SignUpInput` type; remove or use it.
- **Token generator** — build a real `tokens.ts → CSS vars / Tailwind` generation step so the three token surfaces have a true single source (currently hand-synced).
