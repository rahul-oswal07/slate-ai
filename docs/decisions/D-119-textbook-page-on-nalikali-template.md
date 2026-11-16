# D-119 · Textbook lessons go on the existing WhatsApp templates, with the book named after the page

| | |
|---|---|
| Status | Accepted |
| Owner | Rahul, 16 Nov 2026 |
| Gate | T-132 |
| Screens | delivery |

## Decision

`todays_lesson_link_v1` ("…{{2}}, Nalikali Maths page {{3}}.") and `catchup_link_v1` ("…{{2}}, Nalikali Maths {{3}}.") name Nalikali in their fixed text. So `TemplateValues` filled them only for Nalikali pages, and anything else was skipped (`no-values`). Only `M3.BRIDGE.COMPARE` (Nalikali p 4) is on Nalikali. Today's message for add and subtract (T-128) and most catch-ups therefore never went over WhatsApp. Rahul doesn't want to change the templates now.

## Recommendation

- A page in the Maths textbook fills the same templates, with the book after it in the family's language:
  - today's message {{3}}: "80 (Maths textbook)"; from part 2 on, "5 (Maths textbook Part 2)";
  - catch-up {{3}}: "page 80 (Maths textbook)", "pages 77 and 80 (Maths textbook)". Across books, the Nalikali pages come first, unmarked, then each textbook part: "page 4 and page 80 (Maths textbook)".
- The family reads "…Nalikali Maths page 80 (Maths textbook)." This was chosen over the bare "80", which would point to the wrong page of the Nalikali book.
- Nalikali pages are filled exactly as before.
- Book names: en "Maths textbook", kn "ಗಣಿತ ಪಠ್ಯಪುಸ್ತಕ", te "గణిత పాఠ్యపుస్తకం", ta "கணிதப் பாடநூல்", hi "गणित पाठ्यपुस्तक". The part word comes from the evening wording (D-99). Built by code (guardrail 4).
- Any other book (the workbook), or a lesson without a name in the family's language, is still skipped.

Replaces the "Nalikali pages only" rule of T-104. A template without the book in its fixed text would read better: if Monika submits one later, this choice can be revisited.

## Notes

- 16 Nov 2026: Rahul confirmed the Nalikali send works on staging and asked for the other lessons on the existing templates. He chose "page + book" over "page only". Built in T-132.
