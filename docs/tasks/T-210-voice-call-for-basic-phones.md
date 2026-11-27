# T-210 · An automated voice call for families with a basic phone

| | |
|---|---|
| Status | Proposed |
| Issue | #388 |
| Owner | R / M |
| Screens | delivery, admin |
| Source | Rahul, 27 Nov 2026 (delivery list); needs a decision, a provider and the cost; L |

## What

Families with a basic phone get nothing today, and they're often the ones who need it most. Call them at a set time in the evening and play the approved audio: her greeting, the lesson or the catch-up's script, with the AI disclosure first. Optionally take a keypad answer to the picture question ("Press 1 for more…"), scored by code.

## Acceptance criteria

- A decision covers the provider, the calling hours, retries for no answer, the cost and consent for calls.
- Only approved audio at its version (guardrail 3); the disclosure is always first.
- A keypad answer is scored by code and counts on the ledger like a tap, only if decided.
- The family can stop calls (keypad or the school), as with STOP (FR-19).

## How it's tested

- Unit tests with a fake call provider; one real call on staging.
- Touches more than 3 files: plan mode first.

## Notes

- 27 Nov 2026: proposed.
