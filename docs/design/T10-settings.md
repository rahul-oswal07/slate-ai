# T10 Settings

Spec: FR-09 (greeting), FR-15 (consent), FR-20 (evening template) · Tier: MVP · Task: T-44, T-19, T-11 · Prototype: prototype/index.html (gear on T1 Today; TNAMES "T10 Settings", "T10 Settings · Evening message", "T10 Settings · My greetings") · PNG: none exported
Decisions: D-51 (Settings + Families rework), D-49 (evening message changed from Settings), D-44 (consent version, withdraw deletes recordings), D-52 (greeting per language), D-50 (no names in the computer voice), D-48 (no "Tomorrow"), D-43, D-47 (via Families)

Opened from the gear button on [T1 Today](T1-today.md); the Today tab stays active. A list of five things she set up once and may need again: Families and consent ([T8](T8-families.md)), Evening message, My greetings, Help, and App language (Kannada / English). Evening message and My greetings are sub-screens with Back to Settings.

## 1. User voice

- Says: "I said 'Not now' to the message on the first day. Where do I find it again? And I want to re-record the Telugu greeting, it sounded wrong."
- Unsaid: If I stop wanting my voice used, can I really take it back? Will the app quietly change the message without asking me?

## 2. Who does what

- Human: opens Families and consent; reviews the evening message and plays it in Telugu, Tamil or Kannada; asks to change the wording (needs approval again); plays, re-records, records or skips greetings per language; withdraws voice consent; switches the app language.
- AI: none. The evening message is fixed text with slots filled by code (FR-20). Mode: none.
- Code (no AI): summary lines (agreed and no-phone counts from T8; template status and approval date; "{n} of 3 recorded"); greeting status per language; consent version and date; withdraw deletes every greeting recording on the phone and the server and clears `ConsentAt`; any wording change sets the template back to not approved; app language stored on the device.

## 3. Always / never

- Always: show the template as a filled example with highlighted slots and say "No AI writes this message".
- Always: a changed wording needs her approval again before any evening message is sent (D-49).
- Always: withdrawing voice consent asks first, with "Keep" as the default focused action; withdrawing deletes all greeting recordings (D-44).
- Never: the computer voice says names; the audio says "your child" (D-50). Never voice cloning (D-52).
- Never: send greeting audio, her name or parent numbers to AI services (guardrail 1).
- Uncertainty: none (no AI).
- If it goes wrong: a mistaken withdraw deletes her greetings. Limited by the confirm dialog with Keep as default, plain consequences in the dialog, and the option to agree and record again later.

## 4. States

| State | What the user sees | Copy (en) |
|---|---|---|
| Settings (`settings` · `default`) | App bar "Settings" with Back (to Today); list of 4 rows with icon, title, summary line and chevron: Families and consent ("31 agreed · 5 no phone"), Evening message ("Approved · 20 Oct"), My greetings ("3 of 3 recorded"), Help ("How it works · who to contact"); section "App language" with a 2-way control ಕನ್ನಡ / English; bottom nav, Today active | "Settings" |
| Evening message (`tpl` · `default`) | Back to Settings; "Approved" tag + date; lead; filled template card with "Your greeting (in the family’s language)" chip and highlighted slots; note on names in audio; 3 ghost buttons to play Telugu, Tamil, Kannada; text button "Change the wording (needs approval again)" (D-49) | "Only the highlighted words change each evening, filled from your records. No AI writes this message." |
| My greetings (`greetings` · `default`) | Back to Settings; lead; Kannada (main) card with "Recorded" tag, Play, Record again; one card each for Telugu and Tamil (as in T0, D-52); "Consent version 1 · 20 Oct"; danger-outline "Withdraw voice consent" (D-44) | "Every message starts with your greeting in the family’s language. The computer voice never says names." |
| Withdraw: confirm (`greetings` · `withdraw`) | Bottom sheet dialog; title, consequences, primary "Keep" (focused), danger-outline "Withdraw and delete" (D-44) | "Withdraw voice consent?" · "All your greeting recordings are deleted. Messages go without your voice. You can agree and record again later." |
| Withdrawn | Greetings show "Not recorded"; polite announcement | "Consent withdrawn. Recordings deleted." |
| Help | Not designed; the prototype shows a toast "Opens help: how it works and who to contact" | see Open items |
| Template changed, awaiting approval | Not in prototype. Proposed: tag "Needs your approval" and no evening messages until approved | see Open items |
| Loading / Offline / Error | Not designed. Proposed: cached values with the T1 offline banner; greeting changes go to the outbox | see Open items |
| Permission denied (mic) | Recording a greeting here uses the T0 mic rules (D-34) | as T0 |

## 5. Copy

kn and en from the prototype. te "—" (teacher UI). Reviewed by: needs review (all rows). Greeting card strings (`greet.*`) are in [T0](T0-setup.md); Families strings in [T8](T8-families.md).

| Key | kn | te | ta | en | Reviewed by |
|---|---|---|---|---|---|
| `settings.title` (app bar, gear aria-label) | ಸೆಟ್ಟಿಂಗ್ಸ್ | — | needs translation | Settings | needs review |
| `settings.families` | ಕುಟುಂಬಗಳು ಮತ್ತು ಒಪ್ಪಿಗೆ | — | needs translation | Families and consent | needs review |
| `settings.families.sub` | {yes} ಒಪ್ಪಿದ್ದಾರೆ · {noPhone} ಫೋನ್ ಇಲ್ಲ | — | needs translation | {yes} agreed · {noPhone} no phone | needs review |
| `settings.evening` | ಸಂಜೆಯ ಸಂದೇಶ | — | needs translation | Evening message | needs review |
| `settings.evening.sub` | ಅನುಮೋದಿಸಲಾಗಿದೆ · {date} | — | needs translation | Approved · {date} | needs review |
| `settings.greetings` | ನನ್ನ ಶುಭಾಶಯಗಳು | — | needs translation | My greetings | needs review |
| `settings.greetings.sub` | {n} / {total} ದಾಖಲಾಗಿದೆ | — | needs translation | {n} of {total} recorded | needs review |
| `settings.help` | ಸಹಾಯ | — | needs translation | Help | needs review |
| `settings.help.sub` | ಇದು ಹೇಗೆ ಕೆಲಸ ಮಾಡುತ್ತದೆ · ಸಂಪರ್ಕ | — | needs translation | How it works · who to contact | needs review |
| `settings.appLanguage` | ಆಪ್ ಭಾಷೆ | — | needs translation | App language | needs review |
| `tpl.lead` | ಪ್ರತಿ ಸಂಜೆ ಬಣ್ಣ ಹಚ್ಚಿದ ಪದಗಳು ಮಾತ್ರ ನಿಮ್ಮ ದಾಖಲೆಯಿಂದ ಬದಲಾಗುತ್ತವೆ. ಈ ಸಂದೇಶವನ್ನು AI ಬರೆಯುವುದಿಲ್ಲ. | — | needs translation | Only the highlighted words change each evening, filled from your records. No AI writes this message. | needs review |
| `status.approved` (tag) | ಅನುಮೋದಿಸಿದ್ದೀರಿ | — | needs translation | Approved | needs review |
| `tpl.play` | {language} | — | needs translation | {language} (plays that version) | needs review |
| `tpl.change` | ಪದಗಳನ್ನು ಬದಲಿಸಿ (ಮತ್ತೆ ಒಪ್ಪಿಗೆ ಬೇಕು) | — | needs translation | Change the wording (needs approval again) | needs review |
| `greetings.lead` | ಪ್ರತಿ ಸಂದೇಶ ಕುಟುಂಬದ ಭಾಷೆಯ ನಿಮ್ಮ ಶುಭಾಶಯದಿಂದ ಆರಂಭವಾಗುತ್ತದೆ. ಕಂಪ್ಯೂಟರ್ ಧ್ವನಿ ಹೆಸರುಗಳನ್ನು ಹೇಳುವುದಿಲ್ಲ. | — | needs translation | Every message starts with your greeting in the family’s language. The computer voice never says names. | needs review |
| `greetings.kannadaMain` | ಕನ್ನಡ (ಮುಖ್ಯ) | — | needs translation | Kannada (main) | needs review |
| `greetings.notRecorded` | ದಾಖಲಾಗಿಲ್ಲ | — | needs translation | Not recorded | needs review |
| `greetings.play` | ಕೇಳಿ | — | needs translation | Play | needs review |
| `greetings.consentVersion` | ಒಪ್ಪಿಗೆ ಆವೃತ್ತಿ {v} · {date} | — | needs translation | Consent version {v} · {date} | needs review |
| `greetings.withdraw` | ಧ್ವನಿ ಒಪ್ಪಿಗೆ ಹಿಂಪಡೆಯಿರಿ | — | needs translation | Withdraw voice consent | needs review |
| `greetings.withdraw.title` | ಧ್ವನಿ ಒಪ್ಪಿಗೆ ಹಿಂಪಡೆಯಬೇಕೆ? | — | needs translation | Withdraw voice consent? | needs review |
| `greetings.withdraw.body` | ನಿಮ್ಮ ಎಲ್ಲಾ ಶುಭಾಶಯ ಧ್ವನಿಮುದ್ರಣಗಳು ಅಳಿಯುತ್ತವೆ. ಸಂದೇಶಗಳು ನಿಮ್ಮ ಧ್ವನಿ ಇಲ್ಲದೆ ಹೋಗುತ್ತವೆ. ನಂತರ ಮತ್ತೆ ಒಪ್ಪಿ ದಾಖಲಿಸಬಹುದು. | — | needs translation | All your greeting recordings are deleted. Messages go without your voice. You can agree and record again later. | needs review |
| `greetings.withdraw.keep` | ಬೇಡ, ಇರಲಿ | — | needs translation | Keep | needs review |
| `greetings.withdraw.confirm` | ಹಿಂಪಡೆಯಿರಿ ಮತ್ತು ಅಳಿಸಿ | — | needs translation | Withdraw and delete | needs review |
| `announce.consentWithdrawn` | ಒಪ್ಪಿಗೆ ಹಿಂಪಡೆಯಲಾಗಿದೆ. ಧ್ವನಿಮುದ್ರಣಗಳು ಅಳಿಸಲಾಗಿದೆ. | — | needs translation | Consent withdrawn. Recordings deleted. | needs review |

Language names in the App language control are shown in their own script (ಕನ್ನಡ, English) and are not translated.

## 6. Tokens and sizes

- Page `bg-canvas`; list `bg-surface border border-border rounded-md`; rows are buttons `min-h-[56px]`, icon 22 px, title `text-label`, summary `text-caption text-ink-muted`, chevron 18 px.
- App language: 2-way segmented control, each button `min-h-target`, pressed `bg-primary text-on-primary` with `aria-pressed`.
- Evening message: template card and slots as T0 (`.slot` underline + highlight); play buttons ghost `min-h-target`, 8 px apart, wrap.
- My greetings: cards as T0 language cards; "Withdraw voice consent" `border-error text-error` danger-outline, `min-h-target-primary`.
- Withdraw sheet: bottom sheet `bg-surface`, `role="dialog"` `aria-modal="true"`, labelled by its title; scrim closes it as Keep.

## 7. Acceptance checklist

- [ ] Every state above is built and reachable
- [ ] One primary action per screen; works with one thumb at 360 px
- [ ] Status shown as icon + word, never colour alone
- [ ] Targets ≥ 48 px; 8 px apart
- [ ] No AI on these screens; the evening message says "No AI writes this message"
- [ ] Every Indic text has a `lang` attribute; strings exist in kn, en, te, ta
- [ ] axe: 0 serious/critical, in light and dark
- [ ] Opened from the gear on T1 (aria-label "Settings"); Back returns to Today; Today tab has `aria-current="page"` on Settings and its sub-screens (D-51)
- [ ] Summary lines are computed in code: agreed and no-phone counts from T8, template status and date, greetings recorded out of the languages in the class (D-51, guardrail 4)
- [ ] "Families and consent" opens T8; Back from T8 returns to Settings (D-51)
- [ ] "Change the wording (needs approval again)" sets the template to not approved; no evening message is sent until she approves again; send endpoint returns 409 meanwhile (D-49, FR-20)
- [ ] Each play button plays that language's version, filled by code from pre-made pieces; no `{tomorrow}` slot (D-48); audio says "your child" (D-50)
- [ ] My greetings: per-language record / record again / skip / delete behave as on T0; skipped languages use the Kannada greeting + computer-voice introduction; never voice cloning (D-52)
- [ ] "Withdraw voice consent" opens a confirm dialog with focus on "Keep"; Escape or scrim = Keep; "Withdraw and delete" deletes every greeting recording on the phone and the server, clears `ConsentAt`, announces "Consent withdrawn. Recordings deleted." and messages go without her voice (D-44). Endpoint test with fakes
- [ ] Consent version and date shown come from the stored consent record (D-44)
- [ ] App language switches the teacher UI between Kannada and English at once and persists on the device; `<html lang>` follows it

## Open items

- Help content is not designed (prototype toast only). Proposed: a short "how it works" page and the school contact.
- The "Change the wording" editor is not designed (prototype toast only). Who reviews changed wording in te and ta before it is sent?
- The Evening message row and screen always show "Approved · 20 Oct" in the prototype, even after "Not now" on setup. A not-approved state ("Not approved yet") is needed on both.
- After withdrawing, the Kannada card still offers "Record again" (should be "Record") and recording again does not ask for consent again. Re-recording must require the consent tick (D-44).
- "Record again" on the Kannada card shows only a toast in the prototype; it should use the same recorder as T0.
- App language offers Kannada and English only; the prototype also has a "both" (Kannada + English captions) mode in the demo shell. Confirm it is demo-only.
- The meaning line under the Telugu and Tamil greeting cards is a hard-coded Kannada string in the prototype, not a K() call.
