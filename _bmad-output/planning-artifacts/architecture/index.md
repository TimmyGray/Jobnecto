# Architecture Decision Document — Jobnecto Phase B

## Navigation Index

The architecture document is organized into the following shards:

1. **[Post-Merge Implementation Status (2026-04-30)](post-merge-implementation-status-2026-04-30.md)** — Current state of Phase B implementation; which stories are merged and what infrastructure is in place
2. **[Project Context Analysis](project-context-analysis.md)** — Requirements overview, technical constraints, and cross-cutting concerns (ownership, soft deletes, validation, filtering, async, migrations, OpenAPI)
3. **[Core Architectural Decisions](core-architectural-decisions.md)** — Detailed architectural decisions: MediatR request/handler structure, validation strategy, repository pattern, error handling, soft deletes, ownership model, concurrency control
4. **[Summary of Architectural Decisions](summary-of-architectural-decisions.md)** — Quick reference table of all decisions, approaches, and key benefits
5. **[Implementation Checklist for Phase B](implementation-checklist-for-phase-b.md)** — Database & entity setup, handlers & repositories, testing requirements, logging & audit, Phase C readiness

## How to Use

- **Starting a story?** Read `project-context-analysis.md` to understand constraints and requirements
- **Designing a new component?** Check `core-architectural-decisions.md` for patterns and decision rationale
- **Need a quick reference?** Use `summary-of-architectural-decisions.md`
- **Implementing Phase B features?** Use `implementation-checklist-for-phase-b.md` to ensure consistency
