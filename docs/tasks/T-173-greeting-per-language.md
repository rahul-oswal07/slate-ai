# T-173 · A greeting in each of her families' languages

| | |
|---|---|
| Status | Proposed |
| Issue | #351 |
| Owner | R |
| Screens | T0-setup, T10-settings |
| Source | Rahul, 27 Nov 2026 (teacher screens list); D-70 (per-language greetings); new UX on Monika's cards; M |

## What

She records one greeting, and a Telugu family hears it in Kannada. Let her record one greeting per home language in her class. Setup and Settings list the languages her families have, with Record for each. A family gets the greeting in their language, or the first one she recorded if there isn't one in their language.

## Acceptance criteria

- The list of languages comes from the register, worked out in code.
- Voice consent (D-44) covers all her greetings; withdrawing deletes them all (T-172).
- Admin's greetings page shows each greeting with its language.
- All new strings in en, kn, te, ta, hi; tell Monika.

## How it's tested

- Pure test for which greeting a family gets; API tests; Vitest for the list.
- `docs/database.md` updated if a migration is needed.

## Notes

- 27 Nov 2026: proposed.
