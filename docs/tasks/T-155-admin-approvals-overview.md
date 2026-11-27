# T-155 · Approvals across classes, read-only

| | |
|---|---|
| Status | Proposed |
| Issue | #333 |
| Owner | R |
| Screens | admin, P0-evening-message, P5-todays-message, P6-weekly-star, T3-approve |
| Source | Rahul, 27 Nov 2026 (admin pages list); guardrail 3; `docs/status.md` Next 1 (check the evening message is approved on prod); needs T-148; S–M |

## What

Nothing reaches a family until the teacher approves it, and today the only way to check is to sign in as her. Add `/admin/approvals`: one row per class with

- the evening message template per language: draft, or approved at which version, by whom and when;
- the weekly star wording: approved at the current version or not;
- today's message today: none, being written, ready and not approved, approved at its current version, sent;
- this week's catch-ups waiting for approval.

Read-only: approving stays the teacher's (guardrail 3). Each row says what she has to do, in a line.

## Acceptance criteria

- No approve, edit or send action on this page or its API.
- An approval made at an older version shows as not approved.
- All new strings in en, kn, te, ta, hi.

## How it's tested

- API tests for each state with fakes; Vitest for the page.

## Notes

- 27 Nov 2026: proposed.
