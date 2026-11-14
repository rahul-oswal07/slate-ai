# T2 Lesson confirm · spec

The lessons matched from her note or photo, as a checklist she confirms; the lesson picker.

| | |
|---|---|
| Route | `/lesson` |
| Web | `features/lessons/` |
| API | `Features/Lessons/` |
| Design card (Monika) | [`T2-lesson-confirm.md`](../../design/T2-lesson-confirm.md) |
| Status | [`status.md`](status.md) |

## Purpose and states

| Screen | Purpose | Key interaction | States to design (besides default) |
|---|---|---|---|
| T2 Lesson confirm | Shows the 1–3 mapped lessons as a ticked checklist (D-33) | Untick wrong ones, one tap "Confirm N lessons"; "Add another lesson"; or pick from chapter list | Processing · slow (> 20 s, pick from list) · confident (checklist) · low confidence ("Looks like one of these. Please choose.") · changed by teacher · could not hear the note (pick from list) · offline (note saved, matched later) |

Every state, with copy and screenshots, is in the design card.

## Requirements

Moved as they are from `spec.md` §2 (15 Nov 2026). This file is now where they are kept; `spec.md` §2 has the index.

| ID | P | Requirement | Acceptance |
|---|---|---|---|
| FR-03 | M | System transcribes the note and Claude maps it to 1–3 competency IDs from the curriculum map, with a one-line Kannada summary and the examples she used | Teacher sees every matched lesson ticked, unticks any that are wrong, and confirms with one tap ("Confirm 2 lessons"); "Add another lesson" and "Change" use the chapter list (D-33) |
| FR-04 | M | Fallback when audio or AI fails: teacher picks the lesson from the chapter list | One tap; ledger still updates |

## Decisions cited

D-14, D-17, D-22, D-31, D-33, D-38, D-56.

D-01 to D-99 are in [`../../decisions/D-01-to-D-99.md`](../../decisions/D-01-to-D-99.md); later ones are one file each in [`../../decisions/`](../../decisions/README.md).
