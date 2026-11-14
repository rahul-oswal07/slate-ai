# T-26 · Native review of the Telugu, Tamil and Hindi text

| | |
|---|---|
| Status | Open |
| Issue | none |
| Owner | M finds reviewers, R applies the changes |
| Screens | every screen with text; P0-evening-message, P1-P4-catch-up, P5-todays-message first |
| Source | Cited as "T-26" across the docs and the locale files; its own row was cleared in the 24 Oct task reset, so this file collects what is waiting |

## What

A native speaker reads the Telugu, Tamil and Hindi text and we apply the corrections. Until then every string in those languages is a draft.

## Waiting for review

- Tamil layout at 360 px (T-84): fixed on the T-101 branch, not on staging or prod yet. For native review (T-26): the Tamil tab "திரும்பி வந்தோர்" (was "திரும்பியவர்கள்"), and the Telugu and Tamil strings that still attach a suffix to a name written in Latin letters: the evening text (`{child}ని`, `{child}இன்`), `te` `parent.reason.question` and `parent.todays.doneBody`, `ta` `parent.title`, `parent.stop.title` and `parent.todays.doneBody`.
- Telugu, Tamil and Hindi text (UI, scripts, evening messages, lesson names) are **drafts for native review** (T-26). The Hindi audio hasn't been listened to on a phone.
- Tamil and Telugu speakers should check the `/demo` guide steps and the Week sentences first (`user-testing.md`).
- The kn / hi / te / ta strings added with T-108 are drafts for Monika.

## Acceptance criteria

Each of the three languages has been read by a native speaker; corrections are in the locale files, the seed text and the prompts' examples; the "draft" notes in the docs are removed.

## How it's tested

`locales.test.ts` (placeholders match in all five languages); a prompt whose examples change gets a version bump and an eval run (guardrail 6).

## Notes

- 15 Nov: file created from the gaps listed in `status.md` (T-110). The Tamil layout fix named above is merged (T-101, #211).
