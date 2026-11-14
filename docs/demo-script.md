# Finale demo script (T-93, D-94)

The 3-minute live demo for the Grand Finale (Wed 18 Nov), told through one child: **Manjunath** (roll 15, Kannada family). Rules R1–R7 are in `hackathon-requirements.md` §5. Rehearse this exact path on the demo laptop and hotspot (R4).

## Before going on stage (R6)

1. Open `/demo` on staging and tap **Reset demo**. Reset gives the same state every time:
   - The five school days before today, with only Manjunath away on the last two.
   - Today's register, stamped 12:00 IST whatever the clock says, with only Manjunath away.
   - No lesson for today yet.
2. Staging needs `Demo__SendOnApprove=true`, so Approve sends at once. Prod doesn't have it and keeps the 6 pm send.

## The sample voice note

Today shows **Use the sample voice note (demo)** once `data/seed/demo-voice-note.webm` exists. It sends the clip through the same path as a recording: Sarvam STT, the name scrubber, then A1. The output on screen is live (R2).

**Record it (Rahul or Monika):** a Kannada speaker reads this sentence in a quiet room on a phone, about 10 seconds. It is the `nalikali-p4-kannada` case from the A1 evals (16/16 on 13 Nov).

> ಇವತ್ತು ಪುಟ ನಾಲ್ಕು ರಲ್ಲಿ ಹೆಚ್ಚು ಕಡಿಮೆ ಸಮ ಹೋಲಿಸಿದೆವು. ಎಂಬತ್ತಾರು ಮತ್ತು ಎಂಬತ್ತಾರು ಸಮ. ಎಲೆಯಲ್ಲಿ ಅತಿ ಚಿಕ್ಕ ಸಂಖ್ಯೆಗೆ ವೃತ್ತ ಹಾಕಿದೆವು.
>
> "Today on page 4 we compared more, less, equal. 86 and 86 are equal. On the leaf we circled the smallest number."

Save the clip as WebM/Opus (the teacher app's recorder makes one: record it on Today and download it, or convert with `ffmpeg -i in.m4a -c:a libopus -b:a 24k demo-voice-note.webm`). Put it at `data/seed/demo-voice-note.webm` and check that it matches *Nalikali Maths · Part 1 · p 4*. Don't use a child's name in it.

## Click path (3 minutes)

| # | Screen | Say | Real or simulated (R7) |
|---|---|---|---|
| 1 | Teacher, Today | "12:00, the register comes in from SATS: Manjunath is away." | SATS is simulated |
| 2 | Today → sample voice note → confirm | "She says what she taught in Kannada. The AI maps it to Nalikali p 4." | STT and A1 are real |
| 3 | Approve | "She checks and approves. Nothing reaches a family until she approves it." | Real |
| 4 | Family phone | "In the demo, Approve sends at once; in a school it goes at 6 pm." | WhatsApp is simulated |
| 5 | Family page → answer | "The child answers the picture question; code scores it." | Real |
| 6 | Teacher, Today | "Her answer is back on the teacher's phone." | Real |

Steps 3–6 use **today's message** (T-88, T-89, T-91, T-92); until those land, step 3 uses the week's catch-up and step 6 uses Missed lessons. Log each timed run in T-93's file (`tasks/T-93-demo-loop-and-reset.md`).
