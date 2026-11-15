# D-110 · WhatsApp goes to the number saved on the admin page

| | |
|---|---|
| Status | Accepted |
| Owner | Rahul, 15 Nov 2026 |
| Gate | none |
| Screens | admin, delivery, demo |

## Decision

WhatsApp sent only to numbers in `WhatsApp__Recipients__{childId}` App Service settings; the `ParentPhone` on each child is a fictional seed value, never dialled. Rahul asked for an admin page to see and edit each child's number (T-118), so what should an edited number do?

## Recommendation

- A number saved on `/admin/children` (`Children.ParentPhone` with `ParentPhoneSetAt` set) is where that family's WhatsApp goes, ahead of the server setting. Without a saved number, the server setting is used; with neither, WhatsApp skips the family. Seeded numbers (no `ParentPhoneSetAt`) are never dialled.
- Consent, "stop these messages" and the approval rules are unchanged: a number never makes a message go by itself.
- Numbers are stored as E.164; a 10-digit Indian mobile gets `+91`.
- Saved numbers survive the demo Reset, so the test phones are set once.
- On Meta's test number WhatsApp still delivers only to registered test phones.

## Notes

- 15 Nov 2026: accepted by Rahul (WhatsApp sends to it; keep across Reset); built in T-118.
