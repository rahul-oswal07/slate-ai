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
| FR-24 | S | **Attendance stars** (T-63, D-81): recognise coming to school and coming back, never punish absence. A weekly star for each finished school week (Mon–Sat) with at least one day present and at most 1 day away (`Stars:MaxDaysAwayPerWeek`); a catch-up star for each catch-up answered at home, whatever the answer. Stars add up over the term and never go away. The teacher's T6 Week shows who earned one (or is on track this week) in roll order with their term total; P4 Done tells the family "{child} earned a star for doing this catch-up." Plan: `docs/archive/T-63-attendance-stars.md` | Counted in code from attendance and check responses, no AI; no ranking and no list of children without a star; families never see numbers or other children |

## Decisions cited

D-20, D-23, D-24, D-60, D-61, D-68, D-81.

D-01 to D-99 are in [`../../decisions/D-01-to-D-99.md`](../../decisions/D-01-to-D-99.md); later ones are one file each in [`../../decisions/`](../../decisions/README.md).
