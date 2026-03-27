---
description: Add XML doc comments to functions that need them
argument-hint: [file-or-folder paths...]
---

Add C# XML documentation comments (`/// <summary>`, `/// <param>`, `/// <returns>`) to functions and methods that benefit from documentation. Skip trivial, self-explanatory, or boilerplate code.

Requirements:
- Use the skill: `.cursor/skills/function-comments/SKILL.md`
- Follow the comment/skip criteria defined in the skill.
- Preserve existing XML doc comments unless they are clearly wrong.

Execution steps:

1. **If arguments are provided** (file paths, folder paths, or function names):
   - Scope the work to only the specified targets.
   - Read each file, identify qualifying functions, add XML doc comments.

2. **If no arguments are provided** (full project scan):
   - Recursively scan `backend/src/` one project at a time.
   - Process in order: Application → Domain → Infrastructure → Infrastructure.LLM → Infrastructure.JobSources → API.
   - Within each project, process one file at a time.
   - Add XML doc comments only to functions matching the "MUST comment" criteria from the skill.

3. After all changes, verify:
   - `dotnet build backend/JobNecto.slnx`
   - `dotnet test backend/JobNecto.slnx`

Output:
- List of files modified and functions commented.
- Build and test results confirming no regressions.
