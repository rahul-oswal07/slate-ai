# T3 Approve (list + catch-up detail)

Spec: FR-06, FR-07, FR-08, FR-09, NFR-08 · Tier: MVP · Task: T-14 (pipeline: T-08, T-09, T-10, T-11) · Prototype: prototype/index.html (step 7; states via the chips under each phone)

Decisions: D-09 (Kannada summary + audio, tick required, cleared by any regenerate), D-18 (spoken AI disclosure), D-28 (bulk approve only for opened and ticked; Withdraw until the send). Review: B-01, B-02, B-03, B-10, B-11, B-12, B-13, B-14, B-15, B-28, B-29.

## 1. User voice
- Says: "Show me what goes to the families this week. I'll check it once and send it. I can't read Telugu, so tell me in Kannada what it says."
- Unsaid: "If the computer says something wrong in my name, the parents will blame me." "I don't have time to open every one." "What if I tap Approve by mistake?"

## 2. Who does what
- Human (teacher): opens each catch-up, plays the audio, reads the Kannada summary, ticks "I've checked the Kannada summary", taps Approve (one catch-up) or "Approve the N you checked" (several). Can ask for Simpler or Shorter, and can Withdraw until the send job starts.
- AI · Mode: augmentation (the teacher decides).
  - A2 compression (Claude): chooses at most 2 targets from the code-computed candidate set; writes the reason for each kept and dropped lesson ("Why this").
  - A3 script writer (Claude): script, 3 Kannada key words, activity, homework page, picture check, Kannada gloss (the "Kannada summary"), buddy content. Placeholders `{child}`, `{teacher}` only.
  - A4 verifier: 6 code checks plus a Haiku check; one auto-retry with the verifier's reasons; a second failure becomes "Needs review".
  - Sarvam TTS: home-language audio. Final file is greeting (teacher's own recording) → spoken disclosure → script.
- Code (no AI): candidate set, validation of A2 targets (fallback to the nearest-prerequisite rule), grouping children into one catch-up per (targets, language), children count, duration shown ("1:24"), the failure reason with numbers ("104 seconds, limit 90"), the "N you checked" count, the pending badge count, status transitions, script-version binding, the 6 pm send schedule, placeholder filling for display.

## 3. Always / never
- Always: one approval per catch-up, not per child. Show which children, which lessons, the duration and why each lesson was kept or dropped. Count the bulk button from the status shown on screen. Bind each approval to the script version she ticked.
- Always: the "AI-assisted" label on the detail screen (NFR-08).
- Never: send anything to a parent unless `CatchUpPackage.Status == Approved` for the current script version (guardrail 3; enforced in the API, not only the UI).
- Never: let a tick survive a regenerate (B-03). Never count a Generating or Needs review catch-up in the bulk button (B-02).
- Never: send child, parent or teacher names to Claude or Sarvam. The summary says "the child", never one child's name (B-12, NFR-05).
- Uncertainty: verifier fails twice → status Needs review. The tick is hidden, Approve is not offered, the reason is shown in numbers next to the action, and "Make it shorter" is the primary button (B-10). A2 output outside the candidate set → rule fallback, never shown as AI choice.
- If it goes wrong: a wrong script reaches up to 3 families in the teacher's name. Limits: verifier, required tick, version-bound approval, Withdraw until 6 pm, and the API 409.

## 4. States
| State | What the user sees | Copy (en) |
|---|---|---|
| List · default | Intro line; one card per catch-up: language · children count, names, lessons · duration, status tag; "Checked" mark when ticked; bulk button disabled at 0 with hint. | "One approval per catch-up, not per child. Nothing is sent until you approve." · "Approve the 0 you checked" · "Open each one and tick the Kannada summary first." |
| List · generating | The generating card shows the Generating tag and is disabled; not counted in the button. | "Generating…" |
| List · offline (approval queued) | Warning banner on top; approved cards show the "Saved · will send" tag. | "No network. Your approval is saved on the phone and will send when you have network." |
| List · some checked | Button counts only Ready + ticked cards; hint switches to needs-review advice | "Approve the 2 you checked" · "Open “Needs review” ones one at a time." |
| List · all approved | Success banner; bulk button and hint hidden (B-29). No PNG exported | "All {packages} approved. {families} families get them after 6 pm. (counts from code)" (counts from code; see Open items) |
| List · empty | Not designed. No catch-ups this week | Open item |
| Detail · ready | Status tag + AI label; children; player with segments; Kannada summary panel with key words; "Why this"; picture question with the correct option marked; tick; Approve (disabled until ticked); Simpler / Shorter. | "I’ve checked the Kannada summary" · "Approve" · "Tick the summary box to approve." |
| Detail · needs review (verifier failed) | Error banner with the reason in numbers; no tick; "Make it shorter" primary; reason next to it. | "Didn’t pass the check: audio is 104 seconds, limit is 90." · "Can’t approve: the audio is too long." |
| Detail · regenerating | Status banner; tick, Approve, Simpler, Shorter disabled. No PNG exported | "Writing a new version… Check it again when it’s ready." |
| Detail · approved | Success banner + "Withdraw (until 6 pm)". No PNG exported | "Approved. Goes to the families after 6 pm." |
| Sent | Not yet designed (B-14). Needs the real send job | — |
| Generation failed | Not yet designed (B-14). A3/TTS error after retry | — |
| Loading | Not designed. Use skeleton cards; no spinner-only screen | — |
| Error (approve rejected, 409) | Not designed. API refused because status or version changed | Open item |
| Permission denied | none: no mic or camera on this screen | — |

## 5. Copy
| Key | kn | te | ta | en | Reviewed by |
|---|---|---|---|---|---|
| `approve.title` | ಈ ವಾರದ ಪುನರಾವರ್ತನೆಗಳು | — | needs translation | This week’s catch-ups | needs review |
| `approve.intro` | ಮಗುವಿಗೊಂದಲ್ಲ, ಪ್ರತಿ ಪುನರಾವರ್ತನೆಗೆ ಒಂದು ಅನುಮೋದನೆ. ನೀವು ಒಪ್ಪುವವರೆಗೆ ಏನೂ ಕಳುಹಿಸುವುದಿಲ್ಲ. | — | needs translation | One approval per catch-up, not per child. Nothing is sent until you approve. | needs review |
| `approve.offline` | ನೆಟ್‌ವರ್ಕ್ ಇಲ್ಲ. ನಿಮ್ಮ ಅನುಮೋದನೆ ಫೋನಿನಲ್ಲಿ ಉಳಿಯುತ್ತದೆ, ನೆಟ್‌ವರ್ಕ್ ಬಂದಾಗ ಕಳುಹಿಸುತ್ತದೆ. | — | needs translation | No network. Your approval is saved on the phone and will send when you have network. | needs review |
| `approve.allDone` | {n} ಕೂ ಅನುಮೋದಿಸಿದ್ದೀರಿ. {families} ಕುಟುಂಬಗಳಿಗೆ ಸಂಜೆ 6 ರ ನಂತರ ಹೋಗುತ್ತದೆ. | — | needs translation | All {n} approved. {families} families get them after 6 pm. | needs review |
| `approve.card.heading` | {language} · {count} ಮಕ್ಕಳು | — | needs translation | {language} · {count} children (1: "child") | needs review |
| `approve.card.checked` | ಪರಿಶೀಲಿಸಿದ್ದೀರಿ | — | needs translation | Checked | needs review |
| `approve.card.goesAt` | ಸಂಜೆ 6 ರ ನಂತರ ಹೋಗುತ್ತದೆ | — | needs translation | Goes after 6 pm | needs review |
| `approve.bulk.button` | ಪರಿಶೀಲಿಸಿದ {n} ಒಪ್ಪಿ | — | needs translation | Approve the {n} you checked | needs review |
| `approve.bulk.hintTick` | ಮೊದಲು ಪ್ರತಿಯೊಂದನ್ನು ತೆರೆದು ಕನ್ನಡ ಸಾರಾಂಶಕ್ಕೆ ಗುರುತು ಹಾಕಿ. | — | needs translation | Open each one and tick the Kannada summary first. | needs review |
| `approve.bulk.hintReview` | “ಪರಿಶೀಲನೆ ಬೇಕು” ಇರುವುದನ್ನು ಒಂದೊಂದಾಗಿ ನೋಡಿ. | — | needs translation | Open “Needs review” ones one at a time. | needs review |
| `status.ready` / `.review` / `.approved` / `.generating` / `.queued` | ಸಿದ್ಧವಾಗಿದೆ / ಪರಿಶೀಲನೆ ಬೇಕು / ಅನುಮೋದಿಸಿದ್ದೀರಿ / ಸಿದ್ಧಪಡಿಸುತ್ತಿದೆ… / ಉಳಿಸಲಾಗಿದೆ · ನಂತರ ಕಳುಹಿಸುತ್ತದೆ | — | needs translation | Ready / Needs review / Approved / Generating… / Saved · will send | needs review |
| `pkg.title` | ಪುನರಾವರ್ತನೆ ಪರಿಶೀಲಿಸಿ | — | needs translation | Review catch-up | needs review |
| `pkg.children` | {language} · ಮಕ್ಕಳು | — | needs translation | {language} · children | needs review |
| `pkg.segments` | ನಿಮ್ಮ ಧ್ವನಿ · ಸೂಚನೆ · ಪಾಠ | — | needs translation | Your voice · Notice · Lesson | needs review |
| `pkg.summary.title` | ಕನ್ನಡ ಸಾರಾಂಶ | — | needs translation | Kannada summary | needs review |
| `pkg.keywords` | ಕನ್ನಡ ಪದಗಳು | — | needs translation | Kannada words | needs review |
| `pkg.why.title` | ಏಕೆ ಇದು | — | needs translation | Why this | needs review |
| `pkg.check.title` | ಚಿತ್ರ ಪ್ರಶ್ನೆ · ಪರೀಕ್ಷಿಸುವುದು: {lesson} | — | needs translation | Picture question · tests: {lesson} | needs review |
| `pkg.review.banner` | ಪರಿಶೀಲನೆಯಲ್ಲಿ ತಪ್ಪಿತು: ಆಡಿಯೋ {seconds} ಸೆಕೆಂಡು, ಮಿತಿ {limit}. | — | needs translation | Didn’t pass the check: audio is {seconds} seconds, limit is {limit}. | needs review |
| `pkg.review.cannotApprove` | ಒಪ್ಪಲು ಆಗದು: ಆಡಿಯೋ ತುಂಬಾ ಉದ್ದ. | — | needs translation | Can’t approve: the audio is too long. | needs review |
| `pkg.regen.shorterPrimary` | ಚಿಕ್ಕದಾಗಿ ಬರೆಯಿರಿ | — | needs translation | Make it shorter | needs review |
| `pkg.regen.banner` | ಹೊಸ ಆವೃತ್ತಿ ಸಿದ್ಧಪಡಿಸುತ್ತಿದೆ… ಸಿದ್ಧವಾದಾಗ ಮತ್ತೆ ಪರಿಶೀಲಿಸಿ. | — | needs translation | Writing a new version… Check it again when it’s ready. | needs review |
| `pkg.tick` | ಕನ್ನಡ ಸಾರಾಂಶ ಪರಿಶೀಲಿಸಿದ್ದೇನೆ | — | needs translation | I’ve checked the Kannada summary | needs review |
| `pkg.approve` | ಒಪ್ಪಿ | — | needs translation | Approve | needs review |
| `pkg.approve.hint` | ಒಪ್ಪಲು ಮೊದಲು ಸಾರಾಂಶಕ್ಕೆ ಗುರುತು ಹಾಕಿ. | — | needs translation | Tick the summary box to approve. | needs review |
| `pkg.simpler` / `pkg.shorter` | ಸರಳವಾಗಿ / ಚಿಕ್ಕದಾಗಿ | — | needs translation | Simpler / Shorter | needs review |
| `pkg.approved.banner` | ಅನುಮೋದಿಸಿದ್ದೀರಿ. ಸಂಜೆ 6 ರ ನಂತರ ಕುಟುಂಬಗಳಿಗೆ ಹೋಗುತ್ತದೆ. | — | needs translation | Approved. Goes to the families after 6 pm. | needs review |
| `pkg.withdraw` | ಹಿಂಪಡೆಯಿರಿ (6 ಗಂಟೆಯವರೆಗೆ) | — | needs translation | Withdraw (until 6 pm) | needs review |
| `ai.label` | AI ನೆರವು | — | needs translation | AI-assisted | needs review |
| `aria.pkg.play` / `aria.pkg.pause` | ಕೇಳಿ / ನಿಲ್ಲಿಸಿ | — | needs translation | Play / Pause | needs review |
| `aria.back` | ಹಿಂದೆ | — | needs translation | Back | needs review |
| `aria.nav.approveBadge` | , {n} ಕಾಯುತ್ತಿವೆ | — | needs translation | , {n} waiting | needs review |
| `announce.approved` | ಅನುಮೋದಿಸಿದ್ದೀರಿ. ಸಂಜೆ 6 ರ ನಂತರ ಹೋಗುತ್ತದೆ. | — | needs translation | Approved. Goes after 6 pm. | needs review |
| `announce.bulkApproved` | ಅನುಮೋದಿಸಿದ್ದೀರಿ | — | needs translation | Approved | needs review |
| `announce.withdrawn` | ಹಿಂಪಡೆಯಲಾಗಿದೆ. ಏನೂ ಕಳುಹಿಸುವುದಿಲ್ಲ. | — | needs translation | Withdrawn. Nothing will be sent. | needs review |
| `announce.regenStarted` | ಹೊಸ ಆವೃತ್ತಿ ಬರೆಯುತ್ತಿದೆ | — | needs translation | Writing a new version | needs review |
| `announce.regenReady` | ಹೊಸ ಆವೃತ್ತಿ ಸಿದ್ಧ. ಮತ್ತೆ ಪರಿಶೀಲಿಸಿ. | — | needs translation | New version ready. Check it again. | needs review |

The "Why this" lines and the Kannada summary are A2/A3 output, not locale strings. Language names (`ತೆಲುಗು`/Telugu…) and lesson names come from locale files and curriculum data.

## 6. Tokens and sizes
- Page `bg-canvas`; cards `bg-surface border border-border rounded-md` (usage rule 3). Summary card `bg-panel border` with title in `text-primary-strong` (rule 1: no `primary` text on `panel`). Key words `bg-panel rounded-sm text-label`, `lang="kn"`.
- Status tags (icon + word, rule 5): Ready = `text-status-listened bg-status-listened-bg` + `circle-check`; Needs review = `status-needs-ear` + `circle-alert`; Approved = `status-caught-up` + `check`; Generating = `status-queued` + `clock`; Saved · will send = `status-queued` + `cloud-off`. tokens.md has no Ready/Review/Approved/Generating rows yet: add them.
- AI label: `text-primary-strong text-label` + bot icon (distinct from the reward star, C-25).
- Banners: offline/regenerating `bg-surface border border-border border-l-4 border-l-warning`, text `text-ink`; error `bg-error-bg border-l-error`, icon `text-error`; success `border-l-primary`.
- Buttons: Approve and bulk button `bg-primary text-on-primary min-h-target-primary rounded-md w-full`; Simpler/Shorter/Withdraw ghost `min-h-target`. Checkbox row `min-h-target`, box 24 px. Play button 52 px `bg-primary text-on-primary`.
- Type: title `text-display`; card heading `text-title`/`text-label`; meta `text-caption text-ink-muted` (14 px Latin / 16 px Indic minimum, B-22). Nav badge neutral, not `error` (B-15).

## 7. Acceptance checklist
- [ ] Every state above is built and reachable (Sent and Generation failed once designed)
- [ ] One primary action; works with one thumb at 360 px
- [ ] Status shown as icon + word, never colour alone
- [ ] Targets ≥ 48 px; 8 px apart
- [ ] AI output carries the "AI-assisted" label (detail screen)
- [ ] Every Indic text has a `lang` attribute; strings exist in kn, en, te, ta
- [ ] axe: 0 serious/critical, in light and dark
- [ ] API: `POST /api/packages/{id}/approve { scriptVersion, clientRequestId }` succeeds only when status is Ready and `scriptVersion` equals the current version the teacher ticked; otherwise 409 (B-01, B-02, B-03). Integration test.
- [ ] API: bulk approve takes an explicit list of `{packageId, scriptVersion}`; each item is checked as above; a stale item returns 409 for that item and approves none of the stale ones (B-02, D-28).
- [ ] Bulk button counts only catch-ups that are Ready on screen and ticked; Generating and Needs review are never counted (B-01, B-02). Vitest.
- [ ] Any regenerate (Simpler, Shorter, Make it shorter) clears the tick and disables Approve until the new version is Ready and ticked again (B-03). Vitest + endpoint test.
- [ ] Needs review: no tick, Approve not offered, failure reason in numbers next to the action, "Make it shorter" primary (B-10). Numbers come from the A4 result, not the LLM.
- [ ] Send endpoint returns 409 for any package not Approved (guardrail 3, FR-08). Integration test "send unapproved → 409" (T-14).
- [ ] Withdraw: `POST /api/packages/{id}/withdraw` allowed until the send job starts for that package; afterwards 409. Withdraw returns status to Ready and clears the tick (B-13, D-28).
- [ ] Offline approval goes to the Dexie outbox with `clientRequestId`; replay is idempotent; the card shows "Saved · will send" until synced (B-14, §6).
- [ ] Kannada summary never names a child; uses "the child" (B-12). Verifier code check 6 rejects real names.
- [ ] Verifier rejects a check item whose competency is not a target (B-12, C-11).
- [ ] Audio player shows the three segments; the "Your voice" and "Notice" segments are present in every package (FR-09, D-18).
- [ ] Polite live region announces approve, withdraw, regenerate start and ready; errors assertive.
- [ ] List cards show language + children first, both lessons, duration (B-11). Card is one button with a chevron and a full accessible name.
- [ ] Nav badge: neutral colour, accessible name "Approve, N waiting" (B-15).
- [ ] Tags wrap at 320 px and 200 % text (B-26).

## Open items
- B-14: "Sent" and "Generation failed" tags are pending the real send job. Design both before T-14 closes.
- `CatchUpPackage.Status` in spec §4.3 is Draft/Verified/Approved/Rejected. The screen needs Generating, Ready, Needs review, Approved, Sent, Generation failed, plus a script version. Rahul to extend the enum and add `ScriptVersion`; update `docs/database.md`.
- `/withdraw` and bulk approve are not in the spec §4.4 API list. Add them.
- FR-08 says Withdraw "until the send starts"; the UI says "until 6 pm". Keep the UI wording only if the send job always starts at 18:00.
- "All {packages} approved. {families} families get them after 6 pm. (counts from code)" The prototype has 6 children across 3 catch-ups and the 5 is hard-coded. The real count must come from code (opted-in, not-stopped families, guardrail 4); confirm whether siblings or no-phone children explain 5.
- B-01 also suggests a confirm sheet listing each catch-up for bulk approve. The prototype has no sheet. Decide whether the tick requirement is enough.
- A-21: the teacher can't hear the Telugu audio in her language; the Kannada summary and audio cover D-09. No change unless round 1 says otherwise.
- Replay of a queued approval after the package was regenerated: show what? Not designed.
- The Kannada summary shows "Show Lakshmi teacher." The stored gloss must hold `{teacher}`; the name is filled only for display.
