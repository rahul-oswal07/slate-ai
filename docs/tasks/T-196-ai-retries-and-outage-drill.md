# T-196 · Retries for brief AI errors, and an outage drill

| | |
|---|---|
| Status | Proposed |
| Issue | #374 |
| Owner | R |
| Screens | ai-pipeline, T1-today, T3-approve |
| Source | Rahul, 27 Nov 2026 (AI list); `ClaudeLlmClient` has no retry; guardrail 2 (fall back to rules); S–M |

## What

A brief error from Claude or Sarvam (rate limit, overloaded, timeout) fails the step straight away. Add a retry with backoff and a time limit for those errors only, in `ILlmClient` and the speech client. Every attempt is logged.

Then a drill, as tests: with Claude down and with Sarvam down, what each step does (A1, A2, A3, A4, A8, A9, A10, STT, TTS) and what the teacher sees. The rules fallback where there is one; nothing sent that she didn't approve; a clear message where there's no fallback.

## Acceptance criteria

- Refusals and invalid output are never retried as brief errors.
- Total wait per step stays under a limit from config.
- A table of what each step does when AI is down goes in `docs/areas/ai-pipeline/spec.md`.

## How it's tested

- Unit tests with a fake client that fails N times; one test per step with AI down.

## Notes

- 27 Nov 2026: proposed.
