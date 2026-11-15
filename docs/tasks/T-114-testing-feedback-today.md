# T-114 · Testing feedback: fixed header, record until approved, Re-record, lesson audio on Approve

| | |
|---|---|
| Status | Proposed |
| Issue | #224 |
| Owner | M (flow and copy), R builds |
| Screens | app-shell, T1-today, T2-lesson-confirm, T3-approve |
| Source | Monika's testing, 15 Nov · Finale Should · changes D-55 (no second voice note on the same day) |

## What

Five fixes from Monika's run-through of the teacher flow on 15 Nov. **Proposed: needs Rahul's approval before building.** Every answer below is Monika's (15 Nov); one question is left for Rahul.

### 1. Fixed header

The header (`AppShell`) stays at the top while she scrolls, on **teacher screens only** (Today, Lesson confirm, Approve, Children, Week, Settings, Profile, sign-in). Family pages (`/p/...`) don't change.

### 2. Today keeps the record card until today's message is approved

The record card ("Tap and say what you taught today", the mic, Add a photo of the page, Pick from the list) no longer turns into the confirmed lesson card once she confirms.

- **Days with today's message:** the card stays on Today until today's message is approved. Once the lesson is confirmed, the card shows **one line above the mic: "Today: <lesson> · p <page>" with Change**. A new note recorded after confirming **replaces the confirmed lesson**, and today's message is drafted again from the new lesson. After approval, the card goes and Today shows the confirmed lesson only, as now.
- **Days without today's message** (no child away, or the lesson has no exercise list from the book): the card **stays all day**, with the same one line once confirmed, and a new note replaces the confirmed lesson, **until tonight's message has gone**. After the send, the card goes and Today shows the confirmed lesson only.
- **API:** today a second note after confirming gets 409 (D-55). It has to be accepted while today's message isn't approved, or on a day without today's message, until tonight's message has gone. After that the 409 stays. This changes D-55, so it needs a decision file.

### 3. Re-record on Lesson confirm, before she confirms

A **Re-record** button on Lesson confirm takes her back to Today's record card. She taps the mic to start, as the first time (it does **not** start by itself). The **new note replaces the first note and its match**. AI matches only the new note.

It shows in every state where she recorded or photographed something:

- **Sure match** and **Please choose**: in the "What you said" section (under the player and what AI heard).
- **Photo only** (the section reads "Your photo"): in that section.
- **Too unclear** (no "What you said" section today): next to the option to pick from the list.

### 4. "Also send today's lesson as audio" moves to Approve

The tick and the clips (`LessonAudioCard`, T-62) leave Tonight's card on Today. They go into **Approve → Today's message, as their own block after the message sections, just above "I've checked this" and Approve and send.** The rules stay the same: unticked each day, one clip per family language, she listens to each clip before approving it (guardrail 3), and it's read-only once sent.

**On a day without today's message, lesson audio is not offered.**

**[CONFIRM: Rahul]** Today's message already has a concept section with audio. Is this lesson audio a separate clip that goes with it, or does it duplicate the concept audio?

### 5. Header text

"Tappida Paatha" sits on the **same line** as the language dropdown and profile icon (centred with them), with the tagline under the name. The tagline loses "at home". **Kannada shows no tagline**: the name "ತಪ್ಪಿದ ಪಾಠ" already means "the missed lesson".

| Key | en | kn | te | ta | hi |
|---|---|---|---|---|---|
| `app.tagline` | The missed lesson | *(none)* | తప్పిన పాఠం | தவறிய பாடம் | छूटा हुआ पाठ |
| Re-record (new key) | Re-record | needs translation | needs translation | needs translation | needs translation |

te, ta and hi are the current taglines with "at home" removed.

## Acceptance criteria

- On teacher screens the header stays visible while scrolling at 360 px and nothing ends up hidden under it; family pages are unchanged.
- The app name is centred with the language dropdown and the profile icon; the tagline reads "The missed lesson" (none in Kannada); the header still fits one row at 360 px in all five languages (T-109).
- Today, with today's message: the record card shows until today's message is approved, with the confirmed lesson as one line and Change; after approval only the confirmed lesson shows.
- Today, without today's message: the record card shows until tonight's message has gone; then only the confirmed lesson; lesson audio is not offered.
- A new note after confirming (in the windows above) replaces the confirmed lesson; with today's message, the message is drafted again from the new lesson. Outside those windows a second note still gets 409.
- Lesson confirm shows Re-record in sure match, please choose, photo only and too unclear; it opens Today's record card with the mic not started; the new note replaces the first note and its match.
- The lesson audio block is on Approve → Today's message above the tick and Approve, and no longer on Today; a clip still goes only after she approves it.
- New strings have keys in all five locale files.

## How it's tested

- Endpoint tests (`FakeApi`): a second note is accepted before approval (with today's message) and before tonight's send (without), and replaces the lesson; 409 after approval and after the send; with today's message, a lesson change makes it drafted again.
- Vitest: Today's record card on a day with and without today's message (before confirm, after confirm with the one line, after approval or send); Re-record on Lesson confirm in all four states, landing on Today's record card with the mic idle; the lesson audio block on the Today's message tab and gone from Today; the header (sticky on teacher screens, tagline per language, none in kn).
- Existing e2e specs pass (no new spec). `verify.sh`.
- 360 px screenshots: Today scrolled (header fixed), Today after confirm (one line + mic), Lesson confirm with Re-record, Approve with the audio block, in kn and en, light and dark; axe on Today, Lesson confirm and Approve.

## Notes

- 15 Nov: proposed by Monika from testing (screenshots of the record card, Lesson confirm "What you said", and the audio tick on Today). Open points answered by Monika the same day: Re-record placement and states, first note replaced, recording after confirm replaces the lesson, the record card on days without today's message, audio placement, header scope, Kannada tagline, button label.
