# Admin pages · spec

Read-only pages to play teachers' greetings and a day's lesson audio.

| | |
|---|---|
| Route | `/admin/greetings`, `/admin/lesson-audio` |
| Web | `features/admin/` |
| API | `Features/Admin/` |
| Design card (Monika) | none yet |
| Status | [`status.md`](status.md) |

## Purpose and states

Read-only, linked from nowhere, protected by the `Admin:Key` secret (sent as a header, never in a request URL); no key configured means no endpoints (D-83).

## Requirements

No FR of its own. Rules in D-83; built behaviour in T-66, T-67, T-68 (`archive/tasks-until-2026-11-15.md`).

## Decisions cited

D-83.

D-01 to D-99 are in [`../../decisions/D-01-to-D-99.md`](../../decisions/D-01-to-D-99.md); later ones are one file each in [`../../decisions/`](../../decisions/README.md).
