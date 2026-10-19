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
- **Subject:** maths. One textbook unit chain: place value (tens and ones) → 2-digit addition without carrying → **addition with carrying** → subtraction without borrowing → **subtraction with borrowing**. About 12–18 competencies in the dependency map. D-01 (Accepted)
- **Languages:** teacher UI in Kannada (English toggle for judges). Catch-up audio in **Telugu** and **Kannada**, fully tested. Tamil works through the same pipeline but is only spot-checked. D-02

### 1.2 Stretch (only after the Day 7 code freeze for MVP features is met)

- Real WhatsApp delivery via WhatsApp Cloud API test number (max 5 recipient numbers). D-05
- Board photo capture (Claude vision) in addition to voice. D-06
- Spoken (voice) answer from the child, marked by Claude, low confidence routed to the teacher.

### 1.3 Not in scope

Voice cloning, avatar video, officer/CRP dashboards, SATS/VSK attendance import, other subjects, native app, multi-school admin.

---

## 2. Functional requirements

Priority: **M** = must (MVP), **S** = should (stretch). IDs are referenced by `docs/tasks.md` and tests.

| ID | P | Requirement | Acceptance |
|---|---|---|---|
| FR-01 | M | Teacher marks absentees for today with one tap per absent child (all present by default) | 4 absentees marked in ≤ 10 s; works offline |
| FR-02 | M | Teacher records a voice note (≤ 30 s, Kannada) saying what she taught | Recording queued offline; uploaded on reconnect |
| FR-03 | M | System transcribes the note and Claude maps it to 1–3 competency IDs from the curriculum map, with a one-line Kannada summary and the examples she used | Teacher sees "Today: Addition with carrying" and confirms or changes with one tap |
| FR-04 | M | Fallback when audio or AI fails: teacher picks the lesson from the chapter list | One tap; ledger still updates |
| FR-05 | M | Ledger: for each child, the lessons missed and current status (missed · sent · caught up · needs buddy · stopped by family) | Matches attendance × lessons exactly (deterministic) |
| FR-06 | M | Compression: for a child with several missed lessons, Claude selects at most 2 target competencies that block next week's lessons, with a reason for each kept and dropped item | Targets are always a subset of the graph-computed candidate set (validator) |
| FR-07 | M | Script writer: per (target set, home language) Claude writes a ≤ 90 s catch-up script with 3 Kannada key words, a 5-minute household-object activity, a 3-picture learning check, a Kannada gloss for the teacher, and buddy-card content | Passes the verifier (§5.4) |
| FR-08 | M | Teacher approval: weekly list of draft packages; play audio, read Kannada gloss, Approve or Regenerate ("simpler", "shorter"); Approve all | No package reaches a parent without approval (enforced in API) |
| FR-09 | M | Audio: text-to-speech in the home language, in this order: the teacher's own recorded greeting (recorded once with consent) → a ~5 s **spoken AI disclosure** in the home language ("The rest of this message is read by a computer voice, from Lakshmi teacher's lesson") → the catch-up script. The written transcript starts with the same disclosure. *(Disclosure proposed in D-18.)* | Audio ≤ 300 KB, ≤ 95 s including greeting and disclosure; disclosure segment present in every package; plays on a low-end Android browser |
| FR-10 | M | Parent page from a link: big Listen button, activity picture, then 3-picture answer; audio prompts so a low-literacy parent can follow | Lighthouse mobile performance ≥ 85; total transfer ≤ 500 KB excluding audio |
| FR-11 | M | Picture-tap answer is scored by code (no LLM) and updates the ledger: correct → caught up; wrong → needs buddy | Ledger updates within 2 s of the tap |
| FR-12 | M | Return-day buddy card for every returning child, whether or not the family has a phone: which classmate, 3 sums, key words, 10 minutes | Buddy is a child who was present for those lessons; card is cached offline and printable |
| FR-13 | M | Weekly pacing nudge, e.g. "11 of 36 missed carrying-over. Recap 10 minutes before moving on." | Numbers come from the ledger by code; Claude writes only the wording |
| FR-14 | M | Demo mode: split screen with teacher phone and parent phone; "Reset demo" reseeds the class | A judge completes the loop in ≤ 5 minutes without instructions beyond the landing page |
| FR-15 | M | Consent and deletion: parent opt-in recorded per child; teacher can delete a child's data | Delete removes child, responses and audio; verified by integration test |
| FR-16 | S | Send via WhatsApp Cloud API (template message with link), scheduled for after 6 pm | Delivered to a registered test number |
| FR-17 | S | Board photo capture via Claude vision | Same output as FR-03 |
| FR-18 | S | Spoken answer: STT + Claude rubric; confidence < 0.8 goes to a teacher review queue with the audio | Never auto-marks below threshold |
| FR-19 | M | **Stop these messages** *(proposed in D-19)*: on the parent page, one tap plus one confirmation stops all future messages for that child; reversible from the same page. The ledger shows "Stopped by family" and the child stays on the buddy-card path | After a stop, the API refuses to send to that child (409), covered by an integration test; the ledger shows the status within 2 s |

### Non-functional requirements

| ID | Requirement |
|---|---|
| NFR-01 | Teacher time ≤ 60 s/day for capture + attendance (measured in usability tests) |
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
| T0 Onboarding | Consent, record 10-second greeting once ("Ravi, this is Lakshmi teacher…") | Record → play back → save | Recording · recorded (play / re-record) · consent given · greeting deleted · mic permission denied |
| T1 **Today** | Attendance + lesson capture on one screen | Tap absent faces/names; hold mic button; sync badge | Attendance already marked · lesson already recorded today · offline ("3 waiting to send") · sending · mic permission denied |
| T2 Lesson confirm | Shows mapped lesson | One tap confirm, or pick from chapter list | Processing · confident · low confidence ("Please confirm") · changed by teacher · could not read the note (pick from list) |
| T3 **Approve** | Draft catch-up packages for the week | Play, read gloss, Approve / Regenerate; Approve all | Generating · ready · needs review (verifier failed) · approved · sent · generation failed · offline (approval queued) |
| T4 **Ledger** | Per-child missed lessons and status | Filter: absent this week; tap child for history | Empty (first week) · filtered · offline ("Last updated …") |
| T5 **Return day** | Buddy cards for children back today | Swipe cards; print/share | None due today · marked done · offline (cached cards) |
| T6 Pacing | Weekly nudge | Read; "Plan recap" adds a reminder | No nudge this week ("Most children are on track") · decided |
| T7 Review queue (stretch) | Low-confidence spoken answers | Listen; mark right/wrong | Nothing waiting · marked |

Parent page (no install, opens from a link, Telugu/Tamil/Kannada):

| Screen | Content | States to design (besides default) |
|---|---|---|
| P1 Listen | Teacher photo initial, big ▶ Listen (greeting + catch-up), "AI-assisted, approved by Lakshmi teacher" | Playing · paused · played · network dropped mid-play (resume) · link expired |
| P2 Activity | One picture of the household-object activity, audio instruction replays | Audio replaying |
| P3 Answer | "Ravi, tap the answer" read aloud; 3 large pictures | Picture chosen (confirm / change) · sending · could not send (try again) |
| P4 Done | Positive sound, "Lakshmi teacher will see this" | none |

Demo shell (`/demo`): teacher phone and parent phone side by side, a step guide for judges, and Reset.

### 3.2 Design decisions for Monika

| # | Decision | Recommendation |
|---|---|---|
| D-03 | Attendance UI: name list vs photo grid vs roll-number grid | Roll number + first name grid, 4 columns; photos raise consent questions **[DECIDE: Monika]** |
| D-04 | Visual style and tokens (colour, type, iconography) for both UIs | Accepted: Build Kit palette (warm canvas, deep ink, teal primary), AAA text contrast, Noto Sans Kannada/Telugu/Tamil. See `docs/design/tokens.md` (D-17) |
| D-07 | Picture library style for the learning check | Code-generated place-value pictures (bundles of ten sticks + loose sticks, numerals) in Monika's style; plus ~10 activity illustrations (bottle caps, sticks, stones) **[DECIDE: Monika]** |
| D-08 | Parent page: one screen with sections vs four steps | Four steps, one action per screen, audio auto-prompts **[DECIDE: Monika]** |
| D-09 | How the teacher reviews a Telugu script she cannot read | Kannada gloss + audio; approval wording "I've checked the Kannada summary" **[DECIDE: Monika]** |

### 3.3 Content rules for generated scripts

- Child addressed by first name via placeholder `{child}`; inserted after generation.
- 90 seconds maximum (≈ 160 spoken Telugu words; tuned on Day 3 by timing TTS output).
- Exactly 3 Kannada key words from the lesson, repeated twice each (the child returns to a Kannada classroom).
- Activity uses only household objects from an allowed list (bottle caps, sticks, stones, tamarind seeds, bangles, rotis, spoons).
- Warm, encouraging, no negative words about absence.
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
| Back end | .NET 10 (LTS) Minimal APIs, EF Core 10 + Npgsql, `Anthropic` NuGet SDK, `Microsoft.Extensions.Http.Resilience` | **[CONFIRM: Rahul]** D-11: official SDK vs typed HttpClient |
| Database | PostgreSQL 16 | Docker locally |
| Audio storage | Azure Blob Storage (local disk in dev) | Opus/MP3 at 24–32 kbps |
| Speech | Sarvam AI: Saarika STT (kn-IN), Bulbul TTS (te-IN, ta-IN, kn-IN) | **[CONFIRM: Rahul]** D-12: API key, cost, voice choice. Fallback: Google Cloud STT/TTS |
| LLM | Claude via API; models set in config | See §5.1 |
| Hosting | Azure App Service for Containers (Linux, Central India), Azure Database for PostgreSQL Flexible (B1ms), GitHub Actions; two web apps (staging, prod) on one B1 plan | D-13 (Accepted). Fallback: same Dockerfile on Render/Fly.io |
| Tests | xUnit, WebApplicationFactory with fake stores (no database); Vitest + Testing Library; Playwright + @axe-core/playwright | |

### 4.3 Data model (core tables)

Built schema, keys, indexes and locking: `docs/database.md`.

| Table | Key fields |
|---|---|
| `Teacher` | Id, DisplayName, Phone, PinHash, GreetingAudioUrl, ConsentAt |
| `Class` | Id, TeacherId, Grade, Medium (kn) |
| `Child` | Id, ClassId, RollNo, FirstName, HomeLanguage, ParentPhone?, ParentConsentAt?, ParentStoppedAt? (D-19), HasPhone |
| `Competency` | Id (e.g. `M3.ADD.CARRY`), Grade, NameKn, NameEn, PrerequisiteIds[], NipunOutcome, SequenceNo — loaded from `data/curriculum/` |
| `Lesson` | Id, ClassId, Date, Source (voice/photo/picked), Transcript?, CompetencyIds[], SummaryKn, ExamplesUsed, Confidence, Status |
| `Attendance` | ClassId, Date, ChildId, Present |
| `CatchUpPackage` | Id, ClassId, TargetCompetencyIds[], Language, Script, GlossKn, Activity, CheckItem (json), BuddyContent (json), AudioUrl, Status (Draft/Verified/Approved/Rejected), PromptVersion |
| `CatchUpAssignment` | Id, ChildId, PackageId, MissedLessonIds[], CompressionRationale (json), Token, ScheduledAt, SentAt, OpenedAt |
| `CheckResponse` | AssignmentId, ChoiceId, Correct, SpokenAudioUrl?, Transcript?, Confidence?, Outcome (CaughtUp/NeedsBuddy/Review) |
| `BuddyCard` | ChildId, BuddyChildId, Date, Sums[], KeyWords[] |
| `PacingNudge` | ClassId, WeekStart, Stats (json), TextKn |
| `AiCall` | Id, Purpose, Model, PromptVersion, InputTokens, OutputTokens, LatencyMs, Verdict, Error |

The ledger is a query (attendance × lessons × assignments × responses), not a stored table.

### 4.4 API (first cut)

```
POST /api/auth/pin                          teacher login (seeded) · POST /api/demo/login
POST /api/classes/{id}/attendance           { date, absentChildIds[] }  (idempotent by clientId)
POST /api/lessons                           multipart audio|image or { pickedCompetencyId } → 202 { lessonId }
GET  /api/lessons/{id}                      status + mapping
PUT  /api/lessons/{id}/competencies         teacher correction
GET  /api/classes/{id}/ledger
POST /api/classes/{id}/catchups/generate    builds assignments + draft packages for the week
GET  /api/classes/{id}/packages?status=
POST /api/packages/{id}/approve | /regenerate { hint }
POST /api/assignments/{id}/send             (demo: immediate; prod: schedule 18:00 IST)
GET  /api/p/{token}                         parent payload (no auth; 128-bit token; expires 14 days)
POST /api/p/{token}/answer                  { choiceId } or audio
GET  /api/classes/{id}/buddy-cards?date=
GET  /api/classes/{id}/pacing?week=
DELETE /api/children/{id}                   data deletion
POST /api/demo/reset · GET /health
```

All write endpoints accept a client-generated `clientRequestId` so offline replays are idempotent.

---

## 5. AI design

### 5.1 Where Claude is called

| # | Call | Input | Output (JSON via tool use) | Model (config) | Guardrails |
|---|---|---|---|---|---|
| A1 | **Lesson mapping** | STT transcript (or board photo, stretch) + the current chapter's competency list and textbook excerpt (retrieved deterministically by the class's syllabus week) | `competencyIds[1..3]`, `summaryKn`, `examplesUsed`, `confidence` | Sonnet 5.5 | IDs constrained to an enum of valid IDs; confidence < 0.7 → teacher picks from list |
| A2 | **Prerequisite-aware compression** | Child's missed competency IDs, graph-computed candidate set (missed ∩ ancestors of next week's competencies), next week's plan, child's prior check results, lesson records | `targets[≤2]` with reason; `dropped[]` with reason | Sonnet 5.5 | Targets must be ⊆ candidate set; else fall back to the graph's nearest-prerequisite rule |
| A3 | **Catch-up script writer** | Targets, teacher's lesson records (her examples), language, content rules (§3.3), allowed objects, picture library IDs | `script`, `keyWordsKn[3]`, `activity`, `checkItem{prompt, options[3]{pictureId}, correct}`, `glossKn`, `buddy{sums[3], keyWords}` | Opus 5.5 (low volume, cached per package) | Verifier A4; one auto-retry with verifier feedback; then teacher sees "needs review" |
| A4 | **Verifier** | A3 output | pass/fail + reasons | Code checks + Haiku 5.5 | See §5.4 |
| A5 | Pacing wording | Stats computed by code | one Kannada sentence with slots `{n}`, `{total}`, `{minutes}` | Haiku 5.5 | Numbers filled by code; LLM cannot change them |
| A6 | Spoken answer rubric (stretch) | STT transcript of child, expected answer | `correct`, `confidence` | Haiku 5.5 | < 0.8 → teacher review queue; never required |

Not done by the LLM, on purpose: attendance, ledger, candidate-set computation, picture-tap scoring, arithmetic, buddy selection, scheduling. Rules are cheaper, faster and verifiable where they suffice; Claude is used for language, judgement and grounding.

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

---

## 8. Decision log

Status: **Proposed** → **Accepted** / **Changed**. *Gate* = must be resolved before the affected build day.

| ID | Decision | Recommendation | Owner | Gate | Status |
|---|---|---|---|---|---|
| D-01 | Demo subject/unit | Grade 3 maths, place value → carrying → borrowing | Monika | Day 1 | Accepted (T-00) |
| D-02 | Languages fully tested | Kannada UI; Telugu + Kannada audio; Tamil spot-checked | Monika | Day 1 | Accepted (T-00) |
| D-03 | Attendance UI pattern | Roll number + first name grid | Monika | Day 2 | Proposed |
| D-04 | Design tokens and type | Monika's tokens; Noto Sans Indic families | Monika | Day 2 | Accepted (T-23): `src/web/src/styles/tokens.css`, `docs/design/tokens.md` |
| D-05 | Delivery channel in demo | Simulated parent phone primary; WhatsApp Cloud API test number stretch; `wa.me` share as fallback | Rahul | Day 1 | Proposed |
| D-06 | Board photo capture | Stretch | Monika + Rahul | Day 7 | Proposed |
| D-07 | Picture library | Code-generated place-value SVGs + ~10 illustrations | Monika | Day 3 | Proposed |
| D-08 | Parent page structure | Four single-action steps | Monika | Day 3 | Proposed |
| D-09 | Teacher review of non-Kannada script | Kannada gloss + audio | Monika | Day 3 | Proposed |
| D-10 | Single container (API serves SPA) | Yes | Rahul | Day 1 | Accepted (T-03) |
| D-11 | Claude client | Official `Anthropic` NuGet SDK behind `ILlmClient` | Rahul | Day 1 | Proposed |
| D-12 | Speech provider | Sarvam (Saarika STT, Bulbul TTS) | Rahul | Day 1 | Proposed |
| D-13 | Hosting | Azure App Service + PostgreSQL Flexible, Central India | Rahul | Day 1 | Accepted (T-04): staging + prod apps on one B1 plan, images on GHCR, OIDC deploy |
| D-14 | Curriculum source and wording | State textbook + NIPUN outcomes; Monika approves Kannada names | Monika | Day 2 | Proposed |
| D-15 | Open-source licence | MIT, repo public from submission day | Rahul + Monika | Day 9 | Proposed |
| D-16 | Test participants | 3 teachers (phone/video during Dasara break), plus 2 parents (incl. Telugu; proxies as fallback) after schools reopen | Monika | Day 1 | Accepted (T-00) |
| D-17 | Contrast level and dark theme | All text ≥ 7:1 (AAA), UI ≥ 3:1; dark theme follows the phone setting (`prefers-color-scheme`), no in-app toggle yet | Monika | Day 2 | Accepted (T-23) |
| D-18 | Spoken AI disclosure in parent audio | Greeting → ~5 s disclosure in the home language → script; fixed text per language, synthesised once and cached; native review of the wording **[CONFIRM: Rahul]** | Monika + Rahul | Day 3 (before T-11) | Proposed |
| D-19 | Parent "Stop these messages" | One tap + confirm on the parent page; `ParentStoppedAt` on `Child`; send endpoint refuses stopped children; ledger status "Stopped by family" **[CONFIRM: Rahul]** | Monika + Rahul | Day 5 (before T-15) | Proposed |

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
