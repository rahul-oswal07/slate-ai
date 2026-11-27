# T-167 · Withdraw an approved catch-up before it goes

| | |
|---|---|
| Status | Proposed |
| Issue | #345 |
| Owner | R |
| Screens | T3-approve |
| Source | Rahul, 27 Nov 2026 (teacher screens list); FR-08, D-70; S–M |

## What

Once she approves a catch-up, the 6 pm job sends it and she can't take it back. Add **Withdraw** on an approved catch-up that hasn't gone: it goes back to waiting for approval, and nothing is queued for it.

After it has gone, Withdraw isn't offered. Decide whether it should also close the family's link; write that in a decision if it should.

## Acceptance criteria

- Withdraw is refused (409) once any `ParentDeliveries` row exists for it; a send racing a withdraw ends one way only (lock or unique key).
- After Withdraw the API refuses to send it until it is approved again.
- Write endpoint takes `clientRequestId` and is idempotent.
- All new strings in en, kn, te, ta, hi.

## How it's tested

- API tests: withdraw then send refused, withdraw after send refused, replay.
- Db suite for the race only if a fake can't prove it (ask first).
- Vitest for the button.

## Notes

- 27 Nov 2026: proposed.
