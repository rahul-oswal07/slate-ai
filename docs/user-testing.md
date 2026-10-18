# User testing log

Owner: Monika. First names only. Consent forms kept outside the repo. Plan and measures: `docs/quality.md` §3.

**Rule: never invent quotes, numbers or sessions.** Weak or mixed findings are fine; honest evidence beats polished fiction. Quotes only with the participant's quote consent. Every number in the deck or video comes from this file or from `evals/results/`.

## Consent script

Read aloud in the participant's language before starting. Record verbal consent (or a signature) in the participant table.

> "We are Monika and Rahul, building a free tool called Tappida Paatha for a hackathon. It helps children who missed school catch up. We'd like you to try it for about 20 minutes and tell us what is hard. There are no right or wrong answers; we are testing the tool, not you. We will take notes. With your permission we may write down your words, without your name. We will not record your face. You can stop at any time. We will not share your phone number. Is it OK to continue? May we quote your words without your name?"

Kannada and Telugu versions: **needs native review** before first use.

## Session script

Don't explain and don't help; watch, time, and note the words people use. If they are stuck for more than a minute, note it, then move on.

**Teacher** (her own Android phone if possible):

| # | Prompt to read | Screens | Watch for |
|---|---|---|---|
| 1 | "Four children are absent today. You taught addition with carrying. Tell the app." | T1 Today | Seconds (target ≤ 60 incl. attendance), any typing, hesitation at the mic |
| 2 | "Check that the app understood. Change it if it's wrong." | T2 Lesson confirm | Does she trust or question the mapped lesson? Can she change it? |
| 3 | "It's Thursday. Get the catch-up ready for the children who were away." | T3 Approve | Seconds; does she read the Kannada summary; does she listen before approving |
| 4 | "Who still needs help this week?" | T4 Ledger | Found in ≤ 15 s? Reads statuses without help? |
| 5 | "Ravi is back on Friday. What will you do with him?" | T5 Return day | Would she use the buddy card in class? |
| 6 | "Should you move on with the next lesson on Monday?" | T6 Pacing | Does the nudge change her decision? Does she trust the numbers? |

**Parent** (shared family phone if possible):

| # | Prompt to read | Screens | Watch for |
|---|---|---|---|
| 1 | "Your phone shows this message. What would you do?" | P1 Listen | Taps Listen without help? Worried about cost or who sent it? |
| 2 | Play the audio, then ask: **"Who is speaking? Was any of it a computer voice?"** | P1 | Did they notice the AI part? (yes / partly / no) |
| 3 | "Help your child do the activity and answer." | P2, P3 | Understands the activity by ear? Child taps without help? |
| 4 | "If you didn't want these messages any more, what would you do?" | P1 | Finds a way to stop? (feature proposed in D-19) |

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
| F-01 | | | | | | | | |
