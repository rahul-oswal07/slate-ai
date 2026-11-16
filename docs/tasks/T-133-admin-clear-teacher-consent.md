# T-133 · Admin page to clear a teacher's voice consent, so she records her greeting again

| | |
|---|---|
| Status | Done (#282) |
| Issue | #277 |
| Owner | R |
| Screens | admin, T0-setup |
| Source | Rahul, 16 Nov 2026; D-44, D-112; S |

## What

A teacher records her greeting once, in the first-run Welcome, with a voice-consent tick. That sets `Teacher.ConsentAt`, the first-run gate, and there was no way back. The new admin page `/admin/teachers` lists each teacher with her class, her voice consent and her greeting's length. **Clear consent** asks first. It then sets `ConsentAt` to null and deletes her greeting, because D-44 says withdrawn consent deletes the recordings. At her next visit she sees the Welcome and records her greeting with her consent again, or continues without one (D-34). The evening message's approval is untouched.

## Acceptance criteria

- `/admin/teachers` lists every teacher: name, class, "Voice consent given <date>" with the greeting's length, or "No voice consent…".
- Clear consent asks "Clear <name>'s voice consent? Her greeting is deleted…", and nothing changes until "Yes, clear it".
- After clearing, her greeting is gone (families' pages no longer play it), and her next visit opens `/welcome`.
- Replaying the same request clears once. The page is listed on `/admin` and in every admin page's nav.

## How it's tested

- API: `AdminTeacherEndpointTests` (list, no key and noindex in every environment, clear, replay, 404, 400), with a fake store.
- Web: `AdminTeachersScreen.test.tsx` (states, confirm, Cancel, failure). `AdminIndexScreen.test.tsx` lists the new page.
- No Db test (CLAUDE.md: ask first). The store was run against the local Postgres instead, which caught a query EF Core couldn't translate.
- A 360 px screenshot and axe from a local run. No Playwright (CLAUDE.md).

## Notes

- 16 Nov 2026: built. The store's first query (one nested LINQ) passed the fake-store tests, but on the real database it threw "could not be translated" and the page returned 500. It is now three simple queries joined in memory, since there are only a few teachers. The kn, te, ta and hi strings are drafts for Monika. Like every admin page, it is open to anyone with the address (D-112), and that includes clearing consent.
