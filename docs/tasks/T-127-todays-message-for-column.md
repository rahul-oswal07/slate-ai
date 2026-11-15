# T-127 · Under the hood says who today's message is for

| | |
|---|---|
| Status | Done |
| Issue | #257 |
| Owner | R |
| Screens | demo |
| Source | Rahul, 15 Nov 2026: the For column was always "–" on staging; D-115 (extends D-87); S |

## What

The demo's Under the hood panel has a **For** column (T-73, D-87), but on staging it was always "–". The demo story now runs through today's message (D-91), and its AI calls were class-level. The weekly catch-up's calls are tagged, but the demo Reset restores a pre-made catch-up (`data/seed/demo-catchup.json`), so they never run there.

Today's message's A9 and voice calls are now tagged with the children whose families get it tonight: the same set that decides its languages (`EveningOutcome.WillSend` or `Sent`). In the demo, that is Manjunath.

## Acceptance criteria

- A fresh draft, "Try again" and the redraft after her Kannada edit tag A9 and every voice call of that run with the children due tonight's message. IDs only, as before (guardrail 1).
- A child who came, or whose family has no consent, is not in it.
- The scope ends with the job: later calls are not tagged.
- The Under the hood panel shows "Manjunath" for those rows. No front-end change was needed.

## How it's tested

- `TodaysMessageGeneratorTests.A9_and_the_voice_are_logged_as_about_the_children_whose_families_get_it`
- `TodaysMessageEndpointTests.The_draft_is_written_as_about_the_absent_children_whose_families_get_it`: confirming a lesson over HTTP; only the absent children with consent.
- Both were written first, and both failed on the old code.

## Notes

- 15 Nov 2026: built on `feat/T-127-todays-message-for-column`. `TodaysJob` carries `ChildIds`. `TodaysMessageService.DueAsync` gives both the languages and the children, and `TodaysMessageGenerator` opens an `AiCallScope` for the run. Not changed: A1, A8, speech-to-text and the shared lesson clip stay class-level. The shared AI disclosure clip, made once per language, is tagged only by the run that first makes it.
- 15 Nov 2026: merged as #258, which closed #257. On staging after deploy; not in a prod tag yet.
- 15 Nov 2026: follow-up asked for by Rahul. The `/demo` subtitle (`demo.subtitle`, all five locales) now says the parent phone is simulated but its messages are the real ones a family is sent.
