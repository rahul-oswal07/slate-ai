# T-144 · README for the finale and a JUDGES.md test guide

| | |
|---|---|
| Status | Done (#305, not merged yet) |
| Issue | #304 |
| Owner | R |
| Screens | demo |
| Source | Rahul, 17 Nov 2026 ("update the readme … and a special judge md file for judges to test the application"); hackathon-requirements §5 (R1–R9, J3); XS |

## What

The README described the 31 Oct build-stage submission.

- **README rewritten for the finale build.** It covers:
  - the problem and what the app does now: today's message with a minute-long voice note, the picture question, WhatsApp, the weekly catch-up, the return day and the week;
  - the five tabs, the family pages, the admin pages and the demo shell;
  - every AI call (A1–A4, A9, Sarvam) with its prompt version, model and the code around it;
  - the guardrails and where each is tested; the latest evals and test counts;
  - architecture, configuration (stale `Storage__*` rows removed: audio is in Postgres), commands and a documentation map.
- **`JUDGES.md`** (new), a judge's test guide:
  - the story through Manjunath;
  - a 10-step guided path on `/demo` (register → lesson → AI draft → approve → family → child's answer → teacher → Under the hood);
  - more things to try, the real app, sign-in details (the PIN goes to judges by email, not in the public repo) and sample inputs;
  - personas (R1), what is real or simulated (R7), where to check the AI, and what to do if something looks off.
  - It answers the jury's J3 (they couldn't reach the family and child views) with the side-by-side phones and `/demo/family/<id>`.

## Acceptance criteria

- Every relative link in both files resolves; `check-docs` passes.
- The judge's path matches the app: checked against the screens' `status.md` files and staging (the sample voice note isn't offered there, so the guide says to record or pick).

## Notes

- 17 Nov 2026: written.
- 17 Nov 2026: checked against the code. JUDGES.md gets *Get the link on your own WhatsApp* (save a number on `/admin/children`; only Navya's and Navneet's phones are on Meta's test-number list), and the README links to it. On-screen wording fixed: *Writing today's message…*, *Try again*.
- 17 Nov 2026: JUDGES.md gets *Start today again: Reset today* (where it is, what it clears and keeps, vs Reset demo).
- 17 Nov 2026: §4's first-time sign-in is now a step list for Savitha (Welcome → greeting with consent → evening message → weekly star → Today), with Clear consent on `/admin/teachers` to see it again.
- The demo's on-screen step guide (`demo.guide.*`) still says "Tap Use the sample voice note", which staging doesn't offer, and it doesn't mention today's message. It is worth updating to match JUDGES.md.
- 17 Nov 2026: §5 lists the three teachers with their numbers and what each is for; the PIN now goes to judges by email, not on slide 14 (README and JUDGES.md).
- 17 Nov 2026: JUDGES.md gets *Go back to a past day* (switch on `/admin/demo`, the arrows at the date, per browser, Reset demo removes those days).
- 17 Nov 2026: matched to the jury email: both servers with their app and admin links, the GitHub link, WhatsApp on both servers (Rahul set production's keys), and *What the parents see* in JUDGES.md.
- 17 Nov 2026: CI on `main` failed after #301 (docs only) on a flaky web test: `ConfirmLesson.test.tsx` read a checkbox's name before the curriculum query had filled in the lesson names (1 failure in 5 runs locally). It now waits with `findByRole`; 8 runs and the full suite (579) pass.
