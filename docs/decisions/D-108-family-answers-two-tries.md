# D-108 · Family pages: Back and title on every step, choose-and-submit with two tries, the latest answer counts

| | |
|---|---|
| Status | Accepted |
| Owner | Monika (flow and copy, 15 Nov), Rahul (build and point 6), 15 Nov 2026 |
| Gate | none |
| Screens | P5-todays-message, P1-P4-catch-up, P0-evening-message, T1-today, T4-missed-lessons, T3-approve, demo |

## Decision

From Monika's testing (T-116). Changes [D-62](D-01-to-D-99.md) (reopen goes to Done; the first answer counts) and [D-95](D-01-to-D-99.md) ("Getting there"), and T-90's "look again".

- **Answer pages** (today's picture question and the catch-up's): tapping only chooses; a submit button sends. **Nothing says right or wrong.** At most **two submits**: after the first the page shows their choice with **Try again** and Next; after the second it is **locked** (their choice, Next only). A third submit is refused by the API (409). **The latest submit is what the teacher sees.**
- **Levels** on Today and Children, from the latest submit: right → **Got it**, wrong → **Needs help**, none → **Not answered yet**. "Getting there" is gone.
- **Every step** of today's message, the evening message and the catch-up starts with one row: **‹ Back + the step's title**, then the teacher line ("Message from Shobha teacher"). No Back on the first step. Back goes to the previous step the family saw; on Done it opens the answer page (open or locked).
- **Reopening a link starts at the first step** (the reason step stays skipped once given).
- **Promised sections can't be removed** (Rahul, point 6): the approved WhatsApp text says "the lesson audio, a home activity and a picture question are ready", so on Approve → Today's message the concept, activity and picture have no Remove (she can still edit them and choose another pair), and the API refuses removing them (400). The video and her note stay removable.
- **The demo's browser bar** shows "Tappida Paatha", the page's own title, in every language.

## Notes

- 15 Nov 2026: built in T-116 (#232). The catch-up answer row gained `CheckResponses.Attempts` (migration `CheckResponseAttempts`); today's picture uses the existing `PictureFirst`/`PictureSecond`. T-97 (level colours) now has three levels, not four.
