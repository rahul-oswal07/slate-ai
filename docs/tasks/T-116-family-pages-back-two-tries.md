# T-116 · Family pages: Back and a title on every step, two tries on the answer

| | |
|---|---|
| Status | Proposed |
| Issue | #232 |
| Owner | M (flow and copy), R builds |
| Screens | P0-evening-message, P1-P4-catch-up, P5-todays-message, T1-today, T4-missed-lessons |
| Source | Monika's testing, 15 Nov · Finale Should · changes D-62 (first answer counts, reopen goes to Done) and D-95 (three levels), and T-90's "look again" |

## What

From Monika's testing of the family pages on 15 Nov (screenshot: Done on today's message in the WhatsApp browser, top bar "Is everything OK?"). **Proposed: needs Rahul's approval before building.** Every point below is Monika's answer (15 Nov).

### 1. Top bar says "Tappida Paatha"

The page title (what the WhatsApp browser shows in its top bar) is always **"Tappida Paatha"**, in every language, on every family page. On staging it showed "Is everything OK?" in English on the Done step of a Kannada page. `index.html` has "Tappida Paatha", so find what sets the other title.

### 2. Back and the step's title on every step

On every family page (**today's message, the evening message, the catch-up**), each step starts with one row, under the top bar and **above** "<teacher> ಟೀಚರ್ ಅವರಿಂದ ಸಂದೇಶ": **‹ Back + the step's title**. The title moves up into that row and is not repeated below.

```
┌─ Tappida Paatha ───────────────┐
│ ‹ Back   Activity at home      │
│ Shobha ಟೀಚರ್ ಅವರಿಂದ ಸಂದೇಶ      │
│ //// 2/4                       │
│  1. ...                        │
│ [          Next          ]     │
└────────────────────────────────┘
```

- **No Back on the first step** (the teacher's voice; Listen on the catch-up). The title row is still there.
- **Done has Back, to the answer page**: open if a try is left, locked if not.
- Back goes to the previous step the family saw (removed sections stay skipped).

### 3. The answer: choose, submit, two tries, no right or wrong

The same rule on **today's picture question (>, <, =)** and the **catch-up answer page**:

- **Choose, then submit:** tapping a sign or picture only chooses it (it can be changed); a submit button sends it. Today's picture question now sends on the tap; it changes to work like the catch-up.
- **No right or wrong is shown to the child.** Today's "look again" after a wrong first tap goes.
- **At most two submits.** After the first, the answer page shows **Try again** next to Next. After the second, the answer is locked: the page shows their choice with Next only.
- **The latest submitted answer is what the teacher sees.**

### 4. The teacher's level from today's picture question

From the **latest** answer: right → **Got it**, wrong → **Needs help**, none → **Not answered yet**. **"Getting there" is no longer used.** Today, Children (Missed lessons), the child view, Back today and Week all follow this.

### 5. Reopening the link starts from the first step

When the family opens the link again (today's message, the catch-up), it starts from the **first step** and goes through every step again, instead of opening on Done. The answer page is open if a try is left, locked if not.

### 6. The WhatsApp text promises audio that may not be there

`todays_lesson_link_v1` says "The lesson audio, a home activity and a picture question are ready". If the teacher removes the concept section, the family page has no audio. **[CONFIRM: Rahul]** how to solve it (the template is approved wording, so the page and the template have to agree).

### Copy

| Key | en | kn, te, ta, hi |
|---|---|---|
| Top bar | Tappida Paatha | Tappida Paatha (the same in every language) |
| Back | Back (reuse the existing key if it fits) | existing key / needs translation |
| Try again (new key) | Try again | needs translation |
| Submit on today's picture question (new key; the catch-up keeps "This one, send") | Submit | needs translation |

Step titles: each step already has a title; they move into the Back row.

### Decisions to write (Rahul, with the build)

D-62 (reopen goes to Done, first answer counts, no second answer) and D-95 (Getting there) change; T-90's "look again" goes. One new decision file for this.

## Acceptance criteria

- The top bar reads "Tappida Paatha" on every family step in all five languages.
- Every step of today's message, the evening message and the catch-up has the ‹ Back + title row above the teacher line; none on the first step; the title isn't repeated below.
- Back on Done opens the answer page, open with a try left, locked without.
- On both answer pages: choosing doesn't send; submit does; nothing says right or wrong; Try again after the first submit; locked after the second; a third submit is refused by the API.
- The teacher sees the latest submitted answer; today's level is Got it (latest right) or Needs help (latest wrong); "Getting there" shows nowhere.
- Reopening the link starts at the first step; the answer page's state (open or locked) is kept.
- A teacher-removed concept and the WhatsApp text don't contradict each other (per Rahul's answer to 6).
- New strings have keys in all five locale files.

## How it's tested

- Endpoint tests (`FakeApi`): two submits accepted on each answer endpoint, the third refused; the latest is stored and returned to the teacher; today's level from the latest answer (Got it / Needs help).
- Vitest: the Back + title row on each step of the three pages, none on the first; Back from Done to an open and a locked answer page; choose-then-submit, Try again, locked; no right/wrong text; reopen starts at the first step; the page title.
- Existing e2e specs pass (no new spec). `verify.sh`.
- 360 px screenshots in kn and en: a middle step with the Back row, the answer page open with Try again, locked, and Done; axe on the three pages.

## Notes

- 15 Nov: proposed by Monika from testing. Code check of "does the family page show today's message": the steps follow the approved sections (voice and text with her note → Is everything OK? → concept with its audio → activity → video when there is one → picture → Done), removed sections skipped. WhatsApp → page can't be seen yet: until the templates are approved, `WhatsAppChannel` still sends `hello_world` (T-104 notes), so a test phone gets no button to the page.
