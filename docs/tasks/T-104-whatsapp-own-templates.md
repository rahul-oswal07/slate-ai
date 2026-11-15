# T-104 · Replace the `hello_world` stand-in with our own WhatsApp templates

| | |
|---|---|
| Status | Open |
| Issue | #182 |
| Owner | R |
| Screens | delivery |
| Source | see What |

## What

**Replace the `hello_world` stand-in with our own WhatsApp templates (#182, Rahul, 13 Nov, after T-40; updated 14 Nov by Monika, D-98):** `WhatsAppChannel` is hardcoded to send Meta's `hello_world` for every delivery (its `helloWorld` parameter, default true). **Templates:** Monika submitted three on 14 Nov, Utility, en/kn/te/ta/hi, in review: `todays_lesson_link_v1`, `todays_lesson_chat_v1`, `catchup_link_v1` (wording, variables and buttons in [`whatsapp-setup.md`](../whatsapp-setup.md) §3). `evening_absence_v1` and `catchup_ready_v1` were **not** submitted: Meta's check classes their wording as Marketing (§3). **Build first: an approved template shows nothing until the code sends it with the right values:** (a) **today's message** (T-88, T-91) sends `todays_lesson_link_v1` with `{{1}}` child, `{{2}}` lesson name, `{{3}}` page number, in the family's language; (b) **the catch-up** sends `catchup_link_v1` with `{{1}}` child, `{{2}}` the package's target lesson names ("More-less-equal and Place value"), `{{3}}` the pages with the word ("page 4", "pages 4 and 6"), both built by code in the family's language (today the code sends child + teacher); (c) the **D-21 plain evening fallback** has no approved template **[CONFIRM: Rahul]**: no WhatsApp for it (demo chat only), or a factual wording as a new teacher-approved version; (d) **templates belong to the WhatsApp Business account they were made in:** Monika's are in her account, so check that staging's `PhoneNumberId` is in the same account, or create the same templates in yours. Then remove the `helloWorld` parameter. **Since T-101 (D-99)** the evening wording is v4 and its `{page}` value carries the book's name ("Nalikali Maths Part 1, page 4"); `WhatsApp:Evening` points at `evening_absence_v2` / version 4, which doesn't exist at Meta, so (c) must settle the plain evening message before `helloWorld` goes

## Acceptance criteria

On a registered phone today's message arrives as `todays_lesson_link_v1` with the child's name, lesson and page, and its button opens today's family page (with the reason step, T-91); a catch-up arrives as `catchup_link_v1` with its lessons and pages (1 and 2 lessons read correctly); nothing goes for a template or language that isn't approved; the `hello_world` stand-in is gone from the code

## How it's tested

`WhatsAppTests` (template name, params per language, 1 and 2 lessons, button token); manual check on a registered phone

## Notes

- 15 Nov: **(a) and (b) built** (templates still in review at Meta, so `hello_world` stays on and nothing changes on staging yet). An evening row carrying an approved today's message is now delivery kind `todays` and sends `todays_lesson_link_v1` with child, lesson names and the bare page number; the catch-up sends `catchup_link_v1` with child, target lesson names ("A and B", te/ta with a comma) and pages with the word ("page 4", "pages 4 and 6"); `Features/Delivery/TemplateValues.cs` builds both from the curriculum, pure, with tests. The templates' fixed text says "Nalikali Maths", but 60 of 61 lessons are on a textbook page, so values are built only for Nalikali pages and anything else is skipped (`no-values`); most real catch-ups won't go until more lessons are re-pointed to Nalikali (T-87) or a `_v2` names the book in a variable. Left: (c) the plain evening fallback **[CONFIRM: Rahul]**, (d) the WhatsApp Business account check, then remove `helloWorld`. New Db test `DeliveryStoreDbTests` runs `GetPendingAsync` on Postgres (Db suite 20/20). D-98 accepted (Rahul).
- 15 Nov: moved here from `tasks.md` (T-110).
- 14 Nov, from `status.md`: **T-104 (#182), build before approval matters:** today's message sends `todays_lesson_link_v1`, the catch-up sends lesson names and pages, decide the D-21 fallback, and check the templates are in the same WhatsApp Business account as staging's number.
- 14 Nov: until our templates are approved, WhatsApp deliveries send `hello_world`, hardcoded in `WhatsAppChannel`. Monika's three templates are in review at Meta.
