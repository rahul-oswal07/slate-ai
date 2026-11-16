# T-132 · Textbook lessons go over WhatsApp on the existing templates

| | |
|---|---|
| Status | Done (#281) |
| Issue | #276 |
| Owner | R |
| Screens | delivery |
| Source | Rahul, 16 Nov 2026; D-119; S |

## What

On staging, today's message for `M3.ADD.CARRY` was skipped with `no-values` (13:14 and 13:33 IST). The approved templates say "Nalikali Maths", so code filled them only for the single Nalikali lesson. Now a Maths textbook page fills the same templates with the book after it, in the family's language: "80 (Maths textbook)". The templates don't change (D-119).

## Acceptance criteria

- Today's message for a textbook lesson (e.g. add with carrying, part 1 p 80) is sent over WhatsApp with {{3}} = "80 (Maths textbook)" in the family's language.
- A catch-up over textbook lessons is sent with {{3}} = "pages 77 and 80 (Maths textbook)". Part 2 says "Part 2".
- Nalikali lessons are filled exactly as before.
- Every lesson with the book's exercises fills both templates in all five languages.

## How it's tested

- `TemplateValuesTests`: textbook page in each language, part 2, catch-up within and across books, workbook still skipped, and the real curriculum filled for every lesson that has exercises. The old test "a lesson outside Nalikali gives nothing" is replaced, because the rule changed. All new tests were written first and failed on the old code.
- A real send on staging after merge: confirm an add or subtract lesson, approve, and send.

## Notes

- 16 Nov 2026: built. The cause was found in staging's logs (`no-values`) and the curriculum: 1 Nalikali lesson, everything else in the textbook. A Tamil catch-up is still skipped, because `catchup_link_v1` has no ta version.
