# Tasks

Priority order. **P0** = on the critical path to a demoable loop; **P1** = needed for a strong submission; **P2** = stretch (only after the Day 7 freeze).
Owner: **R** = Rahul, **M** = Monika. Day refers to `plan.md` §4. Create one GitHub issue per task with the same ID.

## P0 — critical path

| ID | Task | Owner | Day | Depends on | Acceptance criteria | How it's tested |
|---|---|---|---|---|---|---|
| T-00 | Accept `intent.md`; resolve Day 1 gate decisions (D-01, 02, 05, 10–13, 16) | M (R for D-05, 10–13) | 0 | — | Decision log rows marked Accepted/Changed; merged to `main` | PR review |
| T-01 | Repo, CI, branch protection, Project board, issue per task | R | 1 | T-00 | `ci.yml` runs build + tests on PR; `main` protected; board has columns Backlog/Doing/Review/Done | Open a test PR; CI runs |
| T-02 | API skeleton: .NET 10 Minimal API, EF Core + Postgres, `/health`, config, seed of 1 teacher + 36 children | R | 1 | T-01 | `dotnet run` serves `/health` 200; migration creates schema; seed idempotent | Integration test with Testcontainers: `/health`, seed count = 36 |
| T-03 | Web skeleton: Vite + React + TS, router, i18n (kn/en/te/ta), PWA manifest + service worker, tokens placeholder | R | 1 | T-01 | Installable PWA; language toggle works; build output copied to API `wwwroot` | Vitest smoke; Lighthouse PWA check |
| T-04 | Walking skeleton deployed to Azure (Dockerfile, deploy workflow, secrets) | R | 1 | T-02, T-03 | Public URL shows app shell; `/health` green; deploy on merge to `main` | Manual check + workflow run |
| T-21 | Low-fi wireframes: Today, Approve, Ledger, Parent P1–P4 | M | 1 | T-00 | Frames in Figma; linked in `docs/design/wireframes.md` | Rahul walk-through at 21:30 sync |
| T-22 | Competency list draft (12–18 items, Kannada + English names, prerequisites, NIPUN codes, textbook examples) | M | 1 | D-01 | Table in `docs/design/curriculum.md` reviewed by Rahul | Rahul checks graph has no cycles |
| T-20 | Recruit 3 teachers + 2 Telugu-speaking parents (or proxies); consent form | M | 1 | — | Names (first name only in repo), slots for Day 3, 6, 8 booked | — |
| T-05 | Curriculum JSON + graph service (ancestors, candidate set) | R | 2 | T-22 | `data/curriculum/grade3-maths.json` loads; `CandidateSet(missed, nextWeek)` correct | xUnit: 8+ graph cases incl. no-overlap and multi-path |
| T-06 | Attendance API + ledger query | R | 2 | T-02 | `POST attendance` idempotent by `clientRequestId`; ledger = attendance × lessons | xUnit fixtures; integration test for replay |
| T-07 | A1 lesson mapping: upload audio → Sarvam STT → Claude tool call → validated competency IDs; picker fallback | R | 2 | T-05 | IDs always valid; confidence < 0.7 triggers picker; AiCall logged | 10 eval cases ≥ 80% top-1 (→ 25 cases ≥ 90% by Day 8); unit tests on validator with fake `ILlmClient` |
| T-23 | Design tokens + hi-fi Today and Ledger | M | 2 | T-21, D-04 | Tokens JSON exported; contrast AA checked; states: empty/offline/error | Contrast checker; Rahul confirms buildable |
| T-08 | A2 compression with candidate-set validator and rule fallback | R | 3 | T-05, T-06 | Targets ⊆ candidate set, ≤ 2; rationale stored; fallback when invalid | xUnit with fake LLM returning invalid IDs; 10 eval cases |
| T-09 | A3 script writer (script, key words, activity, check item, gloss, buddy content) | R | 3 | T-08 | JSON schema valid; placeholders only; prompt versioned | Eval rubric on 10 cases; manual review by M + native speaker |
| T-10 | A4 verifier (code checks + Haiku check) with one auto-retry | R | 3 | T-09 | All 6 code checks implemented; failing package marked "needs review" | xUnit: one test per check with crafted bad outputs |
| T-11 | TTS + greeting merge; audio to blob storage | R | 3 | T-09 | Audio ≤ 300 KB, ≤ 95 s; greeting plays first | Integration test with recorded fake; manual listen on Android |
| T-24 | Hi-fi Approve + Parent P1–P4 + picture style; interview/test script | M | 2–3 | T-21 | Design PRs with handoff notes (`plan.md` §4) | Rahul confirms buildable |
| T-25 | Round 0: 2 teacher calls reviewing 3 sample scripts and the flow | M (R joins) | 3 | T-20, sample scripts from T-09 | Notes in `docs/user-testing.md`; changes listed with owner | — |
| T-12 | Today screen: attendance grid + voice capture + offline outbox + sync badge | R | 4 | T-03, T-06, T-07, T-23 | FR-01, FR-02; works with network off; no duplicates on replay | Playwright: offline capture → reconnect → one lesson created |
| T-13 | Ledger screen | R | 4 | T-06, T-23 | FR-05; filter by "absent this week" | Vitest component test; Playwright check of counts |
| T-14 | Approve screen + API rule "no send without approval" | R | 5 | T-10, T-11, T-24 | FR-08; Approve all; Regenerate with hint | Integration test: send unapproved → 409 |
| T-15 | Parent page P1–P4 + picture-tap scoring + ledger update | R | 5 | T-14, T-24 | FR-10, FR-11; ≤ 500 KB excluding audio | Playwright happy path; Lighthouse mobile ≥ 85 |
| T-27 | Usability kit: tasks, consent, observation + timing sheet | M | 5 | T-24 | Kit in `docs/user-testing.md` | Dry run with Rahul |
| T-28 | **Usability round 1** (3 teachers, 1–2 parents) | M (R observes) | 6 | T-15, T-27 | Teacher time measured; findings logged; top 5 P0 issues created | — |
| T-16 | Buddy cards + pacing nudge | R | 6 | T-06, T-09 | FR-12, FR-13; numbers by code only | xUnit: buddy was present for the missed lessons; nudge numbers = ledger |
| T-17 | Fix round-1 P0 findings | R + M | 7 | T-28 | Each fix PR links the finding ID | Re-test with the same task |
| T-18 | Demo mode: split screen, Ravi story seed, Reset, "Under the hood" AI call panel | R | 7 | T-15, T-16 | FR-14; reset < 5 s; AI panel lists each call with model, prompt version, verdict | Outsider test: loop in ≤ 5 min |
| T-19 | Consent capture + delete child data | R | 7 | T-02 | FR-15 | Integration test: delete removes rows + audio |
| T-29 | Demo story + video storyboard (≤ 2:50) | M | 7 | T-18 | Shot list and narration in `docs/judging-narrative.md` §9 | Read-through with timer |

## P1 — strong submission

| ID | Task | Owner | Day | Depends on | Acceptance criteria | How it's tested |
|---|---|---|---|---|---|---|
| T-30 | Accessibility + performance pass | R + M | 8 | T-18 | axe 0 serious/critical; targets ≥ 48 px; Slow 3G usable | Playwright + axe; Lighthouse; manual TalkBack check |
| T-31 | **Usability round 2** (2 teachers, ideally one real class) | M | 8 | T-17 | Before/after timing; findings logged | — |
| T-38 | Eval suite to 25 cases each for A1–A3; results committed | R | 8 | T-07–T-10 | Targets in `intent.md` met or gap explained | `dotnet test --filter Category=Eval` |
| T-32 | Round-2 fixes | R | 9 | T-31 | Each fix links finding | Re-test |
| T-33 | Privacy/security pass (no PII to providers, token expiry, secrets, headers) | R | 9 | T-18 | Checklist in `REVIEW.md` passes | Unit test: prompt builder rejects real names; manual header check |
| T-34 | README final, LICENSE, open-source prep | R | 9 | D-15 | A stranger can run locally in ≤ 15 min | Outsider follows README |
| T-35 | Submission deck (AI layers, guardrails, offline, testing evidence, roadmap) | M (R for technical slides) | 9 | T-31, T-38 | Covers every item in `hackathon-requirements.md` §4 | Checklist review together |
| T-36 | Record final demo video | M + R | 10 | T-18, T-29 | ≤ 3:00, captions, English narration with Kannada/Telugu UI shown | Timed playback; watched by one outsider |
| T-37 | Outsider dry run of the judge path | M + R | Buffer | T-34, T-36 | Completed without help | Observed session |

## P2 — stretch (go/no-go on Day 7)

| ID | Task | Owner | Depends on | Acceptance criteria | How it's tested |
|---|---|---|---|---|---|
| T-40 | WhatsApp Cloud API test number: template with link, 18:00 scheduling | R | T-15 | FR-16: delivered to a registered phone | Manual send to test numbers |
| T-41 | Board photo capture (Claude vision) | R | T-07 | FR-17: same output as voice | 5 eval photos |
| T-42 | Spoken child answer + teacher review queue | R | T-15 | FR-18: never auto-marks below 0.8 | Unit test on threshold; 5 recorded samples |
| T-43 | Tamil catch-up fully reviewed by a native speaker | M | T-09 | Tamil scripts approved by reviewer | Native speaker review |
