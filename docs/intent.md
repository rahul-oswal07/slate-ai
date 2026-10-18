# Intent: Tappida Paatha ("The Missed Lesson") — Build-stage MVP

| | |
|---|---|
| Stage | 1 · Plan |
| Originator | Rahul Oswal (engineering) |
| Product owner (accepts) | Monika I |
| Status | **Draft — awaiting Monika's acceptance** (accept = merge to `main`) |
| Challenge | AI for Foundational Learning Hackathon, Problem 1C: student absenteeism |
| Deadline | Submit 31 Oct 2026 (hard deadline 1 Nov, 23:59 IST) |

## Problem

In a Kannada-medium government primary class, about 4 of 36 children are absent on any day (ASER 2024, rural Karnataka). Each missed day is now a missed lesson, because Karnataka is replacing self-paced Nali-Kali with lesson-by-lesson textbook teaching in 2026–27. The teacher:

1. cannot keep track of which child missed which lesson,
2. has no class time to reteach one child, and
3. cannot decide whether to move on or repeat a lesson.

Children of seasonal and migrant workers, often Telugu- or Tamil-speaking, are hit hardest.

## Proposed outcome

Every absent child gets a short catch-up for the lesson they missed, in their home language, using what their own teacher taught, and a teacher-visible result showing whether they caught up. The teacher spends under 60 seconds a day and types nothing.

The MVP proves one complete loop, end to end, with real AI and real audio:

> Teacher records what she taught → Claude maps it to textbook competencies and logs it against absent children → Claude compresses several missed lessons into one catch-up that keeps only what blocks next week's lessons → teacher approves once → parent receives a link, taps Listen, child answers by tapping one of three pictures → teacher sees "caught up" or "needs buddy", a return-day buddy card, and a weekly pacing nudge.

## Affected users and systems

| User | Role in MVP |
|---|---|
| **Lakshmi**, Grade 2–3 teacher, Kolar district (primary) | Records lesson, marks absentees, approves scripts, uses ledger, buddy cards and pacing nudge |
| **Ravi (7) and his mother**, Telugu-speaking seasonal-worker family (secondary) | Receive catch-up on a shared phone after 6 pm; child taps a picture to answer |
| **Buddy classmate** (e.g. Anjali) | Follows a 10-minute buddy card on return day; no device |
| **Judges** | Need a working demo link they can test without a WhatsApp account or a Kannada keyboard |

Systems: Claude API (reasoning, vision, script writing, checks), Sarvam AI (Indic speech-to-text and text-to-speech), WhatsApp Business Cloud API (stretch), PostgreSQL, Azure hosting.

## Constraints

- Teacher phone: low-cost Android, intermittent data → inputs must queue offline; audio (~300 KB), never video.
- No child devices in school; no smart boards.
- Teacher time: under 60 s/day, no typing, one approval per lesson (not per child).
- Teachers distrust monitoring → ledger is the teacher's own; no officer or CRP view in the MVP.
- Child data: parent opt-in; store first name and parent number only; no child names or numbers sent to the LLM; delete on request (DPDP Act 2023).
- AI output can be wrong → every script grounded in the textbook, verified, and approved by the teacher before a parent sees it.
- Team: two people (Monika: product/design; Rahul: engineering), ~10 working days, USD 500 Claude credits.
- Stack: React front end, .NET back end.

## Success measures

**For the MVP and judging (by 31 Oct):**

| Measure | Target |
|---|---|
| Full loop works live on the deployed link, Kannada lesson → Telugu catch-up → picture reply → ledger update | 5 consecutive clean runs in rehearsal |
| Teacher time per day, measured with test teachers | ≤ 60 s median |
| Competency mapping accuracy on the eval set | ≥ 90% top-1 on 25 cases |
| Scripts passing the verifier (correct arithmetic, right language, ≤ 90 s, key words present) | 100% of approved scripts; ≥ 85% first-pass |
| Teachers tested with and changes made from their feedback | ≥ 3 teachers (or proxies), 2 rounds, ≥ 5 logged changes |
| Teacher flow works offline (capture queued, synced on reconnect) | Demonstrated on video |

**Product measure we will report (from the proposal):** share of absentees who pass the learning check within 48 hours of return; teacher seconds per day; fewer whole-class repeat lessons. We measure a passed learning check, never message opens.

## Out of scope for the MVP

Voice cloning, avatar video, CRP/officer dashboards, SATS/VSK integration, subjects other than Grade 2–3 maths, production WhatsApp template approval at scale, native apps.

## Open questions

1. **[DECIDE: Monika]** Demo subject and unit: Grade 2–3 maths, place value → addition with carrying → subtraction with borrowing (recommended; matches "carrying-over" in our proposal and the 19.3% subtraction statistic).
2. **[DECIDE: Monika + Rahul]** Do we have 3 real teachers we can reach this week (schools are reportedly on Dasara break until about 21 Oct)? If not, who are the proxies?
3. **[CONFIRM: Rahul]** Open-source the repo (MIT) for the +USD 250 credits?
4. **[CONFIRM: Rahul]** Real WhatsApp delivery as a stretch, with the simulated parent phone as the primary demo path?
