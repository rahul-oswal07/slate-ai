# Status and handoff

Read this first in any new session, then `CLAUDE.md`, `docs/intent.md`, `docs/spec.md`, `docs/plan.md`, `docs/tasks.md`.
Update it at the end of every working session (what changed, what's next, blockers).

## Current state — Wed 21 Oct 2026 (Day 3)

- Repo: https://github.com/rahul-oswal07/slate-ai (`main`, public). Staging live: https://slateai-staging.azurewebsites.net (prod app exists; first `v*` tag deploys it).
- Done: T-01–T-04 (skeleton, CI, deploy), **T-05** curriculum graph + `grade3-maths.json` (#68, #74), **T-06** attendance API + ledger query (#57), **T-07 PR 1** AI plumbing (#72). Ledger also computes absence spells and weekly return speed.
- **Building screen by screen from Day 3** (order in `plan.md` §3). First slice: **T-47** teacher sign-in (phone once, then PIN) + first-run Welcome, class endpoints locked to the signed-in teacher's class (D-40), branch `feat/T-47-pin-login`.
- Testing approach changed (#58): default tests use fakes and need no Docker; DB access sits behind per-feature stores (`IAttendanceStore`, `ILedgerStore`). A small Postgres suite (`Category=Db`) runs in CI (#59). See CLAUDE.md "Testing expectations".
- Education mentor call held 19 Oct: notes and proposed decisions D-20–D-25 in `docs/mentor-feedback-2026-10-19.md` (#55, #56).
- Monika's PRs #49–#53 merged.
- Submission target: **Sat 31 Oct, 18:00 IST** (hard deadline 1 Nov, 23:59).

## Open items

- [x] Rahul: merge #59 (Db test suite)
- [x] Rahul: Anthropic + Sarvam API keys into user-secrets (Anthropic key starts `sk-ant-usr-`, not `sk-ant-api03-`: confirm it works on the first live call; rotate both, they appeared in a session transcript)
- [x] Rahul: accept D-05, D-11, D-12 in `spec.md` decision log
- [ ] Rahul: confirm D-18 / D-19 before T-11 / T-15
- [ ] Rahul: technical mentor call moved from 21 Oct to next week (brief ready in `docs/mentor-technical-2026-10-21.md`; fill in part C after the call)
- [x] Monika: T-22 competency list drafted in `docs/design/curriculum.md` (16 competencies, textbook page each). D-01 revised to 3-digit. Open: † Kannada names to confirm with a teacher; NIPUN codes have no source yet
- [x] Rahul: D-27 attendance source, decided as D-69 (21 Oct; times from D-45): fetched when needed, repeatable simulation + demo overrides, provenance columns on `Attendance`, teacher marking wins over late SATS. Was: Put attendance behind a source interface (`DemoSource` simulated SATS feed now; file import / state API later) feeding the existing T-06 register endpoint; store source + time per record; teacher corrections overlay, never written back. Update `plan.md` / `tasks.md` (T-12: grid becomes corrections + fallback only) and the demo label "simulated"
- [ ] Monika: T-20 interviews: ask teachers how they mark student attendance today (paper register, SATS app, other). If SATS does not hold daily Grade 3 attendance, revisit D-27
- [x] Rahul: built `data/curriculum/grade3-maths.json` from T-22 (21 Oct). Note: 2-digit → 3-digit changes number ranges for generated sums, checks and buddy cards (≤ 999, no borrowing across zero); test fixtures use `M3.PV.TENS`, which is now `M2.PV.TENS`
- [ ] Rahul: D-28–D-31 from the UX review: approval API accepts only Ready + ticked for that script version, Withdraw until send (D-28); `MessageTemplate` no longer names the buddy to parents, drop or keep `{buddy}` teacher-side only (D-29); STOP reply + confirmation message with restart link (D-30); mic tap-to-start/stop (D-31)
- [x] Monika: T-21 replaced by the clickable prototype v2 + UX review (outside the repo, `Documents/tappida-paatha-prototype`); D-03, D-07, D-08, D-09 accepted as prototyped
- [x] Monika: T-21 delivered as the clickable prototype + design cards (#70, #73)
- [ ] Monika: T-20 recruit teachers + parents (offline, by Monika)
- [ ] Rahul: D-33–D-39 in the build: lesson confirm accepts 1–3 ticked competency IDs (D-33); mic-blocked + sending states and outbox (D-34); tag aliases in tokens.css (D-35); no evening-message listen tracking (D-36); Withdraw until the 6 pm job, then Sent (D-37); Sent / Generation failed package states (D-38); `Child` consent field Yes/No/No phone + date, T8 Families screen, send endpoints 409 unless Yes (D-39, FR-15)
- [x] Monika: decided D-20–D-26 (mentor review) and accepted `intent.md` (PR docs/mentor-decisions)
- [x] Rahul: confirmed D-21 (20 Oct): approved `MessageTemplate` + code-filled slots; guardrail 3 in CLAUDE.md reworded
- [x] Rahul: `plan.md` / `tasks.md` for FR-20–22 (new T-44 evening message, T-45 reason + A7, T-46 return-speed card) and the D-26 cuts (T-40 stretch; drop T-41–T-43); A7 reason classifier (prompt + evals in `quality.md`); data model (`AbsenceReason`, `HomeworkReturned`, textbook page refs)
- [x] Mentor's 6 interview questions added to `user-testing.md`

## Next session (Day 4, Thu 22 Oct)

- Merge T1 attendance slice (`feat/T-12-today`, #81): attendance arrives at 12:00 with a 2:00 pm retry (D-45, D-69); demo SATS controls on by default (`Demo__Enabled`).
- Monika: copy for two lines marked [DECIDE: Monika] in D-69: "SATS came in at {time}. Your marking is kept." and "No school today in SATS".
- Then T2 Lesson confirm = T-07 PR 2 (capture endpoints, A1 prompt + validator returning 1–3 lessons per D-33, name scrubber, 10 evals).
- Monika: D-40 login screen UX/copy review; te/ta strings for login and Welcome are drafts.
- Use plan mode first; one branch per task.

## Local dev

- `./scripts/run-local.sh` runs Postgres + API (:5080) + web (:5173); `--container` runs the image on :8080.
- API keys: `dotnet user-secrets set "Anthropic:ApiKey" …` / `"Sarvam:ApiKey"` (see README). Stored locally (19 Oct); nothing reads them until T-07. Azure: App Service settings `Anthropic__ApiKey`, `Sarvam__ApiKey`.

## Open questions

- Real teachers reachable this week? (schools reportedly on Dasara break until ~21 Oct)
- Open-source licence (D-15, MIT proposed)

## Log

| Date | Who | What |
|---|---|---|
| 18 Oct | Rahul + Claude | Planning artifacts, repo setup, first commit and push |
| 18 Oct | Rahul + Claude | T-01: CODEOWNERS, repo public, issues per P0/P1 task, Project board, branch protection |
| 18 Oct | Rahul + Claude | T-02: API skeleton, Initial migration (Teacher/Class/Child), idempotent + concurrency-safe demo seed, `/health`, 5 integration tests (Testcontainers). xunit.v3 pinned to 3.x to keep VSTest `--filter` syntax |
| 18 Oct | Rahul + Claude | T-03: Vite/React/TS PWA skeleton, i18n with draft kn/te/ta strings (need Monika's review), placeholder tokens, API serves SPA, Vitest + Playwright/axe. D-10 accepted |
| 18 Oct | Rahul + Claude | T-04: Dockerfile (chiseled, non-root), compose app service, CI container smoke, `provision.sh` (idempotent) run on personal Free Trial sub, OIDC deploy workflow main→staging, v*→prod. D-13 accepted |
| 18 Oct | Rahul + Claude | Started local Postgres, API (fake AI), and Vite dev server; `/health` and the web app respond successfully |
| 18 Oct | Rahul + Claude | First deploy to staging green (image sha-6192a40). Fixed OIDC subject (GitHub immutable-ID format). GHCR package already public |
| 18 Oct | Rahul + Claude | API docs: built-in OpenAPI document + Swagger UI at `/swagger` (dev + staging; off on prod via `OpenApi:Enabled`) |
| 18 Oct | Rahul + Claude | Deploy smoke test now waits for `/health` to report the new build (`version`) on 3 consecutive polls; fixes false failures during App Service container swaps |
| 18 Oct | Monika + Claude | T-00: accepted gate decisions D-01 (Grade 3 maths unit), D-02 (Kannada UI; Telugu+Kannada tested, Tamil spot-checked), D-16 (3 teachers + 2 parents/proxies incl. Telugu). Unblocks T-22, T-20, T-05, T-07 |
| 18 Oct | Monika + Claude | Compared the external Build Kit (00–06) with the repo; adopted its design tokens, design-card template, copy rules, testing kit and roadmap; kept our stack and CLAUDE.md. PRs: #50 AAA tokens + system dark theme (T-23, D-04/D-17 accepted; fixed 1.5:1 border), #51 design cards + screen states + copy rules (T-21), #52 testing kit + Now/Next/Later roadmap (T-27), #53 spoken AI disclosure + parent stop (D-18/D-19 proposed). Merge order: #49 → #50 → #53; #51, #52 independent
| 18 Oct | Rahul + Claude | `scripts/run-local.sh` (one-command local run); `UserSecretsId` + README secrets guidance (user-secrets locally, App Service settings on Azure). Next session: review Monika's PRs #49–#53, then T-06 |
| 19 Oct | Rahul + Claude | Mentor session notes committed (#55); fixed real teacher name → `{teacher}` placeholder and flagged D-21 vs FR-08 conflict (#56). Reviewed the feedback: return speed folded into T-06; textbook-page homework and "bring it back" hook proposed for T-05/T-09/T-16 |
| 19 Oct | Rahul + Claude | T-06 (#57): `POST attendance` (full register per date, idempotent by `clientRequestId`, corrections replace, IST future-date check), `GET ledger` (missed lessons, absence spells, return speed), `Attendance`/`Lesson`/`ProcessedRequest` + migration. Fixed startup migration race with an advisory lock |
| 19 Oct | Rahul + Claude | Testing: no database in default tests, stores + fakes, CLAUDE.md updated (#58); small Postgres suite `Category=Db` run in CI (#59) |
| 20 Oct | Monika + Claude | Mentor decisions: D-20 return speed as primary measure, D-21 pre-approved evening template (awaiting Rahul), D-22 textbook homework, D-23 optional reason, D-24 return-speed card, D-25 roadmap only, D-26 scope cuts. New FR-20–22 (renumbered; FR-19 was taken). `intent.md` accepted and revised; narrative, roadmap north star and user-testing script updated; mentor session logged as F-M1–F-M12 |
| 20 Oct | Rahul + Claude | Docs follow-through for D-20–D-26: D-21 confirmed (guardrail 3 now covers approved evening templates), T-44–T-46 added, T-41–T-43 dropped, A7 in spec/quality, new tables in spec/database.md |
| 20 Oct | Rahul + Claude | T-05 code on test data: `Curriculum/` loader (validates IDs, prerequisites, cycles, textbook pages; fails startup on a bad file, warns when empty), `CurriculumGraph` (ancestors, nearest-first candidate set), 29 tests; Dockerfile now copies `data/curriculum/`. Real JSON waits for T-22 |
| 20 Oct | Monika + Claude | T-22: read Grade 3 maths textbook 2026–27 (Part 1 bridge + Ch 2–4, Part 2 Ch 5). Book is 3-digit only (sums ≤ 999), so D-01 revised: 3-digit chain, 2-digit skills as Grade 2 prerequisites. 16 competencies with Kannada names, prerequisites, homework page and examples in `docs/design/curriculum.md`; book errors and pages to avoid listed. Textbook PDFs kept out of the repo (public; "not to be republished") |
| 20 Oct | Monika + Claude | Clickable end-to-end prototype of teacher + parent flows (outside the repo, `Documents/tappida-paatha-prototype`). D-27: attendance comes from the school system (simulated SATS feed in the MVP + manual fallback; real SATS/VSK feed is roadmap, no public API found). FR-01, NFR-01, T1 screen, intent and narrative updated |
| 20 Oct | Monika + Claude | UX review of all prototype screens (heuristics, UX laws, WCAG 2.2 + axe): 93 findings, P0s fixed in prototype v2 (axe 0 violations on 71 states). Accepted as prototyped: D-03, D-07 (pictures are the answer), D-08, D-09; new D-28 bulk approve only after review + withdraw, D-29 buddy not named to parents, D-30 stop everywhere + STOP reply, D-31 tap-to-start mic. FR-02/08/19/20 and screens table updated |
| 20 Oct | Monika + Claude | H2 design handoff: `docs/design/journey.md` + cards T0, T1, T2, T4 (states, kn/en copy, tokens, acceptance checklists traced to UX-review finding IDs), 360 px screenshots, clickable prototype and UX review in `docs/design/`. Spec follow-ups for Rahul listed in journey.md |
| 20 Oct | Monika + Claude | H3 design handoff: cards T3 approve, T5 back today, T6 week, P0 messages and reason, P1–P4 parent steps, with screenshots |
| 21 Oct | Rahul + Claude | T-07 PR 1: `Ai/` (`ILlmClient`, `ClaudeLlmClient` on the Anthropic SDK with structured outputs + cached system prompt, `LoggedLlmClient`, `PromptLibrary`, `FakeLlmClient`), `Speech/` (Sarvam `saaras:v4` STT, logged), `AiCalls` table + migration, 32 tests over stub HTTP. Found and fixed: stop reason must be read with `Raw()` |
| 21 Oct | Rahul + Claude | Technical mentor brief: pre-read, 40-min agenda, 9 prioritised questions, notes template (`docs/mentor-technical-2026-10-21.md`) |
| 21 Oct | Rahul + Claude | T1 Today, attendance slice (T-12 part A, D-69): `IAttendanceSource` + `SimulatedSatsSource` (repeatable per class/date, ~8% absent, Ravi Tue–Thu, no school on Sundays/2nd Saturdays/holidays), fetched when Today asks after 12:00 IST (D-45), provenance columns + `RegisterMerge` (her marking wins over late SATS), `GET attendance/{date}`, demo `POST /api/demo/sats-feed`. Web: Today screen, correct/mark grid, Dexie outbox + sync badge + offline snapshot, bottom nav with placeholder tabs. Mic disabled until T-07 PR 2. Db test caught IST offsets reaching Npgsql (now stored as UTC) |
| 21 Oct | Rahul + Claude | Switched to building screen by screen. T-47: phone + PIN sign-in (no registration, D-40), 30-day cookie session with keys in Postgres, lockout after 5 wrong PINs, `/api/me` + first-run Welcome gated on `Teacher.ConsentAt`, attendance and ledger endpoints limited to the teacher's own class. Web: TanStack Query, login + Welcome screens, offline open from the cached teacher. Mentor call moved to next week |
| 21 Oct | Rahul + Claude | Brought Monika's T-24 cards to `main` (#71 had merged into an already-merged branch). T-05 done: `grade3-maths.json` (16 competencies, every one with a textbook page), CI-validated |
| 21 Oct | Monika + Claude | D-32: English is the default locale for development and design references; prototype opens in English. Screenshots kept as captured (Kannada/Telugu with English captions) |
| 21 Oct | Monika + Claude | Closed the open design gaps (D-33–D-39): multi-lesson checklist confirm, mic blocked + sending, tag colours (tokens.md), no evening-message listens, Withdraw until 6 pm, Sent / Generation failed / offline states, parent opt-in on new T8 Families screen. Prototype checked against every spec §3.1 state (83 states, axe 0 violations); FR-03/08/15 and §3.1 updated; cards updated, T8 card added |
| 22 Oct | Monika + Claude | Decisions D-41–D-54 (setup, phone/consent/language on Family details, voice consent version, 12:00 attendance sync, no "Tomorrow", no names in computer voice, Settings, greeting per language, T1 answers). Spec FR-01/09/15/20 + data model (`TeacherGreeting`, `Child` consent/phone/language) updated. Prototype + cards follow in the next design PR |
| 22 Oct | Monika + Claude | Pre-empted screen-build questions: D-55–D-68 (one note a day, summaryEn in the same A1 call, bulk approve = tick, audio labels, buddy fallback, nudge threshold 5, empty week, reopened link → Done, reason = pictures only (A7 voice cut), no STOP in demo, Help page, phone required for Yes, attendance missing after 2 pm, blanket loading/offline/error/409 rule). Prototype + cards updated |
