# Admin pages · spec

Pages to play teachers' greetings and a day's lesson audio, set each lesson's video link, and set each family's WhatsApp number.

| | |
|---|---|
| Route | `/admin`, `/admin/greetings`, `/admin/lesson-audio`, `/admin/lesson-links`, `/admin/children` |
| Web | `features/admin/` |
| API | `Features/Admin/` |
| Design card (Monika) | none yet |
| Status | [`status.md`](status.md) |

## Purpose and states

Linked from nowhere, protected by the `Admin:Key` secret (sent as a header, never in a request URL); no key configured means no endpoints (D-83). When the API runs in Development (local) the pages open without a key; staging and prod still need it (D-102).

## Requirements

No FR of its own. Rules in D-83 and D-102; built behaviour in T-66, T-67, T-68 (`archive/tasks-until-2026-11-15.md`), T-94 (lesson links, D-106) and T-118 (children and their WhatsApp numbers, D-110).

## Decisions cited

D-83, D-102, D-106, D-110.

D-01 to D-99 are in [`../../decisions/D-01-to-D-99.md`](../../decisions/D-01-to-D-99.md); later ones are one file each in [`../../decisions/`](../../decisions/README.md).
