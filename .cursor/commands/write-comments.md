---
description: Add XML doc comments for important functions only
argument-hint: [paths and/or function filters]
---

Write missing function comments in the requested scope.

Goal:
- Improve developer and agent understanding of non-trivial logic.
- Add comments only where they add value.
- Do not blanket-comment trivial methods or persistence boilerplate.

Scope resolution:
1. Parse arguments as optional targets.
2. If arguments are provided, inspect only the requested files, folders, or function filters.
3. If no arguments are provided, recursively inspect C# source files under `backend/src` file by file and function by function.
4. Unless explicitly targeted by arguments, skip tests, generated files, migrations, build artifacts, and non-C# files.

Accepted target styles:
- File path: `backend/src/JobNecto.Domain/Entities/Resume.cs`
- Directory path: `backend/src/JobNecto.Application`
- File + function filter: `backend/src/JobNecto.Domain/Entities/Resume.cs::BuildPrompt`
- Symbol/function name filter when unambiguous: `BuildPrompt`

Commenting rules:
- Prefer C# XML documentation comments for functions:
  - `/// <summary>`
  - `/// <param name="...">`
  - `/// <returns>`
- Add comments for functions that contain meaningful business logic, orchestration, validation, calculations, multi-step transformations, non-obvious side effects, or important domain decisions.
- Comment public and protected methods first; include private methods only when their intent is not obvious from the code.
- Keep comments concise, factual, and implementation-aware.
- Describe:
  - what the function does
  - each important argument
  - the return value or observable effect

Skip these unless the code is unusually complex:
- trivial getters/setters or obvious one-liners
- simple CRUD/repository/database wrapper methods
- EF Core configurations and mappings
- boilerplate constructors or dependency injection wiring
- obvious DTO/entity mapping helpers
- generated code, migrations, and test boilerplate

Execution workflow:
1. Identify candidate functions in the requested scope.
2. Skip trivial or low-value candidates.
3. Add XML docs only to the remaining functions.
4. Preserve behavior; do not refactor code unless required to make comments accurate.
5. Summarize which files and functions were documented and which categories were intentionally skipped.

Quality bar:
- Do not invent behavior not present in code.
- Do not restate the method name with no extra meaning.
- If the return type is `Task`, document the awaited result/effect, not just that it is asynchronous.
- If a function mutates state or calls external systems, mention that clearly.
