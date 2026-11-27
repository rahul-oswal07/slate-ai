# D-127 · In the WhatsApp chat, the AI disclosure and the lesson are one voice note

| | |
|---|---|
| Status | Proposed |
| Owner | Rahul, 27 Nov 2026 **[CONFIRM: Rahul]** |
| Gate | T-106 (switching it on) |
| Screens | delivery, P5-todays-message |

## Decision

[D-18](D-01-to-D-99.md) says the family hears greeting → AI disclosure → script as **separate segments in order, with no server-side stitching**. That holds on the family page, which plays the segments one after the other. In the WhatsApp chat (step 2, T-106) each clip would arrive as its own voice note: a 5-second "this is read by a computer voice" note on its own, then the lesson. A family that plays only one note might hear the lesson without the disclosure, or the disclosure without the lesson. T-106 asks for the disclosure and the concept in **one Ogg Opus file**.

Two smaller points came up while building T-106 and are recorded here so they're confirmed together:

1. **Taps in the chat follow D-108, not the T-106 text.** T-106 (written 14 Nov) answers a picture tap with "Right ⭐" / "Try again". D-108 (15 Nov) says the family never sees right or wrong. The chat replies say what was chosen and that it can be changed once ("Ravi chose >. You can change it once: tap another answer above."), then "Thank you. The teacher will see Ravi's answer." after the second.
2. **The picture question is written, not drawn.** The app has no drawing library, so the chat sends "47 ? 52" (or "248 + 438 = ?") as text with the > < = (or three number) reply buttons. The family page still draws the sticks.

## Recommendation

- In the chat only, the AI disclosure and today's concept go as **one voice note**: code decodes both stored Ogg Opus clips and encodes them again as one 16 kHz Ogg Opus file at 24 kbps (`OpusEncoding.Join`, pure C#, Concentus; no ffmpeg). The disclosure still comes first and is still the reviewed text-to-speech clip of D-18; the computer voice still never says names (D-50).
- Her greeting stays its own voice note before it (it is her voice, not the computer's).
- Each joined file is uploaded to WhatsApp once per lesson and language and its media ID reused (in memory, 29 days).
- `WhatsApp:ChatLesson:JoinDisclosure` (default `true`) switches it: `false` sends the disclosure and the concept as two voice notes in a row, as D-18 has it today.
- The family page is unchanged: separate segments, no stitching.

## Notes

- 27 Nov 2026: proposed with T-106, which is built behind `WhatsApp:ChatLesson:Enabled` (off). Nothing reaches a family until Rahul switches it on.
