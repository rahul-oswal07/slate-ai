# T-106 · WhatsApp step 2: today's message inside the chat

| | |
|---|---|
| Status | Proposed (likely after the final) |
| Issue | none yet |
| Owner | R |
| Screens | delivery, P5-todays-message |
| Source | see What |

## What

**WhatsApp step 2: today's message inside the chat (proposed, Monika, 14 Nov, D-98; after T-104, likely after the final):** `todays_lesson_chat_v1` has a quick reply "Send it here" + a website button. The tap reaches a webhook (`GET` verify, `POST` with an `X-Hub-Signature-256` check, idempotent per message ID) and opens WhatsApp's 24-hour window; then, only from the **Approved** version (guardrail 3), send in order: (1) the teacher's greeting as a voice note, (2) the lesson audio as a voice note (AI disclosure + concept in one Ogg Opus file: needs D-18 changed, since today the segments stay separate), (3) one text: concept, activity steps, homework page, video link with its size, (4) the picture question as an image drawn by code with **> < =** reply buttons, (5) the reason for absence as a **list message** (5 options; a plain message allows only 3 buttons). The child's tap is scored by code, answered "Right ⭐" / "Try again" and shown on Today (T-92). A `STOP` reply sets `ParentStoppedAt`. Nothing is sent after the window closes. Low data: text carries the lesson even if audio never loads; each clip is uploaded once per lesson and language and its media ID reused. `catchup_chat_v1` was not created (Meta flagged every wording as Marketing), so the catch-up stays link-only

## Acceptance criteria

After "Send it here" a registered phone gets the voice notes, text, picture question and reason list in order; a tap on > < = or a reason reaches the teacher's Today; a bad signature gets 401; a repeated event is handled once; no names in logs or AI calls

## How it's tested

Endpoint tests over `FakeApi` (signature, unknown token, duplicate event, window closed); unit tests for message order and the scorer; manual check on a registered phone

## Notes

- 15 Nov: moved here from `tasks.md` (T-110).
- 14 Nov, from `status.md`: Step 2 (T-106, today's message inside the chat) after the final.
