# P1–P4 Catch-up at home · spec

The family's catch-up: Listen → Activity → Answer → Done, and the stop link.

| | |
|---|---|
| Route | `/p/:token` (kind `catchUp`) |
| Web | `features/parent/` (`ListenStep`, `ActivityStep`, `AnswerStep`, `DoneStep`, `Sticks`) |
| API | `Features/Parent/`, `Features/CatchUps/` |
| Design card (Monika) | [`P1-listen.md`](../../design/P1-listen.md) · [`P2-activity.md`](../../design/P2-activity.md) · [`P3-answer.md`](../../design/P3-answer.md) · [`P4-done.md`](../../design/P4-done.md) |
| Status | [`status.md`](status.md) |

## Purpose and states

| Screen | Content | States to design (besides default) |
|---|---|---|
| P1 Listen | Teacher photo initial, big ▶ Listen (greeting + catch-up), "AI-assisted, approved by Lakshmi teacher" | Playing · paused · played · network dropped mid-play (resume) · link expired |
| P2 Activity | One picture of the household-object activity, audio instruction replays | Audio replaying |
| P3 Answer | "Ravi, tap the answer" read aloud; 3 large pictures | Picture chosen (numeral shown; tap another picture to change) · sending · could not send (answer saved, try again) |
| P4 Done | Positive sound, "Lakshmi teacher will see this" | none |

Every state, with copy and screenshots, is in the design card.

## Requirements

Moved as they are from `spec.md` §2 (15 Nov 2026). This file is now where they are kept; `spec.md` §2 has the index.

| ID | P | Requirement | Acceptance |
|---|---|---|---|
| FR-10 | M | Parent page from a link: big Listen button, activity picture, then 3-picture answer; audio prompts so a low-literacy parent can follow | Lighthouse mobile performance ≥ 85; total transfer ≤ 500 KB excluding audio |
| FR-11 | M | Picture-tap answer is scored by code (no LLM) and updates the ledger: correct → caught up; wrong → needs buddy | Ledger updates within 2 s of the tap |
| FR-19 | M | **Stop these messages** *(D-19, D-30)*: a stop link on every parent screen and in the evening message, plus a STOP reply; one tap plus one confirmation stops all future messages for that child; one confirmation message with a restart link; reversible. The ledger shows "Stopped by family" and the child stays on the buddy-card path | After a stop, the API refuses to send to that child (409), covered by an integration test; the ledger shows the status within 2 s |

## Decisions cited

D-07, D-08, D-18, D-19, D-20, D-22, D-30, D-50, D-52, D-62, D-70, D-91, D-95, D-98.

D-01 to D-99 are in [`../../decisions/D-01-to-D-99.md`](../../decisions/D-01-to-D-99.md); later ones are one file each in [`../../decisions/`](../../decisions/README.md).
