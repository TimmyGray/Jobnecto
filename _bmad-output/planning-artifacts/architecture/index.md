# Architecture Decision Document - Jobnecto Phase B

## Navigation Index

The architecture document is organized into the following shards:

1. **[Post-Merge Implementation Status (2026-05-10)](post-merge-implementation-status-2026-04-30.md)** - Current state of Phase B implementation; which stories are merged and what infrastructure is in place
2. **[Epic 2 Architecture Revision (2026-05-05)](epic-2-architecture-revision-2026-05-05.md)** - Audit of architecture docs after Epic 2; confirms implemented baseline, stale assumptions, and Epic 3 guardrails
3. **[Project Context Analysis](project-context-analysis.md)** - Requirements overview, technical constraints, and cross-cutting concerns (ownership, soft deletes, validation, filtering, async, migrations, OpenAPI)
4. **[Core Architectural Decisions](core-architectural-decisions.md)** - Detailed architectural decisions: MediatR request/handler structure, validation strategy, repository pattern, error handling, soft deletes, ownership model, concurrency control
5. **[Summary of Architectural Decisions](summary-of-architectural-decisions.md)** - Quick reference table of all decisions, approaches, and key benefits
6. **[Implementation Checklist for Phase B](implementation-checklist-for-phase-b.md)** - Completed Phase B checklist plus deferred hardening items

## How to Use

- **Starting a story?** Read `project-context-analysis.md` to understand constraints and requirements.
- **Starting Epic 3?** Read `epic-2-architecture-revision-2026-05-05.md` first for decisions that must not be copied blindly from Epic 2.
- **Designing a new component?** Check `core-architectural-decisions.md` for patterns and decision rationale.
- **Need a quick reference?** Use `summary-of-architectural-decisions.md`.
- **Reviewing Phase B feature closure?** Use `implementation-checklist-for-phase-b.md` to distinguish completed work from deferred hardening.
