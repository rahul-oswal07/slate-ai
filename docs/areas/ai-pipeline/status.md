# AI pipeline · status

What works today and what doesn't. No dated notes here: those go in the task's file ([`docs/tasks/`](../../tasks/README.md)).

Spec: [`spec.md`](spec.md)

## Built

**AI** (all through `Ai/ILlmClient`, logged to `AiCalls`, no names sent):

| Prompt | Job | Latest eval |
|---|---|---|
| `a1-lesson-mapping` v3 | voice note / photo → competencies, with each lesson's page | 16/16 (13 Nov) |
| `a2-compression` v2 | choose 1–2 lessons from the code's candidate set, reasons in English and Kannada | 10/10 |
| `a3-script` v4 | 90-second home-language script | 14/14 first pass |
| `a4-check` v3 | content check alongside 6 code checks, one retry | 8/8 (11/11 incl. Hindi) |
| `a9-todays-message-lesson` v1 | today's message for a lesson without the book's exercises, no picture question (T-139, D-121) | 10/10 first pass (16 Nov) |
| `a8-lesson-recap` v1 | evening lesson audio | 13/15 first pass |
| `a9-todays-message` v4 | today's concept and activity, from the book and what she said in class (T-120); other languages from her Kannada; her note, in any language, written in every family language, Kannada included (T-119) | v3: 17/17 first pass (15 Nov); v4 not run yet (T-120) |
| `a10-textbook-outline` v1, `a10-textbook-lessons` v1, `a10-textbook-prerequisites` v1 | a textbook PDF → a draft curriculum (T-146, D-125): page outline, each chapter's lessons with typed exercises and pictures described, prerequisites; all checked by code | Both parts of the KTBS Grade 3 book (17 Nov): 60/60 lessons found, about 54/60 the same topic by name, 12/12 chapters; exercise accuracy not measured yet; about USD 5 a part |
| `a9-todays-message-sums` v1 | today's message for add and subtract lessons (D-116, T-128): adding and taking away with bundles; never writes a result; used only with `TodaysMessage:ArithmeticExercises` on (the default) | 15 Nov: 8 sum cases, 7 first try, 8 after one retry (with v4's 20: 28/28) |

Sarvam is used for STT (`saaras:v4`) and TTS (`bulbul:v3`, re-encoded to Opus 24 kbps, ~55 s / ~150 KB per catch-up). Numbers, candidate sets, buddies, scores and pacing are all computed in code.
- A verdict longer than the AI log's column (100) is stored cut to fit, so logging can never fail a catch-up or any other AI step (T-137).

## Known gaps and drafts

- Telugu, Tamil and Hindi scripts are drafts for native review; the Hindi audio hasn't been listened to on a phone (T-26).
- The eval sets are smaller than the 25 cases per call that `quality.md` asks for.

## Open tasks

Task files that name this area:

```bash
grep -l "^| Screens | .*ai-pipeline" docs/tasks/[TM]-*.md
```
