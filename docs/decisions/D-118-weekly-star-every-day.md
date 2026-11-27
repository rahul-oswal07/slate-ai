# D-118 · Weekly star: every day present, a fresh list each week, a badge for families

| | |
|---|---|
| Status | Accepted (16 Nov, Rahul); changed by [D-130](D-130-one-learner-level-per-child.md) (27 Nov): Week shows the class star count |
| Owner | Monika (flow, copy, WhatsApp template), 15 Nov; Rahul (link and demo), 16 Nov |
| Gate | T-125 |
| Screens | T6-week, T0-setup, P1-P4-catch-up, P5-todays-message, delivery, demo, P6-weekly-star (new) |

## Decision

D-81 (26 Oct) made the weekly star "at most 1 day away", added it up over the term with a catch-up star for every catch-up answered, and showed it only to the teacher. A child away one day could collect more stars than one who came every day, and families got nothing for coming to school. Monika rethought it on 15 Nov (T-125).

This replaces D-81 (1) the rule, (2) the catch-up star count, (3) stars adding up over the term, (4) the class view's totals and families seeing only the catch-up star, and (6) messages to present families being roadmap. D-81 (5) stays: code only, no AI, and the star itself is not stored. Only the messages sent to families are (`WeeklyStarMessages`), so none goes twice.

## Recommendation

1. **Rule.** A child earns the week's star with **every school day present** (0 days away) in a finished week. A school day is a day with a register (Mon–Sat); a school day without the child's row loses it. The setting `Stars:MaxDaysAwayPerWeek` goes.
2. **Nothing counted.** Each week stands alone: no term total, no catch-up count. Week shows that week's list in roll order with no numbers. While the week is running, it shows the children on track so far. The catch-up and today's message Done pages keep their star picture and words.
3. **Send.** Saturday at 6 pm, with the evening job, once Saturday's register is in; on Sunday for a week whose Saturday had no school (a second Saturday) or whose register came late. One `weekly_star_v1` message per child who earned it, to families with consent Yes, not stopped and a saved number (D-39, D-114). Once per child per week. Over WhatsApp only in a language Meta has approved `weekly_star_v1` in: none yet, since English is in review.
4. **Approval (guardrail 3).** The teacher approves the star wording once in setup (step 3, also in Settings): `data/seed/weekly-star.json`, recorded per class in `WeeklyStarApprovals`. Code fills only `{child}` and `{teacher}`. An edit asks again.
5. **Link (was `[CONFIRM: Rahul]`).** A new kind of `/p/{token}` link, so `weekly_star_v1` keeps the same base URL as the other templates. The page has two screens: the message with "Open to see your star badge", then the badge card with one short confetti burst (none with reduced motion), no numbers.
6. **Demo (was `[CONFIRM: Rahul]`).** Reset seeds a finished week in which at least one child was present every day. On `/demo`, "Send messages now" also sends that week's stars, the same way as the evening messages, so the judges see the page without waiting for Saturday.

## Notes

- 16 Nov 2026: accepted by Rahul with the link and demo answers above. D-81's row in the frozen log is now "Changed (D-118; (5) stays)". Built in T-125.
