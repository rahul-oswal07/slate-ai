# P5 Today's message at home · status

What works today and what doesn't. No dated notes here: those go in the task's file ([`docs/tasks/`](../../tasks/README.md)).

Spec: [`spec.md`](spec.md)

## Built

- With today's message the link opens step by step: her voice and text → "Is everything OK?" (the reason, once) → concept → activity → `>,<,=` in sticks, scored by code → Done (T-90, T-91).
- On the picture question a tap only chooses; Submit sends it. Two submits whatever they are, then locked; nothing says right or wrong, and the answer never reaches the phone; code scores the latest (T-90, D-108).
- For an add or subtract lesson (`TodaysMessage:ArithmeticExercises`, on by default; `false` turns it off), the picture question is the sum, e.g. "248 + 438 = ?", with three stick pictures to choose from. The numeral shows only on the one tapped, the same two-submit rule applies, and code scores the number (D-116, T-128).
- Every step has Back (but the first) and its title in one row, then the teacher line; a reopened link starts at the first step (T-116).
- A stop link is on every step; removed sections are skipped (T-91).
- Her note is shown in the family's home language, Kannada included, whatever language she wrote it in, and marked with that language so a screen reader reads it right (T-119, D-111).
- The activity step has a player for the steps, voiced after the AI disclosure; without a clip (too long to voice, or a draft made before) it is text only (T-120).
- When the lesson has a link on the admin page, the steps include "Watch how it's taught" with that video (T-94, D-106); the teacher can remove it.
- A lesson without the book's exercises (T-139, D-121) has no picture question: the steps go concept → activity → (video) → Done, and Done says "Thank you for doing it together" instead of that the teacher will see the answer. The WhatsApp template still says a picture question is ready (accepted for now).

## Known gaps and drafts

- No design card.

## Open tasks

Task files that name this screen:

```bash
grep -l "^| Screens | .*P5-todays-message" docs/tasks/[TM]-*.md
```
