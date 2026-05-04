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

## Custom Skills & Agents

All custom skills and agents for this project are located in the **[`.agents/skills/`](.agents/skills/)** folder. Each skill has its own directory containing:

- `SKILL.md` — Documentation and usage instructions for the skill
- Implementation files specific to that skill

Before using any custom skill, refer to its `SKILL.md` file in the `.agents/skills/` directory.

## Quick Start

1. Read `AGENTS.md` completely before starting any work.
2. Refer to [`.agents/skills/`](.agents/skills/) for documentation on any custom skills or agents you need to use.
