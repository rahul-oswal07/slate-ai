# D-109 · Approve and send now: the teacher can send an approved message before 6 pm

| | |
|---|---|
| Status | Accepted |
| Owner | Rahul, 15 Nov 2026 |
| Gate | none |
| Screens | T3-approve, demo, delivery |

## Decision

Approved messages went only with the 6 pm job (FR-20 for the evening and today's message, FR-08 for catch-ups); the demo sent at once only with `Demo:SendOnApprove` ([D-94](D-01-to-D-99.md), T-93), which was off on staging. In testing, an approval that didn't send looked broken.

## Recommendation

- After the tick, Approve has two buttons on both Approve → Today's message and a catch-up's review: **Approve and send now** (primary) and **Approve, send after 6 pm** (secondary). The approve request carries `send`.
- Send now on today's message runs the class's evening send at once; the evening send's rules are unchanged (wording approved, lesson confirmed, attendance in, once per child per day). If something blocks it, the message stays approved and goes with the 6 pm send once fixed.
- Send now on a catch-up sends it at once; the separate `/send` endpoint keeps its 6 pm rule.
- Guardrail 3 is unchanged: only what she approved at its current version goes.
- `Demo:SendOnApprove` is removed; every server behaves the same.

## Notes

- 15 Nov 2026: accepted by Rahul (two buttons, both screens); built in T-117.
