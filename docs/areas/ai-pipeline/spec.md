# AI pipeline · spec

Prompts A1–A9, validators, the verifier, speech, evals.

| | |
|---|---|
| Route | none |
| Web | none |
| API | `Ai/`, `Speech/`, `Features/CatchUps/` (`Compression/`, `Script/`, `Verify/`, `Practice/`), `prompts/`, `evals/` |
| Design card (Monika) | none yet |
| Status | [`status.md`](status.md) |

## Purpose and states

Everything Claude and Sarvam do, and the code that checks it. The design is in [`spec.md`](../../spec.md) §3.3 (content rules) and §5 (calls, prompts, grounding data, verifier checks, evaluation); eval results are in [`quality.md`](../../quality.md) §2. The guardrails are in `CLAUDE.md`.

## Requirements

Moved as they are from `spec.md` §2 (15 Nov 2026). This file is now where they are kept; `spec.md` §2 has the index.

| ID | P | Requirement | Acceptance |
|---|---|---|---|
| FR-06 | M | Compression: for a child with several missed lessons, Claude selects at most 2 target competencies that block next week's lessons, with a reason for each kept and dropped item | Targets are always a subset of the graph-computed candidate set (validator). A missed lesson without a practice rule is not a candidate, since code can't make its sums (T-81) |
| FR-07 | M | Script writer: per (target set, home language) Claude writes a ≤ 90 s catch-up script with 3 Kannada key words, a 5-minute homework task that points to the child's own textbook/workbook page (with a household-object option) and ends with "Show `{teacher}` when you come back" (D-22), a 3-picture learning check, a Kannada gloss for the teacher, and buddy-card content. *T-09: numbers (activity sum, check options, correct index) are made by code and A3 writes the words around them; the spoken script names nobody ("your child", "your teacher", D-50); buddy sums come from code (T-16), not A3* | Passes the verifier (§5.4) |
| FR-09 | M | Audio: text-to-speech in the home language, in this order: the teacher's own recorded greeting **in the family's language** where she recorded one (short line shown in Kannada script), otherwise her Kannada greeting followed by a computer-voice introduction in the family's language (D-52) → a ~5 s **spoken AI disclosure** in the home language ("This is a lesson from your child's teacher. The rest is read by a computer voice.") → the catch-up script. **The computer voice never says names**: "your child", "your teacher"; names appear only in written text and in her own greeting (D-50). The written transcript starts with the same disclosure. *(D-18, accepted.)* *T-11: Bulbul v3 voice "priya", 16 kHz WAV from Sarvam encoded here to Ogg Opus 24 kbps (Sarvam's own MP3 is a fixed 128 kbps, ~860 KB a catch-up); segments stored per clip, disclosure made once per wording; real length and size checked after synthesis (`Verifier:MaxAudioSeconds` 95, `MaxAudioKb` 300). Measured: ~55 s and ~150 KB per catch-up.* | Audio ≤ 300 KB, ≤ 95 s including greeting and disclosure; disclosure segment present in every package; plays on a low-end Android browser |
| FR-18 | — | ~~Spoken answer~~ Dropped (D-26); replaced by the optional spoken reason in FR-21. Was: STT + Claude rubric; confidence < 0.8 goes to a teacher review queue with the audio | Never auto-marks below threshold |

## Decisions cited

D-18, D-22, D-26, D-50, D-52.

D-01 to D-99 are in [`../../decisions/D-01-to-D-99.md`](../../decisions/D-01-to-D-99.md); later ones are one file each in [`../../decisions/`](../../decisions/README.md).
