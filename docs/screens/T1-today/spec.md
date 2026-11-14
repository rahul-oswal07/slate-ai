# T1 Today · spec

Attendance, recording today's lesson, tonight's message and today's answers on one screen.

| | |
|---|---|
| Route | `/` |
| Web | `features/today/`, `features/evening/` |
| API | `Features/Attendance/`, `Features/Lessons/`, `Features/LessonAudio/`, `Features/EveningMessages/`, `Features/TodaysMessage/` |
| Design card (Monika) | [`T1-today.md`](../../design/T1-today.md) |
| Status | [`status.md`](status.md) |

## Purpose and states

| Screen | Purpose | Key interaction | States to design (besides default) |
|---|---|---|---|
| T1 **Today** | Attendance (from SATS, D-27) + lesson capture + tonight's message on one screen ("Since yesterday" updates are roadmap, D-53) | Check the absent list, correct only if wrong; tap the mic (tap again to stop); sync badge | Attendance not in yet (mark here instead) · corrected by teacher · lesson already recorded today · offline ("3 waiting to send") · sending · mic permission denied |

Every state, with copy and screenshots, is in the design card.

## Requirements

Moved as they are from `spec.md` §2 (15 Nov 2026). This file is now where they are kept; `spec.md` §2 has the index.

| ID | P | Requirement | Acceptance |
|---|---|---|---|
| FR-01 | M | Today's attendance arrives from the school's attendance system (a **simulated SATS feed** in the MVP, labelled as simulated; D-27). The teacher only corrects mistakes. A server job syncs it at **12:00** and retries at **2:00 pm** (D-45); she can mark absentees in the app at any time, one tap per absent child (all present by default). If nothing arrives, nobody is marked absent automatically. When nobody is absent, Today says "Everyone is here today." and "No messages tonight." (D-54) | Absent list shows its source and time; corrections and fallback marking work offline; corrections are never written back to SATS; fallback: 4 absentees marked in ≤ 10 s |
| FR-02 | M | Teacher records a voice note (≤ 30 s, Kannada) saying what she taught: tap to start, tap to stop (holding also works); elapsed time against the limit; auto-stop at 30 s; works with keyboard and screen reader (D-31) | Recording queued offline; uploaded on reconnect |
| FR-17 | S | ~~Board photo capture via Claude vision~~ Dropped (D-26); back in a smaller form from teacher testing (F-05, T-64, D-82): **one optional photo of the lesson** (textbook page or board) on Today's lesson card, sent with her voice note or alone ("Match from photo"). It is an extra input to A1 only: kept on her phone, never stored on the server, never sent to families. A note that can't be heard is still matched from the photo | Same output as FR-03; the hint says "the page or the board, not the children"; A1's wording is name-scrubbed again after Claude (a name on the board can't be stored) |
| FR-23 | S | **Optional lesson audio on the evening message** (T-62, D-80): a checkbox on Today's tonight card, **unticked each day**, "Also send today's lesson as audio". Ticking makes one AI audio clip per (lesson, language) of tonight's families (A8: a few spoken sentences about what the class learned, curriculum data only, no names, numbers only from the textbook examples), shared across children, days and classes. The teacher **listens to each clip and approves it**; only an approved clip goes with that day's message, behind the spoken AI disclosure, with the AI label and the written words. A child absent several days gets each day's own clip. The coded message (FR-20) is the same with or without it | Nothing AI-written reaches a parent without her approval of that exact clip (endpoint tests); unticked, unapproved, failed, regenerated-after-approval or other-language clips never go; the evening send path calls no LLM; read-only once the messages are sent. **(T-82)** The 6 pm job waits for a class while a ticked clip is being made or awaits her approval, and **(T-85)** gives a ready clip 5 minutes for her to check it (`EveningMessages:LessonAudioWait`, from when it was ready, or from 6 pm if earlier), then the plain message goes; there is no fixed cutoff, so a late tick works too; a failed or unticked clip doesn't hold it |

## Decisions cited

D-03, D-17, D-21, D-26, D-27, D-31, D-33, D-34, D-39, D-45, D-46, D-51, D-53, D-54, D-55, D-67, D-68, D-69, D-70, D-72, D-80, D-82.

D-01 to D-99 are in [`../../decisions/D-01-to-D-99.md`](../../decisions/D-01-to-D-99.md); later ones are one file each in [`../../decisions/`](../../decisions/README.md).
