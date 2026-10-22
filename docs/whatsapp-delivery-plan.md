# WhatsApp delivery plan (proposal for Rahul)

**Status: proposal, `[CONFIRM: Rahul]`.** Nothing in the spec changes until Rahul approves. On approval:
- this becomes decision **D-73**, replacing D-05's "WhatsApp stretch";
- **FR-16 moves from S to M**;
- T-40 is updated in `tasks.md`.

## 1. What Monika wants

Parents receive the **teacher's voice and the written message inside WhatsApp**:
- the evening message on each day of absence (FR-20);
- the Friday catch-up (FR-09, FR-10).

The demo chat on `/demo` stays, so judges can test without a registered phone.

**Why it scores:**
- "works in real scenarios" (AI & technical, 30%);
- **real parents testing on their own phones** (efficacy, 35%: proof of testing);
- a convincing demo video and Top-10 live demo.

## 2. The WhatsApp rule that shapes the design

- A business may **start** a conversation only with a **pre-approved template**.
- Templates carry text, an image, a video or a document, **but not audio**.
- Once the parent **replies or taps a button**, a **24-hour window** opens, and normal messages (including **voice notes**) can be sent. These replies are free.

So every send is two steps, all inside WhatsApp:

```
6:05 pm  Template (text + quick-reply button):
         "We missed Ravi today. In class we learned … Homework: page 15.
          A classmate is keeping Ravi's seat."   [ ▶ Listen to the teacher ]
         ── parent taps the button ──►  webhook  ──►  24 h window open
6:05 pm  Voice note: her greeting (audio/ogg; codecs=opus)  ← plays in WhatsApp
```

**Friday catch-up:**
1. Template: "A short lesson for Ravi from Lakshmi teacher" [ ▶ Listen ].
2. After the tap: the **lesson voice note** (greeting → disclosure → lesson), then a text with the link to the picture question (P3) on our page.

## 3. How it fits the existing code (no rewrite)

`EveningSender.SendAsync` already decides who gets a message (guardrail 3, consent Yes, not stopped, lesson confirmed) and stores an `EveningMessage` with a `Token`. The demo chat reads it through `GET /api/p/{token}`. WhatsApp is **one delivery step after storing**:

| Piece | Change |
|---|---|
| `IParentChannel` (new, in `Features/Delivery/`) | `Task<DeliveryResult> SendEveningAsync(EveningMessage, …)`, plus `SendCatchUpAsync(…)` later (T-15). Two implementations: `DemoChatChannel` (no-op; the demo chat reads by token, as today) and `WhatsAppChannel` |
| Selection | Config `Delivery:Channel = demo \| whatsapp` (`IOptions<DeliveryOptions>`). With `whatsapp`, a child sends only if their parent number is in `WhatsApp:AllowedNumbers` (the ≤ 5 registered test numbers), else falls back to demo |
| `EveningSender.SendAsync` | After `store.AddMessagesAsync`, call the channel for each new message. Store the result |
| `EveningMessage` (migration) | `DeliveryStatus` (`stored` / `templateSent` / `voiceSent` / `failed`), `ProviderMessageId`, `DeliveredAt`, `DeliveryError`. "Sent" on the tonight card counts `templateSent` and above, so the number stays true |
| Webhook (new) | `GET /api/whatsapp/webhook` (Meta verification challenge) and `POST /api/whatsapp/webhook`: verify the `X-Hub-Signature-256` HMAC with the app secret; map the button payload (`ev:{token}` / `cu:{token}`) to the stored message; send the voice note; update `DeliveryStatus`. Idempotent per provider message ID |
| Stop (FR-19, D-30) | A text reply of `STOP` (or `ఆపండి` / `ನಿಲ್ಲಿಸಿ` / `நிறுத்து`) in the webhook sets `Child.ParentStoppedAt` and sends one confirmation text. The existing send filter then refuses that child |
| Audio | WhatsApp voice notes need **OGG/Opus**, ≤ 16 MB. Her greeting is recorded in the browser as WebM/Opus: convert once with `ffmpeg` (already planned for audio) to `.ogg`, and upload with `POST /{phone-number-id}/media`. Cache the media ID per greeting (media IDs last 30 days) |
| Secrets | `WhatsApp:AccessToken`, `WhatsApp:AppSecret`, `WhatsApp:PhoneNumberId`, `WhatsApp:VerifyToken` from user-secrets / App Service settings, never in the repo. Model IDs and keys stay in config, as CLAUDE.md requires |
| Names (guardrail 1) | Allowed: WhatsApp is the "final send" to the family. Names still never go to Claude or Sarvam |
| Logging | Every Graph API call logged like `AiCall` (purpose, status, latency, error), with no message text or phone numbers in logs |

## 4. Meta setup (one-time, about 2 hours)

1. **Meta Business portfolio** (business.facebook.com). Owner: Monika or Rahul, to decide.
2. **Meta developer app** → add the **WhatsApp** product. Meta creates a **free test number**.
3. **Register up to 5 recipient numbers** (Monika's test parents, with consent), each verified by an SMS code.
4. **System-user permanent access token**, plus the app secret and a webhook verify token, stored in user-secrets and App Service.
5. **Webhook URL:** `https://slateai-staging.azurewebsites.net/api/whatsapp/webhook`; subscribe to `messages`.
6. **Submit templates** (category **Utility**), in en, kn, te and ta (wording below). Approval usually takes minutes to 24 h, sometimes longer, so **submit on day one.**

No business verification is needed for the test number. A real school number and verification are roadmap.

## 5. Templates to submit

Parameters are positional in WhatsApp (`{{1}}` …); our code fills them from the same values as `EveningSlots`. Quick-reply button labels are fixed text (WhatsApp allows no variables there); the button **payload** carries our token (`ev:{token}`), set per send.

**`evening_absence_v1`** (Utility; body + one quick-reply button)

| Lang | Body | Button |
|---|---|---|
| en | We missed {{1}} today. In class we learned {{2}}. Homework: textbook {{3}}. A classmate is keeping {{1}}'s seat. | ▶ Listen to the teacher |
| kn | ಇಂದು {{1}} ನಮಗೆ ನೆನಪಾದರು. ಇಂದು ತರಗತಿಯಲ್ಲಿ {{2}} ಕಲಿತೆವು. ಮನೆಕೆಲಸ: ಪಠ್ಯಪುಸ್ತಕ {{3}}. ಒಬ್ಬ ಸಹಪಾಠಿ {{1}}ಯ ಜಾಗ ಕಾಯುತ್ತಿದ್ದಾರೆ. | ▶ ಟೀಚರ್ ಧ್ವನಿ ಕೇಳಿ |
| te | ఈ రోజు మేము {{1}}ని గుర్తు చేసుకున్నాం. ఈ రోజు తరగతిలో {{2}} నేర్చుకున్నాం. ఇంటి పని: పాఠ్యపుస్తకం {{3}}. ఒక సహవిద్యార్థి {{1}} సీటును కాపాడుతున్నారు. | ▶ టీచర్ గొంతు వినండి |
| ta | இன்று {{1}} இல்லாதது எங்களுக்குக் குறையாக இருந்தது. இன்று வகுப்பில் {{2}} கற்றோம். வீட்டுப்பாடம்: பாடநூல் {{3}}. ஒரு வகுப்புத் தோழர் {{1}}இன் இருக்கையைக் காத்து வருகிறார். | ▶ ஆசிரியர் குரலைக் கேளுங்கள் |

**`catchup_ready_v1`** (Utility)

| Lang | Body | Button |
|---|---|---|
| en | A short lesson for {{1}} from {{2}}. About 2 minutes. | ▶ Listen |
| kn | {{2}} ಅವರಿಂದ {{1}}ಗಾಗಿ ಒಂದು ಚಿಕ್ಕ ಪಾಠ. ಸುಮಾರು 2 ನಿಮಿಷ. | ▶ ಕೇಳಿ |
| te | {{2}} నుండి {{1}} కోసం ఒక చిన్న పాఠం. సుమారు 2 నిమిషాలు. | ▶ వినండి |
| ta | {{2}} அவர்களிடமிருந்து {{1}}க்கான ஒரு சிறிய பாடம். சுமார் 2 நிமிடங்கள். | ▶ கேளுங்கள் |

The Indic wording follows the approved evening template (te uses the gender-neutral verb from D-72). All of it needs native review (T-26) before or right after submission. A wording change after approval means a new template version (`_v2`).

## 6. Build steps (about 1 day after setup)

| # | Step | Tests (default suite, no network) |
|---|---|---|
| 1 | `IParentChannel` + `DemoChatChannel`; `EveningSender` calls the channel; delivery columns + migration | Existing evening tests stay green; a fake channel records calls |
| 2 | `WhatsAppClient` (typed `HttpClient`): send template, upload media, send audio, send text | Unit tests with a fake `HttpMessageHandler`: request shapes, error mapping |
| 3 | `WhatsAppChannel`: allow-list check, template params from slots, `DeliveryStatus` updates | Unit tests: not allowed → demo; API error → `failed` + log; idempotent re-send |
| 4 | Webhook: verify challenge + signature, button → voice note, STOP → `ParentStoppedAt` | Endpoint tests over `FakeApi`: bad signature 401, unknown token ignored, duplicate event once |
| 5 | Greeting to OGG/Opus conversion + media-ID cache | Unit test on the converter command; one real conversion in the Db or e2e suite if needed |
| 6 | Catch-up path (after T-14/T-15): `catchup_ready_v1` → lesson voice note + link text | Same pattern as step 4 |
| 7 | Manual check on the 5 test phones; screen recording for the video | Logged in `docs/user-testing.md` |

Guardrails still apply:
- nothing is sent unless the template is approved, the lesson is confirmed, consent is Yes and the family hasn't stopped (guardrail 3);
- the catch-up only after `CatchUpPackage.Status == Approved`.

## 7. Risks and fallbacks

| Risk | Fallback |
|---|---|
| Template approval delayed or rejected | Demo chat shows the same message; resubmit with simpler wording; English template as a first approval |
| Parent never taps "Listen" | Text already carries the lesson and homework; voice is a bonus; the tap count is not a metric (D-20, no open counting) |
| Judges can't receive WhatsApp (not registered) | `/demo` chat, fed by the same stored messages; video shows a real phone |
| Token or secret leaks | System-user token in secrets only; rotate if exposed |
| Cost | Utility template per message (small, India rates on Meta's pricing page); replies in the window are free |

## 8. Timeline

| When | What |
|---|---|
| Day 1 (setup) | Meta app + test number + recipients + **submit templates** (≈ 2 h) |
| While templates are reviewed | Steps 1–3 behind the demo channel (no Meta needed) |
| After approval | Steps 4–5; manual check on test phones |
| After T-14/T-15 | Step 6 (catch-up) |

## 9. Questions for Rahul

1. Approve moving FR-16 to MVP (D-73), given the remaining screens?
2. Who owns the Meta Business portfolio and the app?
3. Fine to add `IParentChannel` after `AddMessagesAsync`, and delivery columns on `EveningMessage`?
4. Any objection to `ffmpeg` in the container for the OGG conversion (it's already planned for audio)?
