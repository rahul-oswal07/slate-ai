# P4 Done + Stop these messages (shared component)

Spec: FR-10, FR-11, FR-19, NFR-04 · Tier: MVP · Task: T-15 · Prototype: prototype/index.html (steps 9–10 for Done; step 13 for stop) · PNG: screenshots/P4-done-default-360-light.png

Decisions: D-08 (four steps), D-19 (`ParentStoppedAt`, send refuses stopped children), D-20 (no score for the family), D-30 (stop link on every parent screen and in the evening message, STOP reply, confirmation message with restart). Review: C-06, C-18, C-22, C-25, C-26, C-27.

This card defines the **stop link, stop dialog and stopped state** once. P0, P1, P2 and P3 reference it.

## 1. User voice
- Says (mother): "Done. He should take page 15 to school." · "We're travelling for a month; stop these for now."
- Unsaid (done): "Did he get it right? Is the teacher disappointed?" Unsaid (stop): "Will the teacher be angry if I stop? Will Ravi lose his place?"

## 2. Who does what
- Human: the parent taps Finish, or taps "Stop these messages", confirms, and can later tap "Start them again".
- AI · Mode: none on this screen.
- Code (no AI): the step dots; the homework page from curriculum data; stop and restart (`Child.ParentStoppedAt` set / cleared); the confirmation chat message; the ledger channel field "stopped"; refusing all later sends.

## 3. Always / never
- Always: "Well done" whatever the answer; the teacher will see it; bring the page (D-20).
- Always: the stop link on every parent screen (messages, reason, P1, P2, P3, P4) and in the evening message, while not stopped (D-30, C-06).
- Always: one tap + one confirmation; the dialog says it is reversible; restart is one tap from the chat confirmation (FR-19, C-22).
- Always: a stopped child stays on the buddy-card path in class (FR-19). Stopping doesn't change learning status.
- Never: show a score, right/wrong, or "needs buddy" to the family.
- Never: an alarm or danger style on the stop dialog (C-27). Never guilt copy.
- Never: send any message to a stopped child: evening, catch-up or reminder (FR-19).
- Uncertainty: none (no AI).
- If it goes wrong: a family that stopped keeps getting messages, or can't restart. Limits: API 409 on every send path; integration test; restart link in the chat.

## 4. States
| State | What the user sees | Copy (en) |
|---|---|---|
| Done (default) | Header "Lakshmi teacher" with Back; step dots 4 of 4; filled star; "Well done, Ravi"; teacher line; homework line; Finish; stop link. `screenshots/P4-done-default-360-light.png`, dark: `screenshots/P4-done-default-360-dark.png` | "Well done, Ravi" · "Lakshmi teacher will see this." · "Bring page 15 to school." · "Finish" |
| Stop: confirm (dialog) | Modal sheet over any parent screen: title, body, "Stop messages", "No, keep them coming" (focused first). `screenshots/P4-done-stop-confirm-360-light.png`; over P3 in dark: `screenshots/P3-answer-stop-confirm-360-dark.png` | "Stop messages for Ravi?" · "No more messages will come. You can start them again any time." |
| Stopped | Bell-off icon, heading, reassurance, "Start them again"; no stop link; no step dots. `screenshots/P4-done-stopped-360-light.png` | "Messages stopped" · "When Ravi comes back, his teacher will help him as usual." · "Start them again" |
| Stopped from another screen | Dialog closes and the phone returns to the chat, which shows the stopped bubble (see P0) | "Messages for Ravi are stopped. You can start them again any time." |
| Restarted | Stop links return; chat bubble removed; announcement | "Messages started again" (announce) |
| Empty / Loading / Offline / Error | Stop and restart offline: not designed. Recommend queue + "Saved · will send" pattern | Open item |
| Permission denied | none | — |

## 5. Copy
| Key | kn | te | ta | en | Reviewed by |
|---|---|---|---|---|---|
| `p4.title` | needs translation | లక్ష్మి టీచర్ | needs translation | Lakshmi teacher | needs review |
| `p4.wellDone` | needs translation | బాగుంది {child_vocative} | needs translation | Well done, {child} | needs review |
| `p4.teacherSees` | needs translation | లక్ష్మి టీచర్ ఇది చూస్తారు. | needs translation | Lakshmi teacher will see this. | needs review |
| `p4.bringPage` | needs translation | పేజీ {page} బడికి తీసుకురా. | needs translation | Bring page {page} to school. | needs review |
| `p4.finish` | needs translation | అయిపోయింది | needs translation | Finish | needs review |
| `stop.link` | needs translation | ఈ సందేశాలు ఆపండి | needs translation | Stop these messages | needs review |
| `stop.dialog.title` | needs translation | {child} కోసం సందేశాలు ఆపాలా? | needs translation | Stop messages for {child}? | needs review |
| `stop.dialog.body` | needs translation | ఇక సందేశాలు రావు. ఎప్పుడైనా మళ్ళీ మొదలుపెట్టవచ్చు. | needs translation | No more messages will come. You can start them again any time. | needs review |
| `stop.dialog.confirm` | needs translation | సందేశాలు ఆపండి | needs translation | Stop messages | needs review |
| `stop.dialog.cancel` | needs translation | వద్దు, వస్తూ ఉండనీ | needs translation | No, keep them coming | needs review |
| `stop.done.title` | needs translation | సందేశాలు ఆపేశాం | needs translation | Messages stopped | needs review |
| `stop.done.body` | needs translation | {child} బడికి వచ్చినప్పుడు టీచర్ ఎప్పటిలాగే సహాయం చేస్తారు. | needs translation | When {child} comes back, his teacher will help him as usual. | needs review |
| `stop.restart` | needs translation | మళ్ళీ మొదలుపెట్టండి | needs translation | Start them again | needs review |
| `aria.p4.region` | needs translation | అయిపోయింది | needs translation | Done | needs review |
| `aria.stop.region` | needs translation | సందేశాలు ఆపేశాం | needs translation | Messages stopped | needs review |
| `aria.p.steps` | needs translation | 4 లో {n}వ అడుగు | needs translation | Step {n} of 4 | needs review |
| `aria.back` | needs translation | వెనక్కి | needs translation | Back | needs review |
| `announce.stop.done` | needs translation | సందేశాలు ఆపేశాం | needs translation | Messages stopped | needs review |
| `announce.stop.restarted` | needs translation | మళ్ళీ మొదలైంది | needs translation | Messages started again | needs review |

"Well done, Ravi" uses the Telugu vocative (రవీ); store it as a separate display form or rephrase. "his teacher" in `stop.done.body` needs a gender-neutral or gendered pair (native review). Teacher-side ledger tag for this state: see Open items.

## 6. Tokens and sizes
- Done: star icon 88 px `text-illustration-warm` (decorative; meaning is carried by the words), a different icon from the AI label (C-25). Heading `text-display`; teacher line `text-body-lg`; homework line `text-caption text-ink-muted`. Finish `bg-primary text-on-primary min-h-target-primary w-full` (C-26).
- Stop link: quiet text button, full width, `min-h-target`, `text-caption text-ink-muted underline` with `bell-off` icon 16 px. Always last on the screen, below the primary action.
- Dialog: scrim + bottom sheet `bg-surface border border-border rounded-md`; title `text-title`; body `text-body text-ink-muted`. "Stop messages" `bg-primary text-on-primary min-h-target-primary` (neutral, not `error`, C-27); "No, keep them coming" text button `min-h-target`, full width.
- Stopped: `bell-off` 56 px `text-ink-muted`; "Start them again" ghost `border border-border min-h-target-primary`.
- Teacher ledger: `text-status-stopped bg-status-stopped-bg` + `bell-off`.

## 7. Acceptance checklist
- [ ] Every state above is built and reachable
- [ ] One primary action; works with one thumb at 360 px
- [ ] Status shown as icon + word, never colour alone
- [ ] Targets ≥ 48 px; 8 px apart
- [ ] AI output carries the "AI-assisted" label (none here: no AI content on P4)
- [ ] Every Indic text has a `lang` attribute; strings exist in kn, en, te, ta
- [ ] axe: 0 serious/critical, in light and dark (dialog open and closed)
- [ ] Stop link rendered on messages (evening bubble), reason, P1, P2, P3, P4 while not stopped; hidden once stopped (D-30, C-06). Playwright per screen.
- [ ] Dialog: `role="dialog"`, `aria-modal="true"`, `aria-labelledby` the title; focus moves to "No, keep them coming"; focus trapped; Esc, Back and scrim close it; focus returns to the stop link (C-27).
- [ ] `POST /api/p/{token}/stop { clientRequestId }` sets `ParentStoppedAt`; `POST /api/p/{token}/restart` clears it; both idempotent.
- [ ] After a stop, every send endpoint (catch-up send, evening-messages send) returns 409 for that child; integration test (FR-19, T-15).
- [ ] Ledger shows the stopped channel status within 2 s; learning status unchanged (FR-19, B-08).
- [ ] One confirmation message with a restart link appears in the chat (D-30, C-22).
- [ ] STOP reply honoured on the real channel (FR-19; T-40 stretch).
- [ ] Done shows no score or right/wrong; same copy for a correct and a wrong answer (D-20). Test both.
- [ ] Polite announcements "Messages stopped" / "Messages started again".
- [ ] Homework page from curriculum data, not a fixed string.

## Open items
- C-26: positive sound and a spoken line on Done need real audio.
- Tag wording conflict: the prototype's ledger tag says "Stopped by family" (ಕುಟುಂಬ ಸಂದೇಶ ನಿಲ್ಲಿಸಿದೆ); tokens.md and FR-05/FR-19 say "Stopped by family". Pick one.
- Stop and restart while offline: not designed.
- Stop/restart endpoints are not in spec §4.4; add them.
- `ParentStoppedAt` is per child; siblings on one phone would need separate stops. Confirm.
- Restart from the chat after the 14-day token expiry: does the restart link use its own token? Rahul.
