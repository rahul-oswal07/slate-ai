# T5 Back today (buddy cards + "Brought homework")

Spec: FR-12, FR-19 (buddy path for stopped families), NFR-08 · Tier: MVP · Task: T-16 · Prototype: prototype/index.html (step 11) · PNG: screenshots/T5-back-today-card-360-light.png

Decisions: D-20 (home engagement is a product measure), D-22 ("Brought homework" tap on the buddy card), D-29 (the buddy is named only on the teacher's card). Review: B-04, B-20, B-21, B-23, B-28, B-31.

## 1. User voice
- Says: "Ravi is back. Who should sit with him, and what should they do? Did he bring the homework?"
- Unsaid: "I can't stop the whole class for one child." "Will the buddy's parents mind?" "I don't want the other children to know his family stopped the messages."

## 2. Who does what
- Human: the teacher reads or prints the card, sends the buddy and the returning child to work 10 minutes, ticks "Brought homework", marks buddy time done. The buddy classmate follows the printed or shown card (no device).
- AI · Mode: none on this screen at render time. Key words come from the textbook glossary. Buddy content was produced by A3 inside the approved catch-up (FR-07); for no-phone children see Open items (B-21).
- Code (no AI): which children are back today (attendance), days away, buddy selection (a child present for all the lessons this child missed), the 3 sums and their answers (guardrail 4, B-21), the homework page from `Competency.TextbookPage`, the 10 minutes, card order and "Card 1 of 2", storing `BuddyCard.HomeworkReturnedAt`.

## 3. Always / never
- Always: a card for every returning child, phone or no phone, stopped or not (FR-12, FR-19).
- Always: the buddy was present on every day the returning child missed; say so on the card ("was present both days").
- Always: cards cached for offline reading and printable.
- Never: show family or channel status ("Stopped by family", "No phone") on the card or the printout (B-20). Never show the check result or a score on the card.
- Never: send child names to an AI provider when building the card (NFR-05).
- Uncertainty: no eligible buddy (nobody present on all missed days) → not designed; see Open items. Buddy selection is deterministic, so there is no confidence score.
- If it goes wrong: a buddy who also missed the lesson can't help, and the child feels singled out. Limit: code selection with a unit test; card wording is positive.

## 4. States
| State | What the user sees | Copy (en) |
|---|---|---|
| Cards (default) | "Card 1 of 2 · next: Manju"; card: child name, days away, buddy panel with 10 min, 3 sums, Kannada words, "Brought homework" checkbox with the page; prev / print / next; "Mark buddy time done". `screenshots/T5-back-today-card-360-light.png`, dark: `screenshots/T5-back-today-card-360-dark.png` | "Back after 2 days" · "Buddy (was present both days)" · "3 sums to do together" · "Brought homework" · "Mark buddy time done" |
| None today (empty) | Users icon, heading, explanation. `screenshots/T5-back-today-none-360-light.png` | "No one is back today" · "A buddy card appears here on the day an absent child comes back." |
| Offline | Warning banner; cards readable from cache; ticks queue. `screenshots/T5-back-today-offline-360-light.png` | "No network. The cards are saved on your phone." |
| Marked done (`done`) | Success banner with Undo replaces the button (B-31). State chip "Marked done" in the prototype. No PNG exported | "Buddy time done" · "Undo" |
| Homework ticked | Checkbox checked; stored with the card. No PNG exported | "Brought homework" |
| Last / first card | Prev or Next disabled with a visible disabled style (B-23) | — |
| Loading | Not designed. Cached cards show at once; no spinner when cached | — |
| Error (tick not saved) | Not designed. Ticks go to the outbox, so the only error is a 409 on replay | Open item |
| Permission denied | none: no mic or camera on this screen | — |

## 5. Copy
| Key | kn | te | ta | en | Reviewed by |
|---|---|---|---|---|---|
| `ret.title` | ಇಂದು ಮರಳಿದವರು | — | needs translation | Back today | needs review |
| `ret.nav` | ಮರಳಿ ಬಂದವರು | — | needs translation | Back today | needs review |
| `ret.position` | ಕಾರ್ಡ್ {i} / {n} | — | needs translation | Card {i} of {n} | needs review |
| `ret.next` | ಮುಂದೆ: {name} | — | needs translation | next: {name} | needs review |
| `ret.daysAway` | {days} ದಿನದ ನಂತರ ಮರಳಿದ್ದಾರೆ | — | needs translation | Back after {days} days | needs review |
| `ret.buddy.label` | ಬಡ್ಡಿ (ಆ ಎರಡೂ ದಿನ ಹಾಜರಿದ್ದರು) | — | needs translation | Buddy (was present both days) | needs review |
| `ret.minutes` | {n} ನಿಮಿಷ | — | needs translation | {n} min | needs review |
| `ret.sums.title` | ಒಟ್ಟಿಗೆ ಮಾಡಲು 3 ಲೆಕ್ಕಗಳು | — | needs translation | 3 sums to do together | needs review |
| `ret.keywords.title` | ಕನ್ನಡ ಪದಗಳು (ಪಠ್ಯಪುಸ್ತಕದಿಂದ) | — | needs translation | Kannada words (from the textbook) | needs review |
| `ret.homework` | ಮನೆಕೆಲಸ ತಂದಿದ್ದಾರೆ | — | needs translation | Brought homework | needs review |
| `ret.homework.page` | ಭಾಗ {part}, ಪುಟ {page} | — | needs translation | Part {part}, page {page} | needs review |
| `ret.done.button` | ಬಡ್ಡಿ ಸಮಯ ಮುಗಿಯಿತು | — | needs translation | Mark buddy time done | needs review |
| `ret.done.banner` | ಬಡ್ಡಿ ಸಮಯ ಮುಗಿದಿದೆ | — | needs translation | Buddy time done | needs review |
| `ret.undo` | ರದ್ದು | — | needs translation | Undo | needs review |
| `ret.none.title` | ಇಂದು ಯಾರೂ ಮರಳಿಲ್ಲ | — | needs translation | No one is back today | needs review |
| `ret.none.body` | ಗೈರಾದ ಮಗು ಮರಳಿದ ದಿನ ಬಡ್ಡಿ ಕಾರ್ಡ್ ಇಲ್ಲಿ ಬರುತ್ತದೆ. | — | needs translation | A buddy card appears here on the day an absent child comes back. | needs review |
| `ret.offline` | ನೆಟ್‌ವರ್ಕ್ ಇಲ್ಲ. ಕಾರ್ಡ್‌ಗಳು ಫೋನ್‌ನಲ್ಲಿ ಉಳಿದಿವೆ. | — | needs translation | No network. The cards are saved on your phone. | needs review |
| `aria.ret.prev` | ಹಿಂದಿನ ಕಾರ್ಡ್ | — | needs translation | Previous card | needs review |
| `aria.ret.print` | ಕಾರ್ಡ್ ಮುದ್ರಿಸಿ ಅಥವಾ ಹಂಚಿ | — | needs translation | Print or share card | needs review |
| `aria.ret.next` | ಮುಂದಿನ ಕಾರ್ಡ್ | — | needs translation | Next card | needs review |
| `announce.ret.done` | ಬಡ್ಡಿ ಸಮಯ ಮುಗಿದಿದೆ | — | needs translation | Buddy time done | needs review |

`ret.buddy.label` must vary with the number of days ("both days" only fits 2). Add plural forms for 1, 2 and 3+ days.

## 6. Tokens and sizes
- Card `bg-surface border-2 border-border rounded-md`; child name `text-title`; meta `text-caption text-ink-muted`.
- Buddy panel `bg-panel border border-border rounded-md`; label `text-primary-strong text-caption` (rule 1); users icon.
- Sums: Latin digits, `text-title` weight 700, no letter-spacing on Indic text. Key words `bg-panel rounded-sm text-label lang="kn"`; plain text, not chip borders (B-28).
- "Brought homework" row `min-h-target`, 24 px checkbox, top `border-divider` plus spacing.
- Prev / print / next: ghost icon buttons `min-h-target min-w-target`, 8 px apart; disabled style distinct (not just opacity on teal). "Mark buddy time done" `bg-primary text-on-primary min-h-target-primary w-full`.
- Banners: offline `border-l-warning`; done `border-l-primary` with text Undo button `min-h-target`.
- Print stylesheet: black on white, no tags, no status, no nav.

## 7. Acceptance checklist
- [ ] Every state above is built and reachable
- [ ] One primary action; works with one thumb at 360 px
- [ ] Status shown as icon + word, never colour alone
- [ ] Targets ≥ 48 px; 8 px apart
- [ ] AI output carries the "AI-assisted" label (only if any card wording is AI-written; sums and textbook words are not, B-21)
- [ ] Every Indic text has a `lang` attribute; strings exist in kn, en, te, ta
- [ ] axe: 0 serious/critical, in light and dark
- [ ] xUnit: the buddy was present on every date the returning child missed (FR-12, T-16).
- [ ] xUnit: the 3 sums are generated by code from the target competency and number range; answers recomputed (guardrail 4, B-21).
- [ ] A card exists for each child back today, including no-phone and stopped children (FR-12, FR-19).
- [ ] Card and printout never show family or channel status (B-20). Test: render a stopped child's card; assert no "stopped"/"no phone" text.
- [ ] "Brought homework": `POST /api/buddy-cards/{id}/homework-returned { returned: bool, clientRequestId }`, idempotent; offline taps queue in the Dexie outbox (D-22, T-16 endpoint test).
- [ ] Homework page comes from `Competency.TextbookPage` for the child's target, not a fixed string.
- [ ] Cards cached for offline (service worker + IndexedDB) and printable (FR-12).
- [ ] Print button has an accessible name in all 4 locales (B-04).
- [ ] Position shown as text "Card 1 of 2 · next: Manju", not dots only (B-23). Disabled Prev/Next look disabled.
- [ ] "Mark buddy time done" has Undo; no auto-advance (B-31). Polite announcement on done. The "Marked done" state is reachable.
- [ ] Focus stays on the card heading when moving between cards (B-19).

## Open items

**Resolved 22 Oct:**
- No eligible buddy: the classmate present on the most missed days; if none, "Teacher-led: 10 minutes"; state chip "No buddy available" (D-59).

**Still open:**
- B-21: approval path for the no-phone child's buddy-card wording. Sums come from code and key words from the textbook; who approves any AI wording for children with no catch-up package is still open.
- No eligible buddy (nobody present on all missed days): fallback rule not specified (closest match, or teacher picks). Rahul + Monika.
- Spec §3.1 T5 says "Swipe cards"; the prototype uses Prev/Next buttons. Keep buttons; swipe is optional.
- FR-07/A3 lists `buddy{sums[3], keyWords}` as AI output, but guardrail 4 and B-21 say sums come from code. Recommend: code generates sums; A3 output for buddy sums is ignored or removed from the schema.
- "Mark buddy time done" is not in the data model (`BuddyCard` has `HomeworkReturnedAt` only). Add `DoneAt` or keep it local.
