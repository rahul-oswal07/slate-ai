# T-186 · Save or share the weekly star badge

| | |
|---|---|
| Status | Proposed |
| Issue | #364 |
| Owner | R |
| Screens | P6-weekly-star |
| Source | Rahul, 27 Nov 2026 (parent screens list); D-118; new UX on Monika's card; S |

## What

Families want to show the star. On the badge card, add **Save** (as an image) and **Share** (the phone's share sheet, e.g. to a WhatsApp status). The image is drawn on the phone from the badge with the child's name; nothing is uploaded.

## Acceptance criteria

- Only the family's tap shares anything; the server never makes or stores the image.
- The image has no numbers, school or teacher name beyond what the badge shows.
- Works without the share sheet (Save only).
- All new strings in en, kn, te, ta, hi; tell Monika.

## How it's tested

- Vitest for the buttons and the fallback.

## Notes

- 27 Nov 2026: proposed.
