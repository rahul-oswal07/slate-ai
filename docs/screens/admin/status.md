# Admin pages · status

What works today and what doesn't. No dated notes here: those go in the task's file ([`docs/tasks/`](../../tasks/README.md)).

Spec: [`spec.md`](spec.md)

## Built

- Admin pages for greetings and lesson audio (T-66–T-68).
- **`/admin`** lists every admin page with its address and what it is for, and every page's nav links back to it (T-94). It asks the API nothing.
- **Lesson links** (`/admin/lesson-links`): every chapter and lesson with one video link each (any https link), saved per lesson for every class (T-94, D-106).
- The lesson links page starts with every chapter folded and "N of M linked" on each; a search (English or Kannada name, or ID) and "Only lessons without a link" narrow it; links typed in any rows are saved together from a bar at the bottom, a refused link stays as typed with how to fix it, and leaving with unsaved links asks first (T-94).
- The kn, te, ta and hi strings of the lesson links page are drafts for Monika (T-94).
- **Children** (`/admin/children`): every class's children by roll number with the number their family's WhatsApp goes to and where it comes from (saved here, the server setting, or none), and a note when consent or "stop" means nothing is sent; Edit / Save / Cancel and Clear per child (T-118, D-110).
- A number saved there is stored as E.164 (a 10-digit Indian mobile gets +91), is dialled ahead of the server setting, and survives the demo Reset; seeded numbers are never shown (T-118).
- The kn, te, ta and hi strings of the children page are drafts for Monika (T-118).
- Every admin page and the WhatsApp test send open without a key, on staging, prod and locally: anyone with the address can read and change what they show (T-121, D-112).

## Known gaps and drafts

- None beyond the draft strings above.

## Open tasks

Task files that name this screen:

```bash
grep -l "^| Screens | .*admin" docs/tasks/[TM]-*.md
```
