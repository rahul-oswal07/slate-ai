# Status and handoff

Read this first in any new session, then `CLAUDE.md`, `docs/intent.md`, `docs/spec.md`, `docs/plan.md`, `docs/tasks.md`.
Update it at the end of every working session (what changed, what's next, blockers).

## Current state — Sun 18 Oct 2026 (Day 0)

- Repo: https://github.com/rahul-oswal07/slate-ai (`main`). No application code yet.
- Committed: SDLC artifacts (intent, spec, plan, tasks, quality, judging narrative, hackathon requirements), CLAUDE.md, README, REVIEW.md, .gitignore, verify script, folder skeleton.
- Hackathon PDFs are in `docs/hackathon/` locally and git-ignored. Everything needed from them is in `docs/hackathon-requirements.md`.
- Submission target: **Sat 31 Oct, 18:00 IST** (hard deadline 1 Nov, 23:59).

## Pending on Day 0 (before build starts)

- [ ] Rahul: move `deploy/repo-setup/*` into `.github/` and `.claude/` (commands in `deploy/repo-setup/README.md`), set CODEOWNERS usernames, delete `deploy/repo-setup/`
- [ ] Rahul: invite Monika, protect `main` (PR + `ci` check), create Project board
- [ ] Rahul: Anthropic API key (hackathon credits), Sarvam API key, Azure resources (Central India: App Service B1 Linux, PostgreSQL Flexible B1ms, Storage)
- [ ] Rahul: install .NET 10 SDK and Docker locally
- [ ] Monika: accept `docs/intent.md`; decide D-01, D-02, D-16 in `docs/spec.md` §8
- [ ] Together: accept D-05, D-10, D-11, D-12, D-13; mark Accepted in the decision log
- [ ] Book education mentor (Monika) and technical mentor (Rahul)

## Next session (Day 1, Mon 19 Oct)

Start T-01 → T-04 from `docs/tasks.md`: solution + API skeleton (.NET 10, EF Core, Postgres, `/health`, seed), React/Vite PWA skeleton with i18n, Dockerfile + docker-compose, deploy workflow to Azure. Goal: live walking-skeleton URL by end of day. Use plan mode first; one branch per task.

## Open questions

- Real teachers reachable this week? (schools reportedly on Dasara break until ~21 Oct)
- Open-source licence (D-15, MIT proposed)

## Log

| Date | Who | What |
|---|---|---|
| 18 Oct | Rahul + Claude | Planning artifacts, repo setup, first commit and push |
