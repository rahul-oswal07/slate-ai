# Status and handoff

Read this first in any new session. Then the map in [`README.md`](README.md), then the task's file and the screen folders for the work in hand; `intent.md` and `spec.md` before changing behaviour.

**This file is project-level only:** the finale, the servers, the logins, what is next. It changes on a release, a change to a server or a change of priorities, not in every PR.

| For | Look in |
|---|---|
| What a screen does today, and its gaps | `screens/<screen>/status.md`, `areas/<area>/status.md` |
| What was done on a task, and when | the task's file in [`tasks/`](tasks/README.md) |
| Everything before 15 Nov | [`archive/status-history.md`](archive/status-history.md), [`archive/tasks-until-2026-11-15.md`](archive/tasks-until-2026-11-15.md) |

## Where things stand (15 Nov 2026)

- **Grand Finale, Wed 18 Nov** (Top 10, shortlisted 12 Nov; build stage submitted 31 Oct): 14 minutes (5 min slides, 3 min live demo, 6 min Q&A); presentation and demo links due **Mon 16 Nov**, so builds merge by **Sun 15 Nov**. Jury feedback, demo rules and guidelines: `hackathon-requirements.md` §5. Demo lesson: Nalikali Maths Part 1 p 4, more–less–equal (`design/curriculum.md`).
- **Finale build on `main`:** the Must rows T-87 to T-92, T-105 and T-108; T-93 in part. Of the Should rows: T-101 (#211), T-107 (#206), part of T-96 (#207) and part of T-100 (#212).
- **Not on `main`:** T-94 (open, waits for Monika's link). Proposed and not approved: T-95, T-97 to T-99, T-102, T-103, T-109 (#210) and the rest of T-96 and T-100.
- **Repo:** https://github.com/rahul-oswal07/slate-ai (public, MIT). Merges to `main` deploy to staging; `v*` tags deploy to prod.
- **Staging** (https://slateai-staging.azurewebsites.net, the judges' guided demo at `/demo`): on `main`. Settings `Demo__TeacherPin`, `Demo__CoTeacherPin`, `Admin__Key`, and WhatsApp (T-40): `Delivery__Channel=whatsapp`, `WhatsApp__AccessToken`, `WhatsApp__PhoneNumberId`, one `WhatsApp__Recipients__*`.
- **Prod** (https://slateai-prod.azurewebsites.net, the "real app"): on **`v1.3`** (tagged 1 Nov at #178: T-85; `v1.2` was #173, T-81; `v1.1` was #174). Judges' story, real date, no `Admin__Key`. The old `EveningMessages__LessonAudioWaitUntil` setting is still there but unused since `v1.3`; delete it after judging.
- **Logins:** Shobha `9000000000` (class teacher), Savitha `9123456789` and Monika `9123456788` (co-teachers), same class, on both servers. All use one PIN, which is on slide 14 of the submitted deck and not in the README. It equals the co-teacher PIN in `appsettings.Development.json`, so change the server PINs after judging (clear the stored `PinHash`; the seeder never overwrites one).
- **Hosting:** the app is one container on Azure App Service with Postgres and blob storage. Each server has one shared class.
- **Tests:** `verify.sh`, the Db suite and the e2e specs run in CI on every PR; the counts are in the CI run, not here. 12 existing e2e specs, no new ones (22 Oct rule).

## Next

1. **Finish T-93** ([task](tasks/T-93-demo-loop-and-reset.md)): record the sample voice note, set `Demo__SendOnApprove=true` on staging, do the timed 3-minute run on staging twice from Reset.
2. **Approve the evening message once on staging** (sign in as Shobha → Settings → Evening message): the wording is v4 (D-99), Reset doesn't approve it, and until she does no evening or today's message goes. Prod asks the same at its next tag.
3. **T-94**, DIKSHA link per lesson ([task](tasks/T-94-diksha-link-per-lesson.md)): waits for M-1 ([finale prep](tasks/M-1-to-M-6-finale-prep.md)).
4. **Should rows**, if time is left, in this order: T-95 → T-96 → T-100 → T-97 → T-98 → T-99 → T-102 → T-103. T-109 waits for Rahul's approval.
5. **T-104**, our own WhatsApp templates ([task](tasks/T-104-whatsapp-own-templates.md)); T-106 after the final. Where WhatsApp stands: [`areas/delivery/status.md`](areas/delivery/status.md).
6. **A problem that needs a task:** an approved today's message stays after the lesson is changed ([`screens/T3-approve/status.md`](screens/T3-approve/status.md)).
7. Run the 6 demo steps once on prod (`v1.3` is deployed; the run hasn't been done yet), including the lesson audio: tick, approve within 5 minutes, and check the clip goes with the message.
8. **After judging:** change the server PINs; delete the unused `EveningMessages__LessonAudioWaitUntil` setting on prod; one Tonight's message card on Today (T-86, [task](tasks/T-86-tonights-message-card.md)).

## Evidence

**Docs and evidence:** the README covers both ways to test, logins, sample inputs and a "For judges" table (T-65, T-77, T-79). User testing so far: P1 and P2 (teachers), P4 and P5 (parents), findings F-01–F-09, two mentor sessions (19 Oct education, 24 Oct technical) in `docs/user-testing.md`. Decisions are D-01 … D-99 in [`decisions/`](decisions/README.md).

## Roadmap

- Roadmap (D-70): a Families screen and consent editing (T8), per-language greetings, withdraw voice consent, real WhatsApp delivery, offline beyond attendance and voice notes.
