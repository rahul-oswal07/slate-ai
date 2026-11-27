# T-103 · Catch-ups get the same sections and editing as today's message

| | |
|---|---|
| Status | Built, not merged |
| Issue | #321 |
| Owner | R |
| Screens | T3-approve, P1-P4-catch-up |
| Source | J1, J2 · Finale Should · size M |

## What

**Catch-ups get the same sections and editing** as today's message (Concept · Activity · Picture · Video · Note)

## Acceptance criteria

Same editor on both tabs

## How it's tested

As T-89

## Notes

- 27 Nov: built (Rahul away; recorded as [D-129](../decisions/D-129-catch-up-sections-and-edits.md), Proposed until he confirms). A catch-up on Approve now has today's message's sections, one open at a time, with the same shared section card, Edit / Remove / Put back and edit form (`features/approve/Sections.tsx`, used by both tabs). Mapping: concept = A3's script with its player and the Kannada summary; activity = A3's instruction and other-objects line (the numbers code chose shown under it); video = the admin page's lesson link, put in by code; picture = the learning check (read only, numbers by code); note = hers. Only the video and her note can be removed.
- 27 Nov: every change is a new `ScriptVersion` and clears the approval (also from approved-but-unsent); her words get code checks only (names, braces, family's script, sums, numbers the catch-up already uses, the activity's numbers kept in the script) and a refusal saves nothing. Her script is voiced again with no AI call (`Hint = voice`, same audio check), the old audio and Kannada summary are dropped. Her note goes as typed, no translation; script and activity are edited in the family's language. The family's Listen page shows her note; the activity page links the video. Migration `CatchUpSections`.
- 27 Nov: tests: `CatchUpSectionsTests` (version binding, approval cleared, 409 older version, replay, each refusal, remove / put back), `CatchUpWriterTests` (video by code, her script voiced with no AI call), `ParentEndpointTests` (note and video, removed not sent), the Db approval test extended (her note after approving: approval cleared, the old version can't be sent, the family's page carries the note); Vitest for the catch-up's sections, editing, refusal, remove and read-only states, and the family's note and video. Not done: 360 px screenshots and axe on the new sections (no servers in this session); Monika's copy and the T3 design card.
- 15 Nov: moved here from `tasks.md` (T-110).
