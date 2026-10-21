# P2 Activity at home

Spec: FR-07, FR-10, FR-19, NFR-04, NFR-08 · Tier: MVP · Task: T-15 (content: T-09) · Prototype: prototype/index.html (step 9, before the answer) · PNG: screenshots/P2-activity-default-360-light.png

Decisions: D-07 (bundles of ten sticks, same picture language as the check), D-08 (four steps, Next never blocked by audio), D-22 (homework from the child's own textbook page, "Show {teacher}"), D-30 (stop link). Review: C-11, C-16, C-17, C-18, C-23.

## 1. User voice
- Says (mother): "What do we do? We have no sticks." · (Ravi) "Four bundles and three. Now take away eighteen."
- Unsaid: "I can't read much; I hope the voice tells me." "I don't have time tonight." "Will the teacher check the homework?"

## 2. Who does what
- Human: the parent and child do the activity with household objects; note the homework page; tap "Next: question".
- AI · Mode: automation, approved by the teacher (T3). A3 wrote the activity instruction and the household-object option; A4 checked the objects are on the allowed list.
- Code (no AI): the sticks picture (SVG from the number: tens as bundles, ones as loose sticks), its text equivalent ("4 bundles of ten, 3 loose sticks"), the number shown, the homework page from `Competency.TextbookPage`, step dots.

## 3. Always / never
- Always: activity first, homework second (C-17). The picture uses the same bundles-of-sticks language as the question (D-07, C-09).
- Always: household-object option from the allowed list (bottle caps, sticks, stones, tamarind seeds, bangles, rotis, spoons; spec §3.3).
- Always: homework points to the child's own textbook page and says "show Lakshmi teacher when you come back" (D-22).
- Always: Next enabled; the activity never depends on audio (C-05). Replay button in the same place as other steps (C-16).
- Always: the stop link (defined in P4).
- Never: numbers written by the LLM without code check; the number in the picture is recomputed (A4 check 1).
- Never: the teacher's real name in AI input; `{teacher}` is filled for display only (NFR-05).
- Uncertainty: none at runtime. If A3 used an object outside the allowed list, A4 failed it before approval.
- If it goes wrong: the activity doesn't match the question, and the child fails a check on something not practised. Limit: same competency and number range rule (C-11).

## 4. States
| State | What the user sees | Copy (en) |
|---|---|---|
| Default | Header "Activity at home" with Back; step dots 2 of 4; card: "43", replay, sticks picture, its description, instruction, object option; "Next: question"; homework card; stop link. `screenshots/P2-activity-default-360-light.png` | "4 bundles of ten, 3 loose sticks" · "Ravi: open one bundle into 10 loose sticks. Now take away 18." · "No sticks? Use bottle caps, stones or tamarind seeds." · "Homework: page 15" |
| Audio replaying (spec §3.1) | Replay button pressed; instruction plays. Not a chip in the prototype (toast only) | — |
| Empty / Loading / Offline / Error | none: content arrives with the P1 payload and is cached; nothing loads on this step | — |
| Permission denied | none | — |

## 5. Copy
| Key | kn | te | ta | en | Reviewed by |
|---|---|---|---|---|---|
| `p2.title` | needs translation | ఇంట్లో చేసే ఆట | needs translation | Activity at home | needs review |
| `p2.sticks.alt` | needs translation | {tens} కట్టలు, {ones} విడి పుల్లలు | needs translation | {tens} bundles of ten, {ones} loose sticks | needs review |
| `p2.next` | needs translation | తరువాత: ప్రశ్న | needs translation | Next: question | needs review |
| `p2.homework.title` | needs translation | ఇంటి పని: పేజీ {page} | needs translation | Homework: page {page} | needs review |
| `p2.homework.body` | needs translation | పాఠ్యపుస్తకం భాగం {part} · బడికి వచ్చినప్పుడు లక్ష్మి టీచర్‌కు చూపించు. | needs translation | Textbook Part {part} · show Lakshmi teacher when you come back. | needs review |
| `aria.p2.region` | needs translation | ఇంట్లో చేసే ఆట | needs translation | Activity at home | needs review |
| `aria.replay` | needs translation | మళ్ళీ వినండి | needs translation | Listen again | needs review |
| `aria.back` | needs translation | వెనక్కి | needs translation | Back | needs review |
| `aria.p.steps` | needs translation | 4 లో {n}వ అడుగు | needs translation | Step {n} of 4 | needs review |

Package content (A3 output, not locale strings), shown here for reference: instruction "రవి: ఒక కట్ట విప్పి 10 విడి పుల్లలు చేయి. ఇప్పుడు 18 తీసేయి." / "Ravi: open one bundle into 10 loose sticks. Now take away 18."; object option "పుల్లలు లేకపోతే సీసా మూతలు, రాళ్ళు లేదా చింతగింజలు వాడండి." / "No sticks? Use bottle caps, stones or tamarind seeds." No announce() strings on this step. Stop-link keys: see P4.

## 6. Tokens and sizes
- Card `bg-surface border border-border rounded-md`, centred. Number `text-display`, Latin digits.
- Sticks SVG: sticks `fill` = the prototype's `--stick` (light `#B45309`, dark `#FBBF24`, the `warning` values); band around each bundle `fill-primary`. Colours from tokens, never hard-coded, so they work in dark (C-03). SVG `aria-hidden`; the description below is the text equivalent.
- Description `text-caption text-ink-muted`; instruction `text-body` weight 600; object option `text-caption text-ink-muted`.
- Replay button 48 px, icon `text-primary-strong`, top-right of the card (same spot on P3 and the reason page, C-16).
- "Next: question" `bg-primary text-on-primary min-h-target-primary w-full`.
- Homework card `bg-surface border border-border`, book icon 28 px, title `text-label`.

## 7. Acceptance checklist
- [ ] Every state above is built and reachable
- [ ] One primary action; works with one thumb at 360 px
- [ ] Status shown as icon + word, never colour alone
- [ ] Targets ≥ 48 px; 8 px apart
- [ ] The "AI-assisted, approved by Lakshmi teacher" label (key `parent.ai_label`, same as P1) shows under the step dots (NFR-08)
- [ ] Every Indic text has a `lang` attribute; strings exist in kn, en, te, ta
- [ ] axe: 0 serious/critical, in light and dark
- [ ] The activity's competency and number range match the learning check on P3 (C-11). Verifier test with a mismatching pair fails.
- [ ] Sticks picture is generated by code from the number; tens = bundles, ones = loose sticks; text equivalent from the same number (D-07, C-13).
- [ ] Household object is on the allowed list (A4 code check 5).
- [ ] Homework page comes from `Competency.TextbookPage` for the target (D-22).
- [ ] "Next: question" is enabled whether or not any audio has played (C-05).
- [ ] Back returns to P1 with the audio position kept.
- [ ] SVG colours switch with the theme; sticks visible in dark (C-03).
- [ ] Stop link present and works (see P4, D-30).

## Open items
- NFR-08: resolved 20 Oct; P1, P2 and P3 all repeat the label, because a parent may open any step directly.
- C-16: the instruction should play once on arrival; needs real audio. The prototype shows a toast.
- C-17 suggested a textbook-page picture with "15"; the prototype uses a book icon. Keep the icon unless round 1 says otherwise.
- tokens.md has no `stick` token; the prototype reuses the `warning` values. Add a decorative `stick` token, since rule 2 reserves `warning` for icons and borders.
