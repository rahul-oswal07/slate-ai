# T-153 · Families and consent: mark each family's consent

| | |
|---|---|
| Status | Proposed |
| Issue | #331 |
| Owner | R |
| Screens | admin, T10-settings |
| Source | Rahul, 27 Nov 2026 (admin pages list); FR-15, FR-19, FR-20, D-30, D-39, D-43, D-51, D-70; design card `docs/design/T8-families.md` (Monika); M |

## What

Consent comes only from the seed. The send filter (consent Yes and not stopped) is already enforced in code. Make consent editable: Yes, No or No phone from the paper form, with when and who changed it, and "Stopped by family" shown (only the family can undo their stop, D-30).

**Decide first where it lives:**

- **On `/admin/children`** (quicker, next to the numbers): but the T8 card says the list is shown to no one but the teacher (spec §7), and `/admin/children` already shows full numbers;
- **As T8 for the teacher** (Families and consent from Settings and Missed lessons, as designed): the card is Monika's copy and UX; tell her.

## Acceptance criteria

- Consent is one of Yes / No / No phone per child, saved with time and who changed it.
- No and No phone send nothing (evening, today's message, catch-up, weekly star); the send endpoints refuse with 409, not only the UI.
- A family's own stop shows as "Stopped by family"; setting Yes doesn't undo it.
- Children without consent still get a buddy card on return (FR-12).
- Write endpoints take `clientRequestId` and are idempotent.
- All new strings in en, kn, te, ta, hi.

## How it's tested

- API tests for the send filter after a change; Vitest for the controls.

## Notes

- 27 Nov 2026: proposed; where it lives is open.
