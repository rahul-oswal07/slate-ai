# T-131 · Demo class with two Hindi and two English families

| | |
|---|---|
| Status | Done (#280) |
| Issue | #275 |
| Owner | R |
| Screens | demo, admin |
| Source | Rahul, 16 Nov 2026; FR-14; XS |

## What

The demo class had 26 Kannada, 6 Telugu, 3 Tamil, 1 Hindi (Rohan) and no English families. It now has two Hindi and two English: Pooja (20) is Hindi, and Sanjay (27) and Shreya (28) are English. All three have a phone and consent, so messages reach them. Manjunath (15), the story child, stays Kannada. The new mix is 23 Kannada, 6 Telugu, 3 Tamil, 2 Hindi and 2 English.

Every seeded wording (AI disclosure, evening message, weekly star) already has English and Hindi, so nothing else changes.

The change reaches a database in one of two ways. A new database gets it when it is seeded. An existing one (staging, prod) gets it at the next demo **Reset**: startup seeding never changes an existing child. A language changed on the admin page is kept on Reset (T-122, D-113).

## Acceptance criteria

- After a Reset, `/admin/children` shows Pooja as Hindi and Sanjay and Shreya as English, and nobody else changes.

## How it's tested

- `DemoSeedFileTests.The_demo_class_has_families_in_every_language_two_of_them_Hindi_and_two_English`, written first. It failed on the old seed.
- The Db suite (`Category=Db`), because the seed changed. `StartupDbTests.Seeding_again_adds_nothing` pinned the old mix (26, 6, 3, 1) and now pins the new one, English included.

## Notes

- 16 Nov 2026: built. `docs/design/journey.md` still says "6 Telugu, 3 Tamil, 1 English". That was already out of date, and it is Monika's file.
