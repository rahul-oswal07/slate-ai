# T-164 · "Since yesterday" on Today

| | |
|---|---|
| Status | Proposed |
| Issue | #342 |
| Owner | R |
| Screens | T1-today |
| Source | Rahul, 27 Nov 2026 (teacher screens list); D-53 (accepted, dropped from the MVP 25 Oct); new UX on Monika's card; M |

## What

What families did overnight is spread over Today's answer table, Missed and Week. Add a short "Since yesterday" card at the top of Today. It covers the time since she last opened Today, and always at least since the start of the previous school day (D-53). It shows:

- picture answers that came in, with their levels;
- reasons for absence the families tapped;
- catch-ups opened and answered;
- families who stopped messages.

Each line links to where it's shown in full. Counts are worked out in code.

## Acceptance criteria

- The window follows D-53; "last opened" is kept per teacher and class.
- Nothing shows when nothing happened (no empty card).
- No family number is shown; child names come from the register, never from AI.
- Works with the class dropdown (T-115): each class has its own window.
- All new strings in en, kn, te, ta, hi; tell Monika (her card).

## How it's tested

- Pure tests for the window and the counts; API test with fakes; Vitest for the card.

## Notes

- 27 Nov 2026: proposed.
