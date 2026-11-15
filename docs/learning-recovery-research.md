# Learning recovery: research and options (jury point J1)

| | |
|---|---|
| Status | Research note, 15 Nov 2026. Nothing here is decided; each option becomes a task (`T-118` on) and, where it changes behaviour, a decision (`D-110` on) |
| For | Rahul (code, prompts), Monika (UX, copy, content) |
| Answers | J1 in [`hackathon-requirements.md`](hackathon-requirements.md) §5 |
| Cites | D-90 to D-96, D-106, D-108 in [`decisions/`](decisions/README.md) |

## 1. The feedback, and how it fits what is built

The jury said (13 Nov, J1):

1. **Meaningful recovery:** one minute of AI audio, mapped from the teacher's voice note to a chapter, is not enough.
2. **A simple, age-appropriate activity** the child does at home with the parent.
3. **A short concept explainer** (video or audio) for parents, children and teachers, ideally from existing resources.
4. **The teacher can personalise or add to** the generated content, based on what she taught in class.

The jury saw the build-stage app (31 Oct). Since then D-91 to D-93 and T-88 to T-94 have answered most of the *form* of J1. The table below is what a family and a teacher get today, checked against the code.

| J1 asks for | Built (on `main`) | Still thin |
|---|---|---|
| Activity at home, led by the parent | Today's message has 2–3 parent steps with sticks, leaves, stones or beans, using the book's own numbers (D-93). The catch-up has an activity with sticks (P2) | Text only: only the concept is voiced (`TodaysMessageGenerator` voices `Concept`, not `Steps`), so a parent who can't read gets no activity. One kind of activity (compare two numbers). Nothing to show the teacher afterwards except the buddy card's "Brought homework" tick |
| Concept explainer, video/audio, existing resources | A voiced concept (≤ 50 words, about 20 s) and "Watch how it's taught": one https link per lesson set on `/admin/lesson-links` and copied in by code (D-106) | Only as many links as someone typed in by hand. **None is entered on staging yet** (M-1), so in the demo today the video step doesn't appear. No explainer that runs without data, and none in Telugu or Tamil |
| Teacher personalises | She edits each section in Kannada, removes the video or note, adds a typed note; other languages are written from her words (D-92, T-89) | The draft never sees **what she said in class**: A1 already pulls `ExamplesUsed` and `SummaryKn` from her voice note, and A3 (catch-up) uses the examples, but A9 (today's message) is given only the curriculum. She can't record her own voice for a section ("spoken later", D-92) |
| More than one minute | Today's message: voice → reason → concept → activity → picture question (two tries, D-108) → Done | **Reach:** today's message needs the book's exercises, and 1 of 61 lessons has them (`M3.BRIDGE.COMPARE`). The catch-up covers 13 lessons (those with a `practice` rule) and still has the older single-audio shape (T-103 is Proposed). **Dose:** one evening per day of absence, the same for every child, whatever their answer |

So the honest answer at the finale is: *"We changed the output from one clip to a short, teacher-approved lesson. Today it works fully for one lesson. Here is how it grows."* The gaps are in reach, dose and personalisation, not in the shape of the output.

## 2. What the evidence says

| Finding | Source | What it means for us |
|---|---|---|
| In Botswana during school closures, weekly SMS problems alone did not raise numeracy. SMS plus a 15–20 min weekly phone call did, and cut innumeracy by 31%. Parents in both arms spent more time on learning with their child | Angrist, Bergman, Matsheng, "School's Out" ([NBER paper](https://conference.nber.org/conf_papers/f145603.pdf), [J-PAL summary](https://www.povertyactionlab.org/print/pdf/node/1773390)) | Messages alone are weak. A **live human touch** and **practice the parent can do** are the active ingredients. That is J1's point exactly |
| Messages **targeted to the child's level** did better than untargeted ones; untargeted SMS had no effect | Same paper | Use the picture answer we already score (D-95, D-108) to choose the next evening's level: Teaching at the Right Level, in code |
| Replications in India (Telangana), Nepal, Kenya, the Philippines and Uganda (16,000+ households, 2020–22): phone tutoring plus SMS raised learning everywhere, about 0.3 SD, about USD 12 per child; government tutors did as well as NGO tutors | [Oxford CSAE](https://www.csae.ox.ac.uk/building-resilient-education-systems), [J-PAL "20 for 20"](https://povertyactionlab.org/node/8131252), [NBER w31208](https://nber.org/system/files/working_papers/w31208/revisions/w31208.rev0.pdf), [Youth Impact](https://youth-impact.org/copy-of-connected) | A short teacher or volunteer call is worth designing for, even though our teacher-time limit (≤ 60 s a day) means it can't be her every evening |
| A **linear** number board game (1–100 in a row, move by counting) played four times for 15 min closed the number-sense gap between low-income and better-off preschoolers; a circular board didn't. Paraprofessionals ran it well | Siegler & Ramani ([2009 study](https://ies.ed.gov/ncee/WWC/Study/73725), [2008 paper](https://onpurposely.com/wp-content/uploads/2023/10/sieg-ram08.pdf), [2012 classroom study](https://education.umd.edu/sites/default/files/uploads/Ramani%20et%20al.%2C%202012%20%281%29.pdf)) | Exactly the kind of **parent-led game** J1 asks for: no device, chalk on the floor, repeatable over several evenings, and the evidence is on magnitude comparison, which is our demo lesson |
| Pratham's "Karona, Thodi Masti, Thodi Padhai" sent mothers daily game-based activities by SMS and WhatsApp (text, audio, short video), with staff phoning families who had basic phones | [Pratham](https://pratham.org/covid-19-response/connecting-with-children-learn-and-fun/), [UNICEF FLN Hub: Pratham's WhatsApp content guidelines](https://www.unicef.org/flnhub/resources/whatsapp-content-guidelines) | A model for format: one activity a day, audio for parents who don't read, games over worksheets. No independent learning evaluation found |
| Central Square Foundation: short WhatsApp activity videos for parents raised parents' engagement in foundational learning | [CSF, behavioural insights on parental engagement](https://centralsquarefoundation.org/blogs/using-behavioral-insights-to-increase-parental-engagement-on-fln-part-2) | Short and regular beats long and rare. A ~60 s demonstration fits |
| Rocket Learning reaches about 3 million families on WhatsApp in India; rigorous results are still in trials | [Mulago](https://www.mulagofoundation.org/portfolio/rocket-learning), [trial registry](https://www.socialscienceregistry.org/trials/17446) | Shows parents act on WhatsApp activities at scale. Don't quote it as proof of learning |

**Existing resources we could reuse (concept explainer):**

| Resource | What is there | Caveat |
|---|---|---|
| DIKSHA energised textbooks | Each QR code in a state textbook is a 6-character code that opens linked content; Karnataka reported its textbooks and content live on DIKSHA in 2019 ([DIKSHA help](https://diksha.gov.in/help/energised-textbooks/consumption/access-qr-code.html), [DIKSHA Karnataka](https://diksha.gov.in/ka/)). Sunbird, the platform under DIKSHA, documents a QR (DIAL) code search and a content search API ([Sunbird Knowlg](https://knowlg.sunbird.org/learn/product-and-developer-guide/assets-search-service/apis)) | **Not checked:** whether the Nalikali 2026-27 books carry QR codes, and whether DIKSHA's public endpoints answer outside its own apps. Monika can check the printed book; Rahul can try one call |
| DSERT video lessons (Samveda) | Daily video lessons for classes 1–10 broadcast on DD Chandana and posted on the Makkalavani YouTube channel ([Vikaspedia, Karnataka](https://education.vikaspedia.in/viewcontent/education/education-best-practices/remote-learning-initiatives-in-india/karnataka?lgn=en)) | 2020–22 lists; Grade 3 Nalikali maths coverage not confirmed. Video is heavy on data (D-01: audio, never video, for the teacher's phone) |
| Khan Academy Kannada | DSERT and Khan Academy reported 5,500+ Kannada maths and science videos ([India Education Diary](https://indiaeducationdiary.in/?p=7900)) | Mostly older grades; check before linking |

Our own book can't be reused this way: the Nalikali PDFs are marked "not to be republished" (D-90), so we cite its pages and never show them.

## 3. Options

Sizes: S ≤ half a day, M 1–2 days, L more. "Gate" is who has to say yes (CLAUDE.md, Collaboration).

### A. Personalised from what she taught (J1 point 4)

| # | Option | Why | Size · gate |
|---|---|---|---|
| A1 | **Give A9 what she said in class:** the lesson's `ExamplesUsed` (already extracted by A1 and used by A3) and `SummaryKn`, so the concept and activity reuse her examples and the objects she used ("like we did with the bangles today"). Numbers still come only from the book's exercises (guardrail 4); an example number that isn't on the list stays out. A1's output is already free of names (guardrail 1) | The cheapest real answer to "based on what was taught in class": no new screen, and the teacher recognises her own lesson | S · prompt change: A9 v3 and an eval run (guardrail 6), Rahul |
| A2 | **Her voice for the concept:** on Approve → Today's message, "Say it yourself" records ≤ 30 s; that clip replaces the computer voice for Kannada families (STT transcript checked for names, like the greeting). Other languages keep the computer voice of her words | The family already hears her greeting in her voice; this carries it into the lesson. D-92 already plans a spoken note | M · consent rule (her greeting's voice consent covers it?), Monika |
| A3 | **"Add from class" chips:** after a lesson is confirmed, the examples A1 heard appear as chips she can tap into the activity (no typing) | Keeps the ≤ 60 s rule while making the content hers | S after A1 · UX, Monika |
| A4 | **Her photo of the board for the family** (F-05: a teacher asked to add a photo of what she taught; built for matching only, D-82, "never sent to families") | Parents see exactly what the class did | S · changes D-82 (privacy: children's faces), Monika |

### B. A real home activity (J1 point 2)

| # | Option | Why | Size · gate |
|---|---|---|---|
| B1 | **Voice the activity steps** in each language, after the concept and behind the same AI disclosure | A parent who doesn't read today gets the idea but not the activity; Pratham's model sends audio for this reason | S · Rahul (audio length limit per message) |
| B2 | **An activity library per lesson kind**, written and checked by Monika (compare: "bundle and line up", "more–less shop with beans", the **chalk number track** from §2; place value: "make the number with bundles"; add/sub: "two piles"). Code picks one by lesson kind and fills its numbers; A9 only adapts the wording and her examples. Each activity card has 1–2 pictures drawn by code (sticks and bundles, as `Sticks` already draws) | Age-appropriate by design rather than by prompt; checkable; the pictures work for parents who read little | M · content by Monika, D-xx for "activities from a library, not free AI text" |
| B3 | **Something to bring back:** the last step is "draw your bundles in your notebook and show the teacher". The buddy card's "Brought homework" tick (D-22) already counts it, and the teacher's Today shows it | Closes the loop the intent cares about most: the pull back to school (D-20) | S · copy, Monika |

### C. Concept explainer (J1 point 3)

| # | Option | Why | Size · gate |
|---|---|---|---|
| C1 | **Enter the checked link for p 4 on staging** (M-1, already in `status.md` Next) | Without it, the demo has no video step, and the jury asked for exactly this | minutes · Monika |
| C2 | **A stick animation drawn by code:** a 30–45 s step-through on the family page (10 loose sticks become a bundle; 48 and 33 built as bundles; tens compared first, then ones), voiced with the concept's audio, numbers from the message. Works with almost no data and in every language | An explainer that is ours, always matches today's numbers, needs no copyright and no download, and can't be wrong because code draws it | M · design by Monika; no AI change |
| C3 | **Suggested links from DIKSHA:** for each lesson, a script (offline, not at runtime) looks up the book's QR code or searches DIKSHA by board, medium, grade and subject, and puts candidates on `/admin/lesson-links` as "suggested". A person still checks and saves each one (D-106 stays) | Scales the video section from 1 to 61 lessons without trusting an unchecked link | M · first check that the QR codes and API exist (§2), Rahul |

### D. Enough dose for a missed lesson (J1 point 1)

| # | Option | Why | Size · gate |
|---|---|---|---|
| D1 | **Exercises for more lessons:** extract the book's exercise numbers for the Nalikali Part 1 lessons (AI drafts them from the pages Monika uploads, she checks every number, `ExerciseAnswers` computes the answers). Each one brings today's message to another lesson | Reach is the biggest gap: 1 of 61 lessons today | M per chapter · Monika (content), D-93 already allows it |
| D2 | **A three-evening plan for a longer absence:** evening 1 learn (concept, explainer, activity), evening 2 play (the game from the library with new numbers), evening 3 check (picture question). If the check is wrong twice, the next evening repeats at an easier level (fewer tens, one digit); if right, it moves on. Code chooses, the teacher approves the plan once | The Botswana and multi-country results: regular, targeted practice works; a single message doesn't | L · D-xx for "one approval covers a plan", Monika and Rahul; guardrail 3 must hold for every evening |
| D3 | **A phone call when it isn't working:** after two "Needs help" answers, the teacher's Today suggests "Call {child}'s family (3 min)" with a printed-on-screen script of the activity; the call itself is hers, nothing automated | The human call is the strongest ingredient in the evidence; this asks for it only where it's needed | S · UX, Monika; teacher time is the risk |
| D4 | **Catch-ups get today's message's sections** (T-103, Proposed) | Multi-day absences get the same activity, explainer and editing | M · already a task |

## 4. Recommendation

**Before the demo (today, if anything):** C1 only, and maybe B1. Builds merge by Sun 15 Nov; A1 changes a prompt and needs an eval run, which is too much risk a day before links are due.

**At the finale (slides and Q&A), say it this way:**

1. *Meaningful:* "It's no longer one clip. Today's message is a short lesson the teacher approves: concept, activity, video, a question the child answers; the teacher sees the answer." Show it live (D-94).
2. *Activity:* "Parent-led, with sticks or beans, using the numbers in the child's own book. Next: a library of activities checked by our educator, with pictures and audio, and a game that research shows closes number-sense gaps in four short sessions" (B1, B2).
3. *Explainer:* "A checked DIKSHA video per lesson today; next, a stick animation drawn from the same numbers, which needs no data" (C1, C2, C3).
4. *Personalised:* "She edits every section in Kannada and adds her note. Next: the draft starts from the examples she used in class, and she can say the concept in her own voice" (A1, A2).
5. *Dose:* cite the phone-tutoring trials: targeted practice over several evenings, plus a human call only when a child is stuck (D2, D3).

**After the finale, in order:** A1 → B1 (both built in [T-118](tasks/T-118-todays-message-from-class-and-voiced-steps.md), 15 Nov, eval pending) → D1 (first chapter) → B2 → C2 → D4 (T-103) → A2 → D2 → C3 → D3. A1 and B1 are small and make every message better; D1 is what makes the product work beyond one page.

## 5. Risks to keep in view

- **Teacher time (≤ 60 s):** A3, D2 and D3 add taps. Each needs a time check with a test teacher (G4).
- **Data and phones:** video is heavy and many phones are shared; prefer C2 and audio over video links, and say the video's size (T-106 already plans it).
- **Guardrails:** every new piece (library activity, plan evening, her clip) must reach a family only through an Approved version, with numbers from code (guardrails 3 and 4).
- **Unverified facts:** the Nalikali QR codes, DIKSHA's public API and the DSERT Grade 3 videos are not confirmed (§2). Don't claim them on a slide until checked.
- **Evidence fit:** the trials are about older primary children during school closures, and the board-game studies about preschoolers. They support the design; they don't prove our product works. Say so if asked.
