# T3 Check and approve · status

What works today and what doesn't. No dated notes here: those go in the task's file ([`docs/tasks/`](../../tasks/README.md)).

Spec: [`spec.md`](spec.md)

## Built

- **Check and approve:** two tabs.
- **Today's message** has sections she can edit or add a note to; only the video and her note can be removed, because the WhatsApp text promises the concept, activity and picture (D-108); her Kannada is kept word for word and the other languages are written from it, and the tick is bound to the version (T-89).
- **Her note** can be written in any language: Approve shows what she typed and, when it was translated, what Kannada families read; every family reads it in their home language, and Edit starts from what she typed (T-119, D-111).
- **Draft of today's message** (finale, D-90–D-96): confirming a lesson with the book's exercises (today only Nalikali p 4) drafts a message for absent families: concept with voice, a parent-led activity and a `>,<,=` picture pair chosen by code (T-88).
- After the tick, two buttons on both tabs: **Approve and send now** (today's message: the evening send runs at once, with its usual rules; catch-up: sent at once) and **Approve, send after 6 pm** (T-117, D-109).
- After a successful Approve on today's message she is back on Today (T-108).
- **Video:** when the lesson has a link on the admin page, today's message shows a Video section with that link, which she can remove (no Edit); code puts it in, never the AI (T-94, D-106).
- **Catch-ups** are the week's catch-ups, one per lesson group and language. Each has a segment player, a Kannada summary, **Why this**, the picture question, Simpler / Shorter, and a tick before Approve, bound to the version she read (T-14).
- A child whose missed lessons have no practice rule gets no catch-up instead of a failed one (part of T-100, #212).

## Known gaps and drafts

- Changing the lesson to one without the book's exercises (or with nobody due) removes the day's unsent today's message, so an approval for the old lesson can't go (T-114).
- Catch-ups already made before the fix (staging's included) stay failed until the next week.
- The design card is not updated for T-89 and T-108 (Monika's file).

## Open tasks

Task files that name this screen:

```bash
grep -l "^| Screens | .*T3-approve" docs/tasks/[TM]-*.md
```
