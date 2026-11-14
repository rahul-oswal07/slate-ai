# T5 Back today · spec

Buddy cards for the children who are back today.

| | |
|---|---|
| Route | `/back` |
| Web | `features/back/` |
| API | `Features/Buddy/` |
| Design card (Monika) | [`T5-back-today.md`](../../design/T5-back-today.md) |
| Status | [`status.md`](status.md) |

## Purpose and states

| Screen | Purpose | Key interaction | States to design (besides default) |
|---|---|---|---|
| T5 **Return day** | Buddy cards for children back today | Previous / Next buttons ("Card 1 of 2"); print/share; "Brought homework" | None due today · marked done · offline (cached cards) |

Every state, with copy and screenshots, is in the design card.

## Requirements

Moved as they are from `spec.md` §2 (15 Nov 2026). This file is now where they are kept; `spec.md` §2 has the index.

| ID | P | Requirement | Acceptance |
|---|---|---|---|
| FR-12 | M | Return-day buddy card for every returning child, whether or not the family has a phone: which classmate, 3 sums, key words, 10 minutes; the teacher taps **Brought homework** (D-22) | Buddy is a child who was present for those lessons; card is cached offline and printable |

## Decisions cited

D-20, D-22, D-29, D-59.

D-01 to D-99 are in [`../../decisions/D-01-to-D-99.md`](../../decisions/D-01-to-D-99.md); later ones are one file each in [`../../decisions/`](../../decisions/README.md).
