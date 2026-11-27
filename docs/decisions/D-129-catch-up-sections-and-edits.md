# D-129 · Catch-ups get today's message's sections; how her edits work there

| | |
|---|---|
| Status | Accepted (27 Nov, Rahul) |
| Owner | Rahul, 27 Nov 2026 |
| Gate | none (T-103 built it; Rahul to confirm) |
| Screens | T3-approve, P1-P4-catch-up |

## Decision

D-91 said catch-ups would get today's message's sections later, and T-103 asks for the same editor on both Approve tabs. A catch-up is built differently from today's message: one language per catch-up (the family's), A3's spoken script with its audio, the activity's words around numbers code chose, and a learning check that updates the ledger. So what maps to what, what she may change, what happens to the audio when she changes the script, and how her note reaches a family in another language had to be decided. Rahul was away; this records what was built so he can confirm or change it.

## Recommendation

**Sections** (the same names, order and editor as today's message):

| Section | On a catch-up | She can |
|---|---|---|
| Concept | A3's script (in the family's language) with its audio player and the Kannada summary | Edit the words |
| Activity at home | A3's instruction and other-objects line; the numbers code chose are shown under it | Edit both lines |
| Video | The lesson's link from the admin page (`LessonLinks`), put in by code when A3's words are saved; only when there is one | Remove, put back |
| Picture question | The learning check: question, three options and answer, all by code | Nothing (no Choose another pair: the check's numbers come from the week's seeded practice) |
| My note | Her note | Add, edit, remove, put back |

- Concept, activity and picture can't be removed: the family's page is Listen → Activity → Answer (FR-10) and the answer updates the ledger (FR-11), as D-108 keeps today's promised sections.
- **Guardrail 3:** every change is a new `ScriptVersion` and clears the approval, so she ticks and approves again; she can change a catch-up that is ready, needs review, or approved but not yet sent (an approved one goes back to ready). The family's page already serves only `approved` at `ApprovedScriptVersion = ScriptVersion`, and edits are refused once sent.
- **Her words are checked by code, never an AI** (as her Kannada on today's message): not empty, the length limits (A3's for the script and activity, today's note limit for the note), no class name, braces or `<>`, the family's script for the script and activity, every written sum right, only numbers the catch-up already uses (0–10, 100, the textbook page, the activity's numbers and their digits, numbers already in its words) and never the picture's answer or other choices; the script must keep the activity's numbers (A4's rule). A refused edit saves nothing and says why.
- **The script's audio after her edit:** her words are voiced again through the same text-to-speech and real-audio check as A3's (`PackageAudio`, FR-09). On save the old audio and A3's Kannada summary are removed (they no longer match), the catch-up goes to *generating* with `Hint = voice`, and the writer voices her words with no AI call, then sets ready (or needs review if the audio is too long or couldn't be made) at the same version. A4's content check and the key-word count judged A3's words, so they leave the review; her words passed the code checks above. This matches today's message, where her edited concept is voiced again (D-92).
- **The script and activity are edited in the family's language**, not translated from her Kannada as on today's message: translating would need a change to A3's prompt (and an eval run). For a Kannada catch-up this is her own language; for another language the edit box is labelled with it and code checks the script.
- **Her note goes as she typed it**, in any language, with no AI and no translation (same reason). The edit box says families read it as she writes it. It shows on the family's Listen page under the teacher line; the video is a link on the activity page (not a new step), so the four-step flow is unchanged.
- **Simpler / Shorter / Try again** still write A3's new words: they replace her script and activity words (`TeacherEdited` goes back to false) but keep her note and what she removed; the video is refreshed from the admin page.

## Notes

- 27 Nov: accepted by Rahul as built in #325.
- 27 Nov 2026: built in T-103 (#321) while Rahul was away; Proposed until he confirms. Open questions for him: translate her note and script edits into other languages through A3 (a prompt change with an eval), and whether the picture should get "Choose another question" from the week's practice.
