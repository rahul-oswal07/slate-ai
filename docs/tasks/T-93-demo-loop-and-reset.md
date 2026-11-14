# T-93 · Demo loop and Reset

| | |
|---|---|
| Status | Built in part |
| Issue | #190 · part merged in #196 |
| Owner | R |
| Screens | demo |
| Source | see What |

## What

**Demo loop and Reset:** in demo mode, Approve sends today's message at once to the family phone; Reset gives the p 4 story every time (a Kannada child away, a sample voice note, no failed card, time 12:00 not 2:37 AM, "goes at 6 pm" before the send); the family phone is labelled with the family *(#190, Finale Must; J3, R1–R7, D-94; size M)* **Built in part (14 Nov):** one-child Reset (Manjunath, Kannada) with today's register at 12:00, "Bring in attendance" stamped 12:00 before noon, the family phone labelled with the family, `Demo:SendOnApprove` + `DemoSendOnApprove` (catch-up Approve sends at once; T-88 calls the same hook), the sample voice note button (clip still to record, `docs/demo-script.md`). The timed run waits for the clip and T-88–T-92.

## Acceptance criteria

From Reset, the 3-minute story runs with no dead ends, twice in a row

## How it's tested

Demo endpoint tests; a timed manual run logged in this file's Notes

## Notes

- 15 Nov: moved here from `tasks.md` (T-110).
- 14 Nov: the D-94 loop runs end to end on a local server and everything it needs is on `main`, so on staging only the steps below are left.
- 14 Nov, still to do: record the sample voice note (`docs/demo-script.md`) as `data/seed/demo-voice-note.webm`; set `Demo__SendOnApprove=true` on staging; do the timed 3-minute run on staging twice from Reset, on the T-108 flow, and log it here.
- 14 Nov: the pre-made catch-up `data/seed/demo-catchup.json` is Hindi, so Reset no longer uses it (no story child speaks Hindi). Export a Kannada one from a live run if we want the R5 fallback.
- 14 Nov: finale demo story (T-93, #190, in part): Reset follows one child, Manjunath (Kannada), and writes today's register stamped 12:00 IST; "Bring in attendance" before noon reads 12:00, not the time of the tap; the family phone reads "Manjunath's family's phone · Kannada"; with `Demo:SendOnApprove` (on in Development, for staging) Approve sends a catch-up at once and says so; Today offers "Use the sample voice note (demo)" when `data/seed/demo-voice-note.webm` exists (clip not recorded yet). `/api/me` carries `demo` flags for the demo teacher. Guide, Reset text and README follow the new story. Db suite 17/17.
