# T-135 · Show which class the app is for, in the header of every teacher screen

| | |
|---|---|
| Status | Done (#286) |
| Issue | #285 |
| Owner | R |
| Screens | app-shell, T11-profile |
| Source | Rahul, 16 Nov 2026; D-79; S |

## What

Only Profile said which class the app was for. Now a line under the app name on every teacher screen says it: **"Grade 3 · Kannada medium · Shobha's class"** (Kannada: "3ನೇ ತರಗತಿ · ಕನ್ನಡ ಮಾಧ್ಯಮ · Shobha ಅವರ ತರಗತಿ"). It names the class's **main** teacher (D-79), so Monika and Savitha, who sign in to Shobha's class, see "Shobha's class" too. It isn't shown on the sign-in screen.

- `/api/me` gains `classTeacherName`. `AuthStore.GetMeAsync` reads it with the grade and medium in one join. No schema change, and the name is never sent to an AI service.
- `features/auth/classLine.ts` builds the line. Profile's class row uses it too, so both read the same.

## Acceptance criteria

- Signed in, every teacher screen shows the class line under the app name, also with the demo's date arrows.
- A co-teacher sees the main teacher's class. An older cached `me` without the name shows grade and medium only.

## How it's tested

- API: `AuthEndpointTests` (`/api/me` carries `classTeacherName`). The existing Db test `StartupDbTests` co-teacher loop now also checks that Monika, Savitha and Shobha all get "Shobha".
- Web: `AppShell.test.tsx` (the line on a screen, the line without a name, none on sign-in), `classLine.test.ts` (en and kn, missing parts), and `ProfileScreen.test.tsx` (the class looked up in Profile's own details, since the header now has it too).
- A local run at 360 px.

## Notes

- 16 Nov 2026: built on the T-134 branch, because both change `session.ts`, `AuthEndpoints.cs` and the locale files. The kn, te, ta and hi strings for "{name}'s class" are drafts for Monika.
