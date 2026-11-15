# T-119 · Today's note marked in the family's language

| | |
|---|---|
| Status | Built, not merged |
| Issue | #242 |
| Owner | R |
| Screens | P5-todays-message |
| Source | Rahul, 15 Nov (spotted while explaining the notes box on Approve); size XS |

## What

On the family page of today's message, the teacher's note was marked `lang="kn"` for every family, but the API sends it in the family's language (A9 writes it from her Kannada; `TodaysFamilyView.For`). A screen reader read a Telugu, Tamil, Hindi or English note with Kannada pronunciation. It is now marked in the family's language, like the rest of the page.

## Acceptance criteria

- The note's element has the family's language as its `lang`.

## How it's tested

`TodaysPages.test.tsx`: "her note shows with her name, marked in the family language it arrives in (T-119)" (a Telugu family). It failed first (`lang="kn"`); the earlier version of this test expected the bug, with a Kannada note for an English family, which the API never sends.

## Notes

- 15 Nov: fixed in `TodaysPages.tsx`.
