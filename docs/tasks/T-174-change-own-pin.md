# T-174 · Change her own PIN on Profile

| | |
|---|---|
| Status | Proposed |
| Issue | #352 |
| Owner | R |
| Screens | T11-profile, T9-sign-in |
| Source | Rahul, 27 Nov 2026 (teacher screens list); T11 gap (nothing can be edited); pairs with T-149; S |

## What

Every login on a server shares one PIN until it's changed after judging, and a teacher can't change her own. Add **Change PIN** on Profile: current PIN, new PIN twice. It's stored hashed with `PasswordHasher`.

## Acceptance criteria

- A wrong current PIN is refused and counts toward the 5-try lockout (T-47).
- The new PIN must be 4–6 digits, entered the same twice.
- The PIN is never logged or returned.
- Write endpoint takes `clientRequestId` and is idempotent.
- All new strings in en, kn, te, ta, hi.

## How it's tested

- API tests: wrong current PIN, rules, sign-in with the new PIN; Vitest for the form.

## Notes

- 27 Nov 2026: proposed.
