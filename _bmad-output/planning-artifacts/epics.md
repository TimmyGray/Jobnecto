---
stepsCompleted:
  - step-01-validate-prerequisites
  - step-02-design-epics
  - step-03-create-stories
  - step-04-final-validation
inputDocuments:
  - e:\apps\Jobnecto\_bmad-output\planning-artifacts\prd-demo-mvp.md
  - e:\apps\Jobnecto\_bmad-output\planning-artifacts\ux-design-specification.md
  - e:\apps\Jobnecto\_bmad-output\planning-artifacts\architecture\demo-mvp-architecture-decisions.md
  - e:\apps\Jobnecto\_bmad-output\planning-artifacts\architecture\authorization-contract-matrix.md
  - e:\apps\Jobnecto\docs\FRONTEND_IMPLEMENTATION_GUIDE.md
  - e:\apps\Jobnecto\_bmad-output\project-context.md
---

# Jobnecto Demo MVP — Epic Breakdown

> **✅ Scope: DEMO MVP / PHASE D — Angular SPA client + LLM cover-letter generation (PRD `prd-demo-mvp.md`, FR1–FR41).**
> This is the **governing** epic set for the current increment (7 epics). Not to be confused with the legacy **Phase B backend** epics under **`_bmad-output/planning-artifacts/epics/`** (parent PRD `prd.md`, FR1–FR28, already merged), whose FR numbers mean different things.

## Overview

This document provides the complete epic and story breakdown for the **Jobnecto Demo MVP (Phase D)**, decomposing the requirements from the Demo-MVP PRD, the UX Design Specification, and the ratified Architecture Decisions into implementable stories.

**Increment context (brownfield):** The .NET 10 REST backend is shipped and ownership-hardened through Phase C. This increment layers an **Angular SPA client** over the full shipped surface plus **two new backend capabilities** (sign-in, LLM generation) and a **seeded vacancy data track**. There is **no greenfield starter epic** — Angular project scaffolding is the enabling slice of the first user-value story.

**Three parallel tracks, coordinated contract-first:**

- **Frontend (FE)** — the Angular client across the full surface.
- **Backend-LLM (BE)** — `POST /api/v1/users/sessions` (sign-in) + `POST /api/v1/cover-letters/generate` (LLM draft service). Both contracts are pinned in the architecture decisions; FE and BE stories run in parallel against the frozen contracts.
- **Data/seed (SEED)** — mocked vacancies behind the swappable `JobSource` seam (synthetic data only).

**Sequencing principle (PRD trim order):** the magic spine — résumé → vacancy → generate → save — is **never** gated behind completeness work (avatar, rich filtering, full AA, polished mobile). Approved trim order under resource risk: pixel-perfect mobile → full AA → rich filtering UI → education UI → avatar.

## Requirements Inventory

### Functional Requirements

**Account & Session**

- **FR1:** A visitor can register a new account with login name, email, and password.
- **FR2:** A user obtains an authenticated session upon successful registration.
- **FR3:** A user's active session can be renewed without re-entering credentials.
- **FR4:** A returning user can sign in with their credentials to establish a session. *(Requires NEW backend capability — `POST /api/v1/users/sessions`, Architecture Decision 2.)*
- **FR5:** A user with an expired/invalid session is routed to authentication and returned to their intended destination afterward.
- **FR6:** The system restricts all capabilities except registration and sign-in to authenticated users.

**Profile Management**

- **FR7:** A user can view their own profile (login name, email, phone, location, about, avatar).
- **FR8:** A user can update their profile fields individually (partial update).
- **FR9:** A user can change their login name, subject to system-wide uniqueness.
- **FR10:** A user can upload, replace, and remove their avatar image.

**Resume Management**

- **FR11:** A user can create a resume capturing skills, experience, work-location preference, salary expectation, and related fields.
- **FR12:** A user can view a paginated list of their own resumes.
- **FR13:** A user can view the full detail of one of their resumes.
- **FR14:** A user can update a resume.
- **FR15:** A user can delete (soft-delete) a resume.

**Education Management**

- **FR16:** A user can create an education record (title, specialization, degree).
- **FR17:** A user can view a paginated list of their education records.
- **FR18:** A user can view, update, and delete (soft-delete) an individual education record.

**Cover Letter Template Management**

- **FR19:** A user can create a reusable cover-letter template with a name (unique per user) and content.
- **FR20:** A user can view a paginated, name-searchable list of their templates.
- **FR21:** A user can view, update, and delete (soft-delete) an individual template.
- **FR22:** A user can insert a template's content into the cover-letter editor as a starting point.

**Vacancy Discovery (mocked data this release)**

- **FR23:** A user can browse a paginated board of available vacancies.
- **FR24:** A user can filter vacancies by multiple criteria (skills, location, salary range, work-location type, etc.).
- **FR25:** A user can view the full detail of a vacancy.
- **FR26:** The system provides a realistic, seeded set of mock vacancies behind a source-swappable seam.

**Cover Letter Composition, Generation & Management**

- **FR27:** A user can generate an AI-drafted cover letter for a chosen vacancy, grounded in their own resume. *(Requires NEW backend capability — `POST /api/v1/cover-letters/generate`, Architecture Decision 3.)*
- **FR28:** The system presents AI generation output as an **editable draft** and does not persist it automatically.
- **FR29:** The system informs the user that generation sends their resume and vacancy data to a third-party AI provider.
- **FR30:** The system communicates generation progress and, on failure or timeout, offers retry without losing context.
- **FR31:** A user can compose a cover letter manually or from an inserted template, without using AI.
- **FR32:** A user can save (persist) a cover letter for a vacancy, limited to one cover letter per vacancy.
- **FR33:** A user can view a paginated list of their saved cover letters.
- **FR34:** A user can view a saved cover letter's detail with its associated vacancy context.
- **FR35:** A user can update a saved cover letter's content.
- **FR36:** A user can delete (soft-delete) a saved cover letter.

**Application Shell, Navigation & Trust**

- **FR37:** A user lands on an orientation view (dashboard) that summarizes their profile completeness and resources and links into key tasks.
- **FR38:** A user can navigate the entire shipped product surface, with every destination resolving to a working feature or a labeled placeholder.
- **FR39:** A user sees consistent, unmistakable "coming soon" placeholders for not-yet-available features (AI match score, job-source connection/aggregation, application tracking, multi-provider LLM configuration).
- **FR40:** A user sees appropriate loading, empty, and error states for every data-driven view, with validation errors surfaced inline and other failures surfaced clearly.
- **FR41:** A user can view and modify only their own resources; cross-user access is denied.

### NonFunctional Requirements

**Performance**

- **NFR1:** AI cover-letter generation returns a draft within **~15s** for a typical resume + vacancy; a hard timeout (≈30s) triggers a clear retry path rather than a hang.
- **NFR2:** Data-driven views render route/section skeletons immediately and avoid visible layout shift once data arrives.
- **NFR3:** The initial application bundle loads acceptably on broadband (no formal Lighthouse gate for the demo).

**Security & Privacy**

- **NFR4:** Sessions are carried by HTTP-only cookies; all capabilities except registration and sign-in require a valid session.
- **NFR5:** Users can access and modify only their own resources; ownership is enforced server-side and respected by the client (no cross-user data exposure).
- **NFR6:** No secrets or AI-provider keys appear in client code or the repository; the provider key is held server-side via configuration.
- **NFR7:** Only the data necessary for generation (resume + vacancy content) is sent to the AI provider; prompt and response payloads containing PII are not written to application logs.
- **NFR8:** Any deployed environment serves the application exclusively over HTTPS.

**Accessibility**

- **NFR9:** Target conformance is **WCAG 2.2 AA**.
- **NFR10:** Non-negotiable floor (even if full AA slips): one `h1` per page with logical heading order, full keyboard navigation with visible focus, inputs tied to labels, form errors announced via `aria-live`, body-text contrast ≥ 4.5:1, and `prefers-reduced-motion` honored.

**Integration (AI Provider)**

- **NFR11:** The generation request enforces a timeout and surfaces provider failures as retryable, never as a silent or fatal error.
- **NFR12:** Basic per-user rate limiting is applied to generation to cap cost and abuse.
- **NFR13:** The AI provider/model is swappable via server-side configuration without changes to client code.

### Additional Requirements

_Implementation requirements derived from the ratified Architecture Decisions, the Authorization Contract Matrix, and the FE Implementation Guide. These constrain how the FRs/NFRs are built and become acceptance criteria on the relevant stories._

**New backend capabilities (Backend-LLM track — contract-first, pinned)**

- **AR1 (Sign-in endpoint, Decision 2):** `POST /api/v1/users/sessions` — `[AllowAnonymous]`. Single `identifier` field (email **or** login, resolved server-side via `GetByEmailAsync` → fallback `GetByLoginAsync`; soft-deleted users do not authenticate) + `password`. `200 OK` sets the HTTP-only auth cookie (same `CookieAuthService` path as registration) and returns the user projection (`id, loginName, email, phone, location, about, avatar`); `accessToken` in body only for bearer transport. MediatR `SignInCommand` + `SignInCommandValidator` + handler (resolve → `IPasswordHasher.VerifyHashedPassword` → user projection); controller issues cookie. **No domain change, no migration, no new auth scheme.**
- **AR2 (Sign-in rules & rate limit, Decision 2.2–2.3):** `401` generic "Invalid credentials" for both unknown-identifier and wrong-password (anti-enumeration; reasonably constant timing); `400` for missing/empty identifier or password (RFC 7807); `429` after **5 failed attempts / 15 min / identifier+IP** with `Retry-After` (count failures only; success resets the window). Config: `RateLimit:SignIn:MaxAttempts = 5`, `RateLimit:SignIn:WindowMinutes = 15`.
- **AR3 (Generation endpoint, Decision 3.1–3.3):** `POST /api/v1/cover-letters/generate` — `[Authorize]` (HTTP-only cookie). Request `{ vacancyId, resumeId }` — **both required GUIDs**; `resumeId` is explicit (not server-picked). `200 OK` (not 201 — nothing created) returns `{ content (50–10000 chars), resumeId, resumeTitle, vacancyId, vacancyTitle, generatedAt (UTC) }`. **Returns a draft; does NOT persist.** No prompt/parameters from client — server assembles the prompt.
- **AR4 (Generation status codes, Decision 3.4):** RFC 7807 on every error. `400` missing/empty ids (no retry); `401` no/expired session (re-auth, preserve destination); `404` vacancy **or** résumé not-found / soft-deleted / **not owned** (body-FK rule — never 403, never leak); `429` per-user generation rate limit (retryable, honor `Retry-After`); `502` LLM provider error (**retryable**); `504` hard-timeout breach (**retryable**). **No `409` on `/generate`** (it does not persist; regeneration permitted). Every error carries a machine-readable `code` ∈ `{ validation, not_found, rate_limited, provider_error, generation_timeout }` + `retryable` boolean.
- **AR5 (LLM seam, Decision 3.6):** Application-layer `ICoverLetterGenerator.GenerateAsync(GenerationContext, CancellationToken)` + `GenerateCoverLetterCommand { UserId, VacancyId, ResumeId }` + handler (loads résumé + vacancy **ownership-checked** → builds context → calls generator). Concrete provider in `Infrastructure.LLM` is **deferred** (HTTP contract is provider-agnostic — Anthropic vs OpenAI choice does not change request/response/status). Server-side config: `Llm:Provider`, `Llm:ApiKey` (env/user-secrets only — NFR6), `Llm:Model` (cheap/fast tier), `Llm:TimeoutSeconds` (~30). The résumé-grounded, fabrication-resistant **prompt template** is a Backend-LLM quality deliverable, not a contract lever.
- **AR6 (Generation rate limit, Decision 3.6):** **10 generations / user / rolling hour** (ASP.NET Core rate limiting, single fixed window) → `429` + `Retry-After`. Config: `RateLimit:Generation:PermitsPerHour = 10`. Same limiter family as sign-in.
- **AR7 (Generation timeout/sync, Decision 3.6):** handler enforces `Llm:TimeoutSeconds` via `CancellationToken`; breach → `504`, provider exception → `502` (both retryable). **Synchronous** request/response with a loading state — no streaming/websockets this round.
- **AR8 (Persistence unchanged, Decision 3.7):** Save uses the existing `POST /api/v1/cover-letters` (`CreateCoverLetterCommand { VacancyId, Content }`, `201/400/401/404/409`). The FE Save posts `{ vacancyId, content }`. No change to this endpoint.
- **AR10 (OpenAPI conformance, Decision 3 open items / FE §12):** both new endpoints carry full `[ProducesResponseType]` attributes (don't reintroduce the R.2 gap). FE enum/DTO generation reads `/openapi/v1.json`.

**Data/seed track**

- **AR9 (JobSource seam, Decision 3.7 + PRD domain reqs):** mocked vacancies seeded behind a swappable `JobSource` adapter; `/generate` and vacancy reads go through the same domain path, so a later mock→real ingestion swap touches neither the contract nor the FE. Mock vacancy data is **synthetic only** (no real PII). Vacancy filtering is **POST-based** (`POST /api/v1/vacancies/filter`, body criteria); detail is `GET /api/v1/vacancies/{id}`.

**Frontend platform (Frontend track — Angular, ratified Decision 1)**

- **AR11 (Stack, Decision 1.1–1.4):** Angular standalone components (no NgModules); **Angular Signals + injectable services** for client/UI state (**no NgRx**); `HttpClient` + signals + a **thin per-entity service cache** with explicit `refetch()`/`invalidate()` (**no TanStack Query**); **typed Reactive Forms** (`FormGroup`/`FormControl<T>`) with validators mirroring backend rules (validate on blur + submit; submit only changed fields/PATCH; disable submit until dirty + valid); **spartan-ng on Angular CDK + helm, themed by tokens** (PrimeNG/Taiga UI approved fallbacks — swapping the kit must not change the token layer or branded component contracts). Feature-sliced layout per FE Guide (`app/processes/pages/widgets/features/entities/shared`).
- **AR12 (HTTP & errors, Decision 1.1):** `withCredentials: true` on every request via an HTTP interceptor (HTTP-only cookie canonical); one interceptor normalizes RFC 7807 → a typed `ProblemDetails` model; a global handler maps `401`→re-auth, `403/404/409/400/5xx`→UX states (Journey 4 recovery model). Surface `errors[field]` inline for forms; generic banner for non-validation failures.
- **AR13 (Routing, Decision 1.6):** FE-Guide routes (`/sign-up, /dashboard, /profile, /resumes, /resumes/:id, /educations, /educations/:id, /settings`) **plus** `/sign-in, /vacancies, /vacancies/:id, /cover-letters, /cover-letters/:id`. **All routes guarded except `/sign-up` and `/sign-in`** (NFR4, FR6). `401` → re-auth with intended-destination return (FR5).
- **AR14 (DraftStore, Decision 1.5):** active draft held in a signal-backed `DraftStore` service, **debounced autosave to `localStorage`** keyed by `userId + vacancyId`. **Local only** — never sent to logs (NFR7); `/generate` still returns a non-persisted draft. Lifecycle: restore-prompt on reopen → cleared on **Save** (server becomes source of truth) or explicit **Discard**; failed Save preserves local text for retry.
- **AR15 (Error contract, Authorization Contract Matrix):** the client honors the canonical wire contract — detail GET-by-id cross-user → `404`; PATCH/DELETE cross-user → `403`; body-supplied FK (cover-letter create, generate) → `404`; caller-owned soft-deleted → `404`; `409` = uniqueness contract (duplicate letter per vacancy; template name collision), carrying an entity-specific message. Exception → status mapping is fixed in `GlobalExceptionHandler` (`NotFoundException`→404, `ForbiddenException`→403, `ConflictException`/unique-violation→409, `UnauthorizedException`→401, `ValidationException`→400 + `errors`).
- **AR16 (Cursor pagination, FE §6.3):** list responses carry `{ items, totalCount, lastSeenId, lastSeenUpdatedAt, pageSize, hasNext, totalPages }`; next page sends `lastSeenId` + `lastSeenUpdatedAt`; `pageSize` defaults to 20, capped at 100 server-side. **Load-more pattern, never page numbers.**
- **AR17 (Enum generation, FE §12):** `WorkLocationType`, `Experience`, `Degree`, `Currency`, `Location`, `Language`, `LanguageLevel` generated from backend OpenAPI/shared schema — never hardcoded partial lists.

### UX Design Requirements

_Extracted from the UX Design Specification. The CRUD runway reuses standardized patterns (UX-DR12–DR18); the net-new generation surface (UX-DR2–DR8) is the deep work and carries the product's signature moments._

- **UX-DR1 (Token layer / hybrid identity):** Author the FE-Guide token scale reconciled to the Career OS palette/type as TS/JSON → CSS custom properties → Tailwind theme config; components reference tokens, never hardcoded values. Color roles: **near-black = action**, **royal blue (`#2348E0`) = brand accent/links/focus**, **lime (`#8FE34A`) = spark (decorative / on-dark only, never essential signal)**, warm cream canvas (`#F6F4EE`). Three type families with strict jobs: **Manrope** (sans UI workhorse, ~95% of text), a **serif-italic display face** (personality accents only — wordmark, accented title word, greetings), **IBM Plex Mono** (uppercase eyebrow labels, entity IDs). Base-4 spacing scale; radius sm6/md10/lg14/pill; hairline borders over heavy shadows.
- **UX-DR2 (GenerationSheet — net-new):** CDK overlay side-sheet (Direction B) hosting the entire generate → draft → edit → save flow over the vacancy detail (role stays visible as live context). Anatomy: header (eyebrow + close + expand/maximize), body (NarratedWait ⇄ CoverLetterEditor), footer (Save / Insert template / Regenerate / Start blank). States: `loading(narrated) · draft-ready · editing · saving · timeout/error(retry) · rate-limited · 409-conflict · restore-prompt`. Variants: right side-sheet (`lg/xl`, ~480–560px, expand-to-wide), wider on `md`, **full-screen on `xs/sm`**. A11y: CDK focus-trap, labelled `role=dialog`, `Esc` to close, focus returns to trigger, `aria-live` for state changes; closing **never discards** the local draft.
- **UX-DR3 (NarratedWait — net-new):** turn the ≤15s wait into anticipation — ordered phase list ("Reading your résumé → Tailoring to *<role>* → Drafting"), animated active dot, subtle "this can take a few seconds" note. States: `phase-active(pulsing) · phase-done(check) · slow(>~15s soft note) · timeout(~30s → error)`. Phases announced via `aria-live=polite`; pulse disabled under `prefers-reduced-motion`. Never a bare spinner or frozen screen.
- **UX-DR4 (GroundingChip — net-new):** make the résumé×role grounding **visible** — "Drafted from *<résumé title>* × *<vacancy title>*" (fed by `/generate`'s `resumeTitle` + `vacancyTitle`). Informational, not interactive; SR-readable. The core trust device.
- **UX-DR5 (DraftBadge + AutosaveIndicator — net-new):** "Draft — review before saving" framing + "Draft saved locally" reassurance. Autosave states: `idle · saving · saved-local · save-failed(kept)`. Status via `aria-live=polite`.
- **UX-DR6 (CoverLetterEditor — net-new):** the editable draft surface (generated or manual). Anatomy: rich-ish textarea, insert-template control, char/empty handling, grounding chip + draft badge. States: `empty/blank · template-inserted · generated-draft · dirty(autosaving) · restored-from-local · read-only(saved view)`. Labelled textarea, keyboard-complete, no keyboard trap. Enforces the 50–10000 char bound before Save (AR3/AR8 compatibility).
- **UX-DR7 (ResumeSelector — net-new, conditional):** appears only when the user has **>1 résumé**; defaults to most recent; the user confirms which "version of themselves" feeds generation (sent as the required `resumeId`). Labelled select, keyboard-navigable. When exactly one résumé exists, generation is zero-input.
- **UX-DR8 (ConsentNotice — net-new):** one-time, **non-blocking** notice (FR29) that résumé + vacancy are sent to a third-party AI provider; acknowledged once then remembered, with a persistent quiet reminder near the Generate action thereafter. States: `first-run(acknowledge) · remembered(quiet inline)`. Not a hard modal trap.
- **UX-DR9 (ComingSoonPlaceholder — net-new):** the one canonical Tier-3 honest treatment everywhere (AI match score, job-source connect/aggregation, application tracking, multi-provider LLM config). Anatomy: dashed/hatched container + mono "Coming soon" pill + title + one-line "previewed now, real next." Variants: **card-stub** (on vacancy cards: "AI match · soon") + **full-panel** (dedicated routes). Clearly labelled non-interactive / "not yet available"; never error-styled, never half-built.
- **UX-DR10 (VacancyCard — net-new):** title, company, location, skill chips, work-type chip, MatchScoreStub (the Tier-3 card-stub), primary "Generate" + "View." States: `default · hover · has-letter(badge)`.
- **UX-DR11 (VacancyFilters — net-new):** faceted chips/controls (skills, location, salary range, work-type), debounced, visible active state. States: `empty · active · results/no-results` (distinct "no results" vs "empty"). **Maps to the POST `/api/v1/vacancies/filter` body contract, not query params.**
- **UX-DR12 (EmptyState / ErrorState / NotFoundState):** standardized "no dead ends" surfaces. Variants: `empty(+CTA)` (e.g. "No resumes yet → Create first resume") · `error(+retry)` · `404(+back-to-list)` · `403(+explain + recovery CTA)`.
- **UX-DR13 (Branded FE-Guide contract components):** thin branded wrappers over spartan-ng/CDK primitives — `TextField`, `SelectField<T>`, `TagInput` (skills/keywords, keyboard add/remove), `AvatarUploader` (≤5 MB; jpeg/jpg/png/webp/gif), `ProfileForm`, `ResumeForm`, `EducationForm`, `TemplateForm`, `CursorPagination` (load-more). A11y baked in at the component (focus, labels, `aria-live`, reduced-motion).
- **UX-DR14 (App shell & page structure):** flat **left sidebar nav** (Dashboard, Profile, Resumes, Education, Vacancies, Cover Letters, Settings); active item near-black; collapses to top-bar + off-canvas drawer on `xs/sm`. Page structure: mono eyebrow label → `h1` (one per page, optional serif-italic accent word) → optional subtitle → one primary action top-right.
- **UX-DR15 (Button hierarchy & feedback patterns):** exactly **one near-black primary action per view** (verb-labelled); secondary (surface + border), ghost/tertiary, destructive (`status.danger`, confirmed via dialog — never one-click irreversible). Save disabled until dirty + valid; inline button spinner on async submit. Success = `aria-live` toast (auto-dismiss, never the only signal); recoverable error = banner with plain-language cause + Retry; conflict = amber (`status.warning`) banner with icon. Status color **always paired with icon/text**.
- **UX-DR16 (Responsive — desktop-primary, mobile-usable):** five FE-Guide breakpoints (`xs 0–479 · sm 480–767 · md 768–1023 · lg 1024–1439 · xl 1440+`), mobile-first, relative units, preserve layout dims (NFR2). **Lists become cards on `xs/sm`, never dense tables**; create/edit + GenerationSheet go **full-screen on `xs/sm`** with a sticky bottom action bar; two-column form+context on `md+`. Touch targets tappable; hover affordances have tap/focus equivalents.
- **UX-DR17 (A11y floor — realizes NFR10, gate on the spine):** one `h1`/page + logical heading order + landmark regions; full keyboard operability with visible focus + **skip-to-content**, no traps; inputs tied to labels (`for`/`id`), errors via `aria-describedby` + `aria-live`, async/form status announced; body contrast ≥ 4.5:1, large ≥ 3:1, **color never the sole signal**; `prefers-reduced-motion` honored globally; hit targets ≥ 24×24px. The **keyboard floor on the spine (sign-up → résumé → vacancy → generate → save) must pass before ship**; full-AA gold-plating is not a ship blocker.
- **UX-DR18 (Dashboard orientation):** first-run orientation that carries the "no instructions" goal — onboarding/profile-completeness checklist (LinkedIn strength-meter lineage, helpful not guilt-tripping), recent résumés, resource counts, quick links into key tasks. Parallel fetch with **partial-failure tolerance** (one widget failing must not break others).

### FR Coverage Map

- **FR1:** Epic 1 — register a new account (existing `POST /users`, FE form).
- **FR2:** Epic 1 — session established on registration (cookie set by `POST /users`).
- **FR3:** Epic 1 — session renewal without re-entering credentials (existing `POST /users/token/refresh`).
- **FR4:** Epic 1 — returning-user sign-in (**NEW** `POST /users/sessions`, contract-first).
- **FR5:** Epic 1 — expired-session routing + return-to-destination (401 guard/interceptor).
- **FR6:** Epic 1 — auth-gate everything except register/sign-in (route guards).
- **FR7:** Epic 6 — view own profile.
- **FR8:** Epic 6 — partial profile update.
- **FR9:** Epic 6 — change login name (uniqueness).
- **FR10:** Epic 6 — avatar upload/replace/remove.
- **FR11:** Epic 2 — create résumé.
- **FR12:** Epic 2 — paginated résumé list.
- **FR13:** Epic 2 — résumé detail.
- **FR14:** Epic 2 — update résumé.
- **FR15:** Epic 2 — soft-delete résumé.
- **FR16:** Epic 6 — create education record.
- **FR17:** Epic 6 — paginated education list.
- **FR18:** Epic 6 — education view/update/soft-delete.
- **FR19:** Epic 5 — create template (unique name per user).
- **FR20:** Epic 5 — paginated, name-searchable template list.
- **FR21:** Epic 5 — template view/update/soft-delete.
- **FR22:** Epic 5 — insert template content into the editor.
- **FR23:** Epic 3 — browse paginated vacancy board.
- **FR24:** Epic 3 — filter vacancies (POST-based criteria).
- **FR25:** Epic 3 — view vacancy detail.
- **FR26:** Epic 3 — seeded mock vacancies behind the swappable `JobSource` seam (**own slice**).
- **FR27:** Epic 4 — AI-generate a résumé-grounded draft (**NEW** `POST /cover-letters/generate`, contract-first).
- **FR28:** Epic 4 — output is an editable draft, never auto-persisted.
- **FR29:** Epic 4 — third-party AI consent notice.
- **FR30:** Epic 4 — generation progress + retry-without-losing-context (incl. draft resilience).
- **FR31:** Epic 5 — manual / template compose without AI.
- **FR32:** Epic 4 — save a letter (one per vacancy) — completes the spine (existing `POST /cover-letters`).
- **FR33:** Epic 5 — paginated saved cover-letter list.
- **FR34:** Epic 5 — saved cover-letter detail with vacancy context.
- **FR35:** Epic 5 — update a saved cover letter.
- **FR36:** Epic 5 — soft-delete a saved cover letter.
- **FR37:** Epic 1 — orientation dashboard.
- **FR38:** Epic 7 — whole-surface navigation, every destination resolves.
- **FR39:** Epic 7 — consistent "coming soon" Tier-3 placeholders.
- **FR40:** Cross-cutting (primary home Epic 1 — state-component library + Problem-Details interceptor; realized as ACs in every data-view story).
- **FR41:** Cross-cutting (primary home Epic 1 — guards + 401/403/404 handling; ownership respected as ACs in every resource story).

## Epic List

### Epic 1: Foundation, Access & Orientation
A visitor can register or sign back in, is kept signed in across refresh, is bounced to sign-in (and returned to where they were) when their session lapses, and lands on an orienting dashboard inside a coherent app shell where the navigation is real. This epic also lays the brownfield Angular foundation — project scaffold, token layer, `withCredentials` HTTP + Problem-Details interceptor, route guards, the standardized loading/empty/error state components, and OpenAPI type generation — folded into its first story as the enabling slice of a user-value outcome (no standalone "infrastructure" epic). **Contract-first:** the NEW `POST /api/v1/users/sessions` sign-in endpoint (Backend-LLM track, Decision 2) and the FE sign-in screen are built in parallel against the pinned contract.
**FRs covered:** FR1, FR2, FR3, FR4, FR5, FR6, FR37 *(+ cross-cutting FR40, FR41 foundation)*

### Epic 2: Résumé Management
A user can create, browse (paginated), view, edit, and soft-delete their résumés — the fuel for generation and the first step of the magic spine. Reuses the branded form components (`ResumeForm`, `TagInput`, typed selects from generated enums) and cursor pagination over the shipped `/resumes` endpoints.
**FRs covered:** FR11, FR12, FR13, FR14, FR15

### Epic 3: Vacancy Discovery
A user can browse a paginated vacancy board, filter it by multiple criteria, and open a vacancy's full detail — running on realistic seeded data. The magic-spine on-ramp. **Own slice:** FR26 seeds mock vacancies behind the swappable `JobSource` seam (Data/seed track) as its own story so the board FE never blocks on absent data; filtering is POST-based (`POST /api/v1/vacancies/filter`).
**FRs covered:** FR23, FR24, FR25, FR26

### Epic 4: AI Cover-Letter Generation & Save *(the magic — risk boundary)*
From a vacancy, a user clicks Generate, sees a narrated grounded wait, receives a résumé-and-role-grounded editable draft, and saves their edited version as the one letter for that vacancy — recovering gracefully from slow/failed/rate-limited generation and never losing in-progress work. This is the product's signature moment and completes the spine end-to-end (generate → edit → save). **Contract-first:** the NEW `POST /api/v1/cover-letters/generate` endpoint + LLM service (Backend-LLM track, Decision 3) and the FE GenerationSheet are built in parallel against the pinned contract; persistence reuses the existing `POST /api/v1/cover-letters`.
**FRs covered:** FR27, FR28, FR29, FR30, FR32

### Epic 5: Compose, Templates & Cover-Letter Library
A user can manage reusable templates, start a letter manually or by inserting a template (no AI), and manage their saved letters (list, detail with vacancy context, edit, soft-delete). The AI-skeptic's first-class path plus the cover-letter management runway — building on the editor and save behavior delivered in Epic 4. Template stories are sequenced before the insert-template capability so there is no forward dependency.
**FRs covered:** FR19, FR20, FR21, FR22, FR31, FR33, FR34, FR35, FR36

### Epic 6: Profile, Avatar & Education *(completeness)*
A user can view and partially update their profile, change their login name, manage their avatar, and keep education records (CRUD) — the account-completeness runway. Sequenced after the spine per the PRD trim order (avatar is the most-protected completeness item; education UI is trimmed before avatar).
**FRs covered:** FR7, FR8, FR9, FR10, FR16, FR17, FR18

### Epic 7: Whole-Surface Shell & Honest Placeholders *(completeness)*
Every destination across the shipped surface resolves to a working feature, a clear empty/error state, or an unmistakable, consistent "coming soon" placeholder — so testers never mistake an unbuilt Tier-3 feature (AI match score, job-source connect/aggregation, application tracking, multi-provider LLM config) for a bug. Naturally last: the no-dead-ends sweep requires the real pages from Epics 1–6 to exist.
**FRs covered:** FR38, FR39

---

## Epic 1: Foundation, Access & Orientation

A visitor can register or sign back in, stays signed in across refresh, is routed to sign-in (and returned to their intended destination) when their session lapses, and lands on an orienting dashboard inside a coherent app shell. The brownfield Angular foundation — scaffold, token layer, `withCredentials` HTTP + Problem-Details interceptor, route guards, the standardized loading/empty/error state components, and OpenAPI type generation — is the enabling slice of this epic's first story (no standalone infrastructure epic). The NEW `POST /api/v1/users/sessions` sign-in endpoint and its FE screen are built in parallel against the pinned contract.

### Story 1.1: Project foundation & user registration

As a **visitor**,
I want **to register an account and immediately land inside the app**,
So that **I can start using Jobnecto without a separate sign-in step**.

**Acceptance Criteria:**

**Given** no Angular client exists yet
**When** the foundation slice for this story is built
**Then** an Angular standalone-components SPA is scaffolded feature-sliced per the FE Guide (`app/processes/pages/widgets/features/entities/shared`) (AR11)
**And** the token layer (Career-OS-reconciled colors, type, spacing, radius, motion) is authored as TS/JSON → CSS custom properties → Tailwind theme, with components referencing tokens not hardcoded values (UX-DR1)
**And** an HTTP interceptor sets `withCredentials: true` on every request and normalizes RFC 7807 responses into a typed `ProblemDetails` model (AR12)
**And** TS enums/DTOs are generated from `/openapi/v1.json` (AR17)
**And** the standardized `EmptyState` / `ErrorState` / `NotFoundState` and skeleton components exist for reuse (UX-DR12, FR40 foundation)

**Given** I am an unauthenticated visitor on `/sign-up`
**When** I submit a valid login name, email, and password
**Then** the client mirrors the backend validation rules (login 3–50 `^[A-Za-z0-9_]+$`, email valid ≤50, password 8–50) before calling `POST /api/v1/users` (FE §7.1)
**And** on `201` the server sets the HTTP-only auth cookie and I am routed to an authenticated landing route and my profile is hydrated via `GET /api/v1/users/me` (FR1, FR2)

**Given** I submit the sign-up form with invalid or conflicting data
**When** the server returns `400` (validation) or `409` (email/login already used)
**Then** field-level errors render inline (tied via `aria-describedby`, announced via `aria-live`) and the conflict is shown as explicit, plain-language guidance — never a raw code (UX-DR15, NFR10)

### Story 1.2: Returning-user sign-in endpoint *(Backend-LLM track)*

As a **returning user whose session has expired**,
I want **a credential sign-in endpoint**,
So that **I can establish a new session without re-registering**.

**Acceptance Criteria:**

**Given** the shipped backend exposes registration and authenticated token-refresh but no cold-start credential entry (Decision 2.1)
**When** the NEW endpoint is implemented
**Then** `POST /api/v1/users/sessions` is `[AllowAnonymous]`, accepts `{ identifier, password }`, and is wired as `SignInCommand` + `SignInCommandValidator` + handler with no domain change and no migration (AR1)

**Given** a valid `identifier` (email **or** login) and correct password for a non-soft-deleted user
**When** the endpoint resolves the user (`GetByEmailAsync` → fallback `GetByLoginAsync`) and verifies via `IPasswordHasher.VerifyHashedPassword`
**Then** it returns `200` with the HTTP-only auth cookie set (same `CookieAuthService` path as registration) and the user projection `{ id, loginName, email, phone, location, about, avatar }`; `accessToken` is in the body only for bearer transport (AR1)

**Given** an unknown identifier **or** a wrong password
**When** the endpoint responds
**Then** it returns a single generic `401` "Invalid credentials" — indistinguishable between the two cases, with reasonably constant timing — never revealing which field failed (AR2, anti-enumeration)

**Given** missing/empty identifier or password
**When** the endpoint validates the request
**Then** it returns `400` (RFC 7807) (AR2)

**Given** 5 failed attempts within 15 minutes for the same identifier+IP
**When** a 6th attempt arrives in the window
**Then** it returns `429` with `Retry-After`; only failures count and a success resets the window; thresholds are config-driven (`RateLimit:SignIn:*`) (AR2)

**Given** the OpenAPI surface
**When** the endpoint ships
**Then** it carries full `[ProducesResponseType]` attributes for 200/400/401/429 (AR10)

### Story 1.3: Sign-in screen *(Frontend track)*

As a **returning user**,
I want **a sign-in page**,
So that **I can get back into my account with my credentials**.

**Acceptance Criteria:**

**Given** the `POST /api/v1/users/sessions` contract is pinned (built in parallel with Story 1.2)
**When** I visit the unguarded `/sign-in` route and submit my identifier + password
**Then** the client POSTs to the sessions endpoint with credentials, and on `200` routes me to my intended destination (or the dashboard) with the cookie session active (FR4, AR13)

**Given** I enter wrong credentials
**When** the server returns `401`
**Then** I see a single generic "Invalid credentials" message that does not disclose whether the identifier or password was wrong (AR2)

**Given** I have been rate-limited
**When** the server returns `429`
**Then** I see a friendly "too many attempts, try again shortly" message honoring `Retry-After`, not a raw code (UX-DR15)

**Given** I am on sign-in or sign-up
**When** the page renders
**Then** it meets the a11y floor — one `h1`, labeled inputs, visible focus, keyboard-complete (NFR10, UX-DR17)

### Story 1.4: Session continuity, route guards & expiry recovery

As a **signed-in user**,
I want **my session to persist and to be gracefully recovered when it lapses**,
So that **I am never stranded or silently logged out mid-task**.

**Acceptance Criteria:**

**Given** I have an active session
**When** the client needs to renew it
**Then** it uses `POST /api/v1/users/token/refresh` without requiring me to re-enter credentials (FR3)

**Given** I navigate to any route except `/sign-up` and `/sign-in`
**When** I am unauthenticated
**Then** a route guard redirects me to sign-in (FR6, NFR4, AR13)

**Given** my cookie session expires while I am working
**When** any request returns `401`
**Then** the interceptor routes me to `/sign-in`, preserves my intended destination, and returns me there after I re-authenticate (FR5, AR12, AR15)

**Given** a request returns `403` / `404` (cross-user or not-found per the contract matrix)
**When** the global handler maps it
**Then** I see the appropriate UX state (403 explain+recovery CTA; 404 not-found+back) and no cross-user data is exposed (FR41, AR15)

### Story 1.5: Application shell & navigation

As a **signed-in user**,
I want **a consistent navigation shell**,
So that **I can move around the product and always know where I am**.

**Acceptance Criteria:**

**Given** I am authenticated
**When** any guarded page renders
**Then** a fixed left sidebar shows Dashboard, Profile, Resumes, Education, Vacancies, Cover Letters, Settings with the active item rendered near-black (UX-DR14)

**Given** each page
**When** it renders
**Then** it follows the structure: mono eyebrow label → one `h1` (optional serif-italic accent) → optional subtitle → at most one near-black primary action top-right (UX-DR14, UX-DR15)

**Given** a viewport at `xs/sm`
**When** the shell renders
**Then** the sidebar collapses to a top-bar + off-canvas drawer, keyboard-operable with a skip-to-content link (UX-DR16, UX-DR17)

### Story 1.6: Orientation dashboard

As a **first-time user**,
I want **a dashboard that orients me**,
So that **I know what to do next without instructions**.

**Acceptance Criteria:**

**Given** I land on `/dashboard` after auth
**When** the page loads
**Then** it fetches profile, first-page résumés, and first-page educations in parallel and shows an onboarding/profile-completeness checklist, recent résumés, resource counts, and quick links into key tasks (FR37, UX-DR18)

**Given** one of the parallel fetches fails
**When** the dashboard renders
**Then** the failing widget shows its own error state while the others still render — partial failure never breaks the whole page (UX-DR18, FR40)

**Given** I am brand new with no résumés
**When** the dashboard renders
**Then** the checklist nudges me toward "create a résumé" helpfully (never guilt-tripping) with a working link (UX-DR18)

---

## Epic 2: Résumé Management

A user can create, browse, view, edit, and soft-delete their résumés — the fuel for generation and the first step of the magic spine — over the shipped `/resumes` endpoints, reusing the branded form components and cursor pagination.

### Story 2.1: Create a résumé

As a **user**,
I want **to create a résumé**,
So that **I have material that generation can ground a cover letter in**.

**Acceptance Criteria:**

**Given** I am on the résumé create form
**When** I fill skills, experience, work-location preference, salary, currency, and related fields
**Then** the form uses `ResumeForm` / `TagInput` / typed selects whose options are generated from backend enums (`WorkLocationType`, `Experience`, `Currency`, `Language`, `LanguageLevel`, `Location`) — never hardcoded (UX-DR13, AR17)
**And** client validation mirrors the backend rules (title ≤500; each skill non-empty ≤30; salary ≥0; enums valid) before submit (FE §7.4)

**Given** a valid form
**When** I submit
**Then** the client calls `POST /api/v1/resumes` and on `201` shows success (`aria-live` toast) and the new résumé is available in my list (FR11)

**Given** validation fails server-side
**When** `400` returns
**Then** field errors render inline with an `aria-live` summary; no field-shake (UX-DR15, NFR10)

### Story 2.2: Browse my résumés

As a **user**,
I want **a paginated list of my résumés**,
So that **I can find and manage them**.

**Acceptance Criteria:**

**Given** I have résumés
**When** I open `/resumes`
**Then** I see a cursor-paginated list using `{ items, lastSeenId, lastSeenUpdatedAt, hasNext }` with a load-more pattern (never page numbers); next page sends both cursor fields (AR16, FR12)

**Given** I have no résumés
**When** the list loads
**Then** I see the "No resumes yet → Create first resume" empty state with a working CTA (UX-DR12)

**Given** the list is loading or at `xs/sm`
**When** it renders
**Then** a layout-preserving skeleton shows first (no layout shift) and rows render as cards, not a dense table (NFR2, UX-DR16)

**Given** another user's résumés exist
**When** I list
**Then** I only ever see my own (server-scoped; client respects ownership) (FR41, NFR5)

### Story 2.3: View & edit a résumé

As a **user**,
I want **to open a résumé and edit it**,
So that **I can keep it current**.

**Acceptance Criteria:**

**Given** I own a résumé
**When** I open `/resumes/:id`
**Then** its detail loads via `GET /api/v1/resumes/{id}` and is editable (FR13)

**Given** I change fields
**When** I save
**Then** only changed fields are PATCHed, Save is disabled until dirty + valid, and on `200` the cached résumé is invalidated/refetched (FR14, AR11)

**Given** the id does not exist, is soft-deleted, or belongs to another user
**When** I open it
**Then** I get `404` and a not-found state with a back-to-résumés CTA — cross-user existence is not disclosed (FR41, AR15)

### Story 2.4: Soft-delete a résumé

As a **user**,
I want **to delete a résumé**,
So that **I can remove outdated material**.

**Acceptance Criteria:**

**Given** I own a résumé
**When** I choose delete
**Then** a destructive confirmation dialog appears (never a one-click irreversible action) (UX-DR15)

**Given** I confirm
**When** the client calls `DELETE /api/v1/resumes/{id}`
**Then** on `204` the row is removed immediately and cursor metadata refreshes (FR15, AR16)

**Given** I attempt to delete a résumé I do not own
**When** the server responds
**Then** it returns `403` and I see an explain + safe-recovery state (FR41, AR15)

---

## Epic 3: Vacancy Discovery

A user can browse a paginated vacancy board, filter it by multiple criteria, and open a vacancy's full detail — running on realistic seeded data behind a swappable seam. The magic-spine on-ramp.

### Story 3.1: Seed mock vacancies behind the JobSource seam *(Data/seed track — own slice)*

As a **product team**,
I want **realistic seeded vacancies behind a swappable source seam**,
So that **the vacancy board has data now and can be swapped for real ingestion later without touching the client or the generation contract**.

**Acceptance Criteria:**

**Given** there is no real ingestion this round
**When** the seed track is built
**Then** a static `JobSource` adapter provides a realistic set of seeded vacancies through the same domain path the shipped `/vacancies` endpoints read (AR9)

**Given** the seeded data
**When** it is authored
**Then** it is entirely synthetic — no real PII (PRD domain requirements, NFR7)

**Given** a future real ingestion adapter
**When** it later replaces the mock
**Then** the swap touches neither the `/generate` contract nor the frontend, because both read vacancy content through the same seam (AR9, Decision 3.7)

### Story 3.2: Browse the vacancy board

As a **user**,
I want **to browse available vacancies**,
So that **I can find a role to apply to**.

**Acceptance Criteria:**

**Given** seeded vacancies exist
**When** I open `/vacancies`
**Then** I see a cursor-paginated board of `VacancyCard`s (title, company, location, skill chips, work-type chip, primary "Generate" + "View"), load-more, cards on `xs/sm` (FR23, UX-DR10, UX-DR16)

**Given** a vacancy card
**When** it renders
**Then** it shows the `MatchScoreStub` Tier-3 placeholder ("AI match · soon") as an honest, non-interactive stub (UX-DR9, UX-DR10)

**Given** the board is loading or empty
**When** it renders
**Then** a skeleton shows first, and an empty state is distinct from a no-results state (NFR2, UX-DR12)

### Story 3.3: Filter vacancies

As a **user**,
I want **to filter vacancies by multiple criteria**,
So that **I can narrow the board to relevant roles**.

**Acceptance Criteria:**

**Given** the board
**When** I apply faceted filters (skills, location, salary range, work-location type)
**Then** the client sends criteria in the **body** of `POST /api/v1/vacancies/filter` (not query params), debounced, with a visible active-filter state (FR24, AR9, UX-DR11)

**Given** filters that match nothing
**When** results return empty
**Then** I see a "no results" state distinct from the board's empty state, with a way to clear filters (UX-DR11, UX-DR12)

### Story 3.4: View vacancy detail

As a **user**,
I want **to open a vacancy's full detail**,
So that **I can decide whether to generate a letter for it**.

**Acceptance Criteria:**

**Given** a vacancy exists
**When** I open `/vacancies/:id`
**Then** its full detail loads via `GET /api/v1/vacancies/{id}` with a prominent primary "Generate cover letter" entry point (FR25)

**Given** the id does not exist, is soft-deleted, or is not mine
**When** I open it
**Then** I get `404` and a not-found state with a back-to-board CTA — existence is not disclosed (FR41, AR15)

**Given** the Generate action
**When** Epic 4 is not yet built
**Then** the entry point is present and wired to open the (initially stubbed) GenerationSheet, so this story stands alone and Epic 4 enhances it without a forward dependency

---

## Epic 4: AI Cover-Letter Generation & Save  *(the magic — risk boundary)*

From a vacancy, a user clicks Generate, sees a narrated grounded wait, receives a résumé-and-role-grounded editable draft, and saves their edited version as the one letter for that vacancy — recovering gracefully from slow/failed/rate-limited generation and never losing in-progress work. This epic completes the magic spine end-to-end. The NEW generation endpoint + LLM service and the FE GenerationSheet are built in parallel against the pinned contract; persistence reuses the existing `POST /api/v1/cover-letters`.

### Story 4.1: Generation endpoint & LLM seam *(Backend-LLM track)*

As a **user**,
I want **a backend that drafts a résumé-grounded cover letter**,
So that **the client can turn one click into a tailored draft**.

**Acceptance Criteria:**

**Given** the contract is pinned (Decision 3)
**When** the endpoint is implemented
**Then** `POST /api/v1/cover-letters/generate` is `[Authorize]`, accepts `{ vacancyId, resumeId }` (both required GUIDs), and returns `200` with `{ content (50–10000 chars), resumeId, resumeTitle, vacancyId, vacancyTitle, generatedAt(UTC) }` and **does not persist** (FR27, FR28, AR3)

**Given** the application layer
**When** generation runs
**Then** it flows through `ICoverLetterGenerator` + `GenerateCoverLetterCommand` + handler, which loads résumé and vacancy **ownership-checked**, builds context, and calls the generator; the concrete provider lives behind the seam in `Infrastructure.LLM` and is provider-agnostic (AR5)

**Given** a missing/empty id, no session, or a foreign/not-found/soft-deleted vacancy or résumé
**When** the endpoint responds
**Then** it returns `400`, `401`, or `404` respectively — body-supplied FKs yield `404` (never `403`, never a leak); there is **no `409`** on generate (AR4, AR15)

**Given** a provider error or a timeout breaching `Llm:TimeoutSeconds` (~30s)
**When** the handler responds
**Then** it returns `502` (provider) or `504` (timeout), both `retryable: true`, with the Problem-Details `code` discriminator set; per-user limit of 10/hour returns `429` + `Retry-After` (AR4, AR6, AR7, NFR1, NFR11, NFR12)

**Given** privacy and configuration constraints
**When** generation runs
**Then** the prompt is built strictly from the user's actual résumé + vacancy content, PII payloads are never written to logs, no provider key appears in client/repo (held server-side via `Llm:*` config), and the endpoint carries full `[ProducesResponseType]` attributes (NFR6, NFR7, NFR13, AR10)

### Story 4.2: GenerationSheet — narrated wait & grounded draft reveal *(Frontend track)*

As a **user**,
I want **to click Generate and watch a tailored draft appear grounded in my résumé and this role**,
So that **I trust the output and feel the product understood me**.

**Acceptance Criteria:**

**Given** a vacancy detail page and the pinned `/generate` contract (built in parallel with Story 4.1)
**When** I click Generate
**Then** a CDK overlay side-sheet opens over the vacancy (right side-sheet on `lg/xl`, expandable; full-screen on `xs/sm`), focus-trapped, `role=dialog`, `Esc`-dismissible, returning focus to the trigger on close (UX-DR2, UX-DR16)

**Given** generation is in flight (≤~15s typical)
**When** I wait
**Then** I see a `NarratedWait` with ordered phases ("Reading your résumé → Tailoring to *<role>* → Drafting"), calm motion, announced via `aria-live=polite`, with the pulse disabled under `prefers-reduced-motion` — never a bare spinner or frozen screen (UX-DR3, NFR1, NFR10)

**Given** the draft returns
**When** the editor populates
**Then** a `GroundingChip` shows "Drafted from *<resumeTitle>* × *<vacancyTitle>*" (from the response), a "Draft — review before saving" badge is shown, and the content lands in an editable `CoverLetterEditor` (FR27, FR28, UX-DR4, UX-DR5, UX-DR6)

### Story 4.3: Generation entry — consent notice & résumé selection

As a **user**,
I want **to be told my data goes to a third-party AI and to choose which résumé feeds generation**,
So that **I consent knowingly and ground the draft in the right version of myself**.

**Acceptance Criteria:**

**Given** my first-ever generation
**When** I click Generate
**Then** a concise, non-blocking `ConsentNotice` states that my résumé + vacancy are sent to a third-party AI provider; once acknowledged it is remembered, with a persistent quiet reminder near the action thereafter (FR29, UX-DR8)

**Given** I have more than one résumé
**When** I start generation
**Then** a `ResumeSelector` appears defaulting to my most recent, and my choice is sent as the required `resumeId`; with exactly one résumé, generation is zero-input (UX-DR7, AR3)

**Given** a saved letter already exists for this vacancy
**When** I trigger Generate
**Then** the entry pre-empts the collision client-side by routing me to edit the existing letter rather than generating a colliding second one (UX-DR2, Journey 1)

### Story 4.4: Draft resilience — local autosave & restore

As a **user**,
I want **my in-progress draft to survive a refresh, navigation, or dropped connection**,
So that **I never lose work**.

**Acceptance Criteria:**

**Given** I am editing a draft (generated or manual)
**When** I make edits
**Then** a signal-backed `DraftStore` debounce-autosaves the text to `localStorage` keyed by `userId + vacancyId`, with a quiet "Draft saved locally" cue; the text is local-only and never sent to logs (FR30, AR14, UX-DR5, NFR7)

**Given** I reopen the editor for a vacancy with an unsaved local draft
**When** the sheet opens
**Then** I am prompted "You have an unsaved draft for this role — restore or discard?" before anything is overwritten (UX-DR6, AR14)

**Given** I successfully Save or explicitly Discard
**When** the action completes
**Then** the local draft is cleared (server becomes the source of truth on Save) (AR14)

### Story 4.5: Generation recovery — timeout, provider error & rate-limit

As a **user**,
I want **slow, failed, or rate-limited generation to recover gracefully**,
So that **I am reassured rather than stranded**.

**Acceptance Criteria:**

**Given** generation returns `502` or `504`
**When** the error surfaces
**Then** I see a friendly, specific message with a **Retry** that preserves all context, and any text I already typed is never lost (FR30, NFR11, AR4)

**Given** generation returns `429` (10/hour limit)
**When** the error surfaces
**Then** I see an explanatory "you've generated several quickly — try again shortly" message honoring `Retry-After`, not a cryptic code (FR30, NFR12, UX-DR15)

**Given** any async generation outcome
**When** it occurs
**Then** it is announced via `aria-live` (NFR10, UX-DR3)

### Story 4.6: Save the edited draft, one per vacancy

As a **user**,
I want **to save my edited letter for a vacancy**,
So that **I keep the tailored result and complete the flow**.

**Acceptance Criteria:**

**Given** an edited draft in the editor
**When** I click Save
**Then** the client calls the existing `POST /api/v1/cover-letters` with `{ vacancyId, content }`, the editor enforces the 50–10000 char bound, and on success the saved letter is scoped to me and tied to that vacancy and the local draft is cleared (FR32, AR8, AR14)

**Given** a letter already exists for this vacancy at save time
**When** the server returns `409`
**Then** I see a plain-language "you already have a letter for this role" and am offered to open/overwrite the existing one — never a dead-end error (FR32, AR15, UX-DR15)

**Given** the save fails on a dropped connection or `500`
**When** the error surfaces
**Then** my edited text remains safe in the local store and I can retry — work is never lost (FR30, AR14)

---

## Epic 5: Compose, Templates & Cover-Letter Library

A user can manage reusable templates, start a letter manually or by inserting a template (no AI), and manage their saved letters. The AI-skeptic's first-class path plus the cover-letter management runway — building on the editor and save behavior from Epic 4. Template stories are sequenced before the insert-template capability so there is no forward dependency.

### Story 5.1: Create a cover-letter template

As a **user**,
I want **to save a reusable template**,
So that **I can start letters from text I already trust**.

**Acceptance Criteria:**

**Given** I am on the template create form
**When** I enter a name and content and submit
**Then** the client calls `POST /api/v1/cover-letter-templates` and on `201` the template is available in my list (FR19)

**Given** I reuse a name I already have
**When** the server returns `409` (unique-per-user name)
**Then** I see plain-language guidance to choose a different name (FR19, AR15, UX-DR15)

### Story 5.2: Browse, search & manage templates

As a **user**,
I want **to find, view, edit, and remove my templates**,
So that **I can keep my reusable text current**.

**Acceptance Criteria:**

**Given** I have templates
**When** I open `/cover-letter-templates`
**Then** I see a cursor-paginated, name-searchable list with load-more (FR20, AR16)

**Given** I open a template's detail and edit it
**When** I save changes (only changed fields, disabled until dirty+valid)
**Then** `PATCH` succeeds; a rename collision returns `409` with guidance (FR21, AR15)

**Given** I delete a template
**When** I confirm the destructive dialog
**Then** `DELETE` soft-deletes it and the row is removed; a cross-user attempt returns `403` with a safe-recovery state (FR21, FR41, AR15)

### Story 5.3: Insert a template & compose without AI

As a **user skeptical of AI (or who simply wants control)**,
I want **to compose a letter manually or from a template, with no LLM call**,
So that **the product offers intelligence without imposing it**.

**Acceptance Criteria:**

**Given** the cover-letter editor for a chosen vacancy (the same `CoverLetterEditor` and Save from Epic 4)
**When** I choose "Insert a template"
**Then** the selected template's content is dropped into the editor in one action as a starting point — no copy-paste, no modal gymnastics (FR22, UX-DR6)

**Given** I start blank or from an inserted template
**When** I write and Save
**Then** no `/generate` call is made; Save persists via `POST /api/v1/cover-letters` with the same one-per-vacancy `409` handling and local draft resilience as the AI path (FR31, FR32 reuse, AR8, AR14)

**Given** the manual/template entry point
**When** the compose surface renders
**Then** the manual path has equal visual weight to Generate — never buried beneath it (UX-DR2, experience principle "offer intelligence, don't impose it")

### Story 5.4: Browse & view saved cover letters

As a **user**,
I want **to see and open my saved cover letters with their vacancy context**,
So that **I can review what I've written for each role**.

**Acceptance Criteria:**

**Given** I have saved letters
**When** I open `/cover-letters`
**Then** I see a cursor-paginated list with load-more; an empty state when I have none (FR33, AR16, UX-DR12)

**Given** I open a saved letter at `/cover-letters/:id`
**When** the detail loads via `GET /api/v1/cover-letters/{id}`
**Then** it shows the content with its associated vacancy context (FR34)

**Given** the id is not mine, not found, or soft-deleted
**When** I open it
**Then** I get `404` and a not-found state with a back-to-list CTA — existence not disclosed (FR41, AR15)

### Story 5.5: Edit & soft-delete a saved cover letter

As a **user**,
I want **to update or remove a saved letter**,
So that **I can keep my applications accurate**.

**Acceptance Criteria:**

**Given** I own a saved letter
**When** I edit its content and save
**Then** `PATCH /api/v1/cover-letters/{id}` updates it (changed content only, disabled until dirty+valid), enforcing the 50–10000 char bound (FR35, AR8)

**Given** I delete a saved letter
**When** I confirm the destructive dialog
**Then** `DELETE` soft-deletes it; a cross-user edit/delete returns `403`, a not-found/soft-deleted target returns `404` (FR36, FR41, AR15)

---

## Epic 6: Profile, Avatar & Education  *(completeness)*

A user can view and partially update their profile, change their login name, manage their avatar, and keep education records — the account-completeness runway. Sequenced after the spine per the PRD trim order; within the epic, profile/avatar (more protected) precede education (trimmed first under resource risk).

### Story 6.1: View & update profile

As a **user**,
I want **to view and edit my profile**,
So that **my account reflects who I am**.

**Acceptance Criteria:**

**Given** I open `/profile`
**When** the page loads
**Then** it shows my login name, email, phone, location, about, and avatar via `GET /api/v1/users/me` (FR7)

**Given** I change one or more fields
**When** I save
**Then** only changed fields are PATCHed to `/api/v1/users/me` (Save disabled until dirty+valid), with client validation mirroring backend rules (FE §7.2) and revalidation on `200` (FR8, AR11)

**Given** I change my login name to one already taken
**When** the server returns `409`
**Then** I see explicit guidance to choose another, surfaced inline — never a raw code (FR9, UX-DR15)

### Story 6.2: Manage avatar

As a **user**,
I want **to upload, replace, and remove my avatar**,
So that **my profile feels personal**.

**Acceptance Criteria:**

**Given** I choose an image
**When** I upload it
**Then** the client pre-checks MIME (`jpeg/jpg/png/webp/gif`) and size (≤5 MB) before `POST`/`PUT /api/v1/users/me/avatar` (multipart), shows an optimistic preview, and commits only on server `200` (FR10, FE §7.3, UX-DR13)

**Given** I remove my avatar
**When** I confirm
**Then** `DELETE /api/v1/users/me/avatar` returns the updated profile and the UI reflects removal (FR10)

**Given** the upload flow
**When** I operate it
**Then** both the dropzone and the button path are fully keyboard accessible (NFR10, UX-DR17)

### Story 6.3: Create & browse education records

As a **user**,
I want **to add and list my education records**,
So that **my background is captured**.

**Acceptance Criteria:**

**Given** I am on the education create form
**When** I enter title, specialization, and degree and submit
**Then** the `degree` select is sourced from the backend enum (`Bachelor, Master, PhD, PostDoc, Other`), client validation mirrors backend rules (title/specialization required, non-whitespace, ≤100), and `POST /api/v1/educations` creates it (FR16, AR17, FE §7.5)

**Given** I open `/educations`
**When** the list loads
**Then** it is cursor-paginated with load-more and shows the "No education records yet → Add education" empty state when empty (FR17, AR16, UX-DR12)

### Story 6.4: View, edit & delete an education record

As a **user**,
I want **to open, update, or remove an education record**,
So that **I can keep my background current**.

**Acceptance Criteria:**

**Given** I own an education record
**When** I open `/educations/:id` and edit it
**Then** `GET` loads detail and `PATCH` updates it (at least one of title/specialization/degree; disabled until dirty+valid) (FR18)

**Given** I delete a record
**When** I confirm the destructive dialog
**Then** `DELETE` soft-deletes it; a not-found/soft-deleted target returns `404`, a cross-user mutation returns `403` (FR18, FR41, AR15)

---

## Epic 7: Whole-Surface Shell & Honest Placeholders  *(completeness)*

Every destination across the shipped surface resolves to a working feature, a clear empty/error state, or an unmistakable "coming soon" placeholder — so testers never mistake an unbuilt Tier-3 feature for a bug. Naturally last: the no-dead-ends sweep needs the real pages from Epics 1–6 to exist.

### Story 7.1: Consistent "coming soon" placeholder system

As a **tester**,
I want **unbuilt features to look intentionally previewed, not broken**,
So that **my UI/UX feedback stays trustworthy**.

**Acceptance Criteria:**

**Given** the Tier-3 features (AI match score, job-source connect/aggregation, application/response tracking, multi-provider LLM config)
**When** I reach any of them
**Then** I see the one canonical `ComingSoonPlaceholder` treatment — dashed/hatched container + mono "Coming soon" pill + title + "previewed now, real next" — non-interactive and clearly labeled, never error-styled or half-built (FR39, UX-DR9)

**Given** a vacancy card or detail
**When** it renders the AI match-score area
**Then** it shows the card-stub variant ("AI match · soon") consistent with the full-panel treatment (FR39, UX-DR9, UX-DR10)

### Story 7.2: No-dead-ends navigation sweep

As a **user**,
I want **every navigation destination to resolve to something coherent**,
So that **the product feels whole with no broken or dead links**.

**Acceptance Criteria:**

**Given** the full shipped navigation surface (all sidebar entries and in-page links)
**When** I navigate anywhere
**Then** every destination resolves to a working feature, a clear empty state, an actionable error, or an intentional placeholder — never a dead link or broken-looking gap (FR38)

**Given** an unknown or unresolved route
**When** I land on it
**Then** a 404 route fallback offers a clear path back to safety (FR38, UX-DR12)

**Given** the navigation audit
**When** it is performed
**Then** every Tier-1/Tier-2 destination is verified working and every Tier-3 destination is verified showing the canonical placeholder (FR38, FR39)
