# Intent: Tappida Paatha ("The Missed Lesson") — Build-stage MVP

| | |
|---|---|
| Stage | 1 · Plan |
| Originator | Rahul Oswal (engineering) |
| Product owner (accepts) | Monika I |
| Status | **Accepted** by Monika, 20 Oct 2026; revised after the education-mentor review (D-20 to D-25, `docs/mentor-feedback-2026-10-19.md`) |
| Challenge | AI for Foundational Learning Hackathon, Problem 1C: student absenteeism |
| Deadline | Submit 31 Oct 2026 (hard deadline 1 Nov, 23:59 IST) |

## Problem

In a Kannada-medium government primary class, about 4 of 36 children are absent on any day (ASER 2024, rural Karnataka). Each missed day is now a missed lesson, because Karnataka is replacing self-paced Nali-Kali with lesson-by-lesson textbook teaching in 2026–27. The teacher:

1. cannot keep track of which child missed which lesson,
2. has no class time to reteach one child, and
3. cannot decide whether to move on or repeat a lesson.

Children of seasonal and migrant workers, often Telugu- or Tamil-speaking, are hit hardest.

## Proposed outcome

Every absent child stays connected to their class so they come back sooner. While a child is away, the family hears from their own teacher each evening: what the class did today, homework from the child's own textbook, and a buddy keeping their seat. When several days are missed, the child gets one short catch-up in their home language, based on what the teacher taught. The teacher sees who is back, how soon, and why children were away. She spends under 60 seconds a day and types nothing.

The education mentor (19 Oct) pointed out that at this age home time gives engagement more than learning, and that the real goal is a **pull back to school**. So return speed leads and the learning check is a secondary signal (D-20).

The MVP proves one complete loop, end to end, with real AI and real audio:

> Teacher records what she taught → Claude maps it to textbook competencies and logs it against absent children → Claude compresses several missed lessons into one catch-up that keeps only what blocks next week's lessons → teacher approves once → parent receives a link, taps Listen, child answers by tapping one of three pictures → teacher sees "caught up" or "needs buddy", a return-day buddy card, and a weekly pacing nudge.
>
> Around it: each evening of absence the family gets a short message from a template the teacher approved once (today's lesson, a textbook-page homework, "a classmate is keeping the seat"); the parent can say why the child was away; on return the teacher taps "Brought homework"; a weekly card shows how quickly absent children came back.

## Affected users and systems

| User | Role in MVP |
|---|---|
| **Lakshmi**, Grade 2–3 teacher, Kolar district (primary) | Records lesson, checks the absent list that arrives from the school's system (corrects it if wrong), approves scripts, uses ledger, buddy cards and pacing nudge |
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

**Product measures we will report (D-20, changed after the mentor review):**

1. **Return speed (primary):** days until an absent child is back in class, and the share of absent children back within 2 school days.
2. **Home engagement:** the family listened, and the child brought the homework back.
3. **Learning check (secondary):** share of absentees who pass the picture check within 48 hours of return.

Plus teacher seconds per day. We never count message opens.

## Out of scope for the MVP

Voice cloning, avatar video, CRP/officer dashboards, a real SATS/VSK attendance integration (the MVP simulates the feed and keeps manual marking as a fallback; roadmap, D-27), home-visit pathways via the cook or community members (roadmap, D-25), board-photo capture and spoken child answers (D-26), subjects other than Grade 2–3 maths, production WhatsApp template approval at scale, native apps.

## Open questions

1. ~~Demo subject and unit~~ Resolved: Grade 3 maths, place value → carrying → borrowing (D-01).
2. Resolved as D-16 (3 teachers + 2 parents or proxies); recruiting is T-20. Do we have 3 real teachers we can reach this week (schools are reportedly on Dasara break until about 21 Oct)? If not, who are the proxies?
3. **[CONFIRM: Rahul]** Open-source the repo (MIT) for the +USD 250 credits?
4. ~~Real WhatsApp delivery as a stretch?~~ Resolved: simulated parent phone primary, WhatsApp stretch (D-05).
