# P0 Messages (chat stand-in) + Reason for absence

> **MVP scope (D-70):** evening message = written text from the approved template + her greeting audio (no stitched clips); reason = picture buttons + Skip; stop link in the message. See [mvp-build-flow.md](mvp-build-flow.md).

Spec: FR-20, FR-21, FR-19, FR-16 (stretch), NFR-05 · Tier: MVP (chat is the demo stand-in; real WhatsApp is FR-16 / T-40, stretch) · Task: T-44 (evening message), T-45 (reason + A7), T-15 (catch-up link) · Prototype: prototype/index.html (steps 5, 6; stopped bubble step 13) · PNG: screenshots/P0-messages-evening-360-light.png

Decisions: D-05 (simulated parent phone primary), D-21 (template approved once, slots by code), D-23 (reason optional, caring, category only), D-29 ("a classmate is keeping Ravi's seat"), D-30 (stop link everywhere, STOP reply, confirmation with restart), D-43 (parent phone from T8, never shared with AI), D-47 (home language sets the message language), D-48 (no "Tomorrow"), D-50 (no names in the computer voice), D-52 (greeting per language). Review: C-06, C-14, C-15, C-16, C-21, C-22, C-30.

The chat screen is a stand-in for WhatsApp in the demo shell. It renders the same three message types the real channel will carry. Build it as a demo component, not as a product inbox.

## 1. User voice
- Says (Ravi's mother): "The teacher remembered Ravi. What did they do today, and what should he do at home?" · "He had fever. Do I have to explain?"
- Unsaid: "Are they angry he's away?" "Will they think we don't care?" "Who else reads this on our shared phone?" "Does this cost data?"

## 2. Who does what
- Human: the teacher approved the evening template once at setup (T0) and confirmed today's lesson (T2). The parent listens to the greeting, reads the message, optionally taps a reason or records a voice note, may tap "Stop these messages".
- AI · Mode:
  - Evening message: none. No LLM on this path (FR-20, D-21).
  - Catch-up link bubble: none at send time. It links to an approved package (A2/A3/A4 ran earlier, T3).
  - Reason voice note: automation with confirmation. Sarvam STT → A7 (Claude Haiku) returns one of 5 categories + confidence; the parent sees "Is it this?" before sending.
- Code (no AI): evening slots `{child}` (written text only), `{lesson}` (name in the family's language), `{page}` from the confirmed lesson record and curriculum; no `{tomorrow}` (D-48); template picked by the child's home language (D-47); send only after 6 pm to opted-in, not-stopped families, to the phone entered on T8 (D-43); audio stitched by the 6 pm job = greeting in that language (or her Kannada greeting + computer-voice introduction, D-52) + pre-made fixed phrases + lesson-name clip + page numbers, saying "your child", never a name (D-50); catch-up duration in the bubble ("2 minutes") from the audio length; storing the reason category; stop / restart.

## 3. Always / never
- Always: the evening message uses only the template text the teacher approved; slots come from teacher-confirmed records (guardrail 3, FR-20).
- Always: a quiet "Stop these messages" link in the evening message (D-30; defined in P4).
- Always: the reason is optional. Skip is visible; skipping never blocks the catch-up page (FR-21).
- Always: say a computer listens before the parent records ("A computer listens to pick a picture") and show the chosen picture for confirmation before sending (C-15).
- Never: name the buddy to the family (D-29). Never ask for symptoms or health details. Never store the transcript or voice audio; category only.
- Never: send child names, parent numbers or teacher names to Claude or Sarvam; the voice note transcript goes to A7 without names (NFR-05). Never send to a stopped child (FR-19).
- Never: blame words about absence; the pull is positive (teacher's voice, homework, the seat kept).
- Uncertainty: A7 confidence below threshold, or a category outside the enum → "Other" (FR-21, T-45). The parent can still change the picture before sending.
- If it goes wrong: a message reaches a family that stopped, or reveals a child's absence on a shared lock screen. Limits: API 409 for stopped/unapproved/unconfirmed; message text kept short and kind (C-30 open on lock-screen preview).

## 4. States
| State | What the user sees | Copy (en) |
|---|---|---|
| Messages · empty | Chat header; empty icon; demo-only English line. `screenshots/P0-messages-empty-360-light.png` | "No messages yet" · (demo) "Ravi’s mother’s phone. Messages from Lakshmi teacher arrive here." |
| Messages · evening message | Day separator; bubble: greeting voice note (play, "0:09"), text with slots, "Is everything OK? Tell us" link, stop link, time. `screenshots/P0-messages-evening-360-light.png`, dark: `screenshots/P0-messages-evening-360-dark.png`, Telugu only: `screenshots/P0-messages-telugu-only-360-light.png` | "We missed Ravi today. In class we learned subtraction with borrowing. Homework: textbook Part 2, page 15. A classmate is keeping Ravi’s seat." (no "Tomorrow", D-48) |
| Messages · catch-up link | Second bubble (Friday) appears only after the teacher approves the package: text + "Open" link to P1. No PNG exported | "A short lesson for Ravi from Lakshmi teacher. 2 minutes." · "Open" |
| Messages · stopped | Confirmation bubble with restart link; stop links disappear. No PNG exported (see P4) | "Messages for Ravi are stopped. You can start them again any time." · "Start them again" |
| Reason · default | Teacher initial; question + replay; 2-column grid of 5 picture buttons; "Say it instead"; disclosure hint; sticky Send (disabled) + hint; Skip; stop link. `screenshots/P0-reason-default-360-light.png`, dark: `screenshots/P0-reason-default-360-dark.png` | "We missed Ravi today. Is everything OK?" · "Tap one picture" · "Skip" |
| Reason · picture chosen | Chosen button pressed (thick border, panel fill); Send enabled. `screenshots/P0-reason-chosen-360-light.png` | "Send" |
| Reason · sent | Check icon, thanks, privacy line, OK. `screenshots/P0-reason-sent-360-light.png` | "Thank you" · "Only Lakshmi teacher sees this." |
| Reason · recording voice note | Not designed (prototype shows a toast). Same mic model as D-31: tap to start, tap to stop, timer, auto-stop | Open item |
| Reason · "Is it this?" | Not designed. A7 result shown as the pre-selected picture with "Is it this?"; parent confirms or taps another | Open item |
| Reason · mic permission denied | Not designed. Fall back to the picture buttons | Open item |
| Loading / Offline / Error (reason send) | Not designed. Recommend: save locally, send on reconnect, same copy pattern as P3 | Open item |

## 5. Copy
| Key | kn | te | ta | en | Reviewed by |
|---|---|---|---|---|---|
| `chat.header` | needs translation | లక్ష్మి టీచర్ · బడి | needs translation | Lakshmi teacher · school | needs review |
| `chat.empty` | needs translation | ఇంకా సందేశాలు లేవు | needs translation | No messages yet | needs review |
| `chat.day.tuesday` / `.friday` | needs translation | మంగళవారం / శుక్రవారం | needs translation | Tuesday / Friday | needs review |
| `evening.voice` | needs translation | లక్ష్మి టీచర్ గొంతు · {duration} | needs translation | Lakshmi teacher’s voice · {duration} | needs review |
| `evening.body` | needs translation | ఈ రోజు {child} మాకు గుర్తొచ్చాడు. ఈ రోజు తరగతిలో {lesson} నేర్చుకున్నాం. ఇంటి పని: పాఠ్యపుస్తకం {page}. ఒక సహవిద్యార్థి {child} సీటు కాపాడుతోంది. | needs translation | We missed {child} today. In class we learned {lesson}. Homework: textbook {page}. A classmate is keeping {child}’s seat. | needs review |
| `evening.reasonLink` | needs translation | అంతా బాగుందా? చెప్పండి | needs translation | Is everything OK? Tell us | needs review |
| `catchup.body` | needs translation | {child} కోసం లక్ష్మి టీచర్ నుండి ఒక చిన్న పాఠం. {minutes} నిమిషాలు. | needs translation | A short lesson for {child} from Lakshmi teacher. {minutes} minutes. | needs review |
| `catchup.open` | needs translation | తెరవండి | needs translation | Open | needs review |
| `stopped.body` | needs translation | {child} కోసం సందేశాలు ఆపేశాం. ఎప్పుడైనా మళ్ళీ మొదలుపెట్టవచ్చు. | needs translation | Messages for {child} are stopped. You can start them again any time. | needs review |
| `stopped.restart` | needs translation | మళ్ళీ మొదలుపెట్టండి | needs translation | Start them again | needs review |
| `reason.title` | needs translation | లక్ష్మి టీచర్ | needs translation | Lakshmi teacher | needs review |
| `reason.question` | needs translation | ఈ రోజు {child} మాకు గుర్తొచ్చాడు. అంతా బాగుందా? | needs translation | We missed {child} today. Is everything OK? | needs review |
| `reason.ill` | needs translation | ఒంట్లో బాగాలేదు | needs translation | Unwell | needs review |
| `reason.travel` | needs translation | కుటుంబ ప్రయాణం లేదా పని | needs translation | Family travel or work | needs review |
| `reason.fest` | needs translation | పండుగ లేదా శుభకార్యం | needs translation | Festival or function | needs review |
| `reason.home` | needs translation | ఇంట్లో అవసరం | needs translation | Needed at home | needs review |
| `reason.other` | needs translation | ఇంకేదైనా | needs translation | Other | needs review |
| `reason.voice` | needs translation | మాట్లాడి చెప్పండి | needs translation | Say it instead | needs review |
| `reason.voice.disclosure` | needs translation | కంప్యూటర్ మీ మాట విని ఒక బొమ్మను ఎంచుకుంటుంది. ఆరోగ్య వివరాలు అడగము. | needs translation | A computer listens to pick a picture. We never ask for health details. | needs review |
| `reason.send` | needs translation | పంపండి | needs translation | Send | needs review |
| `reason.send.hint` | needs translation | ఒక బొమ్మను నొక్కండి | needs translation | Tap one picture | needs review |
| `reason.skip` | needs translation | వద్దు, వదిలేయండి | needs translation | Skip | needs review |
| `reason.thanks` | needs translation | ధన్యవాదాలు | needs translation | Thank you | needs review |
| `reason.private` | needs translation | ఇది లక్ష్మి టీచర్‌కు మాత్రమే కనిపిస్తుంది. | needs translation | Only Lakshmi teacher sees this. | needs review |
| `common.ok` | needs translation | సరే | needs translation | OK | needs review |
| `aria.chat.region` | needs translation | సందేశాలు | needs translation | Messages | needs review |
| `aria.evening.play` | needs translation | లక్ష్మి టీచర్ గొంతు వినండి | needs translation | Play Lakshmi teacher’s voice | needs review |
| `aria.reason.group` | needs translation | కారణం | needs translation | Reason | needs review |
| `aria.reason.region` | needs translation | అంతా బాగుందా? | needs translation | Is everything OK? | needs review |
| `aria.replay` | needs translation | మళ్ళీ వినండి | needs translation | Listen again | needs review |
| `aria.back` | needs translation | వెనక్కి | needs translation | Back | needs review |
| `announce.reason.sent` | needs translation | పంపాము. ధన్యవాదాలు. | needs translation | Sent. Thank you. | needs review |

"Lakshmi teacher" and "Ravi" are display values filled in the browser from the parent payload; they are never in prompts. The Telugu masculine verb ("గుర్తొచ్చాడు") needs a feminine form for girls: store a gendered pair or rephrase (native review). Kannada-home families get a kn version; kn is "needs translation" here.

## 6. Tokens and sizes
- Chat page `bg-canvas`; header `bg-surface` with teacher initial (decorative, `aria-hidden`).
- Bubble `bg-surface border border-border` (rule 3), radius 4/14 px, `text-body`; time `text-caption text-ink-muted` (prototype uses 12 px: raise to 14 px, B-22/C-23).
- Voice-note play 48 px circle `bg-primary text-on-primary` (C-23).
- Link buttons ("Is everything OK? Tell us", "Open", "Start them again"): `bg-panel border border-primary text-primary-strong min-h-target rounded-md w-full` (rule 1).
- Reason buttons: 2-column grid, gap 8 px, `bg-surface border-2 border-border rounded-md`, icon 34 px + `text-label`; pressed: `border-4 border-primary bg-panel` plus `aria-pressed="true"`. Prototype min height 104 px; meets NFR-04 (≥ 96) but not `size-target-picture` (120). Recommend `min-h-target-picture`.
- "Say it instead": ghost button `min-h-target`, mic icon, visually separate from the 5 reasons (C-15).
- Sticky footer `bg-canvas border-t border-divider`; Send `bg-primary text-on-primary min-h-target-primary w-full`; Skip text button `min-h-target` (C-14).
- Replay button 48 px, icon `text-primary-strong`, same position on every parent screen (C-16).

## 7. Acceptance checklist
- [ ] Every state above is built and reachable
- [ ] One primary action; works with one thumb at 360 px
- [ ] Status shown as icon + word, never colour alone
- [ ] Targets ≥ 48 px (picture choices 120 px); 8 px apart
- [ ] AI output carries the "AI-assisted" label (none in the evening message: no AI text; reason confirmation says a computer picked it)
- [ ] Every Indic text has a `lang` attribute; strings exist in kn, en, te, ta
- [ ] axe: 0 serious/critical, in light and dark
- [ ] `POST /api/classes/{id}/evening-messages/send` returns 409 when the `MessageTemplate` is not Approved, the lesson is not teacher-confirmed, the family has not opted in, or the child is stopped (FR-20, FR-19, T-44). One endpoint test per case.
- [ ] No `ILlmClient` call on the evening path: test with a fake `ILlmClient` that fails if invoked (FR-20).
- [ ] xUnit: slot filler from fixtures yields `{lesson}` (in the family's language) and `{page}` from the confirmed lesson and curriculum; there is no `{tomorrow}` slot (D-48); text equals the approved template with slots filled (D-21).
- [ ] The message and audio language follow the child's home language from T8 (D-47); the 6 pm job refuses and logs when a translation or audio clip for that language is missing.
- [ ] Evening audio contains no names: "your child" in the computer voice; names only in the written text and the teacher's own greeting (D-50). Audio ≤ 300 KB.
- [ ] The parent phone is used only by the send step; never in a prompt or AI call (D-43).
- [ ] Sent only after 18:00 IST (demo mode may send immediately, labelled).
- [ ] The buddy is never named in the evening message (D-29, C-30). Test: rendered text contains no other child's name.
- [ ] Catch-up bubble appears only after the package is Approved; withdrawing removes it in the demo (guardrail 3).
- [ ] Stop link in the evening bubble opens the P4 stop dialog (D-30, C-06).
- [ ] Stopped confirmation bubble with "Start them again" appears after a stop; restart reverses it (D-30, C-22).
- [ ] `POST /api/p/{token}/reason { category, clientRequestId }` stores the category only; idempotent.
- [ ] Voice route: audio → STT → A7; fake-LLM tests: invalid category → Other; low confidence → Other; transcript and audio not stored (T-45). 10 A7 eval cases.
- [ ] Parent confirms the A7 result ("Is it this?") before anything is stored (C-15).
- [ ] Skip returns to messages and never blocks P1–P4 (FR-21).
- [ ] Send is visible without scrolling at 360 × 640 (sticky, C-14).
- [ ] Polite live region announces "Sent. Thank you."
- [ ] Reason buttons are toggle buttons with `aria-pressed`, inside a labelled group.

## Open items

**Resolved 22 Oct:**
- Reason: picture buttons only in the MVP; voice note, "Is it this?" and A7 are cut (D-63).
- No STOP reply in the demo chat; the stop link covers FR-19 (D-64).

**Still open:**
- C-21 (gate, Rahul): how the teacher's voice reaches the family on WhatsApp; templates can't carry audio. Link to a voice page, or a separate session message. Blocks T-40 design, not the demo chat.
- STOP reply (FR-19, D-30) only exists on the real channel; the chat stand-in has no reply box. Decide whether the demo needs one.
- C-30 lock-screen preview: "We missed Ravi today" shows on a shared phone's lock screen. D-29 settled the buddy name only.
- C-16: prompts should play once on arrival (question read aloud); needs real audio.
- Resolved 21 Oct in the prototype: the delivered text now matches the approved template ("In class we learned …") and "Tomorrow" is gone (D-48).
- Still open (spec follow-up #4): lesson names in the message are in Telugu ("subtraction by taking a ten"), but `Competency` has only `NameKn` and `NameEn`. Add home-language names (te, ta) plus their pre-made lesson-name audio clips.
- `MessageTemplate` slots in spec §4.3 include `{buddy}`, which D-29 removed; `{child}` is used but not listed. Update the data model.
- FR-21 and T-45 say the reason step is "on the parent page"; the prototype reaches it from the evening message. Confirm whether it also appears in the catch-up flow.
- Voice-note recording, "Is it this?", mic permission denied, offline and send-error states for the reason are not designed.
- Telugu verb gender in `evening.body` and `reason.question` (native review).
