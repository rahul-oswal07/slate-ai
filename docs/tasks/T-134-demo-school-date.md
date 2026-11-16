# T-134 · Go back to any day or week in the demo class, so the app acts as if that day is today

| | |
|---|---|
| Status | Done (#286) |
| Issue | #284 |
| Owner | R |
| Screens | demo, T1-today, T4-missed-lessons, T5-back-today, T6-week, app-shell, admin |
| Source | Rahul, 16 Nov 2026; D-120; L |

## What

To make test data from the demo, the demo teacher picks a past day and the app works as if that day is today (D-120). The controls sit at each screen's date (`app/SchoolDateNav.tsx`), with no separate card:
- **Today:** `◀ Today, Thursday 12 November ▶`, one day per step. Tapping the date opens the calendar.
- **Missed and Back today:** `◀ Week of 9 Nov ▶` under the title. **Week:** the same around its date range. A week per step, and tapping opens the calendar.
- While a day is chosen, a small **Back to today** sits beside it.
- Lesson and Approve have no control: they follow the chosen day.

**Web.** `features/today/schoolDate.ts` keeps the day in `localStorage` (shared with `/demo`'s teacher frame). `api()` sends it as `X-School-Date`, outbox items keep the day they were made on, every screen uses `useSchoolToday()` instead of `istToday()`, and changing the day resets every cached query.

**Server.** `Features/Demo/SchoolDate.cs`: the `SchoolDateClock` wraps the real clock, a middleware applies the day, and `SchoolClock.RealNow` covers the AI log and link expiry.

**`/demo`** shows the chosen day, and Reset's dialog says days made this way are removed too.

**Admin switch, off until switched on.** `/admin/demo` turns the feature on and off, one switch per server. It is stored in the new `AppSettings` table (`demo.school-date`) and cached in memory (`SchoolDateSwitch`). While it is off, there is no bar, the web forgets any chosen day, and the server ignores the header. `/api/me` gives `demo.schoolDate`.

## Acceptance criteria

- As the demo teacher, choosing 12 Nov makes Today load 12 Nov's register and lesson. Confirm, today's message, approve and Send now all work for 12 Nov. Missed and Week then show the week of 10 Nov.
- Back to today returns to the real day. Another class sees no arrows and the server ignores the header for it.
- The arrows appear only after the switch on `/admin/demo` is on. Switching it off removes them and the chosen day at the next load, and the server ignores the header from then on.
- Under the hood still lists AI calls made while acting as a past day. Family links last 14 real days.

## How it's tested

- API: `SchoolDateTests` covers the clock, the rule (demo class, demo on, past day within 120 days, no admin, no Reset), the HTTP effect on the attendance future-date guard plus the echo, another class unaffected, real-time AI log, and catch-up sent that day with a 14-real-day link. With the rule broken on purpose, 4 of them fail.
- Web: `schoolDate.test.ts` (store, steps, clamp, header), `outbox.test.ts` (a queued write keeps its day), `SchoolDateNav.test.tsx` (day and week modes, calendar, Back to today, hidden outside the demo class), and `TodayScreen.test.tsx` (loads the chosen day with the header).
- Switch: `AdminDemoEndpointTests` (off by default, on/off, replay, 400, 409 when the demo is off, no key), `SchoolDateTests` (off → ignored), `DemoShellEndpointTests` (`/api/me` flag), `AdminDemoScreen.test.tsx`, the bar and Today hidden or real while off, and a Db test for the new table (`StartupDbTests`).
- No Playwright (CLAUDE.md). A local run with 360 px screenshots and axe.

## Notes

- 16 Nov 2026: built. Changed from the plan: instead of adding the date to the ledger and catch-up query keys, which are set and invalidated in many places, choosing a day resets every cached query except `me`. The server tests were written before the code but couldn't compile without it, so they were checked by breaking the rule on purpose afterwards. One full web run had a single failing test in `ConfirmLesson.test.tsx` (T-128 tag), which passed alone 4 times and in two more full runs. The kn, te, ta and hi strings are drafts for Monika.
- 16 Nov 2026: admin switch added on Rahul's request, off until switched on: `AppSettings` table and migration, `/admin/demo`, `demo.schoolDate` on `/api/me`. The switch tests were checked by ignoring the switch on purpose (2 fail). The Db suite now has 21 tests.
- 16 Nov 2026: on Rahul's request, the separate card (`SchoolDateBar`) became inline controls at each screen's date (`SchoolDateNav`): arrows beside Today's date, tapping the date opens the native calendar (`showPicker`, focus as fallback), and `◀ Week of … ▶` under the Missed and Back today titles and around Week's range. Back to today sits beside the date. Lesson and Approve have none and follow the chosen day. Strings `region`, `today`, `pick` and `acting` were dropped, and `choose` was added.
