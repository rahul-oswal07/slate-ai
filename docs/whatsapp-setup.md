# WhatsApp setup (T-40, step 1)

How to switch staging from the demo chat to real WhatsApp for the registered test phones. The code is in `src/api/SlateAi.Api/Features/Delivery/`. Decision D-97 and FR-16 are in `spec.md`, and the original proposal is in [`whatsapp-delivery-plan.md`](whatsapp-delivery-plan.md).

> **For now (T-104, #182):** every WhatsApp delivery sends Meta's `hello_world` instead of our templates, hardcoded in `WhatsAppChannel`, until they are approved. It carries no names, no button and none of our text, so it only proves a message reaches the phone.
>
> **14 Nov (D-98):** Monika submitted `todays_lesson_link_v1`, `todays_lesson_chat_v1` and `catchup_link_v1` (§3), in review. **Approval alone changes nothing:** T-104 must first send them with the right values (today's message after T-88/T-91; the catch-up's lesson names and pages) and check they live in the same WhatsApp Business account as staging's phone number.

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
| `WhatsApp__Recipients__<childId>` | `+91…`, one per registered phone. Child IDs are in `data/seed/demo-class.json`. **On App Service, write the ID without hyphens** (`1e95443fd2d9…`): Azure rejects setting names with `-`, and both forms are read the same. The seeded `parentPhone` values are fictional and never dialled |
| `WhatsApp__Evening__Name`, `WhatsApp__CatchUp__Name` | only if the approved names differ from `evening_absence_v2` / `catchup_ready_v1` |
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

### Submitted 14 Nov by Monika (in review, D-98)

Three templates, each in en, kn, te, ta and hi, in **Monika's** WhatsApp Business account (templates belong to the account they were made in: see T-104 d). Same settings for all: category **Utility**, type Default, variables **Number**, no media, no header, no footer, message validity 12 h. Every URL button: **Visit website**, **Dynamic**, `https://slateai-staging.azurewebsites.net/p/{{1}}` (the code passes the token), sample `abc123`. Meta's language codes are `en`, `kn`, `te`, `ta`, `hi`, matching `WhatsAppTemplate.Languages`.

| Template | Used for | Variables (filled by code) | Buttons |
|---|---|---|---|
| `todays_lesson_link_v1` | Today's message (T-88, T-91) | `{{1}}` child · `{{2}}` lesson name · `{{3}}` page number | URL |
| `todays_lesson_chat_v1` | Today's message in the chat (step 2, T-106) | same | Quick reply, then URL |
| `catchup_link_v1` | Friday catch-up | `{{1}}` child · `{{2}}` target lesson names ("A and B") · `{{3}}` pages with the word ("page 4", "pages 4 and 6") | URL |

Not created: `catchup_chat_v1` (flagged as Marketing in every wording; the catch-up stays link-only).

**`todays_lesson_link_v1`**

| Lang | Body | Button |
|---|---|---|
| en | Class update – {{1}}. Today's lesson: {{2}}, Nalikali Maths page {{3}}. The lesson audio, a home activity and a picture question are ready. Tap the button below to open them. | Open today's lesson |
| kn | ತರಗತಿಯ ಮಾಹಿತಿ – {{1}}. ಇಂದಿನ ಪಾಠ: {{2}}, ನಲಿಕಲಿ ಗಣಿತ ಪುಟ {{3}}. ಪಾಠದ ಧ್ವನಿ, ಮನೆಯಲ್ಲಿ ಮಾಡುವ ಚಟುವಟಿಕೆ ಮತ್ತು ಚಿತ್ರ ಪ್ರಶ್ನೆ ಸಿದ್ಧವಾಗಿವೆ. ತೆರೆಯಲು ಕೆಳಗಿನ ಬಟನ್ ಒತ್ತಿ. | ಇಂದಿನ ಪಾಠ ತೆರೆಯಿರಿ |
| te | తరగతి సమాచారం – {{1}}. ఈ రోజు పాఠం: {{2}}, నలికలి గణితం పేజీ {{3}}. పాఠం ఆడియో, ఇంట్లో చేసే కార్యకలాపం, బొమ్మ ప్రశ్న సిద్ధంగా ఉన్నాయి. తెరవడానికి కింది బటన్ నొక్కండి. | ఈ రోజు పాఠం తెరవండి |
| ta | வகுப்புத் தகவல் – {{1}}. இன்றைய பாடம்: {{2}}, நலிகலி கணிதம் பக்கம் {{3}}. பாட ஒலி, வீட்டில் செய்யும் செயல்பாடு, படக் கேள்வி தயாராக உள்ளன. திறக்க கீழே உள்ள பொத்தானை அழுத்தவும். | பாடத்தைத் திறக்கவும் |
| hi | कक्षा की जानकारी – {{1}}। आज का पाठ: {{2}}, नलि-कलि गणित पृष्ठ {{3}}। पाठ का ऑडियो, घर पर करने की गतिविधि और चित्र वाला प्रश्न तैयार हैं। खोलने के लिए नीचे का बटन दबाएँ। | आज का पाठ खोलें |

**`todays_lesson_chat_v1`** (the English entry reads "Class update, {{1}} ." as submitted; cosmetic)

| Lang | Body | Quick reply | URL button |
|---|---|---|---|
| en | Class update – {{1}}. Today's lesson: {{2}}, Nalikali Maths page {{3}}. Tap "Send it here" to get the teacher's voice note, a home activity and a picture question in this chat, or open the full lesson on the website. | Send it here | Open on website |
| kn | ತರಗತಿಯ ಮಾಹಿತಿ – {{1}}. ಇಂದಿನ ಪಾಠ: {{2}}, ನಲಿಕಲಿ ಗಣಿತ ಪುಟ {{3}}. ಶಿಕ್ಷಕರ ಧ್ವನಿ ಸಂದೇಶ, ಮನೆಯ ಚಟುವಟಿಕೆ ಮತ್ತು ಚಿತ್ರ ಪ್ರಶ್ನೆಯನ್ನು ಇಲ್ಲೇ ಪಡೆಯಲು "ಇಲ್ಲೇ ಕಳುಹಿಸಿ" ಒತ್ತಿ, ಅಥವಾ ಪೂರ್ಣ ಪಾಠವನ್ನು ವೆಬ್‌ಸೈಟ್‌ನಲ್ಲಿ ತೆರೆಯಿರಿ. | ಇಲ್ಲೇ ಕಳುಹಿಸಿ | ವೆಬ್‌ಸೈಟ್ ತೆರೆಯಿರಿ |
| te | తరగతి సమాచారం – {{1}}. ఈ రోజు పాఠం: {{2}}, నలికలి గణితం పేజీ {{3}}. టీచర్ వాయిస్ సందేశం, ఇంటి కార్యకలాపం, బొమ్మ ప్రశ్న ఈ చాట్‌లోనే పొందడానికి "ఇక్కడే పంపండి" నొక్కండి, లేదా పూర్తి పాఠం వెబ్‌సైట్‌లో తెరవండి. | ఇక్కడే పంపండి | వెబ్‌సైట్‌లో తెరవండి |
| ta | வகுப்புத் தகவல் – {{1}}. இன்றைய பாடம்: {{2}}, நலிகலி கணிதம் பக்கம் {{3}}. ஆசிரியரின் குரல் செய்தி, வீட்டுச் செயல்பாடு, படக் கேள்வியை இந்த உரையாடலிலேயே பெற "இங்கே அனுப்பவும்" அழுத்தவும், அல்லது முழுப் பாடத்தை இணையதளத்தில் திறக்கவும். | இங்கே அனுப்பவும் | இணையதளத்தில் திற |
| hi | कक्षा की जानकारी – {{1}}। आज का पाठ: {{2}}, नलि-कलि गणित पृष्ठ {{3}}। शिक्षक का वॉइस संदेश, घर की गतिविधि और चित्र वाला प्रश्न इसी चैट में पाने के लिए "यहीं भेजें" दबाएँ, या पूरा पाठ वेबसाइट पर खोलें। | यहीं भेजें | वेबसाइट पर खोलें |

**`catchup_link_v1`**

| Lang | Body | Button |
|---|---|---|
| en | Class update – {{1}}. Catch-up lessons: {{2}}, Nalikali Maths {{3}}. The lesson audio and a picture question are ready. Tap the button below to open them. | Open catch-up lesson |
| kn | ತರಗತಿಯ ಮಾಹಿತಿ – {{1}}. ತಪ್ಪಿದ ಪಾಠಗಳು: {{2}}, ನಲಿಕಲಿ ಗಣಿತ {{3}}. ಪಾಠದ ಧ್ವನಿ, ಮನೆಯಲ್ಲಿ ಮಾಡುವ ಚಟುವಟಿಕೆ ಮತ್ತು ಚಿತ್ರ ಪ್ರಶ್ನೆ ಸಿದ್ಧವಾಗಿವೆ. ತೆರೆಯಲು ಕೆಳಗಿನ ಬಟನ್ ಒತ್ತಿ. | ಪಾಠ ತೆರೆಯಿರಿ |
| te | తరగతి సమాచారం – {{1}}. తప్పిన పాఠాలు: {{2}}, నలికలి గణితం {{3}}. పాఠం ఆడియో, ఇంట్లో చేసే కార్యకలాపం, బొమ్మ ప్రశ్న సిద్ధంగా ఉన్నాయి. తెరవడానికి కింది బటన్ నొక్కండి. | పాఠం తెరవండి |
| ta | வகுப்புத் தகவல் – {{1}}. தவறிய பாடங்கள்: {{2}}, நலிகலி கணிதம் {{3}}. பாட ஒலி, வீட்டில் செய்யும் செயல்பாடு, படக் கேள்வி தயாராக உள்ளன. திறக்க கீழே உள்ள பொத்தானை அழுத்தவும். | பாடத்தைத் திறக்கவும் |
| hi | कक्षा की जानकारी – {{1}}। छूटे पाठ: {{2}}, नलि-कलि गणित {{3}}। पाठ का ऑडियो, घर पर करने की गतिविधि और चित्र वाला प्रश्न तैयार हैं। खोलने के लिए नीचे का बटन दबाएँ। | पाठ खोलें |

`{{3}}` in the catch-up carries the word for page, so code writes it per language: en "page 4" / "pages 4 and 6", kn "ಪುಟ 4" / "ಪುಟ 4 ಮತ್ತು 6", te "పేజీ 4" / "పేజీలు 4, 6", ta "பக்கம் 4" / "பக்கங்கள் 4, 6", hi "पृष्ठ 4" / "पृष्ठ 4 और 6". Lesson names come from the curriculum in the family's language. Indic wording is a draft for native review (T-26).

**What Meta's category check did (14 Nov):** "Category does not match, will be rejected, suggests Marketing" on the warm evening wording ("We missed {{1}} today…"), on catch-up bodies saying "Lessons missed this week" or "Catch-up … from {teacher}", and on every `catchup_chat_v1` body, even the shortest. Passed: bodies that start "Class update – {{1}}" and name the lesson and page. Keep that shape for any `_v2`.

### Earlier proposal (not submitted: Meta classes this wording as Marketing)

The evening body **copies the wording the teacher approves word for word** (`data/seed/evening-message.json`; the table below copies version **3**, and the wording is at version **4** since T-101, D-99: "textbook" left the text and `{page}` now carries the book's name, so a v4 template needs new bodies), with `{child}` → `{{1}}`, `{lesson}` → `{{2}}` and `{page}` → `{{3}}`. That keeps guardrail 3: WhatsApp shows only text she approved.

Code checks this. `WhatsApp:Evening:WordingVersion` (default `4`, with `Name` `evening_absence_v2`, which is not created at Meta yet) must match the version she approved, or the evening send is skipped (`wording-version`). A test fails if the JSON's version moves without the setting.

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

## 5. Next (step 2, T-106)

The full list is in T-106 (`tasks.md`): after "Send it here" on `todays_lesson_chat_v1`, the teacher's greeting and the lesson audio as voice notes, one text with the concept, activity, page and video link, the picture question with > < = buttons, and the reason for absence as a list message.

- The "Send it here" quick-reply, which opens the 24-hour window.
- The webhook (`GET` verify + `POST` with an `X-Hub-Signature-256` check).
- The greeting as a voice note in the chat. The clip is already Ogg Opus, so no ffmpeg is needed.
- A `STOP` reply sets `ParentStoppedAt`.
