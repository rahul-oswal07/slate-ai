# D-115 · Today's message's AI calls are about the families who get it

| | |
|---|---|
| Status | Accepted |
| Owner | Rahul, 15 Nov 2026 |
| Gate | T-127 |
| Screens | demo |

## Decision

D-87 gave Under the hood a **For** column. It is filled only for the weekly catch-up's calls (A2, A3, A4 and its voice); every other call is class-level ("–"). D-87 came before today's message (D-91), so that message's A9 and voice calls were left class-level. The demo story now runs through today's message, and the Reset restores a pre-made catch-up, so on staging the column was always "–".

## Recommendation

- `TodaysMessageGenerator` runs inside `AiCallScope.For(childIds)`. The `childIds` are the children whose families get tonight's message (`EveningOutcome.WillSend` or `Sent`), the same set that picks its languages.
- A9 writes every language in one call, so it is about all of them. Each voice call is one language, so it is about only the children whose home language that is; a language no family gets (Kannada, drafted for her) is class-level.
- This holds for a fresh draft, "Try again" and the redraft after her edit.
- `AiCall.ChildIds` stays IDs only. Nothing new reaches Claude or Sarvam (guardrail 1).
- A1, A8, speech-to-text and the shared lesson clip stay class-level.

Extends D-87. The rest of D-87 still holds.

## Notes

- 15 Nov 2026: asked for and accepted by Rahul. Built in T-127.
- 16 Nov 2026: Rahul saw every voice row name every child on the demo. Each voice call is now scoped to its language's children (T-127 follow-up).
