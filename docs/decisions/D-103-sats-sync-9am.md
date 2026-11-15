# D-103 · SATS attendance sync moves to 9:00

| | |
|---|---|
| Status | Accepted |
| Owner | Monika, 15 Nov 2026 |
| Gate | none |
| Screens | T1-today |

## Decision

[D-45](D-01-to-D-99.md) set the daily SATS attendance sync at **12:00** with a retry at **2:00 pm**. The register in practice is available in the morning, so the sync should run at **9:00** instead. This supersedes the sync time in D-45 only; everything else D-45 decided is unchanged.

## Recommendation

- The attendance-sync job runs at **9:00 IST** (`Attendance:Sats:ArrivesAt`, default `09:00`).
- The late retry stays at **2:00 pm** (`Attendance:Sats:RetryAt`, `14:00`) — still after the sync and before the 6 pm evening send, so the daily loop is unchanged.
- Manual marking any time, nothing marked absent automatically, and the `/demo` "Bring in today's attendance" button are all unchanged (D-45).

## Notes

- 15 Nov 2026: changed at the team's request. Code default `SatsOptions.ArrivesAt` set to `09:00`; the waiting copy fills `{syncTime}`/`{retryTime}` from these options, so no string changed. The frozen D-45 row still reads 12:00 and is not edited (frozen log); this file is the current value.
