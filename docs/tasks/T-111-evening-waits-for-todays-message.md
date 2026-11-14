# T-111 · The evening job waits for today's message before sending the plain one

| | |
|---|---|
| Status | Done |
| Issue | #216 · PR #217 |
| Owner | R |
| Screens | delivery, T3-approve, P5-todays-message |
| Source | Bug found 14 Nov on a local server after 6 pm IST; D-100 (Rahul, 15 Nov) |

## What

After 6 pm the minute job sent the plain evening message 16 s after she confirmed the lesson, 3 s before she tapped Approve on today's message. The family got the plain text and Approve had nothing to attach to.

`EveningSendWorker` held a class for a lesson clip that awaits her approval (T-82, T-85) but not for a draft of today's message. Now it holds for both, by the same rule (D-100):

- while today's message is being written;
- while it is ready and not approved, for `EveningMessages:TodaysMessageWait` (5 minutes) from when it became ready, or from 6 pm if it was ready earlier. Every edit starts the wait again.

When the wait runs out the plain message goes, as before.

## Acceptance criteria

- A today's message approved within 5 minutes of being ready goes with that evening's message, also when the lesson is confirmed after 6 pm.
- An unapproved today's message never holds the plain message for more than 5 minutes after it was ready (or after 6 pm).
- A failed or stuck draft, or one families already have, doesn't hold it.
- Guardrail 3 is unchanged: only a message approved at its current version is attached.

## How it's tested

- `TodaysMessageWaitTests`: the pure rule (none, being written, stale, ready inside and past the wait, ready before 6 pm, an edit, approved, failed, sent).
- `EveningEndpointTests`, worker tests: the 14 Nov case (nothing sent while it is written and read, then sent with today's message once approved); plain message at ready + 5 minutes; ready at 1 pm waits until 18:05; failed or stuck doesn't wait.
- `./scripts/verify.sh`. No Db suite: no store, query or migration changed.

## Notes

- 15 Nov: built on `fix/T-111-evening-waits-for-todays-message`. The four new worker tests failed before the change (the plain message went on the first tick) and pass after. The rule is `Features/TodaysMessage/TodaysMessageWait.cs`, a copy in shape of `LessonAudioWait`; the worker reads the day's row with the existing `ITodaysMessageStore.GetAsync`. No migration, no prompt change, no UI or string change.
- 15 Nov, not changed: the teacher's own Send and the demo's "Send messages now" (as in T-82); Today's card still says "Until you approve it, families get the plain evening message." without the 5 minutes (wording for Monika; T-86 plans a status line).
- 15 Nov, not run: a live check after 6 pm IST on a local server. The worker tests drive the clock instead.
