# T0 Set up

Spec: FR-09 (greeting), FR-15 (consent), FR-20 (evening template), FR-02 (mic gesture) · Tier: MVP · Task: T-44, T-19, T-11 · Prototype: prototype/index.html (step 1; states via the chips under each phone)
Decisions: D-21, D-27, D-29, D-31, D-04, D-17

Four steps, once: Welcome → Step 1 of 2 greeting + voice consent → Step 2 of 2 evening-message template → All set. No bottom nav until set-up is done.

## 1. User voice

- Says: "I'll record it once if it means parents hear me, not a machine. But I won't type messages every evening."
- Unsaid: Is my voice going to be used for something else? Will an officer see these records? Will the app say things to parents in my name that I never checked?

## 2. Who does what

- Human: Lakshmi records a ≤ 10 s greeting, plays it back, re-records if needed, ticks voice consent, listens to the Telugu sample and approves the evening template (or "Not now").
- AI: none. The template is fixed text with slots filled by code (D-21, FR-20: "No AI writing"). Mode: none.
- Code (no AI): mic state machine and 10 s auto-stop; stores `Teacher.GreetingAudioUrl` and `Teacher.ConsentAt`; stores `MessageTemplate.Status = Approved`, `ApprovedAt`, `ApprovedByTeacherId`; renders the filled example from the seed (Ravi, Tuesday) and the Telugu sample from pre-synthesised segments; counts families opted in and families with no phone (`Child.ParentConsentAt`, `Child.HasPhone`).

## 3. Always / never

- Always: show the template as a filled example with marked slots, not `{child}` syntax (A-20). Let her hear the parent-language version before approving (A-21). Say plainly that nothing reaches a parent until she approves.
- Always: allow "Not now". With no approved template, no evening message is sent (FR-20: send endpoint 409).
- Never: send the greeting audio or the teacher's name to Claude or Sarvam (guardrail 1). Name the buddy in the template (D-29: "a classmate is keeping Ravi’s seat"). Promise anything about officers (A-29).
- Never: let "Save and continue" pass without a recorded greeting and the consent tick; say what is missing instead of disabling silently (A-19).
- Uncertainty: none (no AI on this screen). A recording under 1 s is not saved (same rule as T1).
- If it goes wrong: a bad greeting reaches every family → she can play it back and "Record again" before saving, and delete it later (consent text: "I can delete it any time"). An unapproved template → no evening messages, never a silent default.

## 4. States

| State | What the user sees | Copy (en) |
|---|---|---|
| Welcome (`0`) | Book icon, greeting, 3 promise cards (SATS, approval, privacy), sticky Start. No Back. | "Namaskara, Lakshmi ma’am" · "Start" |
| Greeting (`1`) | "Step 1 of 2", suggested words in a panel, 96 px mic, consent checkbox, sticky "Save and continue". Back visible. | "A 10-second greeting in your voice" |
| Recording (`1rec`) | Mic turns error-red with pulse and a stop square; waveform; timer "0:04 / 0:10"; hint. | "Tap again when you finish" |
| Recorded (`1done`) | Player card (play, progress bar, 0:09) with "Record again"; consent ticked. No PNG | "Record again" |
| Missing step (`1err`) | Error hint above the sticky button, `role="alert"`. | "Record your greeting first." / "Tick the box to agree." |
| Evening message (`2`) | "Step 2 of 2", filled example with underlined slots, "Your greeting" chip, "Listen to the Telugu version", sticky "Approve this message" + "Not now". | "Approve this message" |
| All set (`3`) | Large check, family counts, "Go to Today". No Back (A-30). | "All set" |
| Recording cancelled | Call, gesture cancel (`pointercancel`) or app switch stops recording; nothing saved; polite announcement | "Recording stopped. Nothing was sent." |
| Mic permission denied | Not in prototype (spec §3.1 lists it). Proposed: error banner, mic disabled | Proposed: "Can’t use the microphone. Nothing was recorded. Allow the microphone in phone settings, then try again." (needs Monika) |
| Greeting deleted | Not in prototype (spec §3.1 lists it). Returns to the Greeting state | none designed; see Open items |
| Offline | Not designed. Greeting and approval need to reach the server once | none designed; see Open items |
| Loading / Empty | none: content is static; counts come with the seed | — |

## 5. Copy

kn and en from the prototype. te "—" for teacher-only strings. Reviewed by: needs review (all rows).

| Key | kn | te | ta | en | Reviewed by |
|---|---|---|---|---|---|
| `setup.title` (app bar, region label) | ಪ್ರಾರಂಭ | — | needs translation | Set up | needs review |
| `common.back` (aria-label) | ಹಿಂದೆ | — | needs translation | Back | needs review |
| `setup.welcome.heading` | ನಮಸ್ಕಾರ, {teacher} ಮೇಡಂ | — | needs translation | Namaskara, {teacher} ma’am | needs review |
| `setup.welcome.lead` | ಗೈರುಹಾಜರಾದ ಮಕ್ಕಳು ನಿಮ್ಮ ತರಗತಿಯೊಂದಿಗೆ ಸಂಪರ್ಕದಲ್ಲಿರಲು ಇದು ಸಹಾಯ ಮಾಡುತ್ತದೆ. | — | needs translation | This helps absent children stay connected to your class. | needs review |
| `setup.welcome.sats` | ಹಾಜರಾತಿ SATS ನಿಂದ ಬರುತ್ತದೆ. ದಿನಕ್ಕೆ ಒಂದು ಧ್ವನಿ ಟಿಪ್ಪಣಿ ಮಾತ್ರ. | — | needs translation | Attendance comes from SATS. Just one voice note a day. | needs review |
| `setup.welcome.approval` | ನೀವು ಒಪ್ಪುವವರೆಗೆ ಯಾವುದೂ ಪೋಷಕರಿಗೆ ಹೋಗುವುದಿಲ್ಲ. | — | needs translation | Nothing reaches a parent until you approve it. | needs review |
| `setup.welcome.privacy` | ಈ ದಾಖಲೆಗಳನ್ನು ನೀವು ಮಾತ್ರ ನೋಡಬಹುದು. | — | needs translation | Only you can see these records. | needs review |
| `setup.welcome.start` | ಪ್ರಾರಂಭಿಸಿ | — | needs translation | Start | needs review |
| `setup.step1.progress` | ಹಂತ 1 / 2 | — | needs translation | Step 1 of 2 | needs review |
| `setup.greeting.heading` | ನಿಮ್ಮ ಧ್ವನಿಯಲ್ಲಿ 10 ಸೆಕೆಂಡು ಶುಭಾಶಯ | — | needs translation | A 10-second greeting in your voice | needs review |
| `setup.greeting.lead` | ಪ್ರತಿ ಸಂದೇಶ ನಿಮ್ಮ ಧ್ವನಿಯಿಂದ ಆರಂಭವಾಗುತ್ತದೆ. ಒಮ್ಮೆ ಮಾತ್ರ ದಾಖಲಿಸಿ. | — | needs translation | Every message starts with your voice. Record it once. | needs review |
| `setup.greeting.suggestLabel` | ಹೀಗೆ ಹೇಳಬಹುದು: | — | needs translation | You could say: | needs review |
| `setup.greeting.suggestion` | “ನಮಸ್ಕಾರ, ನಾನು {teacher} ಟೀಚರ್. ನಿಮ್ಮ ಮಗು ನಮಗೆ ನೆನಪಾಗುತ್ತಿದೆ. ಈ ಸಂದೇಶ ಕೇಳಿ.” | — | needs translation | “Namaskara, this is {teacher} teacher. We miss your child. Please listen to this message.” | needs review |
| `setup.greeting.idleHint` | ಒತ್ತಿ ಮಾತನಾಡಿ. ಮುಗಿದಾಗ ಮತ್ತೆ ಒತ್ತಿ. | — | needs translation | Tap and speak. Tap again when you finish. | needs review |
| `mic.record` (aria-label) | ದಾಖಲಿಸಿ | — | needs translation | Record | needs review |
| `mic.stop` (aria-label) | ದಾಖಲಿಸುವುದನ್ನು ನಿಲ್ಲಿಸಿ | — | needs translation | Stop recording | needs review |
| `mic.recHint` | ಮುಗಿದಾಗ ಮತ್ತೆ ಒತ್ತಿ | — | needs translation | Tap again when you finish | needs review |
| `mic.timer` | 0:{ss} / 0:{limit} | — | 0:{ss} / 0:{limit} | 0:{ss} / 0:{limit} | needs review |
| `setup.greeting.play` (aria-label) | ನಿಮ್ಮ ಶುಭಾಶಯ ಕೇಳಿ | — | needs translation | Play your greeting | needs review |
| `setup.greeting.again` | ಮತ್ತೆ ದಾಖಲಿಸಿ | — | needs translation | Record again | needs review |
| `setup.consent.voice` | ನನ್ನ ಧ್ವನಿಯನ್ನು ಪೋಷಕರಿಗೆ ಕಳುಹಿಸುವ ಸಂದೇಶಗಳಲ್ಲಿ ಮಾತ್ರ ಬಳಸಲು ಒಪ್ಪುತ್ತೇನೆ. ಯಾವಾಗ ಬೇಕಾದರೂ ಅಳಿಸಬಹುದು. | — | needs translation | I agree my voice is used only in messages to parents. I can delete it any time. | needs review |
| `setup.error.noGreeting` | ಮೊದಲು ಶುಭಾಶಯ ದಾಖಲಿಸಿ. | — | needs translation | Record your greeting first. | needs review |
| `setup.error.noConsent` | ಒಪ್ಪಿಗೆಗೆ ಗುರುತು ಹಾಕಿ. | — | needs translation | Tick the box to agree. | needs review |
| `setup.greeting.save` | ಉಳಿಸಿ, ಮುಂದೆ | — | needs translation | Save and continue | needs review |
| `announce.recording` | ದಾಖಲಿಸುತ್ತಿದೆ | — | needs translation | Recording | needs review |
| `announce.greetingSaved` | ಶುಭಾಶಯ ಉಳಿಸಲಾಗಿದೆ | — | needs translation | Greeting saved | needs review |
| `announce.recCancelled` | ದಾಖಲಿಸುವುದು ನಿಂತಿತು. ಏನೂ ಕಳುಹಿಸಿಲ್ಲ. | — | needs translation | Recording stopped. Nothing was sent. | needs review |
| `setup.step2.progress` | ಹಂತ 2 / 2 | — | needs translation | Step 2 of 2 | needs review |
| `setup.template.heading` | ಗೈರಾದ ದಿನದ ಸಂಜೆಯ ಸಂದೇಶ | — | needs translation | The evening message on a day of absence | needs review |
| `setup.template.lead` | ಇದನ್ನು ಒಮ್ಮೆ ಒಪ್ಪಿ. ಪ್ರತಿ ಸಂಜೆ ಗೆರೆ ಹಾಕಿದ ಪದಗಳು ನಿಮ್ಮ ದಾಖಲೆಯಿಂದ ತುಂಬುತ್ತವೆ. AI ಬರೆಯುವುದಿಲ್ಲ. | — | needs translation | Approve this once. Each evening the underlined words are filled from your records. No AI writing. | needs review |
| `setup.template.greetingChip` | ನಿಮ್ಮ ಶುಭಾಶಯ | — | needs translation | Your greeting | needs review |
| `template.evening.body` (shown to parents; slots underlined) | ಇಂದು {child} ನಮಗೆ ನೆನಪಾದರು. ಇಂದು ತರಗತಿಯಲ್ಲಿ {lesson} ಕಲಿತೆವು. ಮನೆಕೆಲಸ: ಪಠ್ಯಪುಸ್ತಕ {page}. ನಾಳೆ: {tomorrow}. ಒಬ್ಬ ಸಹಪಾಠಿ {child}ಯ ಜಾಗ ಕಾಯುತ್ತಿದ್ದಾರೆ. | ఈ రోజు {child} మాకు గుర్తొచ్చాడు. ఈ రోజు తరగతిలో: {lesson}. ఇంటి పని: పాఠ్యపుస్తకం {page}. రేపు: {tomorrow}. ఒక సహవిద్యార్థి {child} సీటు కాపాడుతోంది. (from the P0 bubble) | needs translation | We missed {child} today. In class we learned {lesson}. Homework: textbook {page}. Tomorrow: {tomorrow}. A classmate is keeping {child}’s seat. | needs review |
| `setup.template.exampleNote` | ಉದಾಹರಣೆ: ರವಿ, ಮಂಗಳವಾರ. ಪೋಷಕರು ಇದನ್ನು ತೆಲುಗು, ತಮಿಳು ಅಥವಾ ಕನ್ನಡದಲ್ಲಿ ಕೇಳುತ್ತಾರೆ. | — | needs translation | Example: Ravi, Tuesday. Parents hear it in Telugu, Tamil or Kannada. | needs review |
| `setup.template.listenTe` | ತೆಲುಗು ಆವೃತ್ತಿ ಕೇಳಿ | — | needs translation | Listen to the Telugu version | needs review |
| `setup.template.approve` | ಈ ಸಂದೇಶ ಒಪ್ಪಿ | — | needs translation | Approve this message | needs review |
| `setup.template.notNow` | ಈಗ ಬೇಡ · ನಂತರ ಬದಲಿಸಬಹುದು | — | needs translation | Not now · you can change it later | needs review |
| `setup.done.heading` | ಸಿದ್ಧ | — | needs translation | All set | needs review |
| `setup.done.summary` | {total} ರಲ್ಲಿ {agreed} ಕುಟುಂಬಗಳು ಸಂದೇಶಗಳಿಗೆ ಒಪ್ಪಿವೆ. {noPhone} ಕುಟುಂಬಗಳಿಗೆ ಫೋನ್ ಇಲ್ಲ: ಅವರ ಮಕ್ಕಳಿಗೆ ಮರಳಿದ ದಿನ ಬಡ್ಡಿ ಕಾರ್ಡ್. | — | needs translation | {agreed} of {total} families agreed to messages. {noPhone} families have no phone: their children get a buddy card on return. | needs review |
| `setup.done.goToday` | ಇಂದು ಗೆ ಹೋಗಿ | — | needs translation | Go to Today | needs review |

Slot values (lesson names, pages) come from `data/curriculum/*.json` (`NameKn`, `NameEn`, `TextbookPage`), not from locale files.

## 6. Tokens and sizes

- Page `bg-canvas`; cards `bg-surface border border-border rounded-md`; suggestion card `bg-panel border border-border` with `text-primary-strong` (rule 1: no `primary` text on `panel`).
- Screen title `text-title`; headings `text-title`; body `text-body`; hints and English captions `text-caption text-ink-muted` (never below 14 px Latin / 16 px Indic).
- Mic `size-target-mic rounded-full bg-primary text-on-primary`; recording `bg-error text-on-error` with pulse, off under `prefers-reduced-motion`.
- Primary button `min-h-target-primary w-full bg-primary text-on-primary rounded-md` in a sticky footer; "Record again", "Listen to the Telugu version" `min-h-target` ghost (`border-border bg-surface`); "Not now" text button `text-primary-strong min-h-target`.
- Error hint `text-error text-caption`, `role="alert"`. Consent row ≥ 48 px, checkbox 24 px with `accent-color: primary`.
- Slots in the example: underline + `bg-status-needs-buddy-bg text-status-needs-buddy` (prototype `.slot`); meaning is also carried by the underline, not colour alone.
- Focus ring: global 3 px `focus` with 2 px offset.

## 7. Acceptance checklist

- [ ] Every state above is built and reachable (7 TSTATES + cancelled + permission denied)
- [ ] One primary action per step; works with one thumb at 360 px; primary button sticky and never hidden by the keyboard (A-19, A-29)
- [ ] Status shown as icon + word, never colour alone
- [ ] Targets ≥ 48 px; 8 px apart; mic 96 px
- [ ] AI label: none needed; the template step says "No AI writing" (FR-20)
- [ ] Every Indic text has a `lang` attribute; strings exist in kn, en, te, ta; English captions carry `lang="en"` (A-24)
- [ ] axe: 0 serious/critical, in light and dark
- [ ] Mic: tap to start, tap to stop; press-and-hold also works; Enter/Space toggles; `aria-pressed` reflects recording; label switches Record / Stop recording (A-01, A-06, D-31)
- [ ] Timer shows elapsed vs 0:10; auto-stops at 10 s; `pointercancel` and `visibilitychange` (app switch) cancel and announce "Recording stopped. Nothing was sent." (A-05)
- [ ] Polite live region announces Recording, Greeting saved, Recording stopped (A-02)
- [ ] "Save and continue" with no greeting shows "Record your greeting first."; with no tick shows "Tick the box to agree."; both `role="alert"` (A-19)
- [ ] Template shows a filled example with underlined slots, no `{child}` syntax (A-20); the buddy is not named (D-29)
- [ ] "Listen to the Telugu version" plays code-filled, pre-synthesised audio; "Not now" saves nothing and no evening message is sent until approved (A-21, FR-20)
- [ ] `POST …/message-template/approve` records `ApprovedAt` and `ApprovedByTeacherId`, accepts `clientRequestId` and is idempotent
- [ ] Greeting upload and consent write accept `clientRequestId`; replays create one greeting
- [ ] Greeting audio and the teacher's name are never sent to Claude; `{teacher}` placeholder only (guardrail 1, NFR-05)
- [ ] Family counts on "All set" are computed in code from `Child` rows (guardrail 4)
- [ ] Back hidden on Welcome and on All set; focus moves to the step heading on each step change (A-30, A-04)

Open items:
- Mic permission denied, greeting deleted and offline states are not in the prototype; copy above is a proposal for Monika.
- Template wording differs between T0 en ("In class we learned subtraction with borrowing") and the P0 bubble en ("In class today: subtraction by taking a ten"); kn "ಹೇಳಿಕೆ ಲೆಕ್ಕಗಳು" differs from the curriculum name "ಹೇಳಿಕೆ ರೂಪದ ಸಮಸ್ಯೆಗಳು (ವ್ಯವಕಲನ)". Decide whether `{lesson}`/`{tomorrow}` use the curriculum name or a parent-friendly name field.
- te template is gendered (గుర్తొచ్చాడు, masculine; కాపాడుతోంది, feminine classmate). Fixed text with a `{child}` slot needs a gender-neutral form or a per-child variant.
- `MessageTemplate` in spec §4.3 still lists a `{buddy}` slot; D-29 removed the buddy's name. Update the data model.
- Where is the per-family opt-in recorded (FR-15, T-19)? "All set" shows counts but no screen captures parent consent.
- Where can she re-open the template or delete the greeting later ("you can change it later")? No settings screen is designed.
