# T-154 · Delivery log: every message to families, with Retry

| | |
|---|---|
| Status | Proposed |
| Issue | #332 |
| Owner | R |
| Screens | admin, delivery |
| Source | Rahul, 27 Nov 2026 (admin pages list); T-40, FR-16, T-140; needs T-148; M |

## What

When a family says "I got nothing", nobody can see why without the database. Add `/admin/deliveries`, built on `ParentDeliveries`:

- pick a day and a class;
- one row per message and number: kind (evening, today's message, catch-up, weekly star), child (first name, roll number), Number 1 or 2 (masked), channel, template, status (queued, sent, failed, skipped) and the reason (`no-recipient`, `no-values`, Meta's error), attempts, last try;
- counts per status at the top;
- **Retry** on a failed row (goes through `DeliveryDispatcher`; it doesn't make a new message, so guardrail 3 still holds);
- **WhatsApp test send** (today only `POST /api/admin/whatsapp/test`) as a small form on the page.

## Acceptance criteria

- Shows only what's in the outbox; nothing here can create or change a message to a family.
- Retry is idempotent: a replay sends nothing twice.
- Numbers are masked ("98•••••644").
- Write endpoints take `clientRequestId`.
- All new strings in en, kn, te, ta, hi.

## How it's tested

- API tests over `FakeApi` with a fake store: list, filters, Retry of a failed row only.
- Vitest for the page.

## Notes

- 27 Nov 2026: proposed.
