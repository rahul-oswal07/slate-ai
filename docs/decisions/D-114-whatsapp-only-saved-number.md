# D-114 · WhatsApp goes only to the number saved on /admin/children

| | |
|---|---|
| Status | Accepted |
| Owner | Rahul, 15 Nov 2026 |
| Gate | T-123 |
| Screens | admin, delivery |

## Decision

WhatsApp dialled the number saved on `/admin/children` first, and fell back to the `WhatsApp__Recipients__{childId}` App Service setting (D-110). Rahul has now saved the families' numbers on the page and wants WhatsApp to use only those numbers, never the setting.

## Recommendation

- WhatsApp sends only to a number saved on `/admin/children` (`Children.ParentPhone` with `ParentPhoneSetAt` set). A family without one is skipped (`no-recipient`) and stays on the demo chat.
- `WhatsApp:Recipients` and its settings are removed, from the code and from staging. Prod never had them.
- The admin test send (`POST /api/admin/whatsapp/test`) uses the child's saved number too.
- Seeded numbers are still never dialled. Consent, "stop" and the approval rules (guardrail 3) are unchanged.
- The admin page shows only the saved number, without a "where it comes from" note.

Supersedes D-110's fallback to the setting. The rest of D-110 still holds: the saved number comes first, numbers are stored as E.164, and they survive the demo Reset.

## Notes

- 15 Nov 2026: accepted by Rahul. Before the change, staging's two setting-only numbers (Ravi, Manjunath) were saved on the page, so those families keep getting WhatsApp. Built in T-123.
