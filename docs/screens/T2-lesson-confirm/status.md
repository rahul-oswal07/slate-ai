# T2 Lesson confirm · status

What works today and what doesn't. No dated notes here: those go in the task's file ([`docs/tasks/`](../../tasks/README.md)).

Spec: [`spec.md`](spec.md)

## Built

- The lesson comes from a Kannada voice note (Sarvam STT → name scrubber → A1) or a photo of the textbook page (T-07, T-64), or is picked from the list.
- A lesson from the Nalikali book reads "Nalikali Maths · Part 1 · p 4" (T-87).
- A lesson with the book's exercises shows a **Book exercises** tag under its page, in the matched list and in the picker (T-128).
- Confirming a lesson that starts today's message opens Approve → Today's message by itself; offline, or with no answer in 4 s, she goes to Today (T-108).

## Known gaps and drafts

- **Re-record** shows under "What you said" or "Your photo" (sure match, please choose, photo only) and next to Pick from the list when the note was unclear. It clears the note and photo on the phone and goes back to Today's mic; the new note replaces the first and its match (T-114).
- The kn, te, ta and hi "Re-record" strings are drafts for Monika (T-114).
- The kn, te, ta and hi words of the **Book exercises** tag are drafts for Monika (T-128, D-116).
- The design card is not updated for T-108 or T-114 (Monika's file).

## Open tasks

Task files that name this screen:

```bash
grep -l "^| Screens | .*T2-lesson-confirm" docs/tasks/[TM]-*.md
```
