# Tappida Paatha · "The Missed Lesson"

Every absent child gets the lesson they missed, based on what their own teacher taught, in their home language: at home on the family phone, and again on the day they return.

Built by **Slate AI** (Monika I, Rahul Oswal) for the AI for Foundational Learning Hackathon, Problem 1C: student absenteeism.

> **Judges:** open the demo link, choose **Try the demo**, and follow the 5 steps on screen. No sign-up, no WhatsApp needed. Use the **EN** toggle for English. Where AI is used in the code: `src/api/SlateAi.Api/Ai/`, `prompts/`, `evals/`.

| | |
|---|---|
| Demo | _link added on submission_ |
| Video (3 min) | _link added on submission_ |
| Status | Build stage, in development |

## How it works

1. The teacher marks absentees and records a 20-second voice note in Kannada about what she taught.
2. Claude maps the note to textbook competencies and logs it against each absent child.
3. For a child who missed several lessons, Claude keeps only what blocks next week's lessons and writes a 90-second home-language catch-up, a household-object activity and a 3-picture check.
4. The teacher approves once per lesson. The parent opens a link, taps Listen, and the child taps a picture to answer.
5. The teacher sees who caught up, gets a buddy card for each returning child, and a weekly pacing nudge.

Product and design detail: [`docs/intent.md`](docs/intent.md) · [`docs/spec.md`](docs/spec.md) · [`docs/plan.md`](docs/plan.md)

## Architecture

```
React PWA (teacher) ──┐                      ┌── Claude API (mapping, compression, scripts, checks)
Parent web page ──────┼── SlateAi.Api (.NET) ┼── Sarvam AI (speech-to-text, text-to-speech)
                      │   serves the SPA     ├── PostgreSQL
                      └──────────────────────┴── Blob storage (audio)
```

| Path | Contents |
|---|---|
| `src/api/SlateAi.Api` | .NET 10 Minimal API; `Features/` slices, `Ai/` (all LLM calls), `Speech/`, `Curriculum/`, `Data/` |
| `src/web` | React + Vite + TypeScript PWA (teacher app, parent page, demo shell) |
| `tests/api`, `tests/e2e` | xUnit unit/integration tests; Playwright end-to-end + accessibility |
| `evals/` | Live evaluation cases and results for each Claude call |
| `prompts/` | Versioned prompt files |
| `data/` | Curriculum dependency graph, picture library manifest, seed data |
| `deploy/` | Dockerfile, docker-compose, hosting notes |
| `docs/` | SDLC artifacts: intent, spec, plan, tasks, quality, user testing, judging narrative |

## Local setup

Prerequisites: .NET 10 SDK, Node 22 LTS, Docker.

```bash
git clone <repo-url> slate-ai && cd slate-ai
cp .env.example .env                          # fill in keys (see below)
docker compose -f deploy/docker-compose.yml up -d db
dotnet run --project src/api/SlateAi.Api      # applies migrations + seeds demo class; http://localhost:5080
cd src/web && npm ci && npm run dev           # http://localhost:5173
```

Without API keys the app runs with fake AI and speech providers (`Ai__Provider=Fake`), which is enough for UI work.

On Apple Silicon, install the **arm64** .NET 10 SDK (`brew install --cask dotnet-sdk`); the x64 build won't run without Rosetta. Run `dotnet tool restore` once to get `dotnet-ef`.

**Colima instead of Docker Desktop:** `brew install colima docker docker-compose && colima start`. Integration tests use Testcontainers, which needs to find Colima's socket:

```bash
export DOCKER_HOST=unix://$HOME/.colima/default/docker.sock
export TESTCONTAINERS_DOCKER_SOCKET_OVERRIDE=/var/run/docker.sock
```

### Environment variables

| Variable | Required | Purpose |
|---|---|---|
| `ConnectionStrings__Db` | yes | PostgreSQL connection string |
| `Anthropic__ApiKey` | for real AI | Claude API key |
| `Ai__Provider` | no | `Claude` (default) or `Fake` |
| `Ai__Models__Script` / `__Mapping` / `__Check` | no | Override model IDs |
| `Sarvam__ApiKey` | for real speech | Sarvam AI key |
| `Storage__Mode` | no | `Local` (default) or `AzureBlob` |
| `Storage__AzureBlob__ConnectionString` | prod | Audio storage |
| `WhatsApp__Token`, `WhatsApp__PhoneNumberId` | stretch | WhatsApp Cloud API |
| `Demo__Enabled` | no | Enables `/demo` and reset (default `true`) |
| `VITE_API_BASE` | no | Front-end API base when not same-origin |

Secrets never go in Git. Use `.env` locally, `dotnet user-secrets` if you prefer, and GitHub/Azure secrets in CI and production.

## Commands

| Task | Command |
|---|---|
| Verify everything before a PR | `./scripts/verify.sh` |
| API tests (no live AI) | `dotnet test --filter "Category!=Eval"` |
| Live AI evals | `dotnet test --filter Category=Eval` |
| Web unit tests | `cd src/web && npm test` |
| End-to-end + accessibility | `cd src/web && npx playwright test` |
| Lint / format | `cd src/web && npm run lint` · `dotnet format` |
| Build container | `docker build -f deploy/Dockerfile -t slate-ai .` |

## Contributing

We work trunk-based with short-lived branches.

1. Pick an issue (task ID `T-xx` from `docs/tasks.md`) and move it to **Doing**.
2. Branch: `feat/T-07-lesson-mapping`, `fix/…`, `design/…`, `docs/…`.
3. Commit with Conventional Commits and the task ID: `feat(api): map transcript to competencies (T-07)`.
4. Run `./scripts/verify.sh`. Open a PR using the template; add screenshots for UI changes.
5. Review: Monika approves UI/copy and `intent.md`/`spec.md` changes; Rahul approves code, `CLAUDE.md`, `prompts/`. Squash-merge when CI is green.
6. Merges to `main` deploy to staging; tags `v*` deploy to production.

Review policy: [`REVIEW.md`](REVIEW.md). Conventions for Claude Code and humans: [`CLAUDE.md`](CLAUDE.md).

## Privacy

Demo data is fictional. Only first names, roll numbers, home language and parent numbers are stored; no child or teacher names are sent to AI providers; parents opt in; data can be deleted on request (DPDP Act 2023).

## Licence

To be confirmed before submission (MIT proposed). See `LICENSE`.
