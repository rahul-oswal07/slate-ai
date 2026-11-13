# Hackathon requirements (Build stage)

Extracted from `docs/hackathon/` (Key Messages, Guidelines, our Ideate proposal) on 18 Oct 2026.
This is the checklist the MVP and submission are measured against. If a requirement changes, update this file first.

## 1. Non-negotiables

| # | Requirement | Source | How we meet it |
|---|---|---|---|
| N1 | Stay on the challenge we were selected for: **Problem 1C, student absenteeism** (no change of challenge or team after Ideate) | Guidelines §3.4–3.5 | Scope is the missed-lesson loop only |
| N2 | **AI must be central** and use frontier LLM capabilities; judges read the source code to verify it | Guidelines §3.7 | All Claude calls live in `src/api/SlateAi.Api/Ai/`, prompts in `prompts/`, evals in `evals/` |
| N3 | Original work built during the hackathon window; AI coding assistants allowed; we must be able to explain the build live | Guidelines §7 | Git history from day 0; every artifact committed |
| N4 | Submit by **1 Nov 2026, 23:59** (organisers recommend a few hours early) | Guidelines §4 | We submit on **31 Oct** |
| N5 | Teacher has a phone with **intermittent connectivity** | Key Messages, constraints | Offline queue for teacher inputs (PWA) |
| N6 | **No devices for children** in school | Key Messages, constraints | Child path is the parent's phone at home, or the return-day buddy card |
| N7 | **No extra teacher time** for assessments or data entry | Key Messages, constraints | Teacher action under 60 s/day, no typing |
| N8 | Teachers are wary of anything that **monitors** them | Key Messages (P3 constraint, applies to our user) | Ledger stays with the teacher; no officer view |
| N9 | Child data handled under the **DPDP Act 2023** (our own proposal commitment) | Proposal p10 | Parent opt-in, first name + parent number only, delete on request |

## 2. Target-user needs (Problem 1C key messages → our feature)

| Need stated by organisers | Feature |
|---|---|
| Teachers can't track which child missed which lesson | Missed-lesson ledger, filled from attendance + the day's lesson capture |
| No class time to reteach one child | 90-second home-language voice catch-up + 5-minute household-object activity; return-day buddy card |
| Impossible pacing decisions (move on or repeat) | Weekly pacing nudge from the ledger |

User constraints from our proposal: one Android phone, patchy data, 35+ children, Kannada-medium; shared family phone, limited parent literacy, Telugu/Tamil home language, phone available after 6 pm.

## 3. Build-stage judging criteria

| Criterion | Weight | What judges look for | Our evidence |
|---|---|---|---|
| Efficacy and user centricity | **35%** | Impact potential; intuitive UI; built for user constraints (vernacular, voice-first, contextual); **proof of testing and refinement based on user input** | Kannada teacher UI, Telugu voice, picture-tap replies; 2 rounds of teacher testing logged in `docs/user-testing.md` |
| Effective AI use and technical strength | **30%** | LLM used for what it is good at; technical quality; **guardrails and error handling**; works in real scenarios | Grounded competency mapping, prerequisite-aware compression, verifier pass, teacher approval gate, eval suite, offline mode |
| Innovation and creativity | **25%** | Novel AI approach or an existing approach never applied here; clear differentiation | "Discharge summary" for a returning child; prerequisite-aware compression; this teacher's actual lesson |
| Future roadmap | **10%** | Pathways to development and integration with existing systems | SATS / Vidya Samiksha Kendra attendance import, portable record for migrant children |

## 4. Submission deliverables (due 1 Nov, 23:59)

- [ ] **Demo link** to a testable version, plus anything needed to test it (judge login / demo mode instructions)
- [ ] **GitHub repository access** (open-sourcing earns +USD 250 Claude credits; no effect on scoring)
- [ ] **3-minute demo video** on YouTube (public or unlisted-but-accessible), covering: challenge and how we address it; UI walkthrough; key features mapped to the challenge; ease-of-use features. Judges stop at 3:00.
- [ ] **Short PowerPoint deck** covering:
  - AI/LLM usage in layers: data input → processing → guardrails → evaluation → output
  - Guardrails for wrong or inappropriate AI output
  - Low-connectivity, low-data, low-cost device behaviour: what needs a connection, what degrades offline
  - User inputs and testing (1–2 pages): evidence of feedback collected and acted on
  - Future roadmap (1–2 pages)

## 5. Grand Finale (Top 10, shortlisted 12 Nov)

Source: "Grand Finale: Team Presentation & Demo Guidelines" (organisers' PDF) and the jury's feedback email to the team. Tasks that answer each point: `tasks.md` "Proposed for the Grand Finale".

**Format, Wed 18 Nov, in person, 14 minutes:** presentation 5 min (5–6 slides: problem & solution · innovation · user inputs & testing · technology & data · future pathways), **live demo 3 min**, jury Q&A 6 min.

| ID | Demo rule (guidelines §3, §5, §6) |
|---|---|
| R1 | Start from the homepage; show the journeys of different personas (teacher, parent, child, CRP) |
| R2 | Live test: give an input, show the expected output |
| R3 | Tell the story through one user; don't show every feature |
| R4 | Rehearse the exact click path on the demo machine and network |
| R5 | Backup demo video with voice-over |
| R6 | Pre-load data: logged in, test data ready, tabs open |
| R7 | Say what is real and what is simulated or hard-coded |
| R8 | **Share the final presentation and demo links by Mon 16 Nov** (so all builds merge by Sun 15) |
| R9 | Arrive early; bring our own accessories (phones, cables, hotspot) |

**Jury feedback on our solution (13 Nov):**

| ID | They said | Our answer |
|---|---|---|
| J1 | One minute of AI audio is not enough to recover learning: add an age-appropriate home activity led by the parent, a short concept explainer (video/audio, existing resources) and let teachers personalise the content | D-91 today's message: concept, activity, DIKSHA video, picture question, teacher's note |
| J2 | Teachers must listen to the whole audio and cannot edit: give an editable text script with the audio | D-92 teacher edits each section in Kannada |
| J3 | They could not reach the family and child views, so they could not judge the whole journey | D-94 demo shows the loop end to end on two phones |
| J4 | Unclear which textbook or official learning outcomes are used; contextualise to one state's curriculum | D-90 Nalikali Grade 3 Maths 2026-27 (Karnataka), named on every screen |

**Common suggestions for all solutions (guidelines §7):**

| ID | Suggestion |
|---|---|
| G1 | Declutter: tables, icons with a clear meaning, section breaks, consistent colours for groups (e.g. beginner / advancing / proficient) |
| G2 | Reduce on-screen text: tabs, accordions, expandable sections |
| G3 | Simple and intuitive: clear calls to action, helpful placeholders, intuitive navigation, logical flow between screens |
| G4 | Test with people outside the team, without guidance; note where they hesitate |
| G5 | Design for personas: teacher, parent and administrator need different views of the same information |

Additional USD 500 Claude credits for Top 10.

## 6. Support available now

- USD 500 Claude credits for the Build stage.
- Two 40-minute mentor sessions: one education mentor, one technical mentor. **Book both on Day 1** (see `plan.md`).
