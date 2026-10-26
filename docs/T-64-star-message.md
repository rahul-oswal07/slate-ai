# T-64 · "Well done" star message to families (FR-25, D-82)

| | |
|---|---|
| Task | T-64 (`docs/tasks.md`, *Open*) · **depends on T-63** (`docs/T-63-attendance-stars.md`) |
| Requirement | FR-25 (`spec.md` §3) · Decision D-82 (`spec.md` §8) |
| Source | Rahul, 26 Oct: "send a message to the parent that their child was present, well done, keep it up" (follows mentor point 5, 24 Oct) |
| Owner | Rahul (product owner with Monika, and engineering) |
| Status | **Planned (26 Oct).** Accepted by Rahul as product owner, including one extra Db test file (§7). Build after T-63 merges |
| Branch | `feat/T-64-star-message` · one PR |
| Size | About 1–1.5 days: one migration, no AI, a new send path behind teacher approval |

---

## 1. Why

T-63 tells the teacher who to praise in class. Families of children who came hear nothing, while families of absent children get a message every evening. A short "well done, keep it up" from the teacher makes the pull back to school positive at home too (`spec.md` §5).

## 2. What we decided (D-82)

1. **Who gets it:** the family of every child who earned the **weekly attendance star** for a finished week (T-63 rule: present at least one day, away at most 1 school day). It is not only for a full week, so a child who was ill one day still gets it. Catch-up stars don't trigger a message (P4 already says it).
2. **Only families who can get messages:** consent `yes`, not stopped (D-19), a template in their language. The same `EveningEligibility.Decide` rules as the evening message: no phone, no consent and stopped families get nothing, and nothing goes out half-filled.
3. **Nobody else hears anything.** No message to families whose child didn't earn the star, and no "your child missed…". Messages never mention other children, counts or days.
4. **The teacher sends it, not a timer** (guardrail 3, two keys):
   - **Wording:** a fixed template per language, approved by her once. It's a new `star` kind of `MessageTemplate`, with the same draft → approved flow and version rule as the evening message (D-21, D-49).
   - **Each week:** she taps **Send** on the T6 Stars card after seeing who it goes to. Nothing is sent automatically.
5. **Fixed wording, slots by code.** The only slot is `{child}` (written text only, D-50). The page shows "From {teacher}" and plays her greeting if she recorded one (T-61). No AI, no numbers, no days.
6. **Once per child per week.** A second tap, a replay or a retry never sends twice (unique child + week).
7. **When she can send:**
   - From the moment the week is **finished** (T-63) until the next week finishes. So there is one week to send at a time, and a missed Saturday can still go out on Monday.
   - Sending needs the network, as delete does in T4 (D-76 (6)). It is not queued offline.
8. **Same link rules as the evening message:** a random 128-bit token, a 14-day expiry (`Evening:LinkDays`), and the stop link on the page.

### Not in T-64

- Automatic or scheduled sends. Messages on catch-up stars. Term-total or "10 stars" messages.
- Real WhatsApp delivery (FR-16): the message goes where the evening message goes today (the parent link and, in the demo, the family chat).

## 3. Screens and copy

### T6 Week: send from the Stars card

| Where | What it shows |
|---|---|
| Finished week, not sent | Under the chips: **"Send well done to {{count}} families"** (secondary button). Hidden when no family is eligible ("No families to send to: no consent, no phone or stopped." as a caption) |
| Current week view, last week not sent and still sendable | A slim row above the chips: "Last week: {{count}} children earned a star." with the same button |
| Sent | "Sent to {{count}} families, {{time}}." No button |
| Too late (the next week finished) | Nothing, so there's no nagging |
| Offline | Button disabled, caption "Sending needs the network." |

**Send sheet** (dialog, focus trapped, Esc closes, **Cancel is the default**, as the T4 delete dialog):
- **Title:** "Well done message"
- **The message:** in Kannada (her language), with the child slot shown as "{child}". Below it, one line per other family language, "Also in Telugu (3), English (1)…", each expandable to read.
- **Who gets it:** "Families of children who earned a star this week, with consent, who haven't stopped messages." and the count.
- **First time, or after the wording version changed:** a tick, "I have read this message", then **"Approve and send"**. That approves the template (all languages) and sends.
- **After that:** **"Send"**.
- **Result:** "Sent to {{count}} families." Failure: "Could not send. Check the network and tap Try again." (same pattern as D-76 (3)).

### P5 Star (parent page, new `kind: "star"` on `/p/{token}`)

Slate parent style (D-77, `docs/design/tokens.md`, warmer surface):
- the `Celebration` medal banner from P4 (decorative);
- the message text (template, code-filled);
- "From {teacher}" with her greeting's play button if she has one;
- the stop link (FR-19).

There is no reason page, no activity and no numbers. An expired link shows the existing Expired page, without the homework card. A stopped family sees the existing Stopped page.

### Demo (`/demo`)

- The family chat gets a **star bubble** ("⭐ Well done" + time) that opens `/p/{token}` in the in-app browser bar, after the day separator, like the evening bubble.
- **Walkthrough:** Week → "Last week" row → Send → the family phone shows the bubble.
- Both demo families are present on the seeded days before today (`DemoStory`, plus T-63's extra weeks), so they earn last week's star.

### Strings (all five locale files)

| Key | English |
|---|---|
| `week.stars.sendButton_one` / `_other` | Send well done to {{count}} family / Send well done to {{count}} families |
| `week.stars.lastWeek_one` / `_other` | Last week: {{count}} child earned a star. / Last week: {{count}} children earned a star. |
| `week.stars.sent_one` / `_other` | Sent to {{count}} family, {{time}}. / Sent to {{count}} families, {{time}}. |
| `week.stars.noFamilies` | No families to send to: no consent, no phone or stopped. |
| `week.stars.needsNetwork` | Sending needs the network. |
| `week.stars.sheet.title` | Well done message |
| `week.stars.sheet.who` | Families of children who earned a star this week, with consent, who haven't stopped messages. |
| `week.stars.sheet.alsoIn` | Also in {{language}} ({{count}}) |
| `week.stars.sheet.readTick` | I have read this message |
| `week.stars.sheet.approveSend` | Approve and send |
| `week.stars.sheet.send` | Send |
| `week.stars.sheet.cancel` | Cancel |
| `week.stars.sheet.failed` | Could not send. Check the network and tap Try again. |
| `parent.star.title` | Well done! |
| `parent.star.from` | From {{teacher}} |
| `demo.chat.star` | Well done |

**Message template**, new seed file `data/seed/star-message.json`, `"version": "1"`:

| Lang | Text (drafts except `en`; native review as for the evening message, T-26; gender-neutral as D-72/D-78) |
|---|---|
| en | {child} came to school this week and earned a star. Well done, keep it up! |
| kn | {child} ಈ ವಾರ ಶಾಲೆಗೆ ಬಂದು ಒಂದು ನಕ್ಷತ್ರ ಗಳಿಸಿದ್ದಾರೆ. ಶಾಬಾಶ್, ಹೀಗೇ ಮುಂದುವರಿಸಿ! |
| te | {child} ఈ వారం బడికి వచ్చి ఒక నక్షత్రం సంపాదించారు. శభాష్, ఇలాగే కొనసాగించండి! |
| ta | {child} இந்த வாரம் பள்ளிக்கு வந்து ஒரு நட்சத்திரம் பெற்றார். பாராட்டுகள், இப்படியே தொடருங்கள்! |
| hi | इस हफ़्ते स्कूल आने के लिए {child} को एक सितारा मिला है। शाबाश, ऐसे ही आते रहें! |

"Came to school this week" stays true for a child away one day. The text never says "every day".

## 4. Data (one migration, `StarMessages`)

| Change | Detail |
|---|---|
| `MessageTemplates.Kind` | `text not null default 'evening'`. Existing rows stay `evening`. The unique index becomes `(ClassId, Kind, Language)`. Every `EveningStore` query filters `Kind = evening`, so the evening flow is unchanged (its tests prove it) |
| `StarMessages` (new) | `Id`, `ClassId` (FK, cascade), `ChildId` (FK, **cascade**, FR-15), `WeekOf` (Monday), `TemplateId` (FK), `Language`, `Token` (128-bit base64url, **unique**), `SentAt`, `ExpiresAt`, `SentByTeacherId` (FK → Teachers). **Unique `(ChildId, WeekOf)`**; index `(ClassId, WeekOf)`. No child name stored |

Also in this PR:
- `docs/database.md`: both changes.
- Demo Reset: removes the class's `StarMessages` (cascades with the children) and treats the `star` template exactly as it treats the `evening` one.

## 5. API

### Shared with T-63: the school calendar

- `SimulatedSatsSource.IsSchoolDay` (Sunday, second Saturday, `Attendance:Sats:Holidays`) moves to a small `SchoolCalendar` in `Features/Attendance/`, used by the source and by `StarRules`.
- A week is **finished** when its last school day is before today (IST), or is today and today's register is in.
- T-63 uses the same rule (its plan says so), so T-64 adds no second definition.

### Templates: `kind` on the existing endpoints

- `GET /api/classes/{id}/message-template?kind=star` and `POST /api/classes/{id}/message-template/approve` with `{ clientRequestId, version, kind }`.
- `kind` defaults to `evening`, so existing callers and tests are untouched.
- Same rules: a stale version gets 409, idempotent by `clientRequestId`, per-class-and-kind advisory lock.

### New slice `src/api/SlateAi.Api/Features/StarMessages/`

| File | What |
|---|---|
| `StarMessagePlanner.cs` | Pure. Inputs: `WeekStars` from `StarRules` (finished week only), children (consent, stopped, language), already-sent child IDs, templates by language. Output: per-child outcome (`EveningOutcome` values reused: `willSend`, `sent`, `noConsent`, `noPhone`, `stopped`, `noWording`), and `Blocked`: `weekNotFinished`, `tooLate`, `templateNotApproved` |
| `IStarMessageStore.cs` + `StarMessageStore.cs` | Sent rows for a class and week; add rows (`ON CONFLICT (ChildId, WeekOf) DO NOTHING`) under advisory lock `stars:{classId}:{weekOf}`; `ProcessedRequests` endpoint `star-send` |
| `StarMessageEndpoints.cs` | The two endpoints below, `RequireClassOwner`, `TypedResults`, `WithTags("Stars")` + `WithSummary` |

```
GET  /api/classes/{id}/star-messages/{weekOf}
     → { weekOf, blocked, templateApproved, templateVersion, families, sent, sentAt, languages: [{ language, count }],
         children: [{ childId, outcome }] }   // only children who earned the star

POST /api/classes/{id}/star-messages/send
     { clientRequestId, weekOf, approveTemplateVersion? }
     → 200 same body as GET
     → 409 { blocked } when weekNotFinished / tooLate / templateNotApproved (and no approveTemplateVersion)
     → 409 when approveTemplateVersion is stale
```

- `approveTemplateVersion` lets "Approve and send" be one tap. The approval and the send happen in one transaction, so nothing is ever sent from a draft.
- A replayed `clientRequestId` returns the current state and sends nothing new.
- A stopped child is skipped in the batch and shown as `stopped`, as the evening batch does. The parent stop endpoints already refuse future sends (D-19).

### Parent page

- `ParentStore` resolves a token from `StarMessages` too, after `CatchUpAssignments` and `EveningMessages`.
- `GET /api/p/{token}` returns `{ kind: "star", language, childName, teacherName, text, greeting, stopped }`. `text` is rendered from the template with `{child}` filled in at read time; the name is never stored.
- 410 after `ExpiresAt`. Stop and restart work from a star token as from an evening one (D-76 (4)).
- `/greeting` works for star tokens.

### Demo

- `FamilyMessage.Star` kind in `DemoStore.FamilyMessagesAsync`.
- No change to "Send messages now" (#109). The star send is the teacher's tap on Week.

### Guardrails

| Guardrail | How T-64 keeps it |
|---|---|
| 1 Names | No external call. The name is filled in only when the family's page renders |
| 2 LLM output | No LLM |
| **3 Approval** | Two keys: an **approved `star` template at its current version** and **her Send tap for that finished week**. Enforced in the API, covered by endpoint tests and one Db test. **`CLAUDE.md` guardrail 3 gains a third case** in this PR: "or a star message (FR-25, D-82) built from an Approved `star` `MessageTemplate`, sent by the teacher for a finished week to children `StarRules` gave the weekly star" |
| 4 Numbers in code | Who qualifies comes from `StarRules`; the message has no numbers |
| 5 / 6 | Not touched; no eval |

## 6. Web

| File | Change |
|---|---|
| `src/web/src/features/week/useStarMessages.ts` (new) | TanStack Query for `GET star-messages/{weekOf}`; mutation for send with a fresh `clientRequestId` per tap (kept across retries of the same tap); invalidates the week and star queries |
| `src/web/src/features/week/StarSendSheet.tsx` (new) | The dialog in §3 |
| `src/web/src/features/week/StarsCard.tsx` | Send button, "Last week" row, sent line, offline caption |
| `src/web/src/features/parent/StarView.tsx` (new) + `ParentScreen.tsx` | `kind: "star"` → P5. Reuses `Celebration` (moved out of `DoneStep` or into `ChalkStar`, T-63), `VoiceNote` for the greeting, the stop link |
| `src/web/src/features/parent/types.ts` | `ParentStar` type |
| `src/web/src/features/demo/DemoScreen.tsx` + `types.ts` | Star bubble |
| `src/web/src/i18n/locales/{en,kn,te,ta,hi}.json` | Keys from §3 |

## 7. Tests

**xUnit, pure (`StarMessagePlannerTests`):**
- Only children `StarRules` starred are considered.
- Each outcome: consent yes, no, nophone, stopped, no wording, already sent.
- Blocked: current week (`weekNotFinished`), two weeks back after the next week finished (`tooLate`), template draft (`templateNotApproved`).
- A holiday Saturday: the week finishes after Friday's register.
- Deterministic.

**xUnit, endpoints (`FakeApi.Create` + fakes, fake `ILlmClient` asserting no calls):**
- Send refused (409) without an approved template; "Approve and send" with the current version sends; with a stale version it's 409 and nothing is sent.
- Refused for an unfinished week and for a too-late week.
- Sends only to starred, consenting, not-stopped families with wording; a stopped child shows `stopped` and gets no row.
- Same `clientRequestId` replayed: no new rows. New id after a send: no new rows (unique child + week).
- Another teacher's class: same refusal as the other class endpoints.
- `GET /api/p/{token}` for a star token: `kind: "star"`, text with the name filled, greeting when recorded; 410 after expiry; stop from a star token stops the child.
- Template endpoints: `kind=star` approves only star rows; existing evening tests unchanged.
- `DemoStore` family messages include the star kind.

**Db suite: approved by Rahul, 26 Oct.** One new file, `StarMessageDbTests.cs`, plus one assertion added to the existing child-deletion test:
- The migration applies on the seeded database; existing templates read as `evening`.
- Two concurrent sends for the same week give one row per child.
- On real Postgres, a send with a draft `star` template writes nothing (guardrail 3).
- `ChildDeleteDbTests`: deleting a child removes their `StarMessages`.

**Vitest:**
- `StarSendSheet`: first-time approve tick gates the button; send; sent; failure and Try again; Cancel is the default and Esc closes.
- `StarsCard`: button, last-week row, sent line, offline disabled, no families caption.
- `StarView`: text, "From", greeting, stop link, no numbers.
- `DemoScreen`: star bubble opens the link.
- `locales.test.ts`.

**Not added:** no new Playwright test (CLAUDE.md, 22 Oct); the existing e2e specs must pass.

**Run:** `./scripts/verify.sh` and `dotnet test --filter Category=Db`. Screenshots at 360 px (light and dark): the Stars card send states, the sheet, P5 and the demo bubble. Run axe on the sheet and P5.

## 8. Docs to update in the same PR

- `CLAUDE.md`: guardrail 3 third case (§5). Also "four locale files" becomes five.
- `docs/spec.md`:
  - FR-25 and D-82 (added with this plan).
  - §6: the two endpoints, `kind` on the template endpoints, `kind: "star"` on `/api/p/{token}`.
- `docs/database.md`: `MessageTemplates.Kind` and `StarMessages`.
- `docs/design/`: `T6-week.md` (send states and sheet), a short P5 section in `P4-done.md` or a new `P5-star.md`, and the chat bubble in `tokens.md` "chat".
- `docs/plan.md` §3: a row. `docs/tasks.md`: T-64 to *Done*. `docs/status.md`.

## 9. Order of work

1. `SchoolCalendar` extracted, with the source's tests still green.
2. Migration (`Kind`, `StarMessages`) + `EveningStore` filter + existing evening tests green.
3. `star-message.json` + template `kind` on the endpoints + tests.
4. `StarMessagePlanner` + tests; store + endpoints + tests.
5. Parent `kind: "star"` + tests.
6. Db tests.
7. Web: hook, sheet, card states, P5, demo bubble, strings, Vitest.
8. Docs (§8), `CLAUDE.md`, verify, Db suite, screenshots and axe, PR (`feat(web,api): well done star message to families (T-64)`).

## 10. Acceptance

- [ ] Only families of children with the T-63 weekly star for a finished week, consent yes, not stopped, with wording, get it.
- [ ] Nothing sends without an approved `star` template at its current version **and** her Send tap; enforced in the API; endpoint and Db tests.
- [ ] Never twice per child per week (replay, second tap, concurrent sends).
- [ ] No AI or speech call; no numbers, other children or absence in the message.
- [ ] Evening message behaviour and tests unchanged.
- [ ] P5 with greeting and stop link; expired and stopped pages work from a star token.
- [ ] Demo: Week → Send → star bubble on the family phone.
- [ ] Every new string in all five locale files; 360 px screenshots and axe 0 on the sheet and P5.
- [ ] `./scripts/verify.sh` green; Db suite green.
