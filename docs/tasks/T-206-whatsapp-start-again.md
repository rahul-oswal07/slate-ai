# T-206 · START in the chat to get messages again

| | |
|---|---|
| Status | Proposed |
| Issue | #384 |
| Owner | R |
| Screens | delivery |
| Source | Rahul, 27 Nov 2026 (delivery list); D-30 (stop and restart), FR-19; S |

## What

A family can reply STOP (or the word in their language) to stop messages, but getting them back needs the restart link (D-30). Accept **START**, or the word in each of our languages, alone, in any case. It clears the family's stop and replies with a confirmation in their language.

## Acceptance criteria

- Only a stop the family made is undone this way; consent No or No phone stays as the school set it.
- Each message handled once (per message ID).
- The words are in `ChatWording` beside STOP's, for en, kn, te, ta, hi.
- All new strings in en, kn, te, ta, hi.

## How it's tested

- Unit tests for the words; API tests with recorded webhook payloads.

## Notes

- 27 Nov 2026: proposed.
