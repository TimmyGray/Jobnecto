# Code / PR review workflow

This implements AGENTS.md → *Mandatory comprehensive PR review workflow*.

## 1. Establish the target

- A PR (a number or a branch): diff against its base (`git fetch origin <base>` then `git diff origin/<base>...HEAD`).
- "Review changes": uncommitted plus unpushed changes against the upstream branch.
- A story key: the story file's File List and baseline, when recorded.

Write the diff to a temp file. Also note the story or plan that motivated it (the intent) if one exists.

## 2. Parallel lenses (subagents, all launched in one turn, then wait)

Each lens gets **only** its instructions, the diff path, and the repo root.

1. **Correctness** — "Does the code do what it intends? Trace each changed function's inputs to its outputs. Report wrong results, unhandled errors, null/empty handling, and wrong status codes versus `_bmad-output/planning-artifacts/architecture/authorization-contract-matrix.md`."
2. **Regression** — "What existing behavior can this break? Follow the callers of every changed public member, shared component, DTO, EF model, route, or config key. Report the concrete break, with the caller."
3. **Architecture & quality** — "Check Clean Architecture boundaries (Domain has no dependencies; Application doesn't reference Infrastructure), namespaces matching folders, XML docs on non-trivial public methods, naming, security (auth, ownership, injection, secrets in code or logs), and CancellationToken forwarding."
4. **Edge cases & optimization** — "Find unhandled boundaries (empty, max length, duplicates, soft-deleted, cross-user, concurrency, time zones) and wasteful work (N+1 queries, loading entities just to check ownership, needless allocations or complexity)."
5. **Test coverage** — "For each changed behavior, find the test that proves it and confirm it asserts that behavior. Report untested behaviors and propose concrete tests (file + scenario)."

Scale it down for a tiny diff: lenses 1, 3, and 5 are the minimum. Say which lenses ran.

## 3. Execute tests

Run the targeted tests for the changed area first, then:

```bash
dotnet test backend/JobNecto.slnx
# risky changes, CI parity:
dotnet build backend/JobNecto.slnx --configuration Release --warnaserror
dotnet test backend/JobNecto.slnx --configuration Release --no-build --warnaserror
# frontend touched:
cd frontend && npx ng test --no-watch
```

Record the real counts. Anything you could not run is stated explicitly.

## 4. Verify and triage

Discard the reviewers' severities. For each finding, open the cited code, follow its callers and guards, and decide whether it is **real**, **false** (write what disproves it), or **unverified** (write what would settle it). Merge findings that share a root cause. Drop the false ones. Keep the unverified ones only if they would be high or critical if true, and label them as unverified.

## 5. Output (required format, ordered by severity)

```markdown
## Review: {target}

Lenses run: correctness, regression, architecture, edge-cases, coverage
Tests: `dotnet test` → Passed: N, Failed: 0 · Release build: 0 warnings · Frontend: Passed: M

### 1. {title}
- **severity:** critical | high | medium | low
- **risk_score:** 1–10
- **impact:** {what breaks, and who is affected}
- **evidence:** `{path}:{line}`, {symbol / command output}
- **recommended_fix:** {concrete change}
```

If nothing survives verification, say so explicitly: **"No findings detected."** Also list the lenses and tests that ran.

For a story review, also append a `### Review Findings` section to the story file using `[Patch]`, `[Defer]`, and `[Dismiss]` tags, and add the `[Defer]` items to `_bmad-output/implementation-artifacts/deferred-work.md`.
