# T-136 · Try again on a catch-up cut off mid-writing

| | |
|---|---|
| Status | Done (#288) |
| Issue | #287 |
| Owner | R |
| Screens | T3-approve |
| Source | Rahul, 16 Nov 2026 (checking catch-ups); bug; XS |

## What

The writing queue for catch-ups is in memory. When the server restarts or is deployed while catch-ups are being written, their rows stay `generating`. After 5 minutes the list showed them as failed (`error:stale`) with **Try again**, but Try again answered **409**: the store accepted only ready, needs review or failed by stored status. The single catch-up view also still said "generating". So a cut-off catch-up could never be retried.

Now one rule, `Features/CatchUps/StaleGenerating.cs`, is used everywhere:
- A package still `generating` 5 minutes after its last update is stale.
- The list and `GET /api/packages/{id}` show a stale package as failed with `error:stale`.
- Try again (`CatchUpStore.StartRegenerateAsync`) accepts ready, needs review, failed **or stale**. A package still being written within the 5 minutes is refused as before.

Today's message and lesson audio already allow Try again on a stuck row. They now read the same 5 minutes from `StaleGenerating.After`.

## Acceptance criteria

- A catch-up stuck in `generating` for over 5 minutes shows as failed in the list and on its own screen, and Try again starts a new version (202).
- Within 5 minutes, Try again is still refused (409).

## How it's tested

- `StaleGeneratingTests` (the rule).
- `CatchUpApprovalEndpointTests.A_catch_up_cut_off_while_being_written_shows_as_failed_and_try_again_takes_it`. Written first, it failed: the view said "generating".
- `A_catch_up_still_being_written_cannot_be_started_again`.
- A local run against Postgres on the 6 stuck catch-ups of the week of 9 Nov.

## Notes

- 16 Nov 2026: found while checking catch-ups end to end locally. Last week's 6 catch-ups had been cut off when the local API was restarted at 19:31. The fake store now uses the same rule as the real one.
