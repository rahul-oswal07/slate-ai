# Spec: Tappida Paatha MVP

| | |
|---|---|
| Stage | 2 · Design |
| Reads | `docs/intent.md`, `docs/hackathon-requirements.md` |
| Product owner (accepts) | Monika I — UX, content, scope |
| Technical owner (accepts) | Rahul Oswal — architecture, AI, data |
| Status | **Draft — awaiting acceptance.** Build starts when every `[DECIDE]`/`[CONFIRM]` item marked *Gate* in §8 is resolved |
| Layout (15 Nov 2026) | This file keeps what cuts across screens: scope, NFRs, copy rules, architecture, AI design, offline, privacy. Per-screen requirements and states are in `docs/screens/` and `docs/areas/`, decisions in `docs/decisions/`; map in [`README.md`](README.md). **Section numbers are kept** because code comments cite them |

Tags used in this file:
- **[DECIDE: Monika]** product or design call that needs Monika's approval
- **[CONFIRM: Rahul]** technical call that needs Rahul's confirmation
- Each tag belongs to a decision (§8, now in [`decisions/`](decisions/README.md)). Resolve it there, then update the text.

---

## 1. Scope

### 1.1 MVP (must ship by 31 Oct)

One class, one subject, one complete loop:

- **Class:** Grade 3, Kannada-medium, 36 seeded children; 6 children with Telugu home language, 3 with Tamil, the rest Kannada.
- **Subject:** maths. One textbook unit chain, as the Grade 3 book teaches it: place value (hundreds, tens, ones) → 3-digit addition without carrying → **addition with carrying** → subtraction without borrowing → **subtraction with borrowing**, sums ≤ 999. Since 1 Nov the curriculum is the whole Grade 3 book: 60 competencies in 12 chapters (`docs/design/curriculum.md`, D-89). The demo story still uses the place value → addition chain. D-01 (Accepted, revised 20 Oct and 1 Nov)
- **Languages:** development and the default UI locale are **English** (D-32); Kannada for the teacher UI and Telugu/Kannada/Tamil for parents are added through i18next from the design cards' copy tables and chosen per user. Catch-up audio in **Telugu** and **Kannada**, fully tested. Tamil works through the same pipeline but is only spot-checked. D-02. **Hindi (hi)** is a fifth language in the teacher and parent screens and as a family language (D-78, T-59, accepted 25 Oct); every Hindi string stays a draft for native review (T-26).

### 1.2 Stretch (only after the Day 7 code freeze for MVP features is met)

- Real WhatsApp delivery via WhatsApp Cloud API test number (max 5 recipient numbers). D-05. **Moved to MVP after judging (13 Nov, D-97, FR-16 = M); step 1 built in T-40.**

### 1.3 Not in scope

Voice cloning, avatar video, officer/CRP dashboards, a real SATS/VSK attendance integration (the MVP uses a simulated feed, D-27), other subjects, native app, multi-school admin. Dropped to make room for the mentor changes (D-26): board-photo capture (FR-17, T-41), spoken child answer (FR-18, T-42; replaced by the parent's optional spoken reason in FR-21), full Tamil review (T-43; Tamil stays spot-checked). Home visits by the cook or community members: roadmap only (D-25).

---

## 2. Functional requirements

Priority: **M** = must (MVP), **S** = should (stretch). IDs are referenced by tasks and tests.

Each requirement, with its acceptance, moved on 15 Nov 2026 (T-110) to the spec of the screen or area that owns it. A requirement lives in one place; other screens cite its ID. This table is the index.

| ID | P | Requirement | Kept in |
|---|---|---|---|
| FR-01 | M | Attendance from the school's system; the teacher only corrects | [`screens/T1-today/spec.md`](screens/T1-today/spec.md) |
| FR-02 | M | Voice note of what she taught | [`screens/T1-today/spec.md`](screens/T1-today/spec.md) |
| FR-03 | M | The note mapped to lessons, which she confirms | [`screens/T2-lesson-confirm/spec.md`](screens/T2-lesson-confirm/spec.md) |
| FR-04 | M | Pick the lesson from the list when audio or AI fails | [`screens/T2-lesson-confirm/spec.md`](screens/T2-lesson-confirm/spec.md) |
| FR-05 | M | Ledger: missed lessons with a learning status and a channel | [`screens/T4-missed-lessons/spec.md`](screens/T4-missed-lessons/spec.md) |
| FR-06 | M | Compression: at most 2 target lessons per child | [`areas/ai-pipeline/spec.md`](areas/ai-pipeline/spec.md) |
| FR-07 | M | Script writer: the catch-up script, homework, check and gloss | [`areas/ai-pipeline/spec.md`](areas/ai-pipeline/spec.md) |
| FR-08 | M | Teacher approval of catch-ups | [`screens/T3-approve/spec.md`](screens/T3-approve/spec.md) |
| FR-09 | M | Audio: greeting, spoken AI disclosure, script | [`areas/ai-pipeline/spec.md`](areas/ai-pipeline/spec.md) |
| FR-10 | M | Parent page from a link | [`screens/P1-P4-catch-up/spec.md`](screens/P1-P4-catch-up/spec.md) |
| FR-11 | M | Picture-tap answer scored by code | [`screens/P1-P4-catch-up/spec.md`](screens/P1-P4-catch-up/spec.md) |
| FR-12 | M | Return-day buddy card | [`screens/T5-back-today/spec.md`](screens/T5-back-today/spec.md) |
| FR-13 | M | Weekly pacing nudge | [`screens/T6-week/spec.md`](screens/T6-week/spec.md) |
| FR-14 | M | Demo mode | [`screens/demo/spec.md`](screens/demo/spec.md) |
| FR-15 | M | Consent and deletion | [`screens/T4-missed-lessons/spec.md`](screens/T4-missed-lessons/spec.md) |
| FR-16 | M | Send via WhatsApp | [`areas/delivery/spec.md`](areas/delivery/spec.md) |
| FR-17 | S | One optional photo of the lesson | [`screens/T1-today/spec.md`](screens/T1-today/spec.md) |
| FR-18 | — | Spoken answer. Dropped (D-26) | [`areas/ai-pipeline/spec.md`](areas/ai-pipeline/spec.md) |
| FR-19 | M | Stop these messages | [`screens/P1-P4-catch-up/spec.md`](screens/P1-P4-catch-up/spec.md) |
| FR-20 | M | Evening message on each day of absence | [`screens/P0-evening-message/spec.md`](screens/P0-evening-message/spec.md) |
| FR-21 | M | Reason for absence | [`screens/P0-evening-message/spec.md`](screens/P0-evening-message/spec.md) |
| FR-22 | M | Return-speed card | [`screens/T6-week/spec.md`](screens/T6-week/spec.md) |
| FR-23 | S | Optional lesson audio on the evening message | [`screens/T1-today/spec.md`](screens/T1-today/spec.md) |
| FR-24 | S | Attendance stars | [`screens/T6-week/spec.md`](screens/T6-week/spec.md) |

### Non-functional requirements

| ID | Requirement |
|---|---|
| NFR-01 | Teacher time ≤ 60 s/day for lesson capture plus any attendance corrections (measured in usability tests) |
| NFR-02 | Teacher flow usable offline; queued actions sync automatically; sync state always visible |
| NFR-03 | Works on a 2 GB RAM Android phone, Chrome, 3G-class network (test with throttling) |
| NFR-04 | WCAG 2.2 AA for teacher and parent UIs, with AAA text contrast: all text ≥ 7:1, UI components ≥ 3:1, in light and dark themes (D-17); touch targets ≥ 48 px (parent picture targets ≥ 96 px) |
| NFR-05 | No child names, parent numbers or teacher names sent to the LLM or STT/TTS providers; IDs and placeholders only |
| NFR-06 | Every AI call logged: prompt version, model, tokens, latency, verifier verdict |
| NFR-07 | p95 lesson processing (upload → teacher sees mapped lesson) ≤ 20 s online |
| NFR-08 | Every AI-generated item shown to a parent or teacher carries an "AI-assisted, approved by your teacher" label |

---

## 3. UX and product decisions

### 3.1 Screens

Every screen and its states moved to its own folder on 15 Nov 2026 (T-110). The list of screens, with the route, code, design card, spec and status of each, is the table in [`README.md`](README.md).

- Teacher app (PWA, Kannada first): [`screens/T0-setup/`](screens/T0-setup/spec.md), [`T1-today/`](screens/T1-today/spec.md), [`T2-lesson-confirm/`](screens/T2-lesson-confirm/spec.md), [`T3-approve/`](screens/T3-approve/spec.md), [`T4-missed-lessons/`](screens/T4-missed-lessons/spec.md), [`T5-back-today/`](screens/T5-back-today/spec.md), [`T6-week/`](screens/T6-week/spec.md), [`T9-sign-in/`](screens/T9-sign-in/spec.md), [`T10-settings/`](screens/T10-settings/spec.md).
- Parent page (no install, opens from a link): [`screens/P0-evening-message/`](screens/P0-evening-message/spec.md), [`P1-P4-catch-up/`](screens/P1-P4-catch-up/spec.md), [`P5-todays-message/`](screens/P5-todays-message/spec.md).
- [`screens/demo/`](screens/demo/spec.md) and [`screens/admin/`](screens/admin/spec.md).

Screens without a folder:

| Screen | Purpose | Key interaction | States to design (besides default) |
|---|---|---|---|
| T7 Review queue | Dropped with FR-18 (D-26) | — | — |
| T8 Families and consent | Record the paper consent per child (D-39) | Yes / No / No phone per child; summary counts; reached from T0 "All set" and Missed lessons | Default · a family marked No · stopped by family (shown on the row) |

T8 is roadmap (D-70); its design card is [`design/T8-families.md`](design/T8-families.md).


### 3.2 Design decisions for Monika

| # | Decision | Recommendation |
|---|---|---|
| D-03 | Attendance UI: name list vs photo grid vs roll-number grid (now only for corrections and the fallback, D-27) | Roll number + first name grid, 4 columns; photos raise consent questions. **Accepted** (prototype v2, 20 Oct) |
| D-04 | Visual style and tokens (colour, type, iconography) for both UIs | Accepted: Build Kit palette (warm canvas, deep ink, teal primary), AAA text contrast, Noto Sans Kannada/Telugu/Tamil. See `docs/design/tokens.md` (D-17) |
| D-07 | Picture library style for the learning check | Code-generated bundles of ten sticks + loose sticks, the same style as the activity. The pictures are the answer: the numeral appears only after the tap. Options shuffled by code, distractors visibly different; plus ~10 activity illustrations (bottle caps, sticks, stones). **Accepted** (prototype v2, 20 Oct) |
| D-08 | Parent page: one screen with sections vs four steps | Four steps, one action per screen, step dots, audio auto-prompts, Next never blocked by audio. **Accepted** (prototype v2, 20 Oct) |
| D-09 | How the teacher reviews a Telugu script she cannot read | Kannada gloss + audio; approval wording "I've checked the Kannada summary"; the tick is required and is cleared by any regenerate. **Accepted** (prototype v2, 20 Oct) |

### 3.3 Content rules for generated scripts

- Child addressed by first name via placeholder `{child}`; inserted after generation.
- 90 seconds maximum (≈ 160 spoken Telugu words; tuned on Day 3 by timing TTS output).
- Exactly 3 Kannada key words from the lesson, repeated twice each (the child returns to a Kannada classroom).
- Homework points to the child's own textbook/workbook page for the competency, with a household-object option from the allowed list (bottle caps, sticks, stones, tamarind seeds, bangles, rotis, spoons), and ends with "Show `{teacher}` when you come back" (D-22).
- Warm, encouraging, no negative words about absence. The pull back to school is always positive (teacher's voice, a buddy keeping the seat, homework to show); never shame or punishment.
- One learning check that tests the single most important target competency.

### 3.4 Interface copy rules (every screen, all four languages)

- **Voice:** a respectful colleague, never a supervisor. Kannada to the teacher uses the formal ನೀವು.
- **Short:** buttons and banners at most about 12 words. Buttons start with the verb ("Approve", "Listen", "Try again").
- **Errors** always say what happened, what is safe, and what to do next. Example: "Could not send. Your note is saved. It will send when you have network."
- **Numbers** as counts ("11 of 36 children"), never percentages. Numbers come from code (guardrail 4).
- **Confidence in words**, not scores: "This is…" when sure, "Looks like… Please confirm" when not.
- **Never:** exclamation marks, "Oops", error codes, abbreviations ("qty", "ETA"), "AI-powered", or blame words about absence ("failed to attend").
- **Numerals:** Latin digits, as in Karnataka textbooks.
- **Status** is always an icon plus a word, never colour alone (see `docs/design/tokens.md`).
- Indic copy stays marked "needs review" until a native speaker approves it.

---

## 4. Architecture

### 4.1 Overview

```
 Teacher phone (React PWA)                      Parent phone (browser, link)
  ├─ IndexedDB outbox (attendance, audio)        └─ /p/{token}: listen → activity → answer
  └─ sync on reconnect                                       │
            │ HTTPS (JSON + multipart)                       │
            ▼                                                ▼
 ┌──────────────────────── SlateAi.Api (.NET 10, one container) ────────────────────────┐
 │ Minimal API endpoints · serves built SPA from wwwroot                                │
 │ Features/  Attendance · Lessons · Ledger · CatchUp · Delivery · Parent · Pacing · Demo│
 │ Jobs/      in-process queue (Channel) for lesson processing, generation, sends       │
 │ Ai/        ClaudeClient (Anthropic SDK) · Prompts · Validators · Verifier · AiCallLog │
 │ Speech/    Sarvam STT/TTS client                                                     │
 │ Curriculum/ dependency graph loaded from data/curriculum/*.json                      │
 └───────────┬──────────────────────────┬────────────────────────┬──────────────────────┘
             ▼                          ▼                        ▼
     PostgreSQL (EF Core)        Blob storage (audio)     Claude API · Sarvam API · (WhatsApp)
```

Why one container: one URL for judges, no CORS, one deploy pipeline, fewer moving parts in 10 days. D-10 (Accepted)

### 4.2 Stack

| Layer | Choice | Notes |
|---|---|---|
| Front end | React 19, TypeScript, Vite, React Router, TanStack Query, i18next, vite-plugin-pwa (Workbox), Dexie (IndexedDB), Tailwind CSS v4 with Monika's tokens | |
| Back end | .NET 10 (LTS) Minimal APIs, EF Core 10 + Npgsql, `Anthropic` NuGet SDK, `Microsoft.Extensions.Http.Resilience` | D-11 accepted: official SDK behind `ILlmClient` |
| Database | PostgreSQL 16 | Docker locally |
| Audio storage | Postgres `AudioClips` for the MVP (T-11; a few MB a week), behind `IAudioStore` so Azure Blob Storage can replace it | Ogg Opus at 24 kbps (encoded with Concentus, pure C#) |
| Speech | Sarvam AI: Saaras STT (kn-IN; `saaras:v4`, configurable), Bulbul TTS (te-IN, ta-IN, kn-IN, hi-IN) | D-12 accepted: Sarvam; voice `bulbul:v3` / `priya` (T-11, `Sarvam:TtsSpeaker`; try others if a language sounds unnatural). Fallback: Google Cloud STT/TTS |
| LLM | Claude via API; models set in config | See §5.1 |
| Hosting | Azure App Service for Containers (Linux, Central India), Azure Database for PostgreSQL Flexible (B1ms), GitHub Actions; two web apps (staging, prod) on one B1 plan | D-13 (Accepted). Fallback: same Dockerfile on Render/Fly.io |
| Tests | xUnit, WebApplicationFactory with fake stores (no database); Vitest + Testing Library; Playwright + @axe-core/playwright | |

### 4.3 Data model (core tables)

Built schema, keys, indexes and locking: `docs/database.md`.

| Table | Key fields |
|---|---|
| `Teacher` | Id, DisplayName, Phone, PinHash, ConsentAt, ConsentVersion (D-44) |
| `TeacherGreeting` | Id, TeacherId, Language (kn/te/ta), AudioUrl (private blob), RecordedAt — one per language; all deleted when voice consent is withdrawn (D-44, D-52) |
| `Class` | Id, TeacherId, Grade, Medium (kn) |
| `Child` | Id, ClassId, RollNo, FirstName, HomeLanguage (from the roster, editable on Family details, D-47), ParentPhone? (entered on Family details, D-43; never sent to AI providers), Consent (Yes/No/NoPhone, a missing form = No) + ConsentChangedAt + ConsentChangedBy (D-39), ParentConsentAt?, ParentStoppedAt? (D-19), HasPhone |
| `Competency` | Id (e.g. `M3.ADD.CARRY`), Grade, NameKn, NameEn, PrerequisiteIds[], NipunOutcome, SequenceNo, TextbookPage (D-22) — loaded from `data/curriculum/` |
| `Lesson` | Id, ClassId, Date, Source (voice/photo/picked), Transcript?, CompetencyIds[], SummaryKn, ExamplesUsed, Confidence, Status |
| `Attendance` | ClassId, Date, ChildId, Present |
| `CatchUpPackage` | Id, ClassId, WeekOf, TargetCompetencyIds[], TargetKey, Language, Status (generating/ready/needsReview/approved/sent/failed, T3), ScriptVersion, PromptVersion, Hint, Verdict, Script, KeyWordsKn[], GlossKn, GlossEn, Activity (json: code numbers + A3 words), CheckItem (json: question, options, correctIndex, prompt), AudioUrl, AudioSegments, ApprovedAt, ApprovedByTeacherId, ApprovedScriptVersion, SentAt (built T-09–T-14; see `docs/database.md`) |
| `AudioClip` | Id, Kind (script/disclosure/greeting), Language, PackageId? (cascade), TextHash? (disclosure cache key), ContentType (audio/ogg), Bytes, Seconds, CreatedAt (built T-11) |
| `CatchUpAssignment` | Id, ClassId, ChildId, PackageId, WeekOf, MissedCompetencyIds[], CompressionRationale (json), Token, ScheduledAt, SentAt, OpenedAt, ExpiresAt (built T-09; token, sent and expiry set by the send step, T-14) |
| `CheckResponse` | AssignmentId, ChoiceId, Correct, Outcome (CaughtUp/NeedsBuddy). Spoken-answer fields dropped with FR-18 (D-26) |
| `BuddyCard` | ChildId, Date, HomeworkReturnedAt? (D-22), DoneAt?. Only the teacher's ticks are stored; the buddy, sums (code, guardrail 4), key words and page are computed from attendance, lessons and the curriculum each time (T-16) |
| `MessageTemplate` | Id, ClassId, Language, Text with slots `{child}` (written text only) `{lesson}` `{page}` (D-48, D-29, D-50), Status (Draft/Approved), ApprovedAt, ApprovedByTeacherId (FR-20, D-21) |
| `EveningMessage` | Id, ChildId, Date, TemplateId, LessonId, SlotValues (json), Token, SentAt (FR-20) |
| `AbsenceReason` | ChildId, ClassId, Date (the evening message's day), Category (unwell/travelOrWork/festival/neededAtHome/other); unique (ChildId, Date). Category only; no transcript or audio kept (FR-21). Source/Confidence dropped with the voice note (D-63) |
| `PacingDecision` | ClassId, WeekOf, LessonId, Decision (recap/skip). The nudge itself (lesson, n, total, minutes, next lesson) is computed from the ledger each time, not stored (T-46) |
| `AiCall` | Id, Purpose, Model, PromptVersion, InputTokens, OutputTokens, LatencyMs, Verdict, Error |

The ledger is a query (attendance × lessons × assignments × responses), not a stored table.

### 4.4 API (first cut)

```
POST /api/auth/pin                          { phone, pin } → session cookie; seeded teachers only, no registration (D-40); a class can have several teachers (D-79); 5 wrong → 15 min lock (429)
POST /api/auth/logout                       ends the session
GET  /api/me                                { teacherId, displayName, classId, setupComplete, phoneLast4, grade, medium }; 401 when signed out. phoneLast4 is the last 4 digits only, for T11 Profile (T-109): the full number never leaves the server
POST /api/me/welcome                        { clientRequestId } → sets Teacher.ConsentAt once; for continuing without a greeting (D-34)
POST /api/me/greeting                       multipart: file (16 kHz mono WAV, 1–12 s, ≤ 1 MB), clientRequestId, consent=true → stores her greeting (replaces the old one), sets Teacher.ConsentAt once, returns /api/me (T-61). 400 without consent or outside the length; 413; 415 not a WAV. The audio is never sent to speech-to-text or an LLM
GET  /api/p/{token}/greeting                the evening message's greeting audio for a valid, unstopped link (Ogg Opus, range requests); 404 otherwise (T-61)
POST /api/demo/login                        one-tap sign-in as the seeded demo teacher, same session cookie as the PIN (T-18; only while Demo:Enabled, like every /api/demo route)
GET  /api/demo                              { classId, parentChildId, lastResetAt } for the demo teacher (the parent phone shows Rohan's Hindi-speaking family; "Under the hood" starts at anyone's last reset); 404 otherwise
GET  /api/classes/{id}/attendance/{date}    { status: sats|manual|waiting|missing|noSchool, receivedAt, corrected, satsDiffers, syncAt, retryAt, absentChildIds[], children[] } (fetches the simulated SATS feed when due, D-45, D-69)
POST /api/classes/{id}/attendance           { date, absentChildIds[] }  (idempotent by clientId; correction or manual marking, never written back to SATS)
POST /api/demo/sats-feed                    { classId, date, mode: waiting|arrive } (only while Demo:Enabled, D-69)
GET  /api/curriculum                        chapters (titles) with their lessons and textbook pages, for the lesson picker
GET  /api/classes/{id}/lessons/{date}       { lesson: { competencyIds, source, status: processing|mapped|low|unclear|confirmed, summaryKn, summaryEn, examplesUsed } | null, nextInBook } (one lesson record per day, D-55; no confidence number)
PUT  /api/classes/{id}/lessons/{date}       { clientRequestId, competencyIds[1..3], source?: voice|picked } → confirmed; "Change" overwrites (FR-03, FR-04)
POST /api/classes/{id}/lessons/{date}/audio multipart { file ≤ 1 MB, clientRequestId } → 202 at once; STT → name scrubber → A1 in the background; 409 once confirmed (D-55); audio not stored
GET  /api/classes/{id}/ledger                { weekOf, lessons (this week), children[{ …, missedLessons[{ status: missed|sent|caughtUp|needsBuddy }], homeLanguage, channel: stopped|noPhone|noConsent|null, daysAwayThisWeek, history[{ date, kind: away|reason|catchUpSent|answered|stopped, … }] }] } (T-13; this week = Monday to today, IST)
DELETE /api/classes/{id}/children/{childId}?clientRequestId=  all of a child's data (FR-15, T-13): the child and, by cascade, attendance, evening messages, catch-up links + answers, reasons; a catch-up made only for that child is removed with its audio (T-19); 204 (replay too), 404 unknown/other class
POST /api/classes/{id}/catchups/generate    { clientRequestId } → 202: A2 per absent child → one package per (targets, language) with code numbers → A3 in the background (generating → ready | failed); re-run keeps existing ones (T-09)
GET  /api/classes/{id}/packages?week=       the week's packages with children, A2 reasons and the check's correct option (teacher only); `audio.segments` in playing order with seconds, total seconds and bytes, disclosure text (T-11)
GET  /api/audio/{id}                        one audio segment (Ogg Opus), range requests; her class's catch-up audio or a shared disclosure clip (teacher only; parents use /api/p/{token}/audio/{clipId})
GET  /api/packages/{id}                     one catch-up for T3's detail (teacher's own class)
POST /api/packages/{id}/approve             { clientRequestId, scriptVersion } → only when ready and scriptVersion is current; 409 + latest otherwise (T-14)
POST /api/packages/{id}/regenerate          { clientRequestId, hint: simpler|shorter|null } → 202, generating, comes back as scriptVersion + 1 (voids any tick); 409 once approved/sent or while generating (T-14)
POST /api/packages/{id}/send                { clientRequestId } → 409 unless approved at the current version (guardrail 3) and, outside demo mode, before 18:00 IST; links only for consent Yes + not stopped; the 6 pm job sends approved catch-ups itself (T-14). Bulk approve and Withdraw: roadmap (D-70)
POST /api/assignments/{id}/send             superseded by POST /api/packages/{id}/send (T-14)
GET  /api/p/{token}                         parent payload (no auth; 128-bit token; expires 14 days → 410). kind "evening" (T-44): { date, language, childName, teacherName, text, greetingAudioUrl, sentAt, stopped }. kind "catchUp" (T-15): { language, childName, teacherName, stopped, answered, expiresAt, homework, audio.segments (token URLs), transcript, activity, check.options[{ id, number }] } — never the correct index; 404 unless approved/sent at the approved version; lesson fields null when stopped; 410 body { language, childName, teacherName, homework }
GET  /api/p/{token}/audio/{clipId}          one segment of this link's catch-up (range requests); 404 for any other clip, or once expired/stopped
POST /api/p/{token}/answer                  { choiceId, clientRequestId } → 200 { answered: true }, never right/wrong; scored by code (FR-11); first answer counts (D-62); 409 when stopped, expired or not approved
POST /api/p/{token}/stop                    { clientRequestId } → sets Child.ParentStoppedAt (D-19); catch-up or evening link, also after expiry; idempotent
POST /api/p/{token}/restart                 { clientRequestId } → clears it (D-30); idempotent
POST /api/p/{token}/reason                  { category, clientRequestId } (FR-21, T-45): evening links only (404 for a catch-up link); unwell|travelOrWork|festival|neededAtHome|other, else 400; one per child per day, a later tap replaces it, a replayed clientRequestId changes nothing; 409 once expired or stopped; Skip sends nothing. The voice note → A7 route is cut (D-63)
GET  /api/classes/{id}/message-template     evening template per language, filled with an example (text parts, slots marked); drafts created from data/seed/evening-message.json (FR-20)
POST /api/classes/{id}/message-template/approve { clientRequestId, version } → all languages Approved; 409 + latest if the version is stale (D-49)
GET  /api/classes/{id}/lesson-audio/{date}   T-62: { available, enabled, sent, items[{ language, status: generating|ready|approved|failed, children, clipId, segments[disclosure, script], seconds, transcript, approvedAt }] }
PUT  /api/classes/{id}/lesson-audio/{date}   { clientRequestId, enabled } ticks (a row per language of tonight's families, clips made in the background, 202) or unticks; 409 unless lesson confirmed + template approved + a family to message, and 409 once the messages are sent
POST /api/classes/{id}/lesson-audio/{date}/{language}/approve  { clientRequestId, clipId } approves exactly the clip she listened to; 409 unless it is ready and that clip is current (or the messages are sent)
POST /api/classes/{id}/lesson-audio/{date}/{language}/regenerate  { clientRequestId } a new clip (back to generating, approval cleared); 409 once sent
GET  /api/p/{token}/lesson-audio/{clipId}    the evening message's AI disclosure or approved lesson clip (Ogg Opus, range requests); only that message's two clips, 404 when stopped, expired or not approved (T-62). `GET /api/p/{token}` carries `lessonAudio { segments, totalSeconds, disclosureText, transcript }`
GET  /api/classes/{id}/evening-messages/{date} tonight's card: { blocked, templateApproved, lessonConfirmed, absent, families, sent, sendsAt, children[{ childId, outcome }] }
POST /api/classes/{id}/evening-messages/send   { clientRequestId } today's absentees; 409 before 18:00 IST (unless Demo:Enabled), unless template Approved + lesson confirmed + attendance in; per child: willSend|sent|noConsent|noPhone|stopped|noWording; one per child per day. The 6 pm job (EveningSendWorker) calls the same path
GET  /api/classes/{id}/buddy-cards/{date}    T5 (T-16): a card per child back that day { childId, firstName, rollNo, daysAway, buddy{ childId, firstName, presentDays }|null (D-59 teacher-led), missedDays, competencyId, sums[{ question, answer }] (code), keyWordsKn, homework, homeworkReturned, done }; never the channel or a score (B-20); future date → 400
PUT  /api/classes/{id}/buddy-cards/{date}/{childId}  { clientRequestId, homeworkReturned?, done? } "Brought homework" (D-22) / "Buddy time done" (false = Undo); replay is a no-op; 404 when the child didn't come back that day (replaces POST /api/buddy-cards/{id}/homework-returned: cards are computed, so there is no ID before a tap)
GET  /api/classes/{id}/week/{date}          T6 (T-46, FR-22/21/13): the school week (Mon–Sat) containing date { from, to, empty, thisWeek / lastWeek { oneDay, twoDays, threeDays, fourPlusDays, backWithinTwoDays, total }, reasons { unwell, travelOrWork, festival, neededAtHome, other, notGiven }, nudge { lessonId, competencyIds, n, total, minutes, nextCompetencyId } | null, decision, stars { finished, schoolDays, children[{ childId, rollNo, firstName, attendance, catchUps, total }] } | null (FR-24: earned this week, or on track; null with no register) }; a spell belongs to the week it started in; future week → 400 (replaces return-speed?week=)
PUT  /api/classes/{id}/week/{date}/pacing   { clientRequestId, decision: recap|skip, lessonId } Change = PUT again; 409 when no nudge or another lesson
GET  /api/classes/{id}/pacing?week=
DELETE /api/children/{id}                   data deletion
POST /api/demo/reset                        { clientRequestId } → { resetAt, preMade }: deletes the demo class's data, re-adds the 36 seeded children, writes the 5 school days before today (SATS registers + confirmed lessons; only Ravi (Telugu) away this week and Manjunath (Kannada) the last 2 days; the feed's scenario keeps Ravi and Rohan (Hindi) away today; random absences off, `Attendance:Sats:AbsenceRate` 0; so the week makes exactly 3 catch-ups) and, when data/seed/demo-catchup.json exists, that catch-up as Ready for the story child of its language. Evening template, teacher sign-in and AiCall rows untouched; replay is a no-op (T-18)
POST /api/demo/send-now                     { clientRequestId } → { eveningBlocked, eveningSent, catchUpsSent }: what the 6 pm job does, now, for the demo class: tonight's evening message (approved template) and every approved, unsent catch-up of the week. Same filters as the job (guardrail 3, consent, stopped); each family gets a message once, so a repeat sends nothing new (T-18)
GET  /api/demo/family/{childId}             the simulated parent phone: [{ kind: evening|catchUp, date, sentAt, token }] newest first, her class only
GET  /api/demo/ai-calls?since=              "Under the hood": up to 50 AiCall rows { purpose, model, promptVersion, tokens, latencyMs, verdict, error }
GET  /api/demo/snapshot/{packageId}         Development only: a package + audio as JSON, saved as data/seed/demo-catchup.json
GET  /health
```

All write endpoints accept a client-generated `clientRequestId` so offline replays are idempotent.

---

## 5. AI design

### 5.1 Where Claude is called

| # | Call | Input | Output (JSON via structured outputs; Sonnet/Opus 5.5 reject forced tool use) | Model (config) | Guardrails |
|---|---|---|---|---|---|
| A1 | **Lesson mapping** | STT transcript (or board photo, stretch) + the current chapter's competency list and textbook excerpt (retrieved deterministically by the class's syllabus week) | `competencyIds[1..3]`, `summaryKn`, `summaryEn` (same call, for the English view, D-56), `examplesUsed`, `confidence` | Sonnet 5.5 | IDs constrained to an enum of valid IDs; confidence < 0.7 → teacher picks from list |
| A2 | **Prerequisite-aware compression** | Child's missed competency IDs, graph-computed candidate set (missed ∩ ancestors of next week's competencies), next week's plan, child's prior check results, lesson records | `targets[≤2]` with reason; `dropped[]` with reason; each reason in English (`reason`) and Kannada (`reasonKn`, prompt v2, D-76) | Sonnet 5.5 | Targets must be ⊆ candidate set; else fall back to the graph's nearest-prerequisite rule (the head of the candidate set; with no candidates, the latest missed lesson; no AI reasons shown). A `reasonKn` under 80% Kannada letters is dropped (T3 then shows the English reason); it never fails the targets. Next week = the 5 lessons after the latest confirmed one in book order until a syllabus file exists (T-08). Week = Monday up to the day |
| A3 | **Catch-up script writer** | Targets, teacher's lesson records (her examples), language, content rules (§3.3), allowed objects, **the activity and check numbers made by code** (T-09), the lesson's textbook Kannada words, hint (simpler/shorter) | `script`, `keyWordsKn[3]` (from the lesson's words), `activity{instruction, objectOption}`, `checkPrompt`, `glossKn`, `glossEn`. Options, correct index and pictures (`sticks:{n}`, drawn from the number) are code's; buddy content is T-16's code | Opus 5.5 (low volume, cached per package) | Verifier A4; one auto-retry with verifier feedback; then teacher sees "needs review" |
| A4 | **Verifier** | A3 output | pass/fail + reasons | Code checks + Haiku 5.5 | See §5.4 |
| A8 | **Lesson recap writer** (T-62, FR-23) | Today's 1–3 lessons from the curriculum only: names in the family's language, textbook examples, key words, page; language; the validator's reasons on the retry. No names, no teacher examples (the clip is shared across classes) | `script` (≤ 80 words, plain spoken sentences) | Opus 5.5 via the Script model key, once per (lesson, language) | Schema + code checks (no names or braces, ≥ 80% of letters in the family's script, only numbers from the lessons' examples and page, audio ≤ 95 s with the disclosure); one retry; then "could not make this clip" and the plain message. She approves every clip |
| A5 | Pacing wording | Stats computed by code | one Kannada sentence with slots `{n}`, `{total}`, `{minutes}` | Haiku 5.5 | Numbers filled by code; LLM cannot change them |
| A6 | ~~Spoken answer rubric~~ Dropped (D-26) | — | — | — | — |
| A7 | Reason classification (FR-21) | STT transcript of the parent's optional voice note (no names) | `category` (one of the 5), `confidence` | Haiku 5.5 | Category constrained to an enum; confidence below threshold → "other"; only the category is stored, transcript and audio discarded; never asks about symptoms |

Not done by the LLM, on purpose: attendance, ledger, candidate-set computation, picture-tap scoring, arithmetic, buddy selection, scheduling, evening-message slots (FR-20), return-speed numbers (FR-22). Rules are cheaper, faster and verifiable where they suffice; Claude is used for language, judgement and grounding.

Model IDs live in `appsettings.json` (`Ai:Models:*`) so they can be swapped without code changes. Use prompt caching on the curriculum system prompt.

### 5.2 Prompts

- One file per call in `prompts/` (`a1-lesson-mapping.md`, …) with a version header; the version is logged with every call.
- Prompts reference IDs and placeholders only (`{child}`, `{teacher}`); never real names (NFR-05).

### 5.3 Grounding data

- `data/curriculum/grade3-maths.json`: competencies, prerequisites, NIPUN Bharat outcome codes, chapter, sequence, Kannada and English names, 1–2 textbook example problems each. **Source: Karnataka state Grade 2–3 maths textbooks (Kannada medium) and NIPUN Bharat learning outcomes.** D-14 — Monika curated names/wording, Rahul built the JSON (D-14 closed 24 Oct).
- `data/pictures/`: picture library manifest (IDs, alt text in 4 languages).

### 5.4 Verifier checks (A4)

Code checks (deterministic, run first):
1. Every number expression in the check item and buddy sums is recomputed; the correct option is the only correct one.
2. Script is in the target script (Unicode block) apart from the 3 Kannada key words.
3. Spoken duration estimate ≤ 90 s (word count × measured rate); after TTS, actual audio duration ≤ 95 s.
4. Key words appear and are from the lesson's competency glossary.
5. Picture IDs exist; activity objects are on the allowed list.
6. No placeholders missing or real names present.

LLM check (Haiku): script teaches the target competency; age-appropriate; no shaming; gloss matches script. Output pass/fail + reason.

*Built (T-10, `Features/CatchUps/Verify/`):* check 1 recomputes the code-made question and every "a ± b = c" written in the script, gloss or instruction, and requires the activity's numbers in the script and the check's lesson among the targets; check 3 estimates seconds as words ÷ `Verifier:WordsPerSecond` (kn/te 1.8, ta 1.6; tuned with real audio in T-11) against `Verifier:MaxScriptSeconds` (90); check 4 also requires each key word twice (§3.3); check 5 reads A3's structured `objects` list (prompt v2), since objects named in Telugu/Tamil text can't be checked by code, and pictures are drawn from numbers so their IDs always exist. Haiku runs only when all code checks pass (prompt `a4-check` v1). One retry with the failed checks' details; outcome: `ready` · `needsReview` (words kept, reasons stored on `CatchUpPackages.Review`) · `failed`. Haiku unavailable = not passed (to review, never to ready).

### 5.5 Evaluation

`evals/` holds 25+ cases per call (A1, A2, A3) with expected outputs or rubrics; run with `dotnet test --filter Category=Eval` against the live API (costs credits; run on demand and nightly, not on every PR). Results are written to `evals/results/` and summarised in the submission deck.

---

## 6. Offline and low-connectivity behaviour

| Function | Offline behaviour |
|---|---|
| App shell, class list, ledger snapshot, buddy cards | Cached (service worker + IndexedDB); readable offline |
| Attendance, voice notes | Queued in IndexedDB outbox; auto-sync on reconnect; visible "3 waiting to send" |
| Lesson mapping, script generation, approval | Need connection; approval taps queue and replay |
| Parent page | Needs a connection once to load; audio ≤ 300 KB; works on 2G/3G |
| Children without a phone at home | Buddy card in class: no connection needed |

---

## 7. Privacy, safety and consent

- Parent opt-in before any message; recorded with timestamp. Parents can stop messages at any time from the parent page (FR-19).
- Every parent audio says out loud that the voice after the greeting is a computer voice (FR-09), because written labels fail for parents with limited literacy.
- Teacher greeting recorded with explicit consent; deletable.
- Store first name, roll number, home language, parent number. No surname, address, caste, photos.
- Pseudonymised IDs to LLM/STT/TTS providers. Claude API data is not used for training by default.
- Parent links are unguessable tokens, expire after 14 days, show first name only.
- Delete endpoint removes child data and audio.
- Label: "AI-assisted, approved by your teacher" on every generated item.
- Ledger is visible only to the teacher. No export to officers in the MVP (N8).
- Reason for absence (FR-21): optional, stored as a category only, visible only to the teacher. "Unwell" is a category, not a diagnosis; never ask for symptoms.

---

## 8. Decision log

The decisions moved out of this file on 15 Nov 2026 (T-110), so that a new decision is no longer a new row at the end of one shared table.

- **D-01 to D-99:** [`decisions/D-01-to-D-99.md`](decisions/D-01-to-D-99.md), the table that was here, as it was. Frozen: no new rows.
- **D-100 onwards:** one file per decision in [`decisions/`](decisions/README.md).
- **Open gates:** a decision with status **Proposed**. Today that is D-98 in the frozen log.

Status: **Proposed** → **Accepted** / **Changed**. *Gate* = must be resolved before the task it names is built.


## 9. Assumptions

- Calendar: build days Mon 19 Oct – Wed 28 Oct; buffer 29–30 Oct; submit Sat 31 Oct. Weekday capacity ~4–5 h each, weekend ~8 h each.
- Schools are on Dasara break until about 21 Oct (reported); teacher sessions before then are remote.
- Sarvam STT is good enough on adult Kannada speech; we do not rely on child speech recognition.
- Claude credits (USD 500) cover development and evals; we log tokens to verify.
- Judges test on a desktop browser; the demo shell must make the phone views obvious.

## 10. Risks

Full register with fallbacks in `docs/plan.md` §6.

## 11. Unresolved questions

1. Exact Grade 3 textbook chapter order for 2026–27 after the Nali-Kali phase-out (Monika to confirm from the textbook PDF). *13 Nov: the 2026-27 Grade 3 maths book is still Nalikali; chapter order in D-90.*
2. Sarvam voice quality for Telugu child-directed speech — check on Day 3 with a test teacher.
3. Whether a test teacher will let us record a real greeting for the demo video (consent form needed).
