# Tasks

**Reset 24 Oct (Rahul):** every open task was cleared to start fresh; the GitHub issues were closed (done ones as completed, the rest as not planned). From now on **Monika adds tasks one at a time** under *Open*, and Rahul implements them one at a time. Each new task gets the next free ID (T-48 onwards) and one GitHub issue.
Owner: **R** = Rahul, **M** = Monika.

## Open

| ID | Task | Owner | Acceptance criteria | How it's tested |
|---|---|---|---|---|
| T-48 | **Slate & chalk foundation (D-77)**, depends on T-15, T-23: additive tokens, scoped `.theme-slate`, per-language fonts, and the shared parent-page parts (frame, header, step tally, AI chip, primary button, stop link) so P1–P4 and the reason page pick it up. Looks only: no change to hooks, props, routes, i18n keys or ARIA roles and names | R (design: M) | New tokens and ratios from `tokens.md` "Slate & chalk" exist in `tokens.css` and every pair is in `tokens.test.ts` (text ≥ 7:1, UI ≥ 3:1, both themes); existing token values unchanged; `ParentFrame` applies `.theme-slate`, the teacher app does not yet; `StepDots` becomes the four-stroke tally with a visible "n/4" and "Step n of 4" for screen readers; the AI label is a quiet chip and the teacher's name leads; fonts self-hosted, subset, loaded per language, `font-display: swap`, parent page still ≤ 500 KB excluding audio; kn/te/ta keep their 1.7 line height and no letter-spacing; reduced motion turns every animation off; before/after 360 px screenshots (light + dark) for P1, P2, P3, P4, reason, stopped, expired | `tokens.test.ts`; Vitest for the tally and chip; existing Vitest and `src/web/e2e/` specs pass unchanged; axe 0 on P1 light and dark; `./scripts/verify.sh`; Monika approves from the screenshots |
| T-49 | **Today re-layout (D-77), later**, depends on T-48, T-12: day strip (Attendance → Lesson → Tonight 6 pm), compact attendance card, lesson card as the one hero, "Added" and "Tonight" merged into one payoff card, "Since yesterday" collapsed to a line and a count. Same data, hooks and components; looks and order only. **Do not start until T-48 is merged and Monika has signed off the mockup.** Issue #115 | R (design: M) | Every state in `docs/design/T1-today.md` still reachable; no change to hooks, props, routes, i18n keys or ARIA roles and names; existing Vitest and `src/web/e2e/` specs pass unchanged (or Monika approves each changed assertion); short nav labels that don't wrap in kn/te/ta; 360 px before/after screenshots, light and dark | Vitest component tests for the day strip and the merged card; axe 0; `./scripts/verify.sh`; Monika approves from the screenshots |

## Done (until 24 Oct)

What was built, kept for reference. "Day" refers to the original ten-day plan.

| ID | Task | Owner | Day | Depends on | Acceptance criteria | How it's tested |
|---|---|---|---|---|---|---|
| T-00 | Accept `intent.md`; resolve Day 1 gate decisions (D-01, 02, 05, 10–13, 16) | M (R for D-05, 10–13) | 0 | — | Decision log rows marked Accepted/Changed; merged to `main` | PR review |
| T-01 | Repo, CI, branch protection, Project board, issue per task | R | 1 | T-00 | `ci.yml` runs build + tests on PR; `main` protected; board has columns Backlog/Doing/Review/Done | Open a test PR; CI runs |
| T-02 | API skeleton: .NET 10 Minimal API, EF Core + Postgres, `/health`, config, seed of 1 teacher + 36 children | R | 1 | T-01 | `dotnet run` serves `/health` 200; migration creates schema; seed idempotent | Integration test with Testcontainers: `/health`, seed count = 36 |
| T-03 | Web skeleton: Vite + React + TS, router, i18n (kn/en/te/ta), PWA manifest + service worker, tokens placeholder | R | 1 | T-01 | Installable PWA; language toggle works; build output copied to API `wwwroot` | Vitest smoke; Lighthouse PWA check |
| T-04 | Walking skeleton deployed to Azure (Dockerfile, deploy workflow, secrets) | R | 1 | T-02, T-03 | Public URL shows app shell; `/health` green; deploy on merge to `main` | Manual check + workflow run |
| T-21 | Low-fi wireframes: Today, Approve, Ledger, Parent P1–P4 | M | 1 | T-00 | Delivered as the clickable prototype + UX review instead of Figma (`docs/design/prototype/`, `docs/design/ux-review.md`) | Rahul walk-through at 21:30 sync |
| T-22 | Competency list draft (12–18 items, Kannada + English names, prerequisites, NIPUN codes, textbook examples, textbook/workbook page per competency (D-22)) | M | 1 | D-01 | Table in `docs/design/curriculum.md` reviewed by Rahul | Rahul checks graph has no cycles |
| T-05 | Curriculum JSON + graph service (ancestors, candidate set) | R | 2 | T-22 | `data/curriculum/grade3-maths.json` loads, incl. `textbookPage` (D-22); `CandidateSet(missed, nextWeek)` correct | xUnit: 8+ graph cases incl. no-overlap and multi-path |
| T-06 | Attendance API + ledger query | R | 2 | T-02 | `POST attendance` idempotent by `clientRequestId`; ledger = attendance × lessons | xUnit fixtures; integration test for replay |
| T-07 | A1 lesson mapping: upload audio → Sarvam STT → Claude tool call → validated competency IDs; picker fallback | R | 2 | T-05 | IDs always valid; confidence < 0.7 triggers picker; AiCall logged | 10 eval cases ≥ 80% top-1 (→ 25 cases ≥ 90% by Day 8); unit tests on validator with fake `ILlmClient` |
| T-23 | Design tokens + hi-fi Today and Ledger | M | 2 | T-21, D-04 | Tokens done (T-23, #50); Today and Ledger delivered as design cards `docs/design/T1-today.md`, `T2-lesson-confirm.md`, `T4-missed-lessons.md` with 360 px screenshots, all states | Contrast checker; Rahul confirms buildable |
| T-08 | A2 compression with candidate-set validator and rule fallback | R | 3 | T-05, T-06 | Targets ⊆ candidate set, ≤ 2; rationale stored; fallback when invalid | xUnit with fake LLM returning invalid IDs; 10 eval cases |
| T-09 | A3 script writer (script, key words, activity, check item, gloss, buddy content) | R | 3 | T-08 | JSON schema valid; placeholders only; prompt versioned; homework points to the textbook page and ends with "Show `{teacher}`" (FR-07, D-22) | Eval rubric on 10 cases; manual review by M + native speaker |
| T-10 | A4 verifier (code checks + Haiku check) with one auto-retry | R | 3 | T-09 | All 6 code checks implemented; failing package marked "needs review" | xUnit: one test per check with crafted bad outputs |
| T-11 | TTS + greeting merge; audio to blob storage | R | 3 | T-09 | Audio ≤ 300 KB, ≤ 95 s; greeting plays first, then the spoken AI disclosure (FR-09, D-18) | Integration test with recorded fake asserts segment order greeting → disclosure → script; manual listen on Android |
| T-24 | Hi-fi Approve + Parent P1–P4 + picture style; interview/test script | M | 2–3 | T-21 | Delivered as design cards `docs/design/T3-approve.md`, `T5`, `T6`, `P0`–`P4` with 360 px screenshots; picture style per D-07 (stick bundles, numeral after the tap) | Rahul confirms buildable |
| T-47 | Teacher sign-in + first-run gate (D-40): PIN login screen, session cookie, `/api/me`, Welcome (T0 state 0), class endpoints limited to the teacher's class | R (UX: M) | 3 | T-02, T-03 | Phone once then PIN; wrong PIN and unknown phone look the same; lockout; signed out → login, `ConsentAt` null → Welcome, else Today; works offline once signed in | xUnit endpoint tests with fake store; Vitest for login/welcome/gate; Playwright flow + axe at 360 px |
| T-12 | Today screen (T1): attendance from the simulated SATS feed (D-27, D-69) with corrections and manual fallback grid + voice capture + offline outbox + sync badge + bottom nav | R | 4 | T-03, T-06, T-07, T-23, T-47 | FR-01, FR-02; source and time shown; corrections never written back; works with network off; no duplicates on replay | Playwright: offline capture → reconnect → one lesson created |
| T-13 | Ledger screen | R | 4 | T-06, T-23 | FR-05; filter by "absent this week" | Vitest component test; Playwright check of counts |
| T-14 | Approve screen + API rule "no send without approval" | R | 5 | T-10, T-11, T-24 | FR-08; Approve all; Regenerate with hint | Integration test: send unapproved → 409 |
| T-15 | Parent page P1–P4 + picture-tap scoring + ledger update | R | 5 | T-14, T-24 | FR-10, FR-11, FR-19 (D-19); ≤ 500 KB excluding audio | Playwright happy path + stop flow; integration test: stopped child gets no send (409); Lighthouse mobile ≥ 85 |
| T-27 | Usability kit: tasks, consent, observation + timing sheet | M | 5 | T-24 | Kit in `docs/user-testing.md` | Dry run with Rahul |
| T-16 | Buddy cards + pacing nudge | R | 6 | T-06, T-09 | FR-12, FR-13; numbers by code only; "Brought homework" tap stored (D-22) | xUnit: buddy was present for the missed lessons; nudge numbers = ledger; endpoint test for the homework tap |
| T-46 | Return-speed card (FR-22, D-24) | R | 4 | T-13 | Absences by length (1, 2, 3, 4+ days) this week vs last, share back within 2 school days, reason categories; teacher-only | xUnit on fixtures (numbers = ledger spells); Vitest component test |
| T-44 | Evening message on each day of absence (FR-20, D-21): `MessageTemplate` + one-time approval, code slot filler, send endpoint; greeting audio + written text only (stitched audio is roadmap, D-70) | R | 5–6 | T-14, T-15, T-16 | No LLM on this path; send only after 6 pm to opted-in, not-stopped families with a confirmed lesson; guardrail 3 | xUnit: slot filler from fixtures; endpoint tests: unapproved template → 409, unconfirmed lesson → 409, stopped child → 409 |
| T-45 | Reason for absence on the parent page (FR-21, D-23); voice note + A7 classification cut (D-63) | R (copy: M) | 5–6 | T-15 | 5 picture buttons + Skip; category only stored; skipping never blocks the page | Endpoint tests: category stored, invalid category rejected, skip stores nothing |
| T-18 | Demo mode: split screen, Ravi story seed, Reset, "Under the hood" AI call panel | R | 7 | T-15, T-16 | FR-14; reset < 5 s; AI panel lists each call with model, prompt version, verdict | Outsider test: loop in ≤ 5 min |
| T-19 | Consent capture + delete child data | R | 7 | T-02 | FR-15 | Integration test: delete removes rows + audio |

## Dropped

| ID | Task | Owner | Day | Depends on | Acceptance criteria | How it's tested |
|---|---|---|---|---|---|---|
| ~~T-25~~ | ~~Round 0: 2 teacher calls reviewing 3 sample scripts and the flow~~ Dropped (D-70): effort goes to usability round 1 (T-28) | — | — | — | — | — |
| ~~T-41~~ | ~~Board photo capture (Claude vision)~~ Dropped (D-26) | — | — | — | — | — |
| ~~T-42~~ | ~~Spoken child answer + teacher review queue~~ Dropped (D-26); replaced by the spoken reason in T-45 | — | — | — | — | — |
| ~~T-43~~ | ~~Tamil catch-up fully reviewed by a native speaker~~ Dropped (D-26); Tamil stays spot-checked | — | — | — | — | — |
