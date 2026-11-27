# T1 Today · status

What works today and what doesn't. No dated notes here: those go in the task's file ([`docs/tasks/`](../../tasks/README.md)).

Spec: [`spec.md`](spec.md)

## Built

- **Attendance:** a simulated SATS register arrives at 9:00 (retry 2 pm). The teacher's marking wins over a late feed, and the register works offline through the Dexie outbox (T-12, D-69).
- There is a day strip (T-49).
- **One next step:** under the day strip, one primary button follows the day: Record today's lesson → Confirm today's lesson → Check today's message → See answers. Every other button on Today is secondary. Nothing shows when there is nothing to do now (T-96).
- Once the lesson is confirmed, the mic is outlined rather than the amber hero, unless she came back with Re-record (T-96).
- **Lesson:** recorded as a voice note, with an optional photo, or picked from the list; confirmed on [T2 Lesson confirm](../T2-lesson-confirm/status.md). "Add a photo of the page" and "Pick from the list" sit side by side as outlined tiles (part of T-96).
- **Today's message card** above Tonight: "waiting for you" with the one button **Check and approve**, or "approved · goes after 6 pm" (T-108).
- Once the lesson is confirmed, the record card stays until today's message is approved, or, on a day without one, until tonight's message has gone: the lesson shows as one line ("Today: … · p …") with Change above the mic, and a new note replaces the lesson (T-114, D-104). After that, only the confirmed lesson shows.
- Once tonight's message has gone to families (any evening message for the day, with or without today's message), the lesson can't change: Today shows the confirmed lesson with "Tonight's message about this lesson has gone to families, so it can't be changed", without Change or the mic, and the API refuses another lesson (409). A change refused that way goes back to the confirmed lesson on screen (T-143, D-124).
- The confirmed lesson shows a **Book exercises** tag under its page when the lesson has the book's exercises (T-128).
- After Re-record on Lesson confirm, Today shows the mic, idle, with focus on it (T-114).
- There is no lesson audio tick: today's message's concept clip is the lesson audio (T-114, D-105).
- After the send, Today shows "sent to N · M answered" and a child · answer · level table (T-92). The level comes from the latest of up to two submits: Got it, Needs help or Not answered yet; Today checks again until every answer is locked (D-108).
- Each level is an icon and a word in its own colour token, the same tag as on Missed, the child view, Back today and Week (T-97).
- In the demo class, arrows beside Today's date step a day back or forward, tapping the date opens the calendar, and Back to today sits below it. Today then loads and writes that day (T-134, D-120).

## Known gaps and drafts

- SATS is simulated.
- The record card's one-line lesson strings in kn, te, ta and hi are drafts for Monika (T-114).
- The design card is not updated for T-108, T-109 or T-114, or for T-109 (the gear left the title row; Settings is in the header's profile menu) (Monika's file).
- The design card is not updated for T-96's next-step button or outlined mic (Monika's file).
- The next-step labels in kn, te, ta and hi are drafts for Monika (T-96).

## Open tasks

Task files that name this screen:

```bash
grep -l "^| Screens | .*T1-today" docs/tasks/[TM]-*.md
```
