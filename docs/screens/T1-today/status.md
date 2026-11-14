# T1 Today · status

What works today and what doesn't. No dated notes here: those go in the task's file ([`docs/tasks/`](../../tasks/README.md)).

Spec: [`spec.md`](spec.md)

## Built

- **Attendance:** a simulated SATS register arrives at 12:00 (retry 2 pm). The teacher's marking wins over a late feed, and the register works offline through the Dexie outbox (T-12, D-69).
- There is a day strip (T-49).
- **Lesson:** recorded as a voice note, with an optional photo, or picked from the list; confirmed on [T2 Lesson confirm](../T2-lesson-confirm/status.md). "Add a photo of the page" and "Pick from the list" sit side by side as outlined tiles (part of T-96).
- **Today's message card** above Tonight: "waiting for you" with the one button **Check and approve**, or "approved · goes after 6 pm" (T-108).
- The tonight card has optional lesson audio, which the teacher approves clip by clip (T-62).
- After the send, Today shows "sent to N · M answered" and a child · answer · level table (T-92).

## Known gaps and drafts

- SATS is simulated.
- The design card is not updated for T-108, or for T-109 (the gear left the title row; Settings is in the header's profile menu) (Monika's file).

## Open tasks

Task files that name this screen:

```bash
grep -l "^| Screens | .*T1-today" docs/tasks/[TM]-*.md
```
