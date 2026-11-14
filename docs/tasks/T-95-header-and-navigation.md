# T-95 · Header and navigation

| | |
|---|---|
| Status | Proposed |
| Issue | none yet |
| Owner | R |
| Screens | app-shell |
| Source | G1, G3, D-96 · Finale Should · size S–M |

## What

**Header and navigation:** small language switch in the header on every screen (Approve too; now its own task, T-107); Log out moves to Settings; the gear gets a "Settings" label; bottom nav Today · Approve · Children · Week, with Back today as a filter in Children

## Acceptance criteria

Same header on every screen; existing routes still work

## How it's tested

Vitest; existing e2e specs pass; 360 px screenshots

## Notes

- 15 Nov: the header part is built by T-109 (D-101): one row with a profile menu, instead of "Log out moves to Settings; the gear gets a Settings label". What is left here is the bottom nav: Today · Approve · Children · Week, with Back today as a filter in Children.
- 15 Nov: moved here from `tasks.md` (T-110).
- 14 Nov: the language switch is its own task and is built (T-107, #206). T-109 (#210, proposed) replaces the header part; the bottom nav part is unchanged.
