# T-202 · WhatsApp cost per message and a monthly total

| | |
|---|---|
| Status | Proposed |
| Issue | #380 |
| Owner | R |
| Screens | delivery, admin |
| Source | Rahul, 27 Nov 2026 (delivery list); `whatsapp-delivery-plan.md` §7 (cost); next to T-157's AI cost; S–M |

## What

Each template message costs money (Meta's utility rate for India); replies within 24 hours are free. Record the price per message from config (by category and country) on each sent delivery, or from Meta's pricing field in the status event once T-200 is in. Show the day's and month's total on the delivery log, beside the AI cost (T-157).

## Acceptance criteria

- Prices from config, dated; never in code.
- Free replies inside the 24-hour window count as zero.
- Totals computed in code.

## How it's tested

- Pure tests for the cost per message and the totals.

## Notes

- 27 Nov 2026: proposed.
