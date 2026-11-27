# T-166 · Approve the checked catch-ups together

| | |
|---|---|
| Status | Proposed |
| Issue | #344 |
| Owner | R |
| Screens | T3-approve |
| Source | Rahul, 27 Nov 2026 (teacher screens list); FR-08 (bulk approve only for packages she has played and ticked), D-70; S–M |

## What

With several catch-ups in a week she approves each one separately. Add **Approve N checked**: it approves together every catch-up she has played and ticked at its current version. A catch-up she hasn't checked isn't included, and the button says how many are left.

## Acceptance criteria

- The API approves only packages ticked at their current `ScriptVersion`; anything else in the request is refused, not skipped silently (guardrail 3).
- One request with `clientRequestId`; a replay approves nothing twice.
- Says which ones were approved and which weren't, and why.
- All new strings in en, kn, te, ta, hi.

## How it's tested

- API tests: mixed ticked and unticked, a version changed after the tick, replay.
- Vitest for the button and its count.

## Notes

- 27 Nov 2026: proposed.
