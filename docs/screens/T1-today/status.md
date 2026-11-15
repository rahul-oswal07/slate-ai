# T1 Today · status

What works today and what doesn't. No dated notes here: those go in the task's file ([`docs/tasks/`](../../tasks/README.md)).

Spec: [`spec.md`](spec.md)

## Built

- **Attendance:** a simulated SATS register arrives at 9:00 (retry 2 pm). The teacher's marking wins over a late feed, and the register works offline through the Dexie outbox (T-12, D-69).
- There is a day strip (T-49).
- **Lesson:** recorded as a voice note, with an optional photo, or picked from the list; confirmed on [T2 Lesson confirm](../T2-lesson-confirm/status.md). "Add a photo of the page" and "Pick from the list" sit side by side as outlined tiles (part of T-96).
- **Today's message card** above Tonight: "waiting for you" with the one button **Check and approve**, or "approved · goes after 6 pm" (T-108).
- Once the lesson is confirmed, the record card stays until today's message is approved, or, on a day without one, until tonight's message has gone: the lesson shows as one line ("Today: … · p …") with Change above the mic, and a new note replaces the lesson (T-114, D-104). After that, only the confirmed lesson shows.
- The confirmed lesson shows a **Book exercises** tag under its page when the lesson has the book's exercises (T-128).
- After Re-record on Lesson confirm, Today shows the mic, idle, with focus on it (T-114).
- There is no lesson audio tick: today's message's concept clip is the lesson audio (T-114, D-105).
- After the send, Today shows "sent to N · M answered" and a child · answer · level table (T-92). The level comes from the latest of up to two submits: Got it, Needs help or Not answered yet; Today checks again until every answer is locked (D-108).

## Known gaps and drafts

- SATS is simulated.
- The kn, te, ta and hi words of the **Book exercises** tag are drafts for Monika (T-128).
- The record card's one-line lesson strings in kn, te, ta and hi are drafts for Monika (T-114).
- The design card is not updated for T-108, T-109 or T-114, or for T-109 (the gear left the title row; Settings is in the header's profile menu) (Monika's file).

## Open tasks

Task files that name this screen:

```bash
grep -l "^| Screens | .*T1-today" docs/tasks/[TM]-*.md
```
