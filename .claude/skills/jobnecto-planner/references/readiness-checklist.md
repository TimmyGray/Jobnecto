# Implementation readiness checklist

## Story level (ready-for-dev standard)

- [ ] **Actionable**: every task names a file path and a specific action.
- [ ] **Logical**: tasks are ordered by dependency.
- [ ] **Testable**: every AC is Given/When/Then with an observable result, and negative paths are included.
- [ ] **Traceable**: the story names the FR/NFR/UX-DR IDs it covers, and every AC maps to at least one task.
- [ ] **Complete**: there are no placeholders, TBDs or open questions.
- [ ] **Grounded**: every referenced existing file or symbol was verified in the repo.
- [ ] **Contract-safe**: status codes follow `architecture/authorization-contract-matrix.md` (403 vs 404), and new endpoints list their `[ProducesResponseType]` set.
- [ ] **Single goal**: the story can be reviewed and merged as one PR. Otherwise propose a split.
- [ ] **Guardrails present**: the relevant `agent-learnings.md` entries and previous-story findings are carried into Dev Notes.
- [ ] **Coverage-aware**: test tasks will keep every touched file at ≥80% line coverage.

## Epic level

- [ ] Every FR assigned to the epic in the FR Coverage Map is covered by at least one story.
- [ ] Story order respects dependencies. Parallel FE/BE stories have a frozen, pinned contract.
- [ ] Architecture decisions the epic needs exist (or an architect task is scheduled first).
- [ ] UX spec covers every `[FE]` story's screens and states (loading / empty / error / not-found).
- [ ] No story depends on a later story.
- [ ] Deferred work relevant to the epic has been triaged (pick up, keep deferred, or drop).
- [ ] Risks that could block the magic spine are named, with a mitigation.
