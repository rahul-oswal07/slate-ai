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
| `prerequisites` | IDs that must be learnt first. Each must exist (in any file); no self-reference; no cycles |
| `chapter`, `sequence` | Chapter number and teaching order (sequence orders the chapter list and breaks ties) |
| `nipunOutcome`, `examples` | Optional. 1–2 textbook example problems each, used as A1/A3 grounding |
| `textbookPage` | Where the child finds it in their own book (D-22). `book` is `textbook` or `workbook`; `part` optional; `page` > 0 |

A bad file stops the app at startup with every problem listed, and `CurriculumStartupTests` fails in CI. With no files, the app starts with an empty graph and logs a warning.
