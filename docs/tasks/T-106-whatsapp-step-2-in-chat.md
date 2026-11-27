# T-106 · WhatsApp step 2: today's message inside the chat

| | |
|---|---|
| Status | Built behind a flag (#324) |
| Issue | #320 |
| Owner | R |
| Screens | delivery, P5-todays-message |
| Source | see What; D-98, D-108, D-127 (Proposed) |

## What

**WhatsApp step 2: today's message inside the chat (proposed, Monika, 14 Nov, D-98; after T-104, likely after the final):** `todays_lesson_chat_v1` has a quick reply "Send it here" + a website button. The tap reaches a webhook (`GET` verify, `POST` with an `X-Hub-Signature-256` check, idempotent per message ID) and opens WhatsApp's 24-hour window; then, only from the **Approved** version (guardrail 3), send in order: (1) the teacher's greeting as a voice note, (2) the lesson audio as a voice note (AI disclosure + concept in one Ogg Opus file: needs D-18 changed, since today the segments stay separate), (3) one text: concept, activity steps, homework page, video link with its size, (4) the picture question as an image drawn by code with **> < =** reply buttons, (5) the reason for absence as a **list message** (5 options; a plain message allows only 3 buttons). The child's tap is scored by code, answered "Right ⭐" / "Try again" and shown on Today (T-92). A `STOP` reply sets `ParentStoppedAt`. Nothing is sent after the window closes. Low data: text carries the lesson even if audio never loads; each clip is uploaded once per lesson and language and its media ID reused. `catchup_chat_v1` was not created (Meta flagged every wording as Marketing), so the catch-up stays link-only

## Acceptance criteria

After "Send it here" a registered phone gets the voice notes, text, picture question and reason list in order; a tap on > < = or a reason reaches the teacher's Today; a bad signature gets 401; a repeated event is handled once; no names in logs or AI calls

## How it's tested

Endpoint tests over `FakeApi` (signature, unknown token, duplicate event, window closed); unit tests for message order and the scorer; manual check on a registered phone

## Switching it on

Built behind `WhatsApp:ChatLesson:Enabled` (default `false`): until then today's message goes as `todays_lesson_link_v1` and the webhook answers 404. Secrets only in user-secrets or App Service settings, never in the repo.

1. **Meta (Monika or Rahul):** check that `todays_lesson_chat_v1` is approved in the family languages in the account that owns the sending number, with the quick reply **first** (button 0) and the URL button second (button 1). Fix its URL button as for the other templates (`…/p/` + Dynamic, no typed `{{1}}`).
2. **App settings** (App Service → Configuration, or `dotnet user-secrets set` locally):

   | Setting | Value |
   |---|---|
   | `WhatsApp__AppSecret` | the Meta app's secret: App settings → Basic → App secret (**secret**) |
   | `WhatsApp__VerifyToken` | any long random string you make up (**secret**); typed into Meta in step 3 |
   | `WhatsApp__ChatLesson__Enabled` | `true` |
   | `WhatsApp__ChatLesson__Template__Name` | only if the approved name isn't `todays_lesson_chat_v1` |
   | `WhatsApp__ChatLesson__Template__Languages__ta` (etc.) | only to stop a language that isn't approved: a language missing from the map goes as the link template |
   | `WhatsApp__ChatLesson__JoinDisclosure` | `false` only if D-127 is turned down: the disclosure and the lesson then go as two voice notes |

   `Delivery__Channel=whatsapp`, `WhatsApp__PhoneNumberId` and `WhatsApp__AccessToken` stay as they are.
3. **Meta webhook:** developers.facebook.com → the app → WhatsApp → Configuration → Webhook → Edit: Callback URL `https://slateai-staging.azurewebsites.net/api/whatsapp/webhook` (prod: its own host), Verify token = `WhatsApp__VerifyToken`. Verify and save (the app must be running with the settings above), then **Subscribe** to the `messages` field.
4. **Phone check:** mark the registered child absent, confirm a lesson, approve today's message, Send messages now. The template now shows "Send it here". Tap it: her greeting, the lesson voice note, one text, the picture question with > < = and the reason list arrive in that order. Tap a sign and a reason: both show on Today. Type `STOP`: "Messages stopped" comes back and the child shows as stopped. Grep the log stream for the number and the child's name: no hits.

## Notes

- 27 Nov: built behind `WhatsApp:ChatLesson:Enabled` (off), #324. `todays_lesson_chat_v1` with a "Send it here" quick reply (payload `lesson:{token}`, button 0) and the URL button (1) replaces the link template for today's message when it's on and the language is in its map. `GET/POST /api/whatsapp/webhook` (`Features/WhatsAppChat/`): verify token, `X-Hub-Signature-256` HMAC with `WhatsApp:AppSecret` (401 otherwise), each wamid handled once (`ProcessedRequests`, endpoint `whatsapp-webhook`, a GUID from its SHA-256). "Send it here" from a saved number sends, only from today's message approved at its version (checked again), consent yes, not stopped, link not expired, and while the 24-hour window from the tap is open: greeting voice note → disclosure + concept as **one** voice note (`OpusEncoding.Join`, D-127 proposed; `JoinDisclosure=false` sends two) → one text (lesson, concept, steps, homework page with its book, video link, her note) → picture question with > < = or the sum's three numbers as reply buttons (unless locked) → reason list of 5 (unless given). Taps go through the same stores as the family page (`SavePictureAnswerAsync`, `SaveReasonAsync`), so they show on Today; replies say what was chosen and never right or wrong (D-108, not "Right ⭐" as in What above). `STOP` (or the word in kn/te/ta/hi) stops every child that number is saved for, also after the window, and confirms while it's open. Each clip or joined pair is uploaded once and its media ID reused (in memory, 29 days). Fixed lines in `data/seed/whatsapp-chat.json` (copied from the family pages; new lines in kn/te/ta/hi are drafts for T-26). Logs carry the hashed wamid and codes only. Tests: `WhatsAppWebhookEndpointTests` (off = 404, verify, bad signature 401, order, duplicate once, media reuse, window closed, unknown token and sender, guardrail 3 and send rules, picture, reason, STOP, late STOP), `ChatLessonPlanTests` (order, join, sums, D-108 replies, window, wording limits, reply IDs), `WhatsAppTests` (chat template shape, fallbacks, shipped settings off), `OpusEncodingTests` (join), `DeliveryStoreDbTests` extended for the chat store's queries. **Left:** the switch-on and the phone check above; the picture question is text, not an image (no drawing library); the video's size isn't known, so the text says it uses mobile data; WhatsApp doesn't guarantee order, so messages are sent one after the other; media IDs are re-uploaded after a restart; `whatsapp-setup.md` §5 still describes step 2 as next (Monika's file).

- 15 Nov: moved here from `tasks.md` (T-110).
- 14 Nov, from `status.md`: Step 2 (T-106, today's message inside the chat) after the final.
