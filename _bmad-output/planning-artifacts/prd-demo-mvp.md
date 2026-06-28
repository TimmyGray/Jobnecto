---
stepsCompleted:
  - step-01-init
  - step-02-discovery
  - step-02b-vision
  - step-02c-executive-summary
  - step-03-success
  - step-04-journeys
  - step-05-domain
  - step-06-innovation
  - step-07-project-type
  - step-08-scoping
  - step-09-functional
  - step-10-nonfunctional
  - step-11-polish
  - step-12-complete
inputDocuments:
  - e:\apps\Jobnecto\_bmad-output\project-context.md
  - e:\apps\Jobnecto\_bmad-output\planning-artifacts\prd.md
  - e:\apps\Jobnecto\docs\JOBNECTO_BACKEND_ROADMAP.md
  - e:\apps\Jobnecto\docs\FRONTEND_IMPLEMENTATION_GUIDE.md
classification:
  projectType: web_app
  domain: HR Tech / Recruitment
  complexity: Medium
  projectContext: brownfield
releaseMode: single-release
workflowType: 'prd'
---

# Product Requirements Document - Jobnecto Demo MVP

**Author:** Timmy
**Date:** 2026-05-26

## Executive Summary

Jobnecto's backend is complete and ownership-hardened through Phase C — five epics of CRUD across users, resumes, educations, cover-letter templates, cover letters, and vacancies — yet no real user has ever touched the product, and its central bet (that AI-assisted job applications are good enough that people would switch) remains unvalidated. This increment delivers the **Demo MVP**: an Angular single-page client over the existing REST API, plus the first real LLM feature — tailored cover-letter generation — assembled into one demonstrable end-to-end journey.

The increment exists to convert untested assumptions into evidence *before* investing in the expensive job-source ingestion-and-mapping engine. A job seeker signs up, builds a profile and resume, browses a vacancy board, opens a vacancy, and clicks **Generate** to receive a cover letter tailored from their resume and that vacancy — which they edit and save. Vacancy data is **mocked** this round; the LLM generation is **real**. The principle is deliberate: *mock the plumbing, never mock the magic.*

**Target users:** The founder and a small circle of friends — a warm, qualitative dogfooding round. The polish bar is "credible and usable," not "market-ready."

**What this demo validates and what it does not:** It genuinely tests (1) the quality and usefulness of AI cover-letter generation and (2) whether the full flow hangs together end-to-end on a stable, battle-tested API. It does **not** validate the aggregation value proposition ("one place instead of 5–7") — mocked vacancies cannot prove that, and real ingestion is deferred to a later phase. The demo proves one half of the pitch for real and previews the other.

### What Makes This Special

The differentiating moment is the **"Generate" click**: writer's block to a tailored draft in seconds, grounded in the user's own resume and the specific vacancy — not a generic template. Profile, resume, and template management are table stakes that frame this moment; the generation *is* the product.

Two insights make the increment possible. The product-level insight, carried from the parent PRD: 2026-era LLMs are finally reliable enough to make real generation valuable. The increment-level insight, which shapes the whole plan: **the core value can be validated without real aggregation** — by mocking vacancies and making only the intelligence real, the team decouples the hard, low-learning ingestion work from the assumption that actually needs proving, reaching a demonstrable product in a fraction of the time.

**Why now:** The backend is stable and ownership-hardened, so a real client can be built against it without rework; the team can run frontend, LLM, and seed-data tracks in parallel, coordinating contract-first on the generation endpoint. Building the ingestion engine first would burn weeks before any user sees value.

## Project Classification

| Attribute | Value |
| --- | --- |
| **Project Type** | Web Application (Angular / TypeScript SPA) — frontend-led, consuming the existing .NET 10 REST backend. |
| **Domain** | HR Tech / Recruitment (job aggregation and AI-assisted applications). |
| **Complexity** | Medium — conventional CRUD-over-REST client, but the LLM generation flow, the mocked-vacancy seam, and contract-first parallel tracks add real coordination complexity. |
| **Project Context** | Brownfield — extends a backend complete through Phase C; layers a client plus one LLM feature on top. |

## Success Criteria

### User Success

A first-time user — the founder or a friend — can complete the full magic journey **without instructions**: register → set up profile → create a resume → browse the vacancy board → open a vacancy → **Generate** a tailored cover letter → edit and save it; and can alternatively start a letter by inserting an existing template's text. Beyond the spine, the product **feels complete** — every navigation destination leads somewhere coherent: either a working feature or a clearly-labeled "coming soon" placeholder, never a dead link or broken-looking gap.

The decisive **"aha" moment**: the generated draft is good enough to send with only minor edits. The surrounding completeness is what makes the UI/UX feedback trustworthy.

### Validation & Business Success

A learning round, not a revenue round — "success" means decision-grade evidence:

1. Qualitative feedback gathered from the friend group on generation usefulness, flow coherence, and UI/UX.
2. Enough signal to make a **go/no-go decision** on investing in real job-source ingestion next.
3. Building a real client surfaces and resolves API **contract gaps** — the demo doubles as the API's first battle test.

Indicator: the team can answer *"is the AI good enough to build the rest around?"* with evidence, not opinion.

### Technical Success

- Angular SPA consumes the existing REST API via a typed client with RFC 7807 Problem Details normalization and **cookie auth working end-to-end in a real browser**.
- New endpoint `POST /api/v1/cover-letters/generate` (backed by a new LLM generation service) returns a real, resume-and-vacancy-grounded **draft** — it does **not** persist. Persisting the edited letter uses the existing cover-letter create endpoint (`POST /api/v1/cover-letters`, vacancyId in body). The `/generate` request/response contract is agreed **before** parallel frontend/backend work begins (contract-first).
- Mocked vacancy data is seeded behind a **clean seam** (a static `JobSource` adapter) that can be swapped for real ingestion later without frontend changes.
- LLM provider and API-key configuration is resolved (provider choice, where the key lives, per-user vs global) — no hardcoded secrets.
- Every client-consumed endpoint has a matching typed service function and handled `400/401/403/404/409/500` states.

### Measurable Outcomes

1. The full demo spine is completable end-to-end by a first-time user with no written instructions.
2. 3–5 friends run the flow and return structured feedback (letter usefulness + flow coherence + UI/UX).
3. Cover-letter generation returns a usable draft within target latency (≈ ≤ 15s typical) and visibly reflects both the resume and the vacancy.
4. 100% of client-consumed endpoints have typed services and error-state handling; contract gaps found during integration are logged and closed.
5. A documented go/no-go recommendation on real ingestion, backed by the feedback.
6. **UI/UX completeness:** every shipped-backend feature has a working page; every unbuilt feature shows a consistent, unmistakable "coming soon" placeholder (never an error or dead end), so testers can form a real opinion on layout, navigation, and visual design.

## Product Scope

The frontend covers the **entire shipped backend surface** with complete pages, layouts, and components, because UI/UX is itself a feedback target. Scope is organized into three honest tiers by how "real" each area is in the demo.

### MVP - Minimum Viable Product (this increment)

**Tier 1 — Real & fully functional** (backed by a shipped endpoint):

- Auth: sign up + cookie session
- Profile / Settings — including avatar upload / replace / delete
- Resumes — full CRUD (list, create, detail, edit, soft-delete)
- Educations — full CRUD
- Cover letter templates — full CRUD + insert-into-editor
- Cover letters — full CRUD
- Vacancies — browse, filter, detail
- **Cover letter generation** — real LLM draft via new endpoint `POST /api/v1/cover-letters/generate` + new generation service (returns a draft; does not persist)
- **Cover letter creation/persistence** — existing `POST /api/v1/cover-letters` (vacancyId in body) saves the user's edited letter

**Tier 2 — Real UI, mocked data** (UI is final; data is seeded, not ingested):

- Vacancy board & detail run on seeded mock vacancies behind a swappable `JobSource` seam

**Tier 3 — "Coming soon" placeholders** (designed, not functional — no backend yet):

- AI match score (shown on vacancy cards / detail as a labeled placeholder)
- Connect a job source / multi-source aggregation
- Application & response tracking
- Multiple LLM providers / per-user LLM config

**Placeholder discipline:** Tier 3 items get a consistent, obviously-non-functional "coming soon" treatment — never half-built — so testers never mistake an unbuilt feature for a bug.

### Growth Features (Post-Demo)

Promote Tier 3 placeholders to real features — starting with the first real ingestion adapter (one source) replacing the mock, then AI match scoring, then application / response tracking.

### Vision (Future)

- Full multi-source aggregation (HeadHunter, LinkedIn, Indeed, …)
- Multiple LLM providers + per-user LLM config
- Cross-platform application / response tracking
- Mobile client

## User Journeys

**Persona — Daria, mid-level frontend developer, actively job-hunting.**
She's juggling four job boards, rewriting the same cover letter for the tenth time, and losing evenings to copy-paste. She's skeptical of "AI tools" that produce generic fluff. She's exactly the founder's friend who agreed to try the demo and tell the truth.

> **Actor coverage note:** The Demo MVP is single-actor. The only human who interacts with it is the job seeker (the founder/friend). There are no admin, support/operations, or external API-consumer actors this round — no ops console and no third-party integrations are in scope. The journeys below are therefore variations of one persona, not distinct user types.

### Journey 1 — First letter (happy path, the core magic)

- **Opening:** Daria signs up, lands on a dashboard that orients her ("complete your profile," "add a resume"). Nothing is confusing; every tile goes somewhere.
- **Rising action:** She fills her profile, creates a resume (skills, experience, work preference), then opens the vacancy board and picks a role that fits.
- **Climax:** On the vacancy detail page she clicks **Generate**. Within seconds she gets a cover letter that references *her* skills and *this* role — not a template. This is the moment she decides whether the product is real.
- **Resolution:** She lightly edits the draft, saves it, and feels she just reclaimed 20 minutes. She wants to do it again for the next role.
- **Reveals:** auth + session, profile management, resume create, vacancy board + detail, the generation endpoint, a cover-letter editor with save.

### Journey 2 — Start from my own template (alternate compose path)

- **Opening:** Daria already has a cover letter she likes and doesn't want AI to rewrite from scratch.
- **Rising action:** She saves it once as a template, then opens a vacancy and chooses to **insert the template text** into the editor as her starting point.
- **Climax:** She tweaks the inserted text for this role and saves — no LLM needed. The product respects that she sometimes wants control, not generation.
- **Resolution:** She trusts the tool more *because* it didn't force the AI on her.
- **Reveals:** template CRUD, insert-template-into-editor, manual compose + save, one-letter-per-vacancy rule.

### Journey 3 — Coming back (returning-user maintenance)

- **Opening:** A week later Daria returns with a new certification and a sharper resume.
- **Rising action:** She updates her profile, edits a resume, adds an education record, prunes an old template, and reviews the cover letters she's already generated.
- **Climax:** Everything she created is still there, scoped to her, editable — the product feels like *hers* over time, not a one-shot demo.
- **Resolution:** She keeps her materials current and treats Jobnecto as her application home base.
- **Reveals:** full CRUD across resumes/educations/templates/cover-letters, list + detail + edit, soft-delete, ownership isolation.

### Journey 4 — When things wobble (edge cases & recovery)

- **Slow/failed generation:** the editor shows a clear loading state and, on failure, a retry — never a frozen screen or a silent blank.
- **Duplicate letter:** she tries to create a second letter for a vacancy she already has one for; she gets a clear "you already have a letter for this role" message (409), not a crash.
- **Session expiry:** her cookie session lapses; she's routed to re-auth and returned to where she was (401 handling).
- **"Coming soon" encounter:** she clicks "AI match score" or "Connect a job source" and sees an unmistakable, intentional placeholder — she understands it's *planned*, not *broken*. This protects the UI/UX feedback.
- **Reveals:** loading/empty/error states, Problem Details surfacing, 401/403/404/409 handling, consistent Tier-3 placeholder treatment.

### Journey Requirements Summary

The four journeys collapse to these capability areas: **account + session**, **profile & avatar**, **resume management**, **education management**, **cover-letter template management**, **vacancy browsing on mocked data**, **AI cover-letter generation**, **manual/template-based compose**, **cover-letter management**, and a **consistent placeholder system** for Tier-3 features. No admin, support, or API-consumer capabilities are in scope — the product is single-actor this round.

## Domain-Specific Requirements

### LLM Output Integrity (this domain's signature risk)

The defining failure mode for AI-generated cover letters is **fabrication** — the model inventing skills, employers, or achievements the candidate doesn't have. A fabricated qualification in a real job application is actively harmful to the user, not just a quality miss. Mitigations are product requirements, not nice-to-haves:

- The generation prompt must be **strictly grounded** in the user's actual resume content.
- Output is framed as a **draft to review**, and the user saves *their edited version* — human review is by-design, not optional.
- Secondary risk: generic "obviously-AI" tone undercuts the whole value prop; prompt quality matters as much as model choice.

### Data Handling & Privacy

- Resumes and profiles are **real PII**, and generation sends resume + vacancy content to a **third-party LLM provider** — that data leaves the project's infrastructure. Acceptable for a consenting friends round, with three guardrails: (a) tell testers their data goes to an external model, (b) keep PII out of prompt/response logs, (c) use synthetic data in the mocked vacancies.

### Technical Constraints

- Generation is synchronous from the user's view; latency, timeout/retry, per-user rate limiting, and secret-based provider configuration are specified as measurable NFRs (see NFR1, NFR6, NFR11–NFR13).

### Deliberately Out of Scope

- No formal compliance program (SOC 2 / GDPR DSAR / consent & retention flows), no audit logging. Appropriate for a friends dogfood round; **must be revisited before any public launch.**

### Risk Mitigations Summary

| Risk | Mitigation |
| --- | --- |
| Fabricated qualifications | Resume-grounded prompt + mandatory human review + "draft" framing |
| PII to third-party LLM | Informed tester consent · no PII in logs · synthetic mock-vacancy data |
| Cost / latency runaway | Timeouts · basic rate limiting · cheap/fast model tier for demo |

## Web Application Specific Requirements

### Project-Type Overview

Angular / TypeScript **SPA**, client-side rendered, served as a static bundle against the existing REST API. It's an **authenticated app** — every route except sign-up is session-guarded — so it behaves as a private tool, not a public site. The existing `docs/FRONTEND_IMPLEMENTATION_GUIDE.md` (tokens, page UX, validation mirrors, a11y, responsive rules) is adopted as the canonical FE handoff, with its React-flavored state/form tooling remapped to Angular equivalents by the architect/UX.

### Rendering & SEO Strategy

**SPA, client-side rendered. No SSR, no SEO.** Every meaningful route is behind auth — nothing to index, and testers arrive via a direct link. Angular SSR/Universal is explicitly out of scope; it would add build/deploy complexity for zero benefit this round.

### Real-Time

**None.** Cover-letter generation is synchronous request/response with a loading state — no websockets/SignalR. Revisit only if token-streaming generation becomes a desired UX later.

### Browser Support Matrix

Evergreen only: latest 2 versions of Chrome, Edge, Firefox, Safari (Chromium / Gecko / WebKit). No legacy/IE. Smoke-test on Chromium + Firefox + WebKit (matches the FE guide).

### Responsive Design

- Breakpoint tokens per FE guide: xs 0–479 / sm 480–767 / md 768–1023 / lg 1024–1439 / xl 1440+.
- **Desktop-primary, mobile-usable:** testers are on laptops, but core create/edit flows must stay usable on mobile (card layouts instead of dense tables on xs/sm). Not pixel-perfect mobile.

### Performance Targets

- Initial bundle loads acceptably on broadband; route-level skeletons for perceived performance.
- Cover-letter generation ≤ ~15s typical, with explicit loading + timeout/retry.
- No formal Lighthouse gate for the demo; avoid layout shift on data-driven pages.

### Accessibility Level

- **Target:** WCAG 2.2 AA, per the existing FE guide.
- **Demo floor (non-negotiable even if full AA slips):** semantic structure (one `h1`/page), full keyboard navigation with visible focus, inputs tied to labels, errors announced via `aria-live`. Don't gold-plate AA at the expense of shipping — but never ship something keyboard-inaccessible.

### Technical Architecture Considerations

- Feature-sliced architecture per FE guide (`app/processes/pages/widgets/features/entities/shared`).
- Typed API client with RFC 7807 Problem Details normalization; credentials/cookies sent on every request.
- State/form tooling **deferred to architect/UX** (Angular Signals + services vs NgRx; reactive forms; a server-cache layer) — this PRD does not pin them.
- Mocked vacancy data behind a swappable data seam.

### Implementation Considerations

- **Contract-first** on the new `POST /api/v1/cover-letters/generate` endpoint before parallel tracks start.
- Generate frontend enums from backend OpenAPI / shared schema to avoid drift (FE guide §12).
- Three-tier scope visible in navigation: Tier 1/2 fully wired, Tier 3 consistent "coming soon" placeholders.

## Delivery Strategy & Risk

This increment ships as a **single release** — the Demo MVP is one coherent deliverable. The post-demo roadmap (Growth / Vision) is documented under Product Scope and is genuine future work, not invented phasing for this release.

### Strategy & Philosophy

**Approach: an Experience + Validated-Learning MVP.** The goal is a *complete-feeling* product that earns honest UX feedback and proves the AI is good enough to build on — not a revenue MVP, not a bare problem-solving spike. Completeness of the surface is a feature here, because UI/UX is itself under test; honesty about what's real (Tier 1/2) vs. previewed (Tier 3) is what keeps "complete-feeling" from becoming "fake."

**Resource model:** parallel agent-team tracks, coordinated **contract-first**:

- **Frontend track** — the Angular client across the full surface.
- **Backend-LLM track** — new `POST /api/v1/cover-letters/generate` endpoint + **new LLM generation service** (returns draft, no persistence); LLM router + config. Persistence reuses the existing `POST /api/v1/cover-letters`.
- **Data/seed track** — mocked vacancies behind the swappable `JobSource` seam.
- **Architect + UX designer** — Angular stack/state decisions and the screen designs feeding the frontend track.

### Complete Feature Set (this release)

This prioritizes the Product Scope tiers above for delivery sequencing; it does not redefine scope.

**Core journeys supported:** all four documented journeys — first-letter happy path, template-based compose, returning-user maintenance, and edge/error recovery.

**Must-have — the demo fails without these:**

- Auth + cookie session
- Resume create/edit (generation has no input otherwise)
- Vacancy board + detail on seeded mock data
- **LLM generation** (`POST /api/v1/cover-letters/generate` + new service) **and** persistence of the edited letter (existing `POST /api/v1/cover-letters`)
- Typed API client with Problem Details + error states
- Consistent Tier-3 "coming soon" placeholders

**In scope, but ranked as completeness (protect the must-haves first if a track slips):**

- Profile/settings page + avatar upload
- Education management UI
- Full template CRUD richness
- Rich vacancy filtering UI
- Full WCAG 2.2 AA above the keyboard-accessible floor
- Polished mobile layouts above "usable"

**Resource-risk contingency (approved trim order):** if a track falls behind, trim in this order — pixel-perfect mobile → full AA → rich filtering UI → education UI → avatar. Resume, vacancy browse, and generation are **never** trimmed.

### Risk Mitigation Strategy

**Technical risks:** The riskiest pieces are LLM **output grounding/quality** and the **new `/generate` endpoint contract**. Mitigations: contract-first agreement before parallel work; resume-grounded prompt; draft-and-review framing; latency timeout + retry. The mock `JobSource` seam removes ingestion as a risk entirely this round.

**Market/validation risks:** The real danger is a *hollow* demo producing misleading feedback. Mitigations: make the magic real and mock only plumbing; gather structured feedback from 3–5 friends; carry the explicit "what this validates / what it does not" framing so aggregation feedback isn't over-read.

**Resource risks:** Parallel tracks can collide or slip. Mitigations: contract-first decoupling; the priority ordering above to protect the magic spine; Tier-3 placeholders cap total scope; the approved trim order absorbs overruns without touching the core.

## Functional Requirements

> **Capability contract.** This list is binding: UX designs, architecture, and epics implement only what is listed here. **Open flag:** FR4 (returning-user sign-in) may require a new backend capability — the shipped backend currently exposes registration and token-refresh but no explicit credential sign-in endpoint. Resolve in the architecture step.

### Account & Session

- **FR1:** A visitor can register a new account with login name, email, and password.
- **FR2:** A user obtains an authenticated session upon successful registration.
- **FR3:** A user's active session can be renewed without re-entering credentials.
- **FR4:** A returning user can sign in with their credentials to establish a session. *(May require new backend capability — see flag.)*
- **FR5:** A user with an expired/invalid session is routed to authentication and returned to their intended destination afterward.
- **FR6:** The system restricts all capabilities except registration and sign-in to authenticated users.

### Profile Management

- **FR7:** A user can view their own profile (login name, email, phone, location, about, avatar).
- **FR8:** A user can update their profile fields individually (partial update).
- **FR9:** A user can change their login name, subject to system-wide uniqueness.
- **FR10:** A user can upload, replace, and remove their avatar image.

### Resume Management

- **FR11:** A user can create a resume capturing skills, experience, work-location preference, salary expectation, and related fields.
- **FR12:** A user can view a paginated list of their own resumes.
- **FR13:** A user can view the full detail of one of their resumes.
- **FR14:** A user can update a resume.
- **FR15:** A user can delete (soft-delete) a resume.

### Education Management

- **FR16:** A user can create an education record (title, specialization, degree).
- **FR17:** A user can view a paginated list of their education records.
- **FR18:** A user can view, update, and delete (soft-delete) an individual education record.

### Cover Letter Template Management

- **FR19:** A user can create a reusable cover-letter template with a name (unique per user) and content.
- **FR20:** A user can view a paginated, name-searchable list of their templates.
- **FR21:** A user can view, update, and delete (soft-delete) an individual template.
- **FR22:** A user can insert a template's content into the cover-letter editor as a starting point.

### Vacancy Discovery (mocked data this release)

- **FR23:** A user can browse a paginated board of available vacancies.
- **FR24:** A user can filter vacancies by multiple criteria (skills, location, salary range, work-location type, etc.).
- **FR25:** A user can view the full detail of a vacancy.
- **FR26:** The system provides a realistic, seeded set of mock vacancies behind a source-swappable seam.

### Cover Letter Composition, Generation & Management

- **FR27:** A user can generate an AI-drafted cover letter for a chosen vacancy, grounded in their own resume.
- **FR28:** The system presents AI generation output as an **editable draft** and does not persist it automatically.
- **FR29:** The system informs the user that generation sends their resume and vacancy data to a third-party AI provider.
- **FR30:** The system communicates generation progress and, on failure or timeout, offers retry without losing context.
- **FR31:** A user can compose a cover letter manually or from an inserted template, without using AI.
- **FR32:** A user can save (persist) a cover letter for a vacancy, limited to one cover letter per vacancy.
- **FR33:** A user can view a paginated list of their saved cover letters.
- **FR34:** A user can view a saved cover letter's detail with its associated vacancy context.
- **FR35:** A user can update a saved cover letter's content.
- **FR36:** A user can delete (soft-delete) a saved cover letter.

### Application Shell, Navigation & Trust

- **FR37:** A user lands on an orientation view (dashboard) that summarizes their profile completeness and resources and links into key tasks.
- **FR38:** A user can navigate the entire shipped product surface, with every destination resolving to a working feature or a labeled placeholder.
- **FR39:** A user sees consistent, unmistakable "coming soon" placeholders for not-yet-available features (AI match score, job-source connection/aggregation, application tracking, multi-provider LLM configuration).
- **FR40:** A user sees appropriate loading, empty, and error states for every data-driven view, with validation errors surfaced inline and other failures surfaced clearly.
- **FR41:** A user can view and modify only their own resources; cross-user access is denied.

## Non-Functional Requirements

### Performance

- **NFR1:** AI cover-letter generation returns a draft within **~15s** for a typical resume + vacancy; a hard timeout (≈30s) triggers a clear retry path rather than a hang.
- **NFR2:** Data-driven views render route/section skeletons immediately and avoid visible layout shift once data arrives.
- **NFR3:** The initial application bundle loads acceptably on broadband (no formal Lighthouse gate for the demo).

### Security & Privacy

- **NFR4:** Sessions are carried by HTTP-only cookies; all capabilities except registration and sign-in require a valid session.
- **NFR5:** Users can access and modify only their own resources; ownership is enforced server-side and respected by the client (no cross-user data exposure).
- **NFR6:** No secrets or AI-provider keys appear in client code or the repository; the provider key is held server-side via configuration.
- **NFR7:** Only the data necessary for generation (resume + vacancy content) is sent to the AI provider; prompt and response payloads containing PII are not written to application logs.
- **NFR8:** Any deployed environment serves the application exclusively over HTTPS.

### Accessibility

- **NFR9:** Target conformance is **WCAG 2.2 AA**.
- **NFR10:** Non-negotiable floor (even if full AA slips): one `h1` per page with logical heading order, full keyboard navigation with visible focus, inputs tied to labels, form errors announced via `aria-live`, body-text contrast ≥ 4.5:1, and `prefers-reduced-motion` honored.

### Integration (AI Provider)

- **NFR11:** The generation request enforces a timeout and surfaces provider failures as retryable, never as a silent or fatal error.
- **NFR12:** Basic per-user rate limiting is applied to generation to cap cost and abuse.
- **NFR13:** The AI provider/model is swappable via server-side configuration without changes to client code.

### Deliberately Out of Scope

- **Scalability / High-Availability / Disaster-Recovery** — not applicable to a small friends-only round; must be revisited before any public launch.
