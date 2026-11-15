# D-106 · Lesson video links are set on an admin page; code puts them in today's message

| | |
|---|---|
| Status | Accepted |
| Owner | Rahul, 15 Nov 2026 |
| Gate | none |
| Screens | admin, T3-approve, P5-todays-message |

## Decision

T-94 asked for "a checked URL field per lesson… only links Monika checked appear", and waited for Monika's link (M-1). Rahul's answers (15 Nov):

- **Where links are set:** a new admin page, `/admin/lesson-links`, lists every chapter and lesson of the curriculum with one link field each. Whoever sets a link there (Rahul or Monika) has checked it on a phone; that is what "checked" means. Only people with the admin link can reach the page.
- **Which links:** any absolute **https** link (DIKSHA, YouTube, a state portal…), at most 500 characters. Anything else is refused.
- **Scope:** one link per lesson, for **every class** (table `LessonLinks`, keyed by the curriculum ID); the demo Reset doesn't touch it.
- **How it reaches families:** when today's message is drafted, **code** copies the link of the first of the day's lessons that has one into the message's video section; the AI never sees or chooses it. The teacher sees it on Approve → Today's message and can remove it; the family page shows it as "Watch how it's taught". The URL is copied into the draft, so her approval covers exactly that link (guardrail 3); a later change on the admin page reaches only the next draft.
- **Key:** saving follows the read pages' rule ([D-102](D-102-admin-pages-open-in-dev.md)): without the key only when the API runs in Development; staging and prod need `Admin:Key`.

## Notes

- 15 Nov 2026: built in T-94 (#191). Monika's link for Nalikali p 4 (M-1) still has to be entered on staging's page.
