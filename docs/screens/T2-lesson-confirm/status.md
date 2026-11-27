# T2 Lesson confirm · status

What works today and what doesn't. No dated notes here: those go in the task's file ([`docs/tasks/`](../../tasks/README.md)).

Spec: [`spec.md`](spec.md)

## Built

- The lesson comes from a Kannada voice note (Sarvam STT → name scrubber → A1) or a photo of the textbook page (T-07, T-64), or is picked from the list.
- A lesson from the Nalikali book reads "Nalikali Maths · Part 1 · p 4" (T-87).
- A lesson with the book's exercises shows a **Book exercises** tag under its page, in the matched list and in the picker (T-128).
- Confirming a lesson that starts today's message opens Approve → Today's message by itself; offline, or with no answer in 4 s, she goes to Today (T-108).
- The "What you said" card says **What I heard: "…"** with A1's summary in her language, and **Matched in the book:** the pages of the lessons A1 matched (the two choices, for a low match), so she sees why the AI chose them (T-102).
- What I heard is A1's stored summary of her note; the transcript is never stored or shown (T-102).
- Confirming over A1's match, from the matched list or from the lesson list, saves what A1 matched and what she chose as an A1 eval case (T-102).

## Known gaps and drafts

- **Re-record** shows under "What you said" or "Your photo" (sure match, please choose, photo only) and next to Pick from the list when the note was unclear. It clears the note and photo on the phone and goes back to Today's mic; the new note replaces the first and its match (T-114).
- The kn, te, ta and hi "Re-record" strings are drafts for Monika (T-114).
- The design card is not updated for T-108, T-114 or T-102 (Monika's file).
- The kn, te, ta and hi strings of What I heard and Matched in the book are drafts for Monika (T-102).

## Open tasks

Task files that name this screen:

```bash
grep -l "^| Screens | .*T2-lesson-confirm" docs/tasks/[TM]-*.md
```
