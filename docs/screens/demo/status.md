# Demo shell · status

What works today and what doesn't. No dated notes here: those go in the task's file ([`docs/tasks/`](../../tasks/README.md)).

Spec: [`spec.md`](spec.md)

## Built

- `/demo` has two phones, Reset, the simulated SATS feed, **Send messages now**, and Under the hood with a "For" column (T-18, T-73).
- The For column names the children a call was about: the weekly catch-up's calls and today's message (A9 and its voice). Every other call is class-level and shows "–" (D-87, D-115).
- After a Reset, Manjunath's weekly catch-up is pre-made, so its A2, A3 and A4 calls don't run and don't appear (T-127).
- It has one story on the real date, told through one child: Manjunath (Kannada), away the two days before today and today (T-93, D-94).
- The video story and the fixed 31 Oct date were removed on 14 Nov (T-105).
- The demo class has a register every day (T-69).
- `/demo/ai` shows Under the hood on its own, and `/demo/family/:childId` shows any demo child's family phone on its own, full screen on a phone (T-126).
- `/demo` links to both from its step guide (T-126).
- The family phone's browser bar reads "Tappida Paatha" on every page, as the real page's title does (T-116).

## Known gaps and drafts

- There is no sample voice note clip, so Today doesn't offer "Use the sample voice note (demo)".
- **Approve and send now** sends at once on every server, so the demo needs no setting for it (T-117, D-109).
- The 6 demo steps have not been run on prod (`v1.3`).

## Open tasks

Task files that name this screen:

```bash
grep -l "^| Screens | .*demo" docs/tasks/[TM]-*.md
```
