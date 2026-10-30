# Optional board photo with the lesson voice note (reopens D-26 / FR-17 / T-41)

## Context
Today the teacher records a ≤30 s voice note (T1 `LessonCard`) → `POST /api/classes/{id}/lessons/{date}/audio` → Sarvam STT → name scrub → A1 (`a1-lesson-mapping`) → she confirms on T2. Board-photo capture was dropped in D-26 (spec §1, FR-17, T-41, D-06 closed 24 Oct, "re-add as a task if wanted"). Rahul decided (this session) to reopen it:
- **Optional**: skipping the photo changes nothing.
- **Job**: the photo goes to Claude vision *together with* the scrubbed transcript to improve A1's lesson match. Same output (`A1Result`), teacher still confirms (FR-03).
- **Stored with the lesson** (reverses spec §7 "no photos" for this one case; needs consent/retention note).
- **UI**: "Add board photo" button next to the mic (phone camera via `<input type="file" accept="image/*" capture="environment">`), before or after recording; photo and audio upload together.

Gates: `spec.md` §8 D-06/D-26 and §7 privacy need an updated decision entry (Rahul owns; note for Monika). CLAUDE.md: >3 files → this plan goes in the PR description.

## Guardrail impact (decide before building)
1. **Names (guardrail 1)**: pixels can't be scrubbed. A board may show child names. Mitigations: A1 prompt tells the model to ignore/never output personal names on the board; output is schema-validated and run through existing `NameScrubber` + a check that no roster name appears in `summaryKn/En/examplesUsed` (reuse `A1Validator`). Add a teacher-facing line next to the button: "Don't include children's faces or names." **Flag for Rahul**: strictly this sends an image that may contain names to Claude; accept as a documented exception or require the hint-only approach.
2. Output still parsed with schema + validated against curriculum IDs (guardrail 2) — unchanged.
3. Photo never reaches parents. Not part of any `CatchUpPackage`/message (guardrail 3 untouched).
4. Prompt change → bump `a1-lesson-mapping` to v2, add photo eval cases, include eval run in PR (guardrail 6).

## Backend (`src/api/SlateAi.Api`)
- **Storage**: new table `LessonPhotos` (`Id`, `LessonId` FK cascade, `Content` bytea, `ContentType`, `CreatedAt`) — separate from `Lessons` so reads of the day's lesson stay light. Migration `LessonPhoto`; update `docs/database.md` in the same PR. One photo per lesson (D-55 one lesson/day); a new photo replaces the old. Delete cascades with class/lesson.
- **Limits**: `image/jpeg|png|webp`, ≤ 1.5 MB after client downscale (client resizes to ≤1600 px long edge, JPEG q≈0.8); `413`/`415` like audio. Magic-byte sniff, don't trust content-type.
- **Endpoint**: extend `POST …/lessons/{date}/audio` to accept an optional second multipart part `photo` (so photo + audio are one idempotent request; `LessonEndpoints.UploadAudioAsync`). Add `PUT/GET/DELETE …/lessons/{date}/photo` (`GET` serves bytes with `Cache-Control: private`, guarded by `RequireClassOwner`) so she can add one later, view or remove it. All with `TypedResults`, `WithSummary/WithTags`, `clientRequestId` idempotency via `ProcessedRequests`.
- **Lessons slice**: `ILessonStore` gains `SavePhotoAsync/GetPhotoAsync/DeletePhotoAsync`; `LessonView` gains `hasPhoto: bool`. `MappingJob` gains `byte[]? Photo, string? PhotoType`.
- **Vision call**: `LlmRequest` gains optional `IReadOnlyList<LlmImage>? Images` (`LlmImage(byte[] Data, string MediaType)`). `ClaudeLlmClient.BuildParams` builds content blocks (image block(s) then text) instead of a plain string when images exist. `LoggedLlmClient`/`AiCall` log image count + bytes (not the bytes). `FakeLlmClient` records images for tests.
- **Mapper**: `LessonMapper.MatchAsync` — if photo present, pass it to `MapTranscriptAsync(..., photo)`. Photo-only path (STT failed/no speech, or no audio): fall back to a vision-only A1 call instead of `Unclear`. Photo-only upload (no audio) allowed when `file` missing but `photo` present.
- **Prompt**: `prompts/a1-lesson-mapping.md` v2: explain optional board image (handwriting may be Kannada/English, may be blurry), use it to corroborate/disambiguate the transcript, never transcribe or output names, lower confidence when photo and note disagree, still ≤3 IDs from the list.
- **Config**: model key `Mapping` must be vision-capable (check `Ai:Models:Mapping`); no hard-coded IDs.

## Frontend (`src/web`)
- `features/lessons/useBoardPhoto.ts`: hidden file input with `capture`, downscale via canvas → JPEG `Blob` (pure helper `resizeImage.ts`, unit-tested), preview URL, remove.
- `LessonCard.tsx`: secondary `btn-secondary` "Add board photo" with camera icon (add to `components/Icon` if missing) beside the mic; thumbnail + "Remove" once chosen; hint text (no faces/names). Photo chosen **before** recording is held in state and sent with the note; chosen **after** (note already sent/processing) goes through the new `PUT …/photo`. Mic remains the only amber hero; button ≥ target size; 360 px layout checked.
- Offline: extend `offline/db.ts` outbox item with optional `photo`, and `outbox.ts toForm` to append it; photo-only items use the `PUT` route. Keep the photo on the phone in Dexie until the lesson is confirmed (like `db.notes`).
- `ConfirmLessonScreen` (T2): small "Board photo" thumbnail (tap to enlarge, alt text via i18n) so she can compare with the match.
- `useLessons.ts`: `useSendNote(note, photo?)`, `useBoardPhoto upload/remove`, types (`hasPhoto`).
- i18n: new keys (`today.lesson.addPhoto`, `.photoHint`, `.removePhoto`, `.photoAlt`, errors) in **all** locale files (`kn`, `en`, `te`, `ta`, `hi`); te/ta/hi drafts flagged for native review like earlier work.

## Tests (no new Playwright, per 22 Oct rule)
- xUnit (fakes, no DB): endpoint accepts audio+photo / photo-only / bad type / too big / replay idempotent; photo reaches `FakeLlmClient` as an image and **not** in logs; roster names in output are rejected; falls back to unclear when LLM fails; `BuildParams` produces image + text blocks; ownership guard on photo GET.
- Vitest: `resizeImage`, `LessonCard` (button shown, optional, remove, sends with note), `ConfirmLesson` thumbnail, locale key parity.
- Db suite: ask first before adding anything (CLAUDE.md); I'd propose none — query is trivial. Run once throwaway as in earlier tasks.
- Eval: add ~6 photo cases to `evals/cases/a1-lesson-mapping.json` (clear board, blurry, board contradicts note, board with a child's name, empty board, photo-only); run `dotnet test --filter Category=Eval` **only when Rahul says** (costs credits) and attach results.

## Docs
`spec.md` (FR-17 reinstated as M/S, new decision D-81 incl. privacy exception §7, API list), `tasks.md` (new task row, next ID), `plan.md` if it drifts, `database.md`, `quality.md` (eval), `status.md` end-of-session entry. Branch `feat/T-xx-board-photo`; Conventional Commits with task ID.

## Verification
1. `./scripts/verify.sh` (build + all API/web tests) and report result.
2. Local run (`docker compose … up -d db`, `dotnet run`, `npm run dev`): at 360 px record + photo → T2 shows match + thumbnail; photo-only; skip photo (unchanged flow); offline queue then reconnect; remove photo; confirm photo row deleted with lesson/class.
3. axe on Today (with and without photo) and T2, light/dark, en + kn; screenshots at 360 px in `docs/screenshots/T-xx/` for the PR (axe note since no new e2e).
4. With real keys, one live A1 call with a real board photo (Chrome Android + iOS Safari for `capture`).

## Open questions for Rahul (non-blocking, defaults chosen)
- Retention: stored indefinitely with the lesson by default; suggest auto-delete after N days or with child/class deletion only — pick N?
- Names-on-board exception to guardrail 1 (see above).
