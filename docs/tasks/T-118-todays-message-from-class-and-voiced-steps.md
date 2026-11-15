# T-118 · Today's message starts from her lesson; the activity is voiced

| | |
|---|---|
| Status | Built, not merged (eval not run) |
| Issue | none yet |
| Owner | R |
| Screens | T3-approve, P5-todays-message, ai-pipeline |
| Source | Jury J1 (13 Nov); options A1 and B1 in [`learning-recovery-research.md`](../learning-recovery-research.md); Rahul asked 15 Nov; size S + S |

## What

1. **From her class (option A1):** a fresh draft of today's message gets what A1 heard in her voice note: the lesson's one-sentence Kannada summary and the examples or objects she used (`SummaryKn`, `ExamplesUsed`), as `fromClass` in A9's input. So the concept and activity follow how she taught it ("with bangles, like in class") instead of being written from the curriculum alone. Code filters it first (`TodaysFromClassFilter`): only the day's own confirmed lesson for these lessons; a phrase with a child's name, a placeholder, a brace or a sign is dropped (guardrail 1), and so is one with a number not in the book's list (guardrail 4: numbers still come only from the book). When she has edited the Kannada, `fromClass` is not sent: A9 then only says her words in the other languages (D-92). Prompt `a9-todays-message` **v3**.
2. **Voiced activity (option B1):** each language's activity steps are voiced as one clip, behind the AI disclosure like the concept, for parents who read little. The family's "Try this at home" step has a player ("Listen to the steps") under the words; the teacher's Activity section has one ("Listen to the activity") so she hears what families will. Steps too long to voice keep their text with no clip; the draft is not failed. The clip ID is in the approved sections, so guardrail 3 holds as for the concept; the family token serves only this message's clips.

## Acceptance criteria

- A fresh draft's A9 request carries her summary and safe examples; never a name, placeholder, sign or number off the book; nothing when she edited the Kannada, or for another day or lesson.
- Every language of a new draft has an activity clip unless too long, and the draft is still ready then.
- The family page plays the steps after the disclosure through its token; a removed activity, a stopped family or another clip gets nothing.
- `a9-todays-message` v3 passes the eval (≥ 85%, the same code checks) before merge.

## How it's tested

- xUnit: `TodaysFromClassTests` (the filter); `TodaysMessageGeneratorTests` (from-class in the request, filtering, her edit wins, steps voiced, too long keeps the text); `ParentEndpointTests.Todays_activity_steps_play_after_the_disclosure_through_the_token_and_only_this_messages_clips`.
- Vitest: `TodaysPages.test.tsx` (player on the activity step, none without a clip); `TodaysMessageTab.test.tsx` (her activity player).
- Eval: `dotnet test --filter Category=Eval`. `evals/cases/a9-todays-message.json` has 3 new from-class cases. **Not run yet:** the session that built this had no `Anthropic:ApiKey`.

## Notes

- 15 Nov: built on `claude/jolly-lamport-c31v7x`. No schema change (the clip IDs are inside the sections JSON; old drafts have no activity clip and show text only). New strings `parent.todays.activityListen` and `todays.listenActivity` in all five locale files; te, ta and hi are drafts for native review (T-26). Left: run the A9 eval and attach the result; a 360 px screenshot of the activity step with the player; listen to one voiced activity in Kannada on a phone (the disclosure now plays before the concept and again before the steps; check that the repeat isn't tiresome). axe not run on the changed steps.
