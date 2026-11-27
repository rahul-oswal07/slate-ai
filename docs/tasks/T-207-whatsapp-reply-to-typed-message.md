# T-207 · A reply when a family types in the chat

| | |
|---|---|
| Status | Proposed |
| Issue | #385 |
| Owner | R |
| Screens | delivery |
| Source | Rahul, 27 Nov 2026 (delivery list); works with T-181; new copy (Monika); S |

## What

A family who types a question in the chat gets no answer. Reply once with a fixed text in their language: this number doesn't read messages; tap **We need help** (T-181) on today's page, or talk to the teacher at school. The reply is inside the 24-hour window, so it's free.

## Acceptance criteria

- No AI reads or answers the family's text; the text isn't stored.
- At most one such reply per family per day.
- STOP and START (T-206) still work and get their own replies.
- All new strings in en, kn, te, ta, hi; tell Monika.

## How it's tested

- API tests with recorded webhook payloads: first text replied, second the same day not.

## Notes

- 27 Nov 2026: proposed.
