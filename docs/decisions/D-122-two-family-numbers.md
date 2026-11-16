# D-122 · Two optional WhatsApp numbers per family; every message goes to both

| | |
|---|---|
| Status | Accepted |
| Owner | Rahul, 16 Nov 2026 |
| Gate | T-140 |
| Screens | admin, delivery |

## Decision

Since D-114 a family had one number, saved on `/admin/children`, and WhatsApp went only there. Often both parents, or a parent and a grandparent, want the message. Rahul: "there will be 2 numbers for a child both optional to update, and the whatsapp message is sent to both the number e.g mom and dad or any other guardian."

## Recommendation

- **Each child has Number 1 and Number 2, both optional**, saved, changed and cleared separately on `/admin/children`. They follow the same rules as before: E.164, and only a number saved there is dialled (D-114). The seed has no Number 2.
- **No label is stored.** Rahul chose plain "Number 1" / "Number 2" over "Mother / Father / Guardian".
- **Every WhatsApp message goes to each saved number:** today's message, the catch-up and the weekly star.
- **One outbox row per number.** `ParentDeliveries.Recipient` (1 or 2) means each number is sent, retried and failed separately. A failure on Number 2 never re-sends to Number 1. A number not saved by send time is skipped (`no-recipient`), as before.
- **Approval is unchanged (guardrail 3):** both rows are queued in the same transaction as the approved message. Consent and "Stop these messages" are per family, so one Stop stops both numbers.
- The admin test send (`hello_world`) goes to each saved number and reports each outcome without the number.
- Both numbers survive the demo Reset.

Extends D-114 (only a number saved on the admin page is dialled) to two numbers.

## Notes

- 16 Nov 2026: asked for by Rahul, who chose no labels. Built in T-140, in the same PR as T-139 (#295).
