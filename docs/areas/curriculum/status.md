# Curriculum · status

What works today and what doesn't. No dated notes here: those go in the task's file ([`docs/tasks/`](../../tasks/README.md)).

Spec: [`spec.md`](spec.md)

## Built

- Only Grade 3 maths is loaded: the whole book, 60 competencies (T-81, on prod since `v1.2`).
- The Nalikali demo lesson `M3.BRIDGE.COMPARE` (more, less, equal; Nalikali Part 1 p 4) is in chapter 0 with the book's exercises (T-87).
- Exercises can also be `add` and `sub` pairs, checked at startup: a sum ≤ 999, a difference not below 0 (T-128).
- M3.ADD.NOCARRY, M3.ADD.CARRY, M3.ADD.WORD, M3.SUB.NOBORROW, M3.SUB.BORROW and M3.SUB.WORD have `add`/`sub` exercises, and M3.NUM.COMPARE has `extremes`, all copied from their `examples` (T-128).
- `GET /api/curriculum` says which lessons have exercises (`hasExercises`) (T-128).

## Known gaps and drafts

- Only 13 have a practice rule, so the other 47 get no catch-up yet.
- Only the demo lesson has `compare` exercises. Add and subtract lessons start a today's message only with `TodaysMessage:ArithmeticExercises` on, which is the case in Development but not yet in staging or prod (D-116, T-128).
- The add/sub lessons have only the 2–3 pairs in their examples; the book pages' full lists are not extracted yet.
- No DIKSHA link per lesson yet (T-94).

## Open tasks

Task files that name this area:

```bash
grep -l "^| Screens | .*curriculum" docs/tasks/[TM]-*.md
```
