# MVP build flow (D-70)

The one page to build from until submission. It answers three questions:
- **which screens**, in what order;
- **what each must do in the MVP**;
- **what not to build now**.

Cards (`T*.md`, `P*.md`) stay the detailed reference. Where a card describes more than this page, **this page wins for the MVP**, and the rest is roadmap.

Rules for this scope:
1. **Nothing already built changes.** Sign-in (#78) and Today attendance (#81) stay as they are.
2. **Every MVP item scores on a judging criterion.** Anything that doesn't goes to the roadmap slide (10%).
3. **Simplest version that demos the loop.** Real data model, real AI, real guardrails; no extra screens or polish.

## 1. What scores, and how the MVP earns it

| Criterion (weight) | What judges look for | Where the MVP earns it |
|---|---|---|
| Efficacy & user centricity (35%) | Impact, intuitive vernacular voice-first UI, **proof of user testing and refinement** | The loop on two phones in Kannada / Telugu; teacher time under 60 s; **usability round 1 (24 Oct) with logged changes**; mentor-driven features (evening message, reason, return speed) |
| Effective AI & technical strength (30%) | LLM used well, guardrails, error handling, works for real | Lesson mapping (A1), compression (A2), script writer (A3), verifier (A4), **evals in the repo**, approval gate, no names to providers, offline attendance + voice note |
| Innovation (25%) | Novel approach, clear differentiation | "This teacher's own lesson" → prerequisite-aware catch-up in the home language → picture check closes the loop |
| Future roadmap (10%) | Pathways to scale and integration | One slide: real SATS/VSK feed, Settings, per-language greetings, family-details editing, WhatsApp, CSV onboarding. **Designed and prototyped**: the prototype is the evidence |

## 2. Screens in build order

Status: ✅ built · 🔨 in progress · ⬜ to build. "Card" links give the detail; the MVP bullets are the scope.

| # | Screen | Status | MVP scope (only this) | Card | Scores on |
|---|---|---|---|---|---|
| 1 | **T9 Sign in** | ✅ #78 | No change | [T9](T9-sign-in.md) | Works for real |
| 2 | **T1 Today: attendance** | ✅ #81 | No change (SATS 12:00 / 2:00 pm, missing card, nobody absent, corrections, offline) | [T1](T1-today.md) | N7, offline |
| 3 | **T1 Today: lesson** | ✅ #84, #86 | Mic: **tap to start, tap to stop**, auto-stop at 30 s. "Or pick from the list" always visible (it also covers a blocked mic). Done card with **Change**; "Added to missed lessons"; tonight's card (count of families marked Yes). "Since yesterday" is roadmap (25 Oct) | [T1](T1-today.md) | Voice-first, < 60 s |
| 4 | **T2 Lesson confirm** | ✅ #86 | Processing → ticked checklist of 1–3 lessons (D-33); low confidence = choose one; "could not hear" and > 20 s → pick from list; note queued offline. `summaryEn` from the same A1 call (D-56) | [T2](T2-lesson-confirm.md) | AI + guardrails |
| 5 | **T0 Setup** (rest) | ✅ welcome, template approve (#88) · ✅ greeting (T-61) | One **Kannada greeting** (record, play, record again) + consent tick, then **approve the evening template** (no "Tomorrow"). Nothing else | [T0](T0-setup.md) | Teacher's own voice (innovation) |
| 6 | **Evening message** (T-44) | ✅ server + 6 pm job (#88) · ✅ demo parent phone (T-18) | 6 pm job: absent + consent Yes + not stopped + lesson confirmed. Written text from the approved template (`{child}`, `{lesson}`, `{page}`, in the home language) **plus her greeting audio only**. Shown in the demo chat with "Is everything OK?" and a stop link | [P0](P0-messages-reason.md) | Mentor input, guardrail 3 |
| 7 | **Reason** (T-45) | ⬜ | 5 picture buttons + Skip. Category stored; teacher only | [P0](P0-messages-reason.md) | Mentor input |
| 8 | **T3 Approve** | ⬜ (T-14) | List + detail: play, Kannada summary, "Why this", **tick → Approve per catch-up**; Simpler / Shorter regenerate (clears the tick); Needs review blocks Approve; tags Ready / Needs review / Approved / Sent / Generation failed | [T3](T3-approve.md) | Guardrail 3, AI |
| 9 | **P1–P4 Parent page** (T-15) | ⬜ | P1: one big Listen (greeting → disclosure → lesson), AI label, **Next never blocked**, step dots. P2: activity picture + homework page. P3: 3 stick-bundle pictures, **shuffled, stored answer**, numeral after the tap. P4: Done + stop link (one confirm, restart on the same page) | [P1](P1-listen.md) … [P4](P4-done.md) | Low-literacy UX, scoring by code |
| 10 | **T4 Missed lessons** (T-13) | ⬜ | By child and by lesson from the ledger; child history; **delete child data** (FR-15) | [T4](T4-missed-lessons.md) | Core need |
| 11 | **T5 Back today** (T-16) | ⬜ | Buddy card: buddy (present most days, else "Teacher-led"), 3 sums from code, key words, "Brought homework", done / undo | [T5](T5-back-today.md) | Core need |
| 12 | **T6 Week** (T-46, T-16) | ⬜ | Return speed (both weeks, numbers reconcile), reasons, nudge when ≥ 5 missed a lesson; "No absences this week" | [T6](T6-week.md) | Primary measure (D-20) |
| 13 | **/demo** (T-18) | ✅ T-18 | Two phones, Reset, "Bring in today's attendance", Ravi's story | — | Judges can test |

**Demo story** (matches `data/seed/sats-scenario.json`): Ravi (roll 4, Telugu) is absent Tue–Thu.
1. Today shows him absent.
2. Lesson confirmed; the evening message goes out; the family taps a reason.
3. Friday: the teacher approves the Telugu catch-up.
4. The parent listens, the child taps a picture.
5. Missed lessons shows Caught up or Needs buddy.
6. Return-day buddy card; Week view.

## 3. Rules that apply to every unbuilt screen (keep the build small)

| Topic | MVP rule | Not now |
|---|---|---|
| Offline | **Attendance corrections and the voice note only** (outbox, already built). Everything else online, with the D-68 error message | Queueing approvals, stops and answers |
| States | D-68 pattern: saved data or placeholders while loading; "No network. Showing what was saved at {time}…"; error = what happened, what's safe, what to do; 409 = show the latest | Bespoke states per screen |
| Accessibility | 48 px targets, existing contrast tokens, labelled buttons, Esc closes dialogs, axe with no serious or critical issues (already in Playwright) | Full review checklist (focus management everywhere, live regions on every change): T-30 if time permits |
| Strings | Keys in all four locale files; **English filled**; kn/te/ta from the cards or a placeholder until the native review | Blocking a build on translations |
| Audio | Catch-up = whole script through TTS with **no names** (D-50), greeting + disclosure in front. Evening message = greeting audio + written text | Stitched evening audio from clips, auto-play prompts, resume after a drop, back 10 s |
| Data model | One greeting per teacher; `Child` consent (Yes / No / No phone) **from the seed**, with the send filter (409 unless Yes, not stopped) | `TeacherGreeting` per language, consent version and audit fields, editing screens |
| Picture check | Shuffled positions + stored correct index; distractors from seed data | Generated distractor rules |

## 4. Not in the MVP (roadmap slide)

Designed, decided and in the prototype; build only after the loop works end to end.

| Item | Decisions | Why not now |
|---|---|---|
| T10 **Settings** (Families, Evening message view, My greetings, Help, App language) | D-49, D-51, D-65 | No judging score; seed covers the data |
| **Per-language greetings** (Telugu / Tamil lines in Kannada letters) | D-52 | Extra setup work; use her Kannada greeting + disclosure in the home language |
| **Family details editing** (phone, language, consent) and the T8 screen | D-39 UI, D-43, D-47, D-66 | Seed has phone, language and consent; the send filter is enforced in code |
| Voice-consent version and withdraw flow | D-44 | A consent tick at setup is enough |
| **Bulk approve** and **Withdraw** | D-28/D-57, D-37 | Three catch-ups approve one by one |
| Mic-blocked and "Sending" states | D-34 | "Pick from the list" covers it |
| Stitched evening audio; parent-page polish (resume, auto-play, back 10 s) | D-48 audio, C-16, C-19 | Polish, not scored |
| Reason voice note + A7, STOP reply, wording editor, Help page, printed start sheet, CSV onboarding | D-63, D-64, D-65, D-42, D-41 | Already parked |
| WhatsApp delivery | FR-16 | Stretch; demo chat stands in |

These decisions stay valid as the design for the next phase; D-70 only sets what is built before 31 Oct.

## 5. Priority if time runs short

Build top to bottom in §2. If something must slip, keep the **loop** (rows 3, 4, 6, 8, 9, 10, 13) and the **evals**. Rows 5, 7, 11 and 12 can show seeded data in the demo.

**Protect half a day for usability round 1 (24 Oct) and the changes it produces.** That evidence is worth more than any extra screen (35% criterion).
