# Create story: procedure

## 1. Pick the story

- If the user named one (`2.3`, `2-3`, or a title), use it.
- Otherwise take the first `backlog` story in `sprint-status.yaml` whose epic predecessors are `done` or explicitly parallel (check the track notes).
- Confirm in one line: "Creating story 2.3 — View & edit a résumé [FE]."

## 2. Gather context (delegate the heavy reading to subagents; keep only what the dev needs)

1. **Story source**: its section in `_bmad-output/planning-artifacts/epics.md` (user story + Given/When/Then ACs). Copy the ACs faithfully. Sharpen wording only where it is ambiguous, and note each change in the Change Log.
2. **Requirements**: the FR/NFR IDs it covers (`prd-demo-mvp.md` plus `epics/requirements-inventory.md` for NFRs).
3. **Architecture**: read `architecture/index.md`, then only the relevant shards. Extract the binding rules: contract shapes, the 403-vs-404 ownership contract (`authorization-contract-matrix.md`), validation, and soft-delete.
4. **UX** (`[FE]` stories): the relevant parts of `ux-design-specification.md` (tokens, states, accessibility rules, UX-DR IDs).
5. **Codebase reality**: find the files to create or modify, and the existing patterns to copy (a sibling controller/handler/validator/test, or a sibling Angular feature). Record exact paths. Verify each one exists. Don't guess.
6. **Previous story intelligence**: the last done story in this epic (active or `_bmad-output/archive/implementation-artifacts/`). Look at what it established, what it got wrong, and its review findings.
7. **Git intelligence**: `git log --oneline -15` plus any commits that touched the same area.
8. **Agent learnings**: the entries in `_bmad-output/agent-learnings.md` that apply to this kind of work.
9. **Deferred work**: items in `deferred-work.md` this story should resolve, or must not break.

## 3. Write the file

Use `story-template.md`. Requirements:

- **Tasks** are ordered by dependency. Each task names its file path(s) and the ACs it satisfies (`(AC: 1, 3)`). The test tasks name the exact test files and scenarios, including negative paths (401/403/404/400 as the contract requires).
- **Dev Notes** hold *guardrails*, not a tutorial: reuse this, don't touch that, traps (use the `### ⚠️ Trap N — …` headings where there is a real trap), the contract to honor, and stack versions from `project-context.md`.
- **File Structure Requirements** list every expected new/updated file. Namespaces must match folders (AGENTS.md).
- **References** cite each source as `[Source: path - section]`.
- Leave **Dev Agent Record** as stubs. The dev fills it in.
- No placeholders, no TBDs. Anything genuinely unresolved goes in `## Open Questions` and **blocks** `ready-for-dev` until the user answers.

## 4. Gate and update tracker

1. Run the story against `readiness-checklist.md` (story section) and fix what you can.
2. Resolve Open Questions with the user one at a time and record each answer under `## Decision Record`.
3. Set the story file to `Status: ready-for-dev`. In `sprint-status.yaml`, set the story to `ready-for-dev` and the epic to `in-progress` if it was `backlog`. Update `last_updated`.
4. Present the file path and a 3-line summary, then offer the handoff to `jobnecto-dev`.
