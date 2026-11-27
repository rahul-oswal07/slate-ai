# T-209 · SMS with the link for families without WhatsApp

| | |
|---|---|
| Status | Proposed |
| Issue | #387 |
| Owner | R |
| Screens | delivery, admin |
| Source | Rahul, 27 Nov 2026 (delivery list); FR-16; needs a decision, a provider and the cost; M–L |

## What

Some families have a phone but no WhatsApp. Add SMS as a second channel: a short approved text with the family page link, sent when a family is marked "SMS" (or when WhatsApp fails for that number, if decided). In India this needs DLT registration (sender ID and pre-approved templates) through a provider.

## Acceptance criteria

- A decision covers which families get SMS, the provider, the cost, and whether a WhatsApp failure falls back to SMS.
- Only approved content, as for WhatsApp (guardrail 3); text slots filled by code; no name to the provider except in the final send.
- The outbox's one-send-per-number rules hold across channels.
- `docs/database.md` updated with any migration.

## How it's tested

- Unit tests with a fake SMS channel; one real send on staging.
- Touches more than 3 files: plan mode first.

## Notes

- 27 Nov 2026: proposed.
