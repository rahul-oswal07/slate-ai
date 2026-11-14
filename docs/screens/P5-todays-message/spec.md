# P5 Today's message at home · spec

The family's page for today's message: her voice, the reason, concept, activity, the `>,<,=` question, Done.

| | |
|---|---|
| Route | `/p/:token` (the evening link, when today's message is attached) |
| Web | `features/parent/` (`TodaysPages`, `ReasonView`, `Sticks`) |
| API | `Features/Parent/`, `Features/TodaysMessage/` |
| Design card (Monika) | none yet |
| Status | [`status.md`](status.md) |

## Purpose and states

Today's message rides on the evening message's link. When an approved message is attached, the link opens step by step instead of the plain evening page. **No design card yet**, and `P5` is a new screen ID (15 Nov, T-110).

## Requirements

No FR of its own yet. It follows D-91 (today's message, approved every day), D-93 (the picture question and the book's exercises), D-95 (levels from the answer) and D-98 (the reason step); the reason step is FR-21 ([P0](../P0-evening-message/spec.md)). The built behaviour is written up in T-90 and T-91 (`archive/tasks-until-2026-11-15.md`).

## Decisions cited

D-91, D-93, D-95, D-98.

D-01 to D-99 are in [`../../decisions/D-01-to-D-99.md`](../../decisions/D-01-to-D-99.md); later ones are one file each in [`../../decisions/`](../../decisions/README.md).
