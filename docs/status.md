# Status and handoff

Read this first in any new session, then `CLAUDE.md`, `docs/intent.md`, `docs/spec.md`, `docs/plan.md`, `docs/tasks.md`.
Update it at the end of every working session (what changed, what's next, blockers).

## Current state — Sun 18 Oct 2026 (Day 0)

- Repo: https://github.com/rahul-oswal07/slate-ai (`main`, public). API skeleton in place (T-02): .NET 10, EF Core + Postgres, `/health`, demo seed of 1 teacher + 36 children.
- Committed: SDLC artifacts (intent, spec, plan, tasks, quality, judging narrative, hackathon requirements), CLAUDE.md, README, REVIEW.md, .gitignore, verify script, folder skeleton.
- Hackathon PDFs are in `docs/hackathon/` locally and git-ignored. Everything needed from them is in `docs/hackathon-requirements.md`.
- Submission target: **Sat 31 Oct, 18:00 IST** (hard deadline 1 Nov, 23:59).

## Pending on Day 0 (before build starts)

- [x] Rahul: move repo-setup files into `.github/` and `.claude/`, set CODEOWNERS usernames, delete `deploy/repo-setup/` (T-01)
- [x] Rahul: repo public, Monika is a collaborator, `main` protected (PR + required `verify` check), Project board created, issues #1–#38 for P0+P1 tasks (T-01)
- [ ] Rahul: Anthropic API key (hackathon credits), Sarvam API key, Azure resources (Central India: App Service B1 Linux, PostgreSQL Flexible B1ms, Storage)
- [x] Rahul: install .NET 10 SDK (arm64) and Docker (Colima) locally
- [ ] Monika: accept `docs/intent.md`; decide D-01, D-02, D-16 in `docs/spec.md` §8
- [ ] Together: accept D-05, D-10, D-11, D-12, D-13; mark Accepted in the decision log
- [ ] Book education mentor (Monika) and technical mentor (Rahul)

## Next session (Day 1, Mon 19 Oct)

T-01 and T-02 done. Start T-03 → T-04 from `docs/tasks.md`: solution + API skeleton (.NET 10, EF Core, Postgres, `/health`, seed), React/Vite PWA skeleton with i18n, Dockerfile + docker-compose, deploy workflow to Azure. Goal: live walking-skeleton URL by end of day. Use plan mode first; one branch per task.

## Open questions

- Real teachers reachable this week? (schools reportedly on Dasara break until ~21 Oct)
- Open-source licence (D-15, MIT proposed)

## Log

| Date | Who | What |
|---|---|---|
| 18 Oct | Rahul + Claude | Planning artifacts, repo setup, first commit and push |
| 18 Oct | Rahul + Claude | T-01: CODEOWNERS, repo public, issues per P0/P1 task, Project board, branch protection |
| 18 Oct | Rahul + Claude | T-02: API skeleton, Initial migration (Teacher/Class/Child), idempotent + concurrency-safe demo seed, `/health`, 5 integration tests (Testcontainers). xunit.v3 pinned to 3.x to keep VSTest `--filter` syntax |
