# T6 Week (return speed, reasons, pacing nudge)

Spec: FR-22, FR-21 (reason categories), FR-13, NFR-08 · Tier: MVP · Task: T-46 (return speed), T-16 (pacing nudge), T-45 (reason data) · Prototype: prototype/index.html (step 12) · PNG: screenshots/T6-week-nudge-360-light.png

Decisions: D-20 (return speed is the primary measure), D-23 (reason categories only), D-24 (weekly return-speed card, teacher only). Review: B-05, B-06, B-09, B-28, B-30.

## 1. User voice
- Says: "Are children coming back quicker than last week? Why were they away? Should I repeat borrowing before word problems?"
- Unsaid: "Is this going to the BEO?" "Will these numbers be used against me?" "I don't want a chart I have to work out."

## 2. Who does what
- Human (teacher): reads the card, decides "Plan recap" or "Not this week", can change the decision.
- AI · Mode: augmentation.
  - A5 pacing wording (Claude Haiku): one Kannada sentence with slots `{n}`, `{total}`, `{minutes}`; code fills the numbers.
  - A7 (Claude Haiku) only upstream: it turned parents' voice notes into reason categories (see P0). Nothing on this screen calls A7.
- Code (no AI): absence spells from attendance; counts by length (1, 2, 3, 4+ days) this week and last; "back within 2 school days" (= spells of 1 or 2 days); reason category counts and "Not given"; pacing counts from the ledger (children who missed the competency, class size); date range; the reminder for "Plan recap".

## 3. Always / never
- Always: counts, not percentages. Every headline number reconciles with the bars and table under it (B-09). Both weeks shown as numbers (B-06).
- Always: say where numbers come from ("Numbers are counted from attendance") and, when the nudge is shown, that AI writes only the wording.
- Always: reasons are the teacher's own view ("Only for you. Not sent to anyone.").
- Never: export or show this card to officers (FR-22, N8). Never show a child's name next to a reason. Never show health details: "Unwell" is a category only.
- Never: let the LLM produce or change a number (guardrail 4). A5 output that changes a slot or adds a number is rejected → fixed fallback sentence.
- Uncertainty: A5 output fails schema or slot check → use a code template sentence. A7 low confidence upstream already became "Other".
- If it goes wrong: a wrong count misleads her recap decision or makes her distrust the app. Limit: one query, one definition, a reconciliation test.

## 4. States
| State | What the user sees | Copy (en) |
|---|---|---|
| Nudge (default) | Date range; return-speed card with headline "10 of 12", sentence with last week, bars per length with "this week" / "last week" numbers, hidden table; reasons card with counts; pacing panel with AI label, "Plan recap" / "Not this week"; footer. `screenshots/T6-week-nudge-360-light.png`, dark: `screenshots/T6-week-nudge-360-dark.png` | "How quickly children came back" · "10 of 12" · "absent children were back within 2 school days. Last week: 9 of 13." · "6 of 36 children missed subtraction with borrowing. Recap 10 minutes before word problems." |
| No nudge this week | Return speed and reasons as above; pacing panel replaced by an on-track card; footer without the AI sentence. `screenshots/T6-week-no-nudge-360-light.png` | "Most children are on track. No recap needed this week." · "Numbers are counted from attendance." |
| Decided: recap planned (`decided`) | Success banner with Change replaces the two buttons. State chip "Decided (recap planned)" in the prototype. No PNG exported | "We’ll remind you on Monday morning." · "Change" |
| Decided: skipped | Success banner with Change. Reached by tapping "Not this week"; no state chip of its own. No PNG exported | "OK. Skipped this week." · "Change" |
| Empty (first week, no absences) | Not designed. Needs "No absences this week" and no last-week comparison | Open item |
| Loading | Not designed. Skeleton cards | — |
| Offline | Not designed. Recommend last cached week with "Last updated …" (as T4) | Open item |
| Error | Not designed | — |
| Permission denied | none: no mic or camera on this screen | — |

## 5. Copy
| Key | kn | te | ta | en | Reviewed by |
|---|---|---|---|---|---|
| `week.title` | ಈ ವಾರ | — | needs translation | This week | needs review |
| `week.nav` | ವಾರ | — | needs translation | Week | needs review |
| `week.range` | {from}–{to} ಅಕ್ಟೋಬರ್ | — | needs translation | {from}–{to} October | needs review |
| `week.return.title` | ಎಷ್ಟು ಬೇಗ ಮರಳಿದರು | — | needs translation | How quickly children came back | needs review |
| `week.return.headline` | {total} ರಲ್ಲಿ {within} | — | needs translation | {within} of {total} | needs review |
| `week.return.sentence` | ಗೈರಾದ ಮಕ್ಕಳು 2 ಶಾಲಾ ದಿನಗಳೊಳಗೆ ಮರಳಿದರು. ಕಳೆದ ವಾರ: {lastTotal} ರಲ್ಲಿ {lastWithin}. | — | needs translation | absent children were back within 2 school days. Last week: {lastWithin} of {lastTotal}. | needs review |
| `week.return.axis` | ಗೈರು ಅವಧಿ (ದಿನ) | — | needs translation | Days away | needs review |
| `week.return.day` | ದಿನ | — | needs translation | day / days | needs review |
| `week.return.thisWeek` / `.lastWeek` | ಈ ವಾರ / ಕಳೆದ ವಾರ | — | needs translation | this week / last week | needs review |
| `week.return.table.caption` | ಗೈರು ಅವಧಿ | — | needs translation | Days away | needs review |
| `week.return.table.days` | ದಿನ | — | needs translation | Days | needs review |
| `week.reasons.title` | ಕಾರಣಗಳು (ಪೋಷಕರು ತಿಳಿಸಿದ್ದು) | — | needs translation | Reasons (from parents) | needs review |
| `reason.ill` / `.travel` / `.fest` / `.home` / `.other` | ಹುಷಾರಿಲ್ಲ / ಕುಟುಂಬ ಪ್ರಯಾಣ ಅಥವಾ ಕೆಲಸ / ಹಬ್ಬ ಅಥವಾ ಸಮಾರಂಭ / ಮನೆಯಲ್ಲಿ ಅಗತ್ಯ / ಬೇರೆ | — | needs translation | Unwell / Family travel or work / Festival or function / Needed at home / Other | needs review |
| `week.reasons.notGiven` | ತಿಳಿಸಿಲ್ಲ | — | needs translation | Not given | needs review |
| `week.reasons.private` | ನಿಮಗೆ ಮಾತ್ರ. ಯಾರಿಗೂ ಕಳುಹಿಸುವುದಿಲ್ಲ. | — | needs translation | Only for you. Not sent to anyone. | needs review |
| `week.nudge.title` | ಮುಂದಿನ ವಾರದ ಮೊದಲು | — | needs translation | Before next week | needs review |
| `week.nudge.fallback` | 36 ರಲ್ಲಿ {n} ಮಕ್ಕಳು {lesson} ತಪ್ಪಿಸಿಕೊಂಡಿದ್ದಾರೆ. {next} ಮೊದಲು {minutes} ನಿಮಿಷ ಪುನರಾವರ್ತಿಸಿ. | — | needs translation | {n} of {total} children missed {lesson}. Recap {minutes} minutes before {next}. | needs review |
| `week.nudge.plan` | ಪುನರಾವರ್ತನೆ ಯೋಜಿಸಿ | — | needs translation | Plan recap | needs review |
| `week.nudge.skip` | ಈ ವಾರ ಬೇಡ | — | needs translation | Not this week | needs review |
| `week.nudge.planned` | ಸೋಮವಾರ ಬೆಳಿಗ್ಗೆ ನೆನಪಿಸುತ್ತೇವೆ. | — | needs translation | We’ll remind you on Monday morning. | needs review |
| `week.nudge.skipped` | ಸರಿ. ಈ ವಾರ ಬಿಡಲಾಗಿದೆ. | — | needs translation | OK. Skipped this week. | needs review |
| `week.nudge.change` | ಬದಲಿಸಿ | — | needs translation | Change | needs review |
| `week.onTrack` | ಹೆಚ್ಚಿನ ಮಕ್ಕಳು ಸರಿಯಾಗಿದ್ದಾರೆ. ಈ ವಾರ ಪುನರಾವರ್ತನೆ ಬೇಕಿಲ್ಲ. | — | needs translation | Most children are on track. No recap needed this week. | needs review |
| `week.footer` | ಸಂಖ್ಯೆಗಳು ಹಾಜರಾತಿಯಿಂದ ಲೆಕ್ಕ ಹಾಕಲಾಗಿದೆ. | — | needs translation | Numbers are counted from attendance. | needs review |
| `week.footer.ai` | AI ಪದಗಳನ್ನು ಮಾತ್ರ ಬರೆಯುತ್ತದೆ. | — | needs translation | AI writes only the wording. | needs review |
| `ai.label` | AI ನೆರವು | — | needs translation | AI-assisted | needs review |
| `aria.week.region` | ಈ ವಾರ | — | needs translation | This week | needs review |

The nudge sentence shown is A5 output (`PacingNudge.TextKn`) with code-filled slots; `week.nudge.fallback` is used when A5 fails. No announce() strings on this screen in the prototype; add a polite announcement for the recap decision (`week.nudge.planned` / `.skipped`).

## 6. Tokens and sizes
- Cards `bg-surface border border-border rounded-md`; titles `text-title`; headline count `text-display` weight 700; sentences `text-caption text-ink-muted`.
- Bars: this week filled `bg-primary`; last week a second filled bar side by side, not an outline only (B-06), in a different fill with `border` ≥ 3:1. Numbers printed next to each bar. Bars `aria-hidden`; a `sr-only` `<table>` with caption carries the data.
- Reason rows: Lucide icons (thermometer, bus, flame, home, message, help-circle) + word + count; separate icons per category (B-30).
- Pacing panel `bg-panel border border-border`; title `text-primary-strong` (rule 1); AI label `text-primary-strong`. "Plan recap" `bg-primary text-on-primary min-h-target`; "Not this week" ghost `min-h-target`; 8 px apart.
- Decision banner `border-l-primary`, Change text button `min-h-target`.
- Page scrolls as a whole, or the scroll region is focusable with a role and label (B-05).

## 7. Acceptance checklist
- [ ] Every state above is built and reachable
- [ ] One primary action; works with one thumb at 360 px
- [ ] Status shown as icon + word, never colour alone
- [ ] Targets ≥ 48 px; 8 px apart
- [ ] AI output carries the "AI-assisted" label (pacing sentence)
- [ ] Every Indic text has a `lang` attribute; strings exist in kn, en, te, ta
- [ ] axe: 0 serious/critical, in light and dark
- [ ] One query for return speed: `GET /api/classes/{id}/return-speed?week=` returns spells by length for this and last week and `within2` computed from the same spells (FR-22, T-46).
- [ ] xUnit reconciliation test: headline `within == count(1-day) + count(2-day)` and `total == sum(all lengths)`, for both weeks; fixture matches the prototype (this week 7, 3, 1, 1 → 10 of 12; last week 5, 4, 2, 2 → 9 of 13) (B-09).
- [ ] Written definition in code comments and `docs/database.md`: a spell is consecutive school days absent; it belongs to the week it ends in; "back within 2 school days" = spell length ≤ 2. (Rahul to confirm, see Open items.)
- [ ] Reason counts + "Not given" = number of absent children in the week. xUnit.
- [ ] Reasons show categories only; no names, no transcripts (FR-21, D-23).
- [ ] Pacing numbers come from the ledger by code (FR-13); test "nudge numbers = ledger" (T-16). The A5 response cannot change `{n}`, `{total}`, `{minutes}`; a response with other digits is rejected → fallback sentence.
- [ ] "Plan recap" stores the decision and schedules the Monday reminder; the "Decided (recap planned)" banner replaces the buttons; "Change" reopens the choice (B-30). Write accepts `clientRequestId`.
- [ ] Last week shown as numbers and side-by-side bars; screen reader reads the table (B-06).
- [ ] Date range shown ("19–23 October") (B-30).
- [ ] No route, export or API exposes this card outside the teacher's session (FR-22).

## Open items

**Resolved 22 Oct:**
- Nudge shows when 5 or more of the class missed the same lesson this week (D-60).
- No absences / first week: "No absences this week." with no comparison; state chip "No absences" (D-61).
- Offline / loading / error: follow D-68.

**Still open:**
- Definition of a spell that crosses weeks, or is still open on Friday (child not back yet): which week counts it, and is an open spell "not back within 2 days"? Rahul to define before T-46.
- First week (no last week) and "no absences" states are not designed.
- Offline state not designed; recommend cached snapshot with "Last updated …".
- When is "No nudge this week" chosen? FR-13 gives an example but no threshold. Rahul + Monika to set the rule in code.
- The nudge's "{next}" ("word problems") comes from next week's plan; confirm the source (curriculum sequence).
- A5 is not in the task list's AI pipeline (T-08 to T-11); it belongs to T-16. Prompt file and eval cases needed.
