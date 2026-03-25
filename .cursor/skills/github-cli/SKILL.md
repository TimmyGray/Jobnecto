---
name: github-cli
description: Use the GitHub CLI (gh) for issues, pull requests, branches (with git), commits (with git), and repository operations. Use when the user or task mentions GitHub, gh, creating issues/PRs, or pushing work to origin.
---

# GitHub CLI (`gh`) for agents

Use **`gh`** for anything that talks to GitHub’s API (issues, PRs, releases, repo metadata). Use **`git`** for the object database (commits, local branches, merge/rebase). **`gh`** wraps Git and can open PRs from the current branch after you push.

## Prerequisites

```bash
gh --version
gh auth status
```

If not logged in: `gh auth login` (follow prompts). In Cloud, auth is usually preconfigured.

**Current repo** (when run inside a git clone):

```bash
gh repo view --json nameWithOwner,url -q .
```

## Issues

| Action | Command |
|--------|---------|
| Create (editor) | `gh issue create` |
| Create (inline) | `gh issue create --title "Title" --body "Description"` |
| Create from file | `gh issue create --title "Title" --body-file issue.md` |
| List (open) | `gh issue list` |
| List with state | `gh issue list --state all` |
| View in terminal | `gh issue view <number>` |
| View in browser | `gh issue view <number> --web` |
| Comment | `gh issue comment <number> --body "Comment"` |
| Close | `gh issue close <number>` |
| Reopen | `gh issue reopen <number>` |

**Labels and metadata** (examples):

```bash
gh issue create --title "Bug: …" --body "…" --label bug
gh issue edit 42 --add-label "priority/high"
```

## Branches (git + GitHub)

Local branch operations are **git**:

| Action | Command |
|--------|---------|
| Current branch | `git branch --show-current` |
| List local | `git branch` |
| List remote | `git branch -r` |
| Create and switch | `git switch -c feature/my-change` |
| Switch | `git switch main` |
| Delete local (safe) | `git branch -d feature/done` |
| Fetch | `git fetch origin` |
| Push new branch | `git push -u origin <branch-name>` |

**Remote branch cleanup** (after PR merge, if desired):

```bash
git fetch origin --prune
```

`gh` can show **default branch** and repo info:

```bash
gh repo view --json defaultBranchRef -q .defaultBranchRef.name
```

## Commits (git)

`gh` does not create commits. Standard workflow:

```bash
git status
git add <paths>
git commit -m "Short imperative description"
```

Amend last commit (only when appropriate, e.g. before push or on private branch):

```bash
git commit --amend --no-edit
```

**Push** (Cloud convention: set upstream on first push):

```bash
git push -u origin "$(git branch --show-current)"
```

Retry pushes on transient network errors with backoff (e.g. 4s, 8s, 16s, 32s).

## Pull requests

Assume the branch is pushed to `origin` first.

| Action | Command |
|--------|---------|
| Create (interactive) | `gh pr create` |
| Create draft | `gh pr create --draft --title "…" --body "…"` |
| Create ready | `gh pr create --title "…" --body "…"` |
| Specify base | `gh pr create --base main` |
| List | `gh pr list` |
| View current branch’s PR | `gh pr view` |
| View by number | `gh pr view <number>` |
| View URL | `gh pr view --json url -q .url` |
| Checks | `gh pr checks <number>` |
| Merge (when allowed) | `gh pr merge <number>` |
| Merge squash | `gh pr merge <number> --squash` |
| Comment | `gh pr comment <number> --body "…"` |
| Request review | `gh pr edit <number> --add-reviewer username` |

**Status for automation**:

```bash
gh pr view --json state,mergeable,url,title
```

## Repo and workflow extras

```bash
gh workflow list
gh run list --limit 5
gh run view <run-id>
```

## Practices for Cloud agents

1. **Branch**: Work on the task branch; never push to a different branch unless instructed.
2. **Commit often**: Small commits with clear messages; push before heavy testing if policy requires.
3. **PR**: Create or update the PR when the task says to; use `--draft` when work is incomplete unless told otherwise.
4. **Issues**: Use `gh issue create` for tracking work requested via GitHub; put acceptance criteria in the body.
5. **Secrets**: Do not paste tokens into issues, PR bodies, or commit messages. Rely on `gh auth` and environment-provided credentials.

## When to use this skill vs. Cursor tools

- If the environment provides a **ManagePullRequest** or similar integration, prefer it for PR open/update when it satisfies the task.
- Otherwise use **`gh pr create`**, **`gh pr view`**, and **`gh issue create`** as above.

## Updating this skill

When a new `gh` pattern is proven in Cloud (new flags, org rules), add a row to the relevant table or a short subsection—keep commands copy-pasteable.
