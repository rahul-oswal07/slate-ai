# Demo shell · status

What works today and what doesn't. No dated notes here: those go in the task's file ([`docs/tasks/`](../../tasks/README.md)).

Spec: [`spec.md`](spec.md)

## Built

- `/demo` has two phones, Reset, the simulated SATS feed, **Send messages now**, and Under the hood with a "For" column (T-18, T-73).
- It has one story on the real date, told through one child: Manjunath (Kannada), away the two days before today and today (T-93, D-94).
- The video story and the fixed 31 Oct date were removed on 14 Nov (T-105).
- The demo class has a register every day (T-69).

## Known gaps and drafts

- T-93 is not finished: the sample voice note, `Demo__SendOnApprove` on staging and the timed run ([task](../../tasks/T-93-demo-loop-and-reset.md)).
- The 6 demo steps have not been run on prod (`v1.3`).

## Open tasks

Task files that name this screen:

```bash
grep -l "^| Screens | .*demo" docs/tasks/[TM]-*.md
```
