# AGENTS.md

## AI AGENTS specific instructions

### Project overview

JobNecto is a .NET 10 backend API for job vacancy aggregation and matching. Clean Architecture with layers: API, Application, Domain, Infrastructure, Infrastructure.LLM, Infrastructure.JobSources. See `.github/workflows/ci.yml` for CI commands.

### Build, test, lint

- **Solution file:** `backend/JobNecto.slnx` (used by CI and for all dotnet commands)
- **Build:** `dotnet build backend/JobNecto.slnx`
- **Test:** `dotnet test backend/JobNecto.slnx`
- **CI-equivalent lint/build:** `dotnet build backend/JobNecto.slnx --configuration Release --warnaserror`

### Running the API

- Start: `cd backend/src/JobNecto.API && ASPNETCORE_ENVIRONMENT=Development DOTNET_URLS="http://localhost:5000" dotnet run`
- The API serves OpenAPI spec at `GET /openapi/v1.json`.
- No user-facing routes exist yet; 404 on root is expected.

### Key Reference Files — When to Read Them

| File | Read when |
|------|----------|
| `_bmad-output/planning-artifacts/prd.md` | Implementing a feature; clarifying what a feature must do; writing or reviewing stories |
| `_bmad-output/planning-artifacts/architecture/index.md` | Designing new components; choosing patterns, abstractions, or infrastructure; validating Clean Architecture boundaries (see **Architecture shards** below) |
| `_bmad-output/planning-artifacts/epics/requirements-inventory.md` | Checking non-functional requirements (security, performance, pagination, validation rules) before implementation |
| `_bmad-output/agent-learnings.md` | Starting any non-trivial task; before writing tests; when a mistake was made — check if a lesson already covers it |

**Architecture shards:** Technical architecture is split across `_bmad-output/planning-artifacts/architecture/`. **Always read `architecture/index.md` first** — it is the navigation index (table of contents). Use it to decide which topical `.md` files in that same folder you need (for example detailed decisions versus summaries versus implementation checklist). Older docs that cite `architecture.md` refer to this folder. Add or rename shards by updating relevant files and keeping `index.md` in sync when the TOC or layout changes.

### Agent Pushback Rule

- Do **not** agree with every user suggestion. Only agree when the suggestion is technically sound and consistent with the project's architecture, conventions, and requirements.
- If a user suggestion is clearly wrong, introduces a regression, violates Clean Architecture, or contradicts a documented decision — **say so explicitly** before proceeding. Provide a brief, direct reason.
- Do not silently implement something incorrect just to comply. A short correction is always preferable to silent wrong work.
- If the user insists after being informed, acknowledge the override and proceed, but note any risks.

### Custom Agent Skills

- **Function Comments**: Use the `@[/function-comments]` skill from `.agents/skills/function-comments/SKILL.md` to add C# XML docs (`/// <summary>`, `<param>`, `<returns>`) to functions that benefit from documentation. Skip trivial code per the skill.
- **PostgreSQL**: Use the `@[/jobnecto-postgresql]` skill from `.agents/skills/jobnecto-postgresql/SKILL.md` when querying the DB, running migrations, or working with local Postgres databases.
- **Archon (Workflow Orchestration)**: The `@[/jobnecto-archon]` skill provides worktree isolation, MCP context (GitHub + PostgreSQL schema), parallel sub-agents, and repeatable quality gates. Before starting a non-trivial task, consider whether any of these capabilities would reduce risk or add value — if so, use Archon.

### Agent Routing Instructions

When a user request requires specific workflows, code generation, or role-playing, you must determine the intent and call or recommend the appropriate BMad agent skill:

- **Code Generation & Development**: If the user asks to write code, build, fix, tweak, refactor, add or modify any code/component, you must call the `@[/bmad-quick-dev]` skill (or `@[/bmad-agent-dev]` for story execution).
- **Product Management & Requirements**: If the user requests a product manager, PRD creation, or requirements discovery, you must call the `@[/bmad-agent-pm]` skill.
- **Architecture & System Design**: If the user requests an architect, technical design guidance, or a solution design, you must call the `@[/bmad-agent-architect]` skill.
- **Sprint Management & Scrum**: If the user needs sprint planning or sprint status checking (acting as a Scrum Master), you must call `@[/bmad-sprint-planning]` or `@[/bmad-sprint-status]`.
- **UX Design**: For UX patterns and design specifications, call the `@[/bmad-agent-ux-designer]` skill.
- **Testing & QA**: For test architecture, design, and QA guidance, call the `@[/bmad-tea]` skill.
- **Archon**: Before starting a task, assess its complexity and whether Archon's capabilities are relevant. Consider: Does the task span multiple files or require CI validation? Would worktree isolation protect master from in-progress work? Would live DB/schema context improve accuracy? Would parallel sub-agents speed up analysis? If the answer to any of these is yes, prefer `@[/jobnecto-archon]` over direct implementation.

### Namespace conventions

- **Namespaces must match the folder structure.** Every C# file's namespace must reflect its location: start with the project root namespace (e.g. `JobNecto.API`, `JobNecto.Application`, `JobNecto.Domain`, `JobNecto.Infrastructure`) and append each subfolder as a namespace segment.
- **Example:** A file at `backend/src/JobNecto.API/Infrastructure/Cors/CorsServiceExtensions.cs` must declare `namespace JobNecto.API.Infrastructure.Cors;`.
- **All agents must strictly follow this rule** — never use a flat or mismatched namespace regardless of convenience.

### Code documentation requirements

- **Functions & Methods:** All new valuable or non-trivial functions/methods MUST include C# XML documentation comments (`/// <summary>`, `<param>`, `<returns>`). Trivial or boilerplate code can be skipped. See the `@[/function-comments]` skill for detailed guidelines.

### Version Control & Commit Practices

- **Commit Workflow:** Every time the agent makes substantive changes that should be committed, follow this workflow:
  1. **Inspect the diff:** Use `get_changed_files` to review all modified files and their changes.
  2. **Create summary:** Analyze the diff to understand what was changed, why, and the impact.
  3. **Comprehensive commit message:** Write a detailed commit message that includes:
     - Type and scope (feat, fix, docs, refactor, etc.)
     - Clear description of what was changed
     - Bullet points detailing specific changes by file/area
     - Context about why the changes were made
     - Any breaking changes or important notes
- **Commit Message Format:** Use conventional commits format with detailed body explaining the changes comprehensively.

### Security — secrets and credentials

- **NEVER** write secrets, passwords, API keys, or database connection strings (containing credentials) into any documentation, markdown files, architecture docs, stories, or any other text artifacts.
- When documentation needs to reference a connection string or secret, instruct the reader to **see the local config file** instead (e.g. `appsettings.local.json`, `.env.local`). Example: _"Set the connection string in `appsettings.local.json` under `ConnectionStrings:Default`."_
- This rule applies to all agent outputs: PRDs, architecture docs, stories, code comments, README files, and any generated content.

### Gotchas

- The root `.sln` (`Jobnecto.sln`) uses Windows-style backslash paths and does not include the test project. Always use `backend/JobNecto.slnx` for builds and tests.
- Docker files (`docker/Dockerfile`, `docker/docker-compose.yml`) are empty placeholders.
- Redis and Quartz are referenced in `Infrastructure.csproj` but have no implementation yet.
- **CORS config key:** `Cors:AllowedOrigins` (array). Development defaults (`http://localhost:5173`, `https://localhost:5173`) are in `appsettings.Development.json`. In Production, override with env vars using double-underscore notation: `Cors__AllowedOrigins__0=https://app.example.com`. Policy name: `"Frontend"`.

## Mandatory comprehensive PR review workflow

Run this workflow every time a PR is created or updated.

### Trigger

- PR opened, synchronized, rebased, or receives follow-up commits.
- Any request to "review PR", "review changes", or "code review".

### Required execution model

- Instead of using a generic subagent, you must invoke the BMad Code Review skill `@[/bmad-code-review]` to run adversarial code review.
- Do not skip this review even when implementation appears small.

### Review scope (must include all)

1. **Correctness of new implementation**
   - Validate behavior against changed requirements and code intent.
2. **Regression safety**
   - Check whether existing functionality can break due to changed contracts, side effects, or shared components.
   - Verify project-level health (build/test stability for the solution).
3. **Best practices + code quality**
   - Clean Architecture boundaries, readability, naming, null/error handling, security, and maintainability.
4. **Optimization opportunities**
   - Identify unnecessary complexity, possible performance waste, and simplification opportunities.
5. **Test coverage**
   - Confirm tests cover the new behavior and important edge cases.
   - If coverage is missing, propose concrete tests.
6. **Test execution**
   - Run relevant targeted tests first, then run:
     - `dotnet test backend/JobNecto.slnx`
   - For CI-parity confidence on risky changes, also run:
     - `dotnet build backend/JobNecto.slnx --configuration Release --warnaserror`
     - `dotnet test backend/JobNecto.slnx --configuration Release --no-build --warnaserror`

### Required review output format

- Prioritized findings ordered by severity.
- For each finding include:
  - `severity`: `critical | high | medium | low`
  - `risk_score`: integer `1-10` (10 = highest risk)
  - `impact`: what can break and who is affected
  - `evidence`: file paths, symbols, commands, or test outputs
  - `recommended_fix`: concrete change suggestion
- Explicitly state when no findings are detected.

## Post-Merge / Post-Push Documentation Updates

Run this workflow after a PR is merged or when committing directly to the master branch.

### Trigger

- PR successfully merged into master.
- Commit(s) pushed directly to master branch (when master is the active branch).

### Important Documents to Update

After a feature is implemented and merged, the agent **MUST** update the following documents:

1. **`JOBNECTO_BACKEND_ROADMAP.md`** — Update completed features, mark as done, adjust timeline for remaining items.
2. **`_bmad-output/planning-artifacts/prd.md`** — Reflect completed features, update feature status, adjust scope if needed.
3. **`_bmad-output/planning-artifacts/architecture/`** — Update if implementation changed from design: edit the relevant **shard(s)** under that folder (for example decisions, summaries, implementation status); adjust **`architecture/index.md`** if the TOC or shard layout changes so agents can still navigate the split document.
4. **`_bmad-output/planning-artifacts/epics/requirements-inventory.md`** — Update if NFRs changed or new constraints were introduced during implementation.
5. **`_bmad-output/implementation-artifacts/sprint-status.yaml`** — Mark completed stories/epics as done, update sprint metrics, remove merged items.
6. **`README.md`** — Update feature list, API capabilities, or usage examples if the new feature is user-facing.
7. **Project context files** (`_bmad-output/project-context.md`, etc.) — Update project state, completed features, and current status.

### Update Procedure

1. **Verify the merge/push** — Confirm that the feature has been successfully merged into master or that commits are on master.
2. **Identify affected documents** — Review which documents are impacted by the implemented feature.
3. **Update each document** — Make changes to reflect:
   - Completed features marked as done
   - Updated timelines and roadmap
   - Changed architecture (if applicable)
   - Updated sprint/project status
   - New capabilities or features listed
4. **Verify consistency** — Ensure all documentation is consistent across files (e.g., same feature status in roadmap and sprint status).
5. **Commit documentation updates** — If changes are made to documents, commit them with a clear message like `"docs: update after feature [name] merged to master"`.

### Required Verification

- [ ] All impacted documents have been reviewed.
- [ ] Document updates reflect the implemented feature accurately.
- [ ] Timeline and roadmap are synchronized.
- [ ] No outdated information remains from previous versions.
