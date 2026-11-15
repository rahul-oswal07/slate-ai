# T-114 · Testing feedback: fixed header, record until approved, record again, lesson audio on Approve

| | |
|---|---|
| Status | Proposed |
| Issue | #224 |
| Owner | M (flow and copy), R builds |
| Screens | app-shell, T1-today, T2-lesson-confirm, T3-approve |
| Source | Monika's testing, 15 Nov · Finale Should · reverses D-55 (no second voice note) · size M–L |

## What

Five fixes from Monika's run-through of the teacher flow on 15 Nov. **Proposed: needs Rahul's approval before building.** Items 1 and 5 are small and can go first; items 2–4 are the bigger part.

1. **The header stays at the top while she scrolls**, on every teacher screen (`AppShell`): sticky, on the page background, above the content. Anything that scrolls into view (skip link target, focused headings) isn't hidden under it.
2. **Today keeps the record card until today's message is approved.** Today the mic card ("Tap and say what you taught today", photo, pick from the list) turns into the confirmed lesson with Change once she confirms. Instead, it stays on Today, showing the lesson she confirmed, until today's message is approved. After that, Today shows the confirmed lesson only, as now. On a day with no today's message (no child away, or a lesson without the book's exercise list), it behaves as now: the card goes when the lesson is confirmed.
3. **Record again from Lesson confirm.** Under "What you said" (the player and what AI heard), a **Record again** button takes her back to Today's record card, with focus on the mic. This also covers "The note is too unclear to tell which lesson was taught". A new note replaces the earlier note and its match. The API now refuses a second note once the lesson is confirmed (409, D-55), so it has to accept one while today's message isn't approved. Then the new match replaces the confirmed lesson and today's message is drafted again. Once today's message is approved, the 409 stays.
4. **"Also send today's lesson as audio" moves to Approve.** The tick and the clips (`LessonAudioCard`, T-62) leave Tonight's card on Today and go into Approve → Today's message, next to the message she checks, approves and sends. The rules stay the same: unticked each day, one clip per family language, she listens to each clip before approving it (guardrail 3), and it's read-only once sent. **[CONFIRM: Rahul]** Today's message already has a concept section with audio. Should this lesson audio stay a separate opt-in, or become that concept audio? If there's no today's message that day, where does the opt-in go?
5. **Header text.** "Tappida Paatha" sits on the same line as the language dropdown and profile icon (centred with them), with the tagline under the name. The tagline loses "at home": **"The missed lesson"**, in all five locale files. In Kannada the app name already is "ತಪ್ಪಿದ ಪಾಠ" (the missed lesson), so **no tagline in Kannada** (Monika to confirm).

| Key | en | kn | te | ta | hi |
|---|---|---|---|---|---|
| `app.tagline` | The missed lesson | *(none)* | తప్పిన పాఠం | தவறிய பாடம் | छूटा हुआ पाठ |
| Record again (new key) | Record again | ಮತ್ತೆ ರೆಕಾರ್ಡ್ ಮಾಡಿ | మళ్ళీ రికార్డ్ చేయండి | மீண்டும் பதிவு செய் | फिर से रिकॉर्ड करें |

kn/te/ta/hi are drafts for native review (T-26).

## Acceptance criteria

- On every teacher screen the header stays visible while scrolling at 360 px; nothing ends up hidden under it.
- The app name is vertically centred with the language dropdown and the profile icon; the tagline reads "The missed lesson" (none in Kannada); the header still fits one row at 360 px in all five languages (T-109).
- Before today's message is approved, Today shows the record card with the confirmed lesson; after approval it doesn't.
- From Lesson confirm, Record again → Today's record card; a new note replaces the confirmed lesson and today's message is drafted again from it. An approved message is never left approved for a lesson that changed (guardrail 3).
- After today's message is approved, a second note still gets 409.
- The lesson audio opt-in and its clips are on Approve → Today's message and no longer on Today; a clip still goes only after she approves it.
- New strings have keys in all five locale files.

## How it's tested

- Endpoint tests (`FakeApi`): a second note before approval is accepted and replaces the match; after approval it gets 409; a lesson change marks today's message for drafting again and clears any approval.
- Vitest: Today's record card before and after approval, and on a day without today's message; Record again on Lesson confirm (matched and unclear) goes to Today with focus on the mic; the lesson audio card on the Today's message tab; the header (sticky class, tagline per language).
- Existing e2e specs pass (no new spec). `verify.sh`.
- 360 px screenshots: Today scrolled (header fixed), Lesson confirm with Record again, Approve with the audio, in kn and en, light and dark; axe on Today, Lesson confirm and Approve.

## Notes

- 15 Nov: proposed by Monika from testing, with screenshots of the record card, Lesson confirm ("What you said") and the audio tick on Today.
