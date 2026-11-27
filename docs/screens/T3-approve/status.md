# T3 Check and approve · status

What works today and what doesn't. No dated notes here: those go in the task's file ([`docs/tasks/`](../../tasks/README.md)).

Spec: [`spec.md`](spec.md)

## Built

- **Check and approve:** two tabs.
- **Today's message** has sections she can edit or add a note to; only the video and her note can be removed, because the WhatsApp text promises the concept, activity and picture (D-108); her Kannada is kept word for word and the other languages are written from it, and the tick is bound to the version (T-89).
- Today's message's concept is about a minute (5–6 sentences, built on the lesson's description from the textbook), and her own Kannada edit may be as long: at most 6 sentences and 140 words (T-141, D-123).
- When she edits today's message, the concept box and each activity-step box have a placeholder saying what goes in it, in Kannada (T-96).
- **Her note** can be written in any language: Approve shows what she typed and, when it was translated, what Kannada families read; every family reads it in their home language, and Edit starts from what she typed (T-119, D-111).
- **Draft of today's message** (finale, D-90–D-96): confirming a lesson with the book's exercises (today only Nalikali p 4) drafts a message for absent families: concept with voice, a parent-led activity and a `>,<,=` picture pair chosen by code (T-88).
- Since T-139 (D-121) every confirmed lesson gets today's message. A lesson without the book's exercises has concept, activity and video but **no picture question**: Approve shows no picture section and no "Choose another pair", and only the concept and activity are promised.
- With `TodaysMessage:ArithmeticExercises` on (the default everywhere), add and subtract lessons with the book's pairs get today's message too. The picture section shows "248 + 438 = ?", the three choices and the answer by code, and "Choose another pair" lists the other sums (D-116, T-128).
- A fresh draft of today's message starts from what she said in class: A1's summary and her examples, filtered by code; her own Kannada edit wins over it (T-120).
- The Activity section has a player for the voiced steps, as families will hear them (T-120).
- After the tick, two buttons on both tabs: **Approve and send now** (today's message: the evening send runs at once, with its usual rules; catch-up: sent at once) and **Approve, send after 6 pm** (T-117, D-109).
- After a successful Approve on today's message she is back on Today (T-108).
- **Video:** when the lesson has a link on the admin page, today's message shows a Video section with that link, which she can remove (no Edit); code puts it in, never the AI (T-94, D-106).
- **Catch-ups** are the week's catch-ups, one per lesson group and language. Each has a segment player, a Kannada summary, **Why this**, the picture question, Simpler / Shorter, and a tick before Approve, bound to the version she read (T-14).
- A catch-up has today's message's sections, one open at a time, with the same section card and edit form: Concept (the script, its player and the Kannada summary), Activity, Video (only with an admin link), Picture question (read only) and My note (T-103, D-129).
- On a catch-up she can edit the script, the activity's two lines and her note while it is ready or needs review; only the video and her note can be removed (T-103).
- Every change to a catch-up is a new version that clears her tick and any approval not yet sent (T-103).
- Her words on a catch-up are checked by code, never an AI: names, braces, the family's script, sums, and only the catch-up's own numbers; a refusal saves nothing and says why (T-103).
- After she edits a catch-up's script its voice is made again from her words (no AI), the old audio and Kannada summary go, and she checks it again (T-103).
- On a catch-up the script and activity are edited in the family's language, and her note goes as she typed it, not translated (D-129).
- Simpler, Shorter and Try again replace her script and activity words but keep her note and what she removed (T-103).
- A child whose missed lessons have no practice rule gets no catch-up instead of a failed one (part of T-100, #212).
- The Catch-ups tab lists cards by action needed: ready and needs review, then failed, being written, approved and sent (T-100).
- A failed card has **Try again** under it, which writes a new version, as on the catch-up itself (T-100).
- A catch-up cut off while being written (a restart or deploy) shows as failed after 5 minutes, in the list and on its own screen, and Try again takes it (T-136).
- Counts take the singular: "In Hindi · 1 child", "Sent at … · 1 family", "1 family gets them after 6 pm" (T-100).

## Known gaps and drafts

- Changing the lesson starts a new draft for the new lesson, which clears the old approval; with nobody due it removes the day's unsent today's message, so an approval for the old lesson can't go (T-114, T-139).
- Catch-ups already made before the fix (staging's included) stay failed until the next week.
- The design card is not updated for T-89 and T-108 (Monika's file).
- The design card and the screen spec don't describe the catch-up's sections yet (T-103, Monika's files).
- A catch-up's picture question has no "Choose another question" (D-129).

## Open tasks

Task files that name this screen:

```bash
grep -l "^| Screens | .*T3-approve" docs/tasks/[TM]-*.md
```
