# Judging narrative

Used for the 3-minute video, the submission deck and (if shortlisted) the 18 Nov pitch. Numbers marked **[fill]** come from our own testing and evals; do not publish them until measured.

## 1. Problem

On any school day, about 4 children in a rural Karnataka class of 36 are absent (ASER 2024). With Karnataka moving from Nali-Kali to lesson-by-lesson textbook teaching in 2026–27, each missed day is a missed lesson. The teacher cannot track who missed what, has no class time to reteach one child, and has to guess whether to move on or repeat. Children of seasonal and migrant workers, often Telugu- or Tamil-speaking in Kannada-medium schools, fall behind most.

## 2. Target user

- **Lakshmi**, Grade 3 teacher in Kolar: one Android phone, patchy data, 36 children, no spare minutes, wary of being monitored.
- **Ravi (7) and his mother**: Telugu-speaking, seasonal work, one shared phone in the evening, limited literacy.

## 3. Solution

Tappida Paatha gives every absent child the lesson they missed, based on what their own teacher taught, in their home language, at home on the family phone and again on the day they return.

The teacher's daily effort: mark absentees and record a 20-second voice note saying what she taught. Under 60 seconds, no typing.

What she gets back:
1. A missed-lesson ledger that fills itself.
2. A 90-second home-language catch-up for each absent child, with a 5-minute activity using household objects and a 3-picture learning check, approved by her once per lesson.
3. A 10-minute buddy card for every returning child, with or without a phone at home.
4. A weekly pacing nudge: "11 of 36 missed carrying-over. Recap 10 minutes before moving on."

## 4. Live demo flow (≈ 4 minutes live; 2:50 in the video)

| Step | Screen | What we say |
|---|---|---|
| 1 | Teacher phone, Today | Lakshmi taps Ravi and three others absent, then records "Today we did addition with carrying using bundles of sticks" in Kannada |
| 2 | Lesson confirm | Claude mapped it to *Addition with carrying (2-digit)*, grounded in the Grade 3 textbook. One tap to confirm |
| 3 | Ledger | Ravi has now missed 4 lessons this week |
| 4 | Approve | Claude compressed 4 lessons into one catch-up and dropped the one that does not block next week; it explains why. Telugu audio plays with Lakshmi's own greeting; she reads the Kannada summary and approves |
| 5 | "Under the hood" | Each AI call, the model, the verifier checks (arithmetic recomputed, language, length) |
| 6 | Parent phone | Ravi's mother taps Listen; activity with bottle caps; Ravi taps one of three pictures |
| 7 | Ledger | Ravi: place value caught up, carrying needs buddy |
| 8 | Return day | Buddy card: pair with Anjali, 10 minutes, 3 sums, 3 Kannada words |
| 9 | Pacing | Weekly nudge for the whole class |
| 10 | Offline | Network off → capture queues → network on → synced |

## 5. Evidence of impact

- **Research basis:** weekly 20-minute phone tutorials plus SMS raised numeracy by 0.33 SD across six countries including India; government teachers were as effective as NGO tutors; one-way messages alone did little (J-PAL 2023; Angrist et al., NBER 31208). Our design keeps what worked (short, targeted, two-way) and adds the return-day buddy path.
- **Our testing:** **[fill]** teachers and **[fill]** parents across 2 rounds; median teacher time **[fill]** s/day; **[fill]** changes made from feedback (see `docs/user-testing.md`).
- **AI quality:** lesson-mapping accuracy **[fill]%** on 25 cases; **[fill]%** of scripts pass the verifier first time; 100% of parent-facing scripts teacher-approved.
- **What we will measure in a pilot:** share of absentees who pass the learning check within 48 hours of return; teacher seconds per day; fewer whole-class repeat lessons. We do not count message opens.

## 6. Technical approach

Layers, as the deck requires:

| Layer | What happens |
|---|---|
| Data input | Teacher voice note (Kannada) or chapter pick; attendance taps; child's picture tap |
| Processing | Sarvam speech-to-text → Claude maps transcript to competency IDs from a curriculum dependency graph → code computes which missed items block next week → Claude chooses at most 2 targets and writes the script, activity, check and buddy card → Sarvam text-to-speech |
| Guardrails | IDs restricted to the curriculum; targets must be in the code-computed set; verifier recomputes arithmetic, checks language, length, key words, objects; Claude (Haiku) checks tone and topic; one retry; teacher approval before anything reaches a parent; no child names sent to any AI provider; "AI-assisted" label |
| Evaluation | 75 eval cases across three Claude calls; every call logged with model, prompt version, tokens, verdict |
| Output | Teacher ledger, approved audio catch-up via link (WhatsApp in the stretch build), buddy card, pacing nudge |

Real-world conditions: teacher inputs queue offline and sync later; audio ≈ 300 KB, no video; buddy cards cached; the parent page is under 500 KB; children without a phone get the buddy card. What needs a connection: mapping, generation, approval sync, parent page load.

Stack: React PWA, .NET 10 API, PostgreSQL, Claude (Opus 5.5 for scripts, Sonnet 5.5 for mapping and compression, Haiku 5.5 for checks), Sarvam AI, Azure.

## 7. What makes it distinctive

1. **A discharge summary for a returning child.** Hospitals hand over a short note on what happened and what matters next. We apply that to a child coming back to class.
2. **Prerequisite-aware compression.** Claude reasons over the curriculum dependency graph and keeps only what blocks next week's lessons.
3. **This class's actual lesson.** The catch-up uses what this teacher taught on the days this child missed, opened by her own recorded greeting.
4. **It answers the pacing question.** Absence data becomes a concrete recap-or-move-on suggestion.

Comparison: Rocket Learning, Avanti Fellows, SwiftChat send the same content to everyone; Shiksha Copilot and VallamAI help lesson planning; VSK dashboards show absence to officers. None links attendance → missed concept → personalised catch-up → learning check → pacing decision.

## 8. Roadmap

Used for the deck (1–2 pages, 10% of the score), the in-demo roadmap and the 18 Nov pitch. Judges look for how the product develops and how it **integrates with existing systems**. Anything under Next or Later is out of scope for the build.

**North star:** share of absent children who pass the learning check within 48 hours of returning, plus teacher seconds per day (target < 60) and fewer whole-class repeat lessons. We never count message opens.

### Now: Build stage (by 31 Oct)

| Area | What ships |
|---|---|
| Core loop | Attendance + voice note → ledger → prerequisite-aware compression → teacher approval → parent link (WhatsApp in stretch) → picture-tap check → buddy card → pacing nudge |
| Real AI path | Sarvam Kannada STT → Claude lesson mapping → Claude compression and script → verifier (code + Haiku) → Sarvam Telugu/Kannada TTS |
| Guardrails | Curriculum-only IDs, code-computed candidate set, verifier with arithmetic recomputed, approval gate, no names to providers, consent and delete |
| Real conditions | Offline outbox, ~300 KB audio, cached buddy cards, parent page ≤ 500 KB |
| Evidence | 2 rounds of teacher testing, 75 eval cases, every AI call logged |

### Next: pilot (Nov 2026 – Mar 2027)

Week 1: ledger and script quality with 3 teachers → week 2: WhatsApp and voice → week 3: one class for two weeks. Then 5–10 classes in one Kolar block.

| Workstream | What | Evidence to collect |
|---|---|---|
| Real WhatsApp delivery | Verified sender, approved utility template, opt-in capture, audio after the parent taps Listen | Real cost per catch-up against the < ₹10 target |
| Attendance integration | Read attendance from **SATS** or **Vidya Samiksha Kendra** so the teacher enters nothing | Needs a data-sharing agreement with the state |
| Curriculum retrieval | Index over the full Karnataka Grade 1–3 maths and Kannada-literacy textbooks + NIPUN Bharat outcomes; teacher-reviewed dependency maps | Moves beyond the hand-built JSON; adds literacy |
| Teacher voice clone (consented, deletable) | Only for languages the teacher speaks; stock voice + her real greeting stays for others | A/B test against the stock voice for parent trust |
| Learning from teacher edits | Use "change" and "regenerate" corrections to improve prompts and examples | Fewer edits per approval over time |
| Feature-phone families | IVR call delivering the same audio via a missed-call callback | Reach without smartphones; buddy card stays the equal path |
| More home languages | Urdu, Hindi, Marathi for migrant families, with the same per-language evals | Driven by pilot class demographics |

### Later: scale and system integration (2027 onwards)

| Workstream | Integration |
|---|---|
| Portable learning record for seasonal migrant children: the ledger travels with the child, with consent | Inter-state learning-record standards; APAAR ID, with care for consent |
| More subjects and grades: Kannada literacy (Grades 1–3), then EVS | Same loop, new curriculum maps |
| Other states: Telangana, Andhra Pradesh, Tamil Nadu | State textbooks + state attendance systems |
| Aggregated, anonymised insights (e.g. "carrying is the most-missed concept in this block"), never per teacher or child, only with teacher consent | VSK dashboards, without breaking the "ledger stays with the teacher" rule |
| Partners already reaching parents on WhatsApp plug in missed-lesson catch-ups | Partnership, not competition |

### Integration principles

1. **The teacher enters nothing new:** attendance comes from SATS or VSK.
2. **The ledger stays with the teacher:** aggregation only above the school, anonymised, with consent.
3. **Grounded in the state curriculum:** Karnataka textbooks + NIPUN Bharat.
4. **Works on what families already have:** WhatsApp on a shared phone → IVR for feature phones → buddy card for no phone.
5. **Consent-first under the DPDP Act 2023:** verifiable parental consent, minimal data, delete on request.

### Risks we will track in the pilot

| Risk | Signal | Response |
|---|---|---|
| One-way messages do little on their own (J-PAL) | Learning-check pass rate flat | Strengthen the two-way check and buddy card; try the phone-tutorial format |
| Teachers stop recording | Teacher seconds rising; notes missing | Board photo as backup; shorter capture; ask why |
| Parents distrust AI voices | Stops; parents say it is hard to follow | Stronger human greeting; consented clone A/B test |
| Uneven language quality | A language scores lower in evals | Per-language alert; switch voice or provider; human-recorded key words |
| Cost rises with WhatsApp pricing | ₹ per catch-up above 10 | Batch per lesson, cache audio, IVR for some families |

## 9. Video storyboard (target 2:50)

| Time | Shot | Narration |
|---|---|---|
| 0:00–0:20 | Empty desk, attendance register | The problem in three sentences |
| 0:20–0:35 | Lakshmi and Ravi personas | Who we built for, and their constraints |
| 0:35–1:05 | Teacher phone: attendance + voice note → mapped lesson | Under 60 seconds, no typing |
| 1:05–1:35 | Approve: compression reason, Telugu audio, Kannada gloss, verifier | AI that explains itself; teacher stays in control |
| 1:35–2:05 | Parent phone: Listen, activity, picture tap | Voice-first, home language, large picture targets |
| 2:05–2:25 | Ledger update, buddy card, pacing nudge | Three outputs from one loop |
| 2:25–2:40 | Offline toggle; "Under the hood" | Works on patchy data; guardrails |
| 2:40–2:50 | Testing evidence numbers, team | What teachers told us and what we changed |
