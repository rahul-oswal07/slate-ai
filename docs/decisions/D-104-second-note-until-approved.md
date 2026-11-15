# D-104 · A new voice note can replace the confirmed lesson until today's message is approved

| | |
|---|---|
| Status | Accepted |
| Owner | Monika (flow), Rahul (rule), 15 Nov 2026 |
| Gate | none |
| Screens | T1-today, T2-lesson-confirm |

## Decision

[D-55](D-01-to-D-99.md) said no second voice note on the same day: once she confirmed, a new note got 409 and she could only use Change. From Monika's testing (T-114) this changes:

- **Before she confirms**, Re-record on Lesson confirm takes her back to Today's mic. The new note replaces the first note and its match (the API already allowed this).
- **After she confirms**, a new note still replaces the lesson **while today's message isn't approved**, or, on a day without today's message, **until tonight's message has gone**. Today keeps the record card for that long, with the confirmed lesson as one line and Change.
- **After that**, a note gets 409 as before and she uses Change.
- A note that replaces a confirmed lesson makes the day **unconfirmed until she confirms the new match** (Rahul, 15 Nov). In the meantime the evening send waits and Missed lessons doesn't count the day. If the new lesson differs, today's message is drafted again; if it gets no today's message, the unsent one is removed so a message about the old lesson can't go (guardrail 3).

## Notes

- 15 Nov 2026: built in T-114. The rule is `LessonWindow.CanReplaceConfirmed` (API) and `recordOpen` on Today (web). The frozen D-55 row is marked Changed; this file is the current rule.
