# T-181 · "We need help": a family can tell the teacher

| | |
|---|---|
| Status | Proposed |
| Issue | #359 |
| Owner | R |
| Screens | P1-P4-catch-up, P5-todays-message, P0-evening-message, T1-today |
| Source | Rahul, 27 Nov 2026 (parent screens list); D-63 (picture choices, no voice); works with T-164; new copy on Monika's cards; M |

## What

A family that couldn't do the lesson has no way to say so. Add **We need help** on Done and in the footer of every family page. It has fixed choices only:

- We couldn't do it;
- We didn't understand the audio;
- Please call me.

No free text and no voice, so nothing needs reading for safety and nothing goes to AI. The teacher sees it on Today (in "Since yesterday", T-164), with the child and the lesson.

## Acceptance criteria

- One tap per choice per link; a later tap replaces it; a replayed `clientRequestId` changes nothing.
- "Please call me" shows the teacher who, not the number; she calls as she would today.
- Refused on a stopped or expired link (409 / 410).
- Nothing here reaches the ledger's levels.
- All new strings in en, kn, te, ta, hi; tell Monika.

## How it's tested

- API tests for the rules above; Vitest for the choices and for how it shows on Today.
- `docs/database.md` updated with the migration.

## Notes

- 27 Nov 2026: proposed.
