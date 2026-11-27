# T-97 · Learner level colours

| | |
|---|---|
| Status | Done (#323) |
| Issue | #316 |
| Owner | R |
| Screens | T1-today, T4-missed-lessons, T5-back-today, T6-week, app-shell |
| Source | G1, D-95 · Finale Should · size S |

## What

**Learner level colours:** four tokens with icon and word (D-95), used the same way on Today, Children, the child view, Back today and Week

## Acceptance criteria

Contrast AAA in light and dark; colour is never the only signal

## How it's tested

`tokens.test.ts`; Vitest

## Notes

- 27 Nov: three levels, not four (D-108): `level-got-it`, `level-needs-help`, `level-not-answered`, each a colour and a fill in light and dark, AAA on the fill and on canvas, surface and panel (`tokens.test.ts`). `LevelTag` (icon + word in the level colour, border in the same) is used on Today's answers, the Missed table, the child view's lesson chips, above the Back today card (never on it or printed) and in Week's headline. Where an away child's level comes from is D-130. Vitest: `LevelTag.test.tsx`, `levels.test.ts`, screen tests. Left: 360 px screenshots and axe (not run here); Monika to add the tokens to `docs/design/tokens.md` and the level look to the T1, T4, T5 and T6 cards; kn, te, ta and hi of "Catch-up at home:" are drafts for her.
- 15 Nov: moved here from `tasks.md` (T-110).
