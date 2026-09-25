# Self-review: parallel lenses, then triage

## 1. Stage the diff

Write the diff since the baseline, untracked files included, to a unique temp file:

```bash
f="${TMPDIR:-/tmp}/review-{slug}.diff"
git diff {baseline_revision} > "$f"
git ls-files --others --exclude-standard | while read -r u; do git diff --no-index /dev/null "$u" >> "$f"; done   # untracked files; index untouched
```

Read it yourself. Judge the diff, not the implementer's summary.

## 2. Launch the lenses in parallel

Spawn each lens as a subagent **in the same turn** and wait for all of them before reading any result. Give each one only: its lens instructions (below), the diff file path, and the repo root. Don't paste in the diff text or your own opinion.

| Lens | Gets | Instructions |
|---|---|---|
| **Adversarial** | diff path | "Assume this diff has at least three real defects. Find them. Check correctness, error paths, concurrency, null handling, security (auth, ownership, injection, secret leakage), and Clean Architecture boundary violations. For each: file:line, what goes wrong, and a concrete input or state that triggers it." |
| **Edge-case hunter** | diff path, then the plan/story path **only after** its own tracing | "Walk every branch and boundary in the changed code: empty, null, max length, duplicates, soft-deleted, cross-user, concurrent update, cancellation, time zones. Report only unhandled cases, with the triggering input." |
| **Verification gap** | diff path + test files | "For each behavior the diff adds or changes, find the test that proves it. Report behaviors with no test, tests that don't actually assert the behavior, and tests that wouldn't run (filtered, skipped, or unregistered). Cite the test file and name." |
| **Acceptance** (quick) | diff path + plan/story path | "Check each AC and matrix row against the diff. Report any that are unmet or only partly met." |

For a small or low-risk change, run just **Acceptance + Adversarial**, and say that you did.

## 3. Triage — you judge, not the reviewers

Ignore any severity a reviewer assigned. For **each** finding, verify the claim at the cited location (follow callers and upstream guards), then give exactly one verdict:

- `high`: intolerable harm to users or developers
- `medium`: real but tolerable
- `low`: cosmetic or negligible
- `false`: you checked, and it doesn't happen. Write what disproves it.
- `maybe-false`: you can't tell. Write what would settle it.

Drop `false` findings. Drop `low` findings whose fix adds complexity (guards, branches, parameters) and that users would rarely hit.

Route each surviving finding (grouped by shared root cause):

| Route | When | Action |
|---|---|---|
| **patch** | Caused by this change, with a trivial fix that adds no public surface | Fix it now (smallest change), then re-run the affected tests. |
| **defer** | Pre-existing and not caused by this change; or `maybe-false` that would be medium/high if true | Append to `_bmad-output/implementation-artifacts/deferred-work.md` under a `## Deferred from: code review of {story/plan} ({date})` heading. |
| **bad_plan** | Caused by this change, and the plan should have prevented it | Note KEEP instructions (what worked), amend the non-frozen plan sections, log it in the Plan Change Log, and re-implement the affected part. |
| **intent_gap** | The frozen intent is incomplete | Stop and ask the human. Don't guess. |

Each loopback increments `review_loop_iteration`. Above 5, stop and escalate to the human.

## 4. Record

- **Quick mode**: one row per finding in the plan's `## Review Triage Log`: `verdict | route | evidence`.
- **Story mode**: `### Review Findings` in the story, e.g. `- [x] [Review][Patch] <what> \`path\` — fixed`, `[Defer]`, `[Dismiss]`.
