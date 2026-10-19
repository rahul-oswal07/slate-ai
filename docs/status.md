# Status and handoff

Read this first in any new session, then `CLAUDE.md`, `docs/intent.md`, `docs/spec.md`, `docs/plan.md`, `docs/tasks.md`.
Update it at the end of every working session (what changed, what's next, blockers).

## Current state — Sun 18 Oct 2026 (Day 0)

- Repo: https://github.com/rahul-oswal07/slate-ai (`main`, public). API skeleton in place (T-02): .NET 10, EF Core + Postgres, `/health`, demo seed of 1 teacher + 36 children. Web skeleton (T-03): React PWA with i18n (kn/en/te/ta), served by the API from `wwwroot`.
- Committed: SDLC artifacts (intent, spec, plan, tasks, quality, judging narrative, hackathon requirements), CLAUDE.md, README, REVIEW.md, .gitignore, verify script, folder skeleton.
- Hackathon PDFs are in `docs/hackathon/` locally and git-ignored. Everything needed from them is in `docs/hackathon-requirements.md`.
- Submission target: **Sat 31 Oct, 18:00 IST** (hard deadline 1 Nov, 23:59).

## Pending on Day 0 (before build starts)

- [x] Rahul: move repo-setup files into `.github/` and `.claude/`, set CODEOWNERS usernames, delete `deploy/repo-setup/` (T-01)
- [x] Rahul: repo public, Monika is a collaborator, `main` protected (PR + required `verify` check), Project board created, issues #1–#38 for P0+P1 tasks (T-01)
- [x] Azure resources (personal Free Trial subscription, Central India): `rg-slateai` with App Service B1 (staging + prod apps), PostgreSQL Flexible B1ms. Storage deferred to T-11
- [ ] Rahul: Anthropic API key (hackathon credits), Sarvam API key
- [x] Rahul: install .NET 10 SDK (arm64) and Docker (Colima) locally
- [ ] Monika: accept `docs/intent.md` (D-01, D-02, D-16 decided: PR #49)
- [ ] Together: accept D-05, D-10, D-11, D-12, D-13; mark Accepted in the decision log
- [ ] Book education mentor (Monika, by 22 Oct: validate pacing + buddy card) and technical mentor (Rahul)
- [ ] Rahul: review #50 (tokens, touches `src/web`) and confirm D-18 / D-19 in #53 before T-11 / T-15

## Next session (Day 1, Mon 19 Oct)

T-01–T-04 done. Staging live: https://slateai-staging.azurewebsites.net (prod app exists; first `v*` tag deploys it). Next: T-05 (curriculum graph, needs D-01/T-22 from Monika), T-06 (attendance API) from `docs/tasks.md`: solution + API skeleton (.NET 10, EF Core, Postgres, `/health`, seed), React/Vite PWA skeleton with i18n, Dockerfile + docker-compose, deploy workflow to Azure. Goal: live walking-skeleton URL by end of day. Use plan mode first; one branch per task.

## Waiting for review (from Monika, 18 Oct)

- #49 accept D-01, D-02, D-16 (T-00): unblocks T-05
- #50 AAA design tokens + dark theme (T-23)
- #51 design-card template, screen states, copy rules (T-21)
- #52 testing kit + Now/Next/Later roadmap (T-27)
- #53 propose D-18, D-19 (spoken AI disclosure, parent stop)

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
