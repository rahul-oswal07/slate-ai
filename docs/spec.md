# Spec: Tappida Paatha MVP

| | |
|---|---|
| Stage | 2 · Design |
| Reads | `docs/intent.md`, `docs/hackathon-requirements.md` |
| Product owner (accepts) | Monika I — UX, content, scope |
| Technical owner (accepts) | Rahul Oswal — architecture, AI, data |
| Status | **Draft — awaiting acceptance.** Build starts when every `[DECIDE]`/`[CONFIRM]` item marked *Gate* in §8 is resolved |

Tags used in this file:
- **[DECIDE: Monika]** product or design call that needs Monika's approval
- **[CONFIRM: Rahul]** technical call that needs Rahul's confirmation
- Each tag has a row in the decision log (§8). Resolve it there, then update the text.

---

## 1. Scope

### 1.1 MVP (must ship by 31 Oct)

One class, one subject, one complete loop:

- **Class:** Grade 3, Kannada-medium, 36 seeded children; 6 children with Telugu home language, 3 with Tamil, the rest Kannada.
- **Subject:** maths. One textbook unit chain, as the Grade 3 book teaches it: place value (hundreds, tens, ones) → 3-digit addition without carrying → **addition with carrying** → subtraction without borrowing → **subtraction with borrowing**, sums ≤ 999. 2-digit addition and subtraction are Grade 2 prerequisites. 16 competencies in the dependency map (`docs/design/curriculum.md`). D-01 (Accepted, revised 20 Oct)
- **Languages:** development and the default UI locale are **English** (D-32); Kannada for the teacher UI and Telugu/Kannada/Tamil for parents are added through i18next from the design cards' copy tables and chosen per user. Catch-up audio in **Telugu** and **Kannada**, fully tested. Tamil works through the same pipeline but is only spot-checked. D-02

### 1.2 Stretch (only after the Day 7 code freeze for MVP features is met)

- Real WhatsApp delivery via WhatsApp Cloud API test number (max 5 recipient numbers). D-05

### 1.3 Not in scope

Voice cloning, avatar video, officer/CRP dashboards, a real SATS/VSK attendance integration (the MVP uses a simulated feed, D-27), other subjects, native app, multi-school admin. Dropped to make room for the mentor changes (D-26): board-photo capture (FR-17, T-41), spoken child answer (FR-18, T-42; replaced by the parent's optional spoken reason in FR-21), full Tamil review (T-43; Tamil stays spot-checked). Home visits by the cook or community members: roadmap only (D-25).

---

## 2. Functional requirements

Priority: **M** = must (MVP), **S** = should (stretch). IDs are referenced by `docs/tasks.md` and tests.

| ID | P | Requirement | Acceptance |
|---|---|---|---|
| FR-01 | M | Today's attendance arrives from the school's attendance system (a **simulated SATS feed** in the MVP, labelled as simulated; D-27). The teacher only corrects mistakes. A server job syncs it at **12:00** and retries at **2:00 pm** (D-45); she can mark absentees in the app at any time, one tap per absent child (all present by default). If nothing arrives, nobody is marked absent automatically. When nobody is absent, Today says "Everyone is here today." and "No messages tonight." (D-54) | Absent list shows its source and time; corrections and fallback marking work offline; corrections are never written back to SATS; fallback: 4 absentees marked in ≤ 10 s |
| FR-02 | M | Teacher records a voice note (≤ 30 s, Kannada) saying what she taught: tap to start, tap to stop (holding also works); elapsed time against the limit; auto-stop at 30 s; works with keyboard and screen reader (D-31) | Recording queued offline; uploaded on reconnect |
| FR-03 | M | System transcribes the note and Claude maps it to 1–3 competency IDs from the curriculum map, with a one-line Kannada summary and the examples she used | Teacher sees every matched lesson ticked, unticks any that are wrong, and confirms with one tap ("Confirm 2 lessons"); "Add another lesson" and "Change" use the chapter list (D-33) |
| FR-04 | M | Fallback when audio or AI fails: teacher picks the lesson from the chapter list | One tap; ledger still updates |
| FR-05 | M | Ledger: for each child, the lessons missed and current status (missed · sent · caught up · needs buddy · stopped by family) | Matches attendance × lessons exactly (deterministic) |
| FR-06 | M | Compression: for a child with several missed lessons, Claude selects at most 2 target competencies that block next week's lessons, with a reason for each kept and dropped item | Targets are always a subset of the graph-computed candidate set (validator) |
| FR-07 | M | Script writer: per (target set, home language) Claude writes a ≤ 90 s catch-up script with 3 Kannada key words, a 5-minute homework task that points to the child's own textbook/workbook page (with a household-object option) and ends with "Show `{teacher}` when you come back" (D-22), a 3-picture learning check, a Kannada gloss for the teacher, and buddy-card content | Passes the verifier (§5.4) |
| FR-08 | M | Teacher approval: weekly list of draft packages; play audio, read Kannada gloss, Approve or Regenerate ("simpler", "shorter"); bulk approve only for packages she has opened and ticked; Withdraw until the 6 pm send job starts, labelled "Withdraw (until 6 pm)"; after that the status is Sent (D-28, D-37) | No package reaches a parent without approval (enforced in API) |
| FR-09 | M | Audio: text-to-speech in the home language, in this order: the teacher's own recorded greeting **in the family's language** where she recorded one (short line shown in Kannada script), otherwise her Kannada greeting followed by a computer-voice introduction in the family's language (D-52) → a ~5 s **spoken AI disclosure** in the home language ("This is a lesson from your child's teacher. The rest is read by a computer voice.") → the catch-up script. **The computer voice never says names**: "your child", "your teacher"; names appear only in written text and in her own greeting (D-50). The written transcript starts with the same disclosure. *(Disclosure proposed in D-18.)* | Audio ≤ 300 KB, ≤ 95 s including greeting and disclosure; disclosure segment present in every package; plays on a low-end Android browser |
| FR-10 | M | Parent page from a link: big Listen button, activity picture, then 3-picture answer; audio prompts so a low-literacy parent can follow | Lighthouse mobile performance ≥ 85; total transfer ≤ 500 KB excluding audio |
| FR-11 | M | Picture-tap answer is scored by code (no LLM) and updates the ledger: correct → caught up; wrong → needs buddy | Ledger updates within 2 s of the tap |
| FR-12 | M | Return-day buddy card for every returning child, whether or not the family has a phone: which classmate, 3 sums, key words, 10 minutes; the teacher taps **Brought homework** (D-22) | Buddy is a child who was present for those lessons; card is cached offline and printable |
| FR-13 | M | Weekly pacing nudge, e.g. "11 of 36 missed carrying-over. Recap 10 minutes before moving on." Shown when 5 or more of the class missed the same lesson this week; otherwise "No nudge this week" (D-60) | Numbers come from the ledger by code; Claude writes only the wording |
| FR-14 | M | Demo mode: split screen with teacher phone and parent phone; "Reset demo" reseeds the class | A judge completes the loop in ≤ 5 minutes without instructions beyond the landing page |
| FR-15 | M | Consent and deletion: parent opt-in comes from the paper consent form and is recorded by the teacher per child (Yes / No / No phone, with date) on T8 Family details, together with the parent's phone number (D-43) and the child's home language (D-47) (D-39); nothing is sent unless Yes; teacher can delete a child's data | Send endpoints refuse children not marked Yes (409, integration test); delete removes child, responses and audio; verified by integration test |
| FR-16 | S | Send via WhatsApp Cloud API (template message with link), scheduled for after 6 pm | Delivered to a registered test number |
| FR-17 | — | ~~Board photo capture via Claude vision~~ Dropped (D-26) | Same output as FR-03 |
| FR-18 | — | ~~Spoken answer~~ Dropped (D-26); replaced by the optional spoken reason in FR-21. Was: STT + Claude rubric; confidence < 0.8 goes to a teacher review queue with the audio | Never auto-marks below threshold |
| FR-19 | M | **Stop these messages** *(D-19, D-30)*: a stop link on every parent screen and in the evening message, plus a STOP reply; one tap plus one confirmation stops all future messages for that child; one confirmation message with a restart link; reversible. The ledger shows "Stopped by family" and the child stays on the buddy-card path | After a stop, the API refuses to send to that child (409), covered by an integration test; the ledger shows the status within 2 s |
| FR-20 | M | **Evening message on each day of absence** (D-21): the teacher's recorded greeting plus a fixed message the teacher approved once at setup, with slots filled by code: today's lesson (from her confirmed lesson record), the textbook page for homework, and "a classmate is keeping `{child}`'s seat" (the buddy is named only on the teacher's card, D-29). No AI-written text, so no daily approval. No "tomorrow" slot (D-48). Audio is stitched from pre-made pieces (greeting in the family's language or fallback, fixed phrases, lesson-name and page clips); the child's name is not spoken (D-50). The template is reviewed and changed from Settings, and a change needs approval again (D-49). Several missed days still compress into one catch-up (FR-06–FR-08) | Sent after 6 pm only to opted-in, not-stopped families; slots filled by code; template approval recorded once; teacher time stays within NFR-01. Guardrail 3 (confirmed by Rahul, 20 Oct): the send endpoint returns 409 unless the class's `MessageTemplate` is Approved, the lesson record is teacher-confirmed, and the family has opted in and not stopped; no LLM is called on this path; audio is the greeting plus pre-synthesised template and competency-name segments, covered by an endpoint test |
| FR-21 | M | **Reason for absence** (D-23), optional, on the parent page: "We missed `{child}` today. Is everything OK?" with 5 picture buttons (unwell · family travel or work · festival or function · needed at home · other) (picture buttons only in the MVP; the voice note and its A7 classification are cut, D-63) | Category only is stored; shown only to the teacher; never asks for symptoms; skipping it never blocks the rest of the page |
| FR-22 | M | **Return-speed card** (D-24): weekly card for the teacher with absences by length (1, 2, 3, 4+ days) this week vs last, the share back within 2 school days, and reason categories | Numbers computed by code from attendance; teacher's own view; nothing exported to officers |

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

Teacher app (PWA, Kannada first):

| Screen | Purpose | Key interaction | States to design (besides default) |
|---|---|---|---|
| T0 Onboarding | Consent, record 10-second greeting once ("Ravi, this is Lakshmi teacher…") | Record → play back → save | Recording · recorded (play / re-record / delete) · consent given · missing step · greeting deleted · mic permission denied (continue without greeting) · offline (greeting saved, uploads later) |
| T1 **Today** | Attendance (from SATS, D-27) + lesson capture + tonight's message + "Since yesterday" updates on one screen | Check the absent list, correct only if wrong; tap the mic (tap again to stop); sync badge | Attendance not in yet (mark here instead) · corrected by teacher · lesson already recorded today · offline ("3 waiting to send") · sending · mic permission denied |
| T2 Lesson confirm | Shows the 1–3 mapped lessons as a ticked checklist (D-33) | Untick wrong ones, one tap "Confirm N lessons"; "Add another lesson"; or pick from chapter list | Processing · slow (> 20 s, pick from list) · confident (checklist) · low confidence ("Looks like one of these. Please choose.") · changed by teacher · could not hear the note (pick from list) · offline (note saved, matched later) |
| T3 **Approve** | Draft catch-up packages for the week | Play, read gloss, tick, Approve / Regenerate; approve the checked ones; Withdraw | Generating · ready · needs review (verifier failed) · approved · sent · generation failed · offline (approval queued) |
| T4 **Missed lessons** (ledger) | Per-child missed lessons with learning status and separate channel status; by-lesson view | By child / by lesson; filter: absent this week; tap child for history; delete child data; link to T8 Families | Empty (first week) · filtered · by lesson · offline ("Showing the saved list …") · delete confirm |
| T5 **Return day** | Buddy cards for children back today | Previous / Next buttons ("Card 1 of 2"); print/share; "Brought homework" | None due today · marked done · offline (cached cards) |
| T6 Pacing | Weekly nudge | Read; "Plan recap" adds a reminder | No nudge this week ("Most children are on track") · decided |
| T7 Review queue | Dropped with FR-18 (D-26) | — | — |
| T8 Families and consent | Record the paper consent per child (D-39) | Yes / No / No phone per child; summary counts; reached from T0 "All set" and Missed lessons | Default · a family marked No · stopped by family (shown on the row) |

Parent page (no install, opens from a link, Telugu/Tamil/Kannada):

| Screen | Content | States to design (besides default) |
|---|---|---|
| P1 Listen | Teacher photo initial, big ▶ Listen (greeting + catch-up), "AI-assisted, approved by Lakshmi teacher" | Playing · paused · played · network dropped mid-play (resume) · link expired |
| P2 Activity | One picture of the household-object activity, audio instruction replays | Audio replaying |
| P3 Answer | "Ravi, tap the answer" read aloud; 3 large pictures | Picture chosen (numeral shown; tap another picture to change) · sending · could not send (answer saved, try again) |
| P4 Done | Positive sound, "Lakshmi teacher will see this" | none |

Demo shell (`/demo`): teacher phone and parent phone side by side, a step guide for judges, and Reset.

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
| Audio storage | Azure Blob Storage (local disk in dev) | Opus/MP3 at 24–32 kbps |
| Speech | Sarvam AI: Saaras STT (kn-IN; `saaras:v4`, configurable), Bulbul TTS (te-IN, ta-IN, kn-IN) | D-12 accepted: Sarvam; voice choice during T-11. Fallback: Google Cloud STT/TTS |
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
| `CatchUpPackage` | Id, ClassId, TargetCompetencyIds[], Language, Script, GlossKn, Activity, CheckItem (json), BuddyContent (json), AudioUrl, Status (Draft/Verified/Approved/Rejected), PromptVersion |
| `CatchUpAssignment` | Id, ChildId, PackageId, MissedLessonIds[], CompressionRationale (json), Token, ScheduledAt, SentAt, OpenedAt |
| `CheckResponse` | AssignmentId, ChoiceId, Correct, Outcome (CaughtUp/NeedsBuddy). Spoken-answer fields dropped with FR-18 (D-26) |
| `BuddyCard` | ChildId, BuddyChildId, Date, Sums[], KeyWords[], HomeworkReturnedAt? (D-22) |
| `MessageTemplate` | Id, ClassId, Language, Text with slots `{child}` (written text only) `{lesson}` `{page}` (D-48, D-29, D-50), Status (Draft/Approved), ApprovedAt, ApprovedByTeacherId (FR-20, D-21) |
| `EveningMessage` | Id, ChildId, Date, TemplateId, LessonId, SlotValues (json), Token, SentAt (FR-20) |
| `AbsenceReason` | ChildId, Date, Category (Unwell/TravelOrWork/Festival/NeededAtHome/Other), Source (Picture/Voice), Confidence?. Category only; no transcript or audio kept (FR-21) |
| `PacingNudge` | ClassId, WeekStart, Stats (json), TextKn |
| `AiCall` | Id, Purpose, Model, PromptVersion, InputTokens, OutputTokens, LatencyMs, Verdict, Error |

The ledger is a query (attendance × lessons × assignments × responses), not a stored table.

### 4.4 API (first cut)

```
POST /api/auth/pin                          { phone, pin } → session cookie; seeded teachers only, no registration (D-40); 5 wrong → 15 min lock (429)
POST /api/auth/logout                       ends the session
GET  /api/me                                { teacherId, displayName, classId, setupComplete }; 401 when signed out
POST /api/me/welcome                        { clientRequestId } → sets Teacher.ConsentAt once (first-run gate)
POST /api/demo/login                        one-tap demo sign-in (T-18)
GET  /api/classes/{id}/attendance/{date}    { status: sats|manual|waiting, receivedAt, correctedAt, recheckAt, absentChildIds[] } (fetches the simulated SATS feed when due, D-41)
POST /api/classes/{id}/attendance           { date, absentChildIds[] }  (idempotent by clientId; correction or manual marking, never written back to SATS)
POST /api/demo/sats-feed                    { classId, date, mode: waiting|arrive } (only while Demo:Enabled, D-41)
POST /api/lessons                           multipart audio|image or { pickedCompetencyId } → 202 { lessonId }
GET  /api/lessons/{id}                      status + mapping
PUT  /api/lessons/{id}/competencies         teacher correction
GET  /api/classes/{id}/ledger
POST /api/classes/{id}/catchups/generate    builds assignments + draft packages for the week
GET  /api/classes/{id}/packages?status=
POST /api/packages/{id}/approve | /regenerate { hint }
POST /api/assignments/{id}/send             (demo: immediate; prod: schedule 18:00 IST)
GET  /api/p/{token}                         parent payload (no auth; 128-bit token; expires 14 days)
POST /api/p/{token}/answer                  { choiceId }
POST /api/p/{token}/reason                  { category } or audio → A7 (FR-21)
PUT  /api/classes/{id}/message-template     draft evening template (FR-20) · POST …/message-template/approve
POST /api/classes/{id}/evening-messages/send   today's absentees; 409 unless template Approved + lesson confirmed
GET  /api/classes/{id}/buddy-cards?date=
POST /api/buddy-cards/{id}/homework-returned   "Brought homework" tap (D-22)
GET  /api/classes/{id}/return-speed?week=   FR-22
GET  /api/classes/{id}/pacing?week=
DELETE /api/children/{id}                   data deletion
POST /api/demo/reset · GET /health
```

All write endpoints accept a client-generated `clientRequestId` so offline replays are idempotent.

---

## 5. AI design

### 5.1 Where Claude is called

| # | Call | Input | Output (JSON via structured outputs; Sonnet/Opus 5.5 reject forced tool use) | Model (config) | Guardrails |
|---|---|---|---|---|---|
| A1 | **Lesson mapping** | STT transcript (or board photo, stretch) + the current chapter's competency list and textbook excerpt (retrieved deterministically by the class's syllabus week) | `competencyIds[1..3]`, `summaryKn`, `summaryEn` (same call, for the English view, D-56), `examplesUsed`, `confidence` | Sonnet 5.5 | IDs constrained to an enum of valid IDs; confidence < 0.7 → teacher picks from list |
| A2 | **Prerequisite-aware compression** | Child's missed competency IDs, graph-computed candidate set (missed ∩ ancestors of next week's competencies), next week's plan, child's prior check results, lesson records | `targets[≤2]` with reason; `dropped[]` with reason | Sonnet 5.5 | Targets must be ⊆ candidate set; else fall back to the graph's nearest-prerequisite rule |
| A3 | **Catch-up script writer** | Targets, teacher's lesson records (her examples), language, content rules (§3.3), allowed objects, picture library IDs | `script`, `keyWordsKn[3]`, `activity`, `checkItem{prompt, options[3]{pictureId}, correct}`, `glossKn`, `buddy{sums[3], keyWords}` | Opus 5.5 (low volume, cached per package) | Verifier A4; one auto-retry with verifier feedback; then teacher sees "needs review" |
| A4 | **Verifier** | A3 output | pass/fail + reasons | Code checks + Haiku 5.5 | See §5.4 |
| A5 | Pacing wording | Stats computed by code | one Kannada sentence with slots `{n}`, `{total}`, `{minutes}` | Haiku 5.5 | Numbers filled by code; LLM cannot change them |
| A6 | ~~Spoken answer rubric~~ Dropped (D-26) | — | — | — | — |
| A7 | Reason classification (FR-21) | STT transcript of the parent's optional voice note (no names) | `category` (one of the 5), `confidence` | Haiku 5.5 | Category constrained to an enum; confidence below threshold → "other"; only the category is stored, transcript and audio discarded; never asks about symptoms |

Not done by the LLM, on purpose: attendance, ledger, candidate-set computation, picture-tap scoring, arithmetic, buddy selection, scheduling, evening-message slots (FR-20), return-speed numbers (FR-22). Rules are cheaper, faster and verifiable where they suffice; Claude is used for language, judgement and grounding.

Model IDs live in `appsettings.json` (`Ai:Models:*`) so they can be swapped without code changes. Use prompt caching on the curriculum system prompt.

### 5.2 Prompts

- One file per call in `prompts/` (`a1-lesson-mapping.md`, …) with a version header; the version is logged with every call.
- Prompts reference IDs and placeholders only (`{child}`, `{teacher}`); never real names (NFR-05).

### 5.3 Grounding data

- `data/curriculum/grade3-maths.json`: competencies, prerequisites, NIPUN Bharat outcome codes, chapter, sequence, Kannada and English names, 1–2 textbook example problems each. **Source: Karnataka state Grade 2–3 maths textbooks (Kannada medium) and NIPUN Bharat learning outcomes. [DECIDE: Monika]** D-14 — Monika curates names/wording, Rahul builds the JSON.
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

Status: **Proposed** → **Accepted** / **Changed**. *Gate* = must be resolved before the affected build day.

| ID | Decision | Recommendation | Owner | Gate | Status |
|---|---|---|---|---|---|
| D-01 | Demo subject/unit | Grade 3 maths, place value → carrying → borrowing. Revised 20 Oct (T-22): 3-digit, as in the 2026–27 textbook (Part 1 Ch 2–4, Part 2 Ch 5); 2-digit skills become Grade 2 prerequisites | Monika | Day 1 | Accepted (T-00); revised (T-22) |
| D-02 | Languages fully tested | Kannada UI; Telugu + Kannada audio; Tamil spot-checked | Monika | Day 1 | Accepted (T-00) |
| D-03 | Attendance UI pattern | Roll number + first name grid | Monika | Day 2 | Accepted |
| D-04 | Design tokens and type | Monika's tokens; Noto Sans Indic families | Monika | Day 2 | Accepted (T-23): `src/web/src/styles/tokens.css`, `docs/design/tokens.md` |
| D-05 | Delivery channel in demo | Simulated parent phone primary; WhatsApp Cloud API test number stretch; `wa.me` share as fallback | Rahul | Day 1 | Accepted (19 Oct) |
| D-06 | Board photo capture | Stretch | Monika + Rahul | Day 7 | Proposed |
| D-07 | Picture library | Code-generated place-value SVGs + ~10 illustrations | Monika | Day 3 | Accepted (pictures are the answer) |
| D-08 | Parent page structure | Four single-action steps | Monika | Day 3 | Accepted |
| D-09 | Teacher review of non-Kannada script | Kannada gloss + audio | Monika | Day 3 | Accepted |
| D-10 | Single container (API serves SPA) | Yes | Rahul | Day 1 | Accepted (T-03) |
| D-11 | Claude client | Official `Anthropic` NuGet SDK behind `ILlmClient` | Rahul | Day 1 | Accepted (19 Oct) |
| D-12 | Speech provider | Sarvam (Saarika STT, Bulbul TTS) | Rahul | Day 1 | Accepted (19 Oct): API key in user-secrets; voice choice during T-11. 21 Oct: Sarvam's current STT model is `saaras:v4` (Saarika superseded), set via `Sarvam:SttModel` |
| D-13 | Hosting | Azure App Service + PostgreSQL Flexible, Central India | Rahul | Day 1 | Accepted (T-04): staging + prod apps on one B1 plan, images on GHCR, OIDC deploy |
| D-14 | Curriculum source and wording | State textbook + NIPUN outcomes; Monika approves Kannada names | Monika | Day 2 | Proposed |
| D-15 | Open-source licence | MIT, repo public from submission day | Rahul + Monika | Day 9 | Proposed |
| D-16 | Test participants | 3 teachers (phone/video during Dasara break), plus 2 parents (incl. Telugu; proxies as fallback) after schools reopen | Monika | Day 1 | Accepted (T-00) |
| D-17 | Contrast level and dark theme | All text ≥ 7:1 (AAA), UI ≥ 3:1; dark theme follows the phone setting (`prefers-color-scheme`), no in-app toggle yet | Monika | Day 2 | Accepted (T-23) |
| D-18 | Spoken AI disclosure in parent audio | Greeting → ~5 s disclosure in the home language → script; fixed text per language, synthesised once and cached; native review of the wording **[CONFIRM: Rahul]** | Monika + Rahul | Day 3 (before T-11) | Proposed |
| D-19 | Parent "Stop these messages" | One tap + confirm on the parent page; `ParentStoppedAt` on `Child`; send endpoint refuses stopped children; ledger status "Stopped by family" **[CONFIRM: Rahul]** | Monika + Rahul | Day 5 (before T-15) | Proposed |
| D-20 | Primary success measure | Return speed (days until back; share back within 2 school days), then home engagement (listened + homework brought back), then learning check. Never message opens | Monika | Day 2 | Accepted (mentor review) |
| D-21 | Message on each evening of absence | Template approved once by the teacher, slots filled by code; no AI text daily (FR-20). Guardrail 3 covers it: approved `MessageTemplate` + code-filled slots from confirmed records, send refused otherwise (T-44) | Monika + Rahul | Day 3 | Accepted (Monika; Rahul confirmed 20 Oct) |
| D-22 | Homework from school materials | Textbook/workbook page per competency + household option; "Show `{teacher}`"; "Brought homework" on the buddy card. Page numbers added in T-22 | Monika + Rahul | Day 2 | Accepted (mentor review) |
| D-23 | Reason for absence | Optional, caring wording, 5 picture buttons + optional voice note classified by Claude (A7); category only (FR-21) | Monika | Day 3 | Accepted (mentor review) |
| D-24 | Return-speed view | Weekly card, teacher only (FR-22) | Monika | Day 4 | Accepted (mentor review) |
| D-25 | Existing home-visit pathways (cook, community) | Roadmap only; no research or build in this stage | Monika | — | Accepted (mentor review) |
| D-26 | Make room for the mentor changes | Drop T-41 board photo, T-42 spoken child answer, T-43 full Tamil review; T-40 WhatsApp stays stretch | Monika | Day 2 | Accepted (mentor review) |
| D-27 | Where attendance comes from | Read from the school's system so the teacher enters nothing new. MVP: simulated SATS feed (labelled as simulated) plus manual fallback and corrections in the app; corrections never written back. Real SATS/VSK feed is roadmap: needs a data-sharing agreement with the state (no public API found, 20 Oct). Never scrape the SATS portal with a teacher's login | Monika | Day 2 | Accepted |
| D-28 | Bulk approve | Only for catch-ups she has opened and ticked; count uses the status shown; Withdraw until the 6 pm send (UX review B-01–B-03, B-13) | Monika | Day 3 | Accepted |
| D-29 | Buddy in the evening message | "A classmate is keeping `{child}`'s seat"; the buddy is named only on the teacher's buddy card (UX review C-30) | Monika | Day 3 | Accepted |
| D-30 | Where families can stop messages | Stop link on every parent screen and in the evening message; STOP reply honoured; confirmation message with restart link (UX review C-06, C-22) | Monika + Rahul | Day 3 | Accepted |
| D-31 | Mic gesture | Both mics: tap to start, tap to stop; holding also works; timer and auto-stop; keyboard and screen-reader operable (UX review A-01, A-06) | Monika | Day 3 | Accepted |
| D-32 | Default language during development | English is the default locale for development, demos and design references (prototype opens in English). Kannada/Telugu/Tamil strings come from the cards' copy tables via i18next and switch per user; D-02 language scope (Kannada UI, Telugu + Kannada audio tested, Tamil spot-checked) is unchanged | Monika | Day 3 | Accepted |
| D-33 | Note matching several lessons | Checklist of the 1–3 matched lessons, all ticked; untick wrong ones; "Confirm N lessons"; "Add another lesson" (FR-03) | Monika | Day 3 | Accepted |
| D-34 | Mic blocked and upload states | Mic blocked: explanation + "Pick from the list" + "Open settings"; T0 lets her continue without a greeting. "Sending your note…" on the lesson card; after 3 s it is saved on the phone and queued | Monika | Day 3 | Accepted |
| D-35 | Colours for new status tags | Reuse existing token pairs: Missed lesson = stopped, Ready = listened, Needs review = needs-ear, Approved = caught-up, Generating = queued, Generation failed = error; stick picture = warning amber (illustration only). See `docs/design/tokens.md` | Monika | Day 3 | Accepted |
| D-36 | Evening message listens | Not tracked or shown (D-20); only the catch-up "Listened" status stays | Monika | Day 3 | Accepted |
| D-37 | Withdraw cut-off wording | "Withdraw (until 6 pm)"; allowed until the 6 pm send job starts, then Sent (FR-08) | Monika | Day 3 | Accepted |
| D-38 | Remaining states | T3 Sent ("Sent at 6:02 pm · N families", no Withdraw) and Generation failed ("Could not write this catch-up. Nothing was sent.", Try again); T0 offline and greeting deleted; T2 offline. Designs in the prototype and cards | Monika | Day 3 | Accepted |
| D-41 | How the simulated SATS feed works (D-27, technical) | (1) **Fetched when needed**, no scheduler: when Today or the ledger needs a date, if nothing is stored and it is after `Attendance:Sats:ArrivesAt` (09:05 IST), the API asks `IAttendanceSource` and stores the result with its arrival time; before that, or if the source has nothing, Today shows "not in yet, will check at `RecheckAt` (10:00)". A demo endpoint `POST /api/demo/sats-feed` (only while `Demo:Enabled`) forces "not in yet" or "arrives now" for the demo shell and usability tests. Swappable sources: `SimulatedSatsSource` now, file import / state API later. Sundays, second Saturdays and configured holidays return nothing, so they never count as absences. (2) **Who is absent:** repeatable pseudo-random per class and date (`AbsenceRate` ≈ 8%) plus scripted overrides in `data/seed/` for the demo story. (3) **Storage:** `Attendance` gains `Source` (sats/manual), `SourcePresent` (what SATS said; null for manual), `ReceivedAt`, `CorrectedAt`; `Present` stays the effective value, so the ledger is unchanged. (4) **SATS arriving after manual marking:** the teacher's marking stands; SATS values are kept in `SourcePresent`; if they differ, Today says so **[DECIDE: Monika]** copy for that line | Rahul | Day 3 | Accepted |
| D-40 | Teacher sign-in | No registration: teachers are seeded (in reality set up by the school/department from SATS). First sign-in on a phone: mobile number + 4–6 digit PIN; the phone is remembered, so later sign-ins ask for the PIN only, with "Not you? Change phone". 30-day session cookie. 5 wrong PINs → locked 15 minutes. First run: Welcome (T0 state 0) shows until `Teacher.ConsentAt` is set; for now "Start" sets it, and it moves to the voice-consent tick when T0 step 1 is built. All class endpoints require the signed-in teacher's own class. **[DECIDE: Monika]** login screen UX and copy (no design card; built from T0 patterns, te/ta strings drafted) | Rahul (+ Monika for UX) | Day 3 | Accepted (technical); UX proposed |
| D-40 | Teacher sign-in | No registration: teachers are seeded (in reality set up by the school/department from SATS). First sign-in on a phone: mobile number + 4–6 digit PIN; the phone is remembered, so later sign-ins ask for the PIN only, with "Not you? Change phone". 30-day session cookie. 5 wrong PINs → locked 15 minutes. First run: Welcome (T0 state 0) shows until `Teacher.ConsentAt` is set; for now "Start" sets it, and it moves to the voice-consent tick when T0 step 1 is built. All class endpoints require the signed-in teacher's own class. Login screen UX: first sign-in (phone + PIN), phone remembered (PIN only, "Not you? Change phone"), wrong PIN, locked 15 min; designed in the prototype (T9 card follows) | Rahul (+ Monika for UX) | Day 3 | Accepted (technical); UX proposed |
| D-39 | Parent opt-in | Paper consent form collected offline; teacher records Yes / No / No phone per child with date on T8 Families; a child whose form has not come back counts as No; nothing sent unless Yes; family can still stop (D-30); tag for No is "No consent" | Monika | Day 3 | Accepted |
| D-41 | School setup | MVP runs on the demo seed (T-02: 1 teacher, 1 class, 36 fictional children with home languages). CSV import of a real teacher + roster at the end if time permits; admin page is roadmap | Monika | Day 4 | Accepted |
| D-42 | First access for a real teacher | Printed start sheet (QR, link, PIN, "Add to Home screen"): roadmap, follows D-41 | Monika | — | Accepted (roadmap) |
| D-43 | Parent phone number | Entered by the teacher on Family details from the paper form; used only to send messages; never sent to AI providers | Monika | Day 4 | Accepted |
| D-44 | Voice consent record | Store the date and consent wording version; withdrawing deletes all greeting recordings (Settings → My greetings) | Monika | Day 4 | Accepted |
| D-45 | Attendance sync time | Server job at 12:00, retry at 2:00 pm, via the attendance source (simulated, D-27); manual marking any time; nothing marked absent automatically; `/demo` gets "Bring in today's attendance" | Monika | Day 4 | Accepted |
| D-46 | MVP scope | One school, one class, one teacher; the data model allows more | Monika | — | Accepted |
| D-47 | Child's home language | Comes with the roster, confirmed on the consent form, editable by the teacher on Family details; sets the language of messages and audio | Monika | Day 4 | Accepted |
| D-48 | "Tomorrow" in the evening message | Removed (it assumed book order) | Monika | Day 4 | Accepted |
| D-49 | Changing the evening message | From Settings → Evening message; a change needs approval again before anything is sent | Monika | Day 4 | Accepted |
| D-50 | Names in audio | The computer voice never says names ("your child", "your teacher"); names only in written text and in the teacher's own greeting (NFR-05, guardrail 1) | Monika | Day 4 | Accepted |
| D-51 | Settings and Families | Settings (gear on Today): Families, Evening message, My greetings, Help, App language. Families = list → Family details (consent, phone, home language) | Monika | Day 4 | Accepted |
| D-52 | Greeting language | Short greeting per home language in her class, in her voice, shown in Kannada script with a sample to listen to; optional; if skipped, her Kannada greeting + computer-voice introduction in the family's language. Never voice cloning | Monika | Day 4 | Accepted |
| D-53 | "Since yesterday" window on Today | Updates since she last opened Today, and always at least since the start of the previous school day | Monika | Day 4 | Accepted |
| D-54 | Today when nobody is absent | Attendance card "Everyone is here today."; tonight's card "No messages tonight."; no "Added to missed lessons" card | Monika | Day 4 | Accepted |
| D-55 | Second voice note on the same day | Not in the MVP; she corrects today's lessons with "Change" (reopens the checklist) | Monika | Day 4 | Accepted |
| D-56 | English view of the AI summary | A1 returns `summaryKn` and `summaryEn` in the same call (no extra call); the English view shows `summaryEn` | Monika | Day 4 | Accepted |
| D-57 | Bulk approve | The per-catch-up tick is enough; no extra confirm sheet. Bulk approves only catch-ups already opened and ticked (D-28) | Monika | Day 4 | Accepted |
| D-58 | Audio segment labels | Same three parts on teacher and parent views: greeting · computer voice · lesson ("Your greeting" for the teacher, "Teacher's greeting" for the parent) | Monika | Day 4 | Accepted |
| D-59 | No eligible buddy | Pick the classmate present on the most missed days; if nobody was present on any, the card says "Teacher-led: 10 minutes" | Monika | Day 4 | Accepted |
| D-60 | When the pacing nudge shows | 5 or more of the class missed the same lesson this week; otherwise "No nudge this week" | Monika | Day 4 | Accepted |
| D-61 | Week view with no absences | "No absences this week." and no comparison line (also the first week) | Monika | Day 4 | Accepted |
| D-62 | Parent reopens the link after answering | Goes straight to Done; the first answer counts; no second answer | Monika | Day 4 | Accepted |
| D-63 | Reason voice note | Cut from the MVP: picture buttons only; A7 classification not needed (saves T-45 time) | Monika | Day 4 | Accepted |
| D-64 | STOP reply in the demo chat | Not in the demo; the stop link on every parent screen covers FR-19. A STOP reply is for the real channel (FR-16) | Monika | Day 4 | Accepted |
| D-65 | Help and wording changes | Help is one static page (how it works + contact). The wording editor is roadmap: "To change the wording, contact your school"; a changed message needs her approval again | Monika | Day 4 | Accepted |
| D-66 | Families: Yes without a phone number | Save is blocked: "Add the parent's phone number, or choose No phone." | Monika | Day 4 | Accepted |
| D-67 | Attendance missing after the 2:00 pm retry | Today shows "Attendance didn't come in today. Mark it here so tonight's messages can go." with "Mark attendance" | Monika | Day 4 | Accepted |
| D-68 | Loading, offline, error, conflict on every screen | Loading: show saved data at once, else grey placeholder cards (no spinner over a blank screen). Offline: saved data + "No network. Showing what was saved at {time}. It updates when the network returns."; actions go to the outbox ("Saved · will send"). Error: what happened, what is safe, what to do (e.g. "Could not save. Your change is kept on the phone. It will try again."). Conflict (409): "This changed on another device. Showing the latest." then reload. First sign-in offline: "Sign-in needs network the first time." Applies wherever a card says "Not designed" for these states | Monika | Day 4 | Accepted |

## 9. Assumptions

- Calendar: build days Mon 19 Oct – Wed 28 Oct; buffer 29–30 Oct; submit Sat 31 Oct. Weekday capacity ~4–5 h each, weekend ~8 h each.
- Schools are on Dasara break until about 21 Oct (reported); teacher sessions before then are remote.
- Sarvam STT is good enough on adult Kannada speech; we do not rely on child speech recognition.
- Claude credits (USD 500) cover development and evals; we log tokens to verify.
- Judges test on a desktop browser; the demo shell must make the phone views obvious.

## 10. Risks

Full register with fallbacks in `docs/plan.md` §6.

## 11. Unresolved questions

1. Exact Grade 3 textbook chapter order for 2026–27 after the Nali-Kali phase-out (Monika to confirm from the textbook PDF).
2. Sarvam voice quality for Telugu child-directed speech — check on Day 3 with a test teacher.
3. Whether a test teacher will let us record a real greeting for the demo video (consent form needed).
