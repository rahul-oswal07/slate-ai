# T-204 · The weekly star template in kn, te, ta and hi

| | |
|---|---|
| Status | Proposed |
| Issue | #382 |
| Owner | M |
| Screens | delivery, P6-weekly-star |
| Source | Rahul, 27 Nov 2026 (delivery list); T-125, D-118; S |

## What

`weekly_star_v1` was submitted in English only (15 Nov), so families in other languages get no star on WhatsApp. Submit it in Kannada, Telugu, Tamil and Hindi from the approved star wording. Then switch each language on with `WhatsApp__WeeklyStar__Languages__<lang>` as Meta approves it.

## Acceptance criteria

- Each language approved at Meta in the account that owns the sending number.
- `whatsapp-setup.md` §3 has the wording; the delivery status lists which languages are on.
- One real send per language on staging.

## How it's tested

- A manual send on a registered phone per language.

## Notes

- 27 Nov 2026: proposed.
