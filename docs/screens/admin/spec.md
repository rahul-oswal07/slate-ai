# Admin pages · spec

Pages to play teachers' greetings and a day's lesson audio, set each lesson's video link, set each family's WhatsApp number and each child's home language, clear a teacher's voice consent, and switch the demo's "go back to a past day" on and off.

| | |
|---|---|
| Route | `/admin`, `/admin/greetings`, `/admin/lesson-audio`, `/admin/lesson-links`, `/admin/children`, `/admin/teachers`, `/admin/demo`; `/admin` also links to the demo's `/demo/ai` (T-142) |
| Web | `features/admin/` |
| API | `Features/Admin/` |
| Design card (Monika) | none yet |
| Status | [`status.md`](status.md) |

## Purpose and states

Linked from nowhere and open to anyone with the address, on every server and locally: there is no key (D-112, which replaced the key of D-83 and D-102). Responses are never cached and never indexed.

## Requirements

No FR of its own. Rules in D-83 and D-112; built behaviour in T-66, T-67, T-68 (`archive/tasks-until-2026-11-15.md`), T-94 (lesson links, D-106), T-118 (children and their WhatsApp numbers, D-110), T-122 (home language, D-113), T-130 (links to the demo children's family phones), T-133 (clear a teacher's voice consent, D-44) T-134 (the demo's past-day switch, D-120) and T-142 (a link to the demo's Under the hood, `/demo/ai`).

## Decisions cited

D-44, D-83, D-102 (superseded), D-106, D-110, D-112, D-113, D-114, D-120.

D-01 to D-99 are in [`../../decisions/D-01-to-D-99.md`](../../decisions/D-01-to-D-99.md); later ones are one file each in [`../../decisions/`](../../decisions/README.md).
