# Status and handoff

Read this first in any new session, then `CLAUDE.md`, `docs/intent.md`, `docs/spec.md`, `docs/plan.md`, `docs/tasks.md`.
Keep it short: at the end of a session, update **Where things stand** and **Next**, and add one dated line under **Recent changes**. The full session-by-session log up to 1 Nov is in [`status-history.md`](status-history.md).

## Where things stand (1 Nov 2026)

- **Submitted** for the build stage on 31 Oct. Code freeze: **Sun 1 Nov, 23:59 IST**. Every task in `tasks.md` (T-01 … T-80, T-82, T-83) and every GitHub issue is closed.
- **Repo:** https://github.com/rahul-oswal07/slate-ai (public, MIT). Merges to `main` deploy to staging; `v*` tags deploy to prod.
- **Staging** (https://slateai-staging.azurewebsites.net, the judges' guided demo at `/demo`): on `main`. Settings `Demo__Story=video`, `Demo__Today=2026-10-31`, `Demo__TeacherPin`, `Demo__CoTeacherPin`, `Admin__Key`.
- **Prod** (https://slateai-prod.azurewebsites.net, the "real app"): on **`v1.1`** (tagged 1 Nov from `main` at #174: T-82, T-83; `v1.0` was #171). Judges' story, real date, no `Admin__Key`.
- **Logins:** Shobha `9000000000` (class teacher), Savitha `9123456789` and Monika `9123456788` (co-teachers), same class, on both servers. All use one PIN, which is on slide 14 of the submitted deck and not in the README. It equals the co-teacher PIN in `appsettings.Development.json`, so change the server PINs after judging (clear the stored `PinHash`; the seeder never overwrites one).
- **Tests:** `verify.sh` OK, 608 API and 349 web tests. The Db suite runs in CI. 12 existing e2e specs, no new ones (22 Oct rule).

## What has been built

**Teacher app** (React PWA, Kannada / English / Hindi / Telugu / Tamil):
- **Sign-in and setup:** phone + PIN, lockout after 5 wrong PINs, 30-day session (T-47). First-run Welcome, a 10-second greeting with voice consent (T-61), and approving the evening message once (T-44). Several teachers can share one class (D-79). Settings for the greeting and the evening message (T-71).
- **Today:** a simulated SATS register arrives at 12:00 (retry 2 pm). The teacher's marking wins over a late feed, and the register works offline through the Dexie outbox (T-12, D-69). There is a day strip (T-49). The lesson comes from a Kannada voice note (Sarvam STT → name scrubber → A1) or a photo of the textbook page (T-07, T-64), or is picked from the list. The tonight card has optional lesson audio, which the teacher approves clip by clip (T-62).
- **Approve:** the week's catch-ups, one per lesson group and language. Each has a segment player, a Kannada summary, **Why this**, the picture question, Simpler / Shorter, and a tick before Approve, bound to the version she read (T-14).
- **Missed lessons:** by child or by lesson, child history, and deleting a child's data (T-13, T-19).
- **Back today:** buddy cards with the buddy chosen from attendance, 3 sums made by code, textbook words and a print view (T-16).
- **Week:** return speed this week and last, reasons for absence, the pacing nudge, attendance stars (T-46, T-63).
- Slate & chalk look, AAA tokens, light and dark (D-77).

**Families** (`/p/<token>`, no sign-in): the evening message with a reason in 5 pictures (T-44, T-45). The catch-up flow is Listen → activity with sticks → 3-picture check scored by code → Done. A stop link is on every page (T-15).

**AI** (all through `Ai/ILlmClient`, logged to `AiCalls`, no names sent):

| Prompt | Job | Latest eval |
|---|---|---|
| `a1-lesson-mapping` v2 | voice note / photo → competencies | 13/13 (30 Oct) |
| `a2-compression` v2 | choose 1–2 lessons from the code's candidate set, reasons in English and Kannada | 10/10 |
| `a3-script` v4 | 90-second home-language script | 14/14 first pass |
| `a4-check` v3 | content check alongside 6 code checks, one retry | 8/8 (11/11 incl. Hindi) |
| `a8-lesson-recap` v1 | evening lesson audio | 13/15 first pass |

Sarvam is used for STT (`saaras:v4`) and TTS (`bulbul:v3`, re-encoded to Opus 24 kbps, ~55 s / ~150 KB per catch-up). Numbers, candidate sets, buddies, scores and pacing are all computed in code.

**Demo and ops:** `/demo` has two phones, Reset, the simulated SATS feed, **Send messages now**, and Under the hood with a "For" column (T-18, T-73). It has two stories: `judges` (prod) and `video` (staging), with a 6-step guide each (T-70, T-74). `Demo:Today` fixes the date (T-75, T-80). The demo class has a register every day (T-69). Admin pages for greetings and lesson audio sit behind `Admin:Key` (T-66–T-68). The app is one container on Azure App Service with Postgres and blob storage.

**Docs and evidence:** the README covers both ways to test, logins, sample inputs and a "For judges" table (T-65, T-77, T-79). User testing so far: P1 and P2 (teachers), P4 and P5 (parents), findings F-01–F-09, two mentor sessions (19 Oct education, 24 Oct technical) in `docs/user-testing.md`. Decisions are D-01 … D-88 in `spec.md` §8.

## Next

1. Run the 6 demo steps once on prod (`v1.1` is deployed; the run hasn't been done yet).
2. After judging, change the server PINs.
3. Fix the Tamil layout gap below, if wanted (left out of `v1.0` and `v1.1` on purpose).

## Known gaps and drafts

- **Tamil layout at 360 px (found 1 Nov, not fixed in `v1.0` or `v1.1`):** the teacher bottom-nav label "திரும்பியவர்கள்" (~100 px in a 64 px slot) runs into the Week tab, because `BottomNav.tsx` uses `[word-break:keep-all]`; and on `/demo` the "Under the hood" tab (திரைக்குப் பின்னால்) makes the page 32 px wide, because the tabs in `DemoScreen.tsx` have no `min-w-0`. Likely fix: `min-w-0` plus `[overflow-wrap:anywhere]` on both. Telugu, Kannada, Hindi and English fit.
- Telugu, Tamil and Hindi text (UI, scripts, evening messages, lesson names) are **drafts for native review** (T-26). The Hindi audio hasn't been listened to on a phone.
- Only Grade 3 maths is loaded: the whole book, 60 competencies (T-81, branch `feat/T-81-textbook-2026-27`, not merged). Only 13 have a practice rule, so the other 47 get no catch-up yet. WhatsApp and SATS are simulated. Each server has one shared class.
- Roadmap (D-70): a Families screen and consent editing (T8), per-language greetings, withdraw voice consent, real WhatsApp delivery, offline beyond attendance and voice notes.
- Staging's fixed date: at midnight IST the clock goes back to 00:00 the same day; a Reset tidies it.

## Recent changes

- 1 Nov: prod released as `v1.1` (T-82 evening send waits for lesson audio, T-83 video story Reset text).
- 1 Nov: the video story's Reset dialog showed the raw key `demo.reset.bodyVideo`; text added in all locales, plus a test for keys missing from the locale files (T-83).
- 1 Nov: the 6 pm job waits (until 9 pm) for a ticked lesson clip she hasn't approved, so Approve clip no longer hits 409 after a send (T-82).
- 1 Nov: T-81 built on a branch, held until after judging: the curriculum follows the textbook PDFs (60 lessons, 12 chapters, D-89), and lessons without a practice rule are left out of the candidate set. A1 eval 13/13 with 60 lessons; the seeded Hindi catch-up regenerated live (page 80, verified, no names). Eval cases moved to the new book (6 A2 cases rechecked on its graph; A8 now covers tables and half); evals on them: A2 10/10, A3 14/14 ready (13/14 first pass), A8 15/15, 14/15 first pass (after adding table rows to the tables lesson's examples; it was 9/15).
- 1 Nov: prod released as `v1.0`; staging checked at 360 px in Telugu and Tamil (Tamil layout gap logged above).
- 1 Nov: README refresh for judges (same-app note, PIN on deck slide 14, second lesson sample); this file shortened, with history moved to `status-history.md` (#171).
- 1 Nov: fixed demo date kept after a reload (T-80, #170); README "two ways to test" and logins (T-79, #169).
- 1 Nov: Telugu and Tamil teacher strings (T-76, #166); video story guide and fixed staging date (T-74, T-75, #165).
- 31 Oct: Under the hood "For" column (T-73), Savitha rename (T-72), Settings (T-71), video story (T-70), demo on any day (T-69).
