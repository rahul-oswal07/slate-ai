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
| `a8-lesson-recap` v1 | evening lesson audio | 13/15 first pass |
| `a9-todays-message` v2 | today's concept and activity; other languages from her Kannada | 15/15 first pass (14 Nov) |

Sarvam is used for STT (`saaras:v4`) and TTS (`bulbul:v3`, re-encoded to Opus 24 kbps, ~55 s / ~150 KB per catch-up). Numbers, candidate sets, buddies, scores and pacing are all computed in code.

## Known gaps and drafts

- Telugu, Tamil and Hindi scripts are drafts for native review; the Hindi audio hasn't been listened to on a phone (T-26).
- The eval sets are smaller than the 25 cases per call that `quality.md` asks for.

## Open tasks

Task files that name this area:

```bash
grep -l "^| Screens | .*ai-pipeline" docs/tasks/[TM]-*.md
```
