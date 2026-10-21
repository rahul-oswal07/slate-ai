# Design handoffs

One file per screen or component: `docs/design/<screen>.md`, opened as a PR labelled `design` (see `plan.md` §4). Rahul builds against its acceptance checklist; Monika approves the result from 360 px screenshots or the preview URL.

Before building, copy the design card below into the file and fill in every field. A field that doesn't apply says "none" and why, so nothing is skipped by accident.

## Cards

Development default language is **English** (D-32); Kannada, Telugu and Tamil come from each card's copy table through i18next. The screenshots show the Kannada/Telugu + English captions view; the prototype now opens in English. Start with [`journey.md`](journey.md) (end-to-end journey, screen map, open gates, spec follow-ups). Clickable source of truth: [`prototype/index.html`](prototype/index.html) (open in a browser). Review with finding IDs: [`ux-review.md`](ux-review.md). Screenshots (360 px, light and dark): [`screenshots/`](screenshots/).

| Card | Screen | Task |
|---|---|---|
| [T0-setup.md](T0-setup.md) | Teacher set-up: greeting, evening template | T-44 |
| [T1-today.md](T1-today.md) | Today: attendance from SATS, voice note, tonight, since yesterday, nav | T-12 |
| [T2-lesson-confirm.md](T2-lesson-confirm.md) | Lesson confirm + pick-lesson sheet | T-07, T-12 |
| [T3-approve.md](T3-approve.md) | Approve catch-ups: list, detail, withdraw | T-14 |
| [T4-missed-lessons.md](T4-missed-lessons.md) | Missed lessons (ledger), child history, delete | T-13, T-19 |
| [T5-back-today.md](T5-back-today.md) | Return-day buddy card, brought homework | T-16 |
| [T6-week.md](T6-week.md) | Return speed, reasons, pacing nudge | T-46, T-16 |
| [P0-messages-reason.md](P0-messages-reason.md) | Parent chat: evening message, catch-up link, reason, stopped | T-44, T-45 |
| [P1-listen.md](P1-listen.md) | Listen: greeting, disclosure, lesson | T-15 |
| [P2-activity.md](P2-activity.md) | Activity at home + textbook page | T-15 |
| [P3-answer.md](P3-answer.md) | Picture answer (scored by code) | T-15 |
| [P4-done.md](P4-done.md) | Done; stop / restart messages (defined here) | T-15 |

Reference: tokens and usage rules in [`tokens.md`](tokens.md); copy rules in `spec.md` §3.3–3.4; screens and states in `spec.md` §3.1.

---

## Design card template

```markdown
# <Screen ID> <Screen name>

Spec: FR-__ · Tier: MVP / stretch · Figma: <link> · PNG: <exported frame>

## 1. User voice
- Says: "<what the user would say, in their words>"
- Unsaid: <the worry they won't say out loud, e.g. being monitored, being blamed, cost>

## 2. Who does what
- Human: <what the teacher / parent / child does>
- AI: <what Claude does> · Mode: automation / augmentation / none
- Code (no AI): <deterministic parts: counts, scoring, scheduling>

## 3. Always / never
- Always: <e.g. show why the AI kept or dropped a lesson>
- Never: <e.g. send without approval; show a score to a parent; use a child's name in audio>
- Uncertainty: <threshold and what happens below it, e.g. confidence < 0.7 → teacher picks from list>
- If it goes wrong: <who is harmed and how the design limits it>

## 4. States (draw every one)
| State | What the user sees | Copy (en) |
|---|---|---|
| Default | | |
| Empty | | |
| Loading | | |
| Offline | | |
| Error | | |
| Permission denied (mic/camera) | | |
| <screen-specific, e.g. low confidence, approved, sent> | | |

## 5. Copy
| Key | kn | te | ta | en | Reviewed by (native speaker) |
|---|---|---|---|---|---|
| | | | | | |

Follow `spec.md` §3.4. Unreviewed Indic copy is marked "needs review".

## 6. Tokens and sizes
Colours, type and targets used (from `tokens.md`), e.g. `bg-surface`, `text-body`, `min-h-target-primary`.

## 7. Acceptance checklist
- [ ] Every state above is built and reachable
- [ ] One primary action; works with one thumb at 360 px
- [ ] Status shown as icon + word, never colour alone
- [ ] Targets ≥ 48 px (picture choices 120 px); 8 px apart
- [ ] AI output carries the "AI-assisted" label
- [ ] Every Indic text has a `lang` attribute; strings exist in kn, en, te, ta
- [ ] axe: 0 serious/critical, in light and dark
- [ ] <screen-specific checks>
```
