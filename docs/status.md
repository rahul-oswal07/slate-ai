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
- [ ] Monika: accept `docs/intent.md`; decide D-01, D-02, D-16 in `docs/spec.md` §8
- [ ] Together: accept D-05, D-10, D-11, D-12, D-13; mark Accepted in the decision log
- [ ] Book education mentor (Monika) and technical mentor (Rahul)

## Next session (Day 1, Mon 19 Oct)

T-01–T-04 done. Staging live: https://slateai-staging.azurewebsites.net (prod app exists; first `v*` tag deploys it). Next: T-05 (curriculum graph, needs D-01/T-22 from Monika), T-06 (attendance API) from `docs/tasks.md`: solution + API skeleton (.NET 10, EF Core, Postgres, `/health`, seed), React/Vite PWA skeleton with i18n, Dockerfile + docker-compose, deploy workflow to Azure. Goal: live walking-skeleton URL by end of day. Use plan mode first; one branch per task.

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
