# T4 Missed lessons · spec

The ledger: who missed which lesson, by child or by lesson; child history; delete a child's data.

| | |
|---|---|
| Route | `/missed`, `/missed/:childId` |
| Web | `features/missed/` |
| API | `Features/Ledger/`, `Features/Children/` |
| Design card (Monika) | [`T4-missed-lessons.md`](../../design/T4-missed-lessons.md) |
| Status | [`status.md`](status.md) |

## Purpose and states

| Screen | Purpose | Key interaction | States to design (besides default) |
|---|---|---|---|
| T4 **Missed lessons** (ledger) | Per-child missed lessons with learning status and separate channel status; by-lesson view | By child / by lesson; filter: absent this week; tap child for history; delete child data; link to T8 Families | Empty (first week) · filtered · by lesson · offline ("Showing the saved list …") · delete confirm |

Every state, with copy and screenshots, is in the design card.

## Requirements

Moved as they are from `spec.md` §2 (15 Nov 2026). This file is now where they are kept; `spec.md` §2 has the index.

| ID | P | Requirement | Acceptance |
|---|---|---|---|
| FR-05 | M | Ledger: for each child, the lessons missed with a **learning status** per lesson (missed · sent · caught up · needs buddy) and a separate **channel** per child (stopped by family · no phone · no consent), so a stop never hides "caught up" (B-08, T-13) | Matches attendance × lessons exactly (deterministic) |
| FR-15 | M | Consent and deletion: parent opt-in comes from the paper consent form and is recorded by the teacher per child (Yes / No / No phone, with date) on T8 Family details, together with the parent's phone number (D-43) and the child's home language (D-47) (D-39); nothing is sent unless Yes; teacher can delete a child's data | Send endpoints refuse children not marked Yes (409, integration test); delete removes child, responses and audio; verified by integration test |

## Decisions cited

D-17, D-19, D-20, D-27, D-30, D-35, D-36, D-39, D-43, D-47, D-70.

D-01 to D-99 are in [`../../decisions/D-01-to-D-99.md`](../../decisions/D-01-to-D-99.md); later ones are one file each in [`../../decisions/`](../../decisions/README.md).
