---
epic: r
date: 2026-05-25
author: Amelia (Developer)
participants:
  - Amelia (Developer)
  - Winston (System Architect)
  - Murat (Test Architect)
  - Alice (Product Owner)
  - Charlie (Senior Dev)
  - Timmy (Project Lead)
summary: Retrospective for Epic R - Authorization & Ownership Enforcement Hardening
---

# Retrospective Analysis: Epic R — Authorization & Ownership Enforcement Hardening

## 1. Epic Summary

Epic R was a brownfield hardening epic applied to already-shipped APIs (Epics 1–5). It closed Phase C (Security) by standardizing ownership enforcement, codifying the 403/404 contract matrix, adding a CI-protected authorization regression suite, and issuing a formal Phase C completion gate.

### Story Delivery

| Story | Status | Character |
|---|---|---|
| R.1 — Separate Soft Delete Repository Contract | done | Pure refactoring — zero HTTP behavioral change |
| R.2 — Endpoint Ownership Policy Audit | done | Audit — no behavioral gaps found; 1 OpenAPI metadata gap |
| R.3 — Authorization Regression Integration Suite | done | Test-only — 40 new integration tests across 7 files |
| R.4 — Consistent Forbidden vs NotFound Contract Matrix | done | Documentation + 1 OpenAPI gap closed; 3 code review patches |
| R.5 — Authorization Hardening Completion Gate | done | Verification + docs — GO issued 2026-05-25 |

### Metrics

| Metric | Value |
|---|---|
| Stories delivered | 5/5 (100%) |
| Test suite growth | 292 → 520 (+228 tests) |
| Behavioral gaps found in audit | 0 |
| OpenAPI metadata gaps closed | 2 |
| Code review patches | 3 (all R.4) |
| Deferred technical debt items | 4 |
| Phase C outcome | CLOSED — GO 2026-05-25 |
| Phase D status | Cleared to start |

---

## 2. What Went Well

### Consistent ownership from the start paid off
Zero behavioral gaps in R.2's audit of 29 endpoints across 6 resources. Five epics built with correct security instincts before any formal policy document existed. Ownership contract (404 on cross-user read, 403 on cross-user mutation) was applied uniformly without a written standard.

### The sequential story chain worked
R.2 → R.3 → R.4 → R.5 had clean handoffs: audit first, regression-test second, codify third, gate fourth. The R.5 gate blocking on its first run (R.2 and R.3 still in `review`) is the process working correctly — not a failure.

### R.1 interface hierarchy was architecturally clean
`IRepository<T>` → `ISoftDeleteRepository<T>` → `IMutableRepository<T>` is a precise separation of concerns. `VacancyRepository` implementing `SoftDeleteAsync` inline (rather than inheriting `SoftDeletableRepository<T>`) demonstrates correct scope discipline — the base-class change would have silently added unintended `UpdateAsync` capability.

### R.3 authorization regression suite is a durable CI asset
40 `[Fact]` integration tests (UsersMe=8, CoverLetters=8, Resumes/Educations/CoverLetterTemplates=7 each, Vacancies=3) exercise the full cross-user surface at the HTTP level. Sentinel-based no-leak assertions and `IgnoreQueryFilters` entity-state checks go beyond status-code verification. Phase D features cannot accidentally break ownership behavior without CI blocking the merge.

### Code review caught what implementation missed (R.4)
The implementation removed `[ProducesResponseType(409)]` from `CoverLetterTemplatesController.UpdateAsync`, reasoning the PATCH handler had no explicit conflict path. The reviewer identified `IX_CoverLetterTemplates_UserId_Name` — a unique DB index that makes 409 reachable via `GlobalExceptionHandler`'s Postgres `UniqueViolationException` mapping. Fix: one line restored. This cannot be caught by handler unit tests.

---

## 3. Challenges and Lessons

### Lesson 1: Interface composition naming is non-obvious
R.1 debug: `SoftDeletableRepository<T>` was initially declared to implement `ISoftDeleteRepository<T>` instead of `IMutableRepository<T>`. Build failed because `UnitOfWork` expects `IMutableRepository<T>` on its properties. Fast fix, but the composition rule — `IMutableRepository` = `IEditableRepository` + `ISoftDeleteRepository` — is not self-evident from the name alone.

**Action:** Consider adding an XML doc comment to `IMutableRepository<T>` stating the composition rule explicitly before Phase D adds new repository types.

### Lesson 2: R.5 required two runs due to sibling story review timing
First gate run (2026-05-23) halted because R.2 and R.3 were `review` not `done`. Gate design correctly blocked. Second run (2026-05-25) completed with a different agent model (claude-opus-4-7). No functional impact found, but the two-run cross-agent handoff is worth noting.

### Lesson 3: Test count arithmetic was confusing across stories
R.1 reported 292. R.2 reported 477 (stale — working tree included Epic 3/4/5 tests). R.3 reported 516 (also stale). R.5 unified count: 520. The per-story counts were misleading because sibling stories accumulated in the working tree. The canonical number is the R.5 gate figure: **520**.

### Lesson 4: Epic 4's deferred policy items remain open
Timestamp UTC policy, validator checklist, conflict-detail privacy, and idempotency strategy were all intentionally deferred by Timmy (Project Lead) in the Epic 4 retro. Epic R did not address them (different scope). They remain parked for a post-MVP cleanup sprint.

---

## 4. Previous Retro Follow-Through (Epic 4 → Epic R)

| Epic 4 Action Item | Epic R Status |
|---|---|
| Tighten artifact review pass (story status drift) | Out of scope for Epic R |
| Set project-wide timestamp policy | Intentionally deferred (Timmy) |
| Define validator checklist | Intentionally deferred |
| Resolve conflict-detail privacy guidance | Intentionally deferred |
| Architecture: vacancy filter approach | Closed in Epic 4 ✅ |

---

## 5. Technical Debt Snapshot

All items below are intentionally deferred per MVP timeline decision:

| Item | Severity | Rationale for deferral |
|---|---|---|
| F4: Concurrent PATCH rename race (CoverLetterTemplates) | MEDIUM | DB constraint is the safety net; no data corruption possible; extremely unlikely in practice |
| F5: GlobalExceptionHandler Postgres string matching fragility | LOW | Production and dev are both Postgres; only bites on provider switch |
| `CancellationToken ct` unused in `SoftDeleteAsync` | LOW | Pre-existing pattern — `EditableRepository.UpdateAsync` same behavior |
| `DateTime.UtcNow` hardcoded in `SoftDeletableRepository` | LOW | Pre-existing pattern; UTC consistency maintained |

---

## 6. Significant Discovery: Phase D Has No Epics Defined

R.5 issued a GO for Phase D on 2026-05-25. However, the epic list ends at Epic R. Phase D (Ingestion and LLM) is cleared to start but has no planned epics, stories, or scope.

**This is the critical path blocker for MVP.**

**Required action before Phase D implementation begins:**
- Run `bmad-agent-pm` or `bmad-agent-architect` to scope Phase D epics
- Define at minimum one Phase D epic before invoking `bmad-dev-story`

---

## 7. Readiness Assessment

| Area | Status |
|---|---|
| All 5 stories | ✅ done |
| Build (Release, --warnaserror) | ✅ 0 warnings, 0 errors |
| Tests | ✅ 520/520 passing |
| Authorization regression suite in CI | ✅ No trait/env gating |
| Endpoint ownership audit doc | ✅ Exists and linked |
| Authorization contract matrix | ✅ Exists and linked |
| Phase C roadmap/docs updated | ✅ All four docs updated in R.5 |
| Phase D cleared | ✅ GO issued 2026-05-25 |

**Epic R is fully complete. No blockers.**

---

## 8. Action Items

| # | Action | Owner | Priority |
|---|---|---|---|
| 1 | Plan Phase D epics (Ingestion + LLM) before starting any Phase D implementation | Timmy + John (PM) / Winston (Architect) | 🔴 Critical path for MVP |
| 2 | Add XML doc comment to `IMutableRepository<T>` clarifying composition rule | Amelia | Low — before next repo type is added |
| 3 | Roll deferred policy items (timestamp, validator, conflict privacy) into post-MVP tech-debt sprint | Timmy | Post-MVP |
| 4 | Run Epic 5 retrospective | Amelia + Timmy | Next (immediate) |

---

## 9. Commitments

- Epic R retrospective complete and recorded.
- `epic-r-retrospective` status → `done` in sprint-status.yaml.
- Phase D planning identified as critical path before any implementation.
- Deferred technical debt items (F4, F5, ct, DateTime) held for post-MVP sprint.
- Epic 5 retrospective to run immediately following this session.

Amelia (Developer): "Epic R is closed. Phase C is closed. Phase D is waiting on planning. On to Epic 5."
