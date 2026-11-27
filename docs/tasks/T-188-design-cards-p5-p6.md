# T-188 · Design cards for P5 today's message and P6 weekly star

| | |
|---|---|
| Status | Proposed |
| Issue | #366 |
| Owner | M |
| Screens | P5-todays-message, P6-weekly-star |
| Source | Rahul, 27 Nov 2026 (parent screens list); P5 and P6 status: no design card; Monika's files; M |

## What

Today's message (P5) and the weekly star (P6) were built without a design card. Write both cards in `docs/design/` from what's built: states, copy in en, the always/never rules, as the other cards do. Note where the built screen should change.

## Acceptance criteria

- One card each in `docs/design/`, linked from each screen's `spec.md`.
- Every state the screens have today is on the card.
- Differences from what's built are listed as tasks.

## How it's tested

- `./scripts/check-docs.sh` passes (links).

## Notes

- 27 Nov 2026: proposed.
