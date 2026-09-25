---
title: '{title}'
type: 'feature'            # feature | bugfix | refactor | chore
story: ''                  # story key if derived from one, else empty
created: '{YYYY-MM-DD}'
status: 'draft'            # draft | ready-for-dev | in-progress | in-review | built | done | blocked
baseline_revision: ''      # full git SHA, set when implementation starts; never overwrite
review_loop_iteration: 0
context: []                # repo-relative docs the implementer must load (keep short)
---

<frozen-after-approval reason="human-owned intent — do not modify unless the human renegotiates">

## Intent

**Problem:** {one or two sentences: what is broken or missing, and why it matters}

**Approach:** {one or two sentences: the "what", not the "how"}

## Boundaries & Constraints

**Always:** {invariant rules, e.g. the 403-vs-404 contract, UTC timestamps, namespaces match folders}

**Never:** {non-goals and forbidden approaches}

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|---|---|---|---|
| Happy path | | | none |
| {error case} | | | |

</frozen-after-approval>

## Code Map

- `{path}` — {role / what to reuse / what not to touch}

## Tasks & Acceptance

**Execution:**
- [ ] `{path}` — {action} — {rationale}
- [ ] `{test path}` — cover every matrix row

**Acceptance Criteria:**
- Given {precondition}, when {action}, then {result}

## Open Questions

1. {choice} — options: (a) … means …; (b) … means …

## Implementation Notes

<!-- append-only during implementation: decisions, surprises -->

## Plan Change Log

<!-- append-only: review-triggered amendments with KEEP instructions -->

## Review Triage Log

<!-- append-only: one row per finding: verdict, route, evidence -->

## Verification

- `dotnet build backend/JobNecto.slnx --configuration Release --warnaserror` — expected: 0 warnings, 0 errors
- `dotnet test backend/JobNecto.slnx` — expected: all pass
- coverage gate ≥80% per touched file
