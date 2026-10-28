# User testing log

Owner: Monika. First names only. Consent forms kept outside the repo. Plan and measures: `docs/quality.md` §3.

**Rule: never invent quotes, numbers or sessions.** Weak or mixed findings are fine; honest evidence beats polished fiction. Quotes only with the participant's quote consent. Every number in the deck or video comes from this file or from `evals/results/`.

## Consent script

Read aloud in the participant's language before starting. Record verbal consent (or a signature) in the participant table.

> "We are Monika and Rahul, building a free tool called Tappida Paatha for a hackathon. It helps children who missed school catch up. We'd like you to try it for about 20 minutes and tell us what is hard. There are no right or wrong answers; we are testing the tool, not you. We will take notes. With your permission we may write down your words, without your name. We will not record your face. You can stop at any time. We will not share your phone number. Is it OK to continue? May we quote your words without your name?"

Kannada and Telugu versions: **needs native review** before first use.

## Before a session (UAT setup, updated 28 Oct)

- **Where:** staging, https://slateai-staging.azurewebsites.net, on the participant's own Android phone if possible (otherwise a low-end Android at 360 px). Note the network (4G / Wi-Fi / weak).
- **Logins:** three teachers share the one demo class (D-79): Shobha (main teacher), Lakshmi and Monika. PINs come from Rahul, never written here (public repo). Each login sees the Welcome once.
- **Shared class:** all three logins see and change the same data, and attendance saves are last-write-wins. Run **one session at a time** and press **Reset** on `/demo` before each one, so every participant starts from the same day (Ravi, Manjunath and Rohan away; today's lesson is `M3.ADD.CARRY`).
- **Parent side:** open the family links from `/demo` (Ravi · Telugu, Rohan · Hindi) on a second phone, or hand over the demo screen.
- **Before the first session:** check that each of the three logins signs in on staging, that Reset works, and that an approved catch-up plays audio. A failure here is a P0 for Rahul, not a finding.
- Bring: consent script, observation sheet (one per participant), a stopwatch.

## Session script

Don't explain and don't help; watch, time, and note the words people use. If they are stuck for more than a minute, note it, then move on.

Each question is tagged with the judging criterion it gives evidence for (`hackathon-requirements.md` §3): **[E]** efficacy and user centricity 35%, **[AI]** effective AI use 30%, **[I]** innovation 25%, **[R]** roadmap 10%.

**Teacher interview** (before the tasks, ~5 min; from the education mentor, 19 Oct) **[E]**:

1. When a child stops coming, what do you do today? Who else helps (cook, neighbour, SDMC member, older sibling)?
2. What are the usual reasons children in your class are absent? Which ones can the school do anything about?
3. Would a parent reply to "Is everything OK?" Would they tell you the reason?
4. Do you give homework now? From which book? Do children bring it back?
5. Which would help you more: knowing who missed what, or knowing who is likely to stay away longer?
6. How many days away before a child becomes hard to bring back?

7. (24 Oct mentor) Before a child does homework, what do they need to hear first? How long can a Class 1–3 child listen? **[E]**
8. (24 Oct mentor) Do you reward attendance now (stars, a chart, praise)? Does it work? **[E]**

**Teacher tasks** (in the order she would meet them; screens as built on 28 Oct):

| # | Prompt to read | Screen | Watch for | Criterion |
|---|---|---|---|---|
| 1 | "Sign in with this phone number and PIN." | Sign-in | Gets past the PIN without help? | E |
| 2 | "Set up the app the first time." | Welcome → greeting → evening message | Records a ≤ 10 s greeting? Understands the voice-consent tick? Understands the evening message goes **every evening without asking her again**? Comfortable with that? | E, AI |
| 3 | "Switch the app to Kannada." (or her language) | Language chips | Finds it? Any text cut off or wrong? | E |
| 4 | "Four children are absent today. Check the list, and correct it if it's wrong." | Today: attendance | Seconds; finds "Wrong? Correct it"? | E |
| 5 | "You taught addition with carrying. Tell the app." | Today: mic | Seconds (target **≤ 60 incl. attendance**); hesitation at the mic; speaks Kannada? | E, AI |
| 6 | "Check that the app understood. Change it if it's wrong." | Today: lesson confirm, Change | Trusts or questions the matched lesson? Can she change it? | AI |
| 7 | "Also send today's lesson as audio tonight." | Today: tonight card, lesson audio | Ticks it; listens to the end before Approve; would she send it? Too long, too short, too hard for a 7-year-old? | AI, E |
| 8 | "Get the catch-up ready for the children who were away." | Approve | Seconds; reads "Why this"; listens before approving? Would she send it as is? | AI |
| 9 | "Who still needs help this week?" | Missed | Found in **≤ 15 s**? Reads statuses without help? | E |
| 10 | "Ravi is back today. What will you do with him?" | Back today | Would she use the buddy card and the 3 sticks numbers in class? | I |
| 11 | "Should you move on with the next lesson on Monday?" | Week: pacing | Does the nudge change her decision? Trusts the numbers? | E, AI |
| 12 | "How quickly did absent children come back? Why were they away?" | Week: return speed, reasons | Reads it without help? Would it change what she does? | E, I |
| 13 | "Which children earned attendance stars?" | Week: stars | Understands why a child got or missed a star? Would she show it in class? | E |

**After the AI tasks (5, 7, 8)** **[AI]**:
- "Did the app get your lesson right? Would you have noticed if it didn't?"
- "Was the Kannada / Telugu / Hindi in the audio correct and natural? Any wrong word or number?"
- "Would you send this to a parent without changing anything? What would you change?"
- "The audio says it is partly a computer voice. Is that OK? Should it say so?"
- "Is it right that nothing goes to a parent until you approve it? Would you ever want to skip that?"

**Innovation and roadmap** **[I] [R]**:
- "What do you do for a returning child today? Is this different? Better or worse?"
- "Would you use this every day? What would stop you on Monday?"
- "Your attendance comes from SATS. Would you trust it coming in by itself?"
- "Who else should see this (head teacher, CRP, SDMC)? Who should not?"
- "Would families in your class open a WhatsApp link from you? Which ones would not?"

**Parent** (shared family phone if possible):

| # | Prompt to read | Screens | Watch for | Criterion |
|---|---|---|---|---|
| 1 | "Your phone shows this message from the teacher. What would you do?" | Evening message (greeting, lesson audio) | Opens it? Recognises the teacher's voice? Worried about cost or who sent it? | E |
| 2 | Show the "Is everything OK?" step. "Would you answer this? Why or why not?" | Reason step (FR-21) | Feels caring or intrusive? Picks a picture without help? Any reason missing? | E |
| 3 | "Now the catch-up arrives. What would you do?" | P1 Listen | Taps Listen without help? | E |
| 4 | Play the audio, then ask: **"Who is speaking? Was any of it a computer voice?"** | P1 | Noticed the AI part? (yes / partly / no) | AI |
| 5 | "Did your child understand it? Was it too long, too short, too fast?" | P1 | Length and level for a 6–8-year-old (24 Oct mentor) | AI, E |
| 6 | "Help your child do the activity and answer." | P2, P3 | Understands the activity by ear? Child taps without help? | E |
| 7 | "What does this last page tell you?" | P4 Done, star | Notices the star? Does it make the child want to go back to school? | E, I |
| 8 | "Your child goes back tomorrow. What would you send with them?" | Homework (D-22) | Notices the textbook-page homework and "show your teacher"? | E |
| 9 | "If you didn't want these messages any more, what would you do?" | Stop link | Finds a way to stop? (D-19) | AI (guardrail) |

**Closing questions (both):** "What would stop you using this every day?" · "What one thing should we change?"

**SUS-lite (teachers, 1 = disagree, 5 = agree):** (1) I would like to use this every school day. (2) It was easy to use. (3) I could use it without help. (4) I trust what it sends to parents. (5) It saves me time. Report the median per question, never a made-up average.

## UAT pass criteria (what "ready to submit" means)

Each line is checked across the participants of a round; record the result in *Timed tasks* and *Findings*.

| Requirement | Pass if | Criterion |
|---|---|---|
| Daily teacher task (FR, NFR 60 s) | Attendance + lesson in **≤ 60 s** without help, for at least 2 of 3 teachers | E |
| Find who needs help | Missed answers it in **≤ 15 s**, at least 2 of 3 | E |
| Lesson match | The matched lesson is right, or she fixes it with Change, in every session | AI |
| Approval gate (guardrail 3) | No catch-up, lesson audio or evening message reaches the parent phone before she approves it | AI |
| Teacher trust | At least 2 of 3 would send the catch-up audio as it is | AI |
| Home language | No wrong word or number reported in the audio; any wrong word is a P0 finding | AI, E |
| AI disclosure | Parents say "partly" or "yes" to "was any of it a computer voice?" | AI |
| Parent loop | Listen → activity → answer done without help by at least 1 of 2 parents | E |
| Stop | Parent finds the stop link without help | AI |
| Refinement evidence | At least 3 findings with a Decision and a merged PR, retested in round 2 | E |
| Stability | No crash, lost save or wrong child shown in any session | AI |

## Observation sheet (one per session)

| Field | Notes |
|---|---|
| Participant / round / date | |
| Device and network | |
| Where they hesitated or got confused | |
| What they ignored | |
| What they wanted that we don't have | |
| Words they used for things (their mental model) | |
| Teacher: seconds for attendance + capture | |
| Parent: understood the AI voice was AI (yes / partly / no) | |
| Quote (only with quote consent) | |
| Answers to closing questions | |

## Participants

| ID | Role | Context | Consent | Quote consent | Rounds |
|---|---|---|---|---|---|
| P1 | Teacher | | | | |
| P2 | Teacher | | | | |
| P3 | Teacher | | | | |
| P4 | Parent (Telugu) | | | | |
| P5 | Parent (Telugu) | | | | |

## Timed tasks

| Round | Participant | Attendance + capture (s) | Approve week (s) | Find who needs help (s) | Notes |
|---|---|---|---|---|---|
| | | | | | |

## Findings

For each finding, trace where it broke so the fix goes to the right place.
- **Lens:** works · works well · right thing · good · responsible
- **Chain link:** user (we didn't understand the need) · requirement (scoped wrong) · spec (lost in translation) · prompt (weak AI instruction)

| ID | Round | Participant | Observation / quote | Lens | Chain link | Severity (P0–P2) | Decision | PR / test |
|---|---|---|---|---|---|---|---|---|
| F-M1 | Mentor | Education mentor (CSF) | Missing the classroom–home link; no "pull" back to school, everything is push | Right thing | Requirement | P0 | Purpose changed to "stay connected, come back sooner" (D-20, D-21) | — |
| F-M2 | Mentor | Education mentor | Messages from the teacher carry weight with parents | Right thing | — | — | Kept: teacher's greeting and voice | — |
| F-M3 | Mentor | Education mentor | At this age home time gives engagement more than learning; learning as main KPI is ambitious | Right thing | Requirement | P0 | Primary measure is now return speed (D-20) | — |
| F-M4 | Mentor | Education mentor | Optimise for returning sooner; test the real pain point with teachers | Right thing | User | P1 | Interview questions 5–6 added | — |
| F-M5 | Mentor | Education mentor | Use school textbooks and homework families already know | Good | Requirement | P1 | Homework from the textbook page + "show your teacher" (D-22) | — |
| F-M6 | Mentor | Education mentor | Teachers would value a simple view of why children are absent; read NFHS-6 | Right thing | Requirement | P1 | Reason step (D-23) + return-speed card (D-24) | — |
| F-M7 | Mentor | Education mentor | Extending learning time beyond school days is valuable; after 6 pm is fine | Right thing | — | — | Kept as is | — |
| F-M8 | Mentor | Education mentor | Numeracy is well scaffolded; start with a rule-based dependency model | Works | — | — | Kept (D-01, code-computed candidate set) | — |
| F-M9 | Mentor | Education mentor | Find pathways schools already use (cook or community visits) | Right thing | User | P2 | Roadmap only (D-25); interview question 1 kept as background | — |
| F-M10 | Mentor | Education mentor | Success = how many come back and how soon; engagement at home; who is absent and why | Right thing | Requirement | P0 | Measures rewritten (D-20); return-speed card (D-24) | — |
| F-M11 | Mentor | Education mentor | Asking parents the reason adds accountability | Responsible | Requirement | P1 | Asked kindly and optionally, category only, no shaming (D-23) | — |
| F-M12 | Mentor | Education mentor | Strong endorsement; reaching children outside school is the hardest gap | — | — | — | Evidence for the deck | — |
| F-01 | | | | | | | | |
