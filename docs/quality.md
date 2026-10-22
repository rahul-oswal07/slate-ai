# Quality strategy

Goal: a demo that works every time, and evidence that real users shaped it. Keep the suite small and fast; test what would embarrass us in front of judges.

## 1. Test layers

| Layer | Tool | What it covers | When it runs | Gate |
|---|---|---|---|---|
| Unit (API) | xUnit | Curriculum graph, candidate set, ledger query, verifier checks, scoring, pacing numbers, prompt builder PII guard, validators with a fake `ILlmClient` | Every PR (CI) | Must pass |
| Endpoint (API) | xUnit + WebApplicationFactory with fake stores (`FakeApi`), no database | Endpoints over HTTP with fake stores and fake Claude/Sarvam: validation, approval rule, parent answer → ledger, delete. Postgres-only behaviour is covered by the Db suite below | Every PR (CI) | Must pass |
| Database (API) | xUnit + Testcontainers PostgreSQL (`Category=Db`), throwaway DB per test | Only what a real Postgres proves: attendance replay idempotency, ledger query translation, concurrent migrate + seed; approval rule (T-14) and child deletion (T-19) when built | Every PR (CI); locally with Docker | Must pass |
| Unit (web) | Vitest + Testing Library | Outbox logic, i18n keys present for all 4 languages, component states | Every PR (CI) | Must pass |
| End to end | Playwright | Happy path (teacher → approve → parent → ledger), offline capture + sync, demo reset | Every PR against the built container | Must pass |
| Accessibility | @axe-core/playwright + manual TalkBack | Teacher and parent screens | Every PR (axe); Day 8 (manual) | 0 serious/critical |
| Performance | Lighthouse (mobile), Chrome throttling "Slow 3G" | Parent page, Today screen | Day 5, Day 8 | ≥ 85; parent page ≤ 500 KB excl. audio |
| **AI evals** | `dotnet test --filter Category=Eval` (live Claude) | A1 mapping accuracy, A2 subset/rationale quality, A3 rubric (language, length, key words, arithmetic, tone) | On demand, before each tag, after any prompt change | Targets in `intent.md` |
| Usability | Moderated sessions | Teacher flow time and errors; parent comprehension | Day 3 (round 0), Day 6, Day 8 | ≥ 5 logged changes |

Rules from the playbook we adopt:
- One command to verify: `./scripts/verify.sh` (build + API + web tests). Claude Code must run it before reporting a task done.
- Bug fix = failing test first, then the fix, without editing the test.
- Every demo failure or production bug becomes a regression test or an eval case (`docs/lessons.md`).
- Prompt changes require an eval run in the PR description.

## 2. AI eval design

| Call | Cases | Pass rule |
|---|---|---|
| A1 Lesson mapping | 25 Kannada teacher transcripts (Monika + teachers write them; include noisy, code-mixed Kannada-English, off-topic) | Top-1 competency matches; off-topic → low confidence |
| A2 Compression | 25 absence patterns (1–5 days, overlapping, already-mastered items) | Targets ⊆ candidates; human-labelled "best target" in top 2 |
| A3 Script | 25 (target, language) pairs | All code checks pass; Haiku rubric ≥ 4/5; native-speaker spot check on 5 |
| A7 Reason classification | 10 parent voice-note transcripts (Kannada, Telugu, code-mixed; include off-topic and symptom talk) | Category matches the label; off-topic or unclear → other; output never contains symptoms (only the category is stored) |

Store cases in `evals/cases/*.json`; results (date, model, prompt version, score) in `evals/results/`. Quote these numbers in the deck.

**A1 case format** (`evals/cases/a1-lesson-mapping.json`): `{ id, transcript, expected?, alsoAcceptable?, expectLow? }`. A case passes when A1's first lesson is `expected` or in `alsoAcceptable`; an `expectLow` case (vague, off-topic) passes when the result is low confidence or unclear. The eval runs production's path minus speech-to-text: name scrubber → A1 prompt → validator (`tests/api/.../Evals/A1LessonMappingEvals.cs`, `dotnet test --filter Category=Eval`).

**First run** (22 Oct, prompt `a1-lesson-mapping` v1, Sonnet 5.5): **10/10** on 10 draft cases (`evals/results/2026-10-22-a1-lesson-mapping.json`). The cases were drafted by Claude and are cleaner than real speech; Monika / teachers to replace them with real transcripts and grow to 25 (T-38).

## 3. Usability testing with representative users

**Participants:** 3 government primary teachers (Grade 2–3, Kannada medium) or close proxies; 2 parents from Telugu-speaking families. Monika recruits; consent form signed; first names only in the repo.

**Round 0 (Day 3, remote, 30 min):** play 3 sample catch-up audios; ask: would you send this? what's wrong? Show wireframes of Today and Approve.

**Round 1 (Day 6, 45 min each, on a low-end Android phone):**

| Task | Measure |
|---|---|
| Mark 4 children absent and record what you taught | Seconds (target ≤ 60), errors, hesitations |
| Confirm the detected lesson | Correct? Understood? |
| Approve this week's catch-ups | Seconds; does she trust the Kannada gloss? |
| Find who still needs help | Found in ≤ 15 s? |
| Use Ravi's buddy card | Would she use it on return day? |
| (Parent) Listen and answer | Completes without help? Understands the activity? |

Close with: SUS-lite (5 questions, 1–5) and "What would stop you using this on Monday?"

**Round 2 (Day 8):** repeat the timed tasks after fixes; one session in a real classroom if schools are open.

**Log format** (`docs/user-testing.md`): finding ID, participant, observation, quote, severity, decision, PR link. The "decision" and "PR" columns are the judging evidence for "proof of testing and refinement".

## 4. Demo rehearsal checklist

Run 3 times on Day 10 and once on submission day.

- [ ] Production URL loads in an incognito window on desktop and on an Android phone
- [ ] `Reset demo` restores the Ravi story; ledger shows 4 missed lessons
- [ ] Record a Kannada voice note live; mapping appears in ≤ 20 s
- [ ] Toggle network off → capture queues → network on → syncs once
- [ ] Approve shows Telugu audio + Kannada gloss; verifier badge visible
- [ ] Parent phone: Listen plays greeting then catch-up; picture tap updates ledger to "caught up"
- [ ] Wrong answer path → "needs buddy" → buddy card shows classmate + 3 sums
- [ ] Pacing nudge shows correct numbers
- [ ] "Under the hood" panel lists each Claude call with model and verifier result
- [ ] English toggle works for judges
- [ ] Fallback ready: if Claude/Sarvam fails, seeded approved packages still run the loop
- [ ] Backup video file available offline on the laptop
- [ ] Timer: full live demo ≤ 4 minutes; video ≤ 3:00
