# Journey: Tappida Paatha, end to end

Spec: FR-01–FR-22 · Tier: MVP · Prototype: [prototype/index.html](prototype/index.html) (steps 1–13) · Review: [ux-review.md](ux-review.md)
Decisions: D-03, D-07, D-08, D-09, D-20–D-24, D-27–D-39

English is the default language for development (D-32); Kannada, Telugu and Tamil come from each card's copy table.

## Summary

Lakshmi teaches a Grade 3 Kannada-medium class of 36. Attendance arrives from SATS (simulated in the MVP, D-27); she only corrects it. Once a day she taps the mic and says what she taught. Claude maps the note to a textbook competency; she confirms it with one tap. Code then adds that lesson to every absent child's missed lessons. Each evening a child is away, the family gets the teacher's recorded greeting plus a fixed message she approved once at setup, with slots filled by code (no AI). The family can optionally say why the child is away. On Friday Lakshmi approves this week's catch-ups (one per language and target set, not per child). The family listens, does a short activity, and the child taps one of three pictures. Code scores the tap and updates her missed-lessons view. On return day she gets a buddy card and taps "Brought homework". Weekly, she sees how quickly absent children came back, and a pacing nudge. The family can stop messages at any time.

## Actors

| Actor | Who | Device | What they need |
|---|---|---|---|
| Teacher | Lakshmi, Grade 2–3 teacher, Kolar district | Low-cost Android, intermittent data | Under 60 s a day, no typing, nothing reaches a parent without her approval, records only she sees |
| Family | Ravi's mother, Telugu-speaking seasonal-worker family | One shared phone, after 6 pm | Audio first, one action per screen, never stuck, can stop at any time |
| Child | Ravi, age 7 | Uses the family phone | Hears his teacher, does a short activity, taps a picture; no score, no shame |
| Buddy | A classmate who was present (e.g. Anjali) | None (printable card) | A 10-minute card with 3 sums and 3 Kannada words; never sees family or channel status |
| Judges | Hackathon panel | Desktop browser | Complete the loop in ≤ 5 minutes on the split-screen demo (FR-14) |

## Journey steps

Order follows the prototype guide (steps 1–13). The prototype places the Thursday buddy card after the Friday catch-up; see Open items at the end.

| # | When | Who | What they do | Screen card | FR | Task | AI / code / human |
|---|---|---|---|---|---|---|---|
| 1 | Once, at setup | Lakshmi | Records a 10-second greeting, ticks voice consent, listens to the Telugu version of the evening message and approves the template once | [T0](T0-setup.md) | FR-09, FR-15, FR-20 | T-44, T-19, T-11 | Human records and approves. Code stores consent and template approval. No AI |
| 1a | Once, at setup (and when a family changes its mind) | Lakshmi | From "All set" → "Review families", copies each family's paper consent form: Yes / No / No phone. Nothing is sent to families marked No or No phone (D-39) | [T8](T8-families.md) | FR-15, FR-19, FR-20 | T-19, T-44 | Human records. Code stores the value and filters every send (409). No AI |
| 2 | Tue 9:05 | System, then Lakshmi | Absent list arrives from SATS (simulated, D-27), labelled with source and time. She corrects it only if wrong; if nothing has arrived she marks absentees in the app | [T1](T1-today.md) | FR-01 | T-06, T-12 | Code (feed, draft, outbox). Human corrects. Corrections never written back to SATS. No AI |
| 3 | Tue, after class | Lakshmi | Taps the mic, says in Kannada what she taught (≤ 30 s), taps again | [T1](T1-today.md) | FR-02 | T-12, T-07 | Human speaks. Code queues audio in the outbox. Speech-to-text by Sarvam |
| 4 | Tue, ≤ 20 s later | Claude, then Lakshmi | Claude (A1 via `ILlmClient`) maps the transcript to 1–3 competency IDs with a confidence. She sees them as a ticked checklist, unticks any that are wrong and confirms with one tap (D-33), or picks from the chapter list | [T2](T2-lesson-confirm.md) | FR-03, FR-04 | T-07, T-05 | AI proposes (augmentation). Code validates IDs against the curriculum enum. Human confirms. Confidence < 0.7 → she picks |
| 5 | Tue, at confirm | System | "Added to missed lessons: 4 children" appears on Today | [T1](T1-today.md) | FR-05 | T-06, T-13 | Code: attendance × confirmed lesson. No AI |
| 6 | Tue, after 6 pm | System → Ravi's family | Evening message: teacher's greeting + approved template; slots (today's lesson, page, tomorrow, "a classmate is keeping Ravi's seat") filled by code | [P0](P0-messages-reason.md) | FR-20 | T-44 | Code only. Template approved once. Send refused (409) unless template Approved, lesson confirmed, family opted in and not stopped. No AI |
| 7 | Tue evening, optional | Ravi's mother | "We missed Ravi today. Is everything OK?" She taps one of 5 pictures, or speaks, or skips | [P0](P0-messages-reason.md) | FR-21 | T-45 | Human. AI (A7, Haiku) classifies a voice note only; low confidence → "other". Category only stored |
| 8 | Fri afternoon | System, then Lakshmi | Catch-ups are drafted per language and target set. She opens each, plays it, reads the Kannada summary, ticks "I've checked the Kannada summary", approves. Withdraw until 6 pm | [T3](T3-approve.md) | FR-06–FR-09 | T-08, T-09, T-10, T-11, T-14 | AI: A2 compression (targets ⊆ code candidate set), A3 script, A4 verifier. Human approves. Guardrail 3: nothing sent unless `Approved` (D-28) |
| 9 | Fri, after 6 pm | Ravi's family | Opens the link; Listen: teacher's voice → spoken AI notice → lesson | [P1](P1-listen.md) | FR-09, FR-10 | T-15, T-11 | AI-written, teacher-approved audio. Labelled "AI-assisted, approved by Lakshmi teacher". Next never blocked by audio |
| 10 | Fri evening | Ravi + family | Activity with sticks (or household objects) and the textbook homework page | [P2](P2-activity.md) | FR-10, FR-07 | T-15 | Human. Content approved in step 8 |
| 11 | Fri evening | Ravi | Taps one of three bundle-of-sticks pictures; numeral shows only after the tap (D-07) | [P3](P3-answer.md) | FR-11 | T-15 | Code scores the tap (no LLM). Options shuffled by code |
| 12 | Fri evening | Ravi | "Well done, Ravi" whatever the answer | [P4](P4-done.md) | FR-10 | T-15 | Code. No score shown to the family |
| 13 | Within 2 s of the tap | System | Missed lessons update: correct → Caught up; wrong → Needs buddy | [T4](T4-missed-lessons.md) | FR-11, FR-05 | T-15, T-13 | Code only |
| 14 | Next morning | Lakshmi | "Since yesterday" on Today lists listens, answers, reasons, homework, stops | [T1](T1-today.md) | FR-05, FR-11, FR-21 | T-12 | Code. No AI |
| 15 | Thu, return day | Lakshmi + buddy | Buddy card: classmate who was present, 3 sums, 3 Kannada words, 10 minutes. She taps "Brought homework" and "Mark buddy time done" | [T5](T5-back-today.md) | FR-12 | T-16 | Code: buddy selection and sums. Key words from the textbook. Human taps |
| 16 | Fri 3:30 pm, weekly | Lakshmi | Return speed (1, 2, 3, 4+ days; back within 2 school days; reasons) and a pacing nudge ("Plan recap" / "Not this week") | [T6](T6-week.md) | FR-22, FR-13 | T-46, T-16 | Code computes all numbers. AI (A5, Haiku) writes only the nudge wording around slots |
| 17 | Any time | Ravi's family | "Stop these messages" from any parent screen or the evening message, or reply STOP; one confirmation; chat confirmation with "Start them again" | [P0](P0-messages-reason.md)–[P4](P4-done.md) | FR-19 | T-15 | Human. Code: send endpoint returns 409 for a stopped child; ledger shows "Stopped by family"; buddy card continues (D-30) |

## Screen map

Teacher app (PWA, Kannada first, English captions):

```
T0 Set up (once; no bottom nav)
   ├─► T8 Families and consent ("Review families" on All set)
   └─► T1 Today ──► T2 Lesson confirm (+ pick-lesson sheet)
Bottom nav (5 tabs, always visible after setup):
   [Today T1] [Approve T3 + detail] [Missed T4 + child + T8] [Back today T5] [Week T6]
```

| ID | Screen | Tab | Card |
|---|---|---|---|
| T0 | Set up: greeting, consent, evening-message template | none (before nav) | [T0-setup.md](T0-setup.md) |
| T1 | Today: attendance, mic, lesson, tonight, since yesterday | Today | [T1-today.md](T1-today.md) (owns the nav spec) |
| T2 | Lesson confirm + pick-lesson sheet | Today (sub-screen) | [T2-lesson-confirm.md](T2-lesson-confirm.md) |
| T3 | Approve this week's catch-ups + detail | Approve | [T3-approve.md](T3-approve.md) |
| T4 | Missed lessons: by child, by lesson, child history | Missed | [T4-missed-lessons.md](T4-missed-lessons.md) |
| T5 | Back today: buddy cards | Back today | [T5-back-today.md](T5-back-today.md) |
| T6 | Week: return speed + pacing nudge | Week | [T6-week.md](T6-week.md) |
| T8 | Families and consent: paper consent per child | Missed (sub-screen; also from T0 All set) | [T8-families.md](T8-families.md) |

Parent (no install; opens from a link; Telugu/Tamil/Kannada):

```
P0 Messages (evening message, catch-up link) ──► Reason (optional)
                                     └──► P1 Listen ► P2 Activity ► P3 Answer ► P4 Done
Stop link on every screen
```

| ID | Screen | Card |
|---|---|---|
| P0 | Messages (WhatsApp stand-in) and reason for absence | [P0-messages-reason.md](P0-messages-reason.md) |
| P1 | Listen | [P1-listen.md](P1-listen.md) |
| P2 | Activity at home | [P2-activity.md](P2-activity.md) |
| P3 | Answer (picture tap) | [P3-answer.md](P3-answer.md) |
| P4 | Done | [P4-done.md](P4-done.md) |

## All cards

- [T0-setup.md](T0-setup.md)
- [T1-today.md](T1-today.md)
- [T2-lesson-confirm.md](T2-lesson-confirm.md)
- [T3-approve.md](T3-approve.md)
- [T4-missed-lessons.md](T4-missed-lessons.md)
- [T5-back-today.md](T5-back-today.md)
- [T6-week.md](T6-week.md)
- [T8-families.md](T8-families.md)
- [P0-messages-reason.md](P0-messages-reason.md)
- [P1-listen.md](P1-listen.md)
- [P2-activity.md](P2-activity.md)
- [P3-answer.md](P3-answer.md)
- [P4-done.md](P4-done.md)

Accepted design: [prototype/index.html](prototype/index.html) (v2). Findings and IDs: [ux-review.md](ux-review.md).

## Open gates (not designed past)

| Gate | What is open | Where it shows | Until resolved |
|---|---|---|---|
| FR-20 per-child "Skip tonight" **[CONFIRM: Rahul]** | Whether the teacher can skip tonight's evening message for one child until 6 pm (review A-10) | T1 tonight's message card | Not built. The card shows count and "See the message" only |
| Teacher's voice on WhatsApp (review C-21) | WhatsApp templates can't carry audio. Route: link to a voice page, or a separate session message | P0 evening message, T0 template step | Demo uses the simulated parent phone (D-05). WhatsApp stays stretch (T-40) |
| Buddy-card wording for no-phone children (review B-21) | Sums come from code and key words from the textbook; who approves any AI wording on cards for children whose families have no phone | T5 buddy card, T1 "No phone · buddy card" tag | Show only code sums and textbook key words on those cards |

Other open items for the whole journey:

- Calendar: Ravi is away Mon and Tue and is "back after 2 days" on Thursday; Wednesday is not accounted for. The weekly catch-up (Friday) arrives after he is back. Confirm the story dates before the demo seed (T-18).
- Resolved (D-39): family opt-in (FR-15, T-19) is recorded on [T8 Families and consent](T8-families.md); T0 "All set" and T1 tonight's counts come from it.

## Spec follow-ups found while writing the cards

These came up while writing the cards. The prototype slips are already fixed in the prototype and the cards: status words, evening-message wording, the AI label on P2/P3, the hard-coded counts, the low-confidence copy, and gendered fixed text. These need a spec or data-model change, or a decision:

| # | Issue | Affects | Owner |
|---|---|---|---|
| 1 | **Real names reach text-to-speech (NFR-05).** The script addresses the child by name ("Ravi, …"), ends "Show `{teacher}`", and the disclosure names the teacher. If Sarvam synthesises these, names go to the provider. Options: synthesise placeholders as generic words ("your teacher"), or splice the name from the teacher's own recorded greeting | FR-09, T-11, P1 | Rahul (before T-11) |
| 2 | `CatchUpPackage.Status` is Draft/Verified/Approved/Rejected. The UI needs Generating, Ready, Needs review, Approved, Sent and Generation failed, plus a script version that the approval is bound to (D-28) | §4.3, T-14, T3 | Rahul |
| 3 | Endpoints missing from §4.4: withdraw, bulk approve (checked only), parent stop and restart, STOP reply | §4.4, T-14, T-15 | Rahul |
| 4 | `MessageTemplate` slots still list `{buddy}` (removed by D-29) and lack `{child}`. The evening message needs home-language lesson names (Telugu, Tamil) and pre-synthesised audio, but `Competency` has only NameKn and NameEn | §4.3, T-44, T-05 | Rahul; names: Monika (D-14) |
| 5 | FR-07/A3 lists the buddy sums as AI output; guardrail 4 and the review (B-21) say the sums come from code | FR-07, T-09, T-16 | Rahul |
| 6 | FR-05 still has one status per child. The cards use learning status (missed, sent, listened, caught up, needs buddy) plus a separate channel status (no phone, stopped by family). The by-lesson view should count per child × lesson | FR-05, T-13 | Rahul |
| 7 | **Resolved (D-33).** FR-03 maps a note to 1–3 competencies, but T2 shows one. The confirm screen needs a multi-lesson design | FR-03, T-07, T2 | Monika |
| 8 | **Resolved (D-37).** Withdraw cut-off: FR-08 says "until the send starts"; the UI says "until 6 pm". Use one wording | FR-08, T3 | Monika |
| 9 | **Resolved (D-35, tokens.md).** tokens.md has no tags for Ready, Needs review, Approved, Generating or Missed lesson, no `stick` illustration colour, and calls "Needs review" by the name "Needs your ear". Add the tags with contrast values | tokens.md | Monika |
| 10 | Nav labels and attendance tiles render Kannada at 14 px; tokens.md asks 16 px for Indic text. Accept an exception for the 5-tab nav or reduce to 4 tabs | tokens.md, T-12 | Monika |
| 11 | **Resolved (D-39, [T8](T8-families.md)).** No screen records parent opt-in, yet T0 shows "31 of 36 families agreed" (FR-15, T-19) | T-19 | Monika + Rahul |
| 12 | **Resolved (D-36): dropped.** The child history shows "evening message listened", but `EveningMessage` has no listened field, and D-20 says never count message opens. Drop it or define it | T4, D-20 | Monika |
| 13 | Story calendar: Ravi is away Mon and Tue but is "back after 2 days" on Thursday, and the Friday catch-up comes after his return. Fix the demo seed so the dates line up | T-18 demo seed | Rahul |
| 14 | Undefined rules: when the weekly recap nudge shows; absence spells that cross a week; buddy fallback when no classmate was present on every missed day | FR-13, FR-22, FR-12 | Rahul |
| 15 | Telugu fixed text uses gendered verbs; it needs a neutral or per-child-gender form after native review | P0, P1 | Monika (T-26) |
| 16 | **Resolved (D-38 and D-34).** Not yet designed: T0 greeting deleted, mic permission denied (T0, T1), sending (T1), offline (T0, T2), T3 Sent and Generation failed | T0–T3 | Monika |
