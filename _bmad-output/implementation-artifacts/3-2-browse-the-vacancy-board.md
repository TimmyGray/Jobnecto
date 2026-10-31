# Story 3.2: Browse the vacancy board

Status: ready-for-dev

## Story

As a **user**,
I want **to browse available vacancies**,
so that **I can find a role to apply to**.

<!-- Track: [FE] (+ one small additive [BE] contract change) — Covers: FR23, UX-DR9 (card-stub), UX-DR10, UX-DR12, UX-DR16, NFR2 -->

## Decision Record

- 2026-10-31: **Skills come from the list endpoint.** `VacancyListItemResult` (returned by `POST /api/v1/vacancies/filter`) has no `skills` field today; only `VacancyDetailResult` has one. This story adds `Skills` (`string[]?`) to `VacancyListItemResult` and its mapper. The change is additive and non-breaking. Fetching detail per card (N+1) and dropping the chips were both rejected. Precedent: Story 2.1 carried a small BE OpenAPI fix inside an `[FE]` story. (User decision.)
- 2026-10-31: **"Generate" links to vacancy detail for now.** The UX flow is board → detail → Generate (UX spec §User Journey, Direction B side-sheet over detail). The card's primary "Generate" is a router link to `/vacancies/:id`. Story 3.4 builds the detail page with its Generate entry point, and Story 4.3 may rewire the card's Generate to open the sheet directly. Until 3.4 lands, `/vacancies/:id` resolves to the existing `ComingSoonStubPage`, which is not a dead end. (User decision.)
- 2026-10-31 (planner): **Out of scope:** the `has-letter(badge)` card state (needs cover-letter-per-vacancy data; Epic 4.6 / Epic 5), filters and the no-results state (Story 3.3), sort controls (none on the board, so the deferred `UpdatedAt` save-behavior fix is **not** triggered by this story).

## Acceptance Criteria

1. **Board renders seeded vacancies as cards**
   **Given** I am signed in and seeded vacancies exist
   **When** I open `/vacancies`
   **Then** the page sends `POST /api/v1/vacancies/filter` with a body carrying only `pageSize` (browse mode, no criteria, no `sortBy`, so the server default `createdAt` desc applies)
   **And** I see one `VacancyCard` per item, in server order, each showing title, company, location, skill chips, a work-type chip, the `MatchScoreStub`, a primary "Generate" and a secondary "View" action (FR23, UX-DR10)

2. **Cursor load-more**
   **Given** the first page reports `hasNext: true`
   **When** I activate "Load more"
   **Then** the next request echoes `lastSeenId` and `lastSeenUpdatedAt` from the previous page **in the request body** (never query params), and the new cards are appended without discarding the loaded ones
   **And** when `hasNext` is false the "Load more" control is gone, and focus lands on the first new card (or the last card) instead of dropping to `<body>`
   **And** a failed load-more keeps the loaded cards and shows an inline error with "Try again"

3. **Card actions navigate, never dead-end**
   **Given** a card for a vacancy with an `id`
   **When** I activate "View" or "Generate"
   **Then** I navigate to `/vacancies/{id}`
   **And** a vacancy without an `id` renders without either action as a link (no `/vacancies/undefined`)

4. **Honest match-score stub**
   **Given** a vacancy card
   **When** it renders
   **Then** it shows the `MatchScoreStub` Tier-3 placeholder reading "AI match · soon": a dashed container with a mono pill, non-interactive (not focusable, no click handler), with an accessible label stating it is not yet available, and never error-styled (UX-DR9, UX-DR10)
   **And** it shows no number, even if the API returns a `matchScore`

5. **Loading, empty and error states**
   **Given** the first page is in flight
   **When** the board renders
   **Then** a layout-preserving skeleton of card placeholders shows first (`role="status"`, no layout shift when cards arrive) (NFR2, UX-DR12)
   **And** when the first page returns zero items, a board **empty** state shows ("No vacancies yet", with guidance and a CTA to the dashboard). Its copy and `data-testid` are distinct from the "no results" state Story 3.3 will add (UX-DR12)
   **And** when the first page fails, an error state with Retry shows. Retry reloads the first page, and focus moves to the loading region

6. **Responsive cards, never a table**
   **Given** any breakpoint
   **When** the board renders
   **Then** vacancies render as cards: one column on `xs/sm`, two from `md`, and three from `xl`. Action buttons are touch-sized (min 44px tall) and every hover affordance has a focus-visible equivalent (UX-DR16)

7. **List contract carries skills (BE)**
   **Given** a vacancy with skills
   **When** it is returned by `POST /api/v1/vacancies/filter`
   **Then** each item includes `skills` (string array, or null when the vacancy has none), and every existing field, status code and ordering is unchanged
   **And** the OpenAPI document and the regenerated `frontend/src/shared/api/generated/schema.ts` include `skills` on `VacancyListItemResult`

## Tasks / Subtasks

- [ ] Task 1: Add `skills` to the list contract (AC: 7)
  - [ ] `backend/src/JobNecto.Application/Vacancies/FilterVacanciesQuery.cs` (UPDATE): add `public string[]? Skills { get; set; }` to `VacancyListItemResult` (after `Company`).
  - [ ] `backend/src/JobNecto.Application/Vacancies/Mappers/VacancyMappers.cs` (UPDATE): `ToVacancyListItemResult` sets `Skills = vacancy.Skills` (same as the detail mapper). Note that `Vacancy.Skills` is a public **field**, not a property.
  - [ ] `backend/tests/JobNecto.Tests/Application/Vacancies/FilterVacanciesQueryHandlerTests.cs` (UPDATE): assert mapped `Skills` for a vacancy with skills, and `null` for one without.
  - [ ] `backend/tests/JobNecto.Tests/API/Vacancies/VacanciesApiTests.cs` (UPDATE): one test that a seeded vacancy's `skills` round-trips in the filter response JSON.
  - [ ] Regenerate the TS schema: start the API (AGENTS.md *Running the API*), then `cd frontend && npm run gen:api`. Commit the regenerated `schema.ts`. If the diff contains anything beyond `skills`, stop and record why in the Debug Log (it means the committed schema had drifted).
- [ ] Task 2: `vacancy` entity (AC: 1, 2)
  - [ ] `frontend/src/entities/vacancy/model.ts` (CREATE): `VacancyListItem = components['schemas']['VacancyListItemResult']`, `PagedVacancies = components['schemas']['PagedResultOfVacancyListItemResult']`, `FilterVacanciesQuery = components['schemas']['FilterVacanciesQuery']`. Copy the doc-comment style of `entities/resume/model.ts`.
  - [ ] `frontend/src/entities/vacancy/vacancy.service.ts` (CREATE): `VacancyService` (`providedIn: 'root'`), with `filter(pageSize = 20, cursor?: PageCursor): Observable<PagedVacancies>` that POSTs `/vacancies/filter` with body `{ pageSize, ...cursor }`. Use a relative URL (the interceptor prefixes the base). No caching signal is needed; the page accumulates pages, like `ResumeListPage` does. Add a TSDoc on `filter` noting that the cursor goes in the body, and that `lastSeenUpdatedAt` actually carries `createdAt` under the default sort (the client echoes it opaquely).
  - [ ] `frontend/src/entities/vacancy/index.ts` (CREATE): barrel.
  - [ ] `frontend/tsconfig.json`: verify an `@entities/*` path alias already covers the new folder. No change expected.
- [ ] Task 3: Share the enum humanizer (AC: 1)
  - [ ] Move `humanizeEnumMember` from `frontend/src/entities/resume/resume-enums.ts` to `frontend/src/shared/lib/humanize.ts` (CREATE), export it from `shared/lib/index.ts`, and re-export it from `resume-enums.ts` so existing imports keep working. Entities must not import from sibling entities (feature-sliced rule). Move its existing spec cases to `shared/lib/humanize.spec.ts`.
- [ ] Task 4: `MatchScoreStub` (AC: 4)
  - [ ] `frontend/src/shared/ui/placeholder/match-score-stub.ts` (CREATE): `ui-match-score-stub`, a standalone OnPush component with no inputs. It renders a dashed-border container and a mono "AI match · soon" pill. Use `role="note"` and `aria-label="AI match score — not yet available"`, no `tabindex`, and no interactive children. Use tokens only (`border-dashed border-border-default`, `font-mono`, `text-text-muted`); no error/danger tokens. Export it from `shared/ui/index.ts`. This is the card-stub variant only; Story 7.1 builds the full-panel `ComingSoonPlaceholder` and may fold this in.
- [ ] Task 5: `VacancyCard` + list widget (AC: 1, 3, 6)
  - [ ] `frontend/src/widgets/vacancy-board/vacancy-card.ts` (CREATE): `widget-vacancy-card`, `input.required<VacancyListItem>()`. It renders:
    - the title (fallback "Untitled vacancy" for a missing or blank title), with company · location as a meta line (blank parts skipped, no stray separators, and `location` humanized: `CzechRepublic` → "Czech Republic")
    - skill chips, capped at 5 with a "+N" overflow chip and sr-only text. Copy `ResumeCardComponent`'s `usableSkills`/`visibleSkills`/`hiddenSkillCount`, which drop blank entries
    - a work-type chip from `workLocationType` (humanized, omitted when absent)
    - `<ui-match-score-stub />`
    - an action row: primary "Generate" (`bg-action-primary`) and secondary "View" (outline). Both are `routerLink` anchors to `['/vacancies', id]`, each with a distinct accessible name ("Generate cover letter for {title}", "View {title}"). When `id` is absent, render neither link.
  - [ ] `frontend/src/widgets/vacancy-board/vacancy-board.ts` (CREATE): `widget-vacancy-board`, a presentational `<ul>` grid (`grid gap-3 md:grid-cols-2 xl:grid-cols-3`), `track vacancy.id || $index`, plus `focusCard(index)` as in `ResumeListComponent`. The card itself is an `<article>`, **not** a wrapping link: it contains two links, and nested interactive content is invalid. Make `focusCard` focus the card's first link, or the `<article tabindex="-1">` when it has none.
  - [ ] `frontend/src/widgets/vacancy-board/index.ts` (CREATE): barrel.
- [ ] Task 6: `/vacancies` page + route (AC: 1, 2, 5)
  - [ ] `frontend/src/pages/vacancy-board/vacancy-board.page.ts` + `.html` (CREATE): `page-vacancy-board`. Mirror `ResumeListPage` closely: the `LoadStatus` signal, accumulated `items`, `uniqueById`, `cursor` via `nextCursor`, `totalCount` via `toCount`, `loadingMore`/`loadMoreFailed`, `retry()` with focus to `#skeleton`, `focusWhenFree`, and the `aria-live` announcement ("Showing N of M vacancies"). Header: eyebrow "Vacancies", with the serif-italic accent styling of the résumé page. The skeleton is 4 cards in the same grid. The empty state uses `data-testid="board-empty"`, icon 🧭, headline "No vacancies yet", guidance "Vacancies from your job sources will appear here.", and CTA "Back to dashboard" → `/dashboard`.
  - [ ] `frontend/src/app/app.routes.ts` (UPDATE): point `vacancies` at `@pages/vacancy-board/vacancy-board.page` → `VacancyBoardPage` (keep `data: { title: 'Vacancies' }`). Leave `vacancies/:id` on `ComingSoonStubPage` (that's Story 3.4).
  - [ ] `frontend/src/app/app.routes.spec.ts` (UPDATE): remove `'vacancies'` from the coming-soon route list and assert it resolves to `VacancyBoardPage`.
- [ ] Task 7: Verification and test coverage (AC: all)
  - [ ] `frontend/src/entities/vacancy/vacancy.service.spec.ts`: POSTs `/vacancies/filter` with `{ pageSize: 20 }`; a cursor is spread into the body, not the params; a custom pageSize is honored; errors propagate.
  - [ ] `frontend/src/shared/ui/placeholder/match-score-stub.spec.ts`: text, `role`/`aria-label`, not focusable (no `tabindex`, no button/link descendants).
  - [ ] `frontend/src/widgets/vacancy-board/vacancy-card.spec.ts`: all fields render; fallback title; meta with missing company/location (no stray "·"); humanized location and work type; 5-chip cap with "+N"; blank skills dropped; null skills → no chip list; both links' `href` = `/vacancies/{id}` with distinct accessible names; no links when `id` is missing; the stub is present and no number shows even when `matchScore` is set.
  - [ ] `frontend/src/widgets/vacancy-board/vacancy-board.spec.ts`: renders N cards in order; duplicate or undefined ids don't throw; `focusCard` focuses the first link, or the article when the card has no links.
  - [ ] `frontend/src/pages/vacancy-board/vacancy-board.page.spec.ts`: skeleton first; ready → cards; empty → `board-empty` + dashboard CTA; error → retry → reload + focus; load-more appends + dedupes; the cursor is sent in the body; load-more failure keeps the cards + "Try again"; the last page removes the button + focus lands on a card; the announcement text. **Assert the DOM, not just component signals** (agent-learnings 2026-10-26).
  - [ ] `frontend/src/shared/lib/humanize.spec.ts` (moved cases).
  - [ ] Backend: `dotnet test backend/JobNecto.slnx` and `dotnet build backend/JobNecto.slnx --configuration Release --warnaserror`.
  - [ ] Coverage gate ≥80% per file: backend `dotnet test ... --collect:"XPlat Code Coverage" --settings backend/coverlet.runsettings --results-directory ./coverage/backend` then `python scripts/check_coverage.py ./coverage/backend --threshold 80` (use `python`, not `python3`), and frontend `cd frontend && npx ng test --no-watch`. Never skip a gate for a missing tool (agent-learnings 2026-10-26).
  - [ ] `cd frontend && npx ng build` passes.
  - [ ] Live browser check: Development API (seeding on) + `ng serve`. Sign up a fresh user → `/vacancies` shows 20 cards, Load more → 40, button gone; View/Generate → `/vacancies/:id` stub; narrow viewport → one column. Record the result in the Debug Log.
- [ ] Task 8: PR Review (RV) recorded before merge (AGENTS.md completion checklist item 5).

## Dev Notes

### Technical Requirements

- Angular 21 standalone components, OnPush, Signals + injectable services. No NgRx, no TanStack (Decision 1). Feature-sliced placement: `entities/vacancy`, `widgets/vacancy-board`, `pages/vacancy-board`, primitives in `shared/ui`. **Entities never import sibling entities**, hence Task 3.
- Types come only from the generated schema (`components['schemas'][...]`). Never hand-write DTO interfaces. Generated numerics are `number | string`: coerce with `toCount` and never `Number(...)` raw.
- Ownership is server-side (auth cookie). The client sends no user id.
- Backend: Clean Architecture is unchanged. The change is one DTO property + one mapper line in Application. **No** controller, repository, query-handler logic or migration changes. Namespaces must match folders.

### API / Contract Guardrail

- `POST /api/v1/vacancies/filter` (`[Authorize]`): `200` `PagedResultOfVacancyListItemResult` `{ items, totalCount, lastSeenId, lastSeenUpdatedAt, pageSize, hasNext, totalPages }`; `400` for a partial cursor or an unknown `sortBy`; `401` unauthenticated. **The cursor travels in the JSON body.** Both cursor fields go together (a partial cursor → 400); `nextCursor()` already guarantees that.
- An empty or `pageSize`-only body = browse mode, ordered `CreatedAt desc, Id desc`. Do **not** send `sortBy`. `updatedAt`/`relevance` order is degraded on Postgres (see deferred work), and the board doesn't need it.
- AC7 is additive: existing consumers (`VacanciesApiTests`, `OpenApiSchemaTests`) must stay green unchanged, apart from the new assertions.
- `/vacancies/:id` is not part of this story. 404/not-found handling is Story 3.4.

### ⚠️ Trap 1: The card is not a link

`ResumeListComponent` wraps each card in one `<a>`. A vacancy card has **two** actions, so wrapping it in a link would nest interactive content (an a11y violation, and the click target becomes ambiguous). Use an `<article>` with two sibling anchors and adapt `focusCard` (Task 5).

### ⚠️ Trap 2: `lastSeenUpdatedAt` is CreatedAt under the default sort

`VacancyRepository.GetFilteredAsync` stuffs `CreatedAt` into `lastSeenUpdatedAt` when sorting by `createdAt`. Echo it back opaquely. Never derive it from an item's `createdAt` client-side, and never reformat the timestamp: send the exact string received.

### ⚠️ Trap 3: Seeded data is synthetic but per-user

Every user gets the 40-posting catalogue on sign-up only when `Seed:Vacancies:Enabled` is true (on in `appsettings.Development.json`, off in tests by default). A user who existed before the API started with seeding on gets it at the next startup backfill. For the live check, sign up a **fresh** user. Frontend unit tests mock HTTP and need no seed.

### ⚠️ Trap 4: Regenerating the schema can sweep in drift

`npm run gen:api` rewrites the whole `schema.ts` from the running API. If unrelated types change, the committed schema had drifted from master's backend. Keep it (it's generated truth), but list the extra changes in the Debug Log so the reviewer isn't surprised.

### File Structure Requirements

- `backend/src/JobNecto.Application/Vacancies/FilterVacanciesQuery.cs` (UPDATE)
- `backend/src/JobNecto.Application/Vacancies/Mappers/VacancyMappers.cs` (UPDATE)
- `backend/tests/JobNecto.Tests/Application/Vacancies/FilterVacanciesQueryHandlerTests.cs` (UPDATE)
- `backend/tests/JobNecto.Tests/API/Vacancies/VacanciesApiTests.cs` (UPDATE)
- `frontend/src/shared/api/generated/schema.ts` (REGENERATED)
- `frontend/src/entities/vacancy/{model.ts, vacancy.service.ts, vacancy.service.spec.ts, index.ts}` (CREATE)
- `frontend/src/shared/lib/{humanize.ts, humanize.spec.ts}` (CREATE), `shared/lib/index.ts` (UPDATE)
- `frontend/src/entities/resume/resume-enums.ts` (UPDATE: re-export), plus its spec if the cases move
- `frontend/src/shared/ui/placeholder/{match-score-stub.ts, match-score-stub.spec.ts}` (CREATE), `shared/ui/index.ts` (UPDATE)
- `frontend/src/widgets/vacancy-board/{vacancy-card.ts, vacancy-card.spec.ts, vacancy-board.ts, vacancy-board.spec.ts, index.ts}` (CREATE)
- `frontend/src/pages/vacancy-board/{vacancy-board.page.ts, vacancy-board.page.html, vacancy-board.page.spec.ts}` (CREATE)
- `frontend/src/app/app.routes.ts`, `app.routes.spec.ts` (UPDATE)

### Testing Requirements

- Frontend: Vitest via `ng test`, using the existing spec style (`TestBed`, `provideHttpClientTesting`, `HttpTestingController`; see `resume-list.page.spec.ts` and `resume.service.spec.ts`). Per-file coverage ≥80% is enforced by `angular.json` `coverageThresholds.perFile`.
- Backend: xUnit + Moq (handler), `JobNectoApiFactory` (API). Seeding is pinned off in every test host. Seed rows explicitly, as the existing `Filter_EmptyBody_WithSeededVacancies_*` tests do.

### Previous Story Intelligence

- **3.1 (seed track, merged PR #107):** it established the `IJobSource` seam and 40 synthetic vacancies per user, with staggered `CreatedAt` values (40 distinct). `UpdatedAt` is collapsed on Postgres (deferred), which is why the board must stay on the default sort. Its RV caught test hosts that silently enabled seeding; seeding is now pinned off everywhere through `TestHostConfigurationExtensions`.
- **2.2 (browse my résumés):** this is the board's template. It accumulates pages in the page component, de-duplicates by id, moves focus after load-more and retry, and has a `nextCursor`/`toCount` hardening and a polite live-region announcement. The 1.6 deferred items about `/resumes/undefined` and the nullable `PageCursor` are already solved by `nextCursor` and the id guard. Reuse both patterns.
- **Epic 2 retro learnings:** assert the DOM, not component state. Hold busy flags until navigation settles. Never skip a CI gate locally.

### Git Intelligence Summary

- `88cfc49` merged PR #107 (Story 3.1 seed). `c5debb0` was the feature commit (JobSource seam, ingestion, backfill).
- The FE list pattern was last touched in PRs #96/#99/#101 (Stories 2.2–2.4). The shared UI kit (`shared/ui/state`, `pagination/cursor-pagination`) is stable.

### Latest Stack Information

- .NET 10, EF Core + Npgsql. Angular 21.2 (standalone, Signals), Angular CDK 21, Tailwind token classes, Vitest through the `@angular/build` unit-test builder. `openapi-typescript` generates `schema.ts` (`npm run gen:api`). [Source: `_bmad-output/project-context.md`; `frontend/package.json`]

### References

- [Source: `_bmad-output/planning-artifacts/epics.md` - Epic 3 / Story 3.2]
- [Source: `_bmad-output/planning-artifacts/epics.md` - UX-DR9, UX-DR10, UX-DR12, UX-DR16]
- [Source: `_bmad-output/planning-artifacts/ux-design-specification.md` - §ComingSoonPlaceholder, §VacancyCard & VacancyFilters, §Empty/Loading states (lines ~571–585, 645–647)]
- [Source: `_bmad-output/planning-artifacts/architecture/demo-mvp-architecture-decisions.md` - Decision 1 (Angular stack), Decision 3.7 (JobSource seam)]
- [Source: `backend/src/JobNecto.API/Controllers/VacanciesController.cs` - `FilterAsync`]
- [Source: `backend/src/JobNecto.Infrastructure/Repositories/VacancyRepository.cs` - `GetFilteredAsync` keyset]
- [Source: `frontend/src/pages/resume-list/` and `frontend/src/widgets/resume-list/` - sibling pattern]
- [Source: `_bmad-output/implementation-artifacts/deferred-work.md` - Story 3.1 `UpdatedAt` item (not triggered: no sort exposed)]
- [Source: `_bmad-output/agent-learnings.md` - 2026-10-26 entries]

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List

## PR Review (RV)

<!-- Filled by jobnecto-qa / bmad-code-review before merge (AGENTS.md completion checklist item 5). Not a self-review. -->

## Change Log

- 2026-10-31: Story created (ready-for-dev). User decisions: add `skills` to the list DTO; card "Generate" links to vacancy detail until 3.4/4.3. AC wording sharpened: browse request shape (AC1), cursor-in-body and focus rules (AC2), action targets (AC3), stub a11y (AC4), empty-state copy and its distinctness from 3.3's no-results (AC5), breakpoints (AC6); AC7 added for the contract change.
