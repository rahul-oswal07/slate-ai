# Admin pages · status

What works today and what doesn't. No dated notes here: those go in the task's file ([`docs/tasks/`](../../tasks/README.md)).

Spec: [`spec.md`](spec.md)

## Built

- Admin pages for greetings and lesson audio (T-66–T-68).
- **`/admin`** lists every admin page with its address and what it is for, and every page's nav links back to it (T-94). It asks the API nothing.
- `/admin` and every admin page's nav also link to the demo's Under the hood on its own, `/demo/ai`, with what it is for (T-142).
- **Lesson links** (`/admin/lesson-links`): every chapter and lesson with one video link each (any https link), saved per lesson for every class (T-94, D-106).
- The lesson-links search has a placeholder: "Lesson name or ID, e.g. compare" (T-96).
- The lesson links page starts with every chapter folded and "N of M linked" on each; a search (English or Kannada name, or ID) and "Only lessons without a link" narrow it; links typed in any rows are saved together from a bar at the bottom, a refused link stays as typed with how to fix it, and leaving with unsaved links asks first (T-94).
- The kn, te, ta and hi strings of the lesson links page are drafts for Monika (T-94).
- Under each lesson, **About this lesson** shows its description and worked examples (what today's message's spoken concept is built on) with where they came from: From the textbook, Written for this app or Edited here. Edit / Save / Cancel per lesson; refused text stays with how to fix it; each chapter also says "N described" (T-141, D-123).
- Every server loads the descriptions from `data/lesson-content/grade3-maths.json` at startup (61 lessons, 60 from the textbook); an edit made here is never overwritten (T-141).
- The kn, te, ta and hi strings of About this lesson are drafts for Monika (T-141).
- `scripts/set-lesson-links.mjs` sets a DIKSHA link on every lesson that has one in `data/lesson-links/diksha-kn.json` (54 of 61). It is a dry run unless `--apply`, and keeps hand-set links unless `--overwrite` (T-138).
- **Children** (`/admin/children`): every class's children by roll number with the numbers saved for their family, the only ones WhatsApp uses (T-123, D-114), and a note when consent or "stop" means nothing is sent; Edit / Save / Cancel and Clear per number (T-118, D-110).
- Each family has an optional Number 1 and Number 2, saved and cleared separately; every WhatsApp message goes to each one saved. "No number" shows only when both are empty (T-140, D-122).
- A number saved there is stored as E.164 (a 10-digit Indian mobile gets +91), is the only kind WhatsApp dials, and survives the demo Reset (both numbers); seeded numbers are never shown (T-118, T-140).
- Each child's home language can be changed there too, from a picker of Kannada, English, Telugu, Tamil and Hindi that saves on change. A changed language shows "Changed here", is used for everything made or sent after it, and survives the demo Reset. What was already made keeps its language (T-122, D-113).
- In the demo class, each child on the children page links to their simulated family phone, `/demo/family/<id>`. Other classes have no link, and with the demo off neither does the demo class (T-130).
- The kn, te, ta and hi strings of the children page are drafts for Monika (T-118, T-122, T-130).
- **Teachers** (`/admin/teachers`): each teacher's class, voice consent and greeting length. Clear consent asks first, then clears `Teacher.ConsentAt` and deletes her greeting (D-44): at her next visit she sees the Welcome and records it again (T-133).
- The kn, te, ta and hi strings of the teachers page are drafts for Monika (T-133).
- The kn, te, ta and hi strings of the Under the hood entry are drafts for Monika (T-142).
- **Demo** (`/admin/demo`): one switch per server, off until switched on, that lets the demo teacher act as a past school day (T-134, D-120). It saves on change, shows when it last changed, and is disabled when the demo is off on this server. Its kn, te, ta and hi strings are drafts for Monika.
- **Textbook imports, API only** (`/api/admin/curriculum-imports`, T-146, D-125):
  - What it does: upload a textbook PDF (≤ 100 MB, ≤ 600 pages), follow its progress, Try again (carries on where it stopped), Discard.
  - Claude (A10) reads it in the background into a draft curriculum whose lessons all need review.
  - The page that uses this, `/admin/curriculum`, is T-147.
- Every admin page and the WhatsApp test send open without a key, on staging, prod and locally: anyone with the address can read and change what they show (T-121, D-112).

## Known gaps and drafts

- None beyond the draft strings above.

## Open tasks

Task files that name this screen:

```bash
grep -l "^| Screens | .*admin" docs/tasks/[TM]-*.md
```
