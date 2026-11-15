# D-113 · A child's home language can be changed on the admin page

| | |
|---|---|
| Status | Accepted |
| Owner | Rahul, 15 Nov 2026 |
| Gate | T-122 |
| Screens | admin, delivery, demo |

## Decision

A child's home language came only from `data/seed/demo-class.json`. It decides the language of everything the family gets: the evening message, today's message, lesson audio and catch-ups. Rahul asked for a way to change it on `/admin/children`, next to the family's WhatsApp number (T-118), so test families can be set to any language. What should a change do, and what should the demo Reset do with it?

## Recommendation

- `/admin/children` has a home-language picker on each child's row. The choices are Kannada, English, Telugu, Tamil and Hindi (`kn`, `en`, `te`, `ta`, `hi`, the family languages of D-73). The API refuses any other code.
- **Applies forward only.** Saving sets `Children.HomeLanguage` and `HomeLanguageSetAt`, and everything made or sent after that uses the new language. What was made before keeps its old language: a catch-up already generated, today's message already drafted, an evening message already sent.
- **Sends nothing.** Changing the language never sends a message, and the approval rules (guardrail 3) are unchanged.
- **Survives the demo Reset**, like saved numbers (D-110). Reset puts the changed language back on the re-created child, so test families are set once.
- **The demo's pre-made catch-up still goes to its story child by the seed's language.** If that child's language is changed, the pre-made catch-up stays in the seed language.
- **The page is open to anyone with the address (D-112)**, so anyone can change a child's language.

## Notes

- 15 Nov 2026: accepted by Rahul, who chose to keep the language across Reset. Built in T-122.
