# Tappida Paatha · "The Missed Lesson"

**When a child misses school, their family gets that day's lesson at home: as their own teacher taught it, in their home language, approved by the teacher.**

Built by **Slate AI** (Monika Sisodia, Rahul Oswal) for the AI for Foundational Learning Hackathon, Problem 1C: student absenteeism. Grand Finale, 18 Nov 2026.

> ### 👩‍⚖️ Judges: start with **[JUDGES.md](JUDGES.md)**
> It is a step-by-step test guide: the 5-minute guided path, sample inputs, sign-in details, every persona, and what is real or simulated.

| | |
|---|---|
| ⭐ Guided demo (two phones side by side) | https://slateai-staging.azurewebsites.net/demo |
| 📱 Real app (production) | https://slateai-prod.azurewebsites.net |
| 🔍 Under the hood (every AI call) | https://slateai-staging.azurewebsites.net/demo/ai |
| 🎬 Video (3 min) | https://www.youtube.com/watch?v=lDvo7b93d4A |
| 🔑 Sign-in | Shobha (class teacher) `9000000000` · Savitha `9123456789` · Monika `9123456788` (co-teachers); details in [JUDGES.md](JUDGES.md#5-sign-in-details). The PIN is sent to judges separately by email |
| 📲 A real WhatsApp message | Save your number against a child on https://slateai-staging.azurewebsites.net/admin/children: [how](JUDGES.md#get-the-link-on-your-own-whatsapp) |

---

## Contents

[The problem](#the-problem) · [What it does](#what-it-does) · [A day in the app](#a-day-in-the-app) · [Screens](#screens) · [How the AI is used](#how-the-ai-is-used) · [How the AI is kept safe](#how-the-ai-is-kept-safe) · [Quality and evidence](#quality-and-evidence) · [Architecture](#architecture) · [Local setup](#local-setup) · [Commands](#commands) · [Documentation map](#documentation-map) · [Contributing](#contributing) · [Privacy](#privacy)

---

## The problem

In Karnataka's government primary schools, a Grade 3 child who is absent for a day or two falls behind and rarely catches up. The teacher has 30–40 children and no time to re-teach. Most parents want to help but don't know what was taught, and many read little.

**Tappida Paatha** closes that gap with the least work for the teacher:
- one voice note a day about what she taught;
- one tap to approve what goes home.

## What it does

| | |
|---|---|
| 📋 **Knows who was away** | The day's register arrives from **SATS** (the state's attendance system; simulated here) at 9:00. The teacher can mark or correct it, offline too |
| 🎙️ **Knows what was taught** | The teacher records a **20-second voice note in Kannada**, takes a **photo of the textbook page**, or picks from the list. AI matches it to the exact lesson and page in **Karnataka Grade 3 Maths** (Nalikali, Kannada medium) |
| ✉️ **Today's message** for each absent child's family, in their **home language** (Kannada, Telugu, Tamil, Hindi or English) | **A spoken explanation, about a minute long**, built on how the textbook teaches that lesson · **a home activity** with sticks, leaves or coins · **a DIKSHA video** of how it's taught · **a picture question** the child answers (`>`, `<`, `=` or a sum) · the teacher's own note |
| ✅ **Teacher approves everything** | She reads and listens in Kannada, can edit any sentence, and approves. **Nothing reaches a family without her approval**, enforced in code and tests |
| 📲 **Families get it on WhatsApp** | A link, no app, no sign-in. They give a reason for the absence, listen, do the activity together; the child answers |
| 📈 **Answers come back** | Code scores the child's answer and gives a level; the teacher sees it on **Today** and in **Missed lessons** |
| 🔁 **Several days away** | The **weekly catch-up** keeps only the lessons that block next week's teaching: a 90-second home-language script, an activity and a 3-picture check |
| 🤝 **The return day** | A **buddy card** pairs each returning child with a classmate: 3 sums and the textbook words |
| 📊 **The week** | How fast children came back, the reasons families gave, attendance stars, a pacing nudge, and a **weekly star** message for children who came every day |

## A day in the app

```
 9:00  SATS register ──► Today: who is away
       Teacher's voice note / photo / pick ──► AI matches the textbook lesson ──► she confirms
       AI writes today's message (every absent family's language, voiced) ──► teacher checks, edits, approves
18:00  Evening send ──► family's WhatsApp link ──► reason · listen · activity · video · child answers
       Answer scored by code ──► teacher's Today and Missed lessons
 Week  Several days away ──► weekly catch-up · Return day ──► buddy card · Saturday ──► weekly star
```

## Screens

**Teacher app** (a PWA: installs on the phone, works offline for attendance and voice notes). It has five tabs:

| Tab | What she does there |
|---|---|
| **Today** | Register, today's lesson (voice, photo or list), today's message status, tonight's send, the children's answers. One "next step" button at the top |
| **Approve** | **Today's message**: listen, read, edit, approve and send. **Catch-ups**: the week's catch-ups with *Why this*, *Simpler*, *Shorter* |
| **Missed** | Every child's missed lessons, whether their family got them, and how the child did |
| **Back today** | A buddy card for each child returning today |
| **Week** | Return speed, reasons, attendance stars, the pacing nudge |
| ⚙️ Settings | Her voice greeting (families hear it first), the evening message and weekly star wording to approve, and **Reset today** in the demo class |

**Family pages** (`/p/<token>`, from the WhatsApp link): the teacher's greeting → *"Is everything OK?"* (the reason, once) → the explanation (voiced) → *Try this at home* → *Watch how it's taught* → *the child's turn* (the picture question) → *Well done!* "Stop these messages" is always there.

**Admin pages** (`/admin`): family phone numbers (two per family) and home languages, lesson video links, **lesson descriptions from the textbook**, teacher voice consent, greetings, lesson audio, and the demo's past-day switch.

**Demo shell** (`/demo`): the teacher's phone and a family's phone side by side, **Reset demo**, **Bring in today's attendance**, **Send messages now**, and **Under the hood**.

The teacher app and the family pages work in **ಕನ್ನಡ, తెలుగు, தமிழ், हिन्दी and English**.

## How the AI is used

Every call goes through one client, uses a **versioned prompt file** in [`prompts/`](prompts/), is **logged** (`AiCalls`), and shows on **Under the hood**.

| Step | What the AI does | Model | Prompt | What code does around it |
|---|---|---|---|---|
| **A1 · Lesson matching** | Kannada voice note (via Sarvam speech-to-text) and/or a textbook photo → the lesson(s) taught | Claude Sonnet | [`a1-lesson-mapping`](prompts/a1-lesson-mapping.md) v3 | Names scrubbed to `{child}` first; only curriculum IDs are accepted; low confidence → she picks |
| **A2 · Choosing what to teach** | For a child who missed several lessons: which ones block next week | Claude Sonnet | [`a2-compression`](prompts/a2-compression.md) | Code builds the candidate set from the prerequisite graph; the AI only chooses from it, with reasons |
| **A3 · Catch-up script** | 90-second home-language script, activity wording, key words | Claude Opus | [`a3-script`](prompts/a3-script.md) | All numbers made by code; checked for length, language, names, key words |
| **A4 · Checker** | Is the script on topic, warm, right for a Grade 3 child? | Claude Haiku | [`a4-check`](prompts/a4-check.md) | Plus code checks (arithmetic, length, script); one retry, else "needs review" |
| **A9 · Today's message** | The ~1-minute explanation and the home activity in every family's language, built on the lesson's textbook description and what she said in class | Claude Opus | [`a9-todays-message`](prompts/a9-todays-message.md) v5 · [`-sums`](prompts/a9-todays-message-sums.md) v2 · [`-lesson`](prompts/a9-todays-message-lesson.md) v2 | Picture question and answer chosen by code from the book's exercises; only allowed numbers; never the answer; her Kannada edits kept word for word |
| **Speech** | Speech-to-text (Kannada), text-to-speech for every message | Sarvam `saaras:v4`, `bulbul:v3` | — | An AI-voice notice plays first; clips are cached and shared |

Model IDs come from configuration (`Ai:Models:*`), never from code.

## How the AI is kept safe

These rules are in [`CLAUDE.md`](CLAUDE.md) and enforced in code with tests:

| Rule | Where it is enforced and tested |
|---|---|
| **No child or teacher names reach an AI provider** | [`NameScrubber`](src/api/SlateAi.Api/Features/Lessons/Voice/NameScrubber.cs) ([tests](tests/api/SlateAi.Api.Tests/Features/Lessons/Voice/NameScrubberTests.cs)); `{child}` / `{teacher}` placeholders; outputs containing a class name are refused |
| **Nothing reaches a parent without the teacher's approval** of that exact version | [`CatchUpApprovalDbTests`](tests/api/SlateAi.Api.Tests/Db/CatchUpApprovalDbTests.cs), [`EveningMessageDbTests`](tests/api/SlateAi.Api.Tests/Db/EveningMessageDbTests.cs), [`TodaysMessageDbTests`](tests/api/SlateAi.Api.Tests/Db/TodaysMessageDbTests.cs) |
| **Every number a family sees comes from code** | [`ProblemGenerator`](src/api/SlateAi.Api/Features/CatchUps/Practice/ProblemGenerator.cs), [`ExerciseAnswers`](src/api/SlateAi.Api/Curriculum/ExerciseAnswers.cs); A9 output with a number not allowed is refused ([`TodaysMessageWriter`](src/api/SlateAi.Api/Features/TodaysMessage/TodaysMessageWriter.cs)) |
| **AI output is parsed and validated; invalid output falls back to rules** | [`A1Validator`](tests/api/SlateAi.Api.Tests/Features/Lessons/Voice/A1ValidatorTests.cs), [`A2Validator`](tests/api/SlateAi.Api.Tests/Features/CatchUps/A2ValidatorTests.cs), [`A3Validator`](tests/api/SlateAi.Api.Tests/Features/CatchUps/A3ValidatorTests.cs) tests; [`CodeChecks`](src/api/SlateAi.Api/Features/CatchUps/Verify/CodeChecks.cs) |
| **Deterministic logic stays in code** | Attendance, the missed-lesson ledger, the candidate set, picture scoring and levels, buddy pairs, pacing: all code |
| **Every AI call is logged** | [`LoggedLlmClient`](src/api/SlateAi.Api/Ai/LoggedLlmClient.cs) ([tests](tests/api/SlateAi.Api.Tests/Ai/LoggedLlmClientTests.cs)) → Under the hood |
| **A message that has gone can't be quietly contradicted** | Once tonight's message has gone, the day's lesson can't change (D-124) |

## Quality and evidence

**Live evals** (real Claude, the shipped prompts; dated results in [`evals/results/`](evals/results/), method in [`docs/quality.md`](docs/quality.md)):

| Prompt | Latest run | Result |
|---|---|---|
| A1 lesson matching (voice and photo) | 13 Nov | **16/16** |
| A2 choosing what to teach | 1 Nov | **10/10** |
| A3 catch-up script (with A4 and code checks) | 1 Nov | **13/14** first pass, 14/14 after one retry |
| A4 checker | 25 Oct | **11/11** |
| A9 today's message (compare, sums, every other lesson) | 16 Nov | **38/38** first pass; no concept gives away the picture's answer |

**Tests** run on every pull request in CI:
- 1,057 API tests, with fakes and no database;
- 24 PostgreSQL tests (approval, deletion, offline replay, migrations and seed, the day reset);
- 579 web component tests;
- 12 end-to-end tests with accessibility checks (axe).

**Built with users:** 2 government-school teachers, 2 parents and our mentors; findings and what changed are in [`docs/user-testing.md`](docs/user-testing.md).

**The textbook:** all 61 lessons of Karnataka Grade 3 Maths (Parts 1 and 2, plus the Nalikali bridge-course lesson). Each has its page, Kannada key words, a description and worked examples read from the book ([`data/curriculum/`](data/curriculum/), [`data/lesson-content/`](data/lesson-content/)).

## Architecture

```
Teacher PWA (React) ──┐                         ┌── Claude API  (A1, A2, A3, A4, A9)
Family pages ─────────┼── SlateAi.Api (.NET 10) ┼── Sarvam AI   (speech-to-text, text-to-speech)
Demo shell, admin ────┘   one container, serves ├── WhatsApp Cloud API (Meta)
                          the SPA from wwwroot  └── PostgreSQL (data and audio)
```

- **One container** on Azure App Service: the API serves `/api/*` and the built React app.
- **Vertical slices** in `src/api/SlateAi.Api/Features/<Feature>/`. Each has endpoints, a handler and a small store behind an interface, so endpoints are tested with fakes.
- **All AI calls** live in `Ai/`, with the prompts in `prompts/`. **Background jobs** handle the 6 pm send, voice-note matching, today's message drafting and WhatsApp delivery with retries.
- **Offline:** attendance and voice notes go through a Dexie outbox on the phone and are sent when the network is back.
- **Deploy:** merges to `main` go to staging, and `v*` tags go to production (GitHub Actions, Azure OIDC). What runs where: [`docs/status.md`](docs/status.md).

| Path | Contents |
|---|---|
| `src/api/SlateAi.Api` | .NET 10 Minimal API: `Features/`, `Ai/`, `Speech/`, `Curriculum/`, `Data/` (EF Core, migrations) |
| `src/web` | React + Vite + TypeScript PWA: teacher app, family pages, demo shell, admin pages |
| `tests/api`, `src/web/e2e` | xUnit (fakes by default, plus a small Postgres suite), Vitest, Playwright + axe |
| `prompts/`, `evals/` | Versioned prompts; eval cases and dated results |
| `data/` | Curriculum graph, lesson descriptions, DIKSHA links, seed data |
| `deploy/` | Dockerfile, docker-compose, Azure runbook |
| `docs/` | Intent, spec, plan, decisions, a spec and status per screen, one file per task: see [`docs/README.md`](docs/README.md) |

## Local setup

Prerequisites: .NET 10 SDK, Node 22 LTS, Docker (or Colima).

```bash
git clone https://github.com/rahul-oswal07/slate-ai.git && cd slate-ai
./scripts/run-local.sh        # Postgres + API (:5080) + web dev server (:5173); Ctrl+C stops all
```

Or step by step:

```bash
docker compose -f deploy/docker-compose.yml up -d db
dotnet run --project src/api/SlateAi.Api      # applies migrations, seeds the demo class; http://localhost:5080
cd src/web && npm ci && npm run dev           # http://localhost:5173 (proxies /api)
```

- **Without API keys**, set `Ai__Provider=Fake`: fake AI and speech, enough for UI work.
- `./scripts/run-local.sh --container` runs the production-like container on :8080.
- On Apple Silicon, install the **arm64** .NET 10 SDK, then run `dotnet tool restore` once for `dotnet-ef`.
- With **Colima**, also set `export DOCKER_HOST=unix://$HOME/.colima/default/docker.sock` and `export TESTCONTAINERS_DOCKER_SOCKET_OVERRIDE=/var/run/docker.sock`.

### Configuration

| Variable | Purpose |
|---|---|
| `ConnectionStrings__Db` | PostgreSQL connection string (required) |
| `Ai__Provider` | `Claude` (default) or `Fake` |
| `Anthropic__ApiKey` | Claude API key |
| `Ai__Models__Mapping` / `__Compression` / `__Script` / `__Check` | Model IDs for each kind of call (Sonnet, Sonnet, Opus, Haiku by default) |
| `Sarvam__ApiKey` | Sarvam speech key; `Sarvam__SttModel`, `Sarvam__TtsModel`, `Sarvam__TtsSpeaker` override the defaults |
| `Delivery__Channel` | `demo` (default: messages appear on the demo family phone) or `whatsapp` |
| `WhatsApp__AccessToken`, `WhatsApp__PhoneNumberId` | WhatsApp Cloud API, when the channel is `whatsapp` |
| `Demo__Enabled` | `/demo`, Reset and the simulated SATS controls (default `true`) |
| `Demo__TeacherPin`, `Demo__CoTeacherPin` | PINs the seeder sets for the demo teachers on first start |

**Secrets never go in Git.**
- Locally, use .NET user-secrets: `dotnet user-secrets set "Anthropic:ApiKey" "<key>" --project src/api/SlateAi.Api`, and the same for `Sarvam:ApiKey`.
- On Azure, use App Service settings.
- CI needs no secrets: tests use the fakes.

## Commands

| Task | Command |
|---|---|
| Run locally | `./scripts/run-local.sh` |
| Everything before a PR (docs, build, all tests, web build) | `./scripts/verify.sh` |
| API tests without AI or Docker | `dotnet test --filter "Category!=Eval&Category!=Db"` |
| PostgreSQL tests (Docker) | `dotnet test --filter "Category=Db"` |
| Live AI evals (costs credits) | `dotnet test --filter "Category=Eval"` |
| Web tests · lint · build | `cd src/web && npm test` · `npm run lint` · `npm run build` |
| End-to-end + accessibility | `cd src/web && npx playwright test` |
| New migration | `dotnet ef migrations add <Name> -p src/api/SlateAi.Api` |
| API docs | http://localhost:5080/swagger · https://slateai-staging.azurewebsites.net/swagger |

## Documentation map

| Read | For |
|---|---|
| [`JUDGES.md`](JUDGES.md) | Testing the app as a judge |
| [`docs/intent.md`](docs/intent.md) → [`docs/spec.md`](docs/spec.md) → [`docs/plan.md`](docs/plan.md) | Why, what, how |
| [`docs/how-it-works.md`](docs/how-it-works.md) | The product in plain words |
| [`docs/decisions/`](docs/decisions/README.md) | Every decision (D-01 …) and why |
| [`docs/screens/`](docs/README.md) | A spec and status per screen |
| [`docs/quality.md`](docs/quality.md), [`evals/`](evals/) | Evals and test strategy |
| [`docs/user-testing.md`](docs/user-testing.md) | Teachers, parents and mentors we tested with |
| [`docs/database.md`](docs/database.md) | Schema |
| [`docs/whatsapp-setup.md`](docs/whatsapp-setup.md) | WhatsApp templates and setup |
| [`CLAUDE.md`](CLAUDE.md) | Conventions and AI guardrails for contributors and Claude Code |

## Contributing

We work trunk-based with short-lived branches.

1. Pick an issue (task `T-xx`, its file in `docs/tasks/`).
2. Branch: `feat|fix|docs/T-xx-slug`.
3. Commit with Conventional Commits and the task ID, e.g. `feat(api): … (T-xx)`.
4. Run `./scripts/verify.sh`. Open a PR with screenshots for UI changes. Squash-merge when CI is green.
5. Merges to `main` deploy to staging; `v*` tags deploy to production ([`deploy/azure/README.md`](deploy/azure/README.md)).

Review policy: [`REVIEW.md`](REVIEW.md).

## Privacy

- The demo data is fictional.
- Only first names, roll numbers, home language and the family's numbers are stored.
- No child or teacher names are sent to AI providers.
- Families opt in, can stop messages at any time, and a child's data can be deleted (DPDP Act 2023).

## Licence

[MIT](LICENSE).
