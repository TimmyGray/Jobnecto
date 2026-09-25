# Story {E}.{S}: {Title}

Status: ready-for-dev

## Story

As a {role},
I want {capability},
so that {benefit}.

<!-- Track: [FE] | [BE] | [SEED] — Covers: FRx, FRy, NFRz, UX-DRn -->

## Decision Record

<!-- Only when decisions were made while creating the story. Cut otherwise. -->
- {YYYY-MM-DD}: {decision} — {why}

## Acceptance Criteria

1. **{Short behavior name}**
   **Given** {state}
   **When** {one action}
   **Then** {observable result}
   **And** {further observation}

## Tasks / Subtasks

- [ ] Task 1: {goal} (AC: 1, 2)
  - [ ] {file path} — {specific action}
- [ ] Task N: Verification and test coverage (AC: all)
  - [ ] Unit tests: `{path}` — {scenarios, incl. negative paths}
  - [ ] Integration/API tests: `{path}` — {status-code contract scenarios}
  - [ ] Frontend tests (FE): `{path}.spec.ts` — {scenarios}
  - [ ] `dotnet test backend/JobNecto.slnx`
  - [ ] `dotnet build backend/JobNecto.slnx --configuration Release --warnaserror`
  - [ ] Coverage gate ≥80% per file (backend script / `cd frontend && npx ng test --no-watch`)

## Dev Notes

### Technical Requirements
- {Clean Architecture boundary rules relevant here}
- {abstractions to reuse; things not to introduce}

### API / Contract Guardrail
- {endpoint, status codes, ownership contract (403 vs 404) per authorization-contract-matrix}

### File Structure Requirements
- `{path}` (CREATE | UPDATE)

### Testing Requirements
- {existing test patterns to follow, e.g. fresh `JobNectoApiFactory` per test, explicit auth cookie}

### Previous Story Intelligence ({E}.{S-1})
- {what it established / got wrong / review findings that matter}

### Git Intelligence Summary
- {recent commits in this area}

### Latest Stack Information
- {pinned versions from project-context.md}

### References
- [Source: `{path}` - {section}]

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List

## Change Log

- {YYYY-MM-DD}: Story created (ready-for-dev).
