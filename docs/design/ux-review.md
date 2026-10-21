# Tappida Paatha prototype: UX and accessibility review

20 Oct 2026. Review of the clickable prototype ([`prototype/index.html`](prototype/index.html)) before the per-screen requirements for Rahul are written.

## Method

1. **Automated scan.** axe-core on every screen and state, on both phones, light and dark: 67 captures, WCAG 2.2 A/AA + AAA rules.
2. **Expert review.** Three parallel reviews:
   - **A:** teacher setup, Today, lesson confirm.
   - **B:** teacher Approve, Missed lessons, Back today, Week, nav.
   - **C:** parent pages.
3. **Criteria:**
   - Nielsen's 10 heuristics.
   - UX laws: Fitts, Hick, Miller, Jakob, Tesler, Doherty, peak-end, goal-gradient, Von Restorff.
   - WCAG 2.2 AA, plus the project's AAA text contrast and 48 px targets (NFR-04).
   - Spec §3.4 copy rules and the AI guardrails in CLAUDE.md.
4. **Out of scope:** prototype-only simulation (toasts, fake audio) and the accuracy of the Kannada and Telugu wording (native review pending).

**Severity:**
- **P0:** blocks use, fails WCAG AA, or breaks a guardrail.
- **P1:** significant friction.
- **P2:** polish.

**Totals:** 93 findings. 19 P0, 52 P1, 22 P2.

## Automated scan

- **No contrast failures** at AAA, in light or dark.
- **3 rule failures:**
  - **Listen button label (2.5.3):** its name doesn't match the visible text.
  - **Print button (4.1.2):** no name.
  - **Scroll area (2.1.1):** not reachable by keyboard.
- **The scan missed one real contrast failure.** The English caption inside the big Listen button is grey on teal, about 1.1:1. It's confirmed by eye. Automated tools catch only part of the picture, which is why the expert review was needed.

## The 7 themes

### 1. Approval safety (guardrail 3): fix first

| ID | Issue | Fix |
|---|---|---|
| B-01 | "Approve the 2 ready" approves catch-ups she never opened, skipping the "I've checked the Kannada summary" tick | Bulk approve only for catch-ups already opened and ticked. Otherwise use a confirm sheet listing each one |
| B-02 | While one is still generating, the button still counts it as ready | Count from the status shown on screen. The API rejects anything not `Ready` |
| B-03 | After "Shorter" or "Simpler" the tick stays, and Approve is enabled for a script she hasn't seen | Any regenerate clears the tick. Tie approval to a script version |
| B-13 | No way to undo an approval | "Withdraw" until the 6 pm send |
| B-12 | The teacher's Kannada summary names one child (ರವಿಗೆ) in a 3-child catch-up | Use "the child" in the summary |

### 2. The parent must never get stuck

| ID | Issue | Fix |
|---|---|---|
| C-05 | "Next" is locked until the whole audio (~95 s) has played. A dropped network or a muted phone means no activity, no homework, no answer and no Stop | Next always enabled. Activity and homework never depend on audio |
| C-06 | "Stop these messages" exists only on the last screen | A quiet stop link on every parent screen and in the evening message. Accept a STOP reply |
| C-22 | After stopping there's no way back once the page is closed | One confirmation message in the chat with a restart link |

### 3. Accessibility foundations (apply to every screen)

| Area | Findings | Requirement |
|---|---|---|
| Mic gesture | A-01, A-06, A-05 | Same gesture for both mics: tap to start, tap to stop (holding also works). Enter/Space and TalkBack work. Auto-stop at the limit. `pointercancel` cancels. Show elapsed time |
| Dialogs | A-03, B-07, C-27 | Pick sheet, delete, stop: `role="dialog"`, `aria-modal`, focus moves in and is trapped, Esc/Back close it, focus returns to the opener |
| Announcements | A-02, C-08, B-14 | One polite live region per screen (saved, sending, sent, matched). Errors assertive |
| Focus | A-04, B-19, B-18, A-16 | Focus moves on purpose after screen changes. Never dropped to `<body>`, never clipped by `overflow:hidden`, never hidden behind sticky bars |
| Language | A-24, C-07 | `<html lang>` follows the UI language. English captions and Kannada key words carry their own `lang`. Every `aria-label` comes from i18next in all 4 locales |
| Names | C-01, B-04, A-15, B-15 | Accessible name matches the visible text. Icon buttons are named. Attendance buttons say "absent / present". Badge says "Approve, 3 waiting" |
| Text size | A-22, B-22, B-26, A-23 | Nothing under 14 px Latin / 16 px Indic (nav labels, tags, captions, badges are now 10.5–13 px). Tags and nav labels wrap. Test at 320 px and 200 % |
| Targets | C-04, C-23, B-17 | Answer pictures ≥ 120 px (now about 104 × 94). Voice-note play, read-aloud icon, transcript toggle, tabs and chips ≥ 48 px |
| Contrast | C-02, C-03, B-06 | Captions inside filled buttons use `on-primary`. SVG pictures use design tokens (the ones dots vanish in dark). The chart's last-week outline is invisible |

### 4. The learning check isn't valid yet

| ID | Issue | Fix |
|---|---|---|
| C-10 | A numeral sits under each picture, so the pictures add nothing | **[DECIDE: Monika]** under D-07: either the pictures are the answer (numerals hidden or shown after the tap), or the numeral is the answer and the pictures go |
| C-09 | The "hundred" is a 5 × 5 grid (25). 116 vs 106 differ by one thin rod. Flats and rods don't match the activity's bundles of sticks | One visual language: bundles of 10 sticks, and a bundle of 10 bundles for 100. Distractors that look clearly different |
| C-11 | The activity practises 43 − 18; the check asks 234 − 118 | The check comes from the same competency and number range as the activity |
| C-12 | The correct answer is always first | Shuffle positions in code and store the correct one |
| B-12 | Tamil catch-up targets "without borrowing", but its check needs borrowing | The validator rejects a check that doesn't match a target |
| C-13 | Pictures have no text equivalent | Name each option, e.g. "1 hundred, 1 ten, 6 ones, 116" |

### 5. Numbers must add up (guardrail 4)

| ID | Issue | Fix |
|---|---|---|
| B-09 | Week says "9 of 12 back within 2 days" but the bars give 7 + 3 = 10 (last week: 6 of 13 vs 9) | One query, a written definition, and a test that they reconcile |
| B-08 | One tag per child mixes learning with channel: "Stopped by family" hides "Caught up". By-lesson shows child-level status under every lesson | Status per child × lesson. Channel (no phone, stopped) is a separate field |
| B-06 | Last week is shown only as a faint outline, with no numbers | Both counts as text ("7 · last week 5"), side-by-side bars, and a table underneath |
| A-26 | "4 absent" but "message to 3 families" | Say it: "3 of 4 families · Manju: no phone, buddy card" |

### 6. Teacher control

| ID | Issue | Fix |
|---|---|---|
| A-09 | Once confirmed, she can't change today's lesson, so a wrong one feeds the ledger and tonight's message (FR-03 says "confirm or change") | A "Change" action on the lesson card |
| A-10 | Tonight's message goes automatically, with no skip | **Open gate (FR-20 [CONFIRM: Rahul]).** If built: a per-child "Skip tonight" until 6 pm |
| A-14 | Attendance corrections apply on each tap. No cancel | Edit a draft; Save commits, Back discards |
| A-07 | Tapping a lesson in the pick list commits it at once, though the chevron suggests "open" | Show it as a choice with a check and "Use this lesson", or show "Saved · Change" |
| A-05 | A recording sends on release, with no cancel or replay | A short "Sending… Undo" window, and replay of her note on the confirm screen |
| A-21 | She approves a message parents hear in Telugu or Tamil without hearing it | "Listen to the Telugu version", plus "Not now" |

### 7. Trust and tone

| ID | Issue | Fix |
|---|---|---|
| A-11 | The badge says "All sent" at 9 am, which reads like messages went to parents | Show the real outbox state: "Saved" / "3 waiting to send" |
| A-28 | "Done for today · 25 seconds" times the teacher | "Done for today. Thank you." |
| A-29 | "No officer sees them" brings officers to mind and over-promises | "Only you can see these records." |
| A-32 / B-15 | The Approve badge uses error red | A neutral or primary colour |
| B-20 | The printable buddy card shows "Stopped by family" to the buddy classmate | Never put family or channel status on the card |
| A-13 | "From SATS" isn't labelled simulated (D-27 requires it) | "SATS (simulated) · 9:05" in the MVP |
| C-30 | The evening message names a classmate to another family and shows "We missed Ravi" on a shared lock screen | **[DECIDE: Monika]** keep the buddy's name, or say "a classmate is keeping Ravi's seat" |
| C-15 | The "Say it" voice option goes to AI with no notice, and the family doesn't see the chosen category | Say a computer listens. Show "Is it this?" before sending |
| B-21 | The buddy card's sums and words have no AI label. No-phone children's card content was never approved | Sums from code. Label the AI wording. Define the approval path for no-phone children |

## Decisions needed before requirements are written

| # | Decision | Owner | Recommendation |
|---|---|---|---|
| 1 | Learning-check answer: pictures or numerals (C-10, D-07) | Monika | Pictures are the answer. The numeral shows after the tap as feedback |
| 2 | Bulk approve (B-01) | Monika | Allowed only for catch-ups she has opened and ticked |
| 3 | Name the buddy in the evening message (C-30) | Monika | "A classmate is keeping Ravi's seat"; name the buddy only on the teacher's card |
| 4 | Stop by replying STOP, plus a stop link everywhere (C-06) | Monika + Rahul | Yes to both |
| 5 | Same mic gesture: tap to start, tap to stop (A-06) | Monika | Yes; holding still works |
| 6 | Skip tonight's message (A-10) | Rahul (FR-20 gate) | Per-child skip until 6 pm |
| 7 | How the teacher's voice reaches the family on WhatsApp (C-21: templates can't carry audio) | Rahul | Link to a voice page, or a separate session message |

## What to keep (agreed by all three reviews)

- **Today:** one screen that follows her day: attendance from SATS, then the voice note, then what happens tonight, then updates.
- **Approval:**
  - "One approval per catch-up, not per child. Nothing is sent until you approve."
  - The tick names what she checked, and a failed check blocks approval, giving the cause in numbers ("104 seconds, limit 90").
- **Audio parts:** "Your voice · Notice · Lesson" plus the Kannada summary, key words and "Why this" (kept and dropped) answer D-09 well.
- **Error copy:** follows the "what happened, what's safe, what to do" rule throughout.
- **Numbers and labels:**
  - Counts, not percentages.
  - Status as icon plus word, in both themes.
  - Honest labels: "No AI in this", "AI writes only the wording", "Only for you".
- **Reason question:** caring framing, a visible Skip, "We never ask for health details".
- **No score or shame for the family:** "Well done" whatever the answer.
- **The ledger:** by child and by lesson. "Added to missed lessons" shows the result of her note at once.
- **Visual system:** dark theme designed, not inverted. Kannada-first with English captions. 96 px mic, 56 px primary buttons, a visible focus ring.

---

## Appendix: all findings

Full tables from the three reviews, unchanged apart from minor formatting. IDs: A = teacher daily, B = teacher weekly, C = parent.

The three reviews also produced acceptance criteria per screen. Those will go into the per-screen requirements for Rahul; they are summarised in the themes above.

### A. Teacher setup, Today, lesson confirm (32)

| ID | Screen | Sev | Principle | Issue → Recommendation |
|---|---|---|---|---|
| A-01 | T1 mic | P0 | 2.1.1, 2.5.1 | You can only record by holding; Enter/Space skips recording. → Tap to start/stop, keyboard toggle, `aria-pressed` |
| A-02 | T0–T2 | P0 | 4.1.3 | No live regions for recording, processing, saved or sync. → One polite region per screen |
| A-03 | Pick sheet | P0 | 4.1.2, 2.4.3, 2.1.2 | Not a modal dialog. → role, focus trap, Esc/Back, return focus |
| A-04 | All | P0 | 2.4.3 | Focus drops to `<body>` on every tap. → Stable elements, deliberate focus |
| A-05 | T1 mic | P1 | H3, H5 | No cancel or replay, no 30 s countdown, no `pointercancel`. → Timer, auto-stop, undo |
| A-06 | T0 vs T1 | P1 | H4, Jakob | Greeting is tap-toggle, lesson is hold. → One gesture |
| A-07 | Pick sheet, low confidence | P1 | 3.2.2 | Chevron rows commit on tap. → Choice + "Use this lesson" or "Saved · Change" |
| A-08 | Pick sheet | P1 | H1, Hick | "Another chapter" does nothing; no "next in book" first. → Wire it up; next lesson first |
| A-09 | T1 done | P1 | FR-03, H3 | Can't change a confirmed lesson. → "Change" on the card |
| A-10 | T1 tonight | P1 | Trust, FR-20 | No skip for tonight's send. → Gate; per-child skip |
| A-11 | T1 badge | P1 | H1, H2 | "All sent" before anything was sent. → Real outbox state |
| A-12 | T1 offline | P1 | §3.4 | Banner lists unqueued items; "3 waiting" vague. → "3 waiting to send"; design the queued-note state |
| A-13 | T1 attendance | P1 | D-27 | No "simulated" label. → "SATS (simulated)" |
| A-14 | T1 correcting | P1 | H3, H5 | Edits apply live; no cancel. → Draft + Save/Cancel |
| A-15 | T1 grid | P1 | 4.1.2 | "Pressed" doesn't say absent; ✕ is read aloud. → checkbox / label with state; visible word |
| A-16 | T1 grid | P1 | 2.4.11 | Sticky Save bar hides the last rows. → `scroll-padding-bottom` |
| A-17 | T2 low confidence | P1 | Von Restorff, H1 | Looks the same as confident; Confirm below the fold. → Warning style; choices above the fold |
| A-18 | T2 processing | P1 | Doherty, NFR-07 | No timeout or estimate. → After 20 s offer the list; "usually under 20 s" |
| A-19 | T0 greeting | P1 | 3.3.2 | Disabled Save below the fold, no reason. → Say what's missing; sticky footer |
| A-20 | T0 template | P1 | 1.3.3 | "The yellow parts", plus `{child}` syntax. → Filled example; "marked words" |
| A-21 | T0 template | P1 | Trust, H3 | Can't hear the parent-language version; no "Not now". → Listen sample; Not now; change later |
| A-22 | All | P1 | tokens type scale | Text 10.5–13 px. → ≥ 14/16 |
| A-23 | Badge, tags | P1 | 1.4.10, 1.4.12 | `nowrap` overflows. → Wrap; test 320 px, text spacing |
| A-24 | All | P1 | 3.1.2 | English `aria-label`s in a Kannada UI. → i18next names; `<html lang>` |
| A-25 | T1 | P2 | 3.2.4 | "See missed lessons" twice, different targets. → Name by destination |
| A-26 | T1 tonight | P2 | Guardrail 4 | 4 absent vs 3 families. → State the arithmetic |
| A-27 | T1 since yesterday | P2 | H1, Miller | Header counts listens only; no empty state. → "5 updates"; empty state |
| A-28 | T1 done | P2 | Trust | Shows seconds taken. → Drop it |
| A-29 | T0 welcome | P2 | Trust, Fitts | "No officer…"; Start below the fold. → "Only you can see"; sticky footer |
| A-30 | T0 all set | P2 | H2, H3 | Back after done; "Go to attendance". → Hide back; "Go to Today" |
| A-31 | T2 | P2 | H6 | Can't replay her note; button under the nav. → Play button; bottom padding |
| A-32 | Nav badge | P2 | tokens, 1.1.1 | Error red, no context. → Neutral; "Approve, 3 waiting" |

### B. Teacher Approve, Missed lessons, Back today, Week, nav (31)

| ID | Screen | Sev | Principle | Issue → Recommendation |
|---|---|---|---|---|
| B-01 | T3 list | P0 | Guardrail 3, D-09, 3.3.4 | Bulk approve skips review. → Only opened + ticked; confirm sheet; withdraw |
| B-02 | T3 generating | P0 | Guardrail 3 | Counts a generating catch-up as ready. → Count shown status; API rejects |
| B-03 | T3 regenerate | P0 | Guardrail 3 | Tick survives regenerate. → Clear tick; version-bound approval |
| B-04 | T5 card | P0 | 4.1.2 | Print button unnamed. → `aria-label` in 4 locales |
| B-05 | T6 / all | P0 | 2.1.1 | Scroll region not keyboard-reachable. → `tabindex`, role, label, or page scroll |
| B-06 | T6 chart | P0 | 1.1.1, 1.3.1, 1.4.11 | Last week invisible; no text; fixed scale. → Text counts; side-by-side bars; table |
| B-07 | T4 delete | P0 | 2.4.3, 4.1.2, 3.3.4 | Not a dialog; Delete most prominent. → Modal; focus on Keep; Delete secondary |
| B-08 | T4 ledger | P1 | FR-05, 1.3.1 | One tag mixes learning and channel. → Per child × lesson; separate channel field |
| B-09 | T6 headline | P1 | Guardrail 4 | Headline and bars don't reconcile. → One query + test |
| B-10 | T3 needs review | P1 | H9, Von Restorff | Approve greyed with no reason; tick still works. → Reason next to button; Shorter primary |
| B-11 | T3 list | P1 | Recognition | Same titles; "+1" unclear; no names. → Language + children first; both lessons |
| B-12 | T3 detail | P1 | D-09, guardrail 1 | Summary names one child; check doesn't match target. → "The child"; validator |
| B-13 | T3 approved | P1 | 3.3.4, H3 | No undo. → Withdraw until 6 pm |
| B-14 | T3 offline | P1 | §3.1, 4.1.3 | Queued, sent and failed states missing; Wi-Fi icon while offline. → Design them; live region |
| B-15 | Nav | P1 | 4.1.2, 1.4.1 | Badge red, "ಅನುಮೋದನೆ3"; nav unlabelled. → Neutral badge; names |
| B-16 | T4 tabs, expander | P1 | 4.1.2 | Tablist without panels; ✕ to collapse. → Proper tabs or `aria-pressed`; chevron + `aria-expanded` |
| B-17 | T4 tabs, chips | P1 | NFR-04 | 44 px and 36 px. → 48 px |
| B-18 | T4 lists | P1 | 2.4.7, 2.4.11 | Focus ring clipped. → Inset ring |
| B-19 | All | P1 | 2.4.3 | Focus lost on re-render. → Stable keys; deliberate focus |
| B-20 | T5 card | P1 | Privacy | "Stopped by family" on the printable card. → Teacher-only |
| B-21 | T5 card | P1 | NFR-08, guardrail 4 | Sums and words unlabelled; no-phone content unapproved. → Code sums; label; approval path |
| B-22 | All | P1 | tokens | Text below 14 px. → Type scale |
| B-23 | T5 carousel | P1 | 1.3.1, H1 | Position only as dots; disabled looks active. → "1 of 2"; disabled style |
| B-24 | T4 child | P1 | 3.3.4, H5 | Delete under the thumb; consequences unclear. → "Child's data" section; full copy; offline rule |
| B-25 | T4 offline | P2 | §3.4 | No "what's safe". → "Showing saved list…" |
| B-26 | Tags | P2 | 1.4.10, 1.4.12 | `nowrap`. → Wrap; test 200 % |
| B-27 | T4 child | P2 | 2.4.6, 1.3.1 | Generic title; no days on events; dots unexplained. → Name as heading; days; `<ol>` |
| B-28 | T3, T5, T6 | P2 | Jakob | Chips look tappable. → No borders |
| B-29 | T3 list | P2 | Goal-gradient, peak-end | No progress or finish line. → "All 3 approved…" |
| B-30 | T6 | P2 | Clarity | Shared icon; no date range; recap can't change; footer always shown. → Fix each |
| B-31 | T4, T5 | P2 | Affordance | No chevrons; empty state has no action; auto-advance. → Add; Undo |

### C. Parent pages (30)

| ID | Screen | Sev | Principle | Issue → Recommendation |
|---|---|---|---|---|
| C-01 | P1 | P0 | 2.5.3, 4.1.2 | Fixed English "Listen" label. → Visible text as name |
| C-02 | P1 | P0 | 1.4.3 / AAA | Caption in the big button ~1.1:1. → `on-primary` |
| C-03 | P3 dark | P0 | 1.4.11 | SVG colours hard-coded; dots vanish. → Tokens |
| C-04 | P3 | P0 | NFR-04, Fitts | Pictures ~104 × 94 px. → ≥ 120 px |
| C-05 | P1 | P0 | H3, data cost | Next locked until the audio ends. → Always enabled |
| C-06 | P4 | P0 | FR-19, trust | Stop only at the end. → Everywhere + STOP reply |
| C-07 | All | P0 | 3.1.1, 3.1.2 | `lang="en"`; Kannada inside Telugu. → Correct `lang` |
| C-08 | P3, P1, reason | P0 | 4.1.3 | No announcements. → status/alert roles |
| C-09 | P3 | P1 | Legibility, D-07 | 5 × 5 "hundred"; near-identical options. → Bundles; distinct distractors |
| C-10 | P3 | P1 | Validity | Numeral makes pictures pointless. → Decide (D-07) |
| C-11 | P2 → P3 | P1 | Match real world | Activity 43 − 18, check 234 − 118. → Same range |
| C-12 | P3 | P1 | FR-11 | Correct answer always first. → Shuffle |
| C-13 | P3 | P1 | 1.1.1 | Pictures have no text. → Page-language names |
| C-14 | Reason | P1 | Fitts, H1 | Send below the fold. → Sticky or send-on-tap with undo |
| C-15 | Reason "Say it" | P1 | Hick, privacy | Looks like a reason; AI not disclosed. → Separate style; "Is it this?" |
| C-16 | Reason, P2, P3 | P1 | D-08 audio-first | Prompts don't play by themselves. → Play once on arrival; same replay spot |
| C-17 | P2 | P1 | One action per screen | Activity and homework together; text-only page. → Activity first; textbook picture + "15" |
| C-18 | P1–P4 | P1 | Goal-gradient | No step indicator. → 4 dots, constant header |
| C-19 | P1 | P1 | Doherty | No buffering state or time. → < 400 ms feedback; time; back 10 s |
| C-20 | P1 dropped | P1 | H9, §3.4 | Resume fails offline. → Cache; auto-resume; full error copy |
| C-21 | Chat | P1 | Jakob, feasibility | Voice can't ride in a WhatsApp template. → Rahul to confirm route |
| C-22 | P4 stopped | P1 | FR-19 | No way back later. → Confirmation message with restart link |
| C-23 | Several | P1 | NFR-04 | 32–36 px targets. → 48 px |
| C-24 | P3 sending | P2 | H5 | Change works mid-send. → Disable or drop |
| C-25 | P1 vs P4 | P2 | Von Restorff, NFR-08 | Same star for AI and reward. → Separate AI icon |
| C-26 | P4 | P2 | Peak-end | Flat finish. → Filled star, sound, spoken line |
| C-27 | P4 stop | P2 | Dignity, 4.1.2 | Alarm-red confirm; not a dialog. → Neutral; modal |
| C-28 | P1 segments | P2 | Layout | Labels overflow. → Real-length segments; wrap |
| C-29 | P1 expired | P2 | H9 | Dead end. → Spoken; homework page; OK button |
| C-30 | Chat | P2 | Privacy | Buddy named to another family; lock-screen preview. → Decide wording |

---

## v2: what changed in the prototype (20 Oct)

[`prototype/index.html`](prototype/index.html) is v2. (The pre-review v1 is kept outside the repo.)

**Check after the changes:** axe-core on all 71 screens and states, light and dark: **0 violations** (v1 had 3, plus the caption contrast failure the scan missed).

**Decisions prototyped with the recommended option** (all still open for Monika to confirm):
1. The pictures are the answer; the number shows only after the child taps.
2. Bulk approve only covers catch-ups she has opened and ticked.
3. The evening message says "a classmate is keeping Ravi's seat"; the buddy is named only on the teacher's card.
4. A stop link on every parent screen and in the evening message.
5. Both mics: tap to start, tap to stop; holding also works.

### Fixed

| Area | Findings | What v2 does |
|---|---|---|
| Approval safety | B-01, B-02, B-03, B-13, B-10, B-12 | "Approve the N you checked" counts only opened-and-ticked catch-ups, using the status shown on screen. Regenerate clears the tick and disables Approve. Withdraw until 6 pm. A failed check hides the tick, says why next to the button, and makes "Make it shorter" primary. The summary says "the child" |
| Parent never stuck | C-05, C-06, C-22, C-29 | Next always enabled. A stop link on every parent screen and in the evening message. After stopping, a chat confirmation with "Start them again". The expired link shows the homework page, an OK button and a replay |
| Mic | A-01, A-05, A-06 | Same model for both mics: tap to start, tap to stop, hold also works, keyboard toggles. Timer against the limit, auto-stop, `pointercancel` and app switch cancel with a message, inline "too short" hint |
| Dialogs | A-03, B-07, C-27 | Pick sheet, delete and stop are modal dialogs: focus moves in and is trapped, Esc and scrim close them, focus returns. Delete defaults to Keep, with full consequences listed. Stop uses a neutral, not alarm, style |
| Announcements, focus, language | A-02, A-04, A-24, B-19, C-07, C-08 | Polite live region per phone. Focus is kept or moved on purpose (title on screen change). `lang` set per phone, English captions `lang="en"`, Kannada key words `lang="kn"`. All `aria-label`s follow the UI language |
| Names and semantics | C-01, B-04, B-05, A-15, B-15, B-16, C-13 | The Listen button's name is its visible text. Print is named. Scroll regions are focusable and labelled. Attendance tiles are checkboxes ("10 Kiran, absent") with a visible "absent" word. Badge reads "Approve, 3 waiting" in a neutral colour. Segmented control and expander use `aria-pressed` / `aria-expanded`. Answer pictures are named ("2 bundles of ten, 5 loose sticks") |
| Size and contrast | A-22, B-22, B-17, C-02, C-03, C-04, C-23, A-23, B-26 | No text below 14 px Latin (nav, tags, captions). Captions in filled buttons inherit the button colour. SVGs use tokens, so they work in dark. Answer tiles are full-width, at least 112 px high. All targets at least 48 px. Tags wrap. Nav labels wrap only at spaces, with shorter Kannada labels |
| Learning check | C-09, C-10, C-11, C-12, C-24 | Same bundles-of-sticks picture as the activity. Question matches the activity (43 − 18). The correct answer isn't first (stored index). Numeral shown only after the tap. "Change" removed (tap another picture) |
| Numbers | B-06, B-09, B-08, A-26 | Return speed reconciles (10 of 12 = 7 + 3; last week 9 of 13). Both weeks shown as numbers and bars, plus a hidden table. Learning and channel status are separate tags. "Message to 3 of 4 families" plus the no-phone child named |
| Teacher control | A-09, A-14, A-07, A-17, A-18, A-21, A-31 | "Change" on today's lesson. Attendance corrections edit a draft (Cancel / Save). Lesson lists are radio choices with "Next in the book" first. Low confidence is a warning card with both choices above the fold. Slow processing (> 20 s) offers the list. Template step adds "Listen to the Telugu version" and "Not now". She can replay her own note |
| Trust and tone | A-11, A-13, A-28, A-29, A-32, B-20, C-25, C-30 | Badge says "Saved" / "3 waiting to send". "SATS (simulated)". "Done for today. Thank you." with no timer. "Only you can see these records." The buddy card has no family or channel status. A separate AI icon (not the reward star). The classmate is not named in the evening message |
| Parent flow | C-14, C-15, C-16, C-17, C-18, C-19, C-20, C-26, C-28 | Sticky Send on the reason screen. "Say it instead" separated, with "a computer listens to pick a picture". A replay button in the same place on each step. Activity first, homework second. 4-step dots. Loading state and elapsed/total time. Full network-drop copy. Filled star and primary Finish. Audio segment labels fit |
| Smaller fixes | A-12, A-16, A-19, A-20, A-25, A-27, A-30, B-23, B-24, B-25, B-27, B-29, B-30, B-31 | Offline copy lists what's actually queued. Scroll padding keeps focus clear of sticky bars. Setup says what's missing. Underlined slots with a filled example. Buttons named by destination. "N updates" plus an empty state. No Back after setup is done. "Card 1 of 2 · next: Manju". Delete moved to a "Child's data" section. Offline ledger says what's safe. Child name as title, days on every event. "All 3 approved…" finish line. Date range, separate icons, recap can be changed. Chevrons, an empty-state action, Undo on buddy time |

### Not fixed (needs a decision or is outside a prototype)

| Finding | Why |
|---|---|
| A-10 Skip tonight's message | Open gate FR-20 [CONFIRM: Rahul]; not built past it |
| C-21 Teacher's voice on WhatsApp | Rahul to confirm the route (templates can't carry audio) |
| B-21 Approval path for no-phone children's buddy card | Sums come from code and key words from the textbook. Who approves any AI wording for them is still open |
| B-14 "Sent" and "Generation failed" tags on Approve | Need the real send job; queued state shown offline |
| A-05 "Sending… Undo" window after recording | Kept simple; replay on the confirm screen instead |
| C-16 Prompts auto-play on arrival; C-19 "back 10 s"; C-26 sound | Need real audio |
| A-08 "Another chapter" list | Placeholder in the prototype |
