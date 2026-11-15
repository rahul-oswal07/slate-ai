# T-117 · Approve and send now

| | |
|---|---|
| Status | Done |
| Issue | #238 · PR #239 |
| Owner | R |
| Screens | T3-approve, demo |
| Source | Rahul, 15 Nov (testing: "the message is not sent immediately after approval"); [D-109](../decisions/D-109-approve-and-send-now.md); replaces `Demo:SendOnApprove` (T-93, D-94); size M |

## What

Approving used to only schedule a message for the 6 pm job; only `Demo:SendOnApprove` sent at once, and it was off on staging. The today's message button already read "Approve and send" but didn't send. Now, after the tick, the teacher has two buttons on **both** Approve → Today's message and a catch-up's review:

- **Approve and send now** (primary): the approve request carries `send: true`.
  - **Today's message:** the class's evening send runs at once, so due families get the evening message with today's message attached. The evening send's own rules still hold (evening wording approved, lesson confirmed, attendance in, once per child per day). If they block it, the message stays approved, the announcement says Today shows what's missing, and the 6 pm job sends it once that's done.
  - **Catch-up:** sent to consenting families at once, whatever the time.
- **Approve, send after 6 pm** (secondary): `send: false`, the old behaviour.

Guardrail 3 is unchanged: both senders re-check "approved at this version" (`EveningSender` via `GetApprovedTodaysMessageAsync`, `CatchUpStore.SendAsync`). `Demo:SendOnApprove`, `DemoSendOnApprove` and the `sendOnApprove` demo flag on `/api/me` are gone; the shared step is `Features/Delivery/SendNow.cs` (send, then deliver to families' phones).

## Acceptance criteria

- Both buttons are disabled until she ticks the version she read.
- Approve and send now, before 6 pm, sends today's message with the evening message to every due family, and the catch-up to consenting families; Today / the catch-up show it sent.
- Approve, send after 6 pm leaves it approved and unsent until the 6 pm job.
- Send now with attendance not in sends nothing and leaves it approved.
- An unapproved or older version is never sent (409).

## How it's tested

- API: `TodaysMessageEndpointTests.Approve_and_send_now_sends_at_once_and_approve_alone_waits_for_6_pm`, `Approve_and_send_now_without_attendance_sends_nothing_and_stays_approved`; `CatchUpApprovalEndpointTests.Approve_and_send_now_*`, `Approve_alone_leaves_the_catch_up_for_6_pm`; `DemoShellEndpointTests.Me_gives_the_demo_teacher_the_demo_flags`.
- Vitest: `TodaysMessageTab.test.tsx` and `PackageScreen.test.tsx` (both buttons need the tick; each posts the right `send`; the right announcement).
- A local run with screenshots at 360 px for the PR.

## Notes

- 15 Nov: built. Kannada, Telugu, Tamil and Hindi button copy are Claude's drafts for Monika to check (`todays.approve`, `todays.approveLater`, `todays.notSentYet`, `approve.pkg.approve`, `approve.pkg.approveLater`, `approve.pkg.approveHint`, `demo.guide.approve`). Hindi is `hi.json`, which CLAUDE.md doesn't list. Not built: a "Send now" for a message already approved for 6 pm (she would have to wait for 6 pm or use Today's Send messages now in the demo).
