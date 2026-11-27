# T-178 · Read aloud every fixed line on the family pages

| | |
|---|---|
| Status | Proposed |
| Issue | #356 |
| Owner | R |
| Screens | P0-evening-message, P1-P4-catch-up, P5-todays-message, P6-weekly-star |
| Source | Rahul, 27 Nov 2026 (parent screens list); low-literacy families; new copy and UX on Monika's cards; M |

## What

The lesson is spoken, but the page around it isn't: step titles, "Is everything OK?", the reason choices, the buttons, Done. A parent who can't read well gets stuck between the audio. Add a small speaker button beside each fixed line that plays it.

The clips are made once per language from the locale strings, kept with the app, and never contain a name. Lines with `{child}` say "your child".

## Acceptance criteria

- Every fixed line on every family page has a clip in en, kn, te, ta, hi; a build check fails on a missing one.
- No child or teacher name is ever sent to Sarvam (guardrail 1).
- Playing a line stops any other audio on the page.
- The button has a label in the page's language.
- All new strings in en, kn, te, ta, hi; tell Monika.

## How it's tested

- A script test that every key has a clip per language; Vitest for the button.
- Touches more than 3 files: plan mode first.

## Notes

- 27 Nov 2026: proposed.
