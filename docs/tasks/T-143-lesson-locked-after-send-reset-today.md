# T-143 · The lesson can't change once tonight's message has gone; Reset today in Settings

| | |
|---|---|
| Status | Done (#303) |
| Issue | #302 |
| Owner | R |
| Screens | T1-today, T10-settings, T3-approve |
| Source | Rahul, 17 Nov 2026 ("once the voice note is generated and the lesson is changed, the script is not regenerated"; "a reset button inside settings"); D-124; S |

## What

**The bug.** On 17 Nov (local), Rahul approved today's message, which the demo sends at once (D-94), and then changed the lesson. No new draft came.
- That is by design: a message that has gone is never redrafted, and each child gets one evening message a day.
- But the change itself was accepted silently. The day's lesson said one thing while families had the message about another, and nothing on screen said so.
- Voice notes already had this rule (D-104); picking from the list didn't.

**Fixes:**
- **Lesson locked once tonight's message has gone** (D-124).
  - `PUT …/lessons/{date}` answers **409** with the day as it is when any evening message for the day has gone and the lesson would change. The same lesson is still fine (replays, the outbox).
  - `GET …/lessons/{date}` says `sentToFamilies`.
  - Today then shows the confirmed card with "Tonight's message about this lesson has gone to families, so it can't be changed", without Change or the mic. That holds even when a plain evening message went because today's message was never approved.
- **A refused save doesn't stay on screen.** The outbox moves a refused write to `failed`. The lesson query is now refetched then too, so a refused change goes back to the confirmed lesson instead of looking pending forever.
- **A failed draft about an old lesson no longer blocks the new one.**
  - Before: if she changed the lesson while the first draft was being written and that draft then failed, the failure was stored on the restarted row. The new job then skipped it, so the new lesson's message showed "failed".
  - Now the generator records a failure only while the row is still generating the same lesson: the check a successful draft already made.
- **Reset today in Settings**, demo class only (`me.demo`).
  - "Reset today" → "Yes, reset today" calls `POST /api/demo/reset-today`.
  - For the school day (today, or the past day she is acting as, T-134) it deletes the lesson, today's message, lesson audio, tonight's messages with their deliveries and answers, absence reasons, buddy ticks and her register changes.
  - It puts the register back as the simulated SATS sent it.
  - Earlier days, children, catch-ups and weekly stars stay.
  - Families who already got tonight's WhatsApp may get it again after a re-run (the confirm says so).

## Acceptance criteria

- After tonight's message has gone, another lesson is refused (409) and the same lesson is accepted. Today shows the reason and no Change.
- A lesson changed while a failing first draft is in flight still gets its own draft.
- In the demo class, Reset today clears only the day, and Today is ready again with the SATS register. A real class has no Reset today; the endpoint is 404 for any class but the demo class and doesn't exist with the demo off.

## How it's tested

- API:
  - `LessonEndpointTests.Once_tonights_message_has_gone_another_lesson_is_refused_and_the_same_one_is_still_fine` and `The_day_says_when_tonights_message_has_gone`;
  - `TodaysMessageGeneratorTests.A_lesson_changed_while_the_first_draft_was_failing_still_gets_its_own_draft`;
  - `DemoShellEndpointTests` (reset today for the demo class with the register, 404 for another class, not mapped with the demo off).
  - All three bug tests were written first and failed before the fix.
- Web:
  - `LessonCardWindow.test.tsx`: the locked note; the plain message gone without approval; a 409 going back to the confirmed lesson. That last one fails without the refetch.
  - `SettingsScreen.test.tsx`: no Reset for a real class; confirm, cancel, reset; failure.
- Local run (fake AI, throwaway database):
  - send → change refused (409) → same lesson 200;
  - Reset today → 0 evening messages, deliveries, today's message and lessons for the day, the register back (36 rows, SATS at 9:00, one child away), 5 earlier lessons kept, a replay changes nothing;
  - a new lesson saves.
- Screenshots: `docs/design/screenshots/today-lesson-locked-360-light.png`, `settings-reset-today-360-light.png`.

## Notes

- 17 Nov 2026: built.
- 17 Nov 2026: Db test added with Rahul's OK: `DemoResetDbTests.Reset_today_clears_only_the_day_with_its_cascades_and_puts_the_sats_register_back` (the day's deletes and their cascades, the 409 once sent, yesterday kept, the SATS register back, a replay changing nothing).
