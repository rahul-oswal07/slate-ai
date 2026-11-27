# T0 Set-up · status

What works today and what doesn't. No dated notes here: those go in the task's file ([`docs/tasks/`](../../tasks/README.md)).

Spec: [`spec.md`](spec.md)

## Built

- First-run Welcome, a 10-second greeting with voice consent (T-61), and approving the evening message once (T-44).
- An admin can clear a teacher's voice consent on `/admin/teachers`. That deletes her greeting, and her next visit opens the Welcome again (T-133).
- Step 3 is the weekly star (`/setup/star`, also `/settings/star`): the WhatsApp text and the page's two screens, filled with a child from her class; she approves it once, and nothing goes until she does. A new wording version asks again (T-125, D-118).
- The Welcome has a **What the AI does** card under the three promises: what the AI does, what it never does (attendance, numbers and answers, children's names, sending by itself), and that she checks and approves everything (T-102).

## Known gaps and drafts

- Roadmap (D-70): per-language greetings, withdraw voice consent.
- The kn, te, ta and hi words of step 3 are drafts for Monika (T-125).
- The What the AI does card's words, English included, are drafts for Monika, and the design card doesn't show it yet (T-102).

## Open tasks

Task files that name this screen:

```bash
grep -l "^| Screens | .*T0-setup" docs/tasks/[TM]-*.md
```
