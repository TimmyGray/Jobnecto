# Archon Service Integration Guide for JobNecto

<!-- markdownlint-disable MD032 MD029 -->

This guide explains what Archon is, how it can help this repository, and exactly how to use it with MCP and agent patterns.

Scope:
- Practical setup for this repository (`e:/apps/Jobnecto`)
- Real commands you can run
- Ready-to-copy workflow and MCP examples
- Agent usage (built-in workflows, inline sub-agents, and skills)

## 1) What Archon Is

Archon is a workflow orchestrator for coding agents.

At a high level, Archon lets you:
- Define multi-step coding workflows in YAML (DAG nodes)
- Run tasks in isolated git worktrees by default
- Parallelize independent steps (for example, multiple review agents)
- Attach external tools through MCP on specific nodes
- Add approval gates and iterative loops

You can run it:
- From CLI (`archon workflow run ...`)
- From Web UI
- Through adapters (Slack, Telegram, GitHub, etc.)

## 2) Why It Can Help JobNecto

For this repository, Archon is especially useful for:

1. Repeatable implementation loops
- JobNecto has many API stories with similar flow: analyze -> implement -> test -> update docs.
- Archon turns that into a reusable workflow so each task follows the same quality bar.

2. Safe branch isolation
- Workflows run in separate git worktrees by default.
- Your `master` working tree remains clean while experiments or long-running fixes happen in isolated branches.

3. Faster review throughput
- Parallel review nodes can check correctness, test coverage, and security at once.

4. Better context gathering
- MCP nodes can pull live context from GitHub issues and PostgreSQL schema/data before implementation starts.

5. Human-in-the-loop checkpoints
- Approval nodes let you stop before risky changes and continue only after manual sign-off.

## 3) Provider Capability Matrix (Important)

Archon supports multiple assistant providers, but not all features are equal.

For your question about MCP and agents, this is critical:

- Claude provider:
  - MCP on nodes: supported
  - Per-node skills: supported
  - Inline sub-agents (`agents:`): supported

- Codex provider:
  - MCP node field: ignored with warning
  - Per-node skills: ignored with warning
  - Inline sub-agents: ignored with warning

- Pi provider:
  - MCP: not supported by design
  - Inline sub-agents: not supported
  - Skills: supported in Pi-capability terms, but different behavior from Claude-specific SDK features

If you want MCP + inline agents in Archon workflows, use `provider: claude` on those nodes.

## 4) Is Archon Itself an MCP Server?

Short answer: no.

Archon is an orchestrator that consumes MCP servers per node. You attach MCP configs to workflow nodes using the `mcp:` field.

Think of it like this:
- Archon = workflow runtime + DAG orchestration + worktree/session management
- MCP servers = external tool backends (GitHub, Postgres, etc.) used by selected nodes

## 5) Installation and First Run (Windows-Oriented)

### Option A: Binary install (quick)

PowerShell:

```powershell
irm https://archon.diy/install.ps1 | iex
archon version
```

### Option B: Homebrew (if you use it)

```bash
brew install coleam00/archon/archon
```

### Option C: Source/dev install

```powershell
git clone https://github.com/coleam00/Archon
cd Archon
bun install
```

### Claude CLI prerequisite

Archon orchestrates Claude Code; it does not bundle it for all install modes.

PowerShell:

```powershell
irm https://claude.ai/install.ps1 | iex
claude /login
```

If you installed Archon as compiled binary, set `CLAUDE_BIN_PATH` if needed.

Typical Windows location:
- `%USERPROFILE%\.local\bin\claude.exe`

## 6) Minimal JobNecto Setup

From repository root:

```powershell
cd e:/apps/JobNecto
archon workflow list --cwd e:/apps/JobNecto
```

Run a quick repository understanding pass:

```powershell
archon workflow run archon-assist --cwd e:/apps/JobNecto "Summarize architecture and test strategy"
```

Run a built-in smart review:

```powershell
archon workflow run archon-smart-pr-review --cwd e:/apps/JobNecto --branch chore/archon-initial-review "Review current branch and list high-risk issues"
```

## 7) Recommended `.archon/config.yaml` for JobNecto

Create `e:/apps/JobNecto/.archon/config.yaml`:

```yaml
assistant: claude

assistants:
  claude:
    model: sonnet
    settingSources:
      - project

  codex:
    model: gpt-5.3-codex
    modelReasoningEffort: medium
    webSearchMode: disabled

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

Notes:
- `.archon/` is copied into worktrees automatically; do not list it in `copyFiles`.
- Keep secrets in `.archon/.env` (project scope) or `~/.archon/.env` (user scope), not in workflow YAML.

## 8) MCP Integration in This Project

## 8.1 Create MCP config

Create `e:/apps/JobNecto/.archon/mcp/all-services.json`:

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

## 8.2 Create an MCP-aware workflow

Create `e:/apps/JobNecto/.archon/workflows/jobnecto-mcp-issue-fix.yaml`:

```yaml
name: jobnecto-mcp-issue-fix
description: |
  Investigate a GitHub issue with live DB schema context, then implement and validate.
  Use when: issue references DB behavior, migrations, or data-dependent bugs.

provider: claude
model: sonnet

nodes:
  - id: gather-context
    prompt: |
      1) Read issue context from GitHub tools.
      2) Inspect PostgreSQL schema and relevant tables.
      3) Produce a structured summary with root-cause hypotheses.
    mcp: .archon/mcp/all-services.json
    allowed_tools: []

  - id: implement
    prompt: |
      Implement the fix in this repository using the context below:

      $gather-context.output

      Requirements:
      - Follow Clean Architecture boundaries.
      - Build and test with solution: backend/JobNecto.slnx.
      - Add or update tests for the bug.
    depends_on: [gather-context]

  - id: validate
    bash: |
      dotnet build backend/JobNecto.slnx
      dotnet test backend/JobNecto.slnx --no-build
    depends_on: [implement]

  - id: summary
    prompt: |
      Summarize what changed, test results, and any follow-up risks.
      Validation output:
      $validate.output
    depends_on: [validate]
    context: fresh
```

Run it:

```powershell
archon workflow run jobnecto-mcp-issue-fix --cwd e:/apps/JobNecto --branch fix/mcp-issue-123 "Fix issue #123"
```

## 8.3 MCP-only node pattern

Use `allowed_tools: []` together with `mcp:` when you want the agent to only use MCP tools and not local file/system tools.

This is useful for:
- Read-only triage
- Sensitive environments
- Strict context acquisition before implementation

## 9) Using Archon with Agents

There are three practical agent modes in Archon:

1. Built-in multi-agent workflows
- Example: `archon-comprehensive-pr-review`
- Runs parallel reviewer nodes and synthesizes findings.

2. Inline sub-agents (`agents:`)
- Define node-local sub-agents directly in workflow YAML.
- Good for map-reduce patterns (many small analyses, one synthesis).

3. Reusable on-disk agents (`.claude/agents/*.md`)
- Better when multiple workflows share the same specialized sub-agent role.

### 9.1 Inline sub-agent example for JobNecto

Create `e:/apps/JobNecto/.archon/workflows/jobnecto-agentic-review.yaml`:

```yaml
name: jobnecto-agentic-review
description: Parallel architecture-aware review with specialized sub-agents

provider: claude
model: sonnet

nodes:
  - id: review
    prompt: |
      Review current branch changes.
      Spawn sub-agents in parallel using the Task tool:
      - boundary-checker: verify Clean Architecture boundaries
      - test-checker: evaluate test coverage gaps
      - api-checker: validate API contract and status code behavior

      Return merged findings sorted by severity.
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
          Prefer concrete new test suggestions.
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
archon workflow run jobnecto-agentic-review --cwd e:/apps/JobNecto --branch review/agentic-current "Review current branch"
```

## 10) Combining MCP + Skills + Agents on the Same Node

This is the highest-leverage pattern for complex work:
- Skills: teach process and standards
- MCP: provide external tool capabilities
- Inline agents: parallelize focused reasoning

Example node fragment:

```yaml
- id: enriched-analysis
  prompt: "Analyze issue, schema, and propose minimal-risk fix plan"
  provider: claude
  skills:
    - code-review
    - testing-patterns
  mcp: .archon/mcp/all-services.json
  allowed_tools: [Task, Read, Grep, Glob]
  agents:
    db-risk-checker:
      description: Reviews SQL/data risks only
      prompt: "Focus only on migrations, constraints, and rollback safety"
      model: haiku
      tools: [Read, Grep]
```

## 11) Using Archon from Other Agent Environments

If you use Claude Code as your primary coding agent, Archon can be installed as a Claude skill and invoked conversationally.

Typical flow:
- Run `archon setup`
- Install/enable Archon skill into the target repository
- Ask Claude Code to use Archon workflow for a task (for example, issue fix or PR review)

This gives you a "meta-agent" model:
- Claude conversation controls the task
- Archon executes a structured workflow under the hood

## 12) Validation and Operational Commands

Validate your workflow files and referenced resources:

```powershell
archon validate workflows --cwd e:/apps/JobNecto
archon validate commands --cwd e:/apps/JobNecto
```

Check running workflows:

```powershell
archon workflow status
```

Approve or reject paused runs:

```powershell
archon workflow approve <run-id> --comment "Proceed"
archon workflow reject <run-id> --reason "Need additional tests"
```

Clean stale worktrees:

```powershell
archon isolation cleanup
archon isolation cleanup --merged
```

## 13) Troubleshooting (Relevant to JobNecto + Windows)

1. Worktree creation errors mentioning stale source/symlink
- Remove stale folder under `~/.archon/workspaces/<owner>/<repo>/` and retry.

2. MCP config file exists but tools missing
- Ensure node uses `provider: claude`.
- Check environment variables referenced in MCP `env` are actually set.

3. MCP with Haiku model warning
- Tool search for many MCP tools is not supported on Haiku. Use Sonnet or Opus for MCP-heavy nodes.

4. Docker container appears stuck in `Created` with bind mounts
- Validate baseline run without bind mount first.
- Then re-add port and volume options incrementally to isolate mount problems on Windows Docker Desktop.

5. Secrets not loaded from app `.env`
- Archon intentionally does not load `<repo>/.env`.
- Use `<repo>/.archon/.env` or `~/.archon/.env`.

## 14) Suggested Adoption Plan for This Repo

Phase 1 (1 day):
- Install Archon and verify baseline workflows (`archon-assist`, `archon-smart-pr-review`).

Phase 2 (2-3 days):
- Add repo `.archon/config.yaml`.
- Add one custom workflow for .NET validate-and-review.
- Start using isolated branches for bug fixes.

Phase 3 (ongoing):
- Add MCP configs (GitHub + Postgres) for schema-aware issue workflows.
- Add inline sub-agents for architecture/test/API parallel checks.
- Add approval gates for high-risk changes.

## 15) Practical Command Cheat Sheet

```powershell
# List workflows
archon workflow list --cwd e:/apps/JobNecto

# Run built-in workflow with isolated branch
archon workflow run archon-feature-development --cwd e:/apps/JobNecto --branch feat/example "Implement feature X"

# Run custom MCP workflow
archon workflow run jobnecto-mcp-issue-fix --cwd e:/apps/JobNecto --branch fix/issue-77 "Fix issue #77"

# Validate all workflow files
archon validate workflows --cwd e:/apps/JobNecto

# Check active runs
archon workflow status

# Cleanup merged worktrees
archon isolation cleanup --merged
```

## 16) Final Recommendations

For JobNecto, use Archon as:
- A repeatable implementation/review pipeline runner
- A safe worktree orchestrator
- A Claude-based MCP client for GitHub + PostgreSQL context
- A multi-agent execution framework for parallel specialized review

If your immediate goal is highest ROI with lowest effort:
1. Start with built-in `archon-smart-pr-review` and `archon-feature-development`.
2. Add one custom MCP-assisted issue workflow.
3. Introduce inline sub-agents for architecture/test/API parallel review.
