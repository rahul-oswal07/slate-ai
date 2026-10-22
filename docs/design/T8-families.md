# T8 Families and consent + Family details

> **MVP scope (D-70): not in the MVP.** Consent, phone and home language come from the demo seed; the send filter (409 unless Yes and not stopped) is enforced in code. Family-details editing is roadmap. See [mvp-build-flow.md](mvp-build-flow.md).

Spec: FR-15 (opt-in), FR-19 (stop), FR-20 (evening message send rule) · Tier: MVP · Task: T-19 (consent + delete), T-44 (send filter) · Prototype: prototype/index.html (T0 "All set" → "Review families", T10 Settings → "Families and consent", or Missed lessons → "Families and consent"; tap a row for Family details) · PNG: none exported
Decisions: D-39 (parent opt-in screen), D-30 (families stop messages themselves), D-21 (evening message), D-35 (tag colours), D-43 (parent phone entered here), D-47 (home language), D-51 (list + Family details, back to where opened)

Two screens. **Families and consent** (list): one row per child with home language, consent and a masked phone number, and a chevron. **Family details** (tap a row): consent Yes / No / No phone from the paper form, the parent's phone number from the same form, and the home language that sets the language of the parent's messages and audio; Save (D-43, D-47, D-51). Nothing is sent to families marked No or No phone. Reached from T0 "All set" ("Review families"), from T10 Settings ("Families and consent") and from the bottom of T4 Missed lessons; Back returns to where it was opened (D-51). The active tab is Today when opened from Settings, Missed when opened from Missed lessons; no bottom nav when opened from setup.

## 1. User voice

- Says: "I collected the consent forms at the start of the year. Let me mark them once, copy the phone number from the form, and don't send anything to the families who said no."
- Unsaid: If a message reaches a family that said no, they will complain to the headmaster about me. Some families don't want the school to use their number. Will this list be shown to anyone else?

## 2. Who does what

- Human: the teacher opens a child, marks Yes, No or No phone from the paper form, types or corrects the parent's phone number, confirms or changes the home language (it comes with the roster, D-47), and taps Save. She changes it again when a family changes its mind. Families can also stop messages themselves from any parent screen or by replying STOP (D-30); she sees that as a tag, not a control.
- AI: none. Mode: none.
- Code (no AI): stores one consent value per child with the time and the teacher who changed it; stores the parent phone number (D-43) and the home language (D-47); masks the phone in the list (first 2 and last 3 digits); picks the message language and audio from the home language; the summary counts (agreed, said no, no phone) and their sum; the "Last changed" date; the send filter for evening messages and catch-up links (consent Yes and not stopped by the family); the counts on T0 "All set" and T1 tonight's card from the same data; the "Stopped by family" tag from the family's stop record; the No phone channel tag on T4.

## 3. Always / never

- Always: send nothing to a family marked No or No phone, and nothing to a family that stopped messages; the send endpoints refuse with 409, not only the UI (FR-15, FR-19, FR-20).
- Always: show a family's own stop as "Stopped by family" on that row. The teacher's Yes does not restart messages a family stopped; only the family can (D-30).
- Always: children marked No or No phone still get a buddy card on the day they come back (FR-12).
- Always: one value per child, pressed state visible as fill and `aria-pressed`, saved at once and announced.
- Never: show a full phone number in the list; the list shows it masked ("98•••••644"). The full number appears only in the Family details field.
- Never: send a parent phone number to AI services (Claude, Sarvam); it is used only to send messages (D-43, guardrail 1).
- Never: send names or numbers to Claude, Sarvam or WhatsApp templates (guardrail 1).
- Never: show or export this list to anyone but the teacher (spec §7).
- Uncertainty: none (no AI).
- If it goes wrong: a wrong tap sends messages to a family that said no, or stops messages to a family that agreed. Limited by: one visible control per row, a polite "Saved" after each change, the summary recount, the "Last changed" date, the change taking effect at the next 6 pm send, and the family's own stop link.

## 4. States

| State | What the user sees | Copy (en) |
|---|---|---|
| Default (`default`) | App bar "Families and consent" with Back; explanatory text; summary card with the three counts in bold and "Last changed"; list of all children, each a button: roll, first name, a line "Telugu · Yes · 98•••••644" (home language · consent · masked phone; no phone part when No phone), chevron (D-51). Nav: Missed active from Missed lessons, Today active from Settings, none from setup | "From the paper consent form. Nothing is sent to families marked No or No phone. Families can also stop messages themselves at any time." · "31 agreed · 0 said no · 5 no phone" · "Last changed: 18 Oct" |
| Family details (`family` · `default`) | App bar with the child's name and Back; "Stopped by family" tag if stopped; section "Messages (paper consent form)" with a 3-way control Yes / No / No phone and the note "A form not yet returned counts as No."; "Parent’s phone number" field with the note "Used only to send messages. Never shared with AI services."; "Home language (language of messages)" 3-way control Kannada / Telugu / Tamil; primary Save (D-43, D-47, D-51) | "Save" |
| Family details, No phone (`family` · `nophone`) | No phone pressed; the phone field is disabled with the placeholder "No phone" | "No phone" |
| Saved | Back on the list; the row and the summary recount; "Last changed: today"; polite announcement | "Saved" |
| Stopped by family | The row shows the "Stopped by family" tag under the name; the control keeps the paper answer (D-30) | "Stopped by family" |
| Offline | Not in the prototype. Proposed: the change goes to the Dexie outbox and the sync badge counts it, as on T1 | see Open items |
| Loading | Not designed. Show the cached list; no blank screen | — |
| Empty | none: the roster always has children | — |
| Error | Not designed (save rejected online) | see Open items |
| Permission denied | none: no mic or camera | — |

## 5. Copy

kn and en from the prototype. te "—" (teacher UI). Reviewed by: needs review (all rows).

| Key | kn | te | ta | en | Reviewed by |
|---|---|---|---|---|---|
| `families.title` (app bar) | ಕುಟುಂಬಗಳು ಮತ್ತು ಒಪ್ಪಿಗೆ | — | needs translation | Families and consent | needs review |
| `families.region` (aria-label) | ಕುಟುಂಬಗಳು | — | needs translation | Families | needs review |
| `families.lead` | ಕಾಗದದ ಒಪ್ಪಿಗೆ ಪತ್ರದಿಂದ. “ಬೇಡ” ಅಥವಾ “ಫೋನ್ ಇಲ್ಲ” ಎಂದಿರುವ ಕುಟುಂಬಗಳಿಗೆ ಏನೂ ಕಳುಹಿಸುವುದಿಲ್ಲ. ಕುಟುಂಬಗಳು ತಾವೇ ಯಾವಾಗ ಬೇಕಾದರೂ ನಿಲ್ಲಿಸಬಹುದು. | — | needs translation | From the paper consent form. Nothing is sent to families marked No or No phone. Families can also stop messages themselves at any time. | needs review |
| `families.summary` | {yes} ಒಪ್ಪಿದ್ದಾರೆ · {no} ಬೇಡ ಎಂದಿದ್ದಾರೆ · {noPhone} ಫೋನ್ ಇಲ್ಲ | — | needs translation | {yes} agreed · {no} said no · {noPhone} no phone | needs review |
| `families.lastChanged` | ಕೊನೆಯ ಬದಲಾವಣೆ: {date} | — | needs translation | Last changed: {date} | needs review |
| `families.row.detail` | {language} · {consent} · {maskedPhone} | — | needs translation | {language} · {consent} · {maskedPhone} | needs review |
| `family.consent.heading` | ಸಂದೇಶಗಳು (ಕಾಗದದ ಒಪ್ಪಿಗೆ ಪತ್ರ) | — | needs translation | Messages (paper consent form) | needs review |
| `family.consent.group` (aria-label) | ಒಪ್ಪಿಗೆ | — | needs translation | Consent | needs review |
| `family.consent.notReturned` | ಪತ್ರ ಇನ್ನೂ ಬರದಿದ್ದರೆ “ಬೇಡ” ಎಂದೇ ಪರಿಗಣಿಸುತ್ತೇವೆ. | — | needs translation | A form not yet returned counts as No. | needs review |
| `family.phone.label` | ಪೋಷಕರ ಫೋನ್ ನಂಬರ್ | — | needs translation | Parent’s phone number | needs review |
| `family.phone.note` | ಸಂದೇಶ ಕಳುಹಿಸಲು ಮಾತ್ರ. AI ಸೇವೆಗಳಿಗೆ ಎಂದೂ ಕಳುಹಿಸುವುದಿಲ್ಲ. | — | needs translation | Used only to send messages. Never shared with AI services. | needs review |
| `family.language.heading` | ಮನೆ ಭಾಷೆ (ಸಂದೇಶದ ಭಾಷೆ) | — | needs translation | Home language (language of messages) | needs review |
| `family.language.group` (aria-label) | ಮನೆ ಭಾಷೆ | — | needs translation | Home language | needs review |
| `lang.kn` / `lang.te` / `lang.ta` | ಕನ್ನಡ / ತೆಲುಗು / ತಮಿಳು | — | needs translation | Kannada / Telugu / Tamil | needs review |
| `family.save` | ಉಳಿಸಿ | — | needs translation | Save | needs review |
| `families.consent.yes` | ಹೌದು | — | needs translation | Yes | needs review |
| `families.consent.no` | ಬೇಡ | — | needs translation | No | needs review |
| `families.consent.noPhone` | ಫೋನ್ ಇಲ್ಲ | — | needs translation | No phone | needs review |
| `status.stopped` | ಕುಟುಂಬ ಸಂದೇಶ ನಿಲ್ಲಿಸಿದೆ | — | needs translation | Stopped by family | needs review |
| `announce.saved` | ಉಳಿಸಲಾಗಿದೆ | — | needs translation | Saved | needs review |
| `common.back` (aria-label) | ಹಿಂದೆ | — | needs translation | Back | needs review |
| `setup.done.reviewFamilies` (entry on T0) | ಕುಟುಂಬಗಳನ್ನು ನೋಡಿ | — | needs translation | Review families | needs review |
| `ledger.families` (entry on T4 and T10) | ಕುಟುಂಬಗಳು ಮತ್ತು ಒಪ್ಪಿಗೆ | — | needs translation | Families and consent | needs review |

Children's names come from `Child.FirstName`; the date comes from the latest consent change; the phone comes from the stored parent number, masked in code.

## 6. Tokens and sizes

- Page `bg-canvas`; lead `text-caption text-ink-muted`; summary card `bg-surface border border-border rounded-md`, counts `text-label` bold, "Last changed" `text-caption text-ink-muted`.
- List `bg-surface border border-border rounded-md`; rows are buttons `min-h-[56px]` with `border-b border-divider` and an 18 px chevron; roll `text-caption text-ink-muted`; name `text-label`; detail line `text-caption text-ink-muted`.
- Family details: section headings `text-label` bold; phone input `min-h-target w-full border border-border rounded-md bg-surface text-ink`, `type="tel"`, `inputmode="tel"`; disabled when No phone; notes `text-caption text-ink-muted`; Save `min-h-target-primary bg-primary text-on-primary`.
- Segmented control: `border border-border rounded-[10px]`, 3 buttons each `min-h-target min-w-target` (≥ 48 × 48 px), dividers `border-divider`; unpressed `bg-surface text-ink`; pressed `bg-primary text-on-primary` with `aria-pressed="true"`. Labels at `text-label` minimum (14 px Latin, 16 px Indic); the prototype's 13 px is below the token.
- "Stopped by family" tag: `text-status-stopped bg-status-stopped-bg` + `bell-off`, `rounded-sm` (D-35).
- Back button 48 px; Missed tab active in the bottom nav.

## 7. Acceptance checklist

- [ ] Every state above is built and reachable
- [ ] One action per row; works with one thumb at 360 px; the row wraps (name above the control) before anything is cut off
- [ ] Status shown as icon + word, never colour alone
- [ ] Targets ≥ 48 × 48 px for each segmented button (D-39); controls in neighbouring rows at least 8 px apart
- [ ] No AI on this screen; nothing labelled AI
- [ ] Every Indic text has a `lang` attribute; strings exist in kn, en, te, ta
- [ ] axe: 0 serious/critical, in light and dark
- [ ] Reached from T0 "All set", T10 Settings and T4; Back returns to the screen it was opened from; `aria-current="page"` on Missed from T4, on Today from Settings, no nav from setup; focus moves to the title on arrival (D-39, D-51, A-04)
- [ ] Each list row is a button showing "{language} · {consent} · {masked phone}" and a chevron; the masked phone shows the first 2 and last 3 digits; no phone part when No phone (D-51)
- [ ] Family details: consent and home language are `role="group"` controls with 3 buttons each, exactly one `aria-pressed="true"`; the phone field has a visible label and is disabled when No phone (D-43, D-47)
- [ ] Save writes consent, phone and home language together, returns to the list and announces "Saved"; the summary recounts in code and the three counts add up to the class size (guardrail 4)
- [ ] Changing the home language changes the language of that family's next evening message and catch-up audio (D-47)
- [ ] The parent phone is never in any AI call or log sent to Claude or Sarvam; it is used only by the send step (D-43, guardrail 1)
- [ ] `PUT /api/children/{id}/family { consent: Yes | No | NoPhone, parentPhone, homeLanguage, clientRequestId }`, idempotent; stores changed-at and the teacher; offline changes go to the Dexie outbox and replay once (T-19, NFR-02)
- [ ] "Last changed" shows the latest change date from the server and updates after a change
- [ ] A child with no consent form yet is stored as No: nothing is sent and the row shows No until the teacher changes it (D-39)
- [ ] Send filter: the evening-message and catch-up send endpoints return 409 for a child marked No or NoPhone, and for a family that stopped messages; endpoint tests with fakes (FR-15, FR-19, FR-20, T-44)
- [ ] T1 tonight's card counts only absent children marked Yes and not stopped, and lists absent children marked No phone or No with a tag; T0 "All set" counts come from the same data (D-39)
- [ ] A family's stop shows "Stopped by family" on its row within 2 s; setting Yes does not restart messages (D-30, FR-19)
- [ ] Children marked No or No phone still get a buddy card on return (FR-12)
- [ ] Phone numbers masked in the list; no names or numbers in any AI call (guardrail 1, D-43)
- [ ] Visible only to the authenticated teacher; no export (spec §7)

## Open items

**Resolved 22 Oct:**
- Yes without a phone number: Save is blocked with "Add the parent’s phone number, or choose No phone." (D-66).

**Still open:**
- "Last changed" switches to "today" after a change (resolved 21 Oct in the prototype); in the build it comes from the server.
- Segment labels are now 14 px (resolved 21 Oct in the prototype); tokens.md asks 14 px Latin / 16 px Indic. "ಫೋನ್ ಇಲ್ಲ" in a 3-way control beside a name at 360 px needs a layout check (name above the control, or a stacked row).
- Back returns to where T8 was opened (setup "All set", Settings or Missed lessons); no bottom nav when opened from setup (D-51). In the prototype, Family details opened from setup still shows the bottom nav while the list does not; Family details should follow the list.
- Segment labels and the 3-way control now live on Family details, so the 360 px crowding issue from the list is gone.
- Save is not validated in the prototype: Yes with an empty or malformed phone number is accepted. Proposed: Yes needs a 10-digit number; show an inline error.
- The list row for a No phone family ends with a dangling " · " in the prototype.
- Unsaved changes on Family details: Back discards silently in the prototype; consent taps save immediately (`cons` announces "Saved") while phone and language wait for Save. Pick one rule.
- A child whose form has not come back counts as **No** until it arrives, so nothing is sent without consent (FR-15). Decided by Monika, 21 Oct (D-39).
- A child marked No shows the "No consent" (ಒಪ್ಪಿಗೆ ಇಲ್ಲ) tag on T1 and T4 (resolved 21 Oct in the prototype). Tag word confirmed by Monika, 21 Oct.
- T4 channel tags follow this screen (resolved 21 Oct in the prototype).
- Data model: spec §4.3 has `Child.ParentConsentAt` and `Child.HasPhone`; a three-value consent with changed-at and changed-by, the parent phone (D-43) and the home language (D-47) need fields. Rahul to update the spec and `docs/database.md` with the T-19 migration.
- Offline and online-error states are not in the prototype.
