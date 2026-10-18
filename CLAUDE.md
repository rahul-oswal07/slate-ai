# CLAUDE.md — Tappida Paatha (slate-ai)

Hackathon MVP: absent children get a home-language catch-up of the lesson they missed. Read `docs/intent.md` → `docs/spec.md` → `docs/plan.md` before changing behaviour. Task IDs (T-xx) are in `docs/tasks.md`; requirement IDs (FR-xx, NFR-xx) in `docs/spec.md`.

## Commands

```bash
docker compose -f deploy/docker-compose.yml up -d db     # PostgreSQL on :5432
dotnet run --project src/api/SlateAi.Api                 # API + built SPA on :5080
cd src/web && npm ci && npm run dev                      # Vite dev server on :5173 (proxies /api)
./scripts/verify.sh                                      # build + all unit/integration/web tests — run before saying "done"
dotnet test --filter "Category!=Eval"                    # API tests without live AI
dotnet test --filter Category=Eval                       # live Claude evals (costs credits; only when asked or after prompt changes)
cd src/web && npx playwright test                        # e2e + axe
dotnet ef migrations add <Name> -p src/api/SlateAi.Api   # new migration
```

## Architecture

- One container: `SlateAi.Api` (.NET 10 Minimal API) serves `/api/*` and the built React SPA from `wwwroot`.
- Vertical slices in `src/api/SlateAi.Api/Features/<Feature>/` (endpoints, request/response records, handler). No generic repositories; use `AppDbContext` directly.
- **All LLM calls go through `Ai/ILlmClient`** and live in `Ai/`. Prompts are files in `prompts/` with a version header. Every call is logged to `AiCall`.
- Deterministic logic stays in code, never the LLM: attendance, ledger, candidate set, picture scoring, arithmetic, buddy selection, pacing numbers.
- Curriculum graph is loaded from `data/curriculum/*.json`; competency IDs look like `M3.ADD.CARRY`.
- Front end: feature folders in `src/web/src/features/`; server state via TanStack Query; offline writes via the Dexie outbox in `src/offline/`; all strings via i18next (`kn`, `en`, `te`, `ta`).

## Conventions

- C#: nullable enabled, file-scoped namespaces, `record` for DTOs, async all the way, `CancellationToken` on every endpoint. Config via `IOptions<T>`; secrets only from environment/user-secrets.
- TypeScript: strict mode, function components, no `any`. Tailwind classes use design tokens from `src/styles/tokens.css`; no hard-coded colours.
- Write endpoints accept `clientRequestId` and are idempotent.
- No user-facing string without keys in all four locale files.
- Commits: Conventional Commits with task ID, e.g. `feat(api): compression validator (T-08)`.

## AI guardrails (non-negotiable)

1. Never send child names, parent numbers or teacher names to Claude, Sarvam or WhatsApp templates other than the final send. Use IDs and `{child}` / `{teacher}` placeholders.
2. LLM output is parsed with a schema and validated; invalid IDs or targets fall back to rules, never pass through.
3. Nothing reaches a parent unless `CatchUpPackage.Status == Approved` (enforced in the API, covered by a test).
4. Numbers shown to users are computed in code; the LLM writes wording around slots.
5. Model IDs come from `Ai:Models:*` config. Don't hard-code them.
6. Changing a prompt → bump its version and include an eval run in the PR.

## Testing expectations

- New logic ships with xUnit or Vitest tests. Endpoints get an integration test (Testcontainers Postgres, fake `ILlmClient`/speech).
- Bug fix: write the failing test first; don't edit the test to make it pass.
- UI change: attach screenshots (phone width 360 px) to the PR; run axe.
- Run `./scripts/verify.sh` and report its result before declaring a task complete.

## Collaboration

- Rahul owns code, `CLAUDE.md`, `.claude/`, `prompts/`. Monika owns `intent.md`, UX and copy in `spec.md`, `docs/design/`, `docs/user-testing.md`.
- Open decisions are tagged `[DECIDE: Monika]` / `[CONFIRM: Rahul]` in `spec.md` §8. Don't implement past an open gate; build behind a flag or ask.
- If implementation drifts from `plan.md`, update `plan.md` in the same PR.
- Use plan mode for anything touching more than 3 files; save accepted plans into the PR description.
- Branches: `feat|fix|design|docs/T-xx-slug`, short-lived, squash-merged into `main`.

## Recurring mistakes

Add a line here whenever Claude makes the same mistake twice.

- (none yet)
