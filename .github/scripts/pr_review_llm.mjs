/**
 * GitHub Actions helper: read a unified PR diff from disk, send it to OpenRouter for
 * a structured review, and post the result as an issue comment on the PR.
 *
 * Design choices:
 * - Always exits 0 so a failed review does not fail the workflow or block merges
 *   (branch protection should not treat this job as required, or failures are harmless).
 * - Diff is filtered before sending: lockfiles, binaries, build outputs, and generated
 *   code add noise and tokens without helping review quality.
 * - Diff length is capped in characters as a rough proxy for model context; leave room
 *   for the system prompt, the fenced diff wrapper, and max_tokens in the completion.
 *
 * Environment (set by the workflow):
 * - GITHUB_TOKEN, GITHUB_REPOSITORY, PR_NUMBER — post comment via REST
 * - OPENROUTER_API_KEY — required for the model call (secret)
 * - OPENROUTER_MODEL — optional; falls back to DEFAULT_MODEL
 */

import { readFileSync, existsSync } from "node:fs";
import { stderr } from "node:process";

/** OpenRouter chat completions (OpenAI-compatible). */
const OPENROUTER_URL = "https://openrouter.ai/api/v1/chat/completions";

/**
 * Upper bound on diff size sent to the model (~chars). Tuned for a large context window
 * while reserving space for prompts and the API max_tokens response cap.
 */
const MAX_DIFF_CHARS = 900_000;

/** Used when OPENROUTER_MODEL is unset or empty. */
const DEFAULT_MODEL = "openai/gpt-oss-120b:free";

/** Include only the most recent LLM reviews to keep context useful and compact. */
const PREVIOUS_REVIEWS_LIMIT = 3;

/** Per-review character cap when folding previous findings into the prompt context. */
const MAX_PREVIOUS_REVIEW_CHARS = 5_000;

/** Cap completion size so comments stay detailed but not overly long. */
const OPENROUTER_MAX_TOKENS = 1_400;

/**
 * Paths whose entire diff hunks we drop before calling the LLM.
 * Matches paths from `diff --git a/... b/...` (normalized to forward slashes).
 */
const SKIP_PATH_RE = new RegExp(
  "(^|/)(node_modules|bin|obj|\\.git)(/|$)|" +
    "\\.(lock|dll|exe|pdb|png|jpe?g|gif|webp|ico|pdf|zip|ttf|woff2?|eot)$|" +
    "(packages\\.lock\\.json|package-lock\\.json|yarn\\.lock|pnpm-lock\\.yaml|\\.min\\.js)$|" +
    "\\.Designer\\.cs$|\\.g\\.cs$|\\.generated\\.|/Generated/",
  "i",
);

function shouldSkipFile(path) {
  const normalized = path.replace(/\\/g, "/").replace(/^\.\/+/, "");
  return SKIP_PATH_RE.test(normalized);
}

/**
 * Walk unified diff by file: each `diff --git` block is kept or discarded as a whole.
 * Lines before the first `diff --git` (rare) are preserved.
 */
function filterDiff(diffText) {
  // Split after each newline so each element ends with \n (except possibly the last).
  const lines = diffText.split(/(?<=\n)/);
  const out = [];
  let i = 0;
  while (i < lines.length) {
    const line = lines[i];
    if (line.startsWith("diff --git ")) {
      const m = line.match(/^diff --git a\/(\S+) b\/(\S+)/);
      const paths = m ? [m[1], m[2]] : ["", ""];
      const skipBlock = shouldSkipFile(paths[0]) || shouldSkipFile(paths[1]);
      const block = [line];
      i += 1;
      while (i < lines.length && !lines[i].startsWith("diff --git ")) {
        block.push(lines[i]);
        i += 1;
      }
      if (!skipBlock) out.push(...block);
      continue;
    }
    out.push(line);
    i += 1;
  }
  return out.join("");
}

function truncate(text, limit) {
  if (text.length <= limit) return { text, truncated: false };
  return {
    text: text.slice(0, limit) + "\n\n…(diff truncated for token limits)\n",
    truncated: true,
  };
}

function compactPreviousReview(body) {
  const lines = body.split("\n");
  const findingsStart = lines.findIndex((l) =>
    l.includes("## Potential bugs and edge cases")
  );
  const summaryStart = lines.findIndex((l) => l.includes("## Summary of changes"));

  const startIndex = findingsStart >= 0 ? findingsStart : summaryStart;
  const selected = startIndex >= 0 ? lines.slice(startIndex).join("\n") : body;

  if (selected.length <= MAX_PREVIOUS_REVIEW_CHARS) return selected;
  return (
    selected.slice(0, MAX_PREVIOUS_REVIEW_CHARS) +
    "\n\n...(previous review context truncated)\n"
  );
}

/**
 * PRs are issues in GitHub’s API; issue comments appear on the PR conversation tab.
 */
async function githubPostComment(body) {
  const token = process.env.GITHUB_TOKEN ?? "";
  const repo = process.env.GITHUB_REPOSITORY ?? "";
  const pr = process.env.PR_NUMBER ?? "";
  if (!token || !repo || !pr) {
    stderr.write("Missing GITHUB_TOKEN, GITHUB_REPOSITORY, or PR_NUMBER\n");
    return;
  }
  const url = `https://api.github.com/repos/${repo}/issues/${pr}/comments`;
  const res = await fetch(url, {
    method: "POST",
    headers: {
      Authorization: `Bearer ${token}`,
      Accept: "application/vnd.github+json",
      "X-GitHub-Api-Version": "2022-11-28",
      "Content-Type": "application/json",
    },
    body: JSON.stringify({ body }),
  });
  if (!res.ok) {
    const t = await res.text();
    stderr.write(`GitHub comment API ${res.status}: ${t.slice(0, 500)}\n`);
  }
}

/**
 * Fetch previous LLM reviews from the PR to provide context for new reviews.
 * @returns {Promise<string|null>} Summary of previous reviews or null if none found.
 */
async function getPreviousReviewContext() {
  const token = process.env.GITHUB_TOKEN ?? "";
  const repo = process.env.GITHUB_REPOSITORY ?? "";
  const pr = process.env.PR_NUMBER ?? "";
  if (!token || !repo || !pr) return null;

  try {
    const url = `https://api.github.com/repos/${repo}/issues/${pr}/comments`;
    const res = await fetch(url, {
      method: "GET",
      headers: {
        Authorization: `Bearer ${token}`,
        Accept: "application/vnd.github+json",
        "X-GitHub-Api-Version": "2022-11-28",
      },
    });
    if (!res.ok) return null;

    const comments = await res.json();
    if (!Array.isArray(comments)) return null;

    const trustedReviewAuthors = new Set(["github-actions", "github-actions[bot]"]);

    // Extract previous review comments from github-actions
    const previousReviews = comments
      .filter(
        (c) =>
          trustedReviewAuthors.has(c.user?.login ?? "") &&
          c.body &&
          c.body.includes("### LLM PR review")
      )
      .map((c) => compactPreviousReview(c.body))
      .slice(-PREVIOUS_REVIEWS_LIMIT);

    if (previousReviews.length === 0) return null;

    stderr.write(
      `Including ${previousReviews.length} previous review(s) for continuity context.\n`
    );

    // Summarize for the new review prompt
    return `
## Previous Review Context

The following findings were identified in previous reviews; use this context when reviewing new changes:

${previousReviews.map((r, i) => `### Review ${i + 1}\n${r}`).join("\n\n---\n\n")}

When reviewing new changes, consider:
- Do the fixes from previous review findings still apply or have they been addressed?
- Are there new instances of previously-flagged issues?
- Are the recommendations from earlier reviews being followed?
`;
  } catch (err) {
    stderr.write(`Failed to fetch previous reviews: ${err.message}\n`);
    return null;
  }
}

/**
 * Retry with exponential backoff for transient failures.
 * @param {Function} fn - Async function to retry
 * @param {number} maxAttempts - Max retry attempts (default 3)
 * @param {number} initialDelayMs - Initial delay in ms (default 1000)
 */
async function retryWithBackoff(fn, maxAttempts = 3, initialDelayMs = 1000) {
  let lastError;
  for (let attempt = 1; attempt <= maxAttempts; attempt++) {
    try {
      return await fn();
    } catch (err) {
      lastError = err;
      // Determine if error is retryable:
      // - Network/fetch errors: bubbled from fetch API
      // - Timeout errors: explicit timeout or network delay (ETIMEDOUT, EHOSTUNREACH)
      // - HTTP status codes in message: 429 (rate limit), 502/503/504 (server errors)
      const isRetryable =
        err.message?.includes("fetch") ||
        err.message?.includes("timeout") ||
        err.message?.includes("ETIMEDOUT") ||
        err.message?.includes("EHOSTUNREACH") ||
        /\b(429|502|503|504)\b/.test(err.message); // HTTP status codes

      if (attempt < maxAttempts && isRetryable) {
        const delay = initialDelayMs * Math.pow(2, attempt - 1);
        stderr.write(
          `Retryable error (attempt ${attempt}/${maxAttempts}): ${err.message}\n` +
            `Waiting ${delay}ms before retry...\n`
        );
        await new Promise((resolve) => setTimeout(resolve, delay));
      } else {
        throw err;
      }
    }
  }
  throw lastError;
}

async function openrouterReview(diffText, model, previousReviewContext = null) {
  const apiKey = (process.env.OPENROUTER_API_KEY ?? "").trim();
  if (!apiKey) throw new Error("OPENROUTER_API_KEY is not set");

  // Fixed section headings keep PR comments predictable for humans and automation.
  let system = `You are a principal-level software engineer performing a thorough pull request review.
Your audience is the PR author and other maintainers. Be specific: reference file paths and
line ranges from the diff when pointing out issues.

Evidence rule: every finding must be traceable to lines visible in this diff.
If a finding depends on the behavior of a method whose implementation is not in the diff,
you MUST explicitly state the assumption (e.g., "assuming X can return null") and cap the
severity at 🟡 warning. Never rate an inferred, unverified path as 🔴 critical.

Prioritization rule: report findings in strict risk order and include both severity and risk score.
Length rule: keep the whole response high-signal (target 700-1200 words; hard cap 1600 words).
Finding budget: include at most 8 findings total and skip low-value style nits.
Confidence rule: do not speculate beyond visible diff evidence.
Framework rule: do not raise critical findings about JWT signature validation, auth middleware,
CSRF/CORS, or infrastructure config unless the diff explicitly changes those configurations.
If the evidence is indirect, either omit the finding or mark it as P3/🟡 warning with assumptions.

Output valid GitHub-flavoured Markdown. Use exactly these sections in order:

## Summary of changes
Describe WHAT changed and WHY (infer intent from the diff). List affected components, layers,
or modules. Call out new files vs modified files.`;

  // Include previous review context if available
  if (previousReviewContext) {
    system += `

${previousReviewContext}`;
  }

  system += `

## Correctness
Analyse whether the implementation is logically correct. Look for:
- Off-by-one errors, wrong comparisons, missing null checks for values whose source is visible in the diff
- Incorrect async/await usage, unhandled promise rejections
- Misuse of APIs or library functions
- State mutations that could cause race conditions
If everything looks correct, say so explicitly.

## Potential bugs and edge cases
Identify concrete scenarios that could break:
- Empty inputs, boundary values, large payloads
- Concurrent access, retry / idempotency gaps
- Missing error handling or swallowed exceptions
- Broken contracts with callers or downstream services
For each finding, use a markdown table row with columns:
Priority | Severity | Risk (1-10) | Evidence (file + lines) | Impact | Recommended fix.
Use Priority values P0/P1/P2/P3 and Severity values 🔴 critical / 🟡 warning / 🔵 nit.
Before rating any finding 🔴 critical, verify: can you quote specific diff lines that
demonstrate the full fault path? If not, downgrade to 🟡 warning.
If there are no concrete issues, state that explicitly.
Do not create hypothetical security findings from unchanged framework defaults.

## Security
Flag any security concerns:
- Injection risks (SQL, command, template)
- Secrets or credentials in code
- Missing input validation / sanitisation
- Overly permissive CORS, auth, or access control
If none found, state "No security concerns identified."

## Performance
Highlight unnecessary allocations, redundant I/O, N+1 queries, missing indexes, or
algorithmic inefficiencies. Suggest concrete fixes where applicable.
If no concerns, state "No performance concerns identified."

## Design and maintainability
Evaluate architecture and code quality:
- Single Responsibility, separation of concerns, coupling
- Naming clarity, consistency with the rest of the codebase
- Dead code, duplication, overly complex logic
- Missing or incorrect types / interfaces
- Adherence to project conventions (Clean Architecture layers, etc.)

## Test coverage
Assess whether the changes are adequately tested:
- Are new behaviours covered by unit or integration tests?
- Are important edge cases tested?
- Are existing tests still valid after these changes?
If tests are missing, suggest specific test cases.

## Suggestions for improvement
Provide actionable recommendations ordered by impact. For non-trivial suggestions, include
a short code snippet showing the proposed change. Limit this section to at most 5 bullets.

## Verdict
End with one of:
- ✅ **Approve** — no blocking issues found
- ⚠️ **Approve with suggestions** — minor issues that should be addressed but don't block merge
- 🚫 **Request changes** — blocking issues that must be fixed before merge

If the diff is empty or contains only trivial changes (whitespace, formatting), say so briefly
and approve.`;

  const user = `Pull request diff (unified format):\n\n\`\`\`diff\n${diffText}\n\`\`\``;

  // OpenRouter recommends these for attribution on their leaderboard (optional but polite).
  let referer = "https://github.com/";
  const ghRepo = (process.env.GITHUB_REPOSITORY ?? "").trim();
  if (ghRepo) referer = `https://github.com/${ghRepo}`;

  const res = await fetch(OPENROUTER_URL, {
    method: "POST",
    headers: {
      Authorization: `Bearer ${apiKey}`,
      "Content-Type": "application/json",
      "HTTP-Referer": referer,
      "X-OpenRouter-Title": "JobNecto PR Review",
    },
    body: JSON.stringify({
      model,
      messages: [
        { role: "system", content: system },
        { role: "user", content: user },
      ],
      // Low temperature: more consistent review tone; less creative drift.
      temperature: 0.3,
      max_tokens: OPENROUTER_MAX_TOKENS,
    }),
    // Avoid hanging the Actions runner indefinitely on a stuck connection.
    signal: AbortSignal.timeout(300_000), // 5 minutes
  });

  const raw = await res.text();
  if (!res.ok) {
    throw new Error(`OpenRouter HTTP ${res.status}: ${raw.slice(0, 2000)}`);
  }

  if (raw.length === 0) {
    stderr.write("OpenRouter returned empty response body\n");
  } else {
    stderr.write(`OpenRouter raw response size: ${raw.length} chars\n`);
  }

  let parsed;
  try {
    parsed = JSON.parse(raw);
  } catch (err) {
    throw new Error(
      `OpenRouter invalid JSON: ${raw.slice(0, 500)} (parse error: ${err.message})`
    );
  }

  // Check for error message in response (common when model unavailable, rate limited, etc.)
  if (parsed.error) {
    throw new Error(
      `OpenRouter API error: ${JSON.stringify(parsed.error)} (status: ${res.status})`
    );
  }

  const choices = parsed.choices ?? [];
  if (!choices.length) {
    throw new Error(
      `OpenRouter returned no choices. Full response: ${JSON.stringify(parsed).slice(0, 1500)}`
    );
  }

  const content = choices[0]?.message?.content;
  if (content == null || String(content).trim() === "") {
    throw new Error(
      `OpenRouter returned empty content. Full response: ${JSON.stringify(parsed).slice(0, 1500)}`
    );
  }
  const review = String(content).trim();
  stderr.write(`OpenRouter review length: ${review.length} chars\n`);
  stderr.write(`OpenRouter review preview (first 1000 chars): ${review.slice(0, 1000)}\n`);
  return review;
}

async function main() {
  const diffPath = process.argv[2];
  if (!diffPath) {
    stderr.write("Usage: node pr_review_llm.mjs <diff-file>\n");
    return;
  }

  const diffText = existsSync(diffPath)
    ? readFileSync(diffPath, "utf8")
    : "";
  const filtered = filterDiff(diffText);
  const { text: truncated, truncated: wasTruncated } = truncate(
    filtered,
    MAX_DIFF_CHARS,
  );

  const model =
    (process.env.OPENROUTER_MODEL ?? "").trim() || DEFAULT_MODEL;
  const header =
    "### LLM PR review (OpenRouter)\n\n" +
    `_Model: \`${model}\`_` +
    (wasTruncated ? " · _diff truncated_" : "") +
    "\n\n---\n\n";

  // Nothing left after filters: still comment so the PR thread shows the run completed.
  if (!filtered.trim()) {
    try {
      await githubPostComment(
        header +
          "No reviewable diff after filtering (or empty PR). " +
          "Skipped lockfiles, binaries, build outputs, and generated assets.",
      );
    } catch (e) {
      stderr.write(`Failed to post GitHub comment: ${e}\n`);
    }
    return;
  }

  try {
    // Fetch previous review context for continuity across commits
    const previousReviewContext = await getPreviousReviewContext();
    if (previousReviewContext) {
      stderr.write("Found previous reviews; including context for new analysis...\n");
    }

    const review = await retryWithBackoff(
      () => openrouterReview(truncated, model, previousReviewContext),
      3, // maxAttempts
      1000 // initialDelayMs
    );
    await githubPostComment(header + review);
  } catch (e) {
    stderr.write(`${e}\n`);
    const msg = e instanceof Error ? e.message : String(e);
    // Surface failure on the PR for visibility; exit 0 keeps the workflow green.
    try {
      await githubPostComment(
        header +
          "⚠️ **LLM review could not be completed.**\n\n" +
          "```\n" +
          msg +
          "\n```\n\nThis does not block merging.",
      );
    } catch (postErr) {
      stderr.write(`Failed to post failure comment: ${postErr}\n`);
    }
  }

  return;
}

await main().catch((err) => {
  stderr.write(`Unexpected failure in pr_review_llm.mjs: ${err}\n`);
});
