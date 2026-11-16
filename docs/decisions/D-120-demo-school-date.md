# D-120 · The demo teacher can act as a past school day, per browser

| | |
|---|---|
| Status | Accepted |
| Owner | Rahul, 16 Nov 2026 |
| Gate | T-134 |
| Screens | demo, T1-today, T4-missed-lessons, T6-week, app-shell |

## Decision

Rahul wants to make test data from the demo by going back to a past day (e.g. 12 Nov) and using the app as if it were that day. D-88 (T-75) once fixed one date for the whole server with `Demo:Today`, and T-105 removed it on 14 Nov. This brings a date back, chosen in the app, for one browser.

## Recommendation

- **One date for the whole app.** In the demo class, arrows at each screen's date pick the day, and tapping the date opens the calendar (no separate card). Today steps a day. Missed, Back today and Week step a week, which moves the same day by 7. Lesson and Approve follow the chosen day. Everything acts as if that day is today: register, lesson, today's message, approve and send, catch-ups, ledger, week, weekly star.
- **Per browser.** The day is kept in `localStorage` and sent as `X-School-Date` on every API call. Others, such as judges on staging, keep the real date.
- **Server.** A middleware applies the day only for the demo class (signed-in class = the seed's) with `Demo:Enabled`, for a valid day before the real IST today and at most 120 days back. It never applies on `/api/admin/*` or to `POST /api/demo/reset`, which always rebuilds the story on the real date. In that request, `TimeProvider` gives that day at the real IST time of day (as D-88's `DemoClock`), so every clock-based rule follows. Background jobs have no request and stay on the real day.
- **Real time where it must be.** The AI/speech log (`AiCall.CreatedAt`, so Under the hood lists the calls) and how long a family's link lasts (14 days from now, not from the chosen day) stay on real time. Sent and approved times take the chosen day, which the ledger's history needs.
- **WhatsApp sends as on a real day** (Rahul's choice).
- A write queued offline keeps the day it was made on.
- **Behind an admin switch, off by default** (Rahul, 16 Nov). One switch per server on `/admin/demo`, stored in `AppSettings` (`demo.school-date`). While it is off there is no bar, the web forgets any chosen day, and the server ignores `X-School-Date`. `/api/me` tells the web through `demo.schoolDate`.

Brings back D-88's idea for this use, per browser and demo class only, chosen in the app. T-105's removal of the fixed server date still holds.

## Notes

- 16 Nov 2026: asked for by Rahul. He chose one date for the whole app, WhatsApp too, and demo class only on any server. Built in T-134.
- 16 Nov 2026: Rahul asked for an admin switch to turn it on and off, and chose off until switched on.
