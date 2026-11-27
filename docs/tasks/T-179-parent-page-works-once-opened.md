# T-179 · A family page works without data once opened

| | |
|---|---|
| Status | Proposed |
| Issue | #357 |
| Owner | R |
| Screens | P0-evening-message, P1-P4-catch-up, P5-todays-message, app-shell |
| Source | Rahul, 27 Nov 2026 (parent screens list); D-70 (offline beyond attendance and voice notes); M |

## What

Families often open the link when they get it and do it with the child later, when there may be no data. Once a family page has opened, keep it and its audio on the phone so it opens and plays offline until the link expires (14 days).

Taps made offline (reason, answer) wait on the phone and send when back online, with a "Waiting to send" line.

## Acceptance criteria

- After one open, the page and every clip on it work in airplane mode.
- Answers made offline are sent once (`clientRequestId`); the two-submit rule (D-108) still holds on the server.
- Nothing is kept after the link expires or the family stops messages.
- The kept data holds only what the page already shows.
- All new strings in en, kn, te, ta, hi.

## How it's tested

- Vitest for the outbox and the waiting state; API test for replayed answers.

## Notes

- 27 Nov 2026: proposed.
