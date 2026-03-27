---
description: Add structured comments for non-trivial functions
argument-hint: [files=<glob|csv>] [functions=<name|regex|csv>]
---

Write function comments in the requested scope.

Goal:
- Improve maintainability for developers and agents by adding concise, useful function-level comments.
- Comments must include:
  - description of behavior/intent
  - arguments
  - return value (or side-effect note for `void`/no-return functions)

Argument handling:
1. Parse optional command arguments:
   - `files=<glob|csv>`: restrict inspection to matching files.
   - `functions=<name|regex|csv>`: restrict inspection to matching function names/signatures.
2. If no arguments are provided:
   - recursively inspect the entire project file-by-file, function-by-function.

Comment rules:
- Use language-native doc style:
  - C#: XML docs (`/// <summary>`, `/// <param ...>`, `/// <returns>`)
  - TS/JS: JSDoc
  - Python: docstrings
- Preserve existing valid comments; improve them only when clearly incomplete.
- Keep comments short, accurate, and behavior-focused.
- Do not copy implementation details line-by-line.

Skip rules (important):
- Do NOT add comments for trivial functions, such as:
  - one-line obvious getters/setters
  - simple pass-through wrappers
  - boilerplate constructors with no logic
  - basic CRUD/repository plumbing where the name is already self-explanatory
- Prefer commenting functions with meaningful branching, business rules, transformations, validation, or non-obvious side effects.

Execution workflow:
1. Build candidate function list from selected scope.
2. Skip functions that already have good comments or match skip rules.
3. Add/upgrade comments for required functions only.
4. Keep formatting and conventions consistent with the surrounding file.
5. Summarize:
   - files inspected
   - functions commented
   - functions skipped with reason categories
