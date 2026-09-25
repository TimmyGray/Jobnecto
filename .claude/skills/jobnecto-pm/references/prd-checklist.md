# PRD validation checklist

## Clarity & value
- [ ] Executive summary names the user, the problem, and why now, in five sentences or fewer.
- [ ] Success criteria are measurable (number, threshold, or observable behavior).
- [ ] MVP scope is the smallest thing that validates the core assumption.
- [ ] Out-of-scope is explicit, with a reason per item.

## Requirements quality
- [ ] Every FR is one sentence, starts with "A user can…" or "The system…", and is testable yes/no.
- [ ] No FR contains implementation detail (class names, tables, frameworks).
- [ ] FR IDs are unique and stable. No gaps were created by renumbering, and retired IDs are not reused.
- [ ] Every NFR is measurable and names how it will be verified.
- [ ] Security/privacy requirements cover auth, ownership (403 vs 404 contract), and data retention where relevant.
- [ ] LLM-related features state output-integrity rules (grounding, no fabricated facts, user review before save).

## Coverage & consistency
- [ ] Every journey step is covered by at least one FR.
- [ ] Every FR is exercised by at least one journey, or is justified as a system requirement.
- [ ] Nothing contradicts `_bmad-output/planning-artifacts/architecture/` decisions without being flagged.
- [ ] Nothing mixes Phase B (`prd.md`) and Demo MVP (`prd-demo-mvp.md`) FR numbering.
- [ ] Open questions are listed with owners, and none of them block the MVP without being marked as blocking.

## Hygiene
- [ ] No secrets, credentials, or connection strings.
- [ ] Input document paths are repo-relative (not machine-specific like `e:\apps\...`).
