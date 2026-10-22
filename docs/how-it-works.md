# Tappida Paatha ("The missed lesson")

*The whole project in plain words.* For the detail, see [Where to find more](#where-to-find-more) at the end.

---

## The problem (from the organisers, Problem 1C)

In a Kannada-medium government school, about **4 of 36 children are absent each day**. Each missed day is a missed lesson. The teacher:

- can't keep track of who missed what;
- has no class time to reteach one child;
- can't decide whether to move on or repeat a lesson.

Children of migrant families, often Telugu- or Tamil-speaking, fall behind most.

## The idea in one sentence

When a child is absent, the app keeps the family connected every evening, gives the child a short catch-up of what was missed in their home language, and helps the teacher see who is behind. **The teacher spends under a minute a day and types nothing.**

---

## One week, as it happens

*Ravi, Telugu family, absent Tue–Thu*

| When | What happens |
|---|---|
| **Every day, ~12:00** | Attendance arrives from the school system (SATS; simulated for now). The teacher doesn't mark it, only corrects mistakes. |
| **Every day, after class** | The teacher taps the mic and says in Kannada what she taught (30 seconds). The AI works out which textbook lesson it was; she confirms with one tap. The app now knows Ravi missed that lesson. |
| **Every evening Ravi is absent (Tue, Wed, Thu), 6 pm** | His family gets a message with her voice greeting and a short text: *"We missed Ravi today. In class we learned… Homework: page 15. A classmate is keeping his seat."* No AI writes this. It's a fixed message she approved once, and the app fills in the lesson and page. The parent can tap **"Is everything OK?"** and pick a reason (unwell, travel, festival…). |
| **Friday (once a week)** | The AI looks at everything Ravi missed, picks the 1–2 lessons that matter most for next week, and writes a short catch-up in Telugu, which is turned into audio. The teacher listens, reads a Kannada summary, and approves. **Nothing reaches a parent without her approval.** |
| **Friday evening** | Ravi's family opens the catch-up: they listen (her greeting, then the lesson), try a 5-minute activity with sticks or bottle caps, and Ravi taps one of three pictures to answer a question. The app marks him **"caught up"** or **"needs buddy"**. Code does the scoring, not AI. |
| **The day Ravi returns** | The teacher gets a buddy card: a classmate who was present does 10 minutes of sums with him. Children whose families have no phone get this too. |
| **End of week** | The teacher sees how quickly absent children came back, why they were away, and whether to spend 10 minutes recapping a lesson that many missed. |

---

## The teacher's app: what each part is for

### Sign in
Phone number + PIN.
**Why:** each teacher sees only her own class.

### Setup (once)
Records her greeting in her own voice, gives consent, and approves the evening message wording.
**Why:** her voice makes parents listen; approving once means no daily approvals.

### Bottom bar: the five tabs

| Tab | What it shows | Why |
|---|---|---|
| **Today** | Everything for the day on one screen: attendance (arrives by itself), the mic for her lesson, "Added to missed lessons: 4 children", tonight's messages ("Sent tonight to 3 of 4 families"), and "Since yesterday" (which families listened, which children caught up). | Her whole daily task happens here, in under 60 seconds. |
| **Approve** | The week's catch-ups, one per language group. She listens, reads the Kannada summary, ticks, and approves. | The AI writes; she stays in control. This is our main safety guarantee. |
| **Missed** | The record of who missed which lesson, by child or by lesson, and where each child stands (sent, listened, caught up, needs buddy). | Answers problem 1: "can't track who missed what". |
| **Back today** | Buddy cards for children returning today. | Answers problem 2: "no class time to reteach", and covers children without phones. |
| **Week** | How quickly absent children came back, the reasons, and a "recap or move on" nudge. | Answers problem 3: "move on or repeat", and shows our main success measure (return speed). |

---

## The parent's side (no app to install)

- **Evening message (each absent day):** her voice + text + homework page; "Is everything OK?"; "Stop these messages".
- **Catch-up (weekly):** Listen → Activity → Picture question → Done. Big buttons, audio first, works on a shared low-end phone, in Telugu, Kannada or Tamil.
- **How it arrives:** in the demo, a simulated WhatsApp chat on a second phone, so anyone can try the whole loop. Real WhatsApp delivery (voice note + text) is planned: see [`whatsapp-delivery-plan.md`](whatsapp-delivery-plan.md).

---

## What the AI does, and what it never does

### AI (Claude)
- understands her voice note and finds the textbook lesson;
- picks which missed lessons matter for next week;
- writes the catch-up in the home language;
- checks the catch-up before she sees it.

### Sarvam
Turns her voice into text, and the catch-up text into Telugu or Kannada audio.

### Never AI
Attendance, the missed-lesson record, all numbers and counts, scoring the picture answer, choosing the buddy, the evening message text.

### Safety
- Children's and teachers' names are removed before anything goes to AI.
- The computer voice never says names.
- Nothing reaches a parent without the teacher's approval.
- Invalid AI answers fall back to rules.

---

## How this scores against the judging criteria

### Efficacy and user centricity (35%)
- Built for the real constraints: Kannada, voice-first, under a minute a day, works offline, low-literacy parents, shared phones, no child devices.
- Three features came from the education mentor (evening message, reason, return speed).
- Usability testing with teachers and parents, and the changes made from it, are logged in [`user-testing.md`](user-testing.md).

### Effective AI and technical strength (30%)
- AI used only where it adds value.
- Guardrails everywhere: approval gate, name removal, validated outputs, fallbacks.
- Tests and AI evals in the repo; real speech in Indian languages.

### Innovation (25%)
The catch-up is built from this teacher's own lesson today, compressed to what blocks next week, in the child's home language, closed by a picture check and a buddy in class. Existing tools send the same content to everyone.

### Roadmap (10%)
Real SATS / state attendance feed, real WhatsApp at scale, school setup and Settings, more subjects and grades, a record that follows migrant children.

---

## Built so far vs still to build

*As of 23 Oct 2026; the live state is in [`status.md`](status.md).*

| ✅ Built | ⏳ Still to build |
|---|---|
| Sign-in | The catch-up writer + audio |
| Today: attendance + voice lesson + confirm | The Approve screen |
| Evening message (server side) | The parent pages |
| The "which lessons matter" AI step | Missed, Back today, Week |
| | The demo screen for judges |
| | WhatsApp delivery |

**Deliberately left out of the MVP (roadmap):** Settings, greetings in several languages, editing family details, bulk approve, extra polish.

---

## Where to find more

| For | Read |
|---|---|
| Why we're building this, and for whom | [`intent.md`](intent.md) |
| Requirements and every decision (FR-xx, D-xx) | [`spec.md`](spec.md) |
| What to build first, and what is roadmap | [`design/mvp-build-flow.md`](design/mvp-build-flow.md) |
| Each screen in detail (states, copy, checks) | [`design/README.md`](design/README.md) and the cards it lists |
| The clickable design prototype | [`design/prototype/index.html`](design/prototype/index.html) (download, open in a browser) |
| How the AI is tested | [`quality.md`](quality.md) |
| Data and privacy | [`database.md`](database.md), `spec.md` §7 |
