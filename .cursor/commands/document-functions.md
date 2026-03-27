---
description: Add XML documentation comments to functions that benefit from them (scoped or full solution)
argument-hint: [paths-or-globs...] [symbol-names...]
---

Add or improve **XML documentation comments** (`///`) on C# members where they help developers and agents understand behavior.

Requirements:
- Use the skill: `.cursor/skills/document-functions/SKILL.md`
- Follow the skill’s rules for **which members to document** and **which to skip**.

Execution steps:
1. Parse optional arguments:
   - If one or more arguments are provided, treat them as **file paths**, **directory paths**, or **glob patterns** (for example `backend/src/JobNecto.Application/**/*.cs`), and/or **symbol names** to limit scope.
   - If no arguments are provided, apply the skill to the **entire solution source** under `backend/src` (and tests under `backend/tests` only when they contain non-trivial helpers worth documenting—default is to skip ordinary test methods per the skill).
2. Work **systematically**: one file at a time, then member by member; do not skip files in scope unless the skill says to exclude them.
3. For each member that **requires** documentation per the skill, ensure the comment includes:
   - **Description** (`<summary>`)
   - **Parameters** (`<param name="...">`) for every parameter
   - **Return value** (`<returns>`) when the member returns a value, or appropriate wording for `Task` / `Task<T>` / `void`
4. After edits, run `dotnet build backend/JobNecto.slnx` and fix any issues introduced.

Output format:
- Brief summary of scope (paths/globs/symbols or “full solution”).
- List of files touched and approximate count of members documented.
- Note any directories or member kinds **skipped** by policy (for example thin repositories).
