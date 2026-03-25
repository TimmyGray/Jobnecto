---
description: Comprehensive PR review via dedicated Code reviewer subagent
argument-hint: [base-ref]
---

Run a comprehensive PR review for the current branch.

Requirements:
- Use the skill: `.cursor/skills/code-reviewer/SKILL.md`
- Execute review in a separate subagent with:
  - `subagent_type`: `generalPurpose`
  - `description`: `Code reviewer`
- Do not skip regression and test coverage analysis.

Execution steps:
1. Resolve base ref from first argument; default to `origin/master`.
2. Run mechanical checks and generate report:
   - `bash scripts/run_code_reviewer.sh <base-ref> /tmp`
3. Launch the `Code reviewer` subagent to perform deep analysis over:
   - changed files and impacted dependencies
   - correctness, regressions, best practices, optimization opportunities
   - test coverage gaps and concrete test additions
4. Return findings ordered by severity with required fields:
   - `severity`, `risk_score (1-10)`, `impact`, `evidence`, `recommended_fix`
5. If no blocking issues are found, explicitly state residual risk with rationale.

Output format:
- Start with findings (highest severity first).
- Include command outputs used as evidence.
- Include the generated report path from `/tmp/code_review_report_*.md`.
