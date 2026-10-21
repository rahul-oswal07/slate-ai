# P3 Answer (picture check)

Spec: FR-07, FR-10, FR-11, FR-19, NFR-04 · Tier: MVP · Task: T-15 (check content: T-09, T-10) · Prototype: prototype/index.html (step 9) · PNG: screenshots/P3-answer-default-360-light.png

Decisions: D-07 (pictures are the answer; the numeral shows only after the tap; options shuffled by code; distractors visibly different), D-08 (four steps), D-20 (learning check is a secondary signal), D-30 (stop link). Review: C-03, C-04, C-08, C-09, C-10, C-11, C-12, C-13, C-16, C-23, C-24.

## 1. User voice
- Says (mother): "Ravi, which one is it? Tap it." · (Ravi) "This one has two bundles and five."
- Unsaid: "If he gets it wrong, will the teacher think badly of him, or of me?" "Did it go through? The network is weak."

## 2. Who does what
- Human: the child looks at the three pictures and taps one; the parent helps and taps "This one, send".
- AI · Mode: automation, approved by the teacher. A3 proposed the check item; A4 recomputed the arithmetic and confirmed only one option is correct.
- Code (no AI): the question and the three numbers from the target competency and number range; shuffling option positions and storing the correct index (C-12); the sticks SVG and accessible name for each option; scoring the tap (correct → Caught up, wrong → Needs buddy; FR-11); saving the answer locally and sending it with `clientRequestId`.

## 3. Always / never
- Always: the pictures are the answer. The numeral appears only on the picked option, after the tap (D-07, C-10).
- Always: each picture has a text name in the page language ("2 bundles of ten, 5 loose sticks") (C-13).
- Always: the check tests the same competency and number range as the activity (C-11).
- Always: tapping another picture changes the choice; no separate Change button; choices locked while sending (C-24).
- Always: the stop link (defined in P4).
- Never: show right or wrong, or a score, to the family (P4 says "Well done" whatever the answer).
- Never: score with an LLM (FR-11). Never send the correct index to the browser before the answer.
- Uncertainty: none at runtime; scoring is deterministic. Network uncertainty: the answer is saved locally and retried.
- If it goes wrong: a wrong score sends a child to a buddy unnecessarily, or hides a gap. Limits: verifier check 1, shuffle test, server-side scoring.

## 4. States
| State | What the user sees | Copy (en) |
|---|---|---|
| Default | Header "Question" with Back; step dots 3 of 4; prompt + replay; "43 − 18 = ?"; three full-width picture tiles (bundles + loose sticks, no numerals); sticky hint; stop link. `screenshots/P3-answer-default-360-light.png`, Telugu only: `screenshots/P3-answer-telugu-only-360-light.png` | "Ravi, tap the right picture" · "Tap one picture" |
| Chosen | Picked tile: thick primary border, panel fill, numeral on the right; sticky "This one, send" + change hint. `screenshots/P3-answer-chosen-360-light.png` | "This one, send" · "To change, tap another picture" |
| Sending | Button "Sending…", button and tiles disabled. `screenshots/P3-answer-sending-360-light.png` | "Sending…" |
| Could not send | Error banner (assertive); button "Try again"; tapping another picture is allowed again. `screenshots/P3-answer-could-not-send-360-light.png` | "Could not send. Your answer is saved. We’ll send it again when the network is back." · "Try again" |
| Sent | Goes to P4 Done | — |
| Stop dialog open over P3 | P4 stop dialog on top of the question. `screenshots/P3-answer-stop-confirm-360-dark.png` | see P4 |
| Already answered (reopened link) | Not designed. Recommend going straight to P4 | Open item |
| Empty / Loading | none: the check arrives with the P1 payload | — |
| Permission denied | none | — |

## 5. Copy
| Key | kn | te | ta | en | Reviewed by |
|---|---|---|---|---|---|
| `p3.title` | needs translation | ప్రశ్న | needs translation | Question | needs review |
| `p3.prompt` | needs translation | {child}, సరైన బొమ్మను నొక్కు | needs translation | {child}, tap the right picture | needs review |
| `p3.option.name` | needs translation | {tens} కట్టలు, {ones} విడి పుల్లలు | needs translation | {tens} bundles of ten, {ones} loose sticks | needs review |
| `p3.option.nameChosen` | needs translation | {tens} కట్టలు, {ones} విడి పుల్లలు, {n} | needs translation | {tens} bundles of ten, {ones} loose sticks, {n} | needs review |
| `p3.hint.tap` | needs translation | ఒక బొమ్మను నొక్కు | needs translation | Tap one picture | needs review |
| `p3.send` | needs translation | ఇదే, పంపు | needs translation | This one, send | needs review |
| `p3.hint.change` | needs translation | మార్చాలంటే వేరే బొమ్మను నొక్కు | needs translation | To change, tap another picture | needs review |
| `p3.sending` | needs translation | పంపుతోంది… | needs translation | Sending… | needs review |
| `p3.failed` | needs translation | పంపలేకపోయాం. మీ జవాబు దాచి ఉంది. నెట్‌వర్క్ రాగానే మళ్ళీ పంపుతాం. | needs translation | Could not send. Your answer is saved. We’ll send it again when the network is back. | needs review |
| `p3.retry` | needs translation | మళ్ళీ ప్రయత్నించండి | needs translation | Try again | needs review |
| `aria.p3.region` | needs translation | ప్రశ్న | needs translation | Question | needs review |
| `aria.p3.group` | needs translation | జవాబులు | needs translation | Answers | needs review |
| `aria.replay` | needs translation | మళ్ళీ వినండి | needs translation | Listen again | needs review |
| `aria.back` | needs translation | వెనక్కి | needs translation | Back | needs review |
| `aria.p.steps` | needs translation | 4 లో {n}వ అడుగు | needs translation | Step {n} of 4 | needs review |
| `announce.p3.sending` | needs translation | పంపుతోంది | needs translation | Sending | needs review |
| `announce.p3.sent` | needs translation | పంపాము | needs translation | Sent | needs review |

The prompt uses the informal "you" for the child (నొక్కు), the formal form for the parent elsewhere (నొక్కండి): native review to confirm. Stop-link keys: see P4.

## 6. Tokens and sizes
- Picture tiles: full width, `bg-surface border-2 border-border rounded-md`, 8 px+ apart. The prototype uses `min-height: 112px`; **recommend `min-h-target-picture` (120 px)**, which also meets NFR-04 (≥ 96) (C-04).
- Chosen tile: `border-4 border-primary bg-panel`, `aria-pressed="true"`; numeral `text-title` weight 700, Latin digits, `text-ink`.
- Focus ring inset on tiles (offset −4 px) so it is not clipped (B-18 pattern).
- Sticks SVG as on P2: token colours only, works in dark (C-03); `aria-hidden`, name on the button.
- Question `text-display`, Latin digits. Prompt `text-title`.
- Sticky footer `bg-canvas border-t border-divider`; send `bg-primary text-on-primary min-h-target-primary w-full`; hint `text-caption text-ink-muted`.
- Error banner `bg-error-bg border-l-4 border-l-error`, icon `text-error`, text `text-ink`, `role="alert"`.

## 7. Acceptance checklist
- [ ] Every state above is built and reachable
- [ ] One primary action; works with one thumb at 360 px
- [ ] Status shown as icon + word, never colour alone
- [ ] Targets ≥ 48 px (picture choices 120 px); 8 px apart
- [ ] The "AI-assisted, approved by Lakshmi teacher" label (key `parent.ai_label`, same as P1) shows under the step dots (NFR-08)
- [ ] Every Indic text has a `lang` attribute; strings exist in kn, en, te, ta
- [ ] axe: 0 serious/critical, in light and dark
- [ ] Option positions shuffled by code; the correct index is stored in `CheckItem`; the parent payload carries option IDs, not the correct index (C-12). xUnit: over many shuffles the correct option is not always first.
- [ ] `POST /api/p/{token}/answer { choiceId, clientRequestId }` scores on the server: correct → CaughtUp, wrong → NeedsBuddy; ledger updates within 2 s (FR-11).
- [ ] Answer is saved locally (IndexedDB) before sending; retry on reconnect; the same `clientRequestId` never creates a second `CheckResponse` (idempotent).
- [ ] Answer refused (409) for a stopped child or an unapproved / withdrawn package (FR-19, guardrail 3).
- [ ] The check's competency is one of the package targets, and its numbers are in the same range as the activity (C-11, B-12). Verifier test.
- [ ] Distractors are visibly different (different tens and ones; never differ by one loose stick only) (C-09). xUnit on the generator.
- [ ] Numeral hidden until the tap; shown only on the chosen tile (D-07, C-10).
- [ ] Each tile has an accessible name in the page language; chosen tile adds the number (C-13).
- [ ] Tiles and button disabled while sending; tapping another tile after a failure is allowed (C-24).
- [ ] Polite announcements "Sending" / "Sent"; failure banner assertive (C-08).
- [ ] No right/wrong feedback anywhere on the parent side.
- [ ] Playwright happy path P1 → P4 including this step (T-15).
- [ ] Stop link present and works (see P4, D-30).

## Open items
- C-16: the prompt should be read aloud once on arrival; needs real audio.
- Reopening the link after answering: not designed (go to P4, or allow a new answer?). Ledger rule for a second answer is not specified.
- FR-11 says "correct → caught up; wrong → needs buddy" on one tap. Confirm there is no second try, since the family never sees feedback.
- Spec §3.1 lists "Picture chosen (confirm / change)"; v2 removed Change (C-24). Update §3.1.
- Package B in the prototype targets two competencies; the check tests the first. Spec §3.3 says the single most important target: A2 or code must mark which.
