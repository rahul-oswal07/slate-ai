# T-126 · Separate links for Under the hood and a child's family phone

| | |
|---|---|
| Status | In progress |
| Issue | none yet |
| Owner | R |
| Screens | demo |
| Source | Rahul, 15 Nov 2026; FR-14; S |

## What

`/demo` shows the teacher phone, Manjunath's family phone and Under the hood on one page. Two new links show one panel each, for a second screen or a real phone:

- `/demo/ai`: Under the hood on its own, with every AI call since the last reset, the same as the panel.
- `/demo/family/:childId`: the family phone of **any** child in the demo class. On a phone the chat fills the screen; from `lg` up it sits in the 360 px frame under the demo header.

Both pages sign in as the demo teacher themselves, like `/demo`, so the link works in a fresh browser. `/demo` links to both from its step guide. A new `GET /api/demo/children/{childId}` gives the child's first name and saved home language for the family page's heading. It returns 404 unless the user is signed in as the demo teacher and the child is in the demo class.

## Acceptance criteria

- `/demo/ai` lists the same calls as the panel in `/demo`, with no phones and no step guide.
- `/demo/family/<id>` shows that child's messages, headed "<name>'s family's phone · <language>", and opens each link at `/p/<token>`.
- An id outside the demo class shows "No child with this link in the demo class" and a link back to `/demo`.
- `/demo` keeps working as before.

## How it's tested

- API: `DemoShellEndpointTests.Child_gives_a_demo_childs_name_and_saved_language`, `Child_is_404_for_a_child_outside_the_demo_class` and `Child_is_404_for_a_teacher_of_another_class`.
- Web: `DemoAiScreen.test.tsx`, `DemoFamilyScreen.test.tsx`, and the new link test in `DemoScreen.test.tsx`. No new Playwright specs (CLAUDE.md).
- Screenshots at 360 px and desktop width from a local run; axe on both pages.

## Notes

- 15 Nov 2026: built. `DemoScreen.tsx` is split into `DemoParts.tsx` (header, page, frame), `FamilyPhone.tsx` (`FamilyChat` + `ParentPhone`) and `UnderTheHood.tsx`, with no change to `/demo`. The kn strings for the new keys are in English, like the rest of the kn demo strings. Screenshots at 360 px and 1280 px came from the Vite dev server with the API mocked in the browser. Axe found no violations on `/demo/ai`, `/demo/family/<id>` or the not-found state. Left: Monika's check of the copy, a GitHub issue, and one run against a real database.
