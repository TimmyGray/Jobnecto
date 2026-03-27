---
name: document-functions
description: Add C# XML documentation comments to members that need them; supports scoped paths/symbols or full-solution pass; skips trivial boilerplate.
---

# Document functions (XML doc comments)

Use when asked to add documentation comments to functions/methods, or when running the **document-functions** Cursor command.

## Goal

Add **`///` XML documentation** so that:
- Developers and agents can understand **intent**, **inputs**, and **outputs** without reading every line.
- Public contracts and non-obvious logic are described consistently.

## Language and format

- **C# only** in this repo: use standard XML tags.
- Every documented member should have:
  - `<summary>` — what it does and when to use it (not a restatement of the name only).
  - `<param name="...">` — one per parameter, including `cancellationToken` when present.
  - `<returns>` — for non-void returns; for `Task` / `ValueTask` describe completion semantics; for `Task<T>` / `ValueTask<T>` describe the meaning of the result.
- Use `<remarks>`, `<exception cref="...">`, or `<see cref="..."/>` when they add clarity (optional).
- Prefer **accurate** docs over verbose docs; one or two sentences often suffice.

## What to document (“required” members)

Treat these as **normally requiring** documentation when they lack adequate XML docs:

- **Public** surface: API endpoints, public types and public members intended for external or cross-layer use.
- **Application / domain rules**: handlers, services, validators, factories, domain methods with behavior beyond data holding.
- **Non-trivial logic**: branching, retries, compensation, invariants, algorithms, or coordination across dependencies.
- **Non-obvious** helpers: behavior that is not clear from the name alone.

## What to skip

Do **not** add noise comments for:

- **Thin persistence**: repository methods that only forward to EF/`DbContext` with no extra rules.
- **Trivial members**: one-line wrappers, obvious auto-properties, simple forwarding calls where the name is sufficient.
- **Generated or tooling-owned** files (if any appear in scope).
- **Ordinary test methods** (`[Fact]`, `[Theory]`, etc.) unless the task explicitly includes tests or a method encodes unusual shared setup worth explaining.

If unsure: **skip** rather than duplicate the obvious; err on documenting **public and behavior-heavy** code first.

## Scope: arguments vs full project

### With arguments

- **Paths / directories / globs**: restrict to those locations (for example `backend/src/JobNecto.Application`, or `backend/src/**/*.cs`).
- **Symbol names**: if the user names specific types or methods, only those members (and their overload sets if relevant).

### Without arguments (full pass)

- Walk **source projects** under `backend/src` **file by file**, then **member by member**.
- Exclude `bin`, `obj`, and generated artifacts from traversal.
- For large solutions, process **project-by-project** or **folder-by-folder** in separate steps so nothing in scope is missed.

## Workflow

1. Determine scope from user input (see above).
2. For each file in scope, consider **types and members in order** (nested types after outer type).
3. For each candidate member, apply **What to document** vs **What to skip**; add or refine XML comments.
4. Run `dotnet build backend/JobNecto.slnx` after a batch of changes; resolve compile or analyzer issues tied to documentation.

## Consistency

- Match existing file and project conventions (language style, `cref` usage).
- Do not remove existing comments to “rewrite” unless correcting inaccuracies or completing missing `<param>`/`<returns>`.
