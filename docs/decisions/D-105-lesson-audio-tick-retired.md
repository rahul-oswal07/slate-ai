# D-105 · The lesson audio tick is retired; today's message's concept clip is the lesson audio

| | |
|---|---|
| Status | Accepted |
| Owner | Rahul, 15 Nov 2026 (answers T-114's [CONFIRM: Rahul]) |
| Gate | none |
| Screens | T1-today, T3-approve |

## Decision

T-114 asked whether "Also send today's lesson as audio" (FR-23, T-62, [D-80](D-01-to-D-99.md)) is a separate clip that goes with today's message or duplicates its concept audio. In the code it duplicates it: when an approved today's message goes, its concept clip **replaces** the lesson clip (`EveningSender`, `EveningStore.FindByTokenAsync`), so a lesson clip she approves is never sent. It only went on days without today's message, where Monika's answer says it should not be offered.

So the tick is **retired**: it leaves Today and is not added to Approve. The concept clip of today's message, which she checks with the rest of the message, is the lesson audio. The API and data for lesson audio stay (no migration) but nothing in the teacher app starts it; with nothing ticked, the evening job never waits for a clip (T-82, T-85).

## Notes

- 15 Nov 2026: built in T-114 (`LessonAudioCard` and its strings removed). FR-23 is marked retired.
