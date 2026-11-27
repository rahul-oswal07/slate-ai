# T-86 · One "Tonight's message" card on Today

| | |
|---|---|
| Status | Built, not merged (25 Nov) |
| Issue | #312 (#177 isn't in this repo) |
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

- 25 Nov: built on `feat/T-86-tonights-message-card`, with the scope Rahul chose on 24 Nov ([D-126](../decisions/D-126-one-tonight-card.md)): today's message moves into the card, Preview shows the family's real text and what goes with it, and after the send the card is a receipt listing only the children left out.
  - **API:** `EveningSchedule` (`Features/EveningMessages/EveningSchedule.cs`) is now the one rule for when the message goes. The 6 pm job and `GET …/evening-messages/{date}` (`goesAt`, `waiting`, `sentAt`, `withTodays`, per-child `language` and `withTodays`) both use it. New `GET …/evening-messages/{date}/children/{childId}` returns the family's real text, read only. New store query `GetSentAsync`; no migration.
  - **Web:** `TonightCard` rewritten and `TodaysMessageCard` removed. New `tonightStatus.ts`, `FamilyPreviewSheet.tsx` and `useNow.ts`. `useEveningDay` asks again when the message is due, so the card turns into the receipt by itself. The setup/Settings preview says it is an example.
  - **Tests:** xUnit `EveningScheduleTests`, plus new tests in `EveningEndpointTests` (send time, countdown, writing, receipt, past day, preview, 404s). The Db test `EveningMessageDbTests` now also checks the receipt and the preview. Vitest: `tonightStatus.test.ts`, `TonightCard.test.tsx` (each state and Preview), `eveningRefetch.test.ts`.
  - **Local run (25 Nov):** signed in as Shobha with Postgres, the API and Vite, checked at 360 px: the waiting state, Preview (Telugu) and the receipt after a demo send. axe found no serious or critical issues on Today or in Preview. A real browser showed that closing Preview lost focus under StrictMode (jsdom didn't); fixed by reading the opener before the modal opens. `verify.sh` and the Db suite (29) pass.
  - **Left:** PR screenshots (taken locally, not committed); kn/te/ta/hi strings are drafts for Monika; the design card isn't updated (Monika's file).

- 15 Nov: `LessonAudioCard` no longer exists: the lesson audio tick was retired in T-114 (D-105), so this card has no audio checklist to merge; the concept clip of today's message is the audio.
- 15 Nov: moved here from `tasks.md` (T-110).
