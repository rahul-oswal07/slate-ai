# T-63 · Attendance stars (FR-24, D-81)

| | |
|---|---|
| Task | T-63 (`docs/tasks.md`, *Open*) |
| Requirement | FR-24 (`spec.md` §3) · Decision D-81 (`spec.md` §8) |
| Source | Education mentor, 24 Oct (`mentor-feedback-2026-10-24.md` §2 point 5: "attendance incentives: stars and streaks") |
| Owner | Rahul (product owner with Monika from 26 Oct, and engineering) |
| Status | **Planned (26 Oct).** Accepted by Rahul as product owner; build next |
| Branch | `feat/T-63-attendance-stars` · one PR |
| Size | About one day: no migration, no AI, no new send path |

---

## 1. Why

The mentor asked us to recognise children who come to school regularly. Today the app only notices a child when they are away. Children who keep coming, and children who come back and do their catch-up, get nothing.

## 2. What we decided (D-81)

**Stars, not streaks.** A classic streak counts unbroken days and drops to zero after one absence. Our children are away because of illness, seasonal work and festivals, so a streak punishes exactly the children this app is for, and `spec.md` §5 forbids shame or punishment. So:

1. **Weekly attendance star.** A child earns one star for each **finished** school week (Mon–Sat) in which they were present on at least one day and away on **at most 1** school day (`Stars:MaxDaysAwayPerWeek`, default 1). Days away are counted rather than days present, so holiday weeks and short weeks need no special rule: a day with no register is not a school day.
2. **Catch-up star.** A child earns one star for each catch-up whose picture question they answered at home, **whatever the answer** (same rule as "Well done" on P4, D-20). It rewards coming back and doing the work, not luck or correctness.
3. **Stars never go away.** They add up over the term. A bad week means no star that week, nothing more. No screen or message ever says "lost", "broke" or "missed a star".
4. **Positive and private.**
   - Only the teacher sees the class-wide view (T6 Week).
   - No ranking, no "top children", no list or count of children who didn't earn one.
   - Children are listed in roll-number order.
   - The family sees only their own child's catch-up star on P4 Done.
   - Absent families are never told about other children's stars.
5. **Recognition happens in class.** The card ends with "Say well done in class." The teacher's praise is the reward that matters to a child; the app only tells her who to praise.
6. **Code only.** Stars are arithmetic over the `Attendance` register and `CheckResponses` the app already keeps (guardrail 4). No LLM, no Sarvam, no names sent anywhere, nothing stored.

### Not in T-63 (roadmap)

- The "well done, keep it up" message to families of children who earned the weekly star: **T-64** ([`T-64-star-message.md`](T-64-star-message.md)). T-63 adds no send path.
- A printable class star chart, badges for longer runs (for example "10 stars"), and a term-start setting. Until then the term is every register the class has.
- Stars on Today, Missed or Back today.

## 3. Screens and copy

### T6 Week: new "Stars" card

Placed after the reasons card and before the pacing nudge. It shows in every state of the week, including "No absences this week" (D-61), since that is a week to celebrate.

| State | What it shows |
|---|---|
| Finished week, some stars | Title, a sentence with the count, one chip per child who earned the week's star (roll number, first name, amber star, term total), the hint |
| Current week (not finished) | Title "Stars this week", the "so far" sentence, chips for children on track (away at most 1 day so far and present at least once). The hint is the same |
| Nobody earned one | Title, "No stars this week. Every week is a new start.", hint. **No** names |
| No school days that week | The card is hidden |
| Offline (saved week) | Same as the saved data; the existing offline line covers it |

A week is **finished** when its last school day is before today (IST), or is today and today's register has come in. School days come from the school calendar (Sunday, second Saturday and `Attendance:Sats:Holidays` are not school days), moved from `SimulatedSatsSource.IsSchoolDay` into a shared `SchoolCalendar` so T-64 uses the same rule.

The term total on each chip counts weekly stars plus catch-up stars. The screen-reader text spells it out: "Asha, roll 3, 4 stars this term: 3 for coming to school, 1 for a catch-up".

### P4 Done: one extra line

Under "Well done, {child}!": an amber star (decorative, `aria-hidden`) and "{child} earned a star for doing this catch-up." Shown whenever P4 shows (the answer was sent). No totals and no attendance numbers on any parent page.

### Strings (all five locale files: `en`, `kn`, `te`, `ta`, `hi`)

| Key | English |
|---|---|
| `week.stars.title` | Stars |
| `week.stars.titleThisWeek` | Stars this week |
| `week.stars.earned_one` / `_other` | {{count}} child earned a star this week. / {{count}} children earned a star this week. |
| `week.stars.soFar_one` / `_other` | So far {{count}} child is on track for a star. / So far {{count}} children are on track for a star. |
| `week.stars.none` | No stars this week. Every week is a new start. |
| `week.stars.total_one` / `_other` | {{count}} star this term / {{count}} stars this term |
| `week.stars.childLabel` | {{name}}, roll {{roll}}, {{total}}: {{attendance}} for coming to school, {{catchUps}} for a catch-up |
| `week.stars.hint` | Say well done in class. |
| `week.stars.rule` | A star for each week with at most one day away, and for each catch-up done at home. |
| `parent.done.star` | {{child}} earned a star for doing this catch-up. |

Kannada, Telugu, Tamil and Hindi are written in the same warm register as the existing `parent.done.*` and `week.*` strings. The `locales.test.ts` key check fails if one is missing.

Visuals follow `docs/design/tokens.md` (D-77):
- **Teacher side:** the amber star is this screen's one accent element. The nudge stays teal; the star is an icon, never text colour.
- **Chips:** the `chip` class.
- **Phone width:** wraps at 360 px with no horizontal scroll.

## 4. API

### New slice `src/api/SlateAi.Api/Features/Stars/`

- **`StarRules.cs`**: pure and static, unit-tested with in-memory data.
  - Input: `LedgerData` (children, register, catch-ups with outcome), the week's Monday, today (IST), `StarsOptions`.
  - Output: `WeekStars`.
- **`StarsOptions.cs`**: `Section = "Stars"` (as the other options classes), `MaxDaysAwayPerWeek` (int, default 1, validated 0–5), bound from `Stars` in `appsettings.json`.
- **No store.** The data comes from `ILedgerStore.LoadAsync`, which T6 already calls. A child's catch-up counts as done when its `CatchUpRow.Outcome` is not null (`caughtUp` or `needsBuddy`).

```csharp
/// <param name="Finished">The week is over: its last school day is before today, or is today with the register in.</param>
/// <param name="Children">Earned (finished week) or on track (current week), roll-number order. Never the others.</param>
public sealed record WeekStars(bool Finished, int SchoolDays, IReadOnlyList<StarChild> Children);

/// <param name="Attendance">Weekly stars this term, finished weeks only.</param>
/// <param name="CatchUps">Catch-ups answered at home this term.</param>
public sealed record StarChild(Guid ChildId, int RollNo, string FirstName, int Attendance, int CatchUps)
{
    public int Total => Attendance + CatchUps;
}
```

### Rules in `StarRules`

- **School days in a week:** dates in Mon–Sat that have any register row for the class.
- **A child's week:**
  - `away` = days with `Present = false`; `present` = days with `Present = true`.
  - No rows for that child (for example, joined later) means the week is skipped.
  - The star is earned when `present ≥ 1 && away ≤ MaxDaysAwayPerWeek`.
- **Term attendance stars:** the count of finished weeks the child earned, over every week with a register up to and including the shown week. A week in the future of the shown week is not counted, so going back to an older week shows the totals as they were then.
- **Catch-up stars:** answered catch-ups whose week is on or before the shown week.
- **Finished week:** per §3, from `SchoolCalendar` (passed in, so tests can use any calendar).
- **Determinism:** the same `LedgerData` always gives the same result. There is no clock inside the function; `today` is passed in.

### `GET /api/classes/{id}/week/{date}` gains `stars`

- Add `WeekStars? Stars` to `WeekResponse`. It is null when the week has no school days.
- `WeekEndpoints.WeekAsync` calls `StarRules.For(data, weekOf, SchoolClock.Today(clock), options.Value)` and passes the result to `WeekBuilder.Build`, or sets it on the record.
- Update the endpoint's `WithDescription`. It is still teacher-only (`RequireClassOwner`), so no new auth surface.
- The pacing PUT returns the same `WeekResponse`, so it carries `stars` too.
- Register `StarsOptions` in `Program.cs`, next to the other `IOptions` bindings.

Why not a new endpoint: T6 already loads the ledger once, caches it for offline use (`savedAt`) and is the teacher's weekly view. One request keeps the offline copy whole.

### Parent page

No API change. `ParentCatchUpResponse.Answered` already tells the page the answer was sent, and P4 only shows after that. The star line is fixed copy with the child's name, which is already in the written text (D-50). It contains no number.

### Guardrails check

| Guardrail | How T-63 keeps it |
|---|---|
| 1 Names | No external call at all |
| 2 LLM output | No LLM |
| 3 Approval | No new message to families; P4 is reached only through an approved catch-up |
| 4 Numbers in code | Every count comes from `StarRules` |
| 5 / 6 Models, prompts | Not touched; no eval run needed |

## 5. Web

| File | Change |
|---|---|
| `src/web/src/features/week/types.ts` | `WeekStars`, `StarChild`; `stars: WeekStars \| null` on `Week` |
| `src/web/src/features/week/StarsCard.tsx` (new) | The card from §3. Section with `aria-labelledby`, a list (`ul`) of chips, `aria-label` from `week.stars.childLabel`, star SVG `aria-hidden` |
| `src/web/src/features/week/WeekScreen.tsx` | Render `<StarsCard>` after the reasons card in both the empty and non-empty branches; hidden when `week.stars` is null |
| `src/web/src/features/parent/DoneStep.tsx` | The `parent.done.star` line with a small amber star under the "Well done" title |
| `src/web/src/i18n/locales/{en,kn,te,ta,hi}.json` | Keys from §3 |
| `src/web/src/components/` | Reuse the star path from `DoneStep`'s `Celebration`. Move it to a small `ChalkStar` component if both use it |

No new route, hook or query: `useWeek` already fetches the response.

## 6. Demo data

Today the Reset writes only the 5 school days before today (`DemoStory`), so nobody would have more than one finished week. To make the card show at judging:

- `DemoStory` writes **three more finished weeks** of registers before the existing days, with **no lessons** on those days. That leaves Missed lessons, the ledger's 7-day return speed and the T6 this-week/last-week numbers unchanged.
- In those weeks everyone is present except:
  - one child away 1 day (still earns the star, showing the rule is kind);
  - one child away 3 days in one week (no star that week, still has the others).
- Ravi, Manjunath and Rohan keep their current story. Rohan's English catch-up, once answered on the demo phone, shows the P4 star line.
- Update the `DemoStory` doc comments and `docs/design/mvp-build-flow.md` if the demo walkthrough mentions the Week screen.

Check before merging: the existing `DemoStory`, Week and ledger tests still pass unchanged. If any of them pin the number of seeded days, the extra weeks go behind a separate `DemoStory.StarWeeks` list so those tests keep their meaning.

## 7. Tests

**xUnit, `tests/api/.../Stars/StarRulesTests.cs`** (pure, no DB):
- 6 of 6 present gives a star; 1 away gives a star; 2 away gives none (default option).
- `MaxDaysAwayPerWeek = 0` gives no star for 1 day away.
- Holiday week with 3 school days and 1 away gives a star; a week with no registers is skipped and counts as neither.
- Away every school day of a week gives no star, even when `away ≤ max` in a one-day week.
- Child with no rows that week (joined later) is skipped.
- Current week: `Finished = false`, children on track listed, no attendance star added to totals yet.
- Last school day: finished only once its register is in; a second-Saturday week finishes after Friday's register.
- Totals: three finished weeks with one missed gives 2; an older shown week gives the totals as of then.
- Catch-up stars: `caughtUp` and `needsBuddy` both count; unanswered (`Outcome = null`) doesn't; later weeks don't count for an older week.
- Order is by roll number; children who didn't earn a star are never in `Children`.
- Same input twice gives the same output.

**xUnit, endpoint (`FakeApi.Create` with the fake ledger store):**
- `GET week` returns `stars` with the expected children and totals.
- `stars` is null for a week with no school days.
- Another teacher's class is 404/403 as today.
- The pacing PUT response includes `stars`.

**Vitest:**
- `StarsCard.test.tsx`:
  - finished week with chips;
  - current week "so far";
  - nobody earned one ("No stars", no names);
  - hidden when null;
  - accessible names on the chips;
  - plural forms.
- `WeekScreen.test.tsx`: the card renders in the empty-week branch too.
- `DoneStep` (in `ParentScreen.test.tsx` or a new test): the star line with the child's name; no number on the page.
- `locales.test.ts`: passes with the new keys in all five files.

**Not added:**
- No new Playwright test (CLAUDE.md, 22 Oct); the existing `src/web/e2e/` specs must still pass.
- No Db test: no migration and no new query (the ledger query is already covered).

**Run:** `./scripts/verify.sh` and report the result.

**Screenshots:** T6 Week (finished week, current week, none) and P4 Done at 360 px, light and dark, in the PR. Run axe on both screens locally and report the result.

## 8. Docs to update in the same PR

- `docs/spec.md`:
  - FR-24 and D-81: added with this plan; adjust if the build changes anything.
  - §6 endpoint list: `stars` on the week response.
- `docs/database.md` §3: stars are a query like the ledger, nothing stored.
- `docs/design/T6-week.md` and `docs/design/P4-done.md`: the new card and line.
- `docs/plan.md` §3: a row for the stars card.
- `docs/tasks.md`: move T-63 to *Done*.
- `docs/status.md`: what changed and what's next.
- `docs/judging-narrative.md`: one line, "we recognise coming to school and coming back, never punish absence" (mentor point 5).

## 9. Order of work

1. `StarRules` + `StarsOptions` + unit tests (red, then green).
2. `WeekResponse.stars` + endpoint tests.
3. Demo weeks in `DemoStory` + its tests.
4. `StarsCard` + `WeekScreen` + Vitest.
5. P4 star line + Vitest.
6. Strings in five languages.
7. Docs (§8), `./scripts/verify.sh`, screenshots and axe, PR (Conventional Commit `feat(web,api): attendance stars on Week and Done (T-63)`).

## 10. Acceptance

- [ ] Weekly star exactly as in §2.1; catch-up star as in §2.2; totals never go down.
- [ ] No text anywhere says a star was lost or missed; no ranking; children without a star are never listed or counted.
- [ ] Families see only the P4 line, never numbers or other children.
- [ ] No LLM or speech call, no migration, no new message to families.
- [ ] Card in every T6 state (§3), including offline and "No absences this week".
- [ ] Every new string in all five locale files.
- [ ] 360 px screenshots (light and dark) and axe 0 on T6 and P4.
- [ ] Existing Vitest and e2e specs pass unchanged; `./scripts/verify.sh` green.
