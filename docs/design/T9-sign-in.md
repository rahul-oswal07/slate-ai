# T9 Sign in

Spec: new FR (see D-40) · Tier: MVP · Task: T-47 · Prototype: prototype/index.html (step 1, `login`; TNAMES "T9 Sign in") · PNG: none exported
Decisions: D-40 (phone number + PIN, remembered phone, lock), D-41 (school setup: demo seed now, CSV later), D-42 (printed start sheet: roadmap), D-46 (one school, one class, one teacher)

The first screen the teacher sees. She signs in with the phone number and PIN the school gave her. The phone remembers her; later sign-ins ask for the PIN only. After the first sign-in she goes to [T0 Set up](T0-setup.md); Welcome shows until `ConsentAt` is set (D-40). OTP and the printed start sheet are roadmap (D-42).

## 1. User voice

- Says: "The headmaster gave me a number and a PIN. Let me in once and don't ask me every day."
- Unsaid: Can someone else open my class on this phone? What if I forget the PIN in front of the children? Is this tracking where I am?

## 2. Who does what

- Human: types her phone number (pre-filled after the first time) and the 4–6 digit PIN, taps Sign in. Adds the app to the Home screen if she wants.
- AI: none. Mode: none.
- Code (no AI): checks phone + PIN against the teacher record from the seed or the CSV import (D-41); hashes PINs; remembers the phone on the device; issues a 30-day session; counts wrong PINs and locks sign-in for 15 minutes after 5; routes to T0 Welcome until `ConsentAt` is set, otherwise to T1 Today (D-40).

## 3. Always / never

- Always: say what went wrong in plain words and where to get a new PIN (the school).
- Always: keep the phone number on the device after the first sign-in; ask for the PIN only, with "Not you? Change phone" (D-40).
- Never: show the PIN in clear text, log it, or send it anywhere but the sign-in endpoint.
- Never: send the teacher's phone number or name to Claude, Sarvam or WhatsApp templates (guardrail 1).
- Never: say whether the phone number or the PIN was the wrong part (no account discovery).
- Uncertainty: none (no AI).
- If it goes wrong: a wrong PIN locks her out in class. Limited by: 5 tries before a 15-minute lock, a clear message pointing to the school for a new PIN, and a 30-day session so she rarely signs in.

## 4. States

| State | What the user sees | Copy (en) |
|---|---|---|
| Default, first time (`default`) | App bar "Sign in"; book icon; "Tappida Paatha"; lead; "Phone number" field (tel); "PIN (4 digits)" field (password, numeric); primary "Sign in"; hint about remembering the phone and Add to Home screen. No Back, no bottom nav | "Use the phone number and PIN from your school." · "Sign in" |
| Wrong PIN (`wrongpin`) | Error banner (alert icon, `role="alert"`) above the fields; fields keep their values | "That PIN does not match. Check the sheet from your school, or ask them for a new one." |
| PIN only, remembered phone | Not in prototype (from D-40). The phone number shows as text with "Not you? Change phone"; only the PIN field and "Sign in" | "Not you? Change phone" |
| Locked for 15 minutes | Not in prototype (from D-40). After 5 wrong PINs: error banner, PIN field and "Sign in" disabled, the time she can try again | proposed: "Too many wrong PINs. Try again at {time}, or ask your school for a new PIN." |
| Loading | Not designed. "Sign in" shows a spinner and stays disabled until the reply | — |
| Offline | Not designed. Sign-in needs network once; proposed banner "No network. Sign in needs network the first time." | see Open items |
| Empty | none: one screen with two fields | — |
| Permission denied | none: no mic or camera | — |

## 5. Copy

kn and en from the prototype. te "—" (teacher UI). Reviewed by: needs review (all rows).

| Key | kn | te | ta | en | Reviewed by |
|---|---|---|---|---|---|
| `signin.title` (app bar, region label) | ಒಳಗೆ ಬನ್ನಿ | — | needs translation | Sign in | needs review |
| `app.name` | ತಪ್ಪಿದ ಪಾಠ | — | needs translation | Tappida Paatha | needs review |
| `signin.lead` | ನಿಮ್ಮ ಶಾಲೆ ಕೊಟ್ಟ ಫೋನ್ ನಂಬರ್ ಮತ್ತು PIN ಬಳಸಿ. | — | needs translation | Use the phone number and PIN from your school. | needs review |
| `signin.phone` | ಫೋನ್ ನಂಬರ್ | — | needs translation | Phone number | needs review |
| `signin.pin` | PIN (4 ಅಂಕಿ) | — | needs translation | PIN (4 digits) | needs review |
| `signin.submit` | ಒಳಗೆ ಬನ್ನಿ | — | needs translation | Sign in | needs review |
| `signin.hint` | ಒಮ್ಮೆ ಒಳಗೆ ಬಂದ ನಂತರ ಈ ಫೋನಿನಲ್ಲಿ ನೆನಪಿಡುತ್ತದೆ. Chrome ⋮ → “Home screen‌ಗೆ ಸೇರಿಸಿ”. | — | needs translation | After the first time, this phone remembers you. Chrome ⋮ → “Add to Home screen”. | needs review |
| `signin.wrongPin` | PIN ಹೊಂದುತ್ತಿಲ್ಲ. ಶಾಲೆ ಕೊಟ್ಟ ಹಾಳೆ ನೋಡಿ, ಅಥವಾ ಹೊಸ PIN ಕೇಳಿ. | — | needs translation | That PIN does not match. Check the sheet from your school, or ask them for a new one. | needs review |
| `signin.changePhone`  | needs translation | — | needs translation | Not you? Change phone | needs review |
| `signin.locked`  | needs translation | — | needs translation | Too many wrong PINs. Try again at {time}, or ask your school for a new PIN. | needs review |
| `announce.signedIn` | ಒಳಗೆ ಬಂದಿದ್ದೀರಿ | — | needs translation | Signed in | needs review |

## 6. Tokens and sizes

- Page `bg-canvas`; app name `text-title` (22 px) centred; lead `text-body text-ink-muted`.
- Labels `text-label` bold; inputs `min-h-target w-full border border-border rounded-md bg-surface text-ink`, 8–12 px padding; PIN with letter spacing; focus ring 3 px `focus`.
- Primary "Sign in" `min-h-target-primary w-full bg-primary text-on-primary rounded-md`.
- Error banner `bg-error-bg border-l-4 border-l-error`, alert icon 18 px, text `text-ink`.
- Hint `text-caption text-ink-muted`.

## 7. Acceptance checklist

- [ ] Every state above is built and reachable (including the two from D-40 not in the prototype)
- [ ] One primary action; works with one thumb at 360 px; "Sign in" not hidden by the keyboard
- [ ] Status shown as icon + word, never colour alone
- [ ] Targets ≥ 48 px; 8 px apart
- [ ] No AI on this screen; nothing labelled AI
- [ ] Every Indic text has a `lang` attribute; strings exist in kn, en, te, ta
- [ ] axe: 0 serious/critical, in light and dark; both fields have visible labels
- [ ] Phone `type="tel"` `inputmode="tel"` `autocomplete="tel"`; PIN `type="password"` `inputmode="numeric"`, 4–6 digits (D-40)
- [ ] Sign in with the school's phone number + PIN; PINs stored hashed; teacher record from the demo seed (D-41)
- [ ] The phone is remembered: later sign-ins show the PIN only, with "Not you? Change phone" which clears the remembered phone (D-40)
- [ ] Session lasts 30 days; no sign-in needed within that time (D-40)
- [ ] 5 wrong PINs lock sign-in for 15 minutes; the lock is enforced in the API, not only the UI; endpoint test with fakes (D-40)
- [ ] Wrong PIN shows the error banner with `role="alert"`; the message does not reveal whether the phone number exists
- [ ] After sign-in: T0 Welcome until `ConsentAt` is set, otherwise T1 Today (D-40); focus moves to the next screen's title; polite "Signed in"
- [ ] The PIN never appears in logs; phone number and name never sent to AI services (guardrail 1)

## Open items

- The prototype label says "PIN (4 digits)" and `maxlength="4"`; D-40 allows 4–6 digits. Change the label to "PIN" or "PIN (4–6 digits)".
- The wrong-PIN copy says "Check the sheet from your school", but the printed start sheet is roadmap (D-42). Reword to "Check the PIN from your school" until the sheet exists.
- Offline, loading and lock states are not in the prototype.
- OTP sign-in is roadmap (D-40).
