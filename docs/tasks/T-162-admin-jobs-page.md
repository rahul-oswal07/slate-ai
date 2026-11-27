# T-162 · Jobs page: what the 6 pm job did and Run now

| | |
|---|---|
| Status | Proposed |
| Issue | #340 |
| Owner | R |
| Screens | admin, delivery |
| Source | Rahul, 27 Nov 2026 (admin pages list); FR-20, T-85, T-111 (holds), T-125; needs T-148; M |

## What

The evening send worker (`EveningSendWorker`) runs the 6 pm job, the weekly star and the delivery dispatcher, and the textbook import runs in the background; none of it can be seen. Add `/admin/jobs`:

- each job with last run, outcome (sent, held, skipped, failed counts) and next run;
- for tonight, per class: sent, or held and why (today's message being written, ready and not approved, lesson clip waiting; T-85, T-111) and until when;
- **Run now** per job, which runs the same code with the same rules (18:00 IST, approvals, consent).

## Acceptance criteria

- Run now never sends anything the 6 pm job wouldn't (guardrail 3 and the send filter hold); a second Run now sends nothing twice (unique keys).
- Times shown in IST.
- Write endpoints take `clientRequestId` and are idempotent.
- All new strings in en, kn, te, ta, hi.

## How it's tested

- API tests with fakes: holds and reasons, Run now twice sends once.
- Vitest for the page.

## Notes

- 27 Nov 2026: proposed.
