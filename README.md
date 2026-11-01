# Tappida Paatha · "The Missed Lesson"

Every absent child gets the lesson they missed, based on what their own teacher taught, in their home language: at home on the family phone, and again on the day they return.

Built by **Slate AI** (Monika Sisodia, Rahul Oswal) for the AI for Foundational Learning Hackathon, Problem 1C: student absenteeism.

> **Judges: two ways to try it, no sign-up.** The **guided demo** (recommended, about 5 minutes) has a whole school week ready and walks you through it. The **real app** is exactly what a teacher uses, ideally on your own phone. Both are below, with the sign-in details.
>
> **The demo and the real app are the same application** (same code, same screens, same AI). The only difference is the data: the guided demo runs on our **staging** server, where a fictional school week of test data is already set up for judges and the date is fixed so that week is always behind you. The real app runs on **production** on today's date, so you start from today's register.

| | |
|---|---|
| ⭐ Guided demo (recommended) | https://slateai-staging.azurewebsites.net/demo |
| 📱 Real app | https://slateai-prod.azurewebsites.net |
| 🎬 Video (3 min) | https://www.youtube.com/watch?v=lDvo7b93d4A |
| Status | Build stage submission, 31 Oct 2026 |

## At a glance

| | |
|---|---|
| **5 languages** | The teacher app and the family pages work in Kannada, Telugu, Tamil, Hindi and English. Each child's catch-up is in their own home language. |
| **AI that shows its work** | 5 versioned Claude prompts (lesson mapping from voice or photo, choosing what to teach, script writing, verifying, lesson recap) plus Sarvam speech. **Under the hood** shows every call live: model, prompt version, tokens, the check's result and which children it was about. |
| **Measured, not claimed** | Latest eval runs: lesson mapping **13/13**, choosing what to teach **10/10**, scripts **14/14** first pass, verifier **8/8**, lesson recap **13/15**. Dated results in [`evals/results/`](evals/results/). |
| **Safe by design, enforced in code** | No child or teacher names reach an AI provider. Every number a family sees is computed in code. Nothing reaches a parent until the teacher approves it. Each rule has tests ([For judges](#for-judges)). |
| **Built with the people who will use it** | 2 government-school teachers, 2 parents, 2 mentor calls; 9 findings logged with what changed ([`user-testing.md`](docs/user-testing.md)). |
| **Engineered to last** | 605 API tests and 347 web tests run on every PR; attendance and voice notes work offline and sync later; one container on Azure. |

## Two ways to test

| | ⭐ 1. Guided demo (recommended) | 📱 2. Real app |
|---|---|---|
| Server | Staging, with test data set up for judges | Production |
| Open | https://slateai-staging.azurewebsites.net/demo | https://slateai-prod.azurewebsites.net, on your phone or a desktop browser |
| Sign-in | Automatic, as the class teacher | Mobile number ([table below](#login-credentials)) and the PIN on **slide 14 of the submitted deck** |
| Data | A fictional Grade 3 class (36 children) with a full week ready: the date is **fixed at Sat 31 Oct 2026**, so Monday to Friday's absences, lessons and evening messages are already there | The same fictional class on **today's real date** |
| What you see | The teacher's phone and a family's phone side by side, with 6 steps on screen | Exactly what a teacher sees. Families get their messages after 6 pm IST, as in a real school |
| Time | About 5 minutes | About 10 minutes |

### 1. Guided demo (recommended)

Open https://slateai-staging.azurewebsites.net/demo. **EN** switches to English; **Reset demo** starts the story again. The same 6 steps are on screen:

1. **Today** (teacher's phone): the simulated SATS attendance shows only Ananya (roll 1) absent. She has been away all week. Arun is back today after missing Thursday and Friday.
2. **Record today's lesson** in Kannada ([samples below](#sample-inputs)), or pick it from the list, and confirm it. The next lesson in the book after Friday's is 3-digit addition with carrying (sample 1).
3. **Tonight's message:** Ananya's family's phone already has an evening message in Kannada for each day this week. Tap **Send messages now** for tonight's (in a school it goes by itself at 6 pm), open it and pick a reason.
4. **Approve:** tap *Make this week's catch-ups*. You get four, one per home language: Kannada for Ananya (five missed lessons squeezed into the ones that matter for next week), Telugu for Ravi (Mon–Tue), Hindi for Rohan (Wed) and Tamil for Arun (Thu–Fri). Listen, read the Kannada summary and **Why this**, tick, approve, then tap **Send messages now**.
5. **The family's side:** on Ananya's family's phone, open the catch-up, listen, do the activity and tap a picture.
6. **Back on the teacher's phone:** **Back today** has Arun's buddy card, **Week** shows how fast children came back, the reasons and the attendance stars, and **Under the hood** lists every AI call.

### 2. Real app

1. Open https://slateai-prod.azurewebsites.net on your phone (or a desktop browser). Your browser can install it as an app (*Add to Home screen*).
2. Sign in with a teacher's mobile number from the [table below](#login-credentials) and the PIN on slide 14 of the submitted deck. The first time a login is used, a short welcome asks for a 10-second greeting that families hear before each catch-up.
3. **Today:** the simulated SATS attendance arrives at 12:00 (retry at 2 pm). Before then, or if it's wrong, mark or correct it yourself.
4. **Record the lesson** by voice or with a photo of the textbook page, and confirm the match.
5. **Approve the evening message** once (Today → tonight's card → *Review and approve*). After 6 pm IST it goes to each absent child's family with today's lesson and page.
6. **Approve:** *Make this week's catch-ups*, review one, try **Simpler** or **Shorter**, then approve. Approved catch-ups go out after 6 pm.
7. Explore **Missed lessons**, **Back today**, **Week** and **Settings** (your greeting, the evening message).

To see what the families received, open https://slateai-prod.azurewebsites.net/demo. It's the same class, and its family phone shows Rohan's family's messages (WhatsApp is simulated in this build).

### Login credentials

| Server | Role | Name | Mobile number | PIN | Notes |
|---|---|---|---|---|---|
| Production (real app) | Class teacher | Shobha | `9000000000` | deck, slide 14 | Families see her name and greeting |
| Production (real app) | Co-teacher | Savitha | `9123456789` | deck, slide 14 | Same class: several teachers can share one class |
| Production (real app) | Co-teacher | Monika | `9123456788` | deck, slide 14 | Same class |
| Staging (guided demo) | Class teacher | Shobha | `9000000000` | deck, slide 14 | Not needed for `/demo`, which signs you in by itself; for `/login` on staging |
| Staging (guided demo) | Co-teacher | Savitha | `9123456789` | deck, slide 14 | |
| Staging (guided demo) | Co-teacher | Monika | `9123456788` | deck, slide 14 | |
| Both | Family | — | — | none | Families never sign in: each link carries its own token (`/p/<token>`) |
| — | Admin | — | — | — | Not needed for judging and not switched on in production |

The PINs are on **slide 14 of the submitted deck**, not in this public repo. All three teachers use the same PIN, on staging and production. Enter the 10-digit number without `+91`. Five wrong PINs lock that login for 15 minutes.

### Sample inputs

1. **Kannada voice note** (Today → tap the mic). Read one of these aloud, then check the match and confirm it (or tap **Change**). Both are cases from our lesson-mapping evals, which matched them correctly on the latest run.

   | Lesson | Read aloud | What it says | Expect |
   |---|---|---|---|
   | A. Addition with carrying (the next lesson in the guided demo) | ಇವತ್ತು ಮೂರಂಕಿ ಸಂಖ್ಯೆಗಳ ಸಂಕಲನ ಮಾಡಿದೆವು, ದಶಕ ವರ್ಗಾವಣೆ ಜೊತೆ. 456 plus 278, ಬಿಡಿ ಸ್ಥಾನದಲ್ಲಿ 14 ಬಂತು, ಒಂದು ಹತ್ತು carry ಮಾಡಿದೆವು. | "Today we did 3-digit addition with carrying. 456 plus 278: 14 in the ones place, so we carried one ten." | *3-digit addition with carrying* |
   | B. Subtraction with borrowing | ಇವತ್ತು ಕಳೆಯುವಿಕೆ, borrowing ಜೊತೆ. 523 minus 168. ಬಿಡಿಯಲ್ಲಿ 3 ರಿಂದ 8 ಕಳೆಯಲು ಆಗಲ್ಲ, ಹತ್ತರ ಸ್ಥಾನದಿಂದ ಒಂದು ಹತ್ತು ಎರವಲು ತಗೊಂಡೆವು. | "Today, subtraction with borrowing. 523 minus 168. In the ones we can't take 8 from 3, so we borrowed one ten from the tens place." | *3-digit subtraction with borrowing* |

   If you don't read Kannada, pick the lesson from the list instead. Children's names in a voice note are replaced by `{child}` before anything reaches the AI.
2. **Photo of the lesson** (Today → *Add a photo of the page* → *Match from photo*), with or instead of the voice note: download [`sub-borrow-page.png`](evals/cases/images/sub-borrow-page.png) (expect *3-digit subtraction with borrowing*) or [`add-word-page.png`](evals/cases/images/add-word-page.png) (expect *Addition word problems*). These are synthetic textbook pages from our evals. The photo is only used to find the lesson; it is never sent to families.
3. **Choosing what to teach** (Approve → *Make this week's catch-ups*): open the catch-up of a child who missed several lessons (in the guided demo, Ananya, who missed all five) and read **Why this** to see which lessons were kept for next week and which were left out. Then tap **Simpler** or **Shorter** to get a new version.
4. **The family's answer:** on the family's phone, pick a reason on the evening message and tap a picture on the catch-up. **Missed lessons** on the teacher's phone then shows how the child did, and **Week** shows the reason the family gave.
5. **Language switch:** the language buttons at the top of the teacher app switch it to ಕನ್ನಡ, తెలుగు, தமிழ், हिन्दी or English. Children's catch-ups stay in their own home language.

### Limits you will see

- Only Grade 3 maths is loaded: the whole textbook, Parts 1 and 2, as 60 lessons in 12 chapters.
- WhatsApp and the SATS attendance feed are simulated: messages appear on the demo's family phone, and the demo's **Bring in today's attendance** plays the SATS feed.
- Each server has one class for everyone. If someone else uses it at the same time you may see their changes; on the guided demo, **Reset demo** starts again.
- The AI calls are real, so catch-ups take a moment to write. Voice notes are in Kannada, the teacher's language.
- Telugu, Tamil and Hindi text are drafts waiting for native review.

## For judges

Where to check how AI is used. Each row links to the files that prove it.

| What to check | Where |
|---|---|
| Every AI call, live | the demo's **Under the hood** tab: step, which children it was about, model, prompt version, tokens and the check result, read from the `AiCalls` log ([`DemoEndpoints`](src/api/SlateAi.Api/Features/Demo/DemoEndpoints.cs)) |
| Every LLM call goes through one client and is logged | [`Ai/`](src/api/SlateAi.Api/Ai/): [`ILlmClient`](src/api/SlateAi.Api/Ai/ILlmClient.cs), [`LoggedLlmClient`](src/api/SlateAi.Api/Ai/LoggedLlmClient.cs), [`AiCallStore`](src/api/SlateAi.Api/Ai/AiCallStore.cs) · test: [`LoggedLlmClientTests`](tests/api/SlateAi.Api.Tests/Ai/LoggedLlmClientTests.cs) |
| Prompts, versioned | [`prompts/`](prompts/): [a1 lesson mapping](prompts/a1-lesson-mapping.md), [a2 compression](prompts/a2-compression.md), [a3 script](prompts/a3-script.md), [a4 check](prompts/a4-check.md), [a8 lesson recap](prompts/a8-lesson-recap.md) |
| Eval cases and results | [`evals/cases/`](evals/cases/), [`evals/results/`](evals/results/) (dated; latest per prompt) · method and scores: [`docs/quality.md`](docs/quality.md) §2 |
| What to teach is chosen by code; the LLM picks only from it | candidate set: [`CompressionPlanner`](src/api/SlateAi.Api/Features/CatchUps/Compression/CompressionPlanner.cs) · invalid output falls back to rules: [`A1Validator` tests](tests/api/SlateAi.Api.Tests/Features/Lessons/Voice/A1ValidatorTests.cs), [`A2Validator` tests](tests/api/SlateAi.Api.Tests/Features/CatchUps/A2ValidatorTests.cs), [`A3Validator` tests](tests/api/SlateAi.Api.Tests/Features/CatchUps/A3ValidatorTests.cs) |
| Numbers come from code, not the LLM | [`ProblemGenerator`](src/api/SlateAi.Api/Features/CatchUps/Practice/ProblemGenerator.cs) · [tests](tests/api/SlateAi.Api.Tests/Features/CatchUps/ProblemGeneratorTests.cs) |
| Verifier: arithmetic, language, length, key words, names | [`CodeChecks`](src/api/SlateAi.Api/Features/CatchUps/Verify/CodeChecks.cs), [`ContentCheck`](src/api/SlateAi.Api/Features/CatchUps/Verify/ContentCheck.cs) · tests: [`CodeChecksTests`](tests/api/SlateAi.Api.Tests/Features/CatchUps/CodeChecksTests.cs), [`ContentCheckTests`](tests/api/SlateAi.Api.Tests/Features/CatchUps/ContentCheckTests.cs) |
| Nothing reaches a parent without teacher approval | [`CatchUpApprovalDbTests`](tests/api/SlateAi.Api.Tests/Db/CatchUpApprovalDbTests.cs), [`EveningMessageDbTests`](tests/api/SlateAi.Api.Tests/Db/EveningMessageDbTests.cs), [`EveningEligibilityTests`](tests/api/SlateAi.Api.Tests/Features/EveningMessages/EveningEligibilityTests.cs) |
| No names sent to AI providers | [`NameScrubber`](src/api/SlateAi.Api/Features/Lessons/Voice/NameScrubber.cs) (names become `{child}` / `{teacher}`) · [tests](tests/api/SlateAi.Api.Tests/Features/Lessons/Voice/NameScrubberTests.cs) |
| User testing and mentor input | [`docs/user-testing.md`](docs/user-testing.md), [`docs/mentor-feedback-2026-10-19.md`](docs/mentor-feedback-2026-10-19.md), [`docs/mentor-feedback-2026-10-24.md`](docs/mentor-feedback-2026-10-24.md) |
| Decisions and why | [`docs/spec.md` §8 decision log](docs/spec.md#8-decision-log) |
| Rules for AI in this repo | [`CLAUDE.md`](CLAUDE.md) "AI guardrails" |

## How it works

1. The teacher checks the absentees from the SATS register and records a 20-second voice note in Kannada about what she taught (or takes a photo of the textbook page).
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
| `tests/api`, `src/web/e2e` | xUnit tests (fakes by default, plus a small Postgres suite); Playwright end-to-end + accessibility |
| `evals/` | Live evaluation cases and results for each Claude call |
| `prompts/` | Versioned prompt files |
| `data/` | Curriculum dependency graph, picture library manifest, seed data |
| `deploy/` | Dockerfile, docker-compose, hosting notes |
| `docs/` | SDLC artifacts: intent, spec, plan, tasks, quality, user testing, judging narrative |

## Local setup

Prerequisites: .NET 10 SDK, Node 22 LTS, Docker.

```bash
git clone https://github.com/rahul-oswal07/slate-ai.git && cd slate-ai
docker compose -f deploy/docker-compose.yml up -d db
dotnet run --project src/api/SlateAi.Api      # applies migrations + seeds demo class; http://localhost:5080
cd src/web && npm ci && npm run dev           # http://localhost:5173
```

**Quickest way:** `./scripts/run-local.sh` starts Postgres, the API (:5080) and the web dev server with hot reload (:5173), and stops them on Ctrl+C. Use `./scripts/run-local.sh --container` for the production-like container on :8080, or `API_PORT=… WEB_PORT=…` to use other ports.

Without API keys the app runs with fake AI and speech providers (`Ai__Provider=Fake`), which is enough for UI work.

On Apple Silicon, install the **arm64** .NET 10 SDK (`brew install --cask dotnet-sdk`); the x64 build won't run without Rosetta. Run `dotnet tool restore` once to get `dotnet-ef`.

**Colima instead of Docker Desktop:** `brew install colima docker docker-compose && colima start`. Docker is needed to run Postgres and the container locally, and for the small database test suite (`dotnet test --filter Category=Db`); `verify.sh` doesn't need it. Testcontainers needs Colima's socket:

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
| `Sarvam__SttModel` | no | Speech-to-text model (default `saaras:v4`) |
| `Sarvam__TtsModel` / `Sarvam__TtsSpeaker` | no | Text-to-speech model and voice (default `bulbul:v3` / `priya`); audio is re-encoded to Opus 24 kbps |
| `Storage__Mode` | no | `Local` (default) or `AzureBlob` |
| `Storage__AzureBlob__ConnectionString` | prod | Audio storage |
| `WhatsApp__Token`, `WhatsApp__PhoneNumberId` | stretch | WhatsApp Cloud API |
| `Demo__Enabled` | no | Enables `/demo`, reset and the demo SATS controls `POST /api/demo/sats-feed` (default `true`) |
| `Demo__Story` | no | `judges` (default) or `video`: the story the demo Reset writes (D-85; not on prod) |
| `Demo__Today` | no | Fixes the date the app treats as today, e.g. `2026-10-31` (D-88; staging only) |
| `VITE_API_BASE` | no | Front-end API base when not same-origin |

### API keys and other secrets

Secrets never go in Git: not in `appsettings*.json`, code, tests or PR descriptions. `.env.example` only lists variable names; .NET does **not** read `.env` files, so don't put keys there.

**Locally: .NET user-secrets.** They're stored outside the repo (`~/.microsoft/usersecrets/`) and loaded automatically in Development (`./scripts/run-local.sh`, `dotnet run`). Run these in a terminal; don't paste keys into chats or shared logs:

```bash
dotnet user-secrets set "Anthropic:ApiKey" "<key>" --project src/api/SlateAi.Api
dotnet user-secrets set "Sarvam:ApiKey" "<key>"    --project src/api/SlateAi.Api
dotnet user-secrets list --project src/api/SlateAi.Api      # shows values
dotnet user-secrets remove "Sarvam:ApiKey" --project src/api/SlateAi.Api
```

**Demo teacher sign-in.** The seeded class teacher signs in with phone `9000000000` and the PIN from `Demo:TeacherPin`; the co-teachers use `Demo:CoTeacherPin`. `appsettings.Development.json` sets PINs for local use only (like the local database password). Staging and prod read `Demo__TeacherPin` and `Demo__CoTeacherPin` from App Service settings; if they aren't set, those teachers can't sign in. The seeder hashes the PIN on first start and never overwrites an existing one.

**Azure (staging/prod): App Service app settings.** They're encrypted at rest and reach the app as environment variables (`__` = section separator). Setting them restarts the app:

```bash
az webapp config appsettings set -g rg-slateai -n slateai-staging --output none \
  --settings Anthropic__ApiKey="<key>" Sarvam__ApiKey="<key>"
```

**CI:** none needed; tests run with `Ai__Provider=Fake`. Live evals in Actions would use a repository secret `ANTHROPIC_API_KEY`.

Use separate Anthropic keys for local and Azure (revoke one without breaking the other) and set a spend limit per key in the Anthropic console.

## Commands

| Task | Command |
|---|---|
| Run the app locally | `./scripts/run-local.sh` (`--container` for the built image) |
| Verify everything before a PR | `./scripts/verify.sh` |
| API tests (no live AI, no Docker) | `dotnet test --filter "Category!=Eval&Category!=Db"` |
| Database tests (needs Docker; CI runs them) | `dotnet test --filter Category=Db` |
| Live AI evals | `dotnet test --filter Category=Eval` |
| Web unit tests | `cd src/web && npm test` |
| Web build (into the API's `wwwroot`) | `cd src/web && npm run build` |
| End-to-end + accessibility (after a build) | `cd src/web && npm run e2e` |
| Lint / format | `cd src/web && npm run lint` · `dotnet format` |
| Build container | `docker build -f deploy/Dockerfile -t slate-ai .` |
| API docs (Swagger UI) | http://localhost:5080/swagger (dev) · https://slateai-staging.azurewebsites.net/swagger |
| Run full container locally | `docker compose -f deploy/docker-compose.yml up --build` → http://localhost:8080 |

## Contributing

We work trunk-based with short-lived branches.

1. Pick an issue (task ID `T-xx` from `docs/tasks.md`) and move it to **Doing**.
2. Branch: `feat/T-07-lesson-mapping`, `fix/…`, `design/…`, `docs/…`.
3. Commit with Conventional Commits and the task ID: `feat(api): map transcript to competencies (T-07)`.
4. Run `./scripts/verify.sh`. Open a PR using the template; add screenshots for UI changes.
5. Review: Monika approves UI/copy and `intent.md`/`spec.md` changes; Rahul approves code, `CLAUDE.md`, `prompts/`. Squash-merge when CI is green.
6. Merges to `main` deploy to staging (https://slateai-staging.azurewebsites.net); tags `v*` deploy to production (https://slateai-prod.azurewebsites.net). Runbook: [`deploy/azure/README.md`](deploy/azure/README.md).

Review policy: [`REVIEW.md`](REVIEW.md). Conventions for Claude Code and humans: [`CLAUDE.md`](CLAUDE.md).

## Privacy

Demo data is fictional. Only first names, roll numbers, home language and parent numbers are stored; no child or teacher names are sent to AI providers; parents opt in; data can be deleted on request (DPDP Act 2023).

## Licence

[MIT](LICENSE).
