# User testing log

Owner: Monika. First names only. Consent forms kept outside the repo. Plan and measures: `docs/quality.md` §3.

**Rule: never invent quotes, numbers or sessions.** Weak or mixed findings are fine; honest evidence beats polished fiction. Quotes only with the participant's quote consent. Every number in the deck or video comes from this file or from `evals/results/`.

## Consent script

Read aloud in the participant's language before starting. Record verbal consent (or a signature) in the participant table.

> "We are Monika and Rahul, building a free tool called Tappida Paatha for a hackathon. It helps children who missed school catch up. We'd like you to try it for about 20 minutes and tell us what is hard. There are no right or wrong answers; we are testing the tool, not you. We will take notes. With your permission we may write down your words, without your name. We will not record your face. You can stop at any time. We will not share your phone number. Is it OK to continue? May we quote your words without your name?"

Kannada and Telugu versions: **needs native review** before first use.

## Session script

Don't explain and don't help; watch, time, and note the words people use. If they are stuck for more than a minute, note it, then move on.

**Teacher interview** (before the tasks, ~5 min; from the education mentor, 19 Oct):

1. When a child stops coming, what do you do today? Who else helps (cook, neighbour, SDMC member, older sibling)?
2. What are the usual reasons children in your class are absent? Which ones can the school do anything about?
3. Would a parent reply to "Is everything OK?" Would they tell you the reason?
4. Do you give homework now? From which book? Do children bring it back?
5. Which would help you more: knowing who missed what, or knowing who is likely to stay away longer?
6. How many days away before a child becomes hard to bring back?

**Teacher tasks** (her own Android phone if possible):

| # | Prompt to read | Screens | Watch for |
|---|---|---|---|
| 1 | "Four children are absent today. You taught addition with carrying. Tell the app." | T1 Today | Seconds (target ≤ 60 incl. attendance), any typing, hesitation at the mic |
| 2 | "Check that the app understood. Change it if it's wrong." | T2 Lesson confirm | Does she trust or question the mapped lesson? Can she change it? |
| 3 | "It's Thursday. Get the catch-up ready for the children who were away." | T3 Approve | Seconds; does she read the Kannada summary; does she listen before approving |
| 4 | "Who still needs help this week?" | T4 Ledger | Found in ≤ 15 s? Reads statuses without help? |
| 5 | "Ravi is back on Friday. What will you do with him?" | T5 Return day | Would she use the buddy card in class? |
| 6 | "Should you move on with the next lesson on Monday?" | T6 Pacing | Does the nudge change her decision? Does she trust the numbers? |
| 7 | "Approve the evening message that goes to families of absent children." | T0 setup (FR-20) | Does she understand it is sent every evening without asking her again? Comfortable with that? |
| 8 | "How quickly did absent children come back this week?" | Return-speed card (FR-22) | Reads it without help? Would it change what she does? |

**Parent** (shared family phone if possible):

| # | Prompt to read | Screens | Watch for |
|---|---|---|---|
| 1 | "Your phone shows this message. What would you do?" | P1 Listen | Taps Listen without help? Worried about cost or who sent it? |
| 2 | Play the audio, then ask: **"Who is speaking? Was any of it a computer voice?"** | P1 | Did they notice the AI part? (yes / partly / no) |
| 3 | "Help your child do the activity and answer." | P2, P3 | Understands the activity by ear? Child taps without help? |
| 4 | "If you didn't want these messages any more, what would you do?" | P1 | Finds a way to stop? (D-19) |
| 5 | Show the "Is everything OK?" step. "Would you answer this? Why or why not?" | Reason step (FR-21) | Feels caring or intrusive? Picks a picture without help? Any reason missing from the 5? |
| 6 | "Your child is back at school. What would you send with them?" | Evening message + homework (FR-20, D-22) | Did they notice the textbook-page homework and "show your teacher"? |

**Closing questions (both):** "What would stop you using this every day?" · "What one thing should we change?" Teachers also answer the SUS-lite questions in `quality.md` §3.

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
