# Docs map

Everything about one screen is in that screen's folder. Start with [`status.md`](status.md) for where the project stands, then come here to find the files for the work in hand.

## Screens

Each folder has two files: **`spec.md`** (purpose, states, requirements, the decisions it rests on) and **`status.md`** (what works today, known gaps). Web paths are under `src/web/src/`, API paths under `src/api/SlateAi.Api/`.

**Teacher app**

| Screen | What it is | Route | Web | API | Design card | Docs |
|---|---|---|---|---|---|---|
| T0 Set-up | Welcome, her greeting with voice consent, approving the evening message once | `/welcome`, `/setup/*` | `features/setup/` | `Greeting`, `EveningMessages` | [T0](design/T0-setup.md) | [spec](screens/T0-setup/spec.md) · [status](screens/T0-setup/status.md) |
| T1 Today | Attendance, recording the lesson, tonight's message, today's answers | `/` | `features/today/` | `Attendance`, `Lessons`, `LessonAudio` | [T1](design/T1-today.md) | [spec](screens/T1-today/spec.md) · [status](screens/T1-today/status.md) |
| T2 Lesson confirm | The matched lessons as a checklist; the lesson picker | `/lesson` | `features/lessons/` | `Lessons` | [T2](design/T2-lesson-confirm.md) | [spec](screens/T2-lesson-confirm/spec.md) · [status](screens/T2-lesson-confirm/status.md) |
| T3 Check and approve | Today's message (edit, approve) and the week's catch-ups | `/approve` | `features/approve/` | `TodaysMessage`, `CatchUps` | [T3](design/T3-approve.md) | [spec](screens/T3-approve/spec.md) · [status](screens/T3-approve/status.md) |
| T4 Missed lessons | The ledger by child or lesson, child history, delete a child's data | `/missed` | `features/missed/` | `Ledger`, `Children` | [T4](design/T4-missed-lessons.md) | [spec](screens/T4-missed-lessons/spec.md) · [status](screens/T4-missed-lessons/status.md) |
| T5 Back today | Buddy cards for children who are back | `/back` | `features/back/` | `Buddy` | [T5](design/T5-back-today.md) | [spec](screens/T5-back-today/spec.md) · [status](screens/T5-back-today/status.md) |
| T6 Week | Return speed, reasons, pacing nudge, attendance stars | `/week` | `features/week/` | `Week`, `Stars` | [T6](design/T6-week.md) | [spec](screens/T6-week/spec.md) · [status](screens/T6-week/status.md) |
| T8 Families | Consent, parent phone and home language per child. **Roadmap, not built** | none | none | none | [T8](design/T8-families.md) | card only |
| T9 Sign in | Phone number and PIN | `/login` | `features/auth/` | `Auth` | [T9](design/T9-sign-in.md) | [spec](screens/T9-sign-in/spec.md) · [status](screens/T9-sign-in/status.md) |
| T10 Settings | Her greeting and the evening message, after set-up | `/settings` | `features/settings/` | `Greeting`, `EveningMessages` | [T10](design/T10-settings.md) | [spec](screens/T10-settings/spec.md) · [status](screens/T10-settings/status.md) |
| T11 Profile | Her name, the last 4 digits of her phone, her class; read only. Opened from the header's profile menu | `/profile` | `features/profile/` | `Auth` | none yet | [spec](screens/T11-profile/spec.md) · [status](screens/T11-profile/status.md) |

**Family pages** (`/p/:token`, no sign-in; web `features/parent/`, API `Parent`)

| Screen | What it is | Design card | Docs |
|---|---|---|---|
| P0 Evening message and reason | Her greeting, the message for a day of absence, the reason in 5 pictures | [P0](design/P0-messages-reason.md) | [spec](screens/P0-evening-message/spec.md) · [status](screens/P0-evening-message/status.md) |
| P1–P4 Catch-up at home | Listen → Activity → Answer → Done, and the stop link | [P1](design/P1-listen.md) · [P2](design/P2-activity.md) · [P3](design/P3-answer.md) · [P4](design/P4-done.md) | [spec](screens/P1-P4-catch-up/spec.md) · [status](screens/P1-P4-catch-up/status.md) |
| P5 Today's message at home | Her voice, the reason, concept, activity, the `>,<,=` question, Done | none yet | [spec](screens/P5-todays-message/spec.md) · [status](screens/P5-todays-message/status.md) |

**Other**

| Screen | What it is | Route | Web | API | Docs |
|---|---|---|---|---|---|
| Demo shell | Teacher phone and family phone side by side, Reset, Under the hood | `/demo` | `features/demo/` | `Demo` | [spec](screens/demo/spec.md) · [status](screens/demo/status.md) |
| Admin pages | Play teachers' greetings and a day's lesson audio, behind `Admin:Key` | `/admin/*` | `features/admin/` | `Admin` | [spec](screens/admin/spec.md) · [status](screens/admin/status.md) |

## Areas (not a screen)

Same two files per folder.

| Area | What it is | Code | Read with | Docs |
|---|---|---|---|---|
| AI pipeline | Prompts A1–A9, validators, the verifier, speech, evals | `Ai/`, `Speech/`, `Features/CatchUps/`, `prompts/`, `evals/` | [`spec.md`](spec.md) §5, [`quality.md`](quality.md) §2 | [spec](areas/ai-pipeline/spec.md) · [status](areas/ai-pipeline/status.md) |
| Delivery | The 6 pm send job, the outbox, WhatsApp | `Features/Delivery/`, `Features/EveningMessages/` | [`whatsapp-setup.md`](whatsapp-setup.md), [`whatsapp-delivery-plan.md`](whatsapp-delivery-plan.md) | [spec](areas/delivery/spec.md) · [status](areas/delivery/status.md) |
| Curriculum | Lessons, prerequisites, pages, practice rules | `Curriculum/`, `data/curriculum/` | [`design/curriculum.md`](design/curriculum.md) | [spec](areas/curriculum/spec.md) · [status](areas/curriculum/status.md) |
| App shell and look | Header, bottom nav, language, offline outbox, tokens and themes | `src/web/src/app/`, `components/`, `styles/`, `i18n/`, `offline/` | [`design/tokens.md`](design/tokens.md), [`spec.md`](spec.md) §3.4 and §6 | [spec](areas/app-shell/spec.md) · [status](areas/app-shell/status.md) |

## Everything else

| File or folder | What it holds |
|---|---|
| [`status.md`](status.md) | Project-level state: the finale, servers, logins, what is next |
| [`intent.md`](intent.md) | Why the product exists and for whom (Monika) |
| [`spec.md`](spec.md) | What cuts across screens: scope, the FR index, NFRs, copy rules, architecture, AI design, offline, privacy. Section numbers are fixed |
| [`tasks/`](tasks/README.md) | One file per live task: criteria and dated notes |
| [`decisions/`](decisions/README.md) | D-01 to D-99 as a frozen log; one file per decision after that |
| [`database.md`](database.md) | The schema. Updated in the same PR as any migration |
| [`quality.md`](quality.md) | Test layers, eval design and results, usability testing |
| [`plan.md`](plan.md) | The build-stage plan (order of work, risks, branching) |
| [`how-it-works.md`](how-it-works.md) | The product explained in plain words |
| [`user-testing.md`](user-testing.md) | Sessions and findings F-01 onwards (Monika) |
| [`design/`](design/README.md) | Monika's design cards, tokens, journey, prototype, screenshots |
| [`whatsapp-setup.md`](whatsapp-setup.md), [`whatsapp-delivery-plan.md`](whatsapp-delivery-plan.md) | Meta setup and template wording; the design of delivery step 1 |
| [`hackathon-requirements.md`](hackathon-requirements.md), [`judging-narrative.md`](judging-narrative.md), [`demo-script.md`](demo-script.md), `mentor-*.md` | The hackathon: rules, jury feedback, the story for judges, mentor sessions |
| [`lessons.md`](lessons.md) | Bugs and the test that now guards each |
| `screenshots/` | PR screenshots, one folder per task |
| [`archive/`](archive/) | History as it was written: the session log, tasks until 15 Nov, finished plans |

## Rules for editing (so two people don't collide)

1. **A feature PR writes to its own task file and to the `status.md` of the screens it changed.** It touches the top-level [`status.md`](status.md) only for a release, a change to a server or a change of priorities.
2. **Dated notes go in the task's file**, under Notes, newest first. A screen's `status.md` says what is true now and has no dates.
3. **A requirement lives in one place:** the spec of the screen or area that owns it. Others cite the ID. `spec.md` §2 is the index.
4. **New task:** next free `T-` number, one GitHub issue, one file in `tasks/`. **New decision:** next free `D-` number, one file in `decisions/`. Never add to the frozen log or the archive.
5. **One fact per bullet.** No paragraphs with five facts on one line, and no dated notes inside a table cell: two edits to the same line always conflict.
6. **Don't renumber `spec.md` sections or rename `design/` cards:** code comments cite them (`spec §5.4`, `docs/design/T1-today.md`).
7. Run `./scripts/check-docs.sh` (part of `./scripts/verify.sh`): it fails on a task or decision number used twice and on a link to a file that isn't there.

## Adding a screen

Create `screens/<ID>-<slug>/spec.md` and `status.md` (copy a small one such as [`T5-back-today`](screens/T5-back-today/spec.md)), add a row to the table above, and ask Monika for a design card in `design/`.

## Where the old sections went (15 Nov 2026, T-110)

| Was | Now |
|---|---|
| `status.md` · Where things stand, Next | still in `status.md`, shorter |
| `status.md` · What has been built, Known gaps | each screen's or area's `status.md` |
| `status.md` · Recent changes | `archive/status-history.md`; new notes in the task's file |
| `status-history.md` | `archive/status-history.md` |
| `spec.md` §2 requirement rows | the owning screen's or area's `spec.md`; §2 keeps an index |
| `spec.md` §3.1 screen rows | each screen's `spec.md` |
| `spec.md` §8 decision log | `decisions/D-01-to-D-99.md` |
| `tasks.md` · tasks not finished | one file each in `tasks/` |
| `tasks.md` · everything shipped or dropped | `archive/tasks-until-2026-11-15.md` |
| `T-63-attendance-stars.md`, `board-photo-plan.md` | `archive/` |
