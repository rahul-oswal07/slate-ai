# D-126 · One Tonight's message card: today's message inside it, each family's real message, a receipt after the send

| | |
|---|---|
| Status | Accepted |
| Owner | Rahul, 24 Nov 2026 |
| Gate | T-86 |
| Screens | T1-today, T0-setup, T10-settings |

## Decision

T-86 was written on 1 Nov, when Today had a tonight card and a separate lesson audio card. T-114 (D-105) removed the lesson audio card, and today's message's concept clip became the audio. So the card to merge changed. Today now had two cards, Today's message (T-108) and Tonight (T-44). Nothing said when the message actually goes (the 6 pm job holds for today's message, T-111). "See the message" opened the one-time approval preview with a sample child.

Rahul chose (24 Nov):

1. **One card.** Today's message moves into Tonight's message as the last item of its checklist (wording approved once · lesson and page · today's message). `TodaysMessageCard` is gone. The one primary on Today is still the next step (T-96).
2. **Preview = the family's real text and what goes with it.** A read-only teacher endpoint returns that family's message, filled by code, in their language, and whether today's message goes with it. The family's step pages (picture question, clips) aren't replayed in the preview. The other options were the full family view (about twice the work: a teacher-scoped parent payload, with answer, reason and stop turned off) or no per-child preview.
3. **After the send, a receipt**: "Sent at hh:mm · with today's message" or "· text only", and only the children left out, with the reason. The answers card below lists everyone who got it (T-92), so nobody is listed twice.

## Recommendation

- **The status line's times come from the API.** `goesAt` and `waiting` on `GET …/evening-messages/{date}` are worked out by `EveningSchedule`, the same code the 6 pm job uses to hold (T-111, T-85). The countdown she sees therefore can't drift from the job.
- **Guardrail 3 is unchanged.** The preview sends nothing and makes no link; only an approved today's message goes, as before.
- **The setup and Settings preview stays**, with a line saying it is an example and that each family's real message is on Today.

## Notes

- 24 Nov 2026: accepted by Rahul (merge both; text and what's attached; only who didn't get it).
