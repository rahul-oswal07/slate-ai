# T-198 · Prompt caching for the textbook import and today's message

| | |
|---|---|
| Status | Proposed |
| Issue | #376 |
| Owner | R |
| Screens | ai-pipeline, curriculum |
| Source | Rahul, 27 Nov 2026 (AI list); `quality.md` §2 (A10: ~800k input tokens and ~USD 5 per book part); S–M |

## What

The textbook import (A10) sends about 800k input tokens per book part, much of it the same instructions and book on every call. A9 repeats the rules and the lesson's description. Use Claude's prompt caching on the shared start of each prompt: instructions, then the book or lesson, then the part that changes. Record cache reads and writes in `AiCalls`.

## Acceptance criteria

- Prompt order changes only where needed; any prompt text change bumps its version and gets an eval run (guardrail 6).
- Eval scores the same as before (A10 parts 1 and 2, A9).
- Cost and time before and after in `quality.md`.

## How it's tested

- Unit test that the cache markers are set; one Eval run each for A10 and A9.

## Notes

- 27 Nov 2026: proposed.
