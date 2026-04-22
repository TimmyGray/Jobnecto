---
description: "Use when the agent makes an error and the user corrects it, OR when tests fail after implementation. Defines the mandatory self-improvement protocol: analyze the mistake, log the learning, update instructions if the lesson generalizes. Trigger phrases: 'that's wrong', 'no you should', 'you missed', 'why did you', 'actually do it this way', test failures after implementation, or when the user explicitly provides a correction or overrides the agent's decision."
applyTo: "**"
---

# Agent Self-Improvement Protocol

## When This Applies

This protocol has two triggers:

### Trigger A — User Correction
Apply whenever the user corrects the agent:
- Explicit corrections: "That's wrong", "No, it should be...", "You missed..."
- Intent mismatches: the user overrides a decision or provides an alternative the agent didn't choose
- Repeated corrections of the same type within a session

### Trigger B — Test Failure After Implementation
Apply whenever tests are run after the agent writes or modifies code and one or more tests fail:
- Any `dotnet test`, `pytest`, `jest`, or equivalent run that produces failing tests
- Compile errors triggered by the agent's code changes
- Tests that were passing before the agent's changes and now fail (regressions)

## Required Steps (Both Triggers)

### Step 1 — Acknowledge / Fix First
- **User correction**: acknowledge briefly, without defensiveness. Do not repeat the wrong answer.
- **Test failure**: fix the failing tests immediately before logging. Do not skip the log step once tests pass.

### Step 2 — Analyze the Root Cause
After fixing, reason internally:
- **What** was the wrong output or decision?
- **Why** did it go wrong? (misread intent, wrong assumption, skipped context, wrong test assumption, pattern mismatch, missing edge case, etc.)
- **At what point** in the decision process did the error occur?
- **What signal was available** that should have led to the correct path?

### Step 3 — Log to the Learnings File
Append a structured entry to `_bmad-output/agent-learnings.md` using the format below.
Use `replace_string_in_file` or `multi_replace_string_in_file` to append under the `## Log` section.

```markdown
### [DATE] — [Short title: what went wrong]

**Trigger:** [User correction | Test failure]
**Context:** [Brief description of the task/file being worked on]
**Wrong action:** [What the agent did]
**Root cause:** [Why it happened — be specific about the reasoning failure]
**Correct behavior:** [What should have been done]
**Pattern / trigger:** [Describe the situation signature — when to recognize this in the future]
**Generalize?** [Yes / No — should this become a permanent instruction?]
```

### Step 4 — Reflect and Update Instructions
- If `Generalize? = Yes`: propose updating the relevant `.github/instructions/*.instructions.md` file, or `AGENTS.md`, with the new rule. Create the instruction file if none exists for that concern. A single correction is sufficient to trigger generalization — do not wait for the same mistake twice.
- If `Generalize? = No`: note it as a session lesson only — do not pollute instructions with one-off edge cases.

### Step 5 — Apply the Correction
Proceed immediately with the corrected approach.

## Anti-patterns to Avoid
- Logging vague entries like "I misunderstood" — root cause must be specific
- Skipping the log step because the correction seems minor
- Over-generalizing one-off mistakes into permanent rules
- Updating `AGENTS.md` or instructions files without confirming with the user first
