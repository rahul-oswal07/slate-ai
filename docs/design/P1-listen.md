# P1 Listen

Spec: FR-09, FR-10, FR-19, NFR-03, NFR-04, NFR-05, NFR-08 · Tier: MVP · Task: T-15 (audio: T-11) · Prototype: prototype/index.html (step 8) · PNG: screenshots/P1-listen-default-360-light.png

Decisions: D-08 (four steps, step dots, Next never blocked by audio), D-18 (spoken AI disclosure after the greeting), D-30 (stop link on every parent screen). Review: C-01, C-02, C-05, C-07, C-08, C-16, C-18, C-19, C-20, C-23, C-25, C-28, C-29.

## 1. User voice
- Says (mother, then Ravi): "It's Lakshmi teacher's voice. Ravi, come and listen." · "Is it finished? What do I do next?"
- Unsaid: "Will this use up my data?" "Is that really the teacher or a machine?" "If the network goes, do we have to start again?"

## 2. Who does what
- Human: the parent opens the link (shared phone, after 6 pm), taps Listen, may read the words, taps Next. The child listens.
- AI · Mode: automation, approved by the teacher before send. A3 wrote the script; A4 verified it; Sarvam TTS spoke it. Nothing is generated when the page opens.
- Code (no AI): token check and 14-day expiry; audio file assembly (greeting → disclosure → script); segment boundaries; elapsed / total time; caching and resume position; "listened" event for the ledger; step dots.

## 3. Always / never
- Always: the teacher's own greeting first, then the spoken disclosure, then the lesson (FR-09). The written words start with the same disclosure.
- Always: the label "AI-assisted, approved by Lakshmi teacher" with a bot icon, not the reward star (NFR-08, C-25).
- Always: Next enabled from the start; activity and homework never depend on audio (C-05, D-08).
- Always: the stop link (defined in P4).
- Never: serve a package that is not Approved; `GET /api/p/{token}` returns no content for unapproved, withdrawn or stopped (guardrail 3).
- Never: send the child's or teacher's name to Sarvam (NFR-05). See Open items on the disclosure and script.
- Never: count or show message opens to anyone (D-20). "Listened" is recorded only when playback completes.
- Uncertainty: none on this screen (no AI at runtime). Network uncertainty: show loading feedback within 400 ms (C-19).
- If it goes wrong: audio fails on a weak network and the family gives up. Limit: ≤ 300 KB, cached, resume from the saved position, Next always works.

## 4. States
| State | What the user sees | Copy (en) |
|---|---|---|
| Default | Header "Ravi’s lesson" with Back; step dots 1 of 4; teacher initial; "From Lakshmi teacher"; AI label; 156 px Listen button; progress bar 0:00 / 1:35; segments; "Read the words"; Next; stop link. `screenshots/P1-listen-default-360-light.png`, dark: `screenshots/P1-listen-default-360-dark.png` | "Listen" · "AI-assisted, approved by Lakshmi teacher" · "Next" |
| Loading | Button shows a clock icon and "Loading…" within 400 ms of the tap. `screenshots/P1-listen-loading-360-light.png` | "Loading…" |
| Playing | Pause icon and label; bar and elapsed time move; current segment highlighted. `screenshots/P1-listen-playing-360-light.png` | "Pause" |
| Paused (`paused`) | Play icon; label "Resume"; bar and elapsed time stay where playback stopped. The label reads "Resume" whenever playback is part-way, after a pause or a network drop | "Resume" |
| Played | Bar full; label "Listen again". `screenshots/P1-listen-played-360-light.png` | "Listen again" |
| Network dropped mid-play (resume) | Assertive banner; button "Resume"; position kept. `screenshots/P1-listen-network-dropped-360-light.png` | "Network dropped. The part you heard is saved. It will continue from there when the network is back." · "Resume" |
| Link expired | Clock icon, message + replay; homework card; OK; stop link. `screenshots/P1-listen-link-expired-360-light.png` | "This link is old" · "Lakshmi teacher will send a new one. You don’t need to do anything." · "Homework: textbook Part 2, page 15" · "OK" |
| Read the words (expanded) | Disclosure line, then the transcript; Kannada key words carry `lang="kn"` | "The rest of this message is read by a computer voice, from Lakshmi teacher’s lesson." |
| Empty | none: a link always carries one package | — |
| Offline on open | Not designed. The page needs a connection once (spec §6). Recommend the expired-style layout with network copy | Open item |
| Error (audio file fails) | Not designed beyond "network dropped" | Open item |
| Permission denied | none: no mic or camera | — |

## 5. Copy
| Key | kn | te | ta | en | Reviewed by |
|---|---|---|---|---|---|
| `p1.title` | needs translation | {child} కోసం పాఠం | needs translation | {child}’s lesson | needs review |
| `p1.from` | needs translation | లక్ష్మి టీచర్ నుండి | needs translation | From Lakshmi teacher | needs review |
| `p.aiLabel` | needs translation | AI సహాయంతో · లక్ష్మి టీచర్ ఆమోదించారు | needs translation | AI-assisted, approved by Lakshmi teacher | needs review |
| `p1.listen` | needs translation | వినండి | needs translation | Listen | needs review |
| `p1.loading` | needs translation | వస్తోంది… | needs translation | Loading… | needs review |
| `p1.pause` | needs translation | ఆపు | needs translation | Pause | needs review |
| `p1.resume` (paused or dropped part-way) | needs translation | కొనసాగించు | needs translation | Resume | needs review |
| `p1.again` | needs translation | మళ్ళీ వినండి | needs translation | Listen again | needs review |
| `p1.seg.teacher` | needs translation | టీచర్ గొంతు | needs translation | Teacher | needs review |
| `p1.seg.computer` | needs translation | కంప్యూటర్ గొంతు | needs translation | Computer voice | needs review |
| `p1.seg.lesson` | needs translation | పాఠం | needs translation | Lesson | needs review |
| `p1.readWords` | needs translation | రాసినది చూడండి | needs translation | Read the words | needs review |
| `p1.disclosure` | needs translation | మిగిలిన సందేశాన్ని కంప్యూటర్ గొంతు చదువుతుంది, లక్ష్మి టీచర్ పాఠం నుండి. | needs translation | The rest of this message is read by a computer voice, from Lakshmi teacher’s lesson. | needs review |
| `p1.dropped` | needs translation | నెట్‌వర్క్ పోయింది. విన్న భాగం దాచి ఉంది. నెట్‌వర్క్ రాగానే అక్కడి నుండే కొనసాగుతుంది. | needs translation | Network dropped. The part you heard is saved. It will continue from there when the network is back. | needs review |
| `p1.expired.title` | needs translation | ఈ లింక్ పాతది | needs translation | This link is old | needs review |
| `p1.expired.body` | needs translation | లక్ష్మి టీచర్ కొత్తది పంపుతారు. మీరు ఏమీ చేయనక్కర్లేదు. | needs translation | Lakshmi teacher will send a new one. You don’t need to do anything. | needs review |
| `p1.expired.homework` | needs translation | ఇంటి పని: పాఠ్యపుస్తకం {page} | needs translation | Homework: textbook {page} | needs review |
| `p.next` | needs translation | తరువాత | needs translation | Next | needs review |
| `common.ok` | needs translation | సరే | needs translation | OK | needs review |
| `aria.p1.region` / `aria.p1.expiredRegion` | needs translation | వినండి / లింక్ పాతది | needs translation | Listen / Link is old | needs review |
| `aria.p.steps` | needs translation | 4 లో {n}వ అడుగు | needs translation | Step {n} of 4 | needs review |
| `aria.replay` | needs translation | మళ్ళీ వినండి | needs translation | Listen again | needs review |
| `aria.back` | needs translation | వెనక్కి | needs translation | Back | needs review |
| `announce.p1.playing` | needs translation | వినిపిస్తోంది | needs translation | Playing | needs review |
| `announce.p1.paused` | needs translation | ఆపారు | needs translation | Paused | needs review |

The Listen button has no `aria-label`: its accessible name is its visible label (C-01), and it uses `aria-pressed` for play/pause. The transcript is package content (A3 output), not a locale string. Stop-link keys: see P4.

## 6. Tokens and sizes
- Page `bg-canvas`; header `bg-surface`, title `text-title`.
- Listen button: 156 px circle `bg-primary text-on-primary`, icon 52 px, label `text-label`; any caption inside inherits `on-primary` (C-02). Focus ring 3 px `focus` with 2 px offset.
- AI label `text-primary-strong text-label` with a bot icon (C-25).
- Progress bar track `bg-divider` with a `border` outline; fill `bg-primary`. Times `text-caption text-ink-muted`, Latin digits.
- Segment labels `text-caption`, wrap; current segment `text-ink` weight 600 (C-28).
- Step dots: 10 px dots, current 26 px pill `bg-primary`, done `bg-primary`, todo `bg-divider border-border`; `role="img"` with label.
- "Read the words": `<details>` card `bg-surface border border-border`, summary `min-h-target`.
- Network banner `bg-surface border-l-4 border-l-warning`, text `text-ink`, `role="alert"`.
- Next `bg-primary text-on-primary min-h-target-primary w-full`.

## 7. Acceptance checklist
- [ ] Every state above is built and reachable
- [ ] One primary action; works with one thumb at 360 px
- [ ] Status shown as icon + word, never colour alone
- [ ] Targets ≥ 48 px; 8 px apart
- [ ] AI output carries the "AI-assisted" label
- [ ] Every Indic text has a `lang` attribute; strings exist in kn, en, te, ta
- [ ] axe: 0 serious/critical, in light and dark
- [ ] `GET /api/p/{token}` returns the payload only for an Approved package and a not-stopped child; expired token (> 14 days) → expired view (guardrail 3, spec §7).
- [ ] Audio file ≤ 300 KB and ≤ 95 s including greeting and disclosure; segment order greeting → disclosure → script (FR-09, T-11 integration test).
- [ ] Audio cached (service worker / Cache API) after first load; replay works offline; resume continues from the saved position after a drop (C-20).
- [ ] Next is enabled before, during and after playback (C-05, D-08). Playwright: tap Next without playing → P2.
- [ ] Loading feedback within 400 ms of the tap; elapsed and total time shown (C-19).
- [ ] Listen button's accessible name equals its visible text in each state (C-01).
- [ ] Label rule: "Listen" at the start; "Loading…" while loading; "Pause" while playing; "Resume" whenever playback is part-way (paused or dropped); "Listen again" after the end. Vitest on the rule.
- [ ] Polite announcements "Playing" / "Paused"; the network banner is assertive (C-08).
- [ ] "Listened" ledger status is set when playback reaches the end, within 2 s (FR-05).
- [ ] Expired link: no dead end; homework card and OK shown; message has a replay button (C-29).
- [ ] Page transfer ≤ 500 KB excluding audio; Lighthouse mobile performance ≥ 85 (FR-10).
- [ ] `<html lang>` is the page language (`te`); English captions `lang="en"`; Kannada key words `lang="kn"` (C-07).
- [ ] Stop link present and works (see P4, D-30).

## Open items
- NFR-05 conflict: the transcript starts "Ravi, today let's learn…" and ends "Show {teacher}"; the disclosure names "Lakshmi teacher". If these are spoken by Sarvam TTS, real names go to the provider. Options: name only in the teacher's recorded greeting; TTS says "you"/"your teacher"; or splice pre-recorded name segments. Rahul to decide before T-11.
- Resolved: the Paused state is in the prototype; the label reads "Resume" whenever playback is part-way.
- C-19 "back 10 s" control and C-16 auto-play of the spoken prompt need real audio.
- Offline-on-open and audio-file-error states not designed.
- Spec NFR-08 label is "AI-assisted, approved by your teacher"; prototype says "AI-assisted, approved by Lakshmi teacher". Pick one wording for all locales.
- Spec §3.1 P1 shows "Teacher photo initial"; the prototype uses an initial only (no photo, consistent with §7 "no photos").
