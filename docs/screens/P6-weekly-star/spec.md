# P6 Weekly star at home · spec

The family's page for the weekly star: a message, then a star badge.

| | |
|---|---|
| Route | `/p/:token` (a weekly star link, `kind: "weeklyStar"`) |
| Web | `features/parent/WeeklyStarPages.tsx` |
| API | `Features/WeeklyStar/`, `Features/Parent/` |
| Design card (Monika) | none yet |
| Status | [`status.md`](status.md) |

## Purpose and states

On Saturday at 6 pm, once the register is in, the family of each child present on every school day of the week gets `weekly_star_v1` on WhatsApp. Its link opens this page. **No design card yet**; `P6` is a new screen ID (16 Nov, T-125). The flow and the copy are Monika's (T-125 §4–§5).

1. **Message:** "{child} came to school every day this week…" (text only, no teacher voice), and one button: **Open to see your star badge**.
2. **Badge card** (`?step=badge`, so the phone's Back returns): the star badge with its text, and one confetti burst of about 2 s when the card opens, then it rests. No movement with reduced motion. No numbers.

As on every family page: the stop link, the Back + title row (T-116) and the teacher line. A stopped family sees the stopped page; an old link (14 days) gets 410.

## Requirements

FR-24 ([T6](../T6-week/spec.md)) as changed by D-118. Both texts are the wording the teacher approved in setup (step 3) with only `{child}` filled by code (guardrail 3, 4).

## Decisions cited

D-77 (the Done page's star and confetti), D-108, [D-118](../../decisions/D-118-weekly-star-every-day.md).

D-01 to D-99 are in [`../../decisions/D-01-to-D-99.md`](../../decisions/D-01-to-D-99.md); later ones are one file each in [`../../decisions/`](../../decisions/README.md).
