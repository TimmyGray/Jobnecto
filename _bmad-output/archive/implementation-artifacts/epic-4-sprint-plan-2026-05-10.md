---
epic: 4
date: 2026-05-10
author: Amelia (Developer)
participants:
  - Amelia (Developer)
  - Timmy (Project Lead)
summary: Sprint plan and implementation-time requirements for Epic 4 stories
---

# Epic 4 Sprint Plan: Vacancy Browsing & Filtering

Amelia (Developer): "This plan sequences Epic 4 implementation and sets explicit time requirements for each story."

## Planning Assumptions

- Estimates include implementation, tests, CI-parity verification, and one review/fix pass.
- Estimates assume one experienced backend developer with existing project context.
- Estimates exclude external blockers (new infra setup, third-party API downtime, or product-scope changes).
- Productive engineering capacity assumed at ~6 focused implementation hours/day.

## Story-by-Story Time Requirements

| Story | Scope Focus | Base Effort (hours) | Risk Buffer (hours) | Time Requirement (hours) | Time Requirement (dev days) |
| --- | --- | --- | --- | --- | --- |
| 4.1 Browse Vacancies (Empty Filter Mode) | `POST /api/v1/vacancies/filter` with empty criteria, cursor pagination, list DTO mapping, auth, user-scoped results, optional `sortBy` (`createdAt` default, `updatedAt`, `relevance`) and endpoint tests | 12 | 3 | **15** | **2.5** |
| 4.2 Filter Vacancies | same `POST /api/v1/vacancies/filter` route with non-empty criteria, AND/OR filter logic, exclusion keywords, validation matrix, broad integration tests | 20 | 4 | **24** | **4.0** |
| 4.3 Get Vacancy Detail | `GET /api/v1/vacancies/{id}`, detail DTO shape, not-found path, auth + ownership policy, endpoint tests | 9 | 2 | **11** | **1.8** |

## Epic 4 Total Estimate

- Total base effort: **41 hours**
- Total risk buffer: **9 hours**
- Total implementation-time requirement: **50 hours**
- Single-developer duration at planned capacity: **~8.3 dev days**

Amelia (Developer): "Epic 4 is best planned as roughly two development weeks when including verification and review cycles."

## Recommended Execution Sequence

1. Implement Story 4.1 first (establishes the unified filter route in empty-criteria browse mode and pagination contracts).
2. Implement Story 4.2 second (extends the same route with advanced filter behavior).
3. Implement Story 4.3 third (stable once vacancy query/retrieval patterns are finalized).
4. Reserve final hardening pass for regression checks across 4.1 + 4.2 + 4.3.

## Risk Notes

- Story 4.2 is the schedule driver due to filter-composition complexity and validation edge cases.
- If filter semantics expand during implementation, add +4 to +8 hours to Story 4.2.
- If new performance constraints are introduced (index tuning, query-plan optimization), add +4 to +6 hours across Epic 4.

## Delivery Checkpoints

- Checkpoint A (after 4.1): unified filter route returns browse-mode pagination correctly with empty criteria, user-scoped rows, and `sortBy` behavior.
- Checkpoint B (after 4.2): advanced filter matrix and exclusion behavior verified under integration tests.
- Checkpoint C (after 4.3): detail endpoint done, full Epic 4 API regression suite green.
