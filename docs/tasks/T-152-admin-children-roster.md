# T-152 · Children roster: add, edit, move, remove and import a class list

| | |
|---|---|
| Status | Proposed |
| Issue | #330 |
| Owner | R |
| Screens | admin |
| Source | Rahul, 27 Nov 2026 (admin pages list); FR-15 child deletion (T-19); needs T-148; M–L |

## What

The class list comes only from the seed; `/admin/children` can change a family's numbers and the home language but not the child. Add to it:

- **Add child**: name, roll number, home language, numbers;
- **Edit**: name and roll number;
- **Move** to another class (from the next school day; past registers stay with the old class);
- **Remove**: deletes the child and everything about them, as `docs/database.md` "Delete behaviour" says (FR-15), after asking first;
- **Import a class list** from a CSV (roll number, name, home language, Number 1, Number 2): shows a preview with each row's problems before anything is saved.

## Acceptance criteria

- A roll number is unique in a class (`IX_Children_ClassId_RollNo`); a clash is refused with the row.
- Remove leaves no row for that child in any table (the Db test FR-15 / T-19 that CLAUDE.md asks for).
- A CSV with a bad row saves nothing until fixed or that row is skipped; a replayed import adds nobody twice.
- Children's names never go to Claude or Sarvam (guardrail 1); nothing here calls AI.
- Write endpoints take `clientRequestId` and are idempotent.
- All new strings in en, kn, te, ta, hi.

## How it's tested

- Pure tests for the CSV parser and its row rules; API tests over `FakeApi`; Vitest for the page and the preview.
- Db suite: child deletion only (on the CLAUDE.md list).

## Notes

- 27 Nov 2026: proposed.
