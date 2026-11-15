# Demo shell · spec

The judges' guided demo: teacher phone and family phone side by side, Reset, Under the hood.

| | |
|---|---|
| Route | `/demo`; each panel on its own at `/demo/ai` and `/demo/family/:childId` (T-126) |
| Web | `features/demo/` |
| API | `Features/Demo/` |
| Design card (Monika) | none yet |
| Status | [`status.md`](status.md) |

## Purpose and states

Demo shell (`/demo`): teacher phone and parent phone side by side, a step guide for judges, and Reset. The family phone is a chat view (`design/tokens.md`, "Family phone chat"; `design/P0-messages-reason.md`).

Each panel also opens on its own (T-126). `/demo/ai` shows Under the hood: every AI call since the last reset. `/demo/family/:childId` shows the family phone of any demo-class child: full screen on a phone, the 360 px frame from `lg`. An id outside the demo class shows a "no child with this link" message. `/demo` links to both from its step guide. [CONFIRM: Monika] the wording of the two links and of the not-found message.

## Requirements

Moved as they are from `spec.md` §2 (15 Nov 2026). This file is now where they are kept; `spec.md` §2 has the index.

| ID | P | Requirement | Acceptance |
|---|---|---|---|
| FR-14 | M | Demo mode: split screen with teacher phone and parent phone; "Reset demo" reseeds the class | A judge completes the loop in ≤ 5 minutes without instructions beyond the landing page |

## Decisions cited

D-94.

D-01 to D-99 are in [`../../decisions/D-01-to-D-99.md`](../../decisions/D-01-to-D-99.md); later ones are one file each in [`../../decisions/`](../../decisions/README.md).
