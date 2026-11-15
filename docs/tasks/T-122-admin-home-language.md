# T-122 · Change a child's home language on /admin/children

| | |
|---|---|
| Status | Done |
| Issue | #248 |
| Owner | R |
| Screens | admin, demo |
| Source | Rahul, 15 Nov 2026; D-113; S |

## What

Each child's row on `/admin/children` gets a home-language picker: Kannada, English, Telugu, Tamil and Hindi. A change is used for everything made or sent after it, and it survives the demo Reset, as saved WhatsApp numbers do.

## Acceptance criteria

- `PUT /api/admin/children/{childId}/language` with `{ clientRequestId, language }`:
  - sets the language and `HomeLanguageSetAt`;
  - returns 400 for any language other than `kn`, `en`, `te`, `ta` or `hi`, or an empty `clientRequestId`;
  - returns 404 for an unknown child;
  - is idempotent per `clientRequestId`.
- `GET /api/admin/children` returns `languageChanged` for each child.
- On the page, the picker saves on change, says "Home language saved", shows an alert and keeps the stored language on failure, and shows "Changed here" for a changed language.
- The demo Reset keeps a changed language. Children whose language wasn't changed get the seed's.
- Migration `HomeLanguageSetAt`, documented in `docs/database.md`.

## How it's tested

- API tests:
  - `AdminChildEndpointTests`: change, 400s, 404, idempotent, list.
  - `HomeLanguageRulesTests`.
  - `SavedLanguagesTests`.
- Db: `DemoResetDbTests.Reset_puts_a_used_demo_class_back…` also changes Manjunath's language before the Reset, and checks it is kept afterwards.
- Vitest: three new tests in `AdminChildrenScreen.test.tsx`.
- A local run at 360 px, with axe.

## Notes

- 15 Nov 2026: built the API (endpoint, store, rules, Reset keeps it, migration), the web picker and the docs (D-113, `database.md`, admin spec and status). The kn, te, ta and hi strings are drafts for Monika.
- 15 Nov 2026: shipped to prod in `v1.5` (#252 on `main`). Prod serves `sha-e1a9519`, and its `/api/admin/children` returns `languageChanged`.
