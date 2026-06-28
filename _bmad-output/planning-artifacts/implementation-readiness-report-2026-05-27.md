---
stepsCompleted:
  - step-01-document-discovery
  - step-02-prd-analysis
  - step-03-epic-coverage-validation
  - step-04-ux-alignment
  - step-05-epic-quality-review
  - step-06-final-assessment
overallReadiness: 'NEEDS WORK — 1 blocking gap (0% Demo-MVP epic/story coverage); PRD/UX/Architecture ready'
assessmentDate: '2026-05-27'
assessmentScope: 'Jobnecto Demo MVP (Phase D — Angular client + LLM cover-letter generation)'
governingPrd: '_bmad-output/planning-artifacts/prd-demo-mvp.md'
documentsIncluded:
  prd: '_bmad-output/planning-artifacts/prd-demo-mvp.md'
  prdParentContext: '_bmad-output/planning-artifacts/prd.md'
  ux: '_bmad-output/planning-artifacts/ux-design-specification.md'
  uxShowcase: '_bmad-output/planning-artifacts/ux-design-directions.html'
  architecture: '_bmad-output/planning-artifacts/architecture/ (sharded; index.md)'
  architectureDemoMvp: '_bmad-output/planning-artifacts/architecture/demo-mvp-architecture-decisions.md'
  epics: '_bmad-output/planning-artifacts/epics/ (sharded; index.md) — PARENT-PRD epics only'
---

# Implementation Readiness Assessment Report

**Date:** 2026-05-27
**Project:** Jobnecto
**Assessment scope:** Demo MVP increment (Phase D) — Angular SPA client + real LLM cover-letter generation.

## Step 1 — Document Inventory

### PRD
- **Governing (this assessment):** `prd-demo-mvp.md` — the Demo MVP increment PRD (FR1–FR41, NFR1–NFR13).
- **Parent context (not governing):** `prd.md` — the original full-product PRD (different FR numbering).

### UX Design
- **Whole:** `ux-design-specification.md` (status: complete, deep on net-new screens).
- **Supporting:** `ux-design-directions.html` (committed visual showcase; non-`.md` artifact).

### Architecture (sharded set under `architecture/`, `index.md` present)
- Parent/Phase-B set: `core-architectural-decisions.md`, `summary-of-architectural-decisions.md`, `project-context-analysis.md`, `epic-2-architecture-revision-2026-05-05.md`, `implementation-checklist-for-phase-b.md`, `post-merge-implementation-status-2026-04-30.md`.
- Phase C contracts: `authorization-contract-matrix.md`, `endpoint-ownership-audit.md`.
- **Demo MVP (this increment):** `demo-mvp-architecture-decisions.md` (Angular stack/state-form, FR4 sign-in, `/generate` contract).

### Epics & Stories (sharded set under `epics/`, `index.md` present)
- `epic-list.md`, `overview.md`, `requirements-inventory.md`, and `epic-1` … `epic-5`, `epic-r`.
- ⚠️ **These epics map to the PARENT PRD (`prd.md`), not the Demo MVP PRD.** No epics/stories exist yet for the Demo MVP increment (frontend + generation + sign-in + placeholders).

## Step 1 — Issues Flagged at Discovery

1. **Two distinct PRDs (selection, not a duplicate-format conflict).** `prd-demo-mvp.md` governs this assessment; `prd.md` is parent context. No removal needed — they are intentionally separate documents.
2. **🔴 Epics/stories gap (carried into analysis):** the on-disk epics describe already-merged backend work under the parent PRD's FR scheme. The Demo MVP PRD's FR1–FR41 have **no corresponding epics/stories**. This is the central readiness question and will be assessed in later steps.

## PRD Analysis (governing: `prd-demo-mvp.md`)

### Functional Requirements (41 total)

**Account & Session**
- **FR1:** Register a new account with login name, email, password.
- **FR2:** Obtain an authenticated session upon successful registration.
- **FR3:** Renew an active session without re-entering credentials.
- **FR4:** Returning user signs in with credentials to establish a session. *(Open flag — resolved in `demo-mvp-architecture-decisions.md` Decision 2.)*
- **FR5:** Expired/invalid session routed to auth and returned to intended destination.
- **FR6:** All capabilities except registration and sign-in restricted to authenticated users.

**Profile Management**
- **FR7:** View own profile (login name, email, phone, location, about, avatar).
- **FR8:** Update profile fields individually (partial update).
- **FR9:** Change login name, subject to system-wide uniqueness.
- **FR10:** Upload, replace, and remove avatar image.

**Resume Management**
- **FR11:** Create a resume (skills, experience, work-location preference, salary expectation, related fields).
- **FR12:** View a paginated list of own resumes.
- **FR13:** View full detail of one resume.
- **FR14:** Update a resume.
- **FR15:** Soft-delete a resume.

**Education Management**
- **FR16:** Create an education record (title, specialization, degree).
- **FR17:** View a paginated list of education records.
- **FR18:** View, update, and soft-delete an individual education record.

**Cover Letter Template Management**
- **FR19:** Create a reusable template with a name (unique per user) and content.
- **FR20:** View a paginated, name-searchable list of templates.
- **FR21:** View, update, and soft-delete an individual template.
- **FR22:** Insert a template's content into the cover-letter editor.

**Vacancy Discovery (mocked data)**
- **FR23:** Browse a paginated board of available vacancies.
- **FR24:** Filter vacancies by multiple criteria (skills, location, salary range, work-location type, etc.).
- **FR25:** View full detail of a vacancy.
- **FR26:** Realistic, seeded mock vacancies behind a source-swappable seam.

**Cover Letter Composition, Generation & Management**
- **FR27:** Generate an AI-drafted cover letter for a chosen vacancy, grounded in own resume.
- **FR28:** Present AI output as an editable draft; do not persist automatically.
- **FR29:** Inform user that generation sends resume + vacancy data to a third-party AI provider.
- **FR30:** Communicate generation progress and, on failure/timeout, offer retry without losing context.
- **FR31:** Compose a cover letter manually or from an inserted template, without AI.
- **FR32:** Save a cover letter for a vacancy, limited to one per vacancy.
- **FR33:** View a paginated list of saved cover letters.
- **FR34:** View a saved cover letter's detail with associated vacancy context.
- **FR35:** Update a saved cover letter's content.
- **FR36:** Soft-delete a saved cover letter.

**Application Shell, Navigation & Trust**
- **FR37:** Land on an orientation dashboard (profile completeness, resources, links into key tasks).
- **FR38:** Navigate the entire shipped surface; every destination resolves (working feature or labeled placeholder).
- **FR39:** Consistent "coming soon" placeholders (AI match score, job-source connect/aggregation, application tracking, multi-provider LLM config).
- **FR40:** Appropriate loading/empty/error states for every data view; inline validation errors; clear failure surfacing.
- **FR41:** View and modify only own resources; cross-user access denied.

### Non-Functional Requirements (13 total)

- **NFR1:** Generation returns a draft within ~15s typical; hard timeout (~30s) → clear retry, not a hang.
- **NFR2:** Data views render skeletons immediately; avoid visible layout shift.
- **NFR3:** Initial bundle loads acceptably on broadband (no formal Lighthouse gate).
- **NFR4:** Sessions carried by HTTP-only cookies; all capabilities except registration + sign-in require a valid session.
- **NFR5:** Users access/modify only own resources; ownership enforced server-side, respected by client.
- **NFR6:** No secrets/AI keys in client code or repo; provider key held server-side via config.
- **NFR7:** Only data necessary for generation sent to provider; PII payloads not written to app logs.
- **NFR8:** Any deployed environment serves exclusively over HTTPS.
- **NFR9:** Target conformance WCAG 2.2 AA.
- **NFR10:** Non-negotiable a11y floor (one h1/page, keyboard nav + visible focus, labeled inputs, aria-live errors, ≥4.5:1 body contrast, reduced-motion honored).
- **NFR11:** Generation request enforces a timeout; provider failures surfaced as retryable, never silent/fatal.
- **NFR12:** Basic per-user rate limiting on generation to cap cost/abuse.
- **NFR13:** AI provider/model swappable via server-side config without client changes.

### Additional Requirements & Constraints
- **Three-tier scope:** Tier 1 (real & functional), Tier 2 (real UI / mocked vacancy data behind `JobSource` seam), Tier 3 ("coming soon" placeholders). Placeholder discipline is itself a requirement (FR38/FR39).
- **Contract-first** on `POST /api/v1/cover-letters/generate` before parallel tracks — satisfied by `demo-mvp-architecture-decisions.md` Decision 3.
- **Generation returns a draft; does NOT persist** — persistence reuses existing `POST /api/v1/cover-letters`.
- **Frontend enums generated from backend OpenAPI** to avoid drift (FE Guide §12).
- **Single actor** (job seeker); no admin/ops/API-consumer this round.
- **Deliberately out of scope:** SSR/SEO, real-time/streaming, formal compliance (SOC2/GDPR/audit logging), scalability/HA/DR.
- **Approved trim order** under resource risk: pixel-perfect mobile → full AA → rich filtering → education UI → avatar; resume/vacancy/generation never trimmed.

### PRD Completeness Assessment (initial)
The Demo MVP PRD is unusually complete and self-consistent: 41 FRs and 13 NFRs are explicitly numbered, journeys map to capability areas, scope tiers are explicit, and the one acknowledged open item (FR4) was deliberately flagged for architecture and is now resolved. The PRD is **ready as a requirements source**. The risk is **not** in the PRD — it is downstream: whether epics/stories exist to implement these FRs (Step 3).

## Epic Coverage Validation

### What the existing epics actually cover
The `epics/` set is explicitly titled **"Jobnecto Phase B"** and states *"No UX design document for this API-only backend phase."* Its `requirements-inventory.md` defines an **independent** FR1–FR28 / NFR1–NFR13 set describing the **backend REST API** (now merged through Phase C). These reuse the same numbers as the Demo MVP PRD but mean different things (e.g., Phase-B *FR4* = "create resume"; Demo-MVP *FR4* = "returning-user sign-in"). **There is no namespacing collision in scope — they are simply two different documents.**

**Conclusion:** the existing epics map to the *parent* product's backend. **No epic or story exists for the Demo MVP increment (Phase D).**

### Coverage Matrix — Demo MVP FRs vs. Demo MVP epics/stories

Status legend: ❌ = no Demo-MVP epic/story exists. **Backend** column = whether the server-side dependency is already shipped (Phase B/C), needs **NEW** build, or is **SEED/FE** work.

| FR range | Capability | Demo-MVP epic/story | Backend dependency |
|---|---|---|---|
| FR1–FR3 | Register, session-on-register, refresh | ❌ none | ✅ shipped (`POST /users`, `/token/refresh`) — needs FE client |
| **FR4** | Returning-user **sign-in** | ❌ none | 🟠 **NEW** — `POST /users/sessions` (designed, Decision 2; not built) |
| FR5 | Session-expiry routing + return | ❌ none | ✅ 401 exists — FE routing/guard work |
| FR6 | Auth-gate all but register/sign-in | ❌ none | ✅ shipped — FE guards |
| FR7–FR10 | Profile view/update/login-name/avatar | ❌ none | ✅ shipped (`/users/me`, avatar endpoints) — needs FE |
| FR11–FR15 | Resume CRUD | ❌ none | ✅ shipped (`/resumes`) — needs FE |
| FR16–FR18 | Education CRUD | ❌ none | ✅ shipped (`/educations`) — needs FE |
| FR19–FR22 | Template CRUD + insert-into-editor | ❌ none | ✅ shipped (`/cover-letter-templates`); insert = FE |
| FR23–FR25 | Vacancy browse/filter/detail | ❌ none | ✅ shipped (`/vacancies/filter`, `/vacancies/{id}`) — needs FE |
| **FR26** | Seeded mock vacancies behind swappable seam | ❌ none | 🟠 **SEED** — Data/seed track (`JobSource` seam + synthetic data) |
| **FR27** | AI generation, resume-grounded | ❌ none | 🟠 **NEW** — `POST /cover-letters/generate` + LLM service (designed, Decision 3; not built) |
| FR28 | Draft, not auto-persisted | ❌ none | ✅ contract pinned (200, no persist) — FE + new endpoint |
| **FR29** | Third-party AI consent notice | ❌ none | 🔵 FE (ConsentNotice component) |
| **FR30** | Progress + retry without losing context | ❌ none | 🟠 partial: backend timeout (504/502); FE NarratedWait + retry + local draft |
| FR31 | Manual / template compose, no AI | ❌ none | ✅ persistence shipped — FE editor |
| FR32 | Save, one-per-vacancy | ❌ none | ✅ shipped (`POST /cover-letters`, 409) — FE |
| FR33–FR36 | Saved cover-letter list/detail/update/delete | ❌ none | ✅ shipped — needs FE |
| **FR37** | Orientation dashboard | ❌ none | 🔵 FE (composes existing reads) |
| **FR38** | Whole-surface navigation, no dead ends | ❌ none | 🔵 FE (shell + routing) |
| **FR39** | Consistent "coming soon" placeholders | ❌ none | 🔵 FE (ComingSoonPlaceholder) |
| **FR40** | Loading/empty/error states + inline validation | ❌ none | 🔵 FE (state components + Problem Details interceptor) |
| FR41 | Own-resources-only, cross-user denied | ❌ none | ✅ shipped (Epic R) — FE respects |

### Coverage Statistics
- **Total Demo-MVP PRD FRs:** 41
- **FRs covered by Demo-MVP epics/stories:** **0**
- **Coverage percentage:** **0%**
- Backend-readiness of those FRs: **~24 FRs** have shipped backend (need only the FE client); **2 FRs need NEW backend** (FR4 sign-in, FR27 generation — both designed, not built); **1 FR is SEED work** (FR26); the remainder are **pure frontend** (FR29, FR37–FR40) or hybrid (FR30).

### Missing Requirements — the single critical gap
There is exactly one structural gap, and it is total: **the Demo MVP increment has no epics or stories.** The 41 FRs are well-specified in the PRD and well-supported by architecture + UX, but **none have a traceable implementation unit.** Phase D cannot start against the existing `epics/` set (it describes already-merged Phase B work).

- **Impact:** Blocking. The three planned parallel tracks (Frontend, Backend-LLM, Data/seed) have no story-level work breakdown, no acceptance criteria, no sequencing.
- **Recommendation:** Run **`bmad-create-epics-and-stories`** against `prd-demo-mvp.md` (+ the UX spec and `demo-mvp-architecture-decisions.md`) to produce a Demo-MVP epic set — naturally structured along the PRD's three tracks plus the dashboard/shell. This is the prerequisite deliverable before implementation.

## UX Alignment Assessment

### UX Document Status
**Found and complete.** `ux-design-specification.md` (14 steps complete) + `ux-design-directions.html` showcase. Deep on the net-new screens (generation, editor, vacancy board/detail, placeholders, dashboard), lighter on CRUD by design.

### UX ↔ PRD Alignment — **strong**
- The UX persona (Daria) and its four journeys are the PRD's journeys, designed as interaction flows. ✅
- Every PRD capability area is addressed: generation (core), manual/template compose, full CRUD runway, vacancy board/detail, dashboard orientation (FR37), whole-surface navigation (FR38), the "coming soon" system (FR39), and loading/empty/error states (FR40). ✅
- UX honors the backend error contract explicitly (404 detail/cross-user, 403 forbidden mutation, 409 duplicate, 401 expiry, 400 validation, 500/timeout retryable) — matches Phase C's `authorization-contract-matrix.md`. ✅
- Consent notice (FR29), résumé-grounded visible draft (FR27/FR28), one-letter-per-vacancy awareness (FR32) all present. ✅

### UX ↔ Architecture Alignment — **strong (architecture was written with UX as input)**
- UX kit recommendation (spartan-ng on CDK) → **ratified** in Decision 1.4. ✅
- UX GenerationSheet = CDK overlay side-sheet (Direction B) → supported by the CDK-based kit + a11y foundation. ✅
- UX grounding chip needs résumé title × vacancy title → the `/generate` response (Decision 3.3) returns exactly `resumeTitle` + `vacancyTitle`. ✅ (contract designed to feed the chip)
- UX narrated wait + timeout/retry + rate-limit messaging → Decision 3.4's `504`/`502` (retryable) + `429`. ✅
- UX state/form deferred to architect → resolved (Signals + services, typed Reactive Forms, native server cache) in Decision 1. ✅
- UX local-autosave ("localStorage/IndexedDB") → Decision 1.5 **disambiguated to `localStorage` + `DraftStore`**. ✅

### Alignment Issues / Carry-Forward Notes (minor — none blocking)
1. **Draft resilience has no dedicated FR.** UX makes local autosave + restore-on-return a success criterion and architecture pinned it, but it rides implicitly under FR28 (draft) + FR30 (don't lose context). → When epics are written, ensure an explicit story + acceptance criteria so it isn't lost.
2. **Multi-résumé selection at generation** is a UX affordance (ResumeSelector when >1) and architecture made `resumeId` **required**; PRD FR27 only says "grounded in their own resume." Aligned, but the selector behavior should be an explicit story AC.
3. **Vacancy filtering is POST-based** (`POST /api/v1/vacancies/filter`, body criteria) on the shipped backend; the UX faceted-chip filter must map to a POST typed service, not query params. Note for the FE track — not a conflict.
4. **Token hexes "to be confirmed at implementation"** (hybrid Career OS reconciliation) — a known, accepted detail, not a blocker; the token *category* and roles are pinned.

### Warnings
- No UX-side misalignment threatens readiness. The UX is a **strength**, not a risk. The carry-forward notes above are inputs to epic/story creation, not gaps in the UX itself.

## Epic Quality Review

**Review status: BLOCKED — no Demo-MVP epics or stories exist to review.** A quality pass against the create-epics-and-stories standards (user-value focus, epic independence, no forward dependencies, story sizing, acceptance-criteria completeness, FR traceability) cannot be performed against an empty artifact. The existing `epics/` set belongs to the merged Phase B/C backend and is out of scope for this increment.

This is not a "pass" — it is a **deferred review**. The single critical finding from Step 3 (0% epic coverage) is the cause.

### Quality bar the Demo-MVP epics MUST clear when created
Recorded now so the `bmad-create-epics-and-stories` run produces a compliant set on the first pass:

- **User-value epics, not technical milestones.** Structure around outcomes (e.g., "Sign in and land oriented," "Generate a tailored cover letter," "Manage my resume," "Browse vacancies," "Whole-surface shell & honest placeholders") — *not* "Set up Angular project," "Build API client," "Configure tokens." Foundational FE scaffolding belongs *inside* the first user-value epic's first story, brownfield-style.
- **Brownfield indicators (no greenfield starter epic).** The backend is shipped; there is **no starter-template story** for the API. The FE *is* a new codebase, so the first FE epic's first story legitimately includes Angular project init, token layer, HTTP/Problem-Details interceptor, and OpenAPI type generation — scoped as the enabling slice of a user-value story, not a standalone "infrastructure" epic.
- **Contract-first sequencing (the live dependency risk).** Two FRs need NEW backend (FR4 sign-in, FR27 generation). The `/generate` and `/sessions` contracts are already pinned (`demo-mvp-architecture-decisions.md`), so FE and backend stories can run in **parallel** against the frozen contracts — but the epic plan must make that explicit and avoid a forward dependency where an FE story silently needs an unbuilt endpoint with no contract. (The contracts exist, so this is manageable — but it must be written down.)
- **Seed track as its own slice.** FR26 (seeded mock vacancies behind the `JobSource` seam) is distinct work; give it a story so vacancy-board FE stories aren't blocked on absent data.
- **No forward dependencies; independently completable stories; Given/When/Then ACs** covering happy path + the error contract (401/403/404/409/400/500/timeout/429) the UX already specifies.
- **Honor the PRD trim order** in sequencing so the magic spine (resume → vacancy → generate → save) is never gated behind completeness work (avatar, rich filtering, full AA).

## Summary and Recommendations

### Overall Readiness Status
**🟠 NEEDS WORK — one blocking gap, otherwise strong.**

Three of the four planning pillars are implementation-ready:

| Pillar | Status | Note |
|---|---|---|
| PRD (`prd-demo-mvp.md`) | ✅ Ready | 41 FRs / 13 NFRs, numbered, internally consistent; sole open flag (FR4) resolved |
| UX (`ux-design-specification.md`) | ✅ Ready | Complete; strong alignment to PRD; net-new screens specified in depth |
| Architecture (`demo-mvp-architecture-decisions.md`) | ✅ Ready | Stack/state-form ratified, FR4 resolved, `/generate` contract pinned + rate limits |
| **Epics & Stories (Demo MVP)** | **🔴 Absent** | **0% FR coverage — no epics/stories exist for this increment** |

The increment is **not blocked on thinking** — it is blocked on **decomposition**. Everything needed to write the epics (requirements, designs, contracts) is in place and aligned.

### Critical Issues Requiring Immediate Action
1. **🔴 BLOCKING — No Demo-MVP epics or stories.** All 41 FRs lack a traceable implementation unit. The three planned parallel tracks (Frontend, Backend-LLM, Data/seed) have no work breakdown, sequencing, or acceptance criteria. Phase D cannot start. → **Run `bmad-create-epics-and-stories`** against the PRD + UX + `demo-mvp-architecture-decisions.md`.

### Non-Blocking Carry-Forwards (feed into epic creation)
2. **Draft resilience** (local autosave + restore) needs an explicit story/AC — it has no dedicated FR (rides under FR28/FR30).
3. **Multi-résumé selection at generation** (ResumeSelector; `resumeId` required by the contract) needs an explicit story AC.
4. **Vacancy filtering is POST-based** (`/vacancies/filter`) — FE typed service must POST criteria, not use query params.
5. **Open implementation details (already logged in architecture):** concrete LLM provider + key custody, the résumé-grounded prompt template, and OpenAPI `[ProducesResponseType]` conformance on the two new endpoints. None block epic creation.

### Recommended Next Steps
1. **Create the Demo-MVP epic & story set** (`bmad-create-epics-and-stories`), structured as user-value epics along the three tracks + dashboard/shell, clearing the quality bar recorded in the Epic Quality Review section above.
2. **In that breakdown, make the contract-first parallelism explicit:** FE stories consuming `/sessions` and `/generate` reference the pinned contracts; backend stories build those endpoints; neither blocks the other. Give FR26 (seed/`JobSource`) its own slice.
3. **Fold the four carry-forward notes** (#2–#5) into the relevant stories as acceptance criteria so they aren't lost.
4. **Re-run this readiness check** once epics exist — Steps 3 and 5 will then have real artifacts to validate (coverage matrix + quality pass), converting this 🟠 into a ✅.

### Final Note
This assessment found **1 blocking issue** (zero epic/story coverage for the increment) and **4 non-blocking carry-forwards**, across the document-coverage and traceability categories. The PRD, UX, and Architecture are aligned and ready; the missing piece is decomposition into epics and stories. Address the blocking issue — a single, well-scoped workflow run — before proceeding to Phase D implementation.

---

_Assessor: Winston (System Architect) · Date: 2026-05-27 · Governing PRD: `prd-demo-mvp.md`_
