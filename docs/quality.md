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

**A1 case format** (`evals/cases/a1-lesson-mapping.json`): `{ id, transcript, expected?, alsoAcceptable?, expectLow?, photo?, noNames? }`. `photo` is an image in `evals/cases/images/` sent with the transcript (or alone, transcript empty); a `noNames` case also fails if a name from the photo reaches the summary or examples. A case passes when A1's first lesson is `expected` or in `alsoAcceptable`; an `expectLow` case (vague, off-topic) passes when the result is low confidence or unclear. The eval runs production's path minus speech-to-text: name scrubber → A1 prompt → validator (`tests/api/.../Evals/A1LessonMappingEvals.cs`, `dotnet test --filter Category=Eval`).

**First run** (22 Oct, prompt `a1-lesson-mapping` v1, Sonnet 5.5): **10/10** on 10 draft cases (`evals/results/2026-10-22-a1-lesson-mapping.json`). The cases were drafted by Claude and are cleaner than real speech; Monika / teachers to replace them with real transcripts and grow to 25 (T-38).

**A1 v2 with photos** (30 Oct, T-64, prompt `a1-lesson-mapping` v2, Sonnet 5.5): **13/13**: the 10 text cases unchanged plus 3 photo cases (a textbook page alone, a blackboard alone with a child's name written on it, a vague note plus a page of word problems). No name copied from the board (`evals/results/2026-10-30-a1-lesson-mapping.json`). The photos are synthetic, rendered by `evals/cases/images/render.mjs`; real teacher photos should replace them.

**A2 case format** (`evals/cases/a2-compression.json`): `{ id, missed[], lastTaught[], best, alsoAcceptable? }` on the real curriculum graph. The eval builds the input with production's planner code (next week = the 5 lessons after `lastTaught`, candidate set from the graph) and passes a case when the result is Claude's (not the rule), every target is a candidate, and `best` or an `alsoAcceptable` lesson is among the targets (`tests/api/.../Evals/A2CompressionEvals.cs`). The results file keeps Claude's "Why this" reasons for review.

**First run** (22 Oct, prompt `a2-compression` v1, Sonnet 5.5): **10/10** on 10 draft cases (`evals/results/2026-10-22-a2-compression.json`). A first attempt scored 9/10: the five-day case hit the 1024-token output limit, so the rule fallback answered; A2 now uses the default 4096. Cases drafted by Claude; Monika / a teacher to review the "best" labels and grow to 25 (include already-mastered items once check results exist, T-15).

**Kannada reasons** (24 Oct, D-76, prompt `a2-compression` v2, Sonnet 5.5): **10/10**, same targets rule plus every reason also in Kannada (validator: ≥ 80% Kannada letters); 10/10 cases had a Kannada reason for every item (`evals/results/2026-10-24-a2-compression.json`). For Monika: lessons are mostly named by their `nameKn`, but one reason called carrying "ಕೈಹಿಡಿದು ಕೂಡುವಿಕೆ" instead of "ದಶಕ ಸಹಿತ ಸಂಕಲನ"; review the wording in the results file.

**A3 case format** (`evals/cases/a3-script.json`): `{ id, language, targets[], teacherExamples[] }`. The eval makes the numbers with production's code (seeded by the case id) and passes a case when A3's output passes the validator (3 distinct key words from the lesson's textbook words, no placeholders, markup or class names, lengths), each key word appears in the script, at least 80% of the script's letters (Kannada key words aside) are in the family's script, and the script has at most 120 words (`tests/api/.../Evals/A3ScriptEvals.cs`). The Haiku rubric (teaches the target, age-appropriate, no shaming, gloss matches) comes with the verifier (T-10).

**First run** (22 Oct, prompt `a3-script` v1, Opus 5.5): **10/10** on 10 draft cases across kn / te / ta (`evals/results/2026-10-22-a3-script.json`, every script included for Monika and native speakers). Two fixes on the way: the validator first capped the script at 600 characters, which a 120-word Tamil or Telugu script exceeds (now a word limit); one case hit the 4096-token output limit (A3 now 8192).

**A3 + A4 with the verifier** (23 Oct, prompts `a3-script` v2 + `a4-check` v1, Opus 5.5 + Haiku 5.5): **10/10 first pass** (intent.md target ≥ 85%), 10/10 ready after the retry; the eval now runs production's verifier and retry instead of its own checks (`evals/results/2026-10-22-a3-script.json`, overwritten by this run; v1's scripts are kept in `evals/cases/a4-check.json`).

**English families** (23 Oct, T-18, prompts `a3-script` v3 + `a4-check` v2, which add English as a family language): A3 **12/12 first pass** on the 10 cases plus 2 English ones (`add-nocarry-en`, `pv-expand-two-targets-en`; English scripts keep the 3 Kannada classroom words, verifier's script check counts Latin letters); A4 **8/8** with an English good script and an English shaming one added (`evals/results/2026-10-23-a3-script.json`, `2026-10-23-a4-check.json`). English words per second (2.2) is a guess until measured; the first English catch-up ran 55 s.

**Hindi** (25 Oct, T-59, D-78, prompts `a3-script` v4 + `a4-check` v3, Opus 5.5 + Haiku 5.5): A3 **14/14 first pass** on the 12 earlier cases plus 2 Hindi ones (`sub-borrow-hi`, `add-carry-hi`; the three Kannada classroom words stay in Kannada script, the rest is Devanagari); A4 **8/8** on v3 (`evals/results/2026-10-25-a3-script.json`, `2026-10-25-a4-check.json`). A4 has Hindi-script cases too (below). **Sarvam `hi-IN`** (Bulbul v3 "priya"): disclosure 6.4 s, the 110-word `sub-borrow-hi` script 43.1 s, whole catch-up 49.5 s and 135 KB, inside 95 s / 300 KB (`2026-10-25-tts.json`, audio in `evals/results/audio/`). That is **2.55 words a second** from one script, so `Verifier:WordsPerSecond.hi` 2.2 errs long; retune once there is a second sample. **A4 with Hindi** (`a4-check` v3): 3 cases added to `evals/cases/a4-check.json` (a good Hindi script, the same with a shaming line added, a Hindi subtraction script labelled as addition), **11/11 correct** (`2026-10-25-a4-check.json`). Still to do: a native Hindi speaker listens to `disclosure-hi.ogg` and `sub-borrow-hi.ogg` and reads the scripts (T-26).

**Lesson recap, A8** (26 Oct, T-62, prompt `a8-lesson-recap` v1, Opus 5.5): **15/15 valid, 13/15 first pass (86.7%)** against the 85% target, on 15 cases across kn / te / ta / hi / en built from curriculum data only (no names, no teacher examples). Each script is checked by code: at most 80 words, no names or braces, at least 80% in the family's script, only textbook numbers (`evals/cases/a8-lesson-recap.json`, `evals/results/2026-10-26-a8-lesson-recap.json`, `tests/api/.../Evals/A8LessonRecapEvals.cs`). Native-speaker listen and read is still to do (T-26).

**A4 content check** (`evals/cases/a4-check.json`: 2 good A3 scripts + 4 crafted flaws: wrong topic, shaming, mismatched summary, too hard; pass = Haiku's verdict equals `expectPass`): **6/6** (`evals/results/2026-10-22-a4-check.json`). The first run was 5/6: Haiku failed a good Kannada script because the summary mentioned household objects it hadn't been shown; it now gets the object option and judges "same lesson, numbers and homework", not extra detail.

**Text-to-speech** (23 Oct, `TtsSmokeEvals`, Bulbul v3 "priya", 16 kHz → Opus 24 kbps; `evals/results/2026-10-23-tts.json`, audio in `evals/results/audio/` for a listen on an Android phone): disclosure 4.9–6.7 s; scripts of 94–96 words took 48–51 s, so **1.86 (kn), 1.91 (te), 1.96 (ta) words a second**; whole catch-ups 53–57 s and 139–154 KB, inside 95 s / 300 KB. `Verifier:WordsPerSecond` retuned to kn 1.8, te 1.85, ta 1.9 (rounded down so the estimate errs long). Sarvam's own MP3 (fixed 128 kbps) would have been ~860 KB, hence the Opus re-encoding.

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
