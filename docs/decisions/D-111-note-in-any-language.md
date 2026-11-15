# D-111 · Her note in any language, read by every family in their home language

| | |
|---|---|
| Status | Accepted |
| Owner | Rahul, 15 Nov 2026 |
| Gate | none |
| Screens | T3-approve, P5-todays-message, ai-pipeline |

## Decision

Her note on today's message was kept word for word as "her Kannada" ([D-92](D-01-to-D-99.md)), and only the other languages were translated from it. On staging a teacher typed her note in English; it passed the script check (which ran over the concept, the activity and the note together) and went to a Kannada family in English. Should she write it only in Kannada, or in any language?

## Recommendation

- She may write her note in **any language**. What she typed is kept (`TodaysNote.Original`) and is what an edit starts from.
- **Every family reads it in their home language**, Kannada families included: A9 (`a9-todays-message` v3) writes it in each family language. A note she wrote in Kannada still goes to Kannada families word for word.
- Her concept and activity stay Kannada, word for word (D-92 unchanged for them); her note gets every check but the script (no names, the book's numbers only, at most 2 sentences and 40 words).
- Each language's note is now checked on its own for that language's script, so a note left untranslated is refused and retried once.
- Approve → Today's message shows what she typed and, when it was translated, what Kannada families read.

## Notes

- 15 Nov 2026: accepted by Rahul; built in T-119 with the prompt change (v2 → v3) and an eval run.
