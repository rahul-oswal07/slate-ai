# T-171 · Term view on Week

| | |
|---|---|
| Status | Proposed |
| Issue | #349 |
| Owner | R |
| Screens | T6-week |
| Source | Rahul, 27 Nov 2026 (teacher screens list); D-130 (no totals per child over weeks); new UX on Monika's card; M |

## What

Week shows one week. Add a term view showing week by week for the class: children away, how many got their catch-up and how many need help, and the average return speed. It answers "is it getting better?". Class-level only: no per-child totals over weeks (D-130).

## Acceptance criteria

- All numbers come from the ledger, computed in code.
- No per-child rows or ranking.
- A week without school shows as such, not as zero.
- Readable at 360 px.
- All new strings in en, kn, te, ta, hi; tell Monika.

## How it's tested

- Pure tests for the week-by-week numbers; Vitest for the view.

## Notes

- 27 Nov 2026: proposed.
