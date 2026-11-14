# App shell and look · spec

Header, bottom nav, language, offline outbox, tokens and themes.

| | |
|---|---|
| Route | every teacher route |
| Web | `app/` (`AppShell`, `BottomNav`, `TeacherLayout`), `components/`, `styles/`, `i18n/`, `offline/` |
| API | `Features/Spa/` |
| Design card (Monika) | [`tokens.md`](../../design/tokens.md) |
| Status | [`status.md`](status.md) |

## Purpose and states

What every screen shares. Copy rules: [`spec.md`](../../spec.md) §3.4; offline behaviour: §6; accessibility targets: NFR-02 and NFR-04; tokens and components: [`design/tokens.md`](../../design/tokens.md).

## Requirements

No FR of its own: NFR-02 (offline), NFR-04 (WCAG 2.2 AA with AAA text contrast, 48 px targets) in [`spec.md`](../../spec.md) §2.

## Decisions cited

D-04, D-07, D-08, D-17, D-35, D-63, D-70, D-74, D-76, D-77.

D-01 to D-99 are in [`../../decisions/D-01-to-D-99.md`](../../decisions/D-01-to-D-99.md); later ones are one file each in [`../../decisions/`](../../decisions/README.md).
