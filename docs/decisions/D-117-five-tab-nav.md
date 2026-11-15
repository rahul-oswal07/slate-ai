# D-117 · Five tabs again: Missed and Back today each get their own tab

| | |
|---|---|
| Status | Proposed |
| Owner | Monika, 15 Nov 2026. **[CONFIRM: Rahul]** |
| Gate | T-129 |
| Screens | app-shell, T4-missed-lessons, T5-back-today |

## Decision

D-96 (13 Nov, from guideline G3) cut the bottom nav from five tabs to four: **Today · Approve · Children · Week**. "Missed" became "Children", and Back today became a filter inside it. T-95 built this on 15 Nov (#237).

On the built screen, the new nav reads worse than the old one:

- "Missed" is gone, and "Children" doesn't say what the tab is for.
- Back today, the second half of the story (absent → catch-up at home → back in class with a buddy, FR-12), is hidden behind a filter that the teacher has to know about.

Five tabs fit at phone width. WhatsApp uses five, and the app used five until 15 Nov.

This replaces the bottom-nav part of D-96. The header part of D-96 was already replaced by D-101.

## Recommendation

- The bottom nav goes back to the menu used before D-96, with the same order and labels: **Today · Approve · Missed · Back today · Week**.

  | | Missed | Back today |
  |---|---|---|
  | en | Missed | Back today |
  | kn | ತಪ್ಪಿದ ಪಾಠ | ಮರಳಿ ಬಂದವರು |
  | te | తప్పిన పాఠం | తిరిగి వచ్చినవారు |
  | ta | தவறிய பாடம் | திரும்பி வந்தோர் (T-84) |
  | hi | छूटे | आज लौटे |

- **Missed** (`/missed`) is titled **Missed lessons** again (kn ತಪ್ಪಿದ ಪಾಠಗಳು). By child and By lesson stay. The "Back today · n" filter is removed, because Back today has its own tab.
- **Back today** (`/back`) is its own screen again, with the buddy cards. `/back` stops redirecting to `/missed?filter=back`, and the old `/missed?filter=back` link opens `/back`.
- **Count badge on Back today:** when 1 or more children are back today, the tab shows how many, in the same neutral style as Approve's "waiting" badge (never in the error colour). There is no badge when the count is 0. The number is the count of today's buddy cards, worked out in code (guardrail 4). Screen readers hear it as part of the tab name, as with Approve.

Everything else built since D-96 stays as it is, for example the level tags in By child (T-92) and the header from D-101.

## Notes

- 15 Nov 2026: proposed by Monika after seeing the four-tab nav on the built app. Once Rahul accepts, D-96's row in the frozen log gets the status "Changed (nav: D-117; header: D-101)".
