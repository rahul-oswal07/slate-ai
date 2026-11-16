# T-130 · Link each demo child's simulated family phone from the children page

| | |
|---|---|
| Status | Built, not merged |
| Issue | #274 |
| Owner | R |
| Screens | admin, demo |
| Source | Rahul, 16 Nov 2026; FR-14; S |

## What

T-126 added `/demo/family/:childId`, the simulated family phone of any child in the demo class. The only way in was the one link for Manjunath in `/demo`. Now `/admin/children` links every demo child's family phone, so any child's phone can be opened on a second screen or a real phone.

`GET /api/admin/children` marks the demo class (`demo: true`). That is the seed file's class, the same check the demo endpoints use, and only when `Demo:Enabled` is on, because otherwise `/demo/family` isn't mapped. Other classes get no link, because `/demo/family` only knows the demo class.

## Acceptance criteria

- On `/admin/children`, each child of the demo class has a "Family phone (simulated)" link to `/demo/family/<id>`.
- Children of any other class have no link.
- With `Demo:Enabled=false`, no class has links.

## How it's tested

- API: `AdminChildEndpointTests.Marks_the_demo_class_so_the_page_can_link_each_childs_family_phone` and `No_class_is_the_demo_class_when_the_demo_is_off`.
- Web: `AdminChildrenScreen.test.tsx`, "links each demo child's simulated family phone (T-130)" and "a class that is not the demo class has no family phone links (T-130)".
- No new Playwright specs (CLAUDE.md). A 360 px screenshot from a local run.

## Notes

- 16 Nov 2026: built. Both new tests were written first: the API one failed to compile and the web one failed. The link's spoken name contains its visible text ("Family phone (simulated) for Manjunath") for WCAG 2.5.3. The kn, te, ta and hi strings are drafts for Monika. Left: Monika's check of the copy.
