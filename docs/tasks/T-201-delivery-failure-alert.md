# T-201 · An alert when an evening's sends fail

| | |
|---|---|
| Status | Proposed |
| Issue | #379 |
| Owner | R |
| Screens | delivery, admin |
| Source | Rahul, 27 Nov 2026 (delivery list); works with T-162 (jobs) and T-200 (statuses); S–M |

## What

If WhatsApp fails one evening (expired token, template paused, Meta down), nobody knows until a teacher hears from a family. Send an alert to the team when, for a server's evening:

- more than a set share of messages failed, or
- nothing has gone by 18:30 IST on a day with absent children, or
- the access token is refused.

The alert goes by email or push (decide which), with counts and the first error, and never with names or numbers.

## Acceptance criteria

- At most one alert per kind per evening per server.
- Thresholds and recipients from config; secrets from the environment.
- The jobs page (T-162) shows the alert that went.

## How it's tested

- Pure tests for "should alert"; unit test with a fake sender.

## Notes

- 27 Nov 2026: proposed.
