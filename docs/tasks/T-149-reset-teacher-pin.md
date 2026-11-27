# T-149 · Reset a teacher's PIN on /admin/teachers

| | |
|---|---|
| Status | Proposed |
| Issue | #327 |
| Owner | R |
| Screens | admin, T9-sign-in |
| Source | Rahul, 27 Nov 2026 (admin pages list); `docs/status.md` Next 6 (change the server PINs after judging); needs T-148; S |

## What

Changing a teacher's PIN today means clearing `Teachers.PinHash` in the database by hand (the seeder never overwrites one). Add **Reset PIN** to each teacher on `/admin/teachers`: the admin types a new 4–6 digit PIN twice, confirms, and it's stored hashed with `PasswordHasher`, as at sign-in.

Only after T-148: on an open page this would let anyone take over a teacher's login.

## Acceptance criteria

- Reset PIN asks first and saves a new hash; the old PIN stops working, the new one works.
- A PIN that isn't 4–6 digits, or two entries that differ, is refused with how to fix it.
- The PIN is never shown, logged or returned by the API.
- Decide and document whether her signed-in sessions end (cookies are kept in `DataProtectionKeys`).
- Write endpoint takes `clientRequestId` and is idempotent.
- All new strings in en, kn, te, ta, hi.

## How it's tested

- API tests: refused without admin sign-in, PIN rules, old PIN fails and new PIN works at `/api/auth` sign-in.
- Vitest for the dialog on the teachers page.

## Notes

- 27 Nov 2026: proposed.
