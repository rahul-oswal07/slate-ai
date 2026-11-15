# T-119 · Her note in any language, read in each family's home language

| | |
|---|---|
| Status | Done |
| Issue | #242 · PR #243 |
| Owner | R |
| Screens | P5-todays-message, T3-approve, ai-pipeline |
| Source | Rahul, 15 Nov: the note's `lang` (spotted while explaining the notes box), then "the note typed should be translated in their home language"; [D-111](../decisions/D-111-note-in-any-language.md); size S |

## What

1. **Marked in the family's language.** On the family page the teacher's note was marked `lang="kn"` for every family, though the API sends it in the family's language; a screen reader read a Telugu, Tamil, Hindi or English note as Kannada. It is now marked in the family's language.
2. **Any language, translated for every family (D-111).** Her note was kept word for word as "her Kannada"; a note typed in English passed (the script check ran over the whole text) and went to Kannada families in English. Now she may write it in any language: what she typed is kept (`TodaysNote.Original`), and A9 (`a9-todays-message` v3) writes it in every family's language, Kannada included unless she wrote Kannada. Each language's note is checked on its own for the language's script. Approve shows what she typed and, when translated, what Kannada families read; the label says "any language".

## Acceptance criteria

- The note's element has the family's language as its `lang`.
- A note typed in English is saved as typed; Kannada families read it in Kannada, every other family in their language; her Kannada concept and activity stay word for word.
- A note in Kannada still goes to Kannada families word for word, with no AI call when every family is Kannada.
- A translated note left in the wrong script is refused (one retry, then "could not make it").
- The concept must still be Kannada.

## How it's tested

- Vitest: `TodaysPages.test.tsx` (Telugu family, `lang="te"`; failed first with `lang="kn"`); `TodaysMessageTab.test.tsx` (English note: what she typed, "Kannada families read", Edit starts from what she typed; Kannada note shown once).
- API: `TodaysMessageGeneratorTests` (English note → A9 asked for Kannada too, her concept kept; English note with only Kannada families; untranslated note refused); `TodaysMessageEndpointTests` (a note in English is accepted as typed; a concept in English is refused).
- Eval: `A9TodaysMessageEvals` with two new English-note cases (`edit-en-note-kn-te`, `edit-en-note-kn`); result in `evals/results/`.

## Notes

- 15 Nov: the earlier version of the family-page test expected the bug (a Kannada note for an English family, which the API never sends).
- 15 Nov: built both; prompt v2 → v3.
