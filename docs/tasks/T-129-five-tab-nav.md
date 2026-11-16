# T-129 · Five tabs again: Today · Approve · Missed · Back today · Week

| | |
|---|---|
| Status | Done |
| Issue | #265 |
| Owner | R |
| Screens | app-shell, T4-missed-lessons, T5-back-today |
| Source | Monika, 15 Nov 2026, after seeing the four-tab nav on the built app; D-117 (replaces the nav part of D-96); size S |

## What

T-95 (#237) built D-96's four tabs: **Today · Approve · Children · Week**, with Back today as a filter inside Children. On the built screen, "Missed" is gone, "Children" doesn't say what the tab is for, and Back today is hidden behind a filter. Back today is the moment when a returning child gets a buddy (FR-12), and the demo story needs it.

The bottom nav goes back to the five tabs used before D-96, with the same order and labels:

**Today · Approve · Missed · Back today · Week**

- **Missed** (`/missed`) is the missed-lessons screen, titled **Missed lessons** again. By child and By lesson stay. The "Back today · n" filter is removed. The other filters (Absent this week, Everyone) stay.
- **Back today** (`/back`) is its own screen again, with the buddy cards.
- When 1 or more children are back today, the **Back today** tab shows how many, like Approve's "waiting" badge.

Mostly this undoes the nav and title part of #237. Everything built since D-96 stays, for example the level tags in By child (T-92) and the header from D-101.

## Acceptance criteria

- Five equal tabs in this order: Today · Approve · Missed · Back today · Week. The active tab is marked with `aria-current`. Today stays current on its sub-screens (lesson confirm, Settings, Profile).
- Labels are the ones used before D-96, in every language:
  - en: Missed · Back today
  - kn: ತಪ್ಪಿದ ಪಾಠ · ಮರಳಿ ಬಂದವರು
  - te: తప్పిన పాఠం · తిరిగి వచ్చినవారు
  - ta: தவறிய பாடம் · திரும்பி வந்தோர்
  - hi: छूटे · आज लौटे
- The Missed screen's title is "Missed lessons" (kn ತಪ್ಪಿದ ಪಾಠಗಳು, te తప్పిన పాఠాలు, ta தவறிய பாடங்கள், hi छूटे हुए पाठ). By child has no "Back today" filter.
- `/back` shows the Back today screen. It doesn't redirect anymore. Printing still prints only the cards. The old link `/missed?filter=back` opens `/back`.
- The Back today badge:
  - shows the number of today's buddy cards when it is 1 or more, and nothing at 0;
  - uses Approve's neutral style, never the error colour;
  - is read by screen readers as part of the tab name (a new key like `nav.backToday`, with the count, in all five locale files);
  - offline, uses the saved buddy cards.
- At 360 px, all five labels fit in every language, Tamil included. They may wrap to two lines as before T-95, but they must not be cut off or overlap. Each tab is at least 48 px. Design tokens only, in light and dark.

## How it's tested

- Vitest:
  - `BottomNav`: five tabs in order, `aria-current`, the badge at 0 and at n, the screen-reader text;
  - `MissedScreen`: the title, and no Back today filter;
  - `BackTodayScreen` on its own route;
  - the `/missed?filter=back` link;
  - the existing tests that expect "Children" or the filter are updated.
- The existing e2e specs pass. No new spec (22 Oct rule).
- `verify.sh`.
- 360 px screenshots of the nav in kn and ta, light and dark, plus Back today with a badge. axe on Today, Missed and Back today.

## Notes

- 15 Nov 2026: proposed by Monika (D-117). She chose the old order and labels, the "Missed lessons" title without the Back today filter, and the count badge. D-117 was first numbered D-116, which T-128 (#264) took first.
- 16 Nov 2026: **built.** This undoes the nav and title part of #237 (code reverted to before T-95; `App.tsx` and locales edited by hand, so the later work stays). D-117 is accepted, including `/missed?filter=back` → `/back` (handled by a small `MissedRoute` in `App.tsx`). The Back today badge counts today's buddy cards with `useBuddyCards`, the same query as the screen, so offline it uses the saved cards. It reuses Approve's neutral badge (`bg-ink`, `aria-hidden` number, `sr-only` suffix). Screen readers hear "Back today, 2 children" (`nav.backToday_one` / `_other`, plural like the Approve headings). New `BottomNav.test.tsx`: order, hrefs, `aria-current` incl. Today's sub-screens, badge at 0 / 1 / 2, offline snapshot, kn/te/ta/hi labels. `MissedScreen.test.tsx`: title, two filters only, the old link. **Drafts for Monika:** the kn/te/ta/hi badge suffix (taken from the Approve "child / children" headings). Checked at 360 px in all five languages, light and dark (a throwaway Playwright script, not committed): every tab ≥ 48 px, labels inside the screen and not overlapping. Tamil "வந்தோர்" is 74 px in a 72 px tab, so it reaches ~1 px into the neighbours' padding without clipping. axe has no serious/critical issues on Today, Missed and Back today (en, kn; light and dark). Screenshots: `docs/design/screenshots/T1-nav-five-tabs-*`, `T4-missed-five-tabs-*`, `T5-back-today-badge-*`. Existing 12 e2e specs pass. The T1 design card already shows five tabs (it never changed for D-96).
