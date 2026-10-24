# Plan: Tappida Paatha MVP (Build stage)

| | |
|---|---|
| Stage | 3 · Build (with Test, Deploy, Maintain checkpoints) |
| Reads | `docs/intent.md`, `docs/spec.md` |
| Owners | Rahul (engineering), Monika (product, design, user testing) |
| Status | **Draft — accept together with `spec.md`** |
| Task detail | `docs/tasks.md` · Quality: `docs/quality.md` · Narrative: `docs/judging-narrative.md` |

Keep this file honest: when implementation drifts, update `plan.md` in the same PR.

---

## 1. Current state (18 Oct)

- `slate-ai` contains only the three hackathon PDFs. **No code exists yet**; the repository was initialised today with the SDLC artifacts.
- This is a clean start, which suits the originality rule (all code written in the hackathon window, visible in Git history).
- **20 Oct update:** T-01–T-04 and T-06 done; staging live. The mentor review (D-20–D-26) added FR-20–FR-22 (T-44–T-46) and dropped T-41–T-43. T-05/T-07 wait on the competency list (T-22).
- Fastest viable path: deploy a walking skeleton on Day 1, then build the loop in the order data flows (capture → ledger → compress → script → approve → parent → result), so there is a demoable end-to-end path by Day 5 and the rest is refinement.

## 2. Files and components

```
slate-ai/
├─ CLAUDE.md  README.md  REVIEW.md  LICENSE  .gitignore  .gitattributes  .editorconfig  .env.example
├─ SlateAi.sln
├─ docs/
│  ├─ intent.md  spec.md  plan.md  tasks.md  quality.md  judging-narrative.md
│  ├─ hackathon-requirements.md  user-testing.md  lessons.md
│  ├─ design/                     exported frames, design handoff notes
│  └─ hackathon/                  source PDFs (not committed, see .gitignore)
├─ src/
│  ├─ api/SlateAi.Api/            .NET 10 Minimal API
│  │  ├─ Features/                Attendance, Lessons, Ledger, CatchUp, Delivery, Parent, Pacing, Demo
│  │  ├─ Ai/                      ClaudeClient, prompt loader, validators, verifier, AiCall logging
│  │  ├─ Speech/                  Sarvam STT/TTS client
│  │  ├─ Curriculum/              graph loader, candidate-set computation
│  │  ├─ Data/                    DbContext, entities, migrations, seed
│  │  └─ Jobs/                    background queue
│  └─ web/                        React + Vite PWA; `npm run build` writes into the API's wwwroot
│     ├─ src/{app,features/{today,approve,ledger,return-day,pacing,parent,demo},components,i18n,offline,styles}
│     └─ e2e/                     Playwright + axe (lives here because CI runs Playwright from src/web)
├─ tests/
│  └─ api/SlateAi.Api.Tests/      unit + endpoint tests with fakes; Db/ suite on Testcontainers
├─ evals/                         cases/*.json, results/ (live Claude evals)
├─ prompts/                       a1-lesson-mapping.md … versioned prompts
├─ data/curriculum/  data/pictures/  data/seed/
├─ deploy/                        Dockerfile, docker-compose.yml, azure notes
├─ .github/                       workflows/ci.yml, deploy.yml, pull_request_template.md, CODEOWNERS
└─ .claude/                       settings.json (hooks), skills/
```

## 3. Order of work

| Step | Outcome | Spec refs | Proof |
|---|---|---|---|
| 1 | Walking skeleton deployed: API + SPA + Postgres + CI/CD, `/health` green on Azure | §4 | Public URL returns app shell; CI green |
| 2 | Curriculum graph + seed class (36 children) | §5.3 | Unit tests on graph (ancestors, candidate set) |
| 3 | Attendance from the simulated SATS feed (`IAttendanceSource`, fetched when needed, D-69) with corrections + lesson capture + A1 mapping | FR-01–04 | Endpoint tests with fake stores (no DB writes in new tests, 19 Oct); 10 A1 eval cases ≥ 80% |
| 4 | Ledger, incl. absence spells and return speed (days away, back within 2 school days; D-20/D-24), then the weekly return-speed card | FR-05, FR-22 | Deterministic tests on fixtures |
| 5 | A2 compression + A3 script + A4 verifier + TTS. **A2 built (T-08, 22 Oct)** as a service only (`Features/CatchUps/Compression/`): week = Monday up to the day; next week = the 5 lessons after the latest confirmed one (no syllabus data yet); empty candidate set → the latest missed lesson by rule. **A3 + packages built (T-09, 22 Oct):** numbers by code from a per-lesson `practice` rule (activity sum + 3-option check, distractors ≥ 10 apart, shuffled, correct index stored), A3 (Opus) writes the words only, `CatchUpPackages` / `CatchUpAssignments`, `POST catchups/generate` (202, background writer), `GET packages`. Buddy sums move to T-16 (code). **A4 verifier built (T-10, 23 Oct):** 6 code checks + Haiku, one retry with reasons, `ready` / `needsReview` / `failed`, review stored on the package. **Audio built (T-11, 23 Oct):** Sarvam Bulbul v3 → 16 kHz WAV → Ogg Opus 24 kbps (Concentus), clips in Postgres, disclosure once per wording, segments disclosure → script (greeting first once T0 records one), real length/size checked (≤ 95 s, ≤ 300 KB); ~55 s / ~150 KB per catch-up | FR-06, 07, 09 | Validator unit tests; 10 A2/A3 eval cases |
| 6 | Approval flow. **Built (T-14, 23 Oct):** T3 list + detail (segment player, Kannada summary, "Why this", picture question, tick bound to `ScriptVersion`, Simpler / Shorter / Try again, needs-review reasons, nav badge, generate button), approve / regenerate / send API, send only when approved at its version, links only for consenting families, approved catch-ups go with the 6 pm job. Bulk approve, Withdraw and offline approval: roadmap (D-70) | FR-08 | API refuses to send unapproved package (test) |
| 7 | Parent page + picture scoring + ledger update, incl. the optional reason for absence (A7) | FR-10, 11, 19, 21 | Playwright happy path; A7 fake-LLM tests + 10 evals |
| 7b | Evening message on each day of absence from an approved template, slots filled by code. **Built first (T-44 PR 1, 22 Oct, per D-70):** T0 approve step, tonight card, `EveningSendWorker` (every minute from 18:00 IST, idempotent per child and day), consent + stop filter, `GET /api/p/{token}`. The demo-chat bubble follows with T-18 | FR-20 | Endpoint tests: send refused without approved template / confirmed lesson / attendance / before 6 pm; no-consent and stopped families refused; no LLM; one Db test (race + seed consent) |
| 8 | Buddy cards + pacing nudge | FR-12, 13 | Unit tests; snapshot of card |
| 9 | Offline outbox | NFR-02 | Playwright offline test |
| 10 | Demo mode + reset + "Under the hood" AI panel. **Built (T-18, 23 Oct):** `/demo` with the teacher app and the parent page in two 360 px frames (tabs on a phone), one-tap demo sign-in, step guide, Reset that writes the 5 school days before the real today (no demo clock; Ravi away this week, Manjunath back today) plus a pre-made Ready Telugu catch-up from `data/seed/demo-catchup.json` (R5), "Bring in today's attendance", AI calls since the reset. The parent phone's inbox stands in for the demo chat bubble (step 7b) | FR-14 | Outsider completes loop in ≤ 5 min |
| 11 | Consent + delete | FR-15 | Integration test |
| 12 | Stretch: WhatsApp only (board photo and spoken answers dropped, D-26) | FR-16 | Only after Day 7 freeze |

**Build order from Day 3 (21 Oct): screen by screen.** Each screen is one vertical slice (UI + API + any AI step) built against its design card: sign-in + Welcome (T-47) → T1 Today → T2 Lesson confirm → T4 Missed lessons → T0 setup steps + T8 Families → T3 Approve → P1–P4 parent page → P0 evening message + reason → T5 Back today → T6 Week → demo shell (`POST /api/demo/login` stays with T-18). Cut T6 and T5 first if time runs short. **From 22 Oct the order is D-70's** ([`mvp-build-flow.md`](design/mvp-build-flow.md) §2): T-44 (evening message) was built before T-14/T-15 because it is in the demo loop.

## 4. Delivery

**Reset 24 Oct (Rahul):** the ten-day schedule (Days 0–10 with T-xx per day) was retired with the open tasks; what it delivered is in `tasks.md` (*Done*) and `status.md`. From now on Monika adds tasks one at a time in `tasks.md` and Rahul implements them one at a time, in the order she adds them.

Fixed dates: **submit on Hack2Skill by Sat 31 Oct, 18:00 IST** (hard deadline Sun 1 Nov, 23:59); tag `v1.0-submission` and make the repo public on submission.

### Design-to-engineering handoffs

Each handoff is a PR labelled `design` that adds `docs/design/<screen>.md` with: Figma frame link, exported PNG, states (empty, loading, offline, error), copy in Kannada/Telugu with English, tokens used, and an acceptance checklist. Rahul builds against that checklist and attaches screenshots to the implementing PR; Monika approves the UI by reviewing those screenshots or the preview URL.

## 5. Acceptance and test strategy

Acceptance criteria per requirement are in `spec.md` §2 and per task in `tasks.md`. Test layers, tools and gates are in `docs/quality.md`. Summary:

- `dotnet test` (unit + integration) and `npm test` must pass before merge (CI).
- Live AI evals run on demand and before each tag; results in `evals/results/`.
- Playwright happy path + offline path + axe run in CI against the built app.
- Two usability rounds with teachers; every change made from feedback is logged with a link to its PR (this is judging evidence).

## 6. Risks and fallbacks

| # | Risk | Likelihood / impact | Early signal | Fallback |
|---|---|---|---|---|
| R1 | Sarvam STT inaccurate on teacher Kannada | M / H | A1 eval < 80% on Day 3 | One-tap chapter picker (FR-04) is always available; Claude prompt tolerates noisy transcripts |
| R2 | Telugu TTS sounds unnatural to parents | M / H | Round 0 feedback Day 3 | Try other Bulbul voices / Google TTS; for the demo, a native speaker records the approved script (labelled as human-voiced) |
| R3 | WhatsApp Cloud API setup or template approval delays | H / M | Not working by Day 7 | Simulated parent phone in demo (primary); `wa.me` share link from teacher phone |
| R4 | Wrong AI output (arithmetic, language, off-topic) | M / H | Verifier failure rate > 15% | Verifier + one auto-retry + teacher approval; show the guardrail in the demo |
| R5 | Claude API slow or down during judging | L / H | Latency p95 > 20 s | Demo seed includes pre-generated, approved packages; live generation is one step, with cached result if it fails |
| R6 | Credits run out | L / M | AiCall log > USD 300 spent | Haiku for checks, prompt caching, evals on demand only |
| R7 | No real teachers reachable (Dasara break, availability) | M / H | < 2 confirmed by Day 2 | Proxies: retired/private-school teachers, B.Ed students, education mentor session; say honestly in the deck who tested |
| R8 | Azure deployment problems | L / H | Walking skeleton not live by Day 1 EOD | Same Dockerfile on Render or Fly.io; Neon Postgres |
| R9 | Scope creep | M / H | New feature ideas after Day 5 | Stretch only after Day 7 freeze; every new idea goes to the decision log first |
| R10 | Coordination: two people with day jobs | M / M | Handoff missed by > 1 day | Daily 21:30 sync; Rahul builds with wireframes if hi-fi is late; Monika reviews via preview URL, not local setup |
| R11 | Offline sync bugs (duplicates, lost audio) | M / M | Failing offline Playwright test | Idempotent `clientRequestId`; if unstable by Day 8, limit offline to attendance + voice queue and say so |
| R12 | Judges can't test (login, Kannada, no WhatsApp) | M / H | Outsider dry run fails | `/demo` shell, English toggle, landing page with 5 steps, no sign-up |
| R13 | Real child data or teacher voice used without consent | L / H | — | Seed only fictional children; signed consent form for any real teacher voice |
| R14 | Video over 3 minutes or unclear | M / M | Storyboard > 2:50 on Day 7 | Script to 2:50; problem in 20 s; loop shown at 1:00 |
| R15 | AI centrality not obvious in code review | L / H | — | `Ai/` folder, `prompts/`, `evals/`, "Under the hood" panel showing each Claude call |
| R16 | Days 4–6 overloaded by the mentor additions (T-44–T-46) | H / M | T-15 not on staging by Day 5 EOD | T-46 first (code only); T-45 ships picture buttons without the voice note; T-44 demo uses the same send path as T-14 |

## 7. Branching and pull requests

- **Trunk-based.** `main` is protected and always deployable; merges to `main` auto-deploy to the `slateai-staging` web app (after CI passes); `v*` tags deploy to `slateai-prod`. B1 has no slots, so staging is a second app on the same plan. Runbook: `deploy/azure/README.md`.
- Branch names: `feat/T-07-lesson-mapping`, `fix/…`, `design/T-23-today-hifi`, `docs/…`. One task per branch; branches live ≤ 1 day.
- PRs: use the template; squash-merge; Conventional Commit titles (`feat(api): map lesson transcript to competencies (T-07)`).
- Review rules:
  - Rahul's code PRs: CI green + self-review against `REVIEW.md`; Monika approves any PR that changes user-facing UI or copy (screenshots in the PR).
  - Monika's design/docs PRs: Rahul approves when they affect implementation.
  - `intent.md` / `spec.md` changes: Monika approves. `CLAUDE.md`, `.claude/`, `prompts/`: Rahul approves.
  - Emergency rule after code freeze: P0 fixes only, both people notified.
- Monika can work entirely in the GitHub web UI (edit docs, review PRs, comment on preview screenshots); no local setup needed.

## 8. Maintain (after submission)

- `docs/lessons.md`: each production bug or demo failure gets a line and, where possible, a new eval case.
- Watch one metric during judging: `/health` + error rate in Application Insights; alert by email.
- If shortlisted (Top 10, 18 Nov): new `intent.md` revision for the final stage.
