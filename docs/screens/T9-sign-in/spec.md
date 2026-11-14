# T9 Sign in · spec

Phone number and PIN.

| | |
|---|---|
| Route | `/login` |
| Web | `features/auth/` |
| API | `Features/Auth/` |
| Design card (Monika) | [`T9-sign-in.md`](../../design/T9-sign-in.md) |
| Status | [`status.md`](status.md) |

## Purpose and states

Sign in: phone number + PIN, remembered phone, wrong PIN, lock (the design card's own summary, `design/README.md`).

## Requirements

No FR of its own. The rules are D-40 (seeded teachers only, no registration; wrong PIN and unknown phone look the same; lockout) and D-79 (several teachers per class).

## Decisions cited

D-40, D-41, D-42, D-46, D-79.

D-01 to D-99 are in [`../../decisions/D-01-to-D-99.md`](../../decisions/D-01-to-D-99.md); later ones are one file each in [`../../decisions/`](../../decisions/README.md).
