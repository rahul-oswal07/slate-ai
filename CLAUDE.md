# CLAUDE.md — Tappida Paatha (slate-ai)

Hackathon MVP: absent children get a home-language catch-up of the lesson they missed. **Start every session with `docs/status.md`** (project state, next tasks), then the map in `docs/README.md`, then the task's file (`docs/tasks/T-xx-*.md`) and the folders of the screens it touches (`docs/screens/<screen>/`, `docs/areas/<area>/`). Read `docs/intent.md` → `docs/spec.md` → `docs/plan.md` before changing behaviour. Requirement IDs (FR-xx, NFR-xx) are indexed in `docs/spec.md` §2 and kept in the owning screen's `spec.md`; decisions (D-xx) are in `docs/decisions/`.

## Commands

```bash
docker compose -f deploy/docker-compose.yml up -d db     # PostgreSQL on :5432
dotnet run --project src/api/SlateAi.Api                 # API + built SPA on :5080
cd src/web && npm ci && npm run dev                      # Vite dev server on :5173 (proxies /api)
./scripts/verify.sh                                      # build + all API/web tests — run before saying "done"
dotnet test --filter "Category!=Eval&Category!=Db"      # API tests without live AI or Docker
dotnet test --filter Category=Db                         # Postgres tests (needs Docker; CI runs them on every PR)
dotnet test --filter Category=Eval                       # live Claude evals (costs credits; only when asked or after prompt changes)
cd src/web && npx playwright test                        # e2e + axe
dotnet ef migrations add <Name> -p src/api/SlateAi.Api   # new migration
```

## Architecture

- One container: `SlateAi.Api` (.NET 10 Minimal API) serves `/api/*` and the built React SPA from `wwwroot`.
- Vertical slices in `src/api/SlateAi.Api/Features/<Feature>/` (endpoints, request/response records, handler, store). No generic repositories; a feature's store (`I<Feature>Store` + `AppDbContext` implementation) holds its DB access so endpoints can be tested with fakes.
- **All LLM calls go through `Ai/ILlmClient`** and live in `Ai/`. Prompts are files in `prompts/` with a version header. Every call is logged to `AiCall`.
- Deterministic logic stays in code, never the LLM: attendance, ledger, candidate set, picture scoring, arithmetic, buddy selection, pacing numbers.
- Curriculum graph is loaded from `data/curriculum/*.json`; competency IDs look like `M3.ADD.CARRY`.
- Database schema is documented in `docs/database.md`; update it in the same PR as any migration.
- Front end: feature folders in `src/web/src/features/`; server state via TanStack Query; offline writes via the Dexie outbox in `src/offline/`; all strings via i18next (`kn`, `en`, `te`, `ta`).

## Conventions

- C#: nullable enabled, file-scoped namespaces, `record` for DTOs, async all the way, `CancellationToken` on every endpoint. Config via `IOptions<T>`; secrets only from environment/user-secrets.
- TypeScript: strict mode, function components, no `any`. Tailwind classes use design tokens from `src/styles/tokens.css`; no hard-coded colours.
- Write endpoints accept `clientRequestId` and are idempotent.
- Endpoints are documented in OpenAPI (`/swagger` in dev/staging): use `TypedResults` and add `WithSummary`/`WithTags` so the docs stay useful.
- No user-facing string without keys in all four locale files.
- Commits: Conventional Commits with task ID, e.g. `feat(api): compression validator (T-08)`.

## AI guardrails (non-negotiable)

1. Never send child names, parent numbers or teacher names to Claude, Sarvam or WhatsApp templates other than the final send. Use IDs and `{child}` / `{teacher}` placeholders.
2. LLM output is parsed with a schema and validated; invalid IDs or targets fall back to rules, never pass through.
3. Nothing reaches a parent without teacher approval: a `CatchUpPackage` with `Status == Approved`, an evening message (FR-20, D-21) built from an Approved `MessageTemplate` whose slots are filled only by code from teacher-confirmed records, or today's message (D-91) that is Approved at its current `Version` (any edit clears the approval). All enforced in the API and covered by tests.
4. Numbers shown to users are computed in code; the LLM writes wording around slots.
5. Model IDs come from `Ai:Models:*` config. Don't hard-code them.
6. Changing a prompt → bump its version and include an eval run in the PR.

## Testing expectations

- New logic ships with xUnit or Vitest tests. Keep logic pure where possible (e.g. `Features/Ledger/LedgerBuilder.cs`) and unit-test it with in-memory data.
- **Default tests never touch a database** and run without Docker. Put each feature's DB access behind a small store interface (e.g. `IAttendanceStore`; not a generic repository) and test endpoints over HTTP with `FakeApi.Create(...)` + hand-written fakes (`tests/api/.../Infrastructure/FakeApi.cs`), plus fake `ILlmClient`/speech.
- **Db suite** (`tests/api/.../Db/`, `[Trait("Category", "Db")]` + `[Collection(DbCollection.Name)]`): only what a real Postgres can prove, each test on its own throwaway database. Keep it small: offline replay idempotency, store queries translating to SQL, migrations + seed, and when built the approval rule (guardrail 3, T-14) and child deletion (FR-15, T-19). Ask before adding anything else to it. Excluded from `verify.sh`; CI runs it.
- Bug fix: write the failing test first; don't edit the test to make it pass.
- UI change: attach screenshots (phone width 360 px) to the PR; run axe.
- **No new Playwright end-to-end tests for now** (Rahul, 22 Oct): cover new screens with Vitest component tests; keep the existing `src/web/e2e/` specs passing. Take PR screenshots from a local run and say in the PR when axe wasn't run on a new screen.
- Run `./scripts/verify.sh` and report its result before declaring a task complete.

## Collaboration

- Rahul owns code, `CLAUDE.md`, `.claude/`, `prompts/`. Monika owns `intent.md`, UX and copy in `spec.md` and in each `docs/screens/*/spec.md`, `docs/design/`, `docs/user-testing.md`.
- Open decisions are tagged `[DECIDE: Monika]` / `[CONFIRM: Rahul]` and have status **Proposed** in `docs/decisions/` (D-01 to D-99 in the frozen log, one file each after that). Don't implement past an open gate; build behind a flag or ask.
- If implementation drifts from `plan.md`, update `plan.md` in the same PR.
- At the end of each session, add a dated note to the task's file (what changed, what's left, blockers) and update the `status.md` of each screen or area whose behaviour changed. Touch `docs/status.md` only for a release, a change to a server or a change of priorities.
- Use plan mode for anything touching more than 3 files; save accepted plans into the PR description.
- Branches: `feat|fix|design|docs/T-xx-slug`, short-lived, squash-merged into `main`.

## Docs (so two people don't collide)

- One folder per screen (`docs/screens/`) and per cross-cutting area (`docs/areas/`), each with `spec.md` and `status.md`. One file per live task (`docs/tasks/`) and per new decision (`docs/decisions/`). The map and the rules are in `docs/README.md`.
- New task: next free `T-` number, a GitHub issue, a file from the template in `docs/tasks/README.md`. New decision: next free `D-` number, a file from the template in `docs/decisions/README.md`.
- Never add to `docs/archive/` or to the frozen log `docs/decisions/D-01-to-D-99.md` (only a status change there).
- Dated notes go in the task's file. A screen's `status.md` says what is true now, one fact per bullet, no dates.
- Don't renumber `docs/spec.md` sections or rename `docs/design/` cards: code comments cite them.
- `./scripts/check-docs.sh` (part of `verify.sh`) fails on a task or decision number used twice and on a broken relative link.

## Recurring mistakes

Add a line here whenever Claude makes the same mistake twice.

- (none yet)
