# T-203 · A WhatsApp template that works for any textbook

| | |
|---|---|
| Status | Proposed |
| Issue | #381 |
| Owner | R / M |
| Screens | delivery, curriculum |
| Source | Rahul, 27 Nov 2026 (delivery list); D-119, D-125; delivery status (a lesson in any other book isn't sent, `no-values`); M |

## What

The approved templates' fixed text says "Nalikali Maths" (T-132, D-119). A class following a curriculum made from its own textbook (D-125) has lessons in other books, and those messages aren't sent at all (`no-values`). Submit book-neutral versions (`todays_lesson_link_v2`, `catchup_link_v2`, and the chat one) where the book's name is a slot, filled by code from the class's curriculum in the family's language. Switch to them per language as Meta approves them.

## Acceptance criteria

- Monika submits the v2 wording in en, kn, te, ta, hi (`whatsapp-setup.md` §3 updated).
- Code fills the book slot from the curriculum; a book without a name in the family's language falls back to the v1 rule.
- Template names and languages from config, so v1 keeps working until v2 is approved.
- No child name in a slot other than the final send's `{child}` (guardrail 1).

## How it's tested

- `WhatsAppTests` for the values of a stored curriculum's lesson in each language; one real send on staging.

## Notes

- 27 Nov 2026: proposed.
