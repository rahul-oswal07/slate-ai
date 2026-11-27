# T-177 · Family audio: resume after a drop, back 10 s, replay a step

| | |
|---|---|
| Status | Proposed |
| Issue | #355 |
| Owner | R |
| Screens | P0-evening-message, P1-P4-catch-up, P5-todays-message |
| Source | Rahul, 27 Nov 2026 (parent screens list); P1 spec (resume after a drop and back-10 s are roadmap); S–M |

## What

On a weak signal the audio stops, and the family starts it again from the beginning. Make the family pages' players:

- resume where they stopped when the signal comes back, or when Play is tapped again;
- have **Back 10 s**;
- show how far it has played, and replay the step from the start.

The same player is used for the greeting, the concept, the steps and the lesson clip.

## Acceptance criteria

- A dropped download resumes from where it stopped (range requests on the audio endpoint), not from 0.
- Back 10 s and the progress bar work with a keyboard and a screen reader, with labels in the page's language.
- Reduced motion has no animated progress.
- All new strings in en, kn, te, ta, hi.

## How it's tested

- Vitest for the player's states (playing, dropped, resumed); API test for range requests on the audio endpoint.

## Notes

- 27 Nov 2026: proposed.
