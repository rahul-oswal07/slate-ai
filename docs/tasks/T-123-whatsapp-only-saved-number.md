# T-123 · WhatsApp goes only to the number saved on /admin/children

| | |
|---|---|
| Status | Merged (#250), on staging; prod at the next tag |
| Issue | #249 |
| Owner | R |
| Screens | admin, delivery |
| Source | Rahul, 15 Nov 2026; D-114 (replaces D-110's fallback); S |

## What

WhatsApp uses only the family's number saved on `/admin/children`. The `WhatsApp__Recipients__{childId}` server setting is no longer read and is removed from staging.

## Acceptance criteria

- `WhatsAppChannel` dials only `DeliveryMessage.Phone`, the saved number. Without one, the result is `no-recipient` and nothing is sent.
- The test send uses the child's saved number, and returns `no-recipient` without one.
- `WhatsAppOptions.Recipients`, `RecipientFor` and `AdminChild.PhoneFrom` are gone. The admin page shows the saved number with no source caption, and Clear shows for any number.
- No `WhatsApp__Recipients__*` setting is left on staging or prod.

## How it's tested

- API tests:
  - `WhatsAppTests.It_dials_the_number_saved_on_the_admin_page`
  - `WhatsAppTests.Without_a_saved_number_nothing_is_sent_D114`
  - `WhatsAppTestEndpointTests.A_child_without_a_saved_number_is_skipped_D114`
  - `AdminChildEndpointTests.Clearing_leaves_no_number_so_WhatsApp_skips_the_family_D114`
- Vitest: the `AdminChildrenScreen` tests.

## Notes

- 15 Nov 2026:
  - On staging, Ravi's and Manjunath's numbers came only from the setting. They were saved on `/admin/children` through the API, with values identical to the settings, before any code change.
  - Prod has no WhatsApp settings.
  - Built: the API, web, tests and docs.
  - Left: after merge and the staging deploy, delete the two `WhatsApp__Recipients__*` settings from staging. Prod gets the change at its next tag.
  - Merged as #250 and deployed to staging (`sha-67ec349`). Both `WhatsApp__Recipients__*` settings are deleted from staging. Staging keeps saved numbers for Ananya, Ravi, Manjunath and Rohan.
