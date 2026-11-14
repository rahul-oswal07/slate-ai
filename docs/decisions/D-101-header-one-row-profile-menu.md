# D-101 · Header in one row with a profile menu

| | |
|---|---|
| Status | Accepted |
| Owner | Monika (layout and copy), 14 Nov; accepted by Rahul, 15 Nov |
| Gate | T-109 |
| Screens | app-shell, T1-today, T9-sign-in, T10-settings, T11-profile |

## Decision

Where Log out, Settings and the language switch sit. The header took two rows (name + Log out, then tagline + language) and Today added a third (the Saved badge + an unlabelled gear); Log out was the most visible control on every screen and Settings could be reached only from Today. This replaces the header part of D-96 ("Log out moves to Settings; the gear gets a Settings label"). D-96's four-tab bottom nav is unchanged (T-95).

## Recommendation

- **One row on every teacher screen:** the app name with the tagline small under it on the left; the language dropdown and a profile icon button on the right.
- **The profile button opens a menu: Profile · Settings · Log out.** Log out is no longer in the header itself; the gear leaves Today's title row; the Saved badge stays beside Today's title.
- **Profile** (`/profile`, T11) is read only: name, phone with the last 4 digits only, class. The full phone number never leaves the server.
- **Sign-in:** name, tagline and language only. Before set-up is done (`/welcome…`) the menu has Log out only, since Profile and Settings can't be opened yet.
- **Beside the profile button the closed language dropdown shows a short label** ("ಕ", "En", "हि", "తె", "த") with the globe. Measured at 360 px: with the full language name the app name wrapped to two lines in Tamil and English and the header grew to 157 px. The picker still lists the full names, and sign-in and `/demo` keep the full name.

## Notes

- 15 Nov: accepted by Rahul; built in T-109. The short labels are Rahul's draft: Monika may change them (`language.short.*` in the locale files).
