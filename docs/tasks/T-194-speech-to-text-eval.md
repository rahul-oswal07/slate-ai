# T-194 · A speech-to-text eval on real classroom notes

| | |
|---|---|
| Status | Proposed |
| Issue | #372 |
| Owner | R / M |
| Screens | ai-pipeline, T1-today |
| Source | Rahul, 27 Nov 2026 (AI list); Sarvam `saaras:v4` has no eval; needs recording consent; M |

## What

A teacher's voice note goes through Sarvam's STT before A1, and nobody has measured it. Collect 15–20 real classroom notes (Kannada, code-mixed, with classroom noise), with each teacher's written consent and no child names in them. Then measure:

- the words against a hand transcript;
- whether A1, given the transcript from STT, still finds the right lesson.

## Acceptance criteria

- Recordings kept only with consent, outside the public repo if they can identify a teacher; the decision on where is written down.
- Results in `evals/results/` with the word error rate and the A1 match rate.
- `quality.md` updated.

## How it's tested

- An Eval test over the set (costs credits).

## Notes

- 27 Nov 2026: proposed. Where the recordings live is open (the repo is public).
