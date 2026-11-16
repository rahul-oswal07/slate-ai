# T-140 · Two optional WhatsApp numbers per family; every message goes to both

| | |
|---|---|
| Status | Done (#295, not merged yet) |
| Issue | #298 |
| Owner | R |
| Screens | admin, delivery |
| Source | Rahul, 16 Nov 2026; D-122 |

## What

Each child has an optional Number 1 and Number 2 on `/admin/children` (e.g. both parents, or a guardian). Every WhatsApp message goes to each saved number (D-122).

**Database (migration `SecondParentPhone`)**
- `Children.ParentPhone2` (varchar 20) and `ParentPhone2SetAt`: Number 2, with the same rules as Number 1.
- `ParentDeliveries.Recipient` (smallint, default 1): which number a row goes to.
- The message foreign keys are now one-to-many. Their unique indexes are `(EveningMessageId, Recipient)`, `(CatchUpAssignmentId, Recipient)` and `(WeeklyStarMessageId, Recipient)`, so a repeated send still queues nothing new.

**Server**
- `DeliveryStore.QueueAll` queues one row per number. The evening/today's message, catch-up and weekly star stores use it in the same transaction as before.
- `GetPendingAsync` gives each row its own number's saved phone, or none (`no-recipient`).
- The dispatcher logs the recipient (1 or 2), never the number.
- `PUT /api/admin/children/{id}/phone` takes `slot` (1 by default, 2; 400 otherwise), and `GET` returns `phone2`.
- `POST /api/admin/whatsapp/test` sends to each saved number and returns `numbers` with one result per number.
- The demo Reset keeps both numbers.

**Web**
- Each child row on `/admin/children` shows Number 1 and Number 2. Each has its own Edit / Save / Cancel / Clear, and an empty one says "Not set".
- "No number: WhatsApp skips this family" shows only when both are empty.
- New strings in en, kn, te, ta and hi. The non-English ones are drafts for Monika.

**Tests**
- API:
  - Number 2 is saved and cleared on its own; a bad slot gives 400.
  - Both numbers get the message, and a failure on one never re-sends to the other.
  - The test send goes to both.
  - Reset keeps both.
- Db:
  - Two rows per evening message and per catch-up.
  - Each pending row carries its own number; a seeded or cleared one gives none.
  - Number 2 survives Reset.
- Web: both slots: list, save, clear one then both, and edit pre-filled.

## Notes

- 16 Nov 2026: built and tested locally (verify.sh and the Db suite pass) in PR #295 with T-139. Rahul asked not to merge yet. Left: Monika's review of the kn/te/ta/hi strings.
