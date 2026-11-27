# T-151 · Add a teacher and switch one off on /admin/teachers

| | |
|---|---|
| Status | Proposed |
| Issue | #329 |
| Owner | R |
| Screens | admin, T9-sign-in |
| Source | Rahul, 27 Nov 2026 (admin pages list); D-40 (teacher login); needs T-148, works with T-149 and T-150; M |

## What

Teachers come only from the seed. On `/admin/teachers`:

- **Add teacher**: name, 10-digit phone (unique), a first PIN (hashed), and her classes;
- **Edit**: name and phone;
- **Switch off**: she can no longer sign in; what she approved, sent and recorded stays (approvals keep who approved).

## Acceptance criteria

- A phone already used by another teacher is refused with how to fix it.
- A switched-off teacher can't sign in, and an open session of hers stops working.
- A class can't lose its last teacher by switching one off (refused with the class).
- Teacher names stay out of AI calls (guardrail 1); nothing here calls AI.
- Write endpoints take `clientRequestId` and are idempotent.
- `docs/database.md` updated with the migration (e.g. a switched-off column).
- All new strings in en, kn, te, ta, hi.

## How it's tested

- API tests: add, duplicate phone, sign-in after switch-off refused, last-teacher rule.
- Vitest for the page.

## Notes

- 27 Nov 2026: proposed.
