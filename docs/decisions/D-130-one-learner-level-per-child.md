# D-130 · One learner level per child: from the catch-up on Missed, Back today and Week; a star count on Week

| | |
|---|---|
| Status | Accepted |
| Owner | Rahul (build, 27 Nov, on his go-ahead for T-97 to T-99); Monika to confirm the copy |
| Gate | none |
| Screens | T1-today, T4-missed-lessons, T5-back-today, T6-week, app-shell |

## Decision

[D-95](D-01-to-D-99.md) gives each child one learner level, the same on Today, Children, the child view, Back today and Week, and [D-108](D-108-family-answers-two-tries.md) cut it to three: **Got it**, **Needs help**, **Not answered yet**. Until now only Today and Missed showed one, both from today's picture question (T-92). Building T-97 to T-99 needed three answers D-95 didn't give: where the level comes from for a child who was away, which level a child with several missed lessons gets, and whether Week may show a count of stars when [D-118](D-118-weekly-star-every-day.md) says "no numbers".

## Recommendation

1. **A missed lesson's level comes from its catch-up**, as the ledger already counts it (FR-05): caught up → Got it, needs buddy → Needs help, sent but not answered → Not answered yet. A lesson whose catch-up hasn't gone has no level; the child view shows it as a "Missed lesson" chip.
2. **One level per child for this week's missed lessons:** Needs help if any lesson needs help, else Not answered yet if any is waiting, else Got it. One lesson needing help is never hidden by another that was got. Code: `features/missed/levels.ts`.
3. **Missed lessons (table):** the Level column shows that level; a child with no missed lesson this week shows today's picture question level (T-92), as before; no level shows "—".
4. **Back today:** the level of the absence the child is back from (their latest `missedDays` missed lessons) sits above the buddy card, never on it and never printed: the T5 card's "no result or score on the card" (B-20) stands.
5. **Week's headline** counts children away this week (`daysAwayThisWeek > 0`) and, of them, how many are Got it and Needs help: "6 away · 4 got it · 2 need help". Counted in the web app from the ledger for the same school day; no API change.
6. **Week's stars** collapse to "N earned a star · See all" (T-99). N is the class count, shown only to the teacher. D-118's "no numbers" stays true for everything else: nothing per child, no totals over weeks, nothing to families.

## Notes

- 27 Nov 2026: built in T-97, T-98 and T-99. FR-24's acceptance ("with no numbers") in `docs/screens/T6-week/spec.md` needs Monika's wording for point 6.
