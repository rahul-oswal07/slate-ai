# Admin pages · status

What works today and what doesn't. No dated notes here: those go in the task's file ([`docs/tasks/`](../../tasks/README.md)).

Spec: [`spec.md`](spec.md)

## Built

- Admin pages for greetings and lesson audio sit behind `Admin:Key` (T-66–T-68).
- Locally (Development) the pages open without a key; staging and prod still need `Admin:Key`, and the WhatsApp test send needs it everywhere (D-102, T-112).

## Known gaps and drafts

- Prod has no `Admin__Key`, so the pages are off there.

## Open tasks

Task files that name this screen:

```bash
grep -l "^| Screens | .*admin" docs/tasks/[TM]-*.md
```
