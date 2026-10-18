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

## 5. Later stage (if shortlisted to Top 10)

- 18 Nov 2026, in person: 10–12 slide pitch + **live demo**; organisers recommend a pre-recorded backup video.
- Additional USD 500 Claude credits for Top 10.

## 6. Support available now

- USD 500 Claude credits for the Build stage.
- Two 40-minute mentor sessions: one education mentor, one technical mentor. **Book both on Day 1** (see `plan.md`).
