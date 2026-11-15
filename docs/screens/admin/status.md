# Admin pages · status

What works today and what doesn't. No dated notes here: those go in the task's file ([`docs/tasks/`](../../tasks/README.md)).

Spec: [`spec.md`](spec.md)

## Built

- Admin pages for greetings and lesson audio sit behind `Admin:Key` (T-66–T-68).
- **`/admin`** lists every admin page with its address and what it is for; its links keep `#key=…`, and every page's nav links back to it (T-94). It asks the API nothing, so it shows even without a key.
- **Lesson links** (`/admin/lesson-links`): every chapter and lesson with one video link each (any https link), saved per lesson for every class; the only admin page that writes. Same key rule (T-94, D-106).
- The kn, te, ta and hi strings of the lesson links page are drafts for Monika (T-94).
- Locally (Development) the pages open without a key; staging and prod still need `Admin:Key`, and the WhatsApp test send needs it everywhere (D-102, T-112).

## Known gaps and drafts

- Prod has no `Admin__Key`, so the pages are off there.

## Open tasks

Task files that name this screen:

```bash
grep -l "^| Screens | .*admin" docs/tasks/[TM]-*.md
```
