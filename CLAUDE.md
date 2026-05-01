# CLAUDE.md

This file contains instructions for working with Claude AI agents on the Jobnecto project.

## Main Instructions

**All Claude agents working on this project must read and follow the instructions in [`AGENTS.md`](AGENTS.md) as their primary reference.**

`AGENTS.md` contains:
- Project overview and setup commands
- Build, test, and CI commands
- Key reference files and when to read them
- Agent pushback rules and expectations
- Custom agent skills available in this project
- Agent routing instructions (when to call which skill)
- Namespace and code documentation conventions
- Version control and commit practices
- Security rules for secrets and credentials
- Known gotchas and workarounds
- Mandatory PR review workflow
- Post-merge documentation update procedures

## Quick Start

1. Read `AGENTS.md` completely before starting any work.
2. When implementing features, follow the agent routing instructions in `AGENTS.md`.
3. For code changes, use the appropriate BMad agent skill (dev, architect, PM, etc.).
4. Run the mandatory PR review workflow before opening/updating any pull request.
5. After merging to master, follow the post-merge documentation update workflow.

## Project Structure

- **Backend solution:** `backend/JobNecto.slnx` (use this, not `Jobnecto.sln`)
- **API:** `backend/src/JobNecto.API`
- **Application:** `backend/src/JobNecto.Application`
- **Domain:** `backend/src/JobNecto.Domain`
- **Infrastructure:** `backend/src/JobNecto.Infrastructure`
- **Tests:** `backend/tests/JobNecto.Tests`
- **Planning artifacts:** `_bmad-output/planning-artifacts/`
- **Implementation artifacts:** `_bmad-output/implementation-artifacts/`

## Key Commands

```bash
# Build
dotnet build backend/JobNecto.slnx

# Test
dotnet test backend/JobNecto.slnx

# Run API (Development)
cd backend/src/JobNecto.API && \
ASPNETCORE_ENVIRONMENT=Development \
DOTNET_URLS="http://localhost:5000" \
dotnet run

# CI-equivalent (Release + strict warnings)
dotnet build backend/JobNecto.slnx --configuration Release --warnaserror
dotnet test backend/JobNecto.slnx --configuration Release --no-build --warnaserror
```

## Important Files

| File | Purpose |
|------|---------|
| `AGENTS.md` | **[READ THIS FIRST]** Comprehensive agent instructions and workflows |
| `_bmad-output/planning-artifacts/prd.md` | Product requirements and feature specifications |
| `_bmad-output/planning-artifacts/architecture.md` | System design and architectural decisions |
| `_bmad-output/project-context.md` | Project conventions, tech stack, and implementation rules |
| `_bmad-output/agent-learnings.md` | Documented lessons and mistakes to avoid |

---

**Last updated:** 2026-05-01
