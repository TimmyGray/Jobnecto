---
epic: 5
date: 2026-05-10
author: Amelia (Developer)
participants:
  - Amelia (Developer)
  - Timmy (Project Lead)
summary: Sprint plan and implementation-time requirements for Epic 5 stories
---

# Epic 5 Sprint Plan: Cover Letter Application Management

Amelia (Developer): "This plan sequences Epic 5 implementation and sets explicit time requirements for each story."

## Planning Assumptions

- Estimates include implementation, tests, CI-parity verification, and one review/fix pass.
- Estimates assume one experienced backend developer with existing project context.
- Estimates exclude external blockers (Epic 4 merge delays, product-scope changes).
- Productive engineering capacity assumed at ~6 focused implementation hours/day.
- Vacation entity already exists (`JobNecto.Domain.Entities.Vacancy`) — no FK blocker for 5.1.

## Dependency Notes

- **Epic 4 must be merged before Epic 5 begins** — Story 5.1 FK to `Vacancy` and the vacancy ID validation (`404` path) require Epic 4's `POST /api/v1/vacancies/filter` route and its integration tests to be stable. Epic 5 branch should be rebased onto `master` after Epic 4 merges.
- The `CoverLetterTemplate` entity from Epic 3 is already merged — optional template FK in 5.1 is unblocked.
- The shared exception pipeline (Epic 1, story 1.1) is already wired — `409 Conflict` mapping from DB uniqueness violation can reuse the existing pattern.

## Story-by-Story Time Requirements

| Story | Scope Focus | Base Effort (hours) | Risk Buffer (hours) | Time Requirement (hours) | Time Requirement (dev days) |
| --- | --- | --- | --- | --- | --- |
| 5.1 Create Cover Letter | `POST /api/v1/cover-letters`: new `CoverLetter` entity + EF migration, FK to `Vacancy` (exists check → 404) + optional FK to `CoverLetterTemplate` (ownership check → 404), DB-level unique constraint `(UserId, VacancyId)` for non-deleted records mapped to 409, content 50–10,000 char validation, `Location` header in 201, concurrent-create integration test coverage | 15 | 4 | **19** | **3.2** |
| 5.2 List Cover Letters | `GET /api/v1/cover-letters`: cursor pagination reusing existing pattern, JOIN to `Vacancy` for `vacancyTitle` in list items, `createdAt desc` ordering, empty-state path, handler + query tests | 9 | 2 | **11** | **1.8** |
| 5.3 Get Cover Letter Detail | `GET /api/v1/cover-letters/{id}`: full field set including nullable `templateId`, nested `vacancy` object (key fields), ownership + soft-delete guard (404), integration tests | 7 | 2 | **9** | **1.5** |
| 5.4 Update Cover Letter Content | `PATCH /api/v1/cover-letters/{id}`: content update, silent ignore of any `vacancyId` in body (immutable), content validation, `updatedAt` refresh, 403 wrong user, 404 not found/deleted, integration tests | 7 | 2 | **9** | **1.5** |
| 5.5 Delete Cover Letter | `DELETE /api/v1/cover-letters/{id}`: soft-delete, 204, 403 wrong user, 404 not found/deleted, verify cover letter still accessible after linked vacancy is deleted (vacancyId preserved), integration tests | 6 | 1 | **7** | **1.2** |

## Epic 5 Total Estimate

- Total base effort: **44 hours**
- Total risk buffer: **11 hours**
- Total implementation-time requirement: **55 hours**
- Single-developer duration at planned capacity: **~9.2 dev days**

Amelia (Developer): "Epic 5 spans approximately two development weeks including verification and review cycles."

## Recommended Execution Sequence

1. Implement Story 5.1 first — establishes the `CoverLetter` entity, EF migration, uniqueness constraint, and FK validation patterns that all subsequent stories build on.
2. Implement Story 5.2 second — cursor pagination is proven from prior epics; the new complexity is the JOIN for `vacancyTitle`.
3. Implement Story 5.3 third — detail endpoint with nested vacancy object; stable once entity structure is confirmed in 5.1.
4. Implement Story 5.4 fourth — update pattern is well-established; primary attention is the immutable `vacancyId` and 403/404 split.
5. Implement Story 5.5 last — soft-delete pattern is identical to prior epics; add cascade-behavior test for vacancy deletion.
6. Reserve final hardening pass for regression across all five stories plus Epic 5 API regression suite.

## Risk Notes

- **Story 5.1 is the schedule driver.** Uniqueness-constraint race coverage adds test complexity beyond the standard CRUD pattern. If the DB-level constraint mapping requires changes to the exception pipeline, add +2 to +4 hours.
- **Vacancy ownership ambiguity in 5.1.** The AC only checks existence (`404` if vacancy not found) — it does not require the vacancy to be owned by the requesting user. Verify during Story 5.1 implementation; if ownership must be enforced, add +2 hours.
- **Story 5.2 JOIN query.** If the vacancy is soft-deleted after the cover letter is created, `vacancyTitle` resolution must still work (or degrade gracefully). Clarify during 5.2; may add +1 to +2 hours.
- **PATCH vs PATCH method choice in 5.4.** Epic 5 specifies `PATCH` for update (unlike Epic 3's `PATCH`). Verify this is intentional before implementing — if changed to `PATCH`, update the epic and requirements inventory in the same pass (see agent-learnings entry 2026-05-10).

## Delivery Checkpoints

- **Checkpoint A (after 5.1):** Cover letter creation returns `201` with correct `Location` header; uniqueness constraint returns `409`; concurrent-create test is green.
- **Checkpoint B (after 5.3):** Read paths (list + detail) return correct shapes including nested vacancy data; all 404 ownership paths verified.
- **Checkpoint C (after 5.5):** Full CRUD + soft-delete lifecycle verified; Epic 5 API regression suite green; ready for retrospective.
