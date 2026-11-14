# Status and handoff

Read this first in any new session, then `CLAUDE.md`, `docs/intent.md`, `docs/spec.md`, `docs/plan.md`, `docs/tasks.md`.
Keep it short: at the end of a session, update **Where things stand** and **Next**, and add one dated line under **Recent changes**. The full session-by-session log up to 1 Nov is in [`status-history.md`](status-history.md).

## Where things stand (14 Nov 2026)

- **Grand Finale, Wed 18 Nov** (Top 10, shortlisted 12 Nov; build stage submitted 31 Oct). Finale Must rows on `main`: T-87, T-88 to T-92 (#200, #201) and T-105. T-93 (#190) is merged in part (#196), T-94 (#191) is open, and the Should rows T-95 to T-103 are still proposed. After judging: T-84 (Tamil layout, #176, no row in `tasks.md` yet) and T-86 (Today card, #177).
- **Repo:** https://github.com/rahul-oswal07/slate-ai (public, MIT). Merges to `main` deploy to staging; `v*` tags deploy to prod.
- **Staging** (https://slateai-staging.azurewebsites.net, the judges' guided demo at `/demo`): on `main`. Settings `Demo__TeacherPin`, `Demo__CoTeacherPin`, `Admin__Key`, and WhatsApp (T-40): `Delivery__Channel=whatsapp`, `WhatsApp__AccessToken`, `WhatsApp__PhoneNumberId`, one `WhatsApp__Recipients__*`.
- **Prod** (https://slateai-prod.azurewebsites.net, the "real app"): on **`v1.3`** (tagged 1 Nov at #178: T-85; `v1.2` was #173, T-81; `v1.1` was #174). Judges' story, real date, no `Admin__Key`. The old `EveningMessages__LessonAudioWaitUntil` setting is still there but unused since `v1.3`; delete it after judging.
- **Logins:** Shobha `9000000000` (class teacher), Savitha `9123456789` and Monika `9123456788` (co-teachers), same class, on both servers. All use one PIN, which is on slide 14 of the submitted deck and not in the README. It equals the co-teacher PIN in `appsettings.Development.json`, so change the server PINs after judging (clear the stored `PinHash`; the seeder never overwrites one).
- **Tests:** `verify.sh` OK, 729 API and 379 web tests (14 Nov); Db suite 19/19. The Db suite runs in CI. 12 existing e2e specs, no new ones (22 Oct rule).

## What has been built

**Teacher app** (React PWA, Kannada / English / Hindi / Telugu / Tamil):
- **Sign-in and setup:** phone + PIN, lockout after 5 wrong PINs, 30-day session (T-47). First-run Welcome, a 10-second greeting with voice consent (T-61), and approving the evening message once (T-44). Several teachers can share one class (D-79). Settings for the greeting and the evening message (T-71).
- **Today:** a simulated SATS register arrives at 12:00 (retry 2 pm). The teacher's marking wins over a late feed, and the register works offline through the Dexie outbox (T-12, D-69). There is a day strip (T-49). The lesson comes from a Kannada voice note (Sarvam STT → name scrubber → A1) or a photo of the textbook page (T-07, T-64), or is picked from the list. The tonight card has optional lesson audio, which the teacher approves clip by clip (T-62).
- **Today's message** (finale, D-90–D-96): confirming a lesson with the book's exercises (today only Nalikali p 4) drafts a message for absent families: concept with voice, a parent-led activity and a `>,<,=` picture pair chosen by code (T-88). After the send, Today shows "sent to N · M answered" and a child · answer · level table (T-92).
- **Check and approve:** two tabs. **Today's message** has sections she can edit, remove or add a note to; her Kannada is kept word for word and the other languages are written from it, and the tick is bound to the version (T-89). **Catch-ups** are the week's catch-ups, one per lesson group and language. Each has a segment player, a Kannada summary, **Why this**, the picture question, Simpler / Shorter, and a tick before Approve, bound to the version she read (T-14).
- **Missed lessons:** by child or by lesson, child history, and deleting a child's data (T-13, T-19).
- **Back today:** buddy cards with the buddy chosen from attendance, 3 sums made by code, textbook words and a print view (T-16).
- **Week:** return speed this week and last, reasons for absence, the pacing nudge, attendance stars (T-46, T-63).
- Slate & chalk look, AAA tokens, light and dark (D-77).

**Families** (`/p/<token>`, no sign-in): the evening message with a reason in 5 pictures (T-44, T-45). With today's message the link opens step by step: her voice and text → "Is everything OK?" (the reason, once) → concept → activity → `>,<,=` in sticks, scored by code → Done (T-90, T-91). The catch-up flow is Listen → activity with sticks → 3-picture check scored by code → Done. A stop link is on every page (T-15).

**AI** (all through `Ai/ILlmClient`, logged to `AiCalls`, no names sent):

| Prompt | Job | Latest eval |
|---|---|---|
| `a1-lesson-mapping` v3 | voice note / photo → competencies, with each lesson's page | 16/16 (13 Nov) |
| `a2-compression` v2 | choose 1–2 lessons from the code's candidate set, reasons in English and Kannada | 10/10 |
| `a3-script` v4 | 90-second home-language script | 14/14 first pass |
| `a4-check` v3 | content check alongside 6 code checks, one retry | 8/8 (11/11 incl. Hindi) |
| `a8-lesson-recap` v1 | evening lesson audio | 13/15 first pass |
| `a9-todays-message` v2 | today's concept and activity; other languages from her Kannada | 15/15 first pass (14 Nov) |

Sarvam is used for STT (`saaras:v4`) and TTS (`bulbul:v3`, re-encoded to Opus 24 kbps, ~55 s / ~150 KB per catch-up). Numbers, candidate sets, buddies, scores and pacing are all computed in code.

**Demo and ops:** `/demo` has two phones, Reset, the simulated SATS feed, **Send messages now**, and Under the hood with a "For" column (T-18, T-73). It has one story on the real date, told through one child: Manjunath (Kannada), away the two days before today and today (T-93, D-94). The video story and the fixed 31 Oct date were removed on 14 Nov (T-105). The demo class has a register every day (T-69). Admin pages for greetings and lesson audio sit behind `Admin:Key` (T-66–T-68). The app is one container on Azure App Service with Postgres and blob storage.

**Docs and evidence:** the README covers both ways to test, logins, sample inputs and a "For judges" table (T-65, T-77, T-79). User testing so far: P1 and P2 (teachers), P4 and P5 (parents), findings F-01–F-09, two mentor sessions (19 Oct education, 24 Oct technical) in `docs/user-testing.md`. Decisions are D-01 … D-98 in `spec.md` §8.

## Next

**Grand Finale, Wed 18 Nov:** 14 minutes (5 min slides, 3 min live demo, 6 min Q&A); presentation and demo links due **Mon 16 Nov**, so builds merge by **Sun 15 Nov**. Jury feedback, demo rules and guidelines: `hackathon-requirements.md` §5. Rahul approved the Must rows T-87 to T-94 and D-90 to D-96 on 13 Nov. Demo lesson: Nalikali Maths Part 1 p 4, more–less–equal (`curriculum.md`).

1. **Grand Finale build: T-94 next** (DIKSHA link per lesson, #191; Monika finds and checks the link, M-1), then the Should rows (T-101 first). The D-94 loop runs end to end on a local server and everything it needs is on `main`, so on staging only the steps below are left.
   - **To finish T-93 (#190):** record the sample voice note (`docs/demo-script.md`) as `data/seed/demo-voice-note.webm`; set `Demo__SendOnApprove=true` on staging; do the timed 3-minute run on staging twice from Reset and log it here.
   - The pre-made catch-up `data/seed/demo-catchup.json` is Hindi, so Reset no longer uses it (no story child speaks Hindi). Export a Kannada one from a live run if we want the R5 fallback.
2. **WhatsApp (T-40, D-97, D-98): step 1 merged (#180), on staging with Meta's test number.** Token, phone number ID and one recipient (Rahul's phone, as Ravi) are set; the admin `hello_world` test send reached the phone from staging. Until our templates are approved, WhatsApp deliveries send `hello_world`, hardcoded in `WhatsAppChannel`. On 14 Nov Monika submitted `todays_lesson_link_v1`, `todays_lesson_chat_v1` and `catchup_link_v1` (en/kn/te/ta/hi, in review; the earlier `evening_absence_v1` / `catchup_ready_v1` were flagged as Marketing; wording in [`whatsapp-setup.md`](whatsapp-setup.md) §3).
   - **T-104 (#182), build before approval matters:** today's message sends `todays_lesson_link_v1`, the catch-up sends lesson names and pages, decide the D-21 fallback, and check the templates are in the same WhatsApp Business account as staging's number.
   - Step 2 (T-106, today's message inside the chat) after the final.
3. Run the 6 demo steps once on prod (`v1.3` is deployed; the run hasn't been done yet), including the lesson audio: tick, approve within 5 minutes, and check the clip goes with the message.
4. After judging, change the server PINs.
5. After judging: the Tamil layout gap (T-84, #176) and one Tonight's message card on Today (T-86, #177).

## Known gaps and drafts

- **Tamil layout at 360 px (found 1 Nov, not fixed in `v1.0` or `v1.1`):** the teacher bottom-nav label "திரும்பியவர்கள்" (~100 px in a 64 px slot) runs into the Week tab, because `BottomNav.tsx` uses `[word-break:keep-all]`; and on `/demo` the "Under the hood" tab (திரைக்குப் பின்னால்) makes the page 32 px wide, because the tabs in `DemoScreen.tsx` have no `min-w-0`. Likely fix: `min-w-0` plus `[overflow-wrap:anywhere]` on both. Telugu, Kannada, Hindi and English fit.
- Telugu, Tamil and Hindi text (UI, scripts, evening messages, lesson names) are **drafts for native review** (T-26). The Hindi audio hasn't been listened to on a phone.
- Only Grade 3 maths is loaded: the whole book, 60 competencies (T-81, on prod since `v1.2`). Only 13 have a practice rule, so the other 47 get no catch-up yet. SATS is simulated; WhatsApp is simulated unless `Delivery:Channel=whatsapp` (T-40). Each server has one shared class.
- Roadmap (D-70): a Families screen and consent editing (T8), per-language greetings, withdraw voice consent, real WhatsApp delivery, offline beyond attendance and voice notes.

## Recent changes

- 14 Nov: this file tidied (docs only): Where things stand and Next brought up to date (the stacked PRs are merged, duplicate Next items removed, the fixed-date gap dropped after T-105), today's message added to What has been built, A1 v3 and A9 v2 in the AI table, decisions up to D-98.
- 14 Nov: today's family page asks "Is everything OK?" right after the teacher's voice (#201, closes #188, D-98): the evening page's reason screen as a step, Send or Skip moves on, not asked again once given; the first step is now her voice and the message text, then the reason, then the concept. Db test: a reason sent from today's link is stored for the day. #200 merged T-88 to T-92 on `main`; #186, #187, #189 closed by hand.
- 14 Nov: answers back to the teacher (T-92, #189): Today shows "Today's message: sent to N · M answered" and a child · answer · level table, levels by code (D-95), refreshing every 5 s until everyone has answered; Missed lessons (by child) shows the same level tag. Live on a local server: the child's tap showed on Today 5 s later without a reload. axe found one contrast failure in the new tag (amber text 4.44:1); fixed by ink text with the colour on the border and icon.
- 14 Nov: family page for today's message and the picture question (T-91 + T-90, #188, #187): the evening link with today's message opens step by step (her voice, text and page, her note, concept with voice → activity → video if any → `>,<,=` in sticks → Done ⭐ and the reason entry); code scores each tap, one more try after a first that isn't right, taps stored (migration `TodaysAnswer`); the answer never reaches the phone; `TodaysLevel` gives D-95's levels for T-92. Live in Kannada at 360 px on a local server, axe clean. Seen for T-101: "Manjunathಯ" and the evening text's "ಪಠ್ಯಪುಸ್ತಕ" instead of Nalikali.
- 14 Nov: Approve → Today's message editor (T-89, #186): Approve has two tabs, Today's message first; sections open one at a time with Edit, Remove / Put back, Add my note and Choose another pair (from the book, answer by code); her Kannada goes through A9's checks and is kept word for word, A9 v2 (`teacherKannada`) writes the other languages from it, concepts voiced again; the tick is bound to the version. The Approve title is now "Check and approve". First live run of T-88 + T-89 on a local server: draft in 20 s, a refused edit lists the book's numbers, a Kannada-only edit redrafts in 2 s with no AI call, Approve sends at once in the demo and the family page carries it. A9 v2 eval 15/15 first pass.
- 14 Nov: today's message, API and generation (T-88, #185): `Features/TodaysMessage/`, migration `TodaysMessage`. Confirming a lesson with the book's exercises (D-93; today only Nalikali p 4) starts a draft: A9 (`a9-todays-message` v1) writes the concept and a parent-led activity in Kannada and each absent family's language, code picks the picture pair and answer, each concept is voiced. She approves a version (any edit clears it); the evening send attaches only an approved current version, re-checked under the lock, and its concept clip replaces the T-62 lesson clip; otherwise the plain message goes. Demo: Approve sends at once. A9 eval 10/10 first pass. Guardrail 3 in CLAUDE.md now names today's message. No UI yet (T-89, T-91).
- 14 Nov: finale demo story (T-93, #190, in part): Reset follows one child, Manjunath (Kannada), and writes today's register stamped 12:00 IST; "Bring in attendance" before noon reads 12:00, not the time of the tap; the family phone reads "Manjunath's family's phone · Kannada"; with `Demo:SendOnApprove` (on in Development, for staging) Approve sends a catch-up at once and says so; Today offers "Use the sample voice note (demo)" when `data/seed/demo-voice-note.webm` exists (clip not recorded yet). `/api/me` carries `demo` flags for the demo teacher. Guide, Reset text and README follow the new story. Db suite 17/17.
- 14 Nov: WhatsApp templates for today's message and the catch-up (D-98, docs only): three submitted by Monika, wording in `whatsapp-setup.md` §3; T-104 rewritten to build them first; T-91 keeps the reason for absence; T-106 (step 2, in-chat lesson) proposed.
- 14 Nov: removed the 31 Oct demo setup (T-105, #193): the fixed date (`Demo:Today`, `DemoClock`, `X-School-Today`) and the video story (`Demo:Story=video`, its seeded week and scenario file, the evening backfill, the video guide). Staging and local run on the real date with the judges' story; `Demo__Today` and `Demo__Story` come off staging after the deploy.
- 13 Nov: Nalikali demo lesson (T-87, #184): `M3.BRIDGE.COMPARE` (more, less, equal; Nalikali Part 1 p 4) in chapter 0 with the book's exercises (`exercises`, answers by `ExerciseAnswers`); `textbookPage.book` `nalikali` shows "Nalikali Maths · Part 1 · p 4" on lesson confirm, the picker, Today, Back today and the family pages; A1 v3 gets each lesson's page (eval 16/16). The other 60 lessons are unchanged. Rahul approved the finale's Must rows: T-87–T-94 open (#184–#191), D-90–D-96 accepted.
- 13 Nov: Grand Finale proposal (docs only, nothing built): jury feedback J1–J4, demo rules, guidelines G1–G5; D-90–D-96 proposed; T-87–T-103 proposed; Nalikali p 4 lesson content in `curriculum.md`. WhatsApp Cloud API test number tried: one message delivered, then Meta restricted the account (error 131031), so it's parked for the roadmap.
- 13 Nov: WhatsApp delivery step 1 (T-40, D-97, FR-16 now M): `ParentDeliveries` outbox queued in the send transaction, dispatcher on the 6 pm job's tick and Send messages now, demo channel by default, WhatsApp template + URL button to registered test phones only; the evening template must copy the approved wording version; admin `POST /api/admin/whatsapp/test` sends Meta's `hello_world` to check the setup before our templates are approved. Migration `ParentDelivery`. Meta setup not started.
- 1 Nov: prod released as `v1.3` (T-85). The lesson audio wait is now 5 minutes from when the clip is ready (or from 6 pm), with no 9 pm cutoff (T-85, #178). Staging's stop-gap setting is deleted; prod's is unused. `v1.2` (T-81, the textbook curriculum) went to prod earlier the same evening.
- 1 Nov: Approve clip on staging returned 409 when testing after 9 pm IST (Reset at 21:00, day sent before approval). Set `EveningMessages__LessonAudioWaitUntil=23:59` on staging and prod; no code change.
- 1 Nov: prod released as `v1.1` (T-82 evening send waits for lesson audio, T-83 video story Reset text).
- 1 Nov: the video story's Reset dialog showed the raw key `demo.reset.bodyVideo`; text added in all locales, plus a test for keys missing from the locale files (T-83).
- 1 Nov: the 6 pm job waits (until 9 pm) for a ticked lesson clip she hasn't approved, so Approve clip no longer hits 409 after a send (T-82).
- 1 Nov: T-81 built on a branch, held until after judging: the curriculum follows the textbook PDFs (60 lessons, 12 chapters, D-89), and lessons without a practice rule are left out of the candidate set. A1 eval 13/13 with 60 lessons; the seeded Hindi catch-up regenerated live (page 80, verified, no names). Eval cases moved to the new book (6 A2 cases rechecked on its graph; A8 now covers tables and half); evals on them: A2 10/10, A3 14/14 ready (13/14 first pass), A8 15/15, 14/15 first pass (after adding table rows to the tables lesson's examples; it was 9/15).
- 1 Nov: prod released as `v1.0`; staging checked at 360 px in Telugu and Tamil (Tamil layout gap logged above).
- 1 Nov: README refresh for judges (same-app note, PIN on deck slide 14, second lesson sample); this file shortened, with history moved to `status-history.md` (#171).
- 1 Nov: fixed demo date kept after a reload (T-80, #170); README "two ways to test" and logins (T-79, #169).
- 1 Nov: Telugu and Tamil teacher strings (T-76, #166); video story guide and fixed staging date (T-74, T-75, #165).
- 31 Oct: Under the hood "For" column (T-73), Savitha rename (T-72), Settings (T-71), video story (T-70), demo on any day (T-69).
