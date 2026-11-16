# T6 Week · spec

Return speed, reasons for absence, the pacing nudge, attendance stars.

| | |
|---|---|
| Route | `/week` |
| Web | `features/week/` |
| API | `Features/Week/`, `Features/Stars/` |
| Design card (Monika) | [`T6-week.md`](../../design/T6-week.md) |
| Status | [`status.md`](status.md) |

## Purpose and states

| Screen | Purpose | Key interaction | States to design (besides default) |
|---|---|---|---|
| T6 Pacing | Weekly nudge | Read; "Plan recap" adds a reminder | No nudge this week ("Most children are on track") · decided |

Every state, with copy and screenshots, is in the design card.

## Requirements

Moved as they are from `spec.md` §2 (15 Nov 2026). This file is now where they are kept; `spec.md` §2 has the index.

| ID | P | Requirement | Acceptance |
|---|---|---|---|
| FR-13 | M | Weekly pacing nudge, e.g. "11 of 36 missed carrying-over. Recap 10 minutes before moving on." Shown when 5 or more of the class missed the same lesson this week; otherwise "No nudge this week" (D-60) | Numbers come from the ledger by code; in the MVP the sentence is a fixed template filled by code (no AI; A5 wording deferred, T-46); "Plan recap" / "Not this week" stored, and a planned recap shows on Today the following week |
| FR-22 | M | **Return-speed card** (D-24): weekly card for the teacher with absences by length (1, 2, 3, 4+ days) this week vs last, the share back within 2 school days, and reason categories | Numbers computed by code from attendance; teacher's own view; nothing exported to officers |
| FR-24 | S | **Weekly star** (T-125, D-118, replacing D-81's rule): recognise coming to school, never punish absence. A child earns the week's star by being present on **every** school day (Mon–Sat; a day with no register isn't one). Each week stands alone: nothing is counted or carried forward, and catch-ups add nothing. The teacher's T6 Week lists that week's children in roll order (the current week: on track so far), with no numbers. On Saturday at 6 pm, once the register is in, each of their families with consent Yes, not stopped and a saved number gets `weekly_star_v1`, whose link opens a message and a star badge (P6). She approves the wording once in setup (guardrail 3). The catch-up and today's message Done pages keep their star picture and words | Worked out in code from attendance, no AI; no ranking, no counts and no list of children without a star; families never see numbers or other children |

## Decisions cited

D-20, D-23, D-24, D-60, D-61, D-68, D-81; [D-118](../../decisions/D-118-weekly-star-every-day.md) (the weekly star, every day present).

D-01 to D-99 are in [`../../decisions/D-01-to-D-99.md`](../../decisions/D-01-to-D-99.md); later ones are one file each in [`../../decisions/`](../../decisions/README.md).
