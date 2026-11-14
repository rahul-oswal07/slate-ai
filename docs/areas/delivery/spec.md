# Delivery · spec

The 6 pm send job, the outbox, WhatsApp.

| | |
|---|---|
| Route | none |
| Web | none |
| API | `Features/Delivery/`, `Features/EveningMessages/` (`EveningSendWorker`, `EveningSender`), `Features/CatchUps/CatchUpSender.cs` |
| Design card (Monika) | none yet |
| Status | [`status.md`](status.md) |

## Purpose and states

How an approved message leaves the server. Meta setup and template wording: [`whatsapp-setup.md`](../../whatsapp-setup.md); the design of step 1: [`whatsapp-delivery-plan.md`](../../whatsapp-delivery-plan.md). Guardrail 3 (nothing reaches a parent without teacher approval) is in `CLAUDE.md`.

## Requirements

Moved as they are from `spec.md` §2 (15 Nov 2026). This file is now where they are kept; `spec.md` §2 has the index.

| ID | P | Requirement | Acceptance |
|---|---|---|---|
| FR-16 | M | Send via WhatsApp Cloud API (template message with link), scheduled for after 6 pm. **Step 1 (T-40, D-97):** the evening message and the catch-up go as approved Utility templates (`evening_absence_v1`, `catchup_ready_v1`) whose URL button opens `/p/{token}`; only to children listed in `WhatsApp:Recipients` (the ≤ 5 registered test phones), everyone else stays on the demo chat. Step 2 (Listen quick-reply, webhook, voice note in chat, STOP reply) follows. **14 Nov (D-98):** the templates are now `todays_lesson_link_v1` (today's message), `todays_lesson_chat_v1` (step 2, T-106) and `catchup_link_v1`; the old two weren't submitted (Marketing) | Delivered to a registered test number; nothing is queued for a phone except in the same transaction as a message that passed guardrail 3; stop, consent and link expiry are checked again at send time; no names or numbers in logs (unit tests + Db test) |

## Decisions cited

D-97, D-98.

D-01 to D-99 are in [`../../decisions/D-01-to-D-99.md`](../../decisions/D-01-to-D-99.md); later ones are one file each in [`../../decisions/`](../../decisions/README.md).
