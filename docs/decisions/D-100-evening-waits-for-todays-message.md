# D-100 · The evening job waits 5 minutes for today's message

| | |
|---|---|
| Status | Accepted |
| Owner | Rahul, 15 Nov |
| Gate | T-111 |
| Screens | delivery, T3-approve, P5-todays-message |

## Decision

After 6 pm, how long the evening job holds a class's evening messages for a today's message she hasn't approved yet. Without a hold, the plain message goes on the next minute's tick: on 14 Nov that was 16 s after she confirmed the lesson and 3 s before she tapped Approve, so the family got the plain text and Approve had nothing to attach to.

## Recommendation

The same rule as the lesson clip (T-85):

- Hold while today's message is **being written** (not stale: `CatchUpEndpoints.StaleGenerating`, 5 minutes).
- Hold while it is **ready and not approved**, for `EveningMessages:TodaysMessageWait` (**5 minutes**) from when it became ready, or from 6 pm (`EveningMessages:SendAt`) if it was ready earlier. Every edit is a new version, so the wait starts again.
- No hold for a message that is approved, failed, stuck or already sent, or when there is none.
- When the wait runs out the plain message (D-21) goes. Approved catch-ups go on the same tick as before.
- The teacher's own Send and the demo's "Send messages now" don't wait, as with the lesson clip (T-82).

Guardrail 3 is unchanged: the sender attaches only a message approved at its current version.

## Notes

- 15 Nov: accepted by Rahul. Considered and not chosen: 10 minutes (more time to edit, but every family of the class waits longer when she never approves); holding only while it is being written (she would still lose the race unless she approves within a minute).
