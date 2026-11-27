# T-148 · Admin sign-in for the admin pages

| | |
|---|---|
| Status | Proposed |
| Issue | #326 |
| Owner | R |
| Screens | admin |
| Source | Rahul, 27 Nov 2026 (admin pages list); replaces D-112 (needs a new decision); M |

## What

Every admin page, and the admin API behind it, opens without a key (D-112). Anyone with the address can read children's names, change the WhatsApp numbers families are sent to, change a child's home language, clear a teacher's voice consent, upload a textbook and send a WhatsApp test. That was fine for judging; it isn't for real families.

Add an admin sign-in. Options for the decision:

- **A key per server** (like D-83's `Admin:Key`, from the environment): typed once on `/admin`, kept for the session, sent as a header.
- **An admin role on a teacher login**: sign in with phone and PIN as usual, and the admin pages open only for a teacher marked admin.

Write the decision (next free D- number, superseding D-112) before building.

## Acceptance criteria

- Without the admin sign-in, every `/api/admin/*` endpoint (including the WhatsApp test send and `lesson-feedback`) and the curriculum import endpoints refuse (401 or 404, as decided); the pages show a sign-in, not data.
- The sign-in survives a restart and a deploy; signing out works.
- The secret comes only from environment or user-secrets; nothing in the repo.
- The demo (`/demo`, `/demo/ai`, `/demo/family/*`) works as it does today.
- All new strings in en, kn, te, ta, hi.

## How it's tested

- API tests over `FakeApi` for every admin endpoint: refused without sign-in, allowed with it.
- Vitest for the sign-in on `/admin`; the existing e2e specs keep passing.

## Notes

- 27 Nov 2026: proposed from the list of missing admin pages. Needed before T-149, T-151 and T-152 go to a server.
