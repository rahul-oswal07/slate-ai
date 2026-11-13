# Curriculum dependency graph

Every `*.json` file in this folder is loaded at startup (`Curriculum/CurriculumLoader.cs`, T-05) and merged into one prerequisite graph. Expected first file: `grade3-maths.json`, built from Monika's competency list (T-22).
Source: Karnataka state Grade 2–3 maths textbook (Kannada medium) and NIPUN Bharat learning outcomes. Kannada wording approved by Monika (D-14).

```json
{
  "grade": 3,
  "subject": "maths",
  "version": "2026-10-20",
  "competencies": [
    {
      "id": "M3.ADD.CARRY",
      "nameKn": "…",
      "nameEn": "Addition with carrying",
      "prerequisites": ["M3.ADD.NOCARRY", "M3.PV.TENS"],
      "nipunOutcome": "…",
      "chapter": 3,
      "sequence": 12,
      "examples": ["27 + 15 = ?"],
      "textbookPage": { "book": "textbook", "part": 1, "page": 34 }
    }
  ]
}
```

| Field | Rule |
|---|---|
| `id` | Like `M3.ADD.CARRY`: `M` + grade 1–5, then dot-separated upper-case parts. Unique across all files |
| `nameKn`, `nameEn` | Required |
| `keyWordsKn` | Optional; 3+ textbook Kannada words for the lesson (docs/design/curriculum.md word table). A3 picks a catch-up's 3 key words from them (FR-07); drafts for Monika's review (T-09) |
| `practice` | Optional; how code makes the lesson's numbers: `{ kind, digits, carry?, borrow? }`, kind = count · expanded · placeValue · biggest · smallest · add (carry none/ones/any) · sub (borrow none/tens). Add/sub stay ≤ 999; borrowing never crosses a zero |
| `nameTe`, `nameTa` | Optional; not blank when given. Fill `{lesson}` in the evening message for Telugu / Tamil families (FR-20); a family whose language has no name gets no message that evening (logged, never half-filled) |
| `prerequisites` | IDs that must be learnt first. Each must exist (in any file); no self-reference; no cycles |
| `chapter`, `sequence` | Chapter number and teaching order (sequence orders the chapter list and breaks ties). `0` = the Part 1 bridge course (Grade 2 revision) |
| `nipunOutcome`, `examples` | Optional. 1–2 textbook example problems each, used as A1/A3 grounding |
| `textbookPage` | Where the child finds it in their own book (D-22). `book` is `textbook` or `workbook` (the D-89 book), or `nalikali` (Nalikali Grade 3 Maths 2026-27, D-90; screens name the book: "Nalikali Maths · Part 1 · p 4"); `part` optional; `page` > 0 |
| `exercises` | Optional (D-93): the book's own exercise numbers, never answers. `compare`: pairs for >, < or = (2 numbers each); `extremes`: sets to find the smallest and biggest in (2–6 different numbers). Numbers 0–999. Code computes the answers (`Curriculum/ExerciseAnswers.cs`) |

A bad file stops the app at startup with every problem listed, and `CurriculumStartupTests` fails in CI. With no files, the app starts with an empty graph and logs a warning.

## `grade3-maths.json`

The whole Grade 3 maths textbook, Kannada medium, Part 1 and Part 2 (T-81, D-89; `docs/design/curriculum.md`): 60 competencies in 12 chapters, numbers to 999, plus the Nalikali demo lesson `M3.BRIDGE.COMPARE` (T-87, D-90: Nalikali Part 1 p 4, chapter 0 = bridge course, with the book's exercises). Pages are printed page numbers. 13 lessons have a `practice` rule; the other 47 get no catch-up until their practice kind is built (the candidate set leaves them out). `nameTe` / `nameTa` / `nameHi` are drafts awaiting native review (T-26), and the † Kannada names are to be confirmed with a teacher (D-14). `nipunOutcome` is null (nothing reads it yet). Change names and pages in `curriculum.md` first, then here.
