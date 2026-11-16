# T-125 · Weekly star: a fresh list each week of children present every day, a badge for their families, nothing counted

| | |
|---|---|
| Status | Done |
| Issue | #254 |
| Owner | M (flow, copy, WhatsApp template), R builds |
| Screens | P6-weekly-star, T0-setup, T6-week, P1-P4-catch-up, P5-todays-message, delivery, demo |
| Source | Monika, 15 Nov · Finale, before Wed 18 Nov · replaces most of D-81 (star rule, catch-up star, term totals, no family messages) |

## What

Approved by Rahul and built on 16 Nov ([D-118](../decisions/D-118-weekly-star-every-day.md)). Every point below is Monika's answer (15 Nov).

Today (D-81) the weekly star is "at most 1 day away", only the teacher sees it on Week, and stars add up over the term with a catch-up star counted for each catch-up answered. Families get nothing for coming to school. That means a child away one day can collect more stars than a child who came every day, and today's message tells the family "{child} has a star for today" while nothing counts it.

**New idea (Monika, 15 Nov):** the star is only about coming to school **every day of the week**. Each week is a fresh list. **Nothing is counted or carried forward.** The stars on the family Done pages are only a picture.

### 1. Who gets the weekly star

- Children **present every school day** of the week (Mon–Sat; days with no register don't count), so **0 days away**. The "at most 1 day away" rule (`Stars:MaxDaysAwayPerWeek`) goes, on the teacher's side too.
- **Each week stands alone:** no term total, no count, nothing carried from one week to the next.
- The family message goes only to families with **consent Yes, not stopped, with a saved number**, the same rule as the absence messages (D-39, T-123).

### 1a. No counting anywhere

- **Week (teacher):** the stars card lists, in roll order, **that week's** children present every day. **No numbers and no term totals.** While the week is still running it shows the children **on track so far** (present every day up to today), titled as this week's. An earlier week shows its own list.
- **Catch-up star:** no longer counted. The catch-up Done page keeps its star picture and its words ("{child} earned a star for doing this catch-up").
- **Today's message star:** stays a picture. The Done page keeps "{child} has a star for today".
- The term-total code (`StarChild.Attendance`, `CatchUps`, `Total`) and its strings go.

### 2. When

**Saturday at 6 pm, with the evening job**, once Saturday's register is in (the week counts as finished then, as in `StarRules`).

### 3. Teacher approval (guardrail 3)

She approves the star wording **once in setup**: the WhatsApp text and the page text. Code then sends it every Saturday to the children who earned it, filling only `{child}` (and `{teacher}` on the page) from the register. A change to the wording asks for approval again (as D-49).

### 4. The page the link opens (two screens)

1. **Message:** "{child} came to school every day this week…" (text only, **no teacher voice**), and one button: **"Open to see your star badge"**.
2. **Badge card:** the star badge with the message, and **confetti: one short burst (about 2 s) when the card opens, then it rests** like the Done page (D-77). **No movement if the phone asks for reduced motion.** **No numbers.**

As on every family page: the stop link, "Tappida Paatha" in the top bar, and the Back + title row (T-116).

### 5. Copy (page)

kn, te, ta and hi are drafts for native review (T-26).

| Key | en | kn | te | ta | hi |
|---|---|---|---|---|---|
| Screen 1 message | {child} came to school every day this week. Thank you for sending {child} to school every day. | {child} ಈ ವಾರ ಪ್ರತಿದಿನ ಶಾಲೆಗೆ ಬಂದಿದ್ದಾರೆ. ಪ್ರತಿದಿನ ಶಾಲೆಗೆ ಕಳುಹಿಸಿದ್ದಕ್ಕೆ ಧನ್ಯವಾದ. | {child} ఈ వారం ప్రతి రోజు బడికి వచ్చారు. ప్రతి రోజు బడికి పంపినందుకు ధన్యవాదాలు. | {child} இந்த வாரம் ஒவ்வொரு நாளும் பள்ளிக்கு வந்தார். தினமும் பள்ளிக்கு அனுப்பியதற்கு நன்றி. | इस हफ़्ते {child} की हर दिन स्कूल में उपस्थिति रही। हर दिन स्कूल भेजने के लिए धन्यवाद। |
| Screen 1 button | Open to see your star badge | ನಿಮ್ಮ ನಕ್ಷತ್ರ ಬ್ಯಾಡ್ಜ್ ನೋಡಲು ತೆರೆಯಿರಿ | మీ స్టార్ బ్యాడ్జ్ చూడటానికి తెరవండి | உங்கள் நட்சத்திர பேட்ஜைப் பார்க்கத் திறக்கவும் | अपना स्टार बैज देखने के लिए खोलें |
| Badge card | A star for {child}! For coming to school every day this week. | {child} ಗೆ ಒಂದು ನಕ್ಷತ್ರ! ಈ ವಾರ ಪ್ರತಿದಿನ ಶಾಲೆಗೆ ಬಂದದ್ದಕ್ಕೆ. | {child} కు ఒక నక్షత్రం! ఈ వారం ప్రతి రోజు బడికి వచ్చినందుకు. | {child} க்கு ஒரு நட்சத்திரம்! இந்த வாரம் ஒவ்வொரு நாளும் பள்ளிக்கு வந்ததற்காக. | {child} के लिए एक स्टार! इस हफ़्ते हर दिन स्कूल आने के लिए। |

### 6. WhatsApp template `weekly_star_v1` (Monika submits)

**Status (15 Nov): English submitted, in review at Meta, as Utility. kn, te, ta, hi not submitted yet (Monika submits them later).** Until a language is approved, nothing goes over WhatsApp in it.

Test Messaging Account · Utility · Default · variable type Number · no header, footer or media · validity 12 h · `en`, `kn`, `te`, `ta`, `hi`. Each variable appears once. `{{1}}` = child, sample `Manjunath` in every language (the code sends the name in Latin letters). Button: **Visit website**, **Dynamic**, URL `https://slateai-staging.azurewebsites.net/p/` (44 characters, no typed `{{1}}`), sample URL `https://slateai-staging.azurewebsites.net/p/abc123`.

| Lang | Body | Button text |
|---|---|---|
| en | Class update – {{1}}. Weekly attendance: present every school day this week. Tap the button below to open the update. | Open the update |
| kn | ತರಗತಿಯ ಮಾಹಿತಿ – {{1}}. ವಾರದ ಹಾಜರಾತಿ: ಈ ವಾರ ಎಲ್ಲಾ ಶಾಲಾ ದಿನಗಳಲ್ಲೂ ಹಾಜರು. ಮಾಹಿತಿ ತೆರೆಯಲು ಕೆಳಗಿನ ಬಟನ್ ಒತ್ತಿ. | ಮಾಹಿತಿ ತೆರೆಯಿರಿ |
| te | తరగతి సమాచారం – {{1}}. వారపు హాజరు: ఈ వారం అన్ని బడి రోజులలో హాజరు. సమాచారం తెరవడానికి కింది బటన్ నొక్కండి. | సమాచారం తెరవండి |
| ta | வகுப்புத் தகவல் – {{1}}. வார வருகை: இந்த வாரம் எல்லா பள்ளி நாட்களிலும் வருகை. தகவலைத் திறக்க கீழே உள்ள பொத்தானை அழுத்தவும். | தகவலைத் திறக்கவும் |
| hi | कक्षा की जानकारी – {{1}}। साप्ताहिक उपस्थिति: इस हफ़्ते स्कूल के हर दिन उपस्थित। जानकारी देखने के लिए नीचे का बटन दबाएँ। | जानकारी खोलें |

**15 Nov:** the first wording ("…came to school every day this week. Tap the button below to see the star badge.") was flagged by Meta as Marketing ("This message template will be rejected" as Utility). Monika chose to keep Utility: the WhatsApp text is a factual weekly attendance update, and the star badge appears only on the page. The rewritten English passed Meta's category check and was submitted under the same name, `weekly_star_v1`.

### 7. Questions for Rahul

- The link (was `[CONFIRM: Rahul]`): **yes**, a new kind of `/p/{token}` link (`kind: "weeklyStar"`), so the template keeps the same base URL as the others (D-118).
- The demo (was `[CONFIRM: Rahul]`): Reset already seeds finished weeks with children present every day; **Send messages now** also sends the stars for the latest finished week, once she has approved the wording (D-118).
- Done: [D-118](../decisions/D-118-weekly-star-every-day.md), replacing D-81 (1) the rule (at most 1 day away → every day), (2) the catch-up star, (3) stars adding up over the term, (4) the class view's totals and families seeing only the catch-up star, and (6) messages to present families being roadmap. D-81 (5) stays: code only, no AI, nothing stored.

## Acceptance criteria

- The weekly star is earned only with every school day present (0 away); a day with no register doesn't count; one day away earns none.
- Week shows, for the week on screen, the children present every day (finished) or on track so far (current), in roll order, with no numbers or totals anywhere; an earlier week shows its own list.
- Catch-up and today's message answers add nothing to any count; both Done pages keep their star picture and words.
- On Saturday at 6 pm, after the register is in, each child present every day of the finished week, with consent Yes, not stopped and a saved number, gets one `weekly_star_v1` message; nobody else does; nothing goes twice for the same week.
- Nothing is sent until she has approved the star wording in setup; an edit asks again.
- The link opens screen 1 (message + "Open to see your star badge"), then the badge card with one confetti burst that then rests; no movement with reduced motion; no numbers anywhere.
- Stop link, top bar "Tappida Paatha" and the Back + title row as on the other family pages.
- New strings have keys in all five locale files.

## How it's tested

- Unit tests (`StarRules`): every day present earns it; a day with no register; one day away earns none; current week on track; no totals and no catch-up count in the result.
- Vitest: the Week stars card for a finished week, the current week and an earlier week, with no numbers.
- Endpoint and worker tests (`FakeApi`): the Saturday send (approved wording only, consent and stop rules, once per week); the star page for its token.
- Vitest: screen 1 → badge card; confetti off with reduced motion; no numbers.
- Existing e2e specs pass (no new spec). `verify.sh`. 360 px screenshots of both screens in kn and en; axe.

## Notes

- 16 Nov 2026: **built** (D-118 accepted by Rahul, with the link and demo answers in §7).
  - **Rule:** `StarRules` lists children present on every school day (a register day without their row loses it); no `Attendance`/`CatchUps`/`Total`, no `Stars:MaxDaysAwayPerWeek`. Week's card shows names in roll order with no counts.
  - **Approval:** `data/seed/weekly-star.json` v1 (WhatsApp text, message, badge; Monika's copy) approved once per class (`WeeklyStarApprovals`) at `/setup/star` (setup step 3, after the evening message) or `/settings/star`. A new version asks again.
  - **Send:** `WeeklyStarSender` from the 6 pm job on Saturday once the register is in, and on Sunday (a second Saturday has no school). Consent Yes, not stopped, once per child per week (`WeeklyStarMessages`, unique `(ChildId, WeekOf)`, outbox row in the same transaction). Migration `WeeklyStarMessages` (third source on `ParentDeliveries`).
  - **WhatsApp:** `weekly_star_v1` with `{{1}}` = the name, but **no language is switched on** until Meta approves it (English in review). Then `WhatsApp__WeeklyStar__Languages__en=en`.
  - **Page (P6):** message + "Open to see your star badge" → badge card (`?step=badge`) with a ~1.7 s confetti burst that rests; reduced motion off via the global rule. Stopped → stopped page; 14 days → 410.
  - **Demo:** Send messages now also sends the latest finished week's stars; the family phone lists "Weekly star · week of …".
  - **Tests:** `StarRulesTests` rewritten; `WeeklyStarTests` (sender: who, when, approval, once, Sunday, demo; endpoints; the page); `WhatsAppTests`; Vitest for the Week card, the approval screen, the page and the setup chain. `verify.sh`, the Db suite and e2e run (see the PR).
  - **Left:** switch on `weekly_star_v1` per language once Meta approves it; native review of the kn/te/ta/hi page and setup copy (T-26); a design card for P6 and the setup step (Monika). `CLAUDE.md` guardrail 3 now names the weekly star.

- 15 Nov: star concept rethought by Monika: every day present only, a fresh list each week, nothing counted or carried forward (no term totals, no catch-up star count); the Done-page stars stay as pictures with their words; Week shows that week's list, on track mid-week, its own list for an earlier week.
- 15 Nov: `weekly_star_v1` English submitted (in review), as a weekly attendance update after Meta flagged the star wording as Marketing; other languages later.
- 15 Nov: proposed by Monika. Today: the weekly star is at most 1 day away (D-81) and teacher-only on Week; families see only the catch-up star on Done, with still confetti (D-77); no family link or message for the weekly star exists.
