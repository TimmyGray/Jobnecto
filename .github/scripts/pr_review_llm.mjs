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
 * - OPENROUTER_MODEL — optional primary model; falls back to DEFAULT_MODEL
 * - OPENROUTER_FALLBACK_MODELS — optional comma-separated models tried after the primary
 * - OPENROUTER_MAX_RETRIES — optional retries per model for transient errors (default 10)
 * - OPENROUTER_BACKOFF_BASE_MS — optional first backoff delay (default 2000)
 * - OPENROUTER_TOTAL_BUDGET_MS — optional wall-clock budget for all attempts (default 20 min)
 *
 * Model resilience: free OpenRouter models are withdrawn without notice, so the script
 * walks a chain — primary, configured fallbacks, free models discovered from the live
 * catalog, then the `openrouter/free` router — and retries transient failures (429,
 * 5xx, timeouts, network errors) on each model with exponential backoff + jitter,
 * honoring Retry-After. Non-transient model errors (404 unavailable, 400, 402, 413…)
 * skip straight to the next model; 401 and exhausted daily free quota stop the chain.
 */

import { readFileSync, existsSync } from "node:fs";
import { stderr } from "node:process";
import { pathToFileURL } from "node:url";

/** OpenRouter chat completions (OpenAI-compatible). */
const OPENROUTER_URL = "https://openrouter.ai/api/v1/chat/completions";

/**
 * Upper bound on diff size sent to the model (~chars). Tuned for a large context window
 * while reserving space for prompts and the API max_tokens response cap.
 */
const MAX_DIFF_CHARS = 900_000;

/** OpenRouter public model catalog (no auth needed); used to discover free fallbacks. */
const OPENROUTER_MODELS_URL = "https://openrouter.ai/api/v1/models";

/** Used when OPENROUTER_MODEL is unset or empty. 1M-token context fits large diffs. */
const DEFAULT_MODEL = "nvidia/nemotron-3-ultra-550b-a55b:free";

/** OpenRouter's meta-router that picks any currently available free model; last resort. */
const FREE_ROUTER_MODEL = "openrouter/free";

/** How many free models discovered from the live catalog to append as fallbacks. */
const DISCOVERED_FALLBACK_LIMIT = 4;

/** Retries per model for transient failures (so up to MAX_RETRIES + 1 attempts). */
const MAX_RETRIES = envInt("OPENROUTER_MAX_RETRIES", 10);

/** First backoff delay; doubles each retry up to MAX_BACKOFF_MS. */
const BACKOFF_BASE_MS = envInt("OPENROUTER_BACKOFF_BASE_MS", 2_000);

/** Ceiling for a single computed backoff delay. */
const MAX_BACKOFF_MS = 60_000;

/** Ceiling for a server-requested wait (Retry-After / X-RateLimit-Reset). */
const MAX_SERVER_WAIT_MS = 120_000;

/** Wall-clock budget across every model and attempt; keep below the job timeout. */
const TOTAL_BUDGET_MS = envInt("OPENROUTER_TOTAL_BUDGET_MS", 20 * 60_000);

/** Per-request timeout so a stuck connection cannot eat the whole budget. */
const REQUEST_TIMEOUT_MS = 180_000;

function envInt(name, fallback) {
  const n = Number.parseInt(process.env[name] ?? "", 10);
  return Number.isFinite(n) && n >= 0 ? n : fallback;
}

/** Include only the most recent LLM reviews to keep context useful and compact. */
const PREVIOUS_REVIEWS_LIMIT = 3;

/** Per-review character cap when folding previous findings into the prompt context. */
const MAX_PREVIOUS_REVIEW_CHARS = 5_000;

/**
 * Cap completion size. The prompt's own hard cap is 1600 words (~2.1k tokens), and
 * reasoning models can spend part of the budget thinking, so leave generous headroom
 * to avoid reviews cut off mid-sentence.
 */
const OPENROUTER_MAX_TOKENS = 4_000;

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
 * Error raised for a failed OpenRouter call, tagged with how the caller should react.
 * kind: "retryable" (same model again after backoff), "next-model" (skip to the next
 * model in the chain), or "fatal" (stop the whole chain, e.g. bad key or daily quota).
 */
class OpenRouterError extends Error {
  constructor(message, { kind, status = null, retryAfterMs = null } = {}) {
    super(message);
    this.name = "OpenRouterError";
    this.kind = kind;
    this.status = status;
    this.retryAfterMs = retryAfterMs;
  }
}

/** HTTP statuses worth retrying on the same model: timeouts, rate limits, upstream outages. */
const RETRYABLE_STATUSES = new Set([408, 425, 429, 500, 502, 503, 504, 520, 522, 524, 529]);

/**
 * Map an HTTP status (from the response or an error object in a 200 body) to a reaction.
 * @param {number} status - HTTP-like status code.
 * @param {string} detail - Error text, used to spot an exhausted daily free quota.
 * @returns {"retryable"|"next-model"|"fatal"}
 */
function classifyStatus(status, detail) {
  if (status === 401) return "fatal"; // bad or missing key: no model will work
  // The free-tier daily cap is account-wide, so every other free model would 429 too.
  if (status === 429 && /per[- ]day|daily/i.test(detail)) return "fatal";
  if (RETRYABLE_STATUSES.has(status) || status >= 500) return "retryable";
  return "next-model"; // 400/402/403/404/413/422…: this model cannot serve the request
}

/**
 * Read a server-requested wait from Retry-After (seconds or HTTP date) or OpenRouter's
 * X-RateLimit-Reset (epoch ms). Returns null when absent or unparseable.
 * @param {Headers} headers
 * @returns {number|null} Wait in ms, capped at MAX_SERVER_WAIT_MS.
 */
function serverRequestedWaitMs(headers) {
  const now = Date.now();
  let wait = null;
  const retryAfter = headers?.get?.("retry-after");
  if (retryAfter) {
    const seconds = Number(retryAfter);
    wait = Number.isFinite(seconds) ? seconds * 1000 : Date.parse(retryAfter) - now;
  }
  const reset = Number(headers?.get?.("x-ratelimit-reset"));
  if (wait == null && Number.isFinite(reset) && reset > 0) {
    // Seconds vs milliseconds: epoch seconds are ~1e9, epoch ms ~1e12.
    wait = (reset < 1e11 ? reset * 1000 : reset) - now;
  }
  if (wait == null || !Number.isFinite(wait) || wait <= 0) return null;
  return Math.min(wait, MAX_SERVER_WAIT_MS);
}

/**
 * Exponential backoff with equal jitter: half the delay is fixed, half random, so
 * concurrent runs spread out while still waiting at least half the nominal delay.
 * @param {number} retry - 1-based retry number.
 * @param {number|null} serverWaitMs - Wait the server asked for, if any; always honored.
 */
function backoffDelayMs(retry, serverWaitMs) {
  const nominal = Math.min(MAX_BACKOFF_MS, BACKOFF_BASE_MS * 2 ** (retry - 1));
  const jittered = nominal / 2 + Math.random() * (nominal / 2);
  return Math.round(Math.max(jittered, serverWaitMs ?? 0));
}

const sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms));

/**
 * Normalize anything thrown by fetch/parsing into an OpenRouterError.
 * Network failures and timeouts are transient; unknown errors get one more model.
 */
function toOpenRouterError(err) {
  if (err instanceof OpenRouterError) return err;
  const name = err?.name ?? "";
  const msg = err?.message ?? String(err);
  const isTransient =
    name === "TimeoutError" ||
    name === "AbortError" ||
    (name === "TypeError" && /fetch/i.test(msg)) ||
    /ECONNRESET|ETIMEDOUT|EHOSTUNREACH|ENOTFOUND|EAI_AGAIN|socket hang up/i.test(msg);
  return new OpenRouterError(msg, { kind: isTransient ? "retryable" : "next-model" });
}

/**
 * Discover currently free text models from the public catalog, largest context first,
 * keeping only models whose context can hold the prompt. Failures return [] — discovery
 * is best effort and must never block the review.
 * @param {number} minContextTokens - Rough token size of the prompt plus completion.
 * @returns {Promise<string[]>}
 */
async function discoverFreeModels(minContextTokens) {
  try {
    const res = await fetch(OPENROUTER_MODELS_URL, {
      signal: AbortSignal.timeout(30_000),
    });
    if (!res.ok) {
      stderr.write(`Free-model discovery skipped: HTTP ${res.status}\n`);
      return [];
    }
    const { data } = await res.json();
    if (!Array.isArray(data)) return [];
    const isZero = (v) => v != null && Number(v) === 0;
    return data
      .filter(
        (m) =>
          typeof m?.id === "string" &&
          m.id.endsWith(":free") &&
          isZero(m.pricing?.prompt) &&
          isZero(m.pricing?.completion) &&
          (m.architecture?.output_modalities ?? ["text"]).includes("text") &&
          (m.context_length ?? 0) >= minContextTokens,
      )
      .sort((a, b) => (b.context_length ?? 0) - (a.context_length ?? 0))
      .slice(0, DISCOVERED_FALLBACK_LIMIT)
      .map((m) => m.id);
  } catch (err) {
    stderr.write(`Free-model discovery skipped: ${err.message}\n`);
    return [];
  }
}

/**
 * Ordered, de-duplicated model chain: primary, configured fallbacks, discovered free
 * models, then the free router as the last resort.
 * @param {string} primary
 * @param {number} minContextTokens
 */
async function buildModelChain(primary, minContextTokens) {
  const configured = (process.env.OPENROUTER_FALLBACK_MODELS ?? "")
    .split(",")
    .map((m) => m.trim())
    .filter(Boolean);
  const discovered = await discoverFreeModels(minContextTokens);
  if (discovered.length) {
    stderr.write(`Discovered free fallback models: ${discovered.join(", ")}\n`);
  }
  return [...new Set([primary, ...configured, ...discovered, FREE_ROUTER_MODEL])];
}

/**
 * Try each model in order. Transient errors are retried on the same model with
 * exponential backoff (up to MAX_RETRIES); model-specific errors move to the next
 * model; fatal errors or an exhausted wall-clock budget stop the chain.
 * @param {string[]} models - Ordered model chain.
 * @param {(model: string, timeoutMs: number) => Promise<string>} call - One attempt.
 * @returns {Promise<{ review: string, model: string, log: string[] }>}
 * @throws {Error} With a per-model attempt log when every model fails.
 */
async function reviewWithFallbacks(models, call) {
  const deadline = Date.now() + TOTAL_BUDGET_MS;
  const log = [];

  for (const model of models) {
    for (let attempt = 1; attempt <= MAX_RETRIES + 1; attempt++) {
      const remaining = deadline - Date.now();
      if (remaining <= 0) {
        throw new Error(`Time budget exhausted.\n${log.join("\n")}`);
      }
      try {
        const review = await call(model, Math.min(REQUEST_TIMEOUT_MS, remaining));
        return { review, model, log };
      } catch (raw) {
        const err = toOpenRouterError(raw);
        const line = `${model} attempt ${attempt}: ${err.message.slice(0, 300)}`;
        log.push(line);
        stderr.write(`${line}\n`);

        if (err.kind === "fatal") throw new Error(`Stopped: ${err.message}\n${log.join("\n")}`);
        if (err.kind === "next-model" || attempt > MAX_RETRIES) break;

        const delay = backoffDelayMs(attempt, err.retryAfterMs);
        if (Date.now() + delay >= deadline) {
          throw new Error(`Time budget exhausted before retry.\n${log.join("\n")}`);
        }
        stderr.write(`Retrying ${model} in ${delay}ms (retry ${attempt}/${MAX_RETRIES})\n`);
        await sleep(delay);
      }
    }
    stderr.write(`Moving on from ${model}\n`);
  }
  throw new Error(`All models failed.\n${log.join("\n")}`);
}

async function openrouterReview(diffText, model, previousReviewContext = null, timeoutMs = REQUEST_TIMEOUT_MS) {
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
    signal: AbortSignal.timeout(timeoutMs),
  });

  const raw = await res.text();
  if (!res.ok) {
    throw new OpenRouterError(`OpenRouter HTTP ${res.status}: ${raw.slice(0, 2000)}`, {
      kind: classifyStatus(res.status, raw),
      status: res.status,
      retryAfterMs: serverRequestedWaitMs(res.headers),
    });
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
    // A truncated body usually means a gateway hiccup; worth another attempt.
    throw new OpenRouterError(
      `OpenRouter invalid JSON: ${raw.slice(0, 500)} (parse error: ${err.message})`,
      { kind: "retryable", status: res.status }
    );
  }

  // Errors can arrive in a 200 body (model unavailable, upstream rate limit, etc.).
  if (parsed.error) {
    const detail = JSON.stringify(parsed.error);
    const code = Number(parsed.error.code);
    throw new OpenRouterError(`OpenRouter API error: ${detail} (status: ${res.status})`, {
      kind: Number.isFinite(code) ? classifyStatus(code, detail) : "retryable",
      status: Number.isFinite(code) ? code : res.status,
      retryAfterMs: serverRequestedWaitMs(res.headers),
    });
  }

  const choices = parsed.choices ?? [];
  if (!choices.length) {
    throw new OpenRouterError(
      `OpenRouter returned no choices. Full response: ${JSON.stringify(parsed).slice(0, 1500)}`,
      { kind: "retryable", status: res.status }
    );
  }

  const content = choices[0]?.message?.content;
  if (content == null || String(content).trim() === "") {
    // Typically a reasoning model spending the whole token budget on thinking: another
    // model is likelier to succeed than the same one again.
    throw new OpenRouterError(
      `OpenRouter returned empty content. Full response: ${JSON.stringify(parsed).slice(0, 1500)}`,
      { kind: "next-model", status: res.status }
    );
  }
  let review = String(content).trim();
  if (choices[0]?.finish_reason === "length") {
    review += "\n\n…_(review truncated: the model hit its output token limit)_";
  }
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

  const primaryModel =
    (process.env.OPENROUTER_MODEL ?? "").trim() || DEFAULT_MODEL;
  const buildHeader = (modelLabel) =>
    "### LLM PR review (OpenRouter)\n\n" +
    `_Model: ${modelLabel}_` +
    (wasTruncated ? " · _diff truncated_" : "") +
    "\n\n---\n\n";
  const header = buildHeader(`\`${primaryModel}\``);

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

    // Rough token estimate (~3 chars/token) plus room for the prompt and completion.
    const minContextTokens = Math.ceil(truncated.length / 3) + 8_000;
    const models = await buildModelChain(primaryModel, minContextTokens);
    stderr.write(`Model chain: ${models.join(" -> ")}\n`);

    const { review, model } = await reviewWithFallbacks(models, (m, timeoutMs) =>
      openrouterReview(truncated, m, previousReviewContext, timeoutMs)
    );
    const label =
      model === primaryModel
        ? `\`${model}\``
        : `\`${model}\` (fallback; \`${primaryModel}\` unavailable)`;
    await githubPostComment(buildHeader(label) + review);
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

export {
  classifyStatus,
  serverRequestedWaitMs,
  backoffDelayMs,
  reviewWithFallbacks,
  buildModelChain,
  OpenRouterError,
};

if (import.meta.url === pathToFileURL(process.argv[1] ?? "").href) {
  await main().catch((err) => {
    stderr.write(`Unexpected failure in pr_review_llm.mjs: ${err}\n`);
  });
}
