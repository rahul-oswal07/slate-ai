# Delivery · status

What works today and what doesn't. No dated notes here: those go in the task's file ([`docs/tasks/`](../../tasks/README.md)).

Spec: [`spec.md`](spec.md)

## Built

- **WhatsApp (T-40, D-97, D-98): step 1 merged (#180), on staging with Meta's test number.** Token, phone number ID and one recipient (Rahul's phone, as Ravi) are set; the admin `hello_world` test send reached the phone from staging. Until our templates are approved, WhatsApp deliveries send `hello_world`, hardcoded in `WhatsAppChannel`. On 14 Nov Monika submitted `todays_lesson_link_v1`, `todays_lesson_chat_v1` and `catchup_link_v1` (en/kn/te/ta/hi, in review; the earlier `evening_absence_v1` / `catchup_ready_v1` were flagged as Marketing; wording in [`whatsapp-setup.md`](../../whatsapp-setup.md) §3).
- A ready lesson clip that she hasn't approved holds the class's evening message for 5 minutes, then the plain message goes (T-85).
- After 6 pm the job holds a class's evening messages while today's message is being written, and for 5 minutes while it is ready and not approved; then the plain message goes (T-111, D-100).

## Known gaps and drafts

- WhatsApp is simulated unless `Delivery:Channel=whatsapp` (T-40).
- `WhatsApp:Evening` points at `evening_absence_v2` / version 4, which doesn't exist at Meta (T-104).

## Open tasks

Task files that name this area:

```bash
grep -l "^| Screens | .*delivery" docs/tasks/[TM]-*.md
```
