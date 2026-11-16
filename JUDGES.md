# Tappida Paatha · guide for judges

Thank you for testing **Tappida Paatha** ("the missed lesson"). This page gets you from zero to the whole journey in a few minutes: a teacher records what she taught, an absent child's family gets it at home in their own language, the child answers, and the answer comes back to the teacher.

No sign-up is needed. Everything below works on a phone or a laptop browser.

| | |
|---|---|
| ⭐ **Guided demo** (start here, ~5 min) | https://slateai-staging.azurewebsites.net/demo |
| 📱 **Real app** (what a teacher uses, ~10 min) | https://slateai-prod.azurewebsites.net |
| 🔍 **Under the hood** (every AI call, live) | https://slateai-staging.azurewebsites.net/demo/ai |
| 🛠️ **Admin pages** | https://slateai-staging.azurewebsites.net/admin |
| 🎬 **Video** (3 min) | https://www.youtube.com/watch?v=lDvo7b93d4A |
| 📄 **Project overview** | [README.md](README.md) |

> **Same app, two servers.** The guided demo and the real app run the same code, screens and AI. The guided demo (staging) shows the teacher's phone and a family's phone side by side and has a ready-made school week. The real app (production) is the teacher app on its own.

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
| 3 | Teacher · **Approve** | Confirming opens **Approve → Today's message** by itself. Wait about a minute | *Being written…*, then the draft. (If you land on Today instead, tap **Check today's message**) |
| 4 | Teacher · **Approve → Today's message** | Play the voice note (about a minute), read the Kannada, play the voiced activity steps, look at the video and the picture question. Optional: **Edit** a sentence, add a note to families, or **Choose another pair** | Each section in Kannada, the teacher's language; families get the same in their own language |
| 5 | Teacher · **Approve** | Tick, then **Approve and send now** | It goes to the family at once and you are back on Today. (**Approve, send after 6 pm** is the school-day choice) |
| 6 | Family phone | Open the new message | Shobha's greeting, then *"Is everything OK?"* (pick a reason), the explanation (tap to listen), the home activity, *Watch how it's taught*, then **Manjunath's turn** |
| 7 | Family phone | Answer the picture question (**>**, **<** or **=**, drawn as bundles of sticks) | Right or *"try once more"*. Code scores the answer; the AI never sees it |
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
| **Run Today again** | **Settings → Reset today** (demo class only) | Clears today's lesson, message and answers, puts the register back as SATS sent it, and keeps earlier days |
| **Offline** | Turn on airplane mode, mark attendance or record a note, turn it off | It saves on the phone and sends when the network is back |

---

## 4. Real app

1. Open **https://slateai-prod.azurewebsites.net** on your phone (or a laptop). Your browser can install it (*Add to Home screen*).
2. Sign in with a teacher's mobile number ([below](#5-sign-in-details)) and the PIN from **slide 14 of our submitted deck**.
3. Do the same journey as above on today's real date:
   - the register arrives at 9:00 (mark or correct it yourself before then);
   - record or pick the lesson;
   - check today's message on **Approve**;
   - choose **Approve and send now**, or **Approve, send after 6 pm** as in a real school.
4. If **Today** asks you to approve the evening message first, do it once: it is the short message every absent child's family gets.
5. To see what the families received, open **https://slateai-prod.azurewebsites.net/demo**. It is the same class, with the family's phone beside the teacher's.

**First-time sign-in:** to see the welcome a new teacher gets, sign in as **Savitha** (`9123456789`). It shows *"Namaskara, Savitha ma'am"*, three promises, and a 10-second voice greeting with a consent tick. Once someone completes it, her login goes straight to Today, like the others.

---

## 5. Sign-in details

| Server | Role | Name | Mobile number | PIN |
|---|---|---|---|---|
| Both | Class teacher | Shobha | `9000000000` | slide 14 of the deck |
| Both | Co-teacher | Savitha | `9123456789` | slide 14 of the deck |
| Both | Co-teacher | Monika | `9123456788` | slide 14 of the deck |
| Both | Family | — | — | none: each family link carries its own key |

- `/demo` signs you in by itself. The PIN is only needed for `/login`.
- The PIN is not in this public repository. All three teachers use the same PIN on both servers.
- Enter the 10-digit number without `+91`. Five wrong PINs lock that login for 15 minutes.

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
| **Family and child** | A link on WhatsApp (`/p/<token>`): no app, no sign-in | Listen in their language, give a reason for the absence, do the activity, the child answers; "Stop these messages" at any time |
| **Administrator** | `/admin` | Family phone numbers and home languages, lesson video links and lesson descriptions, teacher consent, greetings |
| **Judge / reviewer** | `/demo`, `/demo/ai` | Both phones side by side, and every AI call |

---

## 8. What is real and what is simulated

| Real | Simulated or fixed |
|---|---|
| **Every AI call**: Claude (lesson matching from voice or photo, choosing what to teach, writing messages and catch-ups, checking them) and Sarvam (speech-to-text, text-to-speech) | **SATS attendance**: a simulated feed, arriving at 9:00; **Bring in today's attendance** on `/demo` |
| **The teacher app, approvals, the family pages, answers and levels**, stored in PostgreSQL on Azure | **WhatsApp delivery**: on production, messages appear on the demo's family phone. Staging sends real WhatsApp templates from Meta's test number, but only to registered test phones |
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
- **"Today's message is being written…"** It takes about a minute: the AI writes every language, then each is voiced. The screen updates by itself.
- **The lesson can't be changed.** Once tonight's message has gone to families, the lesson is fixed, because they already have it. Use **Settings → Reset today** to try again.
- **Tonight's message wasn't sent.** `/demo` says why: the register isn't in, the lesson isn't confirmed, or the evening message isn't approved yet (Today → tonight's card).
- **The microphone doesn't work.** Allow it in the browser, or use **Pick from the list** or a photo.
- **Anything else:** the 3-minute [video](https://www.youtube.com/watch?v=lDvo7b93d4A) shows the same path.

Built by **Slate AI** (Monika Sisodia, Rahul Oswal) for the AI for Foundational Learning Hackathon, Problem 1C: student absenteeism.
