# T-208 · A real school number on WhatsApp

| | |
|---|---|
| Status | Proposed |
| Issue | #386 |
| Owner | R / M |
| Screens | delivery |
| Source | Rahul, 27 Nov 2026 (delivery list); `whatsapp-delivery-plan.md` §4 (real number and verification are roadmap); M |

## What

Staging sends from Meta's test number, which reaches only 5 registered phones. To reach real families:

- business verification for the school or project;
- a phone number of its own with a display name families recognise;
- a production WhatsApp business account, with every template submitted again there;
- a permanent system-user token for prod.

Write the steps into `whatsapp-setup.md` as they're done.

## Acceptance criteria

- Prod sends from the real number to an unregistered phone.
- Templates approved in the new account in the languages in use.
- Meta's messaging limit for the number is known and noted in `areas/delivery/status.md`.

## How it's tested

- A real send on prod to a phone that isn't registered for testing.

## Notes

- 27 Nov 2026: proposed.
