# T-175 · Forgot PIN and a clearer lockout on sign-in

| | |
|---|---|
| Status | Proposed |
| Issue | #353 |
| Owner | R |
| Screens | T9-sign-in |
| Source | Rahul, 27 Nov 2026 (teacher screens list); T-47 (lockout after 5 wrong PINs); needs T-149; S |

## What

A teacher who forgets her PIN has no way forward on the sign-in screen. Add **Forgot PIN?**, which says who can reset it (the school's admin, T-149). There's no SMS. After a lockout, say when she can try again instead of only refusing.

## Acceptance criteria

- The lockout message shows the time she can try again, from the API.
- Nothing on sign-in reveals whether a phone number has an account.
- All new strings in en, kn, te, ta, hi.

## How it's tested

- API test for the lockout time; Vitest for both states.

## Notes

- 27 Nov 2026: proposed.
