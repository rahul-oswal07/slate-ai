# T-185 · A family can choose the page's language

| | |
|---|---|
| Status | Proposed |
| Issue | #363 |
| Owner | R |
| Screens | P0-evening-message, P1-P4-catch-up, P5-todays-message, P6-weekly-star, admin |
| Source | Rahul, 27 Nov 2026 (parent screens list); D-113 (home language set on the admin page) needs updating; S–M |

## What

The home language is set by the school (D-113), but sometimes the child reads English better, or the parent reads Kannada better than their spoken language. Add a language choice on the family pages.

- **Page words** (buttons, titles) switch straight away.
- **The lesson itself** stays in the approved language, because nothing else was approved.
- The family can ask for future messages in the new language: that changes the child's home language from the next message on, and the teacher and admin see "Changed by family".

## Acceptance criteria

- Approved content is never translated on the fly (guardrail 3).
- A family's change shows on `/admin/children` and on the child view.
- Update D-113, or write a decision, for families changing it.
- All new strings in en, kn, te, ta, hi.

## How it's tested

- API tests for the change and what is sent after; Vitest for the picker.

## Notes

- 27 Nov 2026: proposed.
