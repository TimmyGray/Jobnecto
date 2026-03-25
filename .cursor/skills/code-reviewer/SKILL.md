---
name: code-reviewer
description: Run comprehensive PR review using a dedicated Code reviewer subagent. Use when PR is created/updated or when asked to review implementation quality, regression safety, tests, and best practices with risk scoring.
---

# Code Reviewer (Comprehensive PR Review)

Use this skill every time a PR is created/updated or a user asks for code review.

## Goal

Perform a deep review that covers:

1. Correctness of the new implementation.
2. Regression risk for existing functionality and project-wide health.
3. Best practices and architecture quality.
4. Optimization opportunities.
5. Test coverage of the new behavior.
6. Actual test execution.

## Required execution model

- Run a separate subagent with:
  - `subagent_type`: `generalPurpose`
  - `description`: `Code reviewer`
- The subagent must not only read diffs; it must execute relevant checks and tests.

## Input checklist (collect before review)

From repo root:

```bash
git branch --show-current
git status --short
git diff --name-only origin/master...HEAD
```

If base branch is not `master`, use the actual target branch in diff commands.

## Mechanical checks and tests

Run these in order:

1. Fast changed-area checks:
   - `dotnet build backend/JobNecto.slnx`
   - Targeted test filter(s) related to modified code when possible.
2. Full regression checks:
   - `dotnet test backend/JobNecto.slnx`
3. CI-parity confidence for medium/high risk changes:
   - `dotnet build backend/JobNecto.slnx --configuration Release --warnaserror`
   - `dotnet test backend/JobNecto.slnx --configuration Release --no-build --warnaserror`

## Review dimensions (must cover all)

### 1) Correctness

- Does implementation match requirement and intent?
- Are edge cases handled (nulls, invalid inputs, error paths)?

### 2) Regression safety

- Could changed contracts break callers?
- Any side effects in shared components?
- Any schema/config/runtime assumptions that can break existing flows?

### 3) Best practices

- Clean Architecture boundaries respected?
- Naming/readability/maintainability acceptable?
- Error handling and logging appropriate?
- Security and secrets handling safe?

### 4) Optimization opportunities

- Remove needless complexity.
- Identify expensive operations and redundant allocations/queries.
- Suggest simpler alternatives where practical.

### 5) Test coverage adequacy

- Are new/changed behaviors covered?
- Are critical edge cases covered?
- If missing, propose exact tests (test name + scenario).

## Required output format

Order findings by severity:

- `critical`
- `high`
- `medium`
- `low`

For each finding include:

- `severity`: critical|high|medium|low
- `risk_score`: 1-10 (10 highest)
- `impact`: what can break and who is affected
- `evidence`: files/symbols/tests/logs
- `recommended_fix`: concrete and actionable

If there are no findings, explicitly output:

- `No blocking findings. Residual risk: <low/medium/high> with rationale.`

## Suggested subagent prompt template

Use this prompt when launching the subagent:

`Perform a comprehensive PR review as Code reviewer. Analyze changed files and impacted dependencies for correctness, regression risk, best practices, optimization opportunities, and test coverage gaps. Run build/tests as needed (targeted first, then full). Return prioritized findings with severity and risk_score(1-10), include impact, evidence, and recommended_fix. Explicitly state if no blocking findings remain and note residual risks.`
