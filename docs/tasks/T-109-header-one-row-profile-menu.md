# T-109 · Header in one row: name left, language and profile menu right

| | |
|---|---|
| Status | Proposed · needs Rahul's approval before building |
| Issue | #210 |
| Owner | M (layout and copy), R builds |
| Screens | app-shell, T1-today, T9-sign-in, T10-settings |
| Source | Monika, 14 Nov · Finale Should · replaces the header part of T-95 / D-96 |

## What

Grand Finale (Wed 18 Nov). **Proposed by Monika, 14 Nov. Needs Rahul's approval before building.** Should, not Must: builds merge by Sun 15 Nov, so cut it if the Must rows need the time. Replaces the header part of T-95 / D-96 ("Log out moves to Settings; the gear gets a Settings label"); the bottom nav part of T-95 is unchanged.

**Problem**
The header takes two rows (name + Log out, then tagline + language), and Today adds a third (the "Saved" badge + an unlabelled gear). Log out sits in the most visible spot on every screen, and Settings can only be reached from Today.

**Wanted (Monika)**
One row on every teacher screen:

```
Tappida Paatha                     [ English ▾ ]  ( 👤 )
The missed lesson, at home
```

- **Left:** the app name, with the tagline small below it (existing keys `app.name`, `app.tagline`; tagline `text-caption text-ink-muted`).
- **Far right:** the language dropdown (T-107's `LanguageSwitcher`), then a **profile icon button**.
- The profile button opens a small menu: **Profile · Settings · Log out**.

**Task (T-109)**
1. **`AppShell` header in one row** as above. `LogoutButton` leaves the header and moves into the menu.
2. **Profile menu:** icon button at least 48 px, `aria-label` "Profile and settings", `aria-haspopup="menu"`, `aria-expanded`. Items: Profile → `/profile`, Settings → `/settings`, Log out (today's `LogoutButton` behaviour, including the offline "stays signed in" message). It closes on Escape, a tap outside and after choosing; focus goes back to the button; arrow keys move between items. At 360 px it doesn't run off the right edge.
3. **New Profile screen `/profile` (read-only):** her name, her phone with only the last 4 digits ("•••• 3210"), and her class ("Grade 3 · Kannada medium"). Link back to Today. `MeResponse` doesn't carry grade, medium or phone yet: add them (phone as last 4 only) or add a small `GET /api/profile`, your call. No editing in this task.
4. **Today:** the gear leaves the title row. The **"Saved" sync badge stays** beside Today's title, as now.
5. **Sign-in screen:** name + tagline + language only, no profile button.

## Acceptance criteria

- At 360 px the header is one row in all five languages (kn, en, te, ta, hi), **Tamil included**: the name isn't cut or broken mid-word, and no page is wider than the screen. (T-107 put language on a second row because a third control squeezed the name to ~90 px in Tamil. This layout has to solve that, e.g. with a shorter label in the closed select; your call.)
- Settings and Log out can be reached from the menu on every teacher screen; Log out is no longer in the header itself.
- The Profile screen never shows the full phone number.
- Design tokens only, visible focus, light and dark.
- New strings (menu label, Profile title and rows) have keys in all five locale files.

## How it's tested

Vitest: the menu (opens, the three items, Escape and an outside tap close it, focus returns, Log out calls the API and goes to `/login`) and the Profile screen with a fake `me`. Endpoint test if `MeResponse` or a new endpoint changes. Existing e2e specs still pass (no new spec). `verify.sh`. 360 px screenshots in kn and ta, light and dark; axe on Today and Profile.

## Notes

- 15 Nov: moved here from issue #210 and `status.md` (T-110).
- 14 Nov, Monika: **Build after #209 (T-108) merges:** both change the title row in `TodayScreen.tsx`.
- 14 Nov, Monika: If Sunday is tight, the menu can ship with Settings and Log out first and Profile can follow.
- 14 Nov, Monika: I'll update T-95, D-96 and the T1 design card once you approve, or you can include them in your PR.

