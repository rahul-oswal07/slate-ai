# Status and handoff

Read this first in any new session, then `CLAUDE.md`, `docs/intent.md`, `docs/spec.md`, `docs/plan.md`, `docs/tasks.md`.
Update it at the end of every working session (what changed, what's next, blockers).

## Current state — Mon 19 Oct 2026 (Day 1)

- Repo: https://github.com/rahul-oswal07/slate-ai (`main`, public). Staging live: https://slateai-staging.azurewebsites.net (prod app exists; first `v*` tag deploys it).
- Done: T-01–T-04 (skeleton, CI, deploy), **T-06** attendance API + ledger query (#57). Ledger also computes absence spells and weekly return speed (mentor review; UI waits for D-20/D-24).
- Testing approach changed (#58): default tests use fakes and need no Docker; DB access sits behind per-feature stores (`IAttendanceStore`, `ILedgerStore`). A small Postgres suite (`Category=Db`) runs in CI (#59, open). See CLAUDE.md "Testing expectations".
- Education mentor call held 19 Oct: notes and proposed decisions D-20–D-25 in `docs/mentor-feedback-2026-10-19.md` (#55, #56).
- Monika's PRs #49–#53 merged.
- Submission target: **Sat 31 Oct, 18:00 IST** (hard deadline 1 Nov, 23:59).

## Open items

- [ ] Rahul: merge #59 (Db test suite)
- [ ] Rahul: Anthropic + Sarvam API keys into user-secrets (needed for T-07)
- [ ] Rahul: accept D-05, D-11, D-12 in `spec.md` decision log (D-11/D-12 gate T-07)
- [ ] Rahul: confirm D-18 / D-19 before T-11 / T-15
- [ ] Rahul: book technical mentor
- [ ] Monika: T-22 competency list (blocks T-05); add textbook page per competency if D-22 is accepted
- [ ] Monika: T-21 low-fi wireframes; T-20 recruit teachers + parents (Dasara break until ~21 Oct)
- [ ] Monika: decide D-20–D-25 (mentor review). D-21 conflicts with FR-08 weekly approval; see the mentor notes
- [ ] Add the mentor's 6 interview questions to the teacher interview script

## Next session (Day 2, Tue 20 Oct)

- T-05 curriculum JSON + graph service, as soon as T-22 lands.
- T-07 A1 lesson mapping (STT + Claude, 10 evals) once keys and D-11/D-12 are in. `Lesson` entity exists in minimal form from T-06; T-07 extends it and adds the capture endpoint.
- If D-20/D-22 are accepted: update `intent.md` / `spec.md` / `tasks.md` and fold the textbook-page homework into T-05/T-09.
- Use plan mode first; one branch per task.

## Local dev

- `./scripts/run-local.sh` runs Postgres + API (:5080) + web (:5173); `--container` runs the image on :8080.
- API keys: `dotnet user-secrets set "Anthropic:ApiKey" …` / `"Sarvam:ApiKey"` (see README). Not stored yet; nothing reads them until T-07.

## Open questions

- Real teachers reachable this week? (schools reportedly on Dasara break until ~21 Oct)
- Open-source licence (D-15, MIT proposed)

## Log

| Date | Who | What |
|---|---|---|
| 18 Oct | Rahul + Claude | Planning artifacts, repo setup, first commit and push |
| 18 Oct | Rahul + Claude | T-01: CODEOWNERS, repo public, issues per P0/P1 task, Project board, branch protection |
| 18 Oct | Rahul + Claude | T-02: API skeleton, Initial migration (Teacher/Class/Child), idempotent + concurrency-safe demo seed, `/health`, 5 integration tests (Testcontainers). xunit.v3 pinned to 3.x to keep VSTest `--filter` syntax |
| 18 Oct | Rahul + Claude | T-03: Vite/React/TS PWA skeleton, i18n with draft kn/te/ta strings (need Monika's review), placeholder tokens, API serves SPA, Vitest + Playwright/axe. D-10 accepted |
| 18 Oct | Rahul + Claude | T-04: Dockerfile (chiseled, non-root), compose app service, CI container smoke, `provision.sh` (idempotent) run on personal Free Trial sub, OIDC deploy workflow main→staging, v*→prod. D-13 accepted |
| 18 Oct | Rahul + Claude | Started local Postgres, API (fake AI), and Vite dev server; `/health` and the web app respond successfully |
| 18 Oct | Rahul + Claude | First deploy to staging green (image sha-6192a40). Fixed OIDC subject (GitHub immutable-ID format). GHCR package already public |
| 18 Oct | Rahul + Claude | API docs: built-in OpenAPI document + Swagger UI at `/swagger` (dev + staging; off on prod via `OpenApi:Enabled`) |
| 18 Oct | Rahul + Claude | Deploy smoke test now waits for `/health` to report the new build (`version`) on 3 consecutive polls; fixes false failures during App Service container swaps |
| 18 Oct | Monika + Claude | T-00: accepted gate decisions D-01 (Grade 3 maths unit), D-02 (Kannada UI; Telugu+Kannada tested, Tamil spot-checked), D-16 (3 teachers + 2 parents/proxies incl. Telugu). Unblocks T-22, T-20, T-05, T-07 |
| 18 Oct | Monika + Claude | Compared the external Build Kit (00–06) with the repo; adopted its design tokens, design-card template, copy rules, testing kit and roadmap; kept our stack and CLAUDE.md. PRs: #50 AAA tokens + system dark theme (T-23, D-04/D-17 accepted; fixed 1.5:1 border), #51 design cards + screen states + copy rules (T-21), #52 testing kit + Now/Next/Later roadmap (T-27), #53 spoken AI disclosure + parent stop (D-18/D-19 proposed). Merge order: #49 → #50 → #53; #51, #52 independent
| 18 Oct | Rahul + Claude | `scripts/run-local.sh` (one-command local run); `UserSecretsId` + README secrets guidance (user-secrets locally, App Service settings on Azure). Next session: review Monika's PRs #49–#53, then T-06 |
| 19 Oct | Rahul + Claude | Mentor session notes committed (#55); fixed real teacher name → `{teacher}` placeholder and flagged D-21 vs FR-08 conflict (#56). Reviewed the feedback: return speed folded into T-06; textbook-page homework and "bring it back" hook proposed for T-05/T-09/T-16 |
| 19 Oct | Rahul + Claude | T-06 (#57): `POST attendance` (full register per date, idempotent by `clientRequestId`, corrections replace, IST future-date check), `GET ledger` (missed lessons, absence spells, return speed), `Attendance`/`Lesson`/`ProcessedRequest` + migration. Fixed startup migration race with an advisory lock |
| 19 Oct | Rahul + Claude | Testing: no database in default tests, stores + fakes, CLAUDE.md updated (#58); small Postgres suite `Category=Db` run in CI (#59) |
