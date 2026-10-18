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

1. Read attendance from SATS / Vidya Samiksha Kendra so the teacher enters nothing.
2. Consented voice clone of the teacher for languages she speaks.
3. All Grade 1–3 maths and Kannada literacy units; Tamil, Urdu, Marathi home languages.
4. Portable learning record that follows seasonal migrant children between schools.
5. Pilot with one block in Kolar; measure 48-hour catch-up rate.

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
