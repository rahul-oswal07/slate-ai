# Review policy

Applies to human and Claude reviews. Findings inform; a human approves.

## Pass 1 — Bugs
- Does the change do what the linked task's acceptance criteria say?
- Error paths: provider timeout, invalid LLM output, offline replay, empty class, missing audio.
- Idempotency of write endpoints (`clientRequestId`).

## Pass 2 — Security and privacy
- No child/teacher names or phone numbers in prompts, logs or provider calls.
- No secrets in code, config files or test fixtures.
- Parent endpoints only reachable by token; tokens unguessable and expiring.
- Unapproved packages can't be sent.

## Pass 3 — Compliance with our artifacts
- Matches `spec.md` (FR/NFR IDs) and `plan.md`; if not, the PR updates them.
- AI guardrails in `CLAUDE.md` respected; prompt changes include an eval run.
- UI: all strings localised (kn/en/te/ta), tokens used, axe clean, 360 px screenshot attached.

## Important vs nit
- **Important:** anything in Pass 2; wrong behaviour; missing test for new logic; broken offline path; inaccessible control.
- **Nit:** naming, formatting, small refactors. Max 3 nits per review; formatting is handled by `dotnet format` / ESLint.

## Skip
Generated files (migrations' designer files, lockfiles), `evals/results/`, `docs/hackathon/`.
