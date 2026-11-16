# Delivery · status

What works today and what doesn't. No dated notes here: those go in the task's file ([`docs/tasks/`](../../tasks/README.md)).

Spec: [`spec.md`](spec.md)

## Built

- **WhatsApp (T-40, D-97, D-98): step 1 merged (#180), on staging with Meta's test number.** Token, phone number ID and one recipient (Rahul's phone, as Ravi) are set; the admin `hello_world` test send reached the phone from staging. Since 15 Nov the templates are approved in the "Test Messaging Account", which owns staging's new test number, and deliveries send them instead of `hello_world` (T-104). On 14 Nov Monika submitted `todays_lesson_link_v1`, `todays_lesson_chat_v1` and `catchup_link_v1` (en/kn/te/ta/hi, in review; the earlier `evening_absence_v1` / `catchup_ready_v1` were flagged as Marketing; wording in [`whatsapp-setup.md`](../../whatsapp-setup.md) §3).
- **Where the number comes from (T-118, T-123, D-114):** only the number saved on `/admin/children`; a family without one is skipped (`no-recipient`). There is no server setting for numbers. Seeded numbers are never dialled; on Meta's test number only registered test phones get it.
- Today's message and the catch-up go as their approved templates (T-104 a, b): an evening message carrying an approved today's message sends `todays_lesson_link_v1` (child, lesson, page number); a catch-up sends `catchup_link_v1` (child, lesson names, pages with the word, e.g. "pages 4 and 6"); values are built by code from the curriculum in the family's language (`TemplateValues`).
- The templates' fixed text says "Nalikali Maths". A Maths textbook page fills them with the book after it, in the family's language: "…Nalikali Maths page 80 (Maths textbook).", with "Part 2" from part 2 on (T-132, D-119). A lesson in any other book, or without a name in the family's language, isn't sent (`no-values`).
- A ready lesson clip that she hasn't approved holds the class's evening message for 5 minutes, then the plain message goes (T-85).
- The weekly star (T-125, D-118): after 6 pm on Saturday once the register is in (or on Sunday, for a week whose Saturday had no school), the job sends `weekly_star_v1` ({{1}} the child's name) to each family whose child was present every school day, with consent Yes, not stopped, and only if she approved the star wording. Once per child per week.
- After 6 pm the job holds a class's evening messages while today's message is being written, and for 5 minutes while it is ready and not approved; then the plain message goes (T-111, D-100).

## Known gaps and drafts

- WhatsApp is simulated unless `Delivery:Channel=whatsapp` (T-40).
- The plain evening message has no approved template, so it isn't sent over WhatsApp; those families keep the demo chat (D-107).
- A Tamil catch-up isn't sent over WhatsApp: `catchup_link_v1` has no ta version yet.
- The weekly star isn't sent over WhatsApp in any language yet: `weekly_star_v1` is in review (English submitted 15 Nov). Once Meta approves a language, add it with `WhatsApp__WeeklyStar__Languages__en=en` (T-125).
- The approved buttons' URL has a stray `{{1}}` before the token; the family page reads past it until Monika fixes the templates (T-104).

## Open tasks

Task files that name this area:

```bash
grep -l "^| Screens | .*delivery" docs/tasks/[TM]-*.md
```
