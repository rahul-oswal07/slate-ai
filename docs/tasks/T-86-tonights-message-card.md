# T-86 · One "Tonight's message" card on Today

| | |
|---|---|
| Status | Open (after judging) |
| Issue | #177 |
| Owner | R |
| Screens | T1-today |
| Source | see What |

## What

**One "Tonight's message" card on Today (#177, after judging, 1 Nov):** today the tonight card and the lesson audio card are separate, "See the message" opens the one-time approval preview with a sample child (Ravi), and nothing says when the message goes or that it is waiting for her. Merge `TonightCard` and `LessonAudioCard` into one card: a checklist (wording approved once · lesson and page · optional lesson audio with player, Approve clip, Try again), a status line (goes at 6 pm / making the clip / waiting N min for you, then sends without it (T-85) / sent at hh:mm, with or without audio), and one line per absent child (name, language, with audio or text only, or the reason it isn't sent) with **Preview** of that family's real message. After the send it reads as a receipt. The API adds the time the job will send if she does nothing, computed in code. The setup and Settings approval preview stays, labelled as an example

## Acceptance criteria

From Today she sees tonight's real families, not a sample child; the audio and the message are in one place, in order; the status line always says what happens next and when; the countdown matches the T-85 hold; nothing changes for guardrail 3 (only an approved clip goes); strings in all five locales

## How it's tested

Vitest component tests for each state (before 6 pm, making, ready and waiting, approved, sent with and without audio, no phone / stopped); API test for the send-at time; 360 px screenshots and axe in the PR; existing e2e specs pass; `verify.sh`

## Notes

- 15 Nov: moved here from `tasks.md` (T-110).
