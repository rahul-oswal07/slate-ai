# WhatsApp setup (T-40, step 1)

How to switch staging from the demo chat to real WhatsApp for the registered test phones. The code is in `src/api/SlateAi.Api/Features/Delivery/`. Decision D-90 and FR-16 are in `spec.md`, and the original proposal is in [`whatsapp-delivery-plan.md`](whatsapp-delivery-plan.md).

**How it works:**
- When an evening message or catch-up is sent (guardrail 3 already passed), a `ParentDeliveries` row is queued in the same transaction.
- Every minute, the 6 pm job sends queued rows over `Delivery:Channel`. "Send messages now" on `/demo` does the same straight away.
- With `whatsapp`, only children listed in `WhatsApp:Recipients` get a message: the approved template, with a button that opens `/p/{token}`. Everyone else stays on the demo chat.

## 1. Meta (one time, about 2 hours)

1. **Business portfolio:** create one at business.facebook.com, if there isn't one yet.
2. **App:** developers.facebook.com → Create app → type *Business* → add the **WhatsApp** product. Meta gives the app a free **test number**. On *WhatsApp → API Setup*, copy the **Phone number ID**.
3. **Recipients:** on the same page, add up to **5** numbers (the test parents, with their consent). Each one confirms with a code sent to their WhatsApp.
4. **Permanent token:**
   1. Business settings → Users → System users → Add (admin).
   2. Assign the app with full control.
   3. Generate a token with `whatsapp_business_messaging` and `whatsapp_business_management`. The temporary token on API Setup expires in 24 h, so don't use that one.
5. **Templates:** WhatsApp Manager → Message templates → Create. Use category **Utility** and the wording in §3 below. Fill in the sample values Meta asks for. **Submit English first.** Approval takes minutes to 24 h. Do this on day one.

## 2. App settings

Secrets go only in user-secrets (local) or App Service settings (staging), never in the repo.

| Setting | Value |
|---|---|
| `Delivery__Channel` | `whatsapp` (default `demo`: nothing leaves the server) |
| `WhatsApp__PhoneNumberId` | from API Setup |
| `WhatsApp__AccessToken` | the system-user token (**secret**) |
| `WhatsApp__Recipients__<childId>` | `+91…`, one per registered phone. Child IDs are in `data/seed/demo-class.json`. The seeded `parentPhone` values are fictional and never dialled |
| `WhatsApp__Evening__Name`, `WhatsApp__CatchUp__Name` | only if the approved names differ from `evening_absence_v1` / `catchup_ready_v1` |
| `WhatsApp__Evening__Languages__te` (etc.) | only to stop a language that isn't approved yet: remove it or point it elsewhere. A language missing from the map is skipped |

Local run with one phone:

```bash
cd src/api/SlateAi.Api
dotnet user-secrets set "Delivery:Channel" "whatsapp"
dotnet user-secrets set "WhatsApp:PhoneNumberId" "<id>"
dotnet user-secrets set "WhatsApp:AccessToken" "<token>"
dotnet user-secrets set "WhatsApp:Recipients:<childId>" "+91<number>"
```

The button URL is fixed when the template is created, so a local run's button opens staging's `/p/{token}`, and that token isn't on staging. For a full local check, run step 3 below on staging.

## 2b. Check the setup before the templates are approved

Meta's `hello_world` template is approved on every test number. The admin test send uses it to check the token, the Phone number ID and one recipient straight away.

How the endpoint works:
- It runs only where `Admin__Key` is set (staging, not prod), and works whatever `Delivery:Channel` is set to.
- A repeated `clientRequestId` returns the first result without sending again.
- The response never contains the number. The message ID is a SHA-256 hash, because WhatsApp's raw `wamid` encodes the number.

```bash
curl -s -X POST https://slateai-staging.azurewebsites.net/api/admin/whatsapp/test \
  -H "X-Admin-Key: $ADMIN_KEY" -H "Content-Type: application/json" \
  -d "{\"clientRequestId\":\"$(uuidgen)\",\"childId\":\"<childId in WhatsApp__Recipients>\"}"
```

`{"outcome":"sent","providerMessageId":"sha256:…"}` means the phone gets "Hello World" within seconds. The error codes are in the table in §4: `graph-190` is the token, `graph-131030` is the number not being on the recipient list.

## 3. Template wording

The evening body **copies the wording the teacher approves word for word** (`data/seed/evening-message.json`, version **3**), with `{child}` → `{{1}}`, `{lesson}` → `{{2}}` and `{page}` → `{{3}}`. That keeps guardrail 3: WhatsApp shows only text she approved.

Code checks this. `WhatsApp:Evening:WordingVersion` (default `3`) must match the version she approved, or the evening send is skipped (`wording-version`). A test fails if the JSON's version moves without the setting.

**A wording change means:** new Meta templates (`evening_absence_v2`), then update `Name` and `WordingVersion`.

**`evening_absence_v1`** (Utility)
- Body: text below.
- Button: **Visit website**, dynamic URL `https://slateai-staging.azurewebsites.net/p/{{1}}` (prod: its own host and its own template name).

| Lang | Body | Button text |
|---|---|---|
| en | We missed {{1}} today. In class we learned {{2}}. Homework: textbook {{3}}. A classmate is keeping {{1}}’s seat. | Listen to the teacher |
| kn | ಇಂದು {{1}} ನಮಗೆ ನೆನಪಾದರು. ಇಂದು ತರಗತಿಯಲ್ಲಿ {{2}} ಕಲಿತೆವು. ಮನೆಕೆಲಸ: ಪಠ್ಯಪುಸ್ತಕ {{3}}. ಒಬ್ಬ ಸಹಪಾಠಿ {{1}}ಯ ಜಾಗ ಕಾಯುತ್ತಿದ್ದಾರೆ. | ಟೀಚರ್ ಧ್ವನಿ ಕೇಳಿ |
| te | ఈ రోజు మేము {{1}}ని గుర్తు చేసుకున్నాం. ఈ రోజు తరగతిలో {{2}} నేర్చుకున్నాం. ఇంటి పని: పాఠ్యపుస్తకం {{3}}. ఒక సహవిద్యార్థి {{1}} సీటును కాపాడుతున్నారు. | టీచర్ గొంతు వినండి |
| ta | இன்று {{1}} இல்லாதது எங்களுக்குக் குறையாக இருந்தது. இன்று வகுப்பில் {{2}} கற்றோம். வீட்டுப்பாடம்: பாடநூல் {{3}}. ஒரு வகுப்புத் தோழர் {{1}}இன் இருக்கையைக் காத்து வருகிறார். | ஆசிரியர் குரலைக் கேளுங்கள் |
| hi | आज हमें {{1}} की कमी खली। आज कक्षा का विषय था: {{2}}। गृहकार्य: पाठ्यपुस्तक {{3}}। एक सहपाठी {{1}} की सीट का ध्यान रख रहे हैं। | शिक्षक की आवाज़ सुनें |

**`catchup_ready_v1`** (Utility). Parameters: `{{1}}` child, `{{2}}` teacher (the class's main teacher's display name). Same URL button.

| Lang | Body | Button text |
|---|---|---|
| en | A short lesson for {{1}} from {{2}}. About 2 minutes. | Listen |
| kn | {{2}} ಅವರಿಂದ {{1}}ಗಾಗಿ ಒಂದು ಚಿಕ್ಕ ಪಾಠ. ಸುಮಾರು 2 ನಿಮಿಷ. | ಕೇಳಿ |
| te | {{2}} నుండి {{1}} కోసం ఒక చిన్న పాఠం. సుమారు 2 నిమిషాలు. | వినండి |
| ta | {{2}} அவர்களிடமிருந்து {{1}}க்கான ஒரு சிறிய பாடம். சுமார் 2 நிமிடங்கள். | கேளுங்கள் |
| hi | {{2}} की ओर से {{1}} के लिए एक छोटा पाठ। लगभग 2 मिनट। | सुनें |

Notes on the wording:
- Button texts are 25 characters at most and can't contain variables.
- Indic wording is a draft for native review (T-26). The catch-up and Hindi lines are new copy, so Monika should review them before submitting.

**Meta sample values:** `Ravi`, `Subtraction with borrowing` and `page 15` for the evening template, plus any token, e.g. `abc123`.

## 4. Check it worked

1. Staging: set the settings above, then Reset on `/demo`.
2. Mark the registered child absent, confirm a lesson, approve the evening message, then tap **Send messages now**.
3. The phone gets the template, and the button opens the parent page with her greeting.
4. Tap Send again: no second message arrives.
5. In Log stream, the delivery shows `sent` with an attempt number. Grep the logs for the phone number and the child's name: there should be no hits.
6. Write the result in `docs/user-testing.md` (Monika).

What a skipped or failed delivery means (`ParentDeliveries.LastError`):

| Code | Meaning |
|---|---|
| `demo` | `Delivery:Channel` is `demo` |
| `no-recipient` | the child isn't in `WhatsApp:Recipients` |
| `no-template` | the family's language isn't in the template's `Languages` |
| `wording-version` | she approved another wording version than the Meta template copies |
| `stopped`, `no-consent`, `expired` | the send rules said no at send time |
| `not-configured` | `PhoneNumberId` or `AccessToken` missing |
| `graph-132001` | the template name or language doesn't exist or isn't approved |
| `graph-131030` | the number isn't on the test number's recipient list |
| `graph-190` | the token expired or is wrong |
| `http-5xx`, `graph-131048`, `timeout`, … | temporary; tried again each minute, 3 tries in all |

## 5. Next (step 2, separate task)

- The "Listen" quick-reply, which opens the 24-hour window.
- The webhook (`GET` verify + `POST` with an `X-Hub-Signature-256` check).
- The greeting as a voice note in the chat. The clip is already Ogg Opus, so no ffmpeg is needed.
- A `STOP` reply sets `ParentStoppedAt`.
