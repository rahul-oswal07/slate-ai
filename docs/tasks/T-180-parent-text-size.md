# T-180 · A text-size button on the family pages

| | |
|---|---|
| Status | Proposed |
| Issue | #358 |
| Owner | R |
| Screens | P0-evening-message, P1-P4-catch-up, P5-todays-message, P6-weekly-star |
| Source | Rahul, 27 Nov 2026 (parent screens list); new UX on Monika's cards; S |

## What

Grandparents often do the lesson with the child, and Kannada script is small on a cheap phone. Add an **A A** button in the header that switches between normal and large text. The choice is kept on the phone.

## Acceptance criteria

- Large text fits at 360 px with no sideways scroll; sticks and pictures still fit.
- Sizes come from design tokens, not hard-coded values.
- The button has a label and pressed state for screen readers.
- All new strings in en, kn, te, ta, hi; tell Monika.

## How it's tested

- Vitest for the switch; screenshots at 360 px in both sizes.

## Notes

- 27 Nov 2026: proposed.
