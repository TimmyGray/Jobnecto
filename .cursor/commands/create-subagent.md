---
description: Create and run the dedicated Code Reviewer subagent
argument-hint: [base-ref]
---

Create/use the repository subagent profile at `.cursor/subagents/code-reviewer.md` and run a full PR review.

Execution:
1. Resolve base ref from first argument (default: `origin/master`).
2. Run mechanical checks and report generation:
   - `bash scripts/run_code_reviewer.sh <base-ref> /tmp`
3. Launch a separate subagent:
   - `subagent_type`: `generalPurpose`
   - `description`: `Code reviewer`
   - Follow profile: `.cursor/subagents/code-reviewer.md`
4. Ask subagent to return findings sorted by severity with:
   - `severity`
   - `risk_score` (1-10)
   - `impact`
   - `evidence`
   - `recommended_fix`
5. If no blocking findings, require residual risk statement.

Output requirements:
- Include generated report path from `/tmp/code_review_report_*.md`.
- Include commands executed and their outcomes.
