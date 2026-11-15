# D-107 · The plain evening message gets a factual WhatsApp template; until then it isn't sent over WhatsApp

| | |
|---|---|
| Status | Accepted |
| Owner | Rahul, 15 Nov 2026 (answers T-104 c, `[CONFIRM: Rahul]` in D-98) |
| Gate | none |
| Screens | delivery |

## Decision

The plain evening message (D-21) goes on a day without an approved today's message. Its warm wording ("We missed {child} today…") was classed as Marketing by Meta, so it has no approved WhatsApp template (D-98).

- **Later:** Monika writes a **factual** wording in the shape Meta accepted ("Class update – {{1}}. Today's lesson: {{2}}, … page {{3}}…") and submits it as a Utility template in the account that owns staging's number. It becomes a new teacher-approved wording version, so WhatsApp only ever carries text she approved (guardrail 3).
- **Until it is approved:** the plain evening message is **not sent over WhatsApp**: `WhatsApp:Evening:Name` is empty, so the delivery is skipped (`no-template`) and the family keeps the demo chat. Today's message and the catch-up go as their approved templates; `hello_world` is gone from deliveries (T-104).

## Notes

- 15 Nov 2026: built in T-104 (`WhatsAppOptions.Evening.Name = ""`). When the template is approved: set `WhatsApp__Evening__Name` and the wording version it copies (`WhatsApp__Evening__WordingVersion`).
