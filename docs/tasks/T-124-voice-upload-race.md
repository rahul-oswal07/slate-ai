# T-124 · Voice upload's reply could say "mapped" instead of "processing"

| | |
|---|---|
| Status | In progress |
| Issue | #251 |
| Owner | R |
| Screens | T2-lesson-confirm |
| Source | CI on #250 (15 Nov 2026), flaky test; S |

## What

`POST /api/classes/{classId}/lessons/{date}/voice` returned its 202 body by reading the day back *after* queueing the mapping job. If the background mapper finished first, the reply said "mapped" instead of "processing". That made `VoiceUploadEndpointTests.After_confirming_a_new_note_replaces_the_lesson_while_todays_message_is_not_approved` fail now and then. The reply is now read before the job is queued. If the queue is full, the lesson is marked unclear and read again.

## Acceptance criteria

- The 202 body always describes the note just accepted ("processing"), however fast the mapper is.
- When the queue is full, the reply still shows "unclear", as before.

## How it's tested

- New test `VoiceUploadEndpointTests.The_202_says_processing_even_when_the_worker_has_already_mapped_the_note`. It makes every read of the day wait for the worker, which turns the race into a sure failure on the old code: written first, it failed with "mapped".
- `FakeLessonStore.BeforeGetDay` is the hook for that wait.
- After the fix, the voice tests passed three runs in a row.

## Notes

- 15 Nov 2026: found when #250's CI failed on this race. The failing test was written first, then the fix, in `LessonEndpoints.UploadAudioAsync`.
