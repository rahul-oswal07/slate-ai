# T-163 · School calendar: holidays and breaks set ahead

| | |
|---|---|
| Status | Proposed |
| Issue | #341 |
| Owner | R |
| Screens | admin, T1-today, T6-week, P6-weekly-star |
| Source | Rahul, 27 Nov 2026 (admin pages list); `docs/database.md` (no register = not a school day); needs T-148; M–L |

## What

Today a day stops counting as a school day only when it has no register. So on a holiday Today still asks for the register, and nothing knows ahead about Dasara. Add `/admin/calendar`: holidays and breaks (one day or a range, with a name), for every class on the server.

On a calendar holiday:

- Today says it's a holiday and doesn't ask for the register;
- no evening message, today's message or catch-up is made for it;
- it isn't a school day for the ledger, the weekly star (`StarRules`), pacing and the Week screen.

## Acceptance criteria

- All of the above is decided in code, never by AI.
- A register already taken on a day later marked a holiday is decided (kept and the day counts, or refused); write it in a decision.
- Write endpoints take `clientRequestId` and are idempotent.
- `docs/database.md` updated with the migration.
- All new strings in en, kn, te, ta, hi.

## How it's tested

- Pure tests for the school-day rule with holidays (ledger, `StarRules`); API tests; Vitest for the page and Today's holiday state.
- Touches more than 3 files: plan mode first.

## Notes

- 27 Nov 2026: proposed.
