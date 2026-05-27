# Demo MVP — Architecture Decisions (Solutioning)

**Author:** Winston (System Architect) · **Date:** 2026-05-27
**Status:** Ratified — contract-first baseline for the Demo MVP parallel tracks
**Inputs:** `prd-demo-mvp.md`, `ux-design-specification.md`, `ux-design-directions.html`, `project-context.md`, `docs/FRONTEND_IMPLEMENTATION_GUIDE.md`, live backend code (Phase C complete).

> This document resolves the three calls the PRD deferred to architecture:
> 1. **Angular stack / state-form** ratification (PRD §"Technical Architecture Considerations").
> 2. **FR4 sign-in open flag** (PRD §"Functional Requirements" — explicit "resolve in the architecture step").
> 3. **`POST /api/v1/cover-letters/generate`** contract, pinned **contract-first** before parallel work begins.
>
> Where the UX spec recommended and where the PRD left a fork, the **ratified** call is stated with its trade-off. These are decisions, not suggestions — the Frontend, Backend-LLM, and Data/seed tracks build against them.

---

## Decision 1 — Angular Stack, State & Forms

The PRD pinned the platform (Angular/TS SPA, CSR, no SSR/SEO, feature-sliced per FE Guide) and deferred state/form/server-cache tooling. The FE Guide is React-flavored; below is the **ratified Angular remapping**.

### 1.1 Ratified stack

| Concern | FE Guide (React) | **Ratified (Angular)** | Rationale |
|---|---|---|---|
| Component framework | — | **Angular standalone components** (no NgModules) | Current Angular default; less ceremony, aligns feature-sliced layout. |
| Client / UI state | minimal ephemeral | **Angular Signals + injectable services** | Native, boring, reactive. No NgRx — see 1.2. |
| Server state / cache | TanStack Query | **HttpClient + signals + a thin per-entity service cache** | See 1.3. Native, zero experimental deps. |
| Forms | React Hook Form + Zod/Yup | **Typed Reactive Forms** (`FormGroup`/`FormControl<T>`) + validators mirroring backend rules | Schema-driven equivalent; typed since Angular 14. Validate on blur + submit; submit only changed fields (PATCH); disable submit until dirty + valid (FE Guide §7). |
| Component kit | bespoke contracts | **spartan-ng (brain) on Angular CDK + helm, themed by tokens** | Ratifies the UX recommendation — see 1.4. |
| HTTP / auth transport | cookie canonical | **`withCredentials: true` on every request via an HTTP interceptor**; HTTP-only cookie is canonical | Matches `CookieAuthService`; bearer body only on refresh for non-browser clients. |
| Error normalization | Problem Details | **One interceptor normalizes RFC 7807 → typed `ProblemDetails` model**; a global handler maps 401→re-auth, 403/404/409/400/5xx→UX states | NFR4/NFR5; Journey 4 recovery model. |
| Enum/type drift | generate from OpenAPI | **Generate TS enums/DTOs from backend OpenAPI** (`/openapi/v1.json`) | FE Guide §12; PRD "avoid drift". |

### 1.2 State management — Signals + services, **not** NgRx

**Ratified:** Angular Signals with injectable, feature-sliced services. **NgRx is explicitly out of scope** this round.

- **Trade-off:** NgRx (or NgRx SignalStore) buys a disciplined action/effect model and devtools — valuable when many components mutate one complex shared state machine. This app has none of that: it is CRUD + one generation flow, single-actor, no cross-feature shared mutable state. NgRx would add ceremony and a dependency for benefit we don't consume (**Rule of Three before abstraction**).
- The one piece of genuinely *shared* client state is the **active cover-letter draft** (cross-cutting, must survive navigation) — handled by a dedicated `DraftStore` service (signal-backed) plus local persistence (see 1.5), not a global store.

### 1.3 Server state — Native Angular + thin cache **(your call)**

**Ratified:** `HttpClient` + signals + a small per-entity service cache; no third-party query library.

```
ResumeService.list() -> Signal<Resume[]>   // caches, exposes refetch()
mutation success     -> service.invalidate('resumes')  // explicit
```

- **Trade-off accepted:** we hand-wire invalidation and forgo automatic background refetch/dedup that TanStack Query's Angular adapter would give for free — but that adapter still carries an *experimental* label, and the demo's data flows are simple (list → detail → mutate → invalidate). Native keeps the dependency surface boring and stable. If post-demo growth (multi-source ingestion, richer caching) makes manual invalidation painful, revisit TanStack then — the service boundary makes that swap local.
- **Convention:** each entity service owns its cache signal + `refetch()` + `invalidate()`. Mutations call `invalidate()` on success; views read the signal. Route/section **skeletons** cover first-load (NFR2, no layout shift).

### 1.4 Component kit — spartan-ng on CDK, themed by tokens (ratified)

The UX-side recommendation is **ratified**. The decision that matters is the *category*: **themeable headless primitives on Angular CDK**, branded entirely from the token layer (FE-Guide structure + Career OS skin). spartan-ng is the recommended concrete kit; **PrimeNG or Taiga UI remain approved fallbacks** if coverage gaps prove costly — swapping the kit must not change the token layer or the branded component contracts (`TextField`, `SelectField<T>`, `TagInput`, `AvatarUploader`, `ResumeForm`, etc.). Kit lives in `shared/ui`; domain components compose upward. CDK supplies focus-trap/overlay/live-announcer for the a11y floor (NFR10) and the GenerationSheet (Direction B).

### 1.5 Draft resilience (cross-cutting, net-new)

UX §2.5 requires the active draft to survive tab-close/refresh/nav/connection-loss. **Ratified:** a `DraftStore` service holding the active draft in a signal, **autosaved (debounced) to `localStorage`**, keyed by `userId + vacancyId`.

- `localStorage` over IndexedDB: a single text draft per (user, vacancy) is tiny and synchronous-friendly; IndexedDB is over-engineering here (**boring technology**).
- **Local only** — never sent to logs (NFR7); `/generate` still returns a non-persisted draft. Lifecycle: restore-prompt on reopen → cleared on **Save** (server becomes source of truth) or explicit **Discard**.

### 1.6 Routing additions

Adopt FE-Guide routes plus the IA growth from the UX spec (flat sidebar adds Vacancies + Cover Letters as peers): add `/sign-in`, `/vacancies`, `/vacancies/:id`, `/cover-letters`, `/cover-letters/:id`. **All routes guarded except `/sign-up` and `/sign-in`** (NFR4, FR6). 401 → re-auth with intended-destination return (FR5).

---

## Decision 2 — FR4 Sign-In (open flag resolved)

### 2.1 The gap (confirmed against live code)

FR4 ("a returning user can sign in with their credentials") **requires a new backend capability.** Verified in `UsersController.cs`:

- `POST /api/v1/users` — registration, `[AllowAnonymous]`, sets cookie. ✅
- `POST /api/v1/users/token/refresh` — **`[Authorize]`-gated**; only re-issues a token from an *already-authenticated* session. It **cannot bootstrap a cold session** — a returning user whose cookie has expired has no credential entry point. ❌

So registration + refresh do not satisfy FR4. A new anonymous credential-verifying endpoint is needed. The building blocks already exist: `IPasswordHasher.VerifyHashedPassword`, and `IUserRepository.GetByEmailAsync` / `GetByLoginAsync`.

### 2.2 Ratified endpoint

**`POST /api/v1/users/sessions`** — `[AllowAnonymous]`. ("Create a session" — REST-clean, parallels registration; avoids overloading `/token/refresh`.)

**Identifier: email *or* login** *(your call)* — single `identifier` field resolved server-side.

```
POST /api/v1/users/sessions
Content-Type: application/json
{
  "identifier": "daria@example.com",   // email OR login name
  "password":   "••••••••"
}

200 OK
  Set-Cookie: <auth cookie, HTTP-only>   // same CookieAuthService path as registration
  { "id","loginName","email","phone","location","about","avatar" }   // mirrors CreateUserResult
  // accessToken in body ONLY for bearer transport (mirror refresh policy); empty for browser

401 Unauthorized  -> generic "Invalid credentials" (no email/login enumeration; same response whether
                     identifier unknown or password wrong)
400 Bad Request   -> missing/empty identifier or password (RFC 7807)
429 Too Many Requests -> sign-in attempt rate limit (see 2.3)
```

### 2.3 Rules

- **Resolution:** try `GetByEmailAsync(identifier)`; if null, `GetByLoginAsync(identifier)`. Soft-deleted users do not authenticate.
- **Anti-enumeration:** one generic 401 for unknown-identifier *and* wrong-password; keep timing reasonably constant (the PBKDF2 verify already dominates). Never reveal which field failed.
- **Session issuance:** identical to registration — `IJwtTokenService.GenerateTokenAsync(userId)` → `CookieAuthService.SetAuthCookie`. No new token mechanics.
- **Rate limiting:** **5 failed attempts per 15 min per identifier+IP**, then `429` + `Retry-After`; count failures only, a success resets the window (blunts credential stuffing without locking out a fat-fingered friend). Same limiter family as generation (Decision 3). Config: `RateLimit:SignIn:MaxAttempts = 5`, `RateLimit:SignIn:WindowMinutes = 15`.
- **Application shape (follows house MediatR convention):** `SignInCommand { Identifier, Password } : IRequest<SignInResult>` + `SignInCommandValidator` (non-empty) + handler (resolve → verify → return user projection); controller issues cookie. No domain change, no migration.
- **No new auth scheme** — the existing JWT-in-cookie pipeline (`AuthenticationCollectionExtensions`) is unchanged.

---

## Decision 3 — `POST /api/v1/cover-letters/generate` (pinned, contract-first)

The contract-first deliverable. **The endpoint returns a draft and does NOT persist** (FR28). Persistence stays on the existing `POST /api/v1/cover-letters` (vacancyId in body). Both tracks code to this section.

### 3.1 Endpoint

```
POST /api/v1/cover-letters/generate
Auth:   required (HTTP-only cookie session; [Authorize])
```

### 3.2 Request

```jsonc
{
  "vacancyId": "f1e2…",   // required, GUID
  "resumeId":  "a9b8…"    // required, GUID — explicit grounding source
}
```

- **`resumeId` is required** (not "server picks latest"). The UX résumé selector defaults to most-recent client-side and confirms the choice; sending it explicitly makes the contract deterministic and the grounding chip truthful. Zero-input UX, explicit wire contract.
- No prompt, no parameters, no tuning knobs — "one decisive click" (UX §2.3). The server assembles the prompt.

### 3.3 Success — `200 OK` (not 201; nothing is created)

```jsonc
{
  "content":      "Dear Hiring Manager, …",  // the draft; 50–10000 chars (save-compatible, see 3.5)
  "resumeId":     "a9b8…",
  "resumeTitle":  "Senior Frontend Engineer",  // feeds GroundingChip
  "vacancyId":    "f1e2…",
  "vacancyTitle": "Frontend Developer — Acme",  // feeds GroundingChip
  "generatedAt":  "2026-05-27T10:15:30Z"        // UTC
}
```

The client drops `content` into the editor (Draft badge, grounding chip), autosaves locally, and on **Save** calls `POST /api/v1/cover-letters` with `{ vacancyId, content }`.

### 3.4 Status codes (RFC 7807 on every error)

| Status | When | Retryable | Client behavior |
|---|---|---|---|
| `200` | Draft generated | — | Populate editor + grounding chip |
| `400` | Missing/empty `vacancyId` or `resumeId` | no | Inline validation |
| `401` | No / expired session | no | Route to sign-in, preserve destination (FR5) |
| `404` | Vacancy **or** résumé not found, soft-deleted, or **not owned** by caller | no | Not-found state + back CTA |
| `429` | Per-user generation rate limit hit (NFR12) | yes (later) | Explain "you've generated several quickly — try again shortly"; honor `Retry-After` |
| `502` | LLM provider returned an error | **yes** | Friendly error + **Retry**, context preserved |
| `504` | Generation exceeded hard timeout (~30s, NFR1/NFR11) | **yes** | Friendly error + **Retry**, context preserved |

**Deliberately no `409` on `/generate`.** Generation does not persist, so the one-letter-per-vacancy rule does not apply here — `409` belongs to the **save** (`POST /api/v1/cover-letters`). The UX pre-empts collisions *client-side* by routing an existing-letter vacancy to edit, but the `/generate` contract itself imposes no uniqueness and permits regeneration.

**Cross-user / body-supplied FK → `404`** (not 403), per the canonical `authorization-contract-matrix.md` rule for body-supplied foreign keys. Both `vacancyId` and `resumeId` are body FKs; a foreign or missing id yields `404`, never a 403 or a leak.

**Problem Details discriminator:** every error carries a machine-readable extension member so the client maps without string-matching:

```jsonc
{
  "type": "https://jobnecto/errors/generation-timeout",
  "title": "Generation timed out",
  "status": 504,
  "detail": "The draft took too long to generate. Please retry.",
  "code": "generation_timeout",   // ∈ { validation, not_found, rate_limited, provider_error, generation_timeout }
  "retryable": true
}
```

### 3.5 Content & grounding constraints

- **`content` length 50–10000 chars** — matches `CreateCoverLetterCommand.Content` so any returned draft is directly savable without truncation surprises; the editor enforces the same bound before Save.
- **Grounding (NFR7, fabrication mitigation):** the server builds the prompt strictly from the user's actual résumé content + vacancy fields. Prompt/response payloads with PII are **never** written to application logs. Output is framed as a draft; the human edits and saves their version (FR28).

### 3.6 LLM seam — abstraction now, provider later **(your call)**

```
Application:        ICoverLetterGenerator
                      GenerateAsync(GenerationContext, CancellationToken) -> GenerationResult
                    GenerateCoverLetterCommand { UserId, VacancyId, ResumeId } : IRequest<GenerateCoverLetterResult>
                    GenerateCoverLetterCommandHandler  (loads résumé+vacancy ownership-checked → builds context → calls generator)
Infrastructure.LLM: concrete provider impl — DEFERRED (project currently empty)
Config (server-side, no secrets in client/repo — NFR6/NFR13):
  Llm:Provider        e.g. "anthropic" | "openai"
  Llm:ApiKey          env / user-secrets only
  Llm:Model           cheap/fast tier for demo
  Llm:TimeoutSeconds  ~30 (drives the 504 hard timeout)
```

- The **HTTP contract above is provider-agnostic** — choosing Anthropic vs OpenAI later does **not** change request/response/status codes, so the Frontend track is unblocked today. The Backend-LLM track picks the concrete provider + key when it wires `Infrastructure.LLM`.
- **Timeout (NFR1/NFR11):** handler enforces `Llm:TimeoutSeconds` via `CancellationToken`; breach → `504`. Provider exception → `502`. Both retryable.
- **Rate limit (NFR12):** **10 generations per user per rolling hour** (ASP.NET Core rate limiting) → `429` + `Retry-After` past that. Single fixed window, no burst sub-limit (boring). Invisible to honest use; caps cost/abuse on a friends round. Config: `RateLimit:Generation:PermitsPerHour = 10`. Same limiter family reused for sign-in (Decision 2.3).
- **Synchronous** request/response with a loading state — no streaming/websockets this round (PRD §Real-Time).

### 3.7 What stays unchanged

- Persistence: existing `POST /api/v1/cover-letters` (`CreateCoverLetterCommand { VacancyId, Content }`, 201/400/401/404/**409**). No change.
- Vacancies: mocked behind the swappable `JobSource` seam (Data/seed track); `/generate` reads vacancy content through the same domain path, so swapping mock→real ingestion later does not touch this contract.

---

## Open items / follow-ups (not blocking parallel start)

1. **Concrete LLM provider + key custody** — chosen by the Backend-LLM track when wiring `Infrastructure.LLM`; HTTP contract is already frozen.
2. **Prompt template** — the résumé-grounded prompt text (fabrication-resistant, non-generic tone) is a Backend-LLM deliverable; it's a *quality* lever, not a contract lever.
3. **Rate-limit thresholds** — ✅ pinned: generation `10/user/hour`; sign-in `5 failed attempts/15 min/identifier+IP`. Config-tunable (`RateLimit:*`); revisit before any public launch.
4. **OpenAPI conformance** — both new endpoints must carry full `[ProducesResponseType]` attributes (the R.2 audit found one gap; don't reintroduce). Frontend enum/DTO generation reads from `/openapi/v1.json`.

---

_These three decisions are the contract-first baseline. The Frontend, Backend-LLM, and Data/seed tracks may proceed in parallel against them. Changes to Decision 3's wire contract require re-coordination across tracks._
