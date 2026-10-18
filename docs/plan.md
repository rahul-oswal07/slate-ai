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
│  └─ api/SlateAi.Api.Tests/      unit + integration (Testcontainers)
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
| 3 | Attendance + lesson capture + A1 mapping | FR-01–04 | Integration test; 10 A1 eval cases ≥ 80% |
| 4 | Ledger | FR-05 | Deterministic tests on fixtures |
| 5 | A2 compression + A3 script + A4 verifier + TTS | FR-06, 07, 09 | Validator unit tests; 10 A2/A3 eval cases |
| 6 | Approval flow | FR-08 | API refuses to send unapproved package (test) |
| 7 | Parent page + picture scoring + ledger update | FR-10, 11 | Playwright happy path |
| 8 | Buddy cards + pacing nudge | FR-12, 13 | Unit tests; snapshot of card |
| 9 | Offline outbox | NFR-02 | Playwright offline test |
| 10 | Demo mode + reset + "Under the hood" AI panel | FR-14 | Outsider completes loop in ≤ 5 min |
| 11 | Consent + delete | FR-15 | Integration test |
| 12 | Stretch: WhatsApp, board photo, spoken answers | FR-16–18 | Only after Day 7 freeze |

## 4. Ten-day delivery plan

Capacity assumption: both work around day jobs; weekdays ~4–5 h each, Sat/Sun ~8 h each. Daily sync **21:30 IST, 15 min** (what shipped, what's blocked, next handoff). Status lives on the GitHub Project board.

| Day | Date | Rahul (engineering) | Monika (product, design, testing) | Handoff / checkpoint |
|---|---|---|---|---|
| 0 | Sun 18 Oct | Create GitHub repo, push initial commit; get Anthropic credits + Sarvam key; create Azure resources | Review and accept `intent.md`; resolve Day 1 gate decisions in `spec.md` §8 | **Gate: intent accepted** |
| 1 | Mon 19 Oct | T-01 repo/CI, T-02 API skeleton + DB, T-03 web skeleton (PWA, i18n), T-04 deploy walking skeleton | T-20 recruit 3 teachers + 2 Telugu-speaking parents; T-21 low-fi wireframes (Today, Approve, Ledger, Parent); T-22 competency list draft; book education mentor | **H1 (EOD):** wireframes + competency list → Rahul. **Gate: spec accepted** |
| 2 | Tue 20 Oct | T-05 curriculum JSON + graph; T-06 attendance API; T-07 A1 lesson mapping (STT + Claude) + 10 evals | T-23 design tokens + hi-fi Today and Ledger; T-24 interview + test script; book technical mentor (Rahul attends) | **H2:** tokens + Today/Ledger hi-fi → Rahul. Rahul sends 3 sample scripts → Monika |
| 3 | Wed 21 Oct | T-08 A2 compression; T-09 A3 script writer; T-10 A4 verifier; T-11 TTS + greeting merge | T-25 Round 0: 2 teacher calls reviewing sample scripts and the flow; hi-fi Approve + Parent P1–P4; picture style | **TC1:** A1 evals ≥ 80%; verifier unit tests green |
| 4 | Thu 22 Oct | T-12 Today screen + offline outbox; T-13 Ledger screen | T-26 native-speaker review of Telugu/Kannada scripts; buddy card + pacing designs; parent copy | **H3:** Parent screens, buddy card, Approve hi-fi → Rahul |
| 5 | Fri 23 Oct | T-14 Approve screen; T-15 parent page + picture scoring | T-27 usability kit: tasks, consent form, observation sheet, timing sheet | **TC2: first end-to-end loop on staging**; Playwright happy path green. Tag `v0.5-loop` |
| 6 | Sat 24 Oct | Observe tests, time teacher actions; then T-16 buddy cards + pacing nudge | **T-28 Usability round 1**: 3 teachers + 1–2 parents; log in `docs/user-testing.md` | Evening triage: findings → issues, top 5 marked P0 |
| 7 | Sun 25 Oct | T-17 fix round-1 P0s; T-18 demo mode + reset + "Under the hood" panel; T-19 consent + delete | Revise designs from round 1; T-29 demo story + video storyboard (≤ 2:50) | **MVP feature freeze (FR-01–15).** Stretch go/no-go decision |
| 8 | Mon 26 Oct | Stretch (if go) or hardening; T-30 accessibility + performance pass; full eval run | **T-31 Usability round 2**: 2 teachers (one in a real class if possible); screen-reader check | **TC3:** axe 0 serious/critical; evals at targets; Lighthouse ≥ 85 |
| 9 | Tue 27 Oct | T-32 round-2 fixes; T-33 privacy/security pass; T-34 README + open-source prep | T-35 deck draft (AI layers, guardrails, offline, testing evidence, roadmap); record raw footage | Deck draft → Rahul for technical review |
| 10 | Wed 28 Oct | **Code freeze 20:00.** Bug bash together; 3 demo rehearsals | Bug bash; T-36 record final demo video | Tag `v1.0-rc`; only P0 fixes after this |
| Buffer | Thu 29 Oct | P0 fixes only; production data reset; uptime check | Edit + upload video (YouTube, publicly accessible); finalise deck | Video and deck done |
| Buffer | Fri 30 Oct | T-37 outsider dry run of judge path using only README; fix blockers | Same dry run with a teacher proxy; check every submission item | All items in `hackathon-requirements.md` §4 ticked |
| Submit | **Sat 31 Oct** | Submit on Hack2Skill by **18:00 IST**; tag `v1.0-submission`; make repo public | Co-submit; confirm links open in incognito | **Submitted** |
| Reserve | Sun 1 Nov | Emergency only (deadline 23:59) | Emergency only | — |

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
