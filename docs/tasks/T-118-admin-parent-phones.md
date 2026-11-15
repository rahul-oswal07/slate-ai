# T-118 · Admin page: children and their parents' WhatsApp numbers

| | |
|---|---|
| Status | Done |
| Issue | #240 · PR #241 |
| Owner | R |
| Screens | admin, delivery |
| Source | Rahul, 15 Nov ("an admin screen with all the children in a class and their phone number, editable"); [D-110](../decisions/D-110-whatsapp-to-saved-number.md); size M |

## What

Changing a family's WhatsApp number used to mean editing a `WhatsApp__Recipients__{childId}` App Service setting; the numbers on the children rows are fictional seed values that are never dialled. Now `/admin/children` (behind `Admin:Key`, like the other admin pages) lists every class's children, by roll number, with:

- the number their family's WhatsApp goes to, and where it comes from: **Saved here**, **From the server setting**, or **No number: WhatsApp skips this family**;
- a note when nothing is sent whatever the number (no consent, or the family stopped messages);
- **Edit** → a number field with **Save** / **Cancel**, and **Clear** for a saved number.

A saved number (`Children.ParentPhone` with `ParentPhoneSetAt` set) is dialled first; without one, the server setting is used (`WhatsAppChannel`). Numbers are stored as E.164: a 10-digit Indian mobile gets `+91`; a full number with a country code is kept. Saved numbers survive the demo Reset. The number only ever goes to WhatsApp's send (guardrail 1) and is never logged.

## Acceptance criteria

- The page lists every class and child with the number WhatsApp would use and its source; seeded numbers are never shown.
- Saving a valid number stores it as E.164 and WhatsApp uses it ahead of the setting; an invalid one is refused with how to fix it and nothing changes.
- Clearing falls back to the server setting.
- Without the key (outside Development) both endpoints are 404.
- A demo Reset keeps saved numbers.

## How it's tested

- API: `ParentPhoneRulesTests`, `AdminChildEndpointTests`, `WhatsAppTests.A_number_saved_on_the_admin_page_is_dialled_first`, `SavedPhonesTests`.
- Db suite: the new migration; `DemoResetDbTests` now saves a number before Reset and checks it is kept.
- Vitest: `AdminChildrenScreen.test.tsx`; `AdminIndexScreen.test.tsx` lists the page.
- A local run at 360 px with screenshots (list; a refused number) and axe (0 violations, list and editing).

## Notes

- 15 Nov: the local run found Reset failing (500) once a number was saved: reading the saved numbers tracked the children that Reset adds back with the same IDs. Fixed with a no-tracking read; the Db Reset test failed first, then passed.
- 15 Nov: built. Migration `ParentPhoneSetAt`. The kn, te, ta and hi strings are drafts for Monika. Not changed: the admin WhatsApp test send (`/api/admin/whatsapp/test`) still uses only the server setting.
