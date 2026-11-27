# T-161 · Correct a published curriculum as a new version

| | |
|---|---|
| Status | Proposed |
| Issue | #339 |
| Owner | R |
| Screens | admin, curriculum |
| Source | Rahul, 27 Nov 2026 (admin pages list); `docs/status.md` Next 7; D-125, T-147; M |

## What

A published curriculum is read only (T-147), so fixing one typo means importing the whole book again. Add **New version** on a published curriculum: it copies it to a draft (lessons, descriptions, links), marked "from version N". The admin edits and publishes it as usual, then moves classes to it with the same rule as today (a move that would lose lessons the class has taught is refused with their IDs).

## Acceptance criteria

- The published one never changes; classes on it stay on it until moved.
- Every lesson in the copy starts as Checked; an edited one goes back to To check.
- Lesson IDs are kept, so what a class has taught still matches.
- Update D-125 or write a decision if versioning changes its rules.
- All new strings in en, kn, te, ta, hi.

## How it's tested

- API tests for copy, publish, move; Vitest for the button and the "from version N" line.

## Notes

- 27 Nov 2026: proposed.
