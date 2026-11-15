# T-125 · Weekly star for families: a message and a badge page for children present every day

| | |
|---|---|
| Status | Proposed |
| Issue | #254 |
| Owner | M (flow, copy, WhatsApp template), R builds |
| Screens | a new family page (no folder yet), T0-setup, T6-week, delivery, demo |
| Source | Monika, 15 Nov · Finale, before Wed 18 Nov · changes D-81 (families see only the catch-up star; messages to present families were roadmap) |

## What

**Proposed: needs Rahul's approval before building.** Every point below is Monika's answer (15 Nov).

Today the weekly star (FR-24, D-81) is seen only by the teacher on Week, and families get nothing for coming to school. This sends the families of children who came **every day** a short message on Saturday evening, with a link to a star badge page.

### 1. Who gets it

- Children **present every school day** of the finished week (Mon–Sat; days with no register don't count, as in D-81), so **0 days away**. This is stricter than the Week star (at most 1 day away), and the Week star itself doesn't change.
- Only families with **consent Yes, not stopped, with a saved number**, the same rule as the absence messages (D-39, T-123).

### 2. When

**Saturday at 6 pm, with the evening job**, once Saturday's register is in (the week counts as finished then, as in `StarRules`).

### 3. Teacher approval (guardrail 3)

She approves the star wording **once in setup**: the WhatsApp text and the page text. Code then sends it every Saturday to the children who earned it, filling only `{child}` (and `{teacher}` on the page) from the register. A change to the wording asks for approval again (as D-49).

### 4. The page the link opens (two screens)

1. **Message:** "{child} came to school every day this week…" (text only, **no teacher voice**), and one button: **"Open to see your star badge"**.
2. **Badge card:** the star badge with the message, and **confetti: one short burst (about 2 s) when the card opens, then it rests** like the Done page (D-77). **No movement if the phone asks for reduced motion.** **No numbers** (no term total; D-81 stays on this).

As on every family page: the stop link, "Tappida Paatha" in the top bar, and the Back + title row (T-116).

### 5. Copy (page)

kn, te, ta and hi are drafts for native review (T-26).

| Key | en | kn | te | ta | hi |
|---|---|---|---|---|---|
| Screen 1 message | {child} came to school every day this week. Thank you for sending {child} to school every day. | {child} ಈ ವಾರ ಪ್ರತಿದಿನ ಶಾಲೆಗೆ ಬಂದಿದ್ದಾರೆ. ಪ್ರತಿದಿನ ಶಾಲೆಗೆ ಕಳುಹಿಸಿದ್ದಕ್ಕೆ ಧನ್ಯವಾದ. | {child} ఈ వారం ప్రతి రోజు బడికి వచ్చారు. ప్రతి రోజు బడికి పంపినందుకు ధన్యవాదాలు. | {child} இந்த வாரம் ஒவ்வொரு நாளும் பள்ளிக்கு வந்தார். தினமும் பள்ளிக்கு அனுப்பியதற்கு நன்றி. | इस हफ़्ते {child} की हर दिन स्कूल में उपस्थिति रही। हर दिन स्कूल भेजने के लिए धन्यवाद। |
| Screen 1 button | Open to see your star badge | ನಿಮ್ಮ ನಕ್ಷತ್ರ ಬ್ಯಾಡ್ಜ್ ನೋಡಲು ತೆರೆಯಿರಿ | మీ స్టార్ బ్యాడ్జ్ చూడటానికి తెరవండి | உங்கள் நட்சத்திர பேட்ஜைப் பார்க்கத் திறக்கவும் | अपना स्टार बैज देखने के लिए खोलें |
| Badge card | A star for {child}! For coming to school every day this week. | {child} ಗೆ ಒಂದು ನಕ್ಷತ್ರ! ಈ ವಾರ ಪ್ರತಿದಿನ ಶಾಲೆಗೆ ಬಂದದ್ದಕ್ಕೆ. | {child} కు ఒక నక్షత్రం! ఈ వారం ప్రతి రోజు బడికి వచ్చినందుకు. | {child} க்கு ஒரு நட்சத்திரம்! இந்த வாரம் ஒவ்வொரு நாளும் பள்ளிக்கு வந்ததற்காக. | {child} के लिए एक स्टार! इस हफ़्ते हर दिन स्कूल आने के लिए। |

### 6. WhatsApp template `weekly_star_v1` (Monika submits)

Test Messaging Account · Utility · Default · variable type Number · no header, footer or media · validity 12 h · `en`, `kn`, `te`, `ta`, `hi`. Each variable appears once. `{{1}}` = child, sample `Manjunath` in every language (the code sends the name in Latin letters). Button: **Visit website**, **Dynamic**, URL `https://slateai-staging.azurewebsites.net/p/` (44 characters, no typed `{{1}}`), sample URL `https://slateai-staging.azurewebsites.net/p/abc123`.

| Lang | Body | Button text |
|---|---|---|
| en | Class update – {{1}} came to school every day this week. Tap the button below to see the star badge. | See the star badge |
| kn | ತರಗತಿಯ ಮಾಹಿತಿ – {{1}} ಈ ವಾರ ಪ್ರತಿದಿನ ಶಾಲೆಗೆ ಬಂದಿದ್ದಾರೆ. ನಕ್ಷತ್ರ ಬ್ಯಾಡ್ಜ್ ನೋಡಲು ಕೆಳಗಿನ ಬಟನ್ ಒತ್ತಿ. | ನಕ್ಷತ್ರ ಬ್ಯಾಡ್ಜ್ ನೋಡಿ |
| te | తరగతి సమాచారం – {{1}} ఈ వారం ప్రతి రోజు బడికి వచ్చారు. స్టార్ బ్యాడ్జ్ చూడటానికి కింది బటన్ నొక్కండి. | స్టార్ బ్యాడ్జ్ చూడండి |
| ta | வகுப்புத் தகவல் – {{1}} இந்த வாரம் ஒவ்வொரு நாளும் பள்ளிக்கு வந்தார். நட்சத்திர பேட்ஜைப் பார்க்க கீழே உள்ள பொத்தானை அழுத்தவும். | நட்சத்திர பேட்ஜ் பார்க்கவும் |
| hi | कक्षा की जानकारी – इस हफ़्ते {{1}} की हर दिन स्कूल में उपस्थिति रही। स्टार बैज देखने के लिए नीचे का बटन दबाएँ। | स्टार बैज देखें |

Meta may class a star message as Marketing; the body keeps the "Class update – {{1}}" shape that passed as Utility.

### 7. Questions for Rahul

- **[CONFIRM: Rahul]** The link: a new kind of `/p/{token}` link so the template keeps the same base URL as the others?
- **[CONFIRM: Rahul]** The demo: the final is on Wed 18 Nov, so the demo Reset needs a finished week with a child present every day, and the send triggered at once (as Send messages now). How should that work?
- A new decision file: D-81 (4) and (6) change.

## Acceptance criteria

- On Saturday at 6 pm, after the register is in, each child present every day of the finished week, with consent Yes, not stopped and a saved number, gets one `weekly_star_v1` message; nobody else does; nothing goes twice for the same week.
- Nothing is sent until she has approved the star wording in setup; an edit asks again.
- The link opens screen 1 (message + "Open to see your star badge"), then the badge card with one confetti burst that then rests; no movement with reduced motion; no numbers anywhere.
- Stop link, top bar "Tappida Paatha" and the Back + title row as on the other family pages.
- New strings have keys in all five locale files.

## How it's tested

- Unit tests: who earns the family star (every day present; a day with no register; one day away gets none).
- Endpoint and worker tests (`FakeApi`): the Saturday send (approved wording only, consent and stop rules, once per week); the star page for its token.
- Vitest: screen 1 → badge card; confetti off with reduced motion; no numbers.
- Existing e2e specs pass (no new spec). `verify.sh`. 360 px screenshots of both screens in kn and en; axe.

## Notes

- 15 Nov: proposed by Monika. Today: the weekly star is at most 1 day away (D-81) and teacher-only on Week; families see only the catch-up star on Done, with still confetti (D-77); no family link or message for the weekly star exists.
