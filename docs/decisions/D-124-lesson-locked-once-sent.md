# D-124 · The lesson can't change once tonight's message has gone; Reset today for the demo class

| | |
|---|---|
| Status | Accepted |
| Owner | Rahul, 17 Nov 2026 |
| Gate | T-143 |
| Screens | T1-today, T10-settings |

## Decision

Once tonight's message (with or without today's message) has gone to families, changing the day's lesson was accepted but changed nothing for families. The message is never sent twice (one per child per day, D-21), so Missed lessons and what families got disagreed. Voice notes were already refused then (D-104); picking from the list wasn't.

Options were: refuse the change; allow it with a warning; or send a second, corrected message. Rahul chose to refuse it. For trying a flow again in the demo, he asked for a Reset in Settings.

## Recommendation

- **Refuse another lesson once any evening message for the day has gone** (`PUT …/lessons/{date}` → 409 with the day). The same lesson is fine. Today says why and has no Change.
- **Before that, a change still redrafts today's message**, and clears an approval (D-92, unchanged).
- **Reset today** in Settings, **demo class only**. For the school day it clears the lesson, today's message, lesson audio, tonight's messages with their answers, reasons, buddy ticks and register changes, and puts the simulated SATS register back. Earlier days stay. The full demo Reset on `/demo` is unchanged.

## Notes

- 17 Nov 2026: accepted by Rahul ("fix both with option 1"; Reset today: only today, demo class only).
