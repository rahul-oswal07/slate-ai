# Education mentor session — 19 Oct 2026

| | |
|---|---|
| Who | Education mentor (CSF panel), Monika, Rahul |
| Source | 25-minute call recording, transcribed automatically. Full transcript kept locally in `docs/hackathon/` (not committed: the repo is public) |
| Status | **Decided by Monika, 20 Oct 2026.** D-20, D-22–D-26 accepted; D-21 accepted as the pre-approved template option, awaiting Rahul's confirmation on guardrail 3 / FR-08. Recorded in `spec.md` §8 |

## 1. What the mentor said

| # | Point | Where |
|---|---|---|
| 1 | We missed the main point: the intervention should strengthen the **connection between classroom and home** and create a **pull** back to school ("I can't miss three days in a row"). Today everything is push ("school bhejo"). | 00:07–01:07 |
| 2 | Messages that come **from the teacher** carry real weight with parents. Rocket Learning's experience confirms this. | 01:07–01:59 |
| 3 | At home, young children (under ~10) get **engagement more than learning**; learning at that age is a directed process. Low-income parents have little time. **Learning as the main KPI is ambitious.** | 01:59–04:42 |
| 4 | Optimise for **getting children back sooner** and **engagement while away**. Test this with teachers and mentors; they will tell you the real pain point. | 04:10–05:14 |
| 5 | Use **resources families already have**: school-issued textbooks and workbooks. Parents everywhere understand **homework** ("more homework = good school"). Every thread should tie back to coming to school. | 05:29–07:07 |
| 6 | Teachers don't want to be monitored, but they would value a **simple view of why children are absent**. Read **NFHS-6**: illness (e.g. diarrhoea) and nutrition drive a lot of absence. Absence is not just "parents don't care" or "school is poor"; there are many factors, including frequent local holidays. | 07:35–10:00 |
| 7 | Instruction target is 200 days; realistically ~160. Anything that **extends learning time beyond school days** is valuable; at home the constraint is the device, not time. Our "after 6 pm" assumption is fine. See CSF's home-learning work. | 10:00–11:42 |
| 8 | Our core depends on **sequenced learning** ("missing one Lego block"). **Numeracy is well scaffolded**, so it's the right choice. Languages are less predictable. Start with a **rule-based** dependency model so we stay in control. | 12:07–14:18 |
| 9 | In teacher interviews, find the **exact pain point** and the **pathways schools already use** to bring children back (e.g. the cook or a community member visits the home). WhatsApp suits hard-to-reach families with internet; local routes may be more personal. | 14:57–16:42 |
| 10 | Success factors from a system view: (a) **how many children come back, and how soon** (plot absence length × number of children); (b) **how engaged they are at home**; (c) **who is absent and why**, at a micro level. | 16:54–18:13 |
| 11 | Capturing the **reason for absence** from the parent could add accountability. | 18:15–18:47 |
| 12 | Strong endorsement: reaching children outside school is the hardest gap; hopes it can be integrated into the system. Offered to stay in touch on LinkedIn. | 19:45–20:26 |

## 2. What this changes

The product stays the same loop, but its purpose and headline measure shift:

> **Before:** catch up the lesson a child missed, measured by a passed learning check.
> **After:** keep an absent child connected to their class so they come back sooner, with a short lesson and homework from their teacher while they are away. Measured by how quickly children return and whether they did the home task. The learning check stays as a secondary signal.

### Decisions (20 Oct)

| ID | Monika's decision |
|---|---|
| D-20 | Accepted: return speed first, then home engagement, learning check secondary |
| D-21 | Accepted: **pre-approved template** with code-filled slots, no AI text daily (FR-20). **Rahul to confirm** guardrail 3 / FR-08 |
| D-22 | Accepted: textbook page per competency (added in T-22), "Brought homework" on the buddy card |
| D-23 | Accepted: caring and optional, picture buttons + optional voice note, category only (FR-21) |
| D-24 | Accepted: weekly return-speed card, teacher only (FR-22) |
| D-25 | **Roadmap only** (no research in this stage) |
| Tone | Agreed with "Where we should not follow the feedback literally" |
| Scope | Approved: drop T-41, T-42, T-43; T-40 stays stretch (D-26) |

**Numbering:** FR-19 was already taken by "Stop these messages" (#53), so the new requirements are **FR-20** (evening message), **FR-21** (reason for absence), **FR-22** (return-speed card).

### Proposed decisions (as discussed)

| ID | Decision | Recommendation | Owner | Build cost |
|---|---|---|---|---|
| D-20 | Primary success measure | **Days until a child returns** (and share of absent children back within 2 school days); then **home engagement** (listened + homework done); learning check secondary. Still never message opens. | Monika | Docs only |
| D-21 | Message on the first evening of absence | Every evening a child is absent: teacher's recorded greeting, today's short lesson, one homework task, and "tomorrow we're doing X; `{buddy}` is keeping your seat". Several missed days still compress into one catch-up on the return evening. **Conflict:** FR-08 has weekly teacher approval and nothing reaches a parent unapproved (guardrail 3); daily generated messages would mean daily approvals and put NFR-01 (≤ 60 s/day) at risk. Option: a fixed, pre-approved template with code-filled slots ("We missed `{child}` today; tomorrow we're doing X"). | Monika (+ Rahul on FR-08) | ~0.5 day (prompt, schedule, template) |
| D-22 | Homework from school materials + bring-back hook | Activity points to the **child's own textbook/workbook page** with a household-object option. "Show `{teacher}` when you come back." (placeholder only, never the real name; AI guardrail 1) On return day the teacher taps **Brought homework** on the buddy card. Needs page numbers per competency in the curriculum data. | Monika (pages), Rahul | ~0.5 day |
| D-23 | Reason for absence | One optional step on the parent page: **"Why was Ravi away?"** with 5 picture buttons (unwell · family travel/work · festival or function · needed at home · other) and an optional voice note. Claude classifies the voice note into one of the categories; low confidence → "other". Teacher sees categories only. | Monika | ~0.75 day |
| D-24 | Return-speed view for the teacher | Weekly card: absences by length (1, 2, 3, 4+ days) this week vs last, plus reasons. Teacher's own view; nothing goes to officers. | Monika | ~0.25 day |
| D-25 | Existing pathways (cook/community visit) | Don't build yet. Ask in round 1 interviews; if confirmed, add a "Ask someone to visit" list for children absent 3+ days with no phone. | Monika | Research first |

**Kept as is (mentor agreed):** Grade 3 numeracy (D-01), rule-based curriculum graph with Claude choosing within code-computed limits, Karnataka/Kannada medium, delivery after 6 pm, teacher's voice as the trusted messenger.

### Where we should not follow the feedback literally

- **"Naming and shaming" and "punishment."** The mentor suggested the reason prompt could make parents feel accountable, and we discussed punishment as a pull. The families we're building for are often absent because of illness or seasonal work. Shame is likely to make them ignore the messages or pull away from the school. Our content rules already ban negative words about absence. **Recommendation:** ask for the reason in a caring way ("We missed Ravi today. Is everything OK?"), keep it optional, and make the pull positive: the teacher's voice, a buddy waiting, homework to show, being part of tomorrow's lesson.
- **Health information.** "Unwell" is a category, not a diagnosis. Store the category only, show it only to the teacher, and never ask for symptoms. This keeps us within our DPDP commitments.

### Scope: what we drop to make room (~2 days)

| Drop / defer | Why |
|---|---|
| T-41 board-photo capture | Was already stretch |
| T-42 spoken answer from the child | Replaced by the parent's spoken reason (same speech pipeline, simpler) |
| T-43 full Tamil review | Tamil stays spot-checked |
| WhatsApp Cloud API (T-40) | Still stretch; simulated phone remains the demo path |

### Changes to artifacts once accepted

- `intent.md`: proposed outcome and success measures (D-20).
- `spec.md`: FR-07 (homework + preview), new FR-20 (evening message), FR-21 (absence reason), FR-22 (return-speed card); content rules §3.3; data model (`AbsenceReason`, `HomeworkReturned`, textbook page refs); new AI call A7 (reason classification, Haiku); decision log D-20 to D-25.
- `plan.md` / `tasks.md`: new tasks for D-21 to D-24 in Days 3–6; drop T-41 to T-43.
- `quality.md`: eval cases for A7; usability questions below.
- `judging-narrative.md`: lead with "keeps absent children connected so they come back sooner"; cite the mentor's input as evidence of refinement.
- `user-testing.md`: log this session as finding source F-M1 to F-M12.

## 3. Add to teacher interviews (round 0/1)

1. When a child stops coming, what do you do today? Who else helps (cook, neighbour, SDMC member, older sibling)?
2. What are the usual reasons children in your class are absent? Which ones can the school do anything about?
3. Would a parent reply to "Is everything OK?" Would they tell you the reason?
4. Do you give homework now? From which book? Do children bring it back?
5. Which would help you more: knowing who missed what, or knowing who is likely to stay away longer?
6. How many days away before a child becomes hard to bring back?

## 4. Reading the mentor recommended

- Rocket Learning RCT on WhatsApp-based at-home learning — [AEA registry entry](https://www.socialscienceregistry.org/trials/17446)
- NFHS-6 (National Family Health Survey), child health and nutrition sections
- Central Square Foundation's work on home-learning solutions (CSF website)
