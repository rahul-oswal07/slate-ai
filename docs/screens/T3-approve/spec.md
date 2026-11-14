# T3 Check and approve · spec

Two tabs: today's message (edit, approve) and the week's catch-ups (listen, approve).

| | |
|---|---|
| Route | `/approve`, `/approve/:id` |
| Web | `features/approve/` |
| API | `Features/TodaysMessage/`, `Features/CatchUps/`, `Features/Audio/` |
| Design card (Monika) | [`T3-approve.md`](../../design/T3-approve.md) |
| Status | [`status.md`](status.md) |

## Purpose and states

| Screen | Purpose | Key interaction | States to design (besides default) |
|---|---|---|---|
| T3 **Approve** | Draft catch-up packages for the week | Play, read gloss, tick, Approve / Regenerate; approve the checked ones; Withdraw | Generating · ready · needs review (verifier failed) · approved · sent · generation failed · offline (approval queued) |

Every state, with copy and screenshots, is in the design card.

## Requirements

Moved as they are from `spec.md` §2 (15 Nov 2026). This file is now where they are kept; `spec.md` §2 has the index.

| ID | P | Requirement | Acceptance |
|---|---|---|---|
| FR-08 | M | Teacher approval: weekly list of draft packages; play audio, read Kannada gloss, Approve or Regenerate ("simpler", "shorter"); bulk approve only for packages she has opened and ticked; Withdraw until the 6 pm send job starts, labelled "Withdraw (until 6 pm)"; after that the status is Sent (D-28, D-37) | No package reaches a parent without approval (enforced in API) |

## Decisions cited

D-09, D-18, D-28, D-35, D-37, D-38, D-50, D-52, D-57, D-58, D-68, D-70, D-90, D-96.

D-01 to D-99 are in [`../../decisions/D-01-to-D-99.md`](../../decisions/D-01-to-D-99.md); later ones are one file each in [`../../decisions/`](../../decisions/README.md).
