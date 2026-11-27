# T-200 · Delivery statuses from Meta on each message

| | |
|---|---|
| Status | Proposed |
| Issue | #378 |
| Owner | R |
| Screens | delivery, admin |
| Source | Rahul, 27 Nov 2026 (delivery list); T-106 webhook ignores statuses; D-20 (never message opens); feeds T-154; M |

## What

Today "sent" means Meta accepted the request, not that the phone got it. Meta posts sent, delivered, read and failed updates to the webhook (`/api/whatsapp/webhook`), and they're ignored. Store **delivered** and **failed** (with Meta's error code) on the `ParentDeliveries` row, so the delivery log (T-154) can say "delivered 18:02" or why it failed.

**Read** isn't stored: D-20 says message opens are never a measure. Changing that needs a decision.

## Acceptance criteria

- Each status event is handled once (signature checked, idempotent per event ID, as the chat taps are).
- Out-of-order events never move a row backwards (failed after delivered stays delivered).
- Webhook works with `WhatsApp:ChatLesson:Enabled` off too (statuses arrive for every template).
- `docs/database.md` updated with the migration.

## How it's tested

- API tests with recorded webhook payloads: delivered, failed, duplicate, out of order, bad signature.

## Notes

- 27 Nov 2026: proposed.
