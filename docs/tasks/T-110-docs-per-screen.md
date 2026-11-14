# T-110 · Docs per screen, one file per task and per decision

| | |
|---|---|
| Status | Built, not merged |
| Issue | not created yet |
| Owner | R |
| Screens | none (docs only) |
| Source | Rahul, 15 Nov |

## What

Almost every docs edit landed in three files, so two people working at once collided in them: `status.md` (105 commits since September; every session prepended to Recent changes at the same line), `spec.md` (77; every decision appended at the end of the §8 table) and `tasks.md` (49; one long table row per task with "Built (date)…" notes inside the row).

Now:

- [`docs/README.md`](../README.md) is the master map: every screen and area in one table.
- Each screen has `docs/screens/<screen>/spec.md` and `status.md`; parts that aren't a screen have the same two files in `docs/areas/`.
- Each live task is one file here, with its acceptance criteria and dated notes.
- Each new decision is one file in `docs/decisions/`; D-01 to D-99 are a frozen log there.
- History is in `docs/archive/`, as it was written.
- `docs/status.md` and `docs/spec.md` keep their paths; `spec.md` keeps its section numbers.

## Acceptance criteria

- No line of the old `status.md`, `spec.md` or `tasks.md` is lost, except the ones rewritten on purpose (listed in the PR).
- Every FR, NFR, D and T ID is defined once.
- Every `docs/` link in `README.md` opens on GitHub.
- Two branches that each add a note to a different task and a different screen merge with no conflict.

## How it's tested

`scripts/check-docs.sh` (duplicate task and decision IDs, broken relative links), called from `./scripts/verify.sh`.

## Notes

- 15 Nov: built on `docs/T-110-docs-per-screen` from `main` at #212.
