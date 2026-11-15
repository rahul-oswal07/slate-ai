# T-121 · Admin pages open without a key

| | |
|---|---|
| Status | Done |
| Issue | #245 |
| Owner | R |
| Screens | admin, delivery |
| Source | Rahul, 15 Nov 2026; D-112 (supersedes the key rule of D-83, and D-102); S |

## What

The admin pages and their API open directly on staging, prod and local, with no `#key=…` link and no `Admin__Key` setting.

## Acceptance criteria

- Every `/api/admin/*` route answers without `X-Admin-Key` in Production, Staging and Development, including `/api/admin/children` and `POST /api/admin/whatsapp/test`.
- Unknown clips, children and lessons are still 404, and admin responses still carry `no-store` and `noindex`.
- `/admin` and every page linked from it open from a plain address. The nav links carry no hash.
- `AdminOptions`, `Admin:Key`, `X-Admin-Key`, `adminKey()` and `admin.needsLink` are gone from the code and the locales.
- `Admin__Key` is deleted from staging once the change is deployed.

## How it's tested

- API tests:
  - `AdminEndpointTests.Every_admin_route_opens_without_a_key_in_every_environment_D112`
  - `AdminChildEndpointTests.The_page_opens_and_saves_without_a_key_D112`
  - `LessonLinkEndpointTests.It_opens_and_saves_without_a_key_D112`
  - `WhatsAppTestEndpointTests.It_sends_without_a_key_D112`
- The Vitest admin screen tests render at plain paths and check that the nav links carry no hash.
- A local run in Production mode with no `Admin__Key`.

## Notes

- 15 Nov 2026:
  - Setting staging's `Admin__Key` to `7755` switched the admin pages off, because keys under 16 characters left the routes unmapped. Rahul then chose to remove the key everywhere (D-112).
  - Built:
    - API: `AdminOptions` deleted, and the key checks removed from the admin, lesson-link, children and WhatsApp test routes.
    - Web: no `#key`, and no "needs its full link" message.
    - Tests updated.
  - Merged as #246.
  - Tagged `v1.4` on `main`. This shipped everything since `v1.3` to prod, including 6 migrations, after Rahul confirmed.
  - Prod and staging both serve `sha-c657fc6`, and their `/api/admin/*` routes answer 200 with no key.
  - `Admin__Key` is deleted from staging; prod never had it.
  - Left: approve the evening message once on prod (see `docs/status.md`).
