# Tappida Paatha · guide for judges

Thank you for testing **Tappida Paatha** ("the missed lesson"). This page gets you from zero to the whole journey in a few minutes: a teacher records what she taught, an absent child's family gets it at home in their own language, the child answers, and the answer comes back to the teacher.

No sign-up is needed. Everything below works on a phone or a laptop browser.

**The app runs on two servers** with the same code, screens and AI. Use either one; each has its own class data.

| Server | Teacher app | Admin pages |
|---|---|---|
| **Staging** | https://slateai-staging.azurewebsites.net | https://slateai-staging.azurewebsites.net/admin |
| **Production** | https://slateai-prod.azurewebsites.net | https://slateai-prod.azurewebsites.net/admin |

| | |
|---|---|
| ⭐ **Guided demo** (~5 min, no sign-in) | https://slateai-staging.azurewebsites.net/demo: the teacher's phone and a family's phone side by side, with a ready-made school week |
| 🔍 **Under the hood** (every AI call, live) | https://slateai-staging.azurewebsites.net/demo/ai |
| 💻 **Source code** (GitHub) | https://github.com/rahul-oswal07/slate-ai |
| 🎬 **Video** (3 min) | https://www.youtube.com/watch?v=lDvo7b93d4A |
| 📄 **Project overview** | [README.md](README.md) |
| 🔑 **Sign-in** | [Teachers and their numbers](#5-sign-in-details); the PIN is sent to you by email |

---

## Contents

1. [The story in one child](#1-the-story-in-one-child)
2. [Guided demo: the 5-minute path](#2-guided-demo-the-5-minute-path)
3. [More to try](#3-more-to-try)
4. [Real app](#4-real-app)
5. [Sign-in details](#5-sign-in-details)
6. [Sample inputs](#6-sample-inputs)
7. [Who sees what (personas)](#7-who-sees-what-personas)
8. [What is real and what is simulated](#8-what-is-real-and-what-is-simulated)
9. [Check how the AI is used](#9-check-how-the-ai-is-used)
10. [If something looks off](#10-if-something-looks-off)

---

## 1. The story in one child

**Manjunath** (roll 15) is in Grade 3 at a Kannada-medium government school in Karnataka. His family speaks Kannada. He was away the two school days before today, and he is away today.

- **9:00** The attendance register arrives from SATS. Manjunath is absent.
- **In class** His teacher, Shobha, says in a 20-second Kannada voice note what she taught: Nalikali Maths Part 1, page 4, "more, less, equal". The AI matches it to the textbook lesson.
- **Today's message** The app writes a message for his family in Kannada:
  - a spoken explanation of the lesson, about a minute long, built on how the textbook teaches it;
  - a home activity with things every home has;
  - a video of how it is taught;
  - a picture question for Manjunath.
- **Teacher approves** Shobha checks it, can edit it in Kannada, and approves it. **Nothing reaches a family until she approves it.**
- **Evening** The family opens the link, listens, does the activity together, and Manjunath answers the picture question.
- **Back to the teacher** His answer, and the level code works out from it, appears on Shobha's Today screen.
- **Several days away?** The **weekly catch-up** chooses only what Manjunath needs for next week's lessons. On the day he returns, a **buddy card** pairs him with a classmate.

---

## 2. Guided demo: the 5-minute path

Open **https://slateai-staging.azurewebsites.net/demo**. It signs you in by itself as the class teacher.
- The left phone is the **teacher's**; the right phone is **Manjunath's family's**. On a phone, they are two tabs.
- **EN** at the top switches the app to English. Kannada, Telugu, Tamil and Hindi are there too.
- Tap **Reset demo** first, so you start from the same state as everyone else. Today's register will be in, Manjunath will be away, and there will be no lesson yet.

| # | Where | Do this | You should see |
|---|---|---|---|
| 1 | Teacher · **Today** | Look at the attendance card | The register came in from SATS at 9:00: Manjunath (roll 15) is absent |
| 2 | Teacher · **Today** | Tap the mic and read the Kannada sentence for **Nalikali p 4** from [Sample inputs](#6-sample-inputs) (allow the microphone). If you don't read Kannada, tap **Pick from the list** and choose *More, less, equal* (Bridge course) | After ~15 s: *Nalikali Maths · Part 1 · p 4*, with what you said. Tap **Confirm** |
| 3 | Teacher · **Approve** | Confirming opens **Approve → Today's message** by itself. Wait about a minute | *Writing today's message…*, then the draft. (If you land on Today instead, tap **Check today's message**) |
| 4 | Teacher · **Approve → Today's message** | Play the voice note (about a minute), read the Kannada, play the voiced activity steps, look at the video and the picture question. Optional: **Edit** a sentence, add a note to families, or **Choose another pair** | Each section in Kannada, the teacher's language; families get the same in their own language |
| 5 | Teacher · **Approve** | Tick, then **Approve and send now** | It goes to the family at once and you are back on Today. (**Approve, send after 6 pm** is the school-day choice) |
| 6 | Family phone | Open the new message | Shobha's greeting, then *"Is everything OK?"* (pick a reason), the explanation (tap to listen), the home activity, *Watch how it's taught*, then **Manjunath's turn** |
| 7 | Family phone | Answer the picture question (**>**, **<** or **=**, drawn as bundles of sticks) | *Well done!*, or **Try again**. Code scores the answer; the AI never sees it |
| 8 | Teacher · **Today** | Tap **See answers** | *Sent to 1 · 1 answered*: Manjunath's answer and his level |
| 9 | Teacher · **Missed**, **Week** | Look around | Manjunath's missed lessons and how he did; how fast children came back, the reasons families gave, and attendance stars |
| 10 | **Under the hood** (tab on `/demo`, or `/demo/ai`) | Read the list | Every AI call you just caused: what it was for, which child it was about, the model, prompt version, tokens and the check's result |

That is the whole loop: **register → lesson → AI draft → teacher approval → family → child's answer → teacher**.

---

## 3. More to try

| Try | How | What it shows |
|---|---|---|
| **The weekly catch-up** | **Approve → Catch-ups → Make this week's catch-ups**. Open Manjunath's catch-up, read **Why this**, try **Simpler** or **Shorter**, then approve | For a child who missed several lessons, the AI keeps only what blocks next week's lessons. A 90-second home-language script, an activity with household objects and a 3-picture check; all numbers come from code |
| **Another language** | On **Today**, edit the register and also mark **Rohan** (Hindi), **Sanjay** (English) or a Telugu or Tamil child absent, then confirm a lesson | Today's message is written in every absent family's language. Kannada stays the teacher's version |
| **A family's phone on its own** | `/admin/children` lists every child with a link to their simulated family phone (`/demo/family/<id>`) | Any family's view, full screen, as a parent would see it on a phone |
| **Edit as the teacher** | **Approve → Today's message → Edit** a sentence in Kannada, or add a note in any language | Her Kannada is kept word for word; the other languages are rewritten to say the same thing. Every edit needs a new approval |
| **A lesson with sums** | Pick *3-digit addition with carrying* (Chapter 3) | The picture question is a sum: the child picks the answer from three pictures of bundles and sticks |
| **A lesson without the book's exercises** | Pick e.g. *Half* (Chapter 8) | Today's message has the explanation, the activity and the video, but no picture question |
| **The textbook behind the voice note** | `/admin/lesson-links` → open a chapter → **About this lesson** | The description and worked examples, read from the textbook, that today's message is built on |
| **The weekly star** | **Settings → Weekly star → approve**, then **Send messages now** on `/demo` | Families of children who came every school day of the week get a star message |
| **The return day** | **Back today** | A buddy card for each returning child: who sits with them, 3 sums and the textbook words |
| **Go back to a past day** | Switch it on in the admin pages, `/admin/demo`, see [below](#go-back-to-a-past-day) | Use the app as if it were an earlier school day, to see a week build up |
| **Run Today again** | **Settings → Reset today** (demo class only), see [below](#start-today-again-reset-today) | Clears today's lesson, message and answers, puts the register back as SATS sent it, and keeps earlier days |
| **Offline** | Turn on airplane mode, mark attendance or record a note, turn it off | It saves on the phone and sends when the network is back |
| **The link on a real WhatsApp** | `/admin/children` on either server: save your number against a child, see [below](#get-the-link-on-your-own-whatsapp) | The same message arrives on WhatsApp, as a family would get it |

### Get the link on your own WhatsApp

Both servers send real WhatsApp messages. Meta only delivers our WhatsApp number's messages to phones registered with it, and for the finale **only the numbers of jury members Navya and Navneet are registered**. Navya, Navneet: this is for you.

1. Open **Admin → Children** on the server you are using: https://slateai-prod.azurewebsites.net/admin/children or https://slateai-staging.azurewebsites.net/admin/children (no key needed).
2. Pick any child, e.g. **Manjunath** (roll 15). Tap **Edit** under **Number 1**, type your 10-digit mobile number (`+91` is added for you), and tap **Save**. **Number 2** is optional: every message goes to each saved number.
3. Make sure that child is absent today (on **Today**, edit the register if needed), then sign in as Shobha ([Real app](#4-real-app)) or follow the [5-minute path](#2-guided-demo-the-5-minute-path), confirm a lesson and tap **Approve and send now**.
4. The message arrives on your WhatsApp in the child's home language, with a button that opens the [family pages](#what-the-parents-see). Open it, and answer as the child.

- WhatsApp only goes to numbers saved on this page; the seeded numbers are never dialled. A saved number survives **Reset demo**.
- Any other number is accepted by the page, but Meta won't deliver to it. Every family's pages can still be opened from **Family phone (simulated)** on the same page.
- The admin page is open to anyone with the address, so please tap **Clear** on your number when you are done.

### Start today again: Reset today

Once tonight's message has gone to families, the day's lesson is locked: Today says *"Tonight's message about this lesson has gone to families, so it can't be changed"*, because they already have it. To try another lesson, a voice note or the WhatsApp send again, reset the day:

1. Tap your profile in the header (**Profile and settings**) → **Settings**.
2. Scroll to **Reset today** and tap it, then **Yes, reset today**.
3. You see *"Today was reset. The register is back as SATS sent it."* Every screen reloads, and **Today** is ready for a new lesson.

| Cleared, for today only | Kept |
|---|---|
| The lesson, today's message and its voice, tonight's messages with the children's answers, the reasons families gave, buddy ticks, and your changes to the register (it goes back as SATS sent it) | Earlier days, the children, saved phone numbers and home languages, catch-ups, weekly stars |

- It is only in the demo class; a real class has no Reset today.
- **Reset today vs Reset demo:** Reset today clears just today, in the teacher app on either server. **Reset demo** on `/demo` puts the whole demo week back as it started.
- If your number is saved on `/admin/children`, sending again after a reset sends the WhatsApp message again.
- What it looks like: [`settings-reset-today-360-light.png`](docs/design/screenshots/settings-reset-today-360-light.png).

### Go back to a past day

The teacher app normally runs on today's real date. To use it as if it were an earlier school day (e.g. to record lessons for several days and watch Missed and Week fill up), switch this on in the admin pages:

1. Open **`/admin/demo`** on the server you are using (e.g. https://slateai-staging.azurewebsites.net/admin/demo), or **`/admin` → Demo**.
2. Turn on **Go back to a past day**. It saves straight away (*"On: the demo teacher sees the arrows beside the date"*).
3. Back in the teacher app, arrows appear beside the date:
   - on **Today**, **Previous day** / **Next day**, or tap the date to **Choose a day**;
   - on **Missed**, **Back today** and **Week**, **Previous week** / **Next week**.
4. Every screen then acts as if that day is today: the register, the lesson, today's message, approve and send, catch-ups, the week and the weekly star. `/demo` shows the day too. **Back to today** returns to the real date.

- Only the demo class, up to 120 days back, never a future day.
- The chosen day is kept **in your browser only**: other judges on the same server stay on the real date.
- The switch is per server and off until someone turns it on. With it off, there are no arrows and every screen is on the real day.
- WhatsApp sends as on a real day, so a family with a saved number gets that day's message.
- **Reset demo** rebuilds the story on the real date and removes the days made this way.

---

## 4. Real app

1. Open **https://slateai-prod.azurewebsites.net** or **https://slateai-staging.azurewebsites.net** on your phone (or a laptop). Your browser can install it (*Add to Home screen*).
2. Sign in with a teacher's mobile number ([below](#5-sign-in-details)) and the PIN **we sent you separately by email**.
3. Do the same journey as above on today's real date:
   - the register arrives at 9:00 (mark or correct it yourself before then);
   - record or pick the lesson;
   - check today's message on **Approve**;
   - choose **Approve and send now**, or **Approve, send after 6 pm** as in a real school.
4. If **Today** asks you to approve the evening message first, do it once: it is the short message every absent child's family gets.
5. Back on **Today**, **See answers** shows each child's answer and level. **Missed**, **Back today** and **Week** show the rest.
6. To see what a family received: get it on [your own WhatsApp](#get-the-link-on-your-own-whatsapp), or open **Admin → Children** and tap **Family phone (simulated)** next to the child.

### What the parents see

Parents get a WhatsApp message with a link. There's no app to install and no sign-in. The link opens the family pages, in the family's home language (Kannada, Telugu, Tamil, Hindi or English):

1. **The teacher's greeting:** a short recording in the class teacher's own voice.
2. **"Is everything OK? Tell us":** the parent taps one picture for why the child was away (*Unwell*, *Family travel or work*, *Festival or function*, *Needed at home* or *Other*), or skips it. Only the teacher sees the reason. The **Week** tab totals the reasons.
3. **What the child learned today:** the lesson explained in about a minute, as spoken audio and as text.
4. **Try this at home:** an activity with sticks, leaves or stones. **Listen to the steps** reads them aloud.
5. **Watch how it's taught:** a short DIKSHA video of the lesson. It can be skipped to save mobile data.
6. **The child's turn:** a picture question with bundles of sticks. The child taps **>**, **<** or **=**, or picks the answer to a sum. A wrong answer gets **Try again**.
7. **Well done!:** the child gets a star for the day. The answer and the child's level appear on the teacher's **Today** screen.

**Stop these messages** is always at the bottom, so a family can opt out at any time. A child who came every school day of the week gets a **weekly star** message instead, with a star badge.

### First-time sign-in: what a new teacher sees

Sign in as **Savitha** to see the set-up a teacher goes through the first time. She is a co-teacher of the same class and hasn't set up yet.

1. Open **/login** on either server (e.g. https://slateai-prod.azurewebsites.net/login). Enter Savitha's mobile number **`9123456789`** (without `+91`) and the PIN from our email.
2. **Welcome:** *"Namaskara, Savitha ma'am"* and three promises: attendance comes from SATS and it's one voice note a day; nothing reaches a parent until she approves it; only she can see these records. Tap **Start**.
3. **Step 1 of 3, her greeting:** tap the mic and record about 10 seconds in Kannada (a suggested sentence is on screen), play it back, tick the voice consent (*"I agree my voice is used only in messages to parents…"*) and tap **Save and continue**. No microphone? **Continue without a greeting**.
4. **Step 2 of 3, the evening message:** the short message families get on a day of absence. Only the highlighted words change each evening, filled from her records, and no AI writes it. **Approve this message**, or **Not now** (it can be changed in Settings).
5. **Step 3 of 3, the weekly star:** the message and the two family screens for a child who came every day, shown with a child from her class. Approve it once; nothing goes until she does.
6. She lands on **Today**, ready for the day.

- The Welcome shows only once. After someone completes it, Savitha's login goes straight to Today, like the others.
- **Already done by someone else?** Open `/admin/teachers` on the same server and tap **Clear consent** next to Savitha, then **Yes, clear it**. It deletes her greeting, and her next sign-in starts at the Welcome again.
- Families hear the class teacher's (Shobha's) greeting, so recording one as Savitha doesn't change what the demo families hear.

---

## 5. Sign-in details

All three teachers share one class, **Grade 3 · Kannada medium** (36 children), on both servers. Sign in with any of them at `/login`.

| Teacher | Role | Mobile number | Use it to |
|---|---|---|---|
| **Shobha** | Class teacher | `9000000000` | Run the class: register, lesson, approvals. Families hear her greeting |
| **Savitha** | Co-teacher | `9123456789` | See the [first-time sign-in](#first-time-sign-in-what-a-new-teacher-sees) |
| **Monika** | Co-teacher | `9123456788` | Work in the same class as a second teacher |
| Family | — | — | No sign-in: each family's link carries its own key |

**The PIN is sent to you separately by email.** It is not in this public repository.
- All three teachers use the same PIN, on both servers.
- `/demo` signs you in by itself as Shobha. The PIN is only needed for `/login`.
- Enter the 10-digit number without `+91`. Five wrong PINs lock that login for 15 minutes.
- No email yet? The guided demo at `/demo` needs no PIN.

---

## 6. Sample inputs

**Kannada voice notes** (Today → tap the mic → read aloud → tap again):

| Lesson | Read aloud | In English | Expect |
|---|---|---|---|
| **Nalikali p 4** (the demo lesson) | ಇವತ್ತು ಪುಟ ನಾಲ್ಕು ರಲ್ಲಿ ಹೆಚ್ಚು ಕಡಿಮೆ ಸಮ ಹೋಲಿಸಿದೆವು. ಎಂಬತ್ತಾರು ಮತ್ತು ಎಂಬತ್ತಾರು ಸಮ. ಎಲೆಯಲ್ಲಿ ಅತಿ ಚಿಕ್ಕ ಸಂಖ್ಯೆಗೆ ವೃತ್ತ ಹಾಕಿದೆವು. | "Today on page 4 we compared more, less, equal. 86 and 86 are equal. On the leaf we circled the smallest number." | *Nalikali Maths · Part 1 · p 4* |
| Addition with carrying | ಇವತ್ತು ಮೂರಂಕಿ ಸಂಖ್ಯೆಗಳ ಸಂಕಲನ ಮಾಡಿದೆವು, ದಶಕ ವರ್ಗಾವಣೆ ಜೊತೆ. 456 plus 278, ಬಿಡಿ ಸ್ಥಾನದಲ್ಲಿ 14 ಬಂತು, ಒಂದು ಹತ್ತು carry ಮಾಡಿದೆವು. | "Today we did 3-digit addition with carrying. 456 plus 278: 14 in the ones place, so we carried one ten." | *3-digit addition with carrying* |
| Subtraction with borrowing | ಇವತ್ತು ಕಳೆಯುವಿಕೆ, borrowing ಜೊತೆ. 523 minus 168. ಬಿಡಿಯಲ್ಲಿ 3 ರಿಂದ 8 ಕಳೆಯಲು ಆಗಲ್ಲ, ಹತ್ತರ ಸ್ಥಾನದಿಂದ ಒಂದು ಹತ್ತು ಎರವಲು ತಗೊಂಡೆವು. | "Today, subtraction with borrowing. 523 minus 168. We can't take 8 from 3, so we borrowed one ten." | *3-digit subtraction with borrowing* |

- Any child's name you say is replaced by `{child}` before anything reaches the AI.
- Mixing in English words, as teachers do, is fine.

**Photo of the textbook page** (Today → *Add a photo of the page*): download [`sub-borrow-page.png`](evals/cases/images/sub-borrow-page.png) (expect *3-digit subtraction with borrowing*) or [`add-word-page.png`](evals/cases/images/add-word-page.png) (expect *Addition word problems*). The photo only finds the lesson; it never goes to families.

**Picking from the list** always works: **Pick from the list** opens the textbook's chapters, with *Next in the book* first.

---

## 7. Who sees what (personas)

| Persona | Where | What they do |
|---|---|---|
| **Class teacher** (Shobha) | Teacher app: **Today · Approve · Missed · Back today · Week**, **Settings** | Register, lesson, approves every message, sees answers and levels, buddy cards, the weekly view |
| **Co-teacher** (Savitha, Monika) | The same app, the same class | Shares the class; families hear the class teacher's greeting |
| **Family and child** | A link on WhatsApp (`/p/<token>`): no app, no sign-in ([what they see](#what-the-parents-see)) | Listen in their language, give a reason for the absence, do the activity, the child answers; "Stop these messages" at any time |
| **Administrator** | `/admin` | Family phone numbers (where WhatsApp goes) and home languages, lesson video links and lesson descriptions, teacher consent, greetings |
| **Judge / reviewer** | `/demo`, `/demo/ai` | Both phones side by side, and every AI call |

---

## 8. What is real and what is simulated

| Real | Simulated or fixed |
|---|---|
| **Every AI call**: Claude (lesson matching from voice or photo, choosing what to teach, writing messages and catch-ups, checking them) and Sarvam (speech-to-text, text-to-speech) | **SATS attendance**: a simulated feed, arriving at 9:00; **Bring in today's attendance** on `/demo` |
| **The teacher app, approvals, the family pages, answers and levels**, stored in PostgreSQL on Azure | **WhatsApp delivery**: both servers send real WhatsApp templates, but Meta only delivers them to phones registered with it (jury members Navya and Navneet); every other family's pages open from **Family phone (simulated)** on `/admin/children`: see [Get the link on your own WhatsApp](#get-the-link-on-your-own-whatsapp) |
| **The textbook**: Karnataka Grade 3 Maths (Kannada medium), all 61 lessons, with page numbers and descriptions read from the book | **The class**: 36 fictional children with first names only; families speak Kannada, Telugu, Tamil, Hindi or English |
| **The 6 pm evening send**, the weekly star on Saturday, offline sync | In the demo, **Approve and send now** and **Send messages now** send at once instead of waiting for 6 pm |

**Also note:**
- DIKSHA video links are set for 54 of 61 lessons on staging.
- Telugu, Tamil and Hindi wording is still waiting for native-speaker review.

---

## 9. Check how the AI is used

| Question | Where to look |
|---|---|
| What did the AI do, for whom, and did it pass the checks? | **Under the hood** on `/demo` or `/demo/ai` |
| Which prompts, and which version? | [`prompts/`](prompts/): one file per AI call, each with a version |
| How well do they work? | [`evals/results/`](evals/results/) (dated runs) and [`docs/quality.md`](docs/quality.md) |
| Can a child's name reach the AI? | No: names become `{child}` before any call ([`NameScrubber`](src/api/SlateAi.Api/Features/Lessons/Voice/NameScrubber.cs) and its tests) |
| Can the AI invent a number? | No: every number a family sees is chosen or checked by code; output with an unexpected number is refused |
| Can anything reach a family without the teacher? | No: only an approved, current version is ever sent; tested in [`tests/api/…/Db/`](tests/api/SlateAi.Api.Tests/Db/) |
| More evidence, file by file | [README → How the AI is kept safe](README.md#how-the-ai-is-kept-safe) |

---

## 10. If something looks off

- **Someone else is testing at the same time.** Each server has one shared class. On the guided demo, tap **Reset demo** to start again; or use **Settings → Reset today** to clear just today.
- **"Writing today's message…"** It takes about a minute: the AI writes every language, then each is voiced. The screen updates by itself.
- **The lesson can't be changed.** Once tonight's message has gone to families, the lesson is fixed, because they already have it. Use **Settings → Reset today** to try again.
- **Tonight's message wasn't sent.** `/demo` says why: the register isn't in, the lesson isn't confirmed, or the evening message isn't approved yet (Today → tonight's card).
- **The microphone doesn't work.** Allow it in the browser, or use **Pick from the list** or a photo.
- **Anything else:** the 3-minute [video](https://www.youtube.com/watch?v=lDvo7b93d4A) shows the same path.

Built by **Slate AI** (Monika Sisodia, Rahul Oswal) for the AI for Foundational Learning Hackathon, Problem 1C: student absenteeism.
