# T-137 · A long A4 verdict fails the whole catch-up

| | |
|---|---|
| Status | Done (#290) |
| Issue | #289 |
| Owner | R |
| Screens | T3-approve, ai-pipeline |
| Source | Rahul, 16 Nov 2026 (checking catch-ups); bug; XS |

## What

When the content check (A4) rejects a script, `ContentCheck` logs `"fail:" + the reasons` as the AI call's verdict. With several reasons that is longer than `AiCalls.Verdict` (varchar 100). Postgres refuses the update (22001), and the exception fails the whole catch-up (`error:exception`) after Claude and Sarvam have already run. Seen locally on the Kannada catch-up for `M3.ADD.CARRY`, week of 9 Nov.

`AiCallStore.SetVerdictAsync` now fits the verdict to its column with `AiCallText.Verdict`, as `AiCallText.Error` already did for errors. Every AI step's verdict goes through it, so logging can never fail the work it logs.

## Acceptance criteria

- A verdict longer than 100 characters is stored cut to 100, and the catch-up carries on: A4's retry, needs review or ready.

## How it's tested

- `AiCallTextTests` (the fit).
- Only Postgres enforces the length, and the fakes don't. So the store change is checked by repeating the failure on a local Postgres run, not by a new Db test (CLAUDE.md: ask first).

## Notes

- 16 Nov 2026: found right after T-136 while checking last week's catch-ups locally. The other 5 catch-ups passed every check.
