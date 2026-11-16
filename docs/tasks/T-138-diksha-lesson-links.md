# T-138 · A DIKSHA link for every lesson, and a script to set them

| | |
|---|---|
| Status | Done (#293) |
| Issue | #292 |
| Owner | R |
| Screens | admin |
| Source | Rahul, 16 Nov 2026; T-94, D-106, M-1; S |

## What

`/admin/lesson-links` (T-94) had no links on staging or prod, so today's message showed no video. This task adds two things:

- **`data/lesson-links/diksha-kn.json`**: one entry per curriculum lesson.
  - Each entry has its DIKSHA page `https://diksha.gov.in/play/content/do_…`, the DIKSHA title, `kind` (`video` or `activity`) and `match`:
    - `topic`: it teaches this lesson's skill, e.g. "Addition with Regrouping" for `M3.ADD.CARRY`;
    - `chapter`: the chapter's general video, the closest there is.
  - `url: null` comes with a reason when nothing suitable is live.
- **`scripts/set-lesson-links.mjs`**: puts them on a server through the admin API.
  - It is a dry run unless `--apply`.
  - It keeps a link someone already set unless `--overwrite`.
  - `--topic-only` leaves out the chapter videos.
  - It never clears a link, and a rerun changes nothing.

**Source.** The Karnataka Class 3 Kannada-medium digital textbooks on DIKSHA, "3. ಗಣಿತ ಭಾಗ-1" (`do_31338234814977638414751`) and "ಭಾಗ-2" (`do_31338236796715827215181`). Their 12 chapters are ours. They were read through DIKSHA's public content API (search, textbook hierarchy and content read). The 2026-27 editions on DIKSHA have their chapters set up but no content linked yet.
- The videos are the state's Kannada **teacher-training** videos ("how it's taught"). The activities are short interactive exercises.
- Every chosen item was `Live` on 16 Nov.

**Result:** 54 of 61 lessons have a link: 28 `topic`, 26 `chapter`; 44 videos and 10 activities. The 7 without a link have nothing live: the mental maths PDF is retired, there's no time content, and the pattern chapter video is still processing.

## Acceptance criteria

- The file lists every curriculum lesson once, and every link passes `LessonLinkRules` (CI: `DikshaLinksFileTests`).
- The script's dry run lists what would change. `--apply` sets them, and a rerun reports them as "already set".
- A link set by hand is kept unless `--overwrite`.

## How it's tested

- `DikshaLinksFileTests` (covers the curriculum exactly; every URL accepted by the admin page's rule).
- The script was run against a local server: dry run, apply (52 set, 2 kept), rerun (52 already set), `--overwrite` and `--topic-only` dry runs. Dry runs against staging and prod (read only) would set 54 each.

## Notes

- 16 Nov 2026: applied to **staging** on Rahul's request (54 set, 7 without a link; a rerun reports all 54 already set). Not applied to prod.
- 16 Nov 2026: built. Worth a look on a phone before the final (M-1): the demo lesson `M3.BRIDGE.COMPARE` gets the comparing activity "ಹೋಲಿಸುವುದು-NSC4_2A-G3", which compares 3-digit numbers. The alternative is the chapter video "Numbers (Kannada) - Full Concept". The teacher-training videos are long and made for teachers.
