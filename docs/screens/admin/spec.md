# Admin pages · spec

Pages to play teachers' greetings and a day's lesson audio, set each lesson's video link, and set each family's WhatsApp number and each child's home language.

| | |
|---|---|
| Route | `/admin`, `/admin/greetings`, `/admin/lesson-audio`, `/admin/lesson-links`, `/admin/children` |
| Web | `features/admin/` |
| API | `Features/Admin/` |
| Design card (Monika) | none yet |
| Status | [`status.md`](status.md) |

## Purpose and states

Linked from nowhere and open to anyone with the address, on every server and locally: there is no key (D-112, which replaced the key of D-83 and D-102). Responses are never cached and never indexed.

## Requirements

No FR of its own. Rules in D-83 and D-112; built behaviour in T-66, T-67, T-68 (`archive/tasks-until-2026-11-15.md`), T-94 (lesson links, D-106), T-118 (children and their WhatsApp numbers, D-110), T-122 (home language, D-113) and T-130 (links to the demo children's family phones).

## Decisions cited

D-83, D-102 (superseded), D-106, D-110, D-112, D-113, D-114.

D-01 to D-99 are in [`../../decisions/D-01-to-D-99.md`](../../decisions/D-01-to-D-99.md); later ones are one file each in [`../../decisions/`](../../decisions/README.md).
