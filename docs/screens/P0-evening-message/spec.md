# P0 Evening message and reason · spec

The family's page on a day of absence: her greeting, the message, the reason in 5 pictures.

| | |
|---|---|
| Route | `/p/:token` (kind `evening`) |
| Web | `features/parent/` (`EveningView`, `ReasonView`, `VoiceNote`, `LessonAudioPlayer`) |
| API | `Features/Parent/`, `Features/EveningMessages/` |
| Design card (Monika) | [`P0-messages-reason.md`](../../design/P0-messages-reason.md) |
| Status | [`status.md`](status.md) |

## Purpose and states

No sign-in. The evening message on each day of absence, with an optional reason for the absence. The chat view around it is the demo's family phone ([demo](../demo/spec.md)).

## Requirements

Moved as they are from `spec.md` §2 (15 Nov 2026). This file is now where they are kept; `spec.md` §2 has the index.

| ID | P | Requirement | Acceptance |
|---|---|---|---|
| FR-20 | M | **Evening message on each day of absence** (D-21): the teacher's recorded greeting plus a fixed message the teacher approved once at setup, with slots filled by code: today's lesson (from her confirmed lesson record), the textbook page for homework, and "a classmate is keeping `{child}`'s seat" (the buddy is named only on the teacher's card, D-29). No AI-written text, so no daily approval. No "tomorrow" slot (D-48). Audio is stitched from pre-made pieces (greeting in the family's language or fallback, fixed phrases, lesson-name and page clips); the child's name is not spoken (D-50). The template is reviewed and changed from Settings, and a change needs approval again (D-49). Several missed days still compress into one catch-up (FR-06–FR-08) | Sent after 6 pm only to opted-in, not-stopped families; slots filled by code; template approval recorded once; teacher time stays within NFR-01. Guardrail 3 (confirmed by Rahul, 20 Oct): the send endpoint returns 409 unless the class's `MessageTemplate` is Approved, the lesson record is teacher-confirmed, and the family has opted in and not stopped; no LLM is called on this path; audio is the greeting plus pre-synthesised template and competency-name segments, covered by an endpoint test |
| FR-21 | M | **Reason for absence** (D-23), optional, on the parent page: "We missed `{child}` today. Is everything OK?" with 5 picture buttons (unwell · family travel or work · festival or function · needed at home · other) (picture buttons only in the MVP; the voice note and its A7 classification are cut, D-63). **14 Nov (D-98):** also on today's message page, after the teacher's voice (T-91), and in WhatsApp step 2 as a list message (T-106) | Category only is stored; shown only to the teacher; never asks for symptoms; skipping it never blocks the rest of the page |

## Decisions cited

D-05, D-21, D-23, D-29, D-30, D-31, D-43, D-47, D-48, D-49, D-50, D-52, D-63, D-64, D-70, D-76, D-98, D-99.

D-01 to D-99 are in [`../../decisions/D-01-to-D-99.md`](../../decisions/D-01-to-D-99.md); later ones are one file each in [`../../decisions/`](../../decisions/README.md).
