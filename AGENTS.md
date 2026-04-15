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


### Custom Agent Skills

- **Function Comments**: Use the `@[/function-comments]` skill from `.agents/skills/function-comments/SKILL.md` to add C# XML docs (`/// <summary>`, `<param>`, `<returns>`) to functions that benefit from documentation. Skip trivial code per the skill.
- **PostgreSQL**: Use the `@[/jobnecto-postgresql]` skill from `.agents/skills/jobnecto-postgresql/SKILL.md` when querying the DB, running migrations, or working with local Postgres databases.

### Agent Routing Instructions

When a user request requires specific workflows, code generation, or role-playing, you must determine the intent and call or recommend the appropriate BMad agent skill:
- **Code Generation & Development**: If the user asks to write code, build, fix, tweak, refactor, add or modify any code/component, you must call the `@[/bmad-quick-dev]` skill (or `@[/bmad-agent-dev]` for story execution).
- **Product Management & Requirements**: If the user requests a product manager, PRD creation, or requirements discovery, you must call the `@[/bmad-agent-pm]` skill.
- **Architecture & System Design**: If the user requests an architect, technical design guidance, or a solution design, you must call the `@[/bmad-agent-architect]` skill.
- **Sprint Management & Scrum**: If the user needs sprint planning or sprint status checking (acting as a Scrum Master), you must call `@[/bmad-sprint-planning]` or `@[/bmad-sprint-status]`.
- **UX Design**: For UX patterns and design specifications, call the `@[/bmad-agent-ux-designer]` skill.
- **Testing & QA**: For test architecture, design, and QA guidance, call the `@[/bmad-tea]` skill.

### Code documentation requirements

- **Functions & Methods:** All new valuable or non-trivial functions/methods MUST include C# XML documentation comments (`/// <summary>`, `<param>`, `<returns>`). Trivial or boilerplate code can be skipped. See the `@[/function-comments]` skill for detailed guidelines.

### Gotchas


- The root `.sln` (`Jobnecto.sln`) uses Windows-style backslash paths and does not include the test project. Always use `backend/JobNecto.slnx` for builds and tests.
- Docker files (`docker/Dockerfile`, `docker/docker-compose.yml`) are empty placeholders.
- Redis and Quartz are referenced in `Infrastructure.csproj` but have no implementation yet.

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
