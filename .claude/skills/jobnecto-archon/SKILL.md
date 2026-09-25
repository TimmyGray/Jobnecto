---
name: jobnecto-archon
description: Orchestrate JobNecto workflows using Archon (workflow runner, MCP integrator, multi-agent coordinator). Use when implementing features, reviewing PRs, fixing issues with schema context, or running parallel agents. Archon isolates work in git worktrees and provides repeatable quality gates.
---

# JobNecto + Archon Workflow Orchestration

This skill guides you through running Archon workflows on JobNecto. Archon is a workflow orchestrator that isolates changes in git worktrees, coordinates MCP services, and parallelizes specialized agents.

## Quick Start

### 1. Verify Archon is installed

```powershell
archon version
```

If not installed:
```powershell
irm https://archon.diy/install.ps1 | iex
```

### 2. List available workflows

```powershell
archon workflow list --cwd e:/apps/Jobnecto
```

### 3. Run a built-in workflow

Example: Smart PR review on current branch

```powershell
archon workflow run archon-smart-pr-review --cwd e:/apps/Jobnecto --branch review/auto "Review current changes"
```

## Common JobNecto Workflows

### Pattern 1: Feature Implementation with Isolated Branch

**Use when:** Building a new feature or story (e.g., Story 3.2: List Cover Letter Templates)

```powershell
archon workflow run archon-feature-development --cwd e:/apps/Jobnecto --branch feat/story-3-2 "Implement Story 3.2 - List Cover Letter Templates"
```

This workflow:
- Creates isolated worktree from `master`
- Prompts for implementation steps
- Validates build and tests
- Summarizes changes

### Pattern 2: MCP-Assisted Issue Fix (DB/Schema Context)

**Use when:** Fixing a bug that touches database schema, migrations, or queries.

**Prerequisite:** Create `.archon/mcp/all-services.json` (see section 4 below)

```powershell
archon workflow run jobnecto-mcp-issue-fix --cwd e:/apps/Jobnecto --branch fix/issue-123 "Fix issue #123 with schema context"
```

This workflow:
- Gathers GitHub issue context + PostgreSQL schema in parallel (via MCP)
- Implements fix with live database awareness
- Validates against schema constraints
- Returns migration + test + summary

### Pattern 3: Parallel Agentic Code Review

**Use when:** Need architecture + test coverage + API contract checks in parallel.

**Prerequisite:** Create `.archon/workflows/jobnecto-agentic-review.yaml` (see section 5 below)

```powershell
archon workflow run jobnecto-agentic-review --cwd e:/apps/Jobnecto --branch review/parallel "Run parallel architecture/test/API review"
```

This workflow:
- Spawns 3 specialized sub-agents in parallel
- boundary-checker: validates Clean Architecture
- test-checker: flags missing tests
- api-checker: validates endpoint contracts
- Merges and prioritizes findings

## Setup: Project Configuration

### Step 1: Create `.archon/config.yaml`

At `e:/apps/Jobnecto/.archon/config.yaml`:

```yaml
assistant: claude

assistants:
  claude:
    model: sonnet
    settingSources:
      - project

worktree:
  baseBranch: master
  copyFiles:
    - backend/src/JobNecto.API/appsettings.Local.json
    - .vscode/

docs:
  path: docs

defaults:
  loadDefaultCommands: true
  loadDefaultWorkflows: true
```

### Step 2: Create MCP Services (Optional, but Recommended)

Create `.archon/mcp/all-services.json`:

```json
{
  "github": {
    "command": "npx",
    "args": ["-y", "@modelcontextprotocol/server-github"],
    "env": {
      "GITHUB_PERSONAL_ACCESS_TOKEN": "$GITHUB_TOKEN"
    }
  },
  "postgres": {
    "command": "npx",
    "args": ["-y", "@modelcontextprotocol/server-postgres"],
    "env": {
      "DATABASE_URL": "$DATABASE_URL"
    }
  }
}
```

**Important:** Set environment variables in `.archon/.env` (not in workflow YAML):

```bash
GITHUB_TOKEN=ghp_xxxxx
DATABASE_URL=Host=localhost;Port=5432;Database=JobNecto;Username=admin;Password=admin
```

### Step 3: Create Custom Workflows (Optional)

#### Workflow: MCP-Assisted Issue Fix

Create `.archon/workflows/jobnecto-mcp-issue-fix.yaml`:

```yaml
name: jobnecto-mcp-issue-fix
description: Investigate GitHub issue with live DB schema, then implement and validate.

provider: claude
model: sonnet

nodes:
  - id: gather-context
    prompt: |
      1) Read issue context from GitHub tools.
      2) Inspect PostgreSQL schema and entity configurations.
      3) Produce a structured summary with root-cause hypotheses.
    mcp: .archon/mcp/all-services.json
    allowed_tools: []

  - id: implement
    prompt: |
      Implement the fix using the context below:

      $gather-context.output

      Requirements:
      - Follow Clean Architecture boundaries.
      - Build and test with: backend/JobNecto.slnx
      - Add or update tests for the bug.
    depends_on: [gather-context]

  - id: validate
    bash: |
      dotnet build backend/JobNecto.slnx
      dotnet test backend/JobNecto.slnx --no-build
    depends_on: [implement]

  - id: summary
    prompt: |
      Summarize changes, test results, and follow-up risks.
      Validation output:
      $validate.output
    depends_on: [validate]
    context: fresh
```

Run it:
```powershell
archon workflow run jobnecto-mcp-issue-fix --cwd e:/apps/Jobnecto --branch fix/issue-77 "Fix issue #77"
```

#### Workflow: Parallel Agentic Review

Create `.archon/workflows/jobnecto-agentic-review.yaml`:

```yaml
name: jobnecto-agentic-review
description: Parallel architecture/test/API review with specialized sub-agents

provider: claude
model: sonnet

nodes:
  - id: review
    prompt: |
      Review current branch changes. Use Task tool to spawn these sub-agents in parallel:
      - boundary-checker: verify Clean Architecture boundaries
      - test-checker: evaluate test coverage
      - api-checker: validate API contracts

      Merge findings sorted by severity.
    allowed_tools: [Task, Read, Grep, Glob, Bash]
    agents:
      boundary-checker:
        description: Checks dependency direction and layer boundaries
        prompt: |
          Focus only on architecture boundaries and dependency flow.
          Report violations with file paths and impact.
        model: haiku
        tools: [Read, Grep, Glob]

      test-checker:
        description: Checks tests for changed behavior
        prompt: |
          Focus on missing tests and weak assertions.
          Suggest concrete new tests.
        model: haiku
        tools: [Read, Grep, Glob]

      api-checker:
        description: Validates API behavior and DTO consistency
        prompt: |
          Focus on endpoint contract, status codes, and auth boundaries.
          Flag regressions and backwards-compatibility risks.
        model: haiku
        tools: [Read, Grep, Glob]
```

Run it:
```powershell
archon workflow run jobnecto-agentic-review --cwd e:/apps/Jobnecto --branch review/parallel "Review current branch"
```

## Operational Commands

### Validate workflows and config

```powershell
archon validate workflows --cwd e:/apps/Jobnecto
archon validate commands --cwd e:/apps/Jobnecto
```

### Check running workflows

```powershell
archon workflow status
```

### Approve or reject paused runs

```powershell
archon workflow approve <run-id> --comment "Proceed"
archon workflow reject <run-id> --reason "Needs fixes"
```

### Clean up merged worktrees

```powershell
archon isolation cleanup --merged
```

## Key Concepts

### Worktrees = Safety

- Each workflow runs in an isolated git worktree under `~/.archon/workspaces/<owner>/<repo>/`
- Your `master` working directory stays clean
- Experiments and long-running tasks don't interfere with local work

### MCP = Context

- GitHub MCP: read issues, PR comments, code
- PostgreSQL MCP: inspect schema, run queries, validate migrations
- Attach to specific nodes with `mcp:` field
- Use `allowed_tools: []` to restrict agents to MCP only

### Agents = Parallelism

- Inline sub-agents run in parallel on the same node
- Each agent has its own scope (tools, model, prompt)
- Results merge into the main prompt context

### Provider Matters

- **Claude**: Full support for MCP + agents + skills
- **Codex/Pi**: Reduced MCP/agent support (check guide section 3)

## Troubleshooting

### Worktree creation errors

If you see stale symlink errors:
```powershell
archon isolation cleanup
```

### MCP tools not available

1. Check node uses `provider: claude`
2. Verify env vars exist in `.archon/.env`
3. For Haiku models with many MCP tools: upgrade to Sonnet

### Secrets not loading

Archon does NOT load `<repo>/.env`. Use `.archon/.env` or `~/.archon/.env` instead.

## Further Reading

- Full integration guide: `docs/ARCHON_INTEGRATION_GUIDE.md`
- Archon docs: https://archon.diy
- JobNecto patterns: see section 14 (adoption plan) in integration guide

---

**When to use this skill:**
- Implementing stories with repeatable quality gates
- Fixing database-related bugs with schema context
- Running parallel reviews on complex PRs
- Orchestrating multi-agent analysis
- Isolating experimental work safely
