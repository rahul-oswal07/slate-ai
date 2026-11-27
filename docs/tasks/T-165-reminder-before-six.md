# T-165 · A reminder before 6 pm when today's message waits for her

| | |
|---|---|
| Status | Proposed |
| Issue | #343 |
| Owner | R |
| Screens | T1-today, T10-settings, app-shell |
| Source | Rahul, 27 Nov 2026 (teacher screens list); T-111 (6 pm holds), D-70 (mic-blocked and notification states); new UX on Monika's cards; M–L |

## What

If she doesn't approve today's message, the 6 pm job holds the class's evening messages and then sends the plain one (T-111). She has no way to know unless Today is open. Add a web-push reminder at about 5:30 pm IST, sent only when today's message is ready and not approved, or the lesson isn't confirmed on a day with absent children. Tapping it opens Approve.

She turns reminders on in Settings, and the browser asks permission there, never on first load. Settings shows when they are blocked in the browser and how to fix it. The mic-blocked state on Today comes with this, since it's the same permission pattern.

## Acceptance criteria

- One reminder per class per day at most; none on a day with nothing to do.
- The push carries no child name and no lesson text, only "Today's message is waiting for you".
- Off by default; turning it off stops it at once.
- Push keys come from configuration, not the repo.
- All new strings in en, kn, te, ta, hi; tell Monika.

## How it's tested

- Pure tests for "should remind" from the day's state; API tests with a fake push sender; Vitest for the Settings switch and blocked states.
- Touches more than 3 files: plan mode first.

## Notes

- 27 Nov 2026: proposed.
